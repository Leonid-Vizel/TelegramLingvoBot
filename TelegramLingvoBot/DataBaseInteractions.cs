using MySql.Data.MySqlClient;
using System.Data.Common;

namespace TelegramLingvoBot
{
    /// <summary>
    /// Класс взаимодейтсвия с базой
    /// </summary>
    internal class DataBaseInteractions
    {
        /// <summary>
        /// Строка подключения к баззе данных, которая будет использвана в последующих запросах через этот объект
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="connectionString">Строка подключения к баззе данных, которая будет использвана в последующих запросах через этот объект</param>
        public DataBaseInteractions(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public MySqlConnection GetConnection() => new MySqlConnection(ConnectionString);

        /// <summary>
        /// Устанавливет кодировку UTF-8 для подключения
        /// </summary>
        /// <param name="connection">Подключение, для которого будет выполнен запрос</param>
        private async Task SetUTF8Async(MySqlConnection connection)
        {
            using (MySqlCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SET NAMES `utf8`;\nSET CHARACTER SET 'utf8';\nSET SESSION collation_connection = 'utf8_general_ci';";
                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Добавляет указанного пользователя в базу
        /// </summary>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <param name="user">Объект пользователя, которого хотите добавить в базу</param>
        public async Task AddUser(User user, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO users(id, QuestionAmount, DialogPosition, QuestionReady) VALUES({ user.Id}, {user.QuestionAmount}, {(int)user.Position}, {Convert.ToInt32(user.QuestionReady)});";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO users(id, QuestionAmount, DialogPosition, QueestionReady) VALUES({ user.Id}, {user.QuestionAmount}, {(int)user.Position}, {Convert.ToInt32(user.QuestionReady)});";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Читает из базы всех пользователей
        /// </summary>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Список пользователей, сохранённых в базе</returns>
        public async Task<List<User>> GetAllUsers(MySqlConnection? connectionInput = null)
        {
            List<User> users = new List<User>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT * FROM users;";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    users.Add(new User(
                                            reader.GetInt64(0),
                                            (DialogPosition)reader.GetInt32(2),
                                            reader.GetInt32(1), reader.GetBoolean(3)));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                await connectionInput.OpenAsync();
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM users;";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                users.Add(new User(
                                        reader.GetInt64(0),
                                        (DialogPosition)reader.GetInt32(2),
                                        reader.GetInt32(1), true));
                            }
                        }
                    }
                }
            }
            return users;
        }

        /// <summary>
        /// Читает из базы всех учителей
        /// </summary>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Список учителей, сохранённых в базе</returns>
        public async Task<List<Teacher>> GetAllTeachers(MySqlConnection? connectionInput = null)
        {
            List<Teacher> teachers = new List<Teacher>();
            List<Teacher> teachersToProcess = new List<Teacher>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT * FROM teachers";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    Teacher teacher = new Teacher(reader.GetInt64(0), reader.GetDecimal(1), (DialogPosition)reader.GetInt32(2));
                                    object answerObj = reader.GetValue(3);
                                    if (answerObj != DBNull.Value && answerObj != null)
                                    {
                                        teacher.CurrentAnswer = new Answer((long)answerObj);
                                        teachersToProcess.Add(teacher);
                                    }
                                    else
                                    {
                                        teacher.CurrentAnswer = null;
                                    }
                                    teachers.Add(teacher);
                                }
                            }
                        }
                    }
                    foreach(Teacher teacher in teachersToProcess)
                    {
                        teacher.CurrentAnswer = await GetAnswer(teacher.CurrentAnswer.Id, connection);
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM teachers";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                Teacher teacher = new Teacher(reader.GetInt64(0), reader.GetDecimal(1), (DialogPosition)reader.GetInt32(2));
                                object answerObj = reader.GetValue(3);
                                if (answerObj != DBNull.Value && answerObj != null)
                                {
                                    teacher.CurrentAnswer = await GetAnswer((long)answerObj, connectionInput);
                                }
                                else
                                {
                                    teacher.CurrentAnswer = null;
                                }
                                teachers.Add(teacher);
                            }
                        }
                    }
                }
            }
            return teachers;
        }

        /// <summary>
        /// Обновляет запись о пользователе в базе данных
        /// </summary>
        /// <param name="user">Данные для обновления</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task UpdateUser(User user, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE users SET QuestionAmount = {user.QuestionAmount}, DialogPosition = {(int)user.Position}, QuestionReady = {Convert.ToInt32(user.QuestionReady)}  WHERE id={user.Id}";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"UPDATE users SET QuestionAmount = {user.QuestionAmount}, DialogPosition = {(int)user.Position}, QuestionReady = {Convert.ToInt32(user.QuestionReady)}  WHERE id={user.Id}";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Обновляет только данные о количетсве оплаченных вопросов пользователя
        /// </summary>
        /// <param name="user">Данные для обновления</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task UpdateUserQuestionAmount(User user, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE users SET QuestionAmount = {user.QuestionAmount} WHERE id={user.Id}";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"UPDATE users SET QuestionAmount = {user.QuestionAmount} WHERE id={user.Id}";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Обновляет только данные о позиции пользователя
        /// </summary>
        /// <param name="user">Данные для обновления</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task UpdateUserDialogPosition(User user, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE users SET DialogPosition = {(int)user.Position} WHERE id={user.Id}";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"UPDATE users SET DialogPosition = {(int)user.Position} WHERE id={user.Id}";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Обновляет только данные о готовность нового вопроса для пользователя
        /// </summary>
        /// <param name="user">Данные для обновления</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task UpdateUserQuestionReady(User user, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE users SET QuestionReady = {Convert.ToInt32(user.QuestionReady)} WHERE id={user.Id}";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"UPDATE users SET QuestionReady = {Convert.ToInt32(user.QuestionReady)} WHERE id={user.Id}";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Обновляет запись об учителе в базе данных
        /// </summary>
        /// <param name="user">Данные для обновления</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task UpdateTeacher(Teacher teacher, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE teachers SET Balance = {teacher.Balance}, DialogPosition = {(int)teacher.Position} WHERE id={teacher.Id}";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"UPDATE teachers SET Balance = {teacher.Balance}, DialogPosition = {(int)teacher.Position} WHERE id={teacher.Id}";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Задаёт значение для проверяемого в данный момент ответа
        /// </summary>
        /// <param name="teacherId">Идентификатор учителя</param>
        /// <param name="answerId">Идентификатор работы или null</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task UpdateTeacherAnswerId(long teacherId, long? answerId, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        if (answerId == null)
                        {
                            command.CommandText = $"UPDATE teachers SET CurrentAnswerId = null WHERE Id = {teacherId};";
                        }
                        else
                        {
                            command.CommandText = $"UPDATE teachers SET CurrentAnswerId = {answerId} WHERE Id = {teacherId};";
                        }
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    if (answerId == null)
                    {
                        command.CommandText = $"UPDATE teachers SET CurrentAnswerId = null WHERE Id = {teacherId};";
                    }
                    else
                    {
                        command.CommandText = $"UPDATE teachers SET CurrentAnswerId = {answerId} WHERE Id = {teacherId};";
                    }
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Считывает из базы сначала количетство вопросов впринципе, потом количетсво вопросов, которое пользователь уже решил
        /// </summary>
        /// <param name="userId">Идентиикатор пользователя для поиска в БД</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Количество доступных пользователю вопросов из БД</returns>
        public async Task<long> GetUserAvailibleQuestionsAmount(long userId, MySqlConnection? connectionInput = null)
        {
            long total = 0;
            long used = 0;
            object? result;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT Count(*) FROM questions;";
                        result = await command.ExecuteScalarAsync();
                        total = (result == null || result == DBNull.Value) ? 0 : (long)result;
                    }
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(DISTINCT(QuestionId)) FROM answers WHERE UserId = {userId};";
                        result = await command.ExecuteScalarAsync();
                        used = (result == null || result == DBNull.Value) ? 0 : (long)result;
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT Count(*) FROM questions;";
                    result = await command.ExecuteScalarAsync();
                    total = (result == null || result == DBNull.Value) ? 0 : (long)result;
                }
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(DISTINCT(QuestionId)) FROM answers WHERE UserId = {userId};";
                    result = await command.ExecuteScalarAsync();
                    used = (result == null || result == DBNull.Value) ? 0 : (long)result;
                }
            }
            return total - used;
        }

        /// <summary>
        /// Считывает количество ответов, которое было проверено учителем
        /// </summary>
        /// <param name="teacherId">Идентификатор учителя для посика в БД</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Количество ответов, которое было проверено учителем </returns>
        public async Task<long> GetCountOfVerifiedAnswersOfTeacher(long teacherId, MySqlConnection? connectionInput = null)
        {
            long countOfVerifiedAnswers = 0;
            object? result;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(*) FROM answers WHERE TeacherId={teacherId}";
                        result = await command.ExecuteScalarAsync();
                        countOfVerifiedAnswers = (result == null || result == DBNull.Value) ? 0 : (long)result;
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM answers WHERE TeacherId={teacherId}";
                    result = await command.ExecuteScalarAsync();
                    countOfVerifiedAnswers = (result == null || result == DBNull.Value) ? 0 : (long)result;
                }
            }
            return countOfVerifiedAnswers;
        }

        /// <summary>
        /// Считывает все ответы, пользователя.
        /// </summary>
        /// <param name="userId">Индентификатор пользователя для поиска в БД</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Список всех ответов пользователя</returns>
        public async Task<List<Answer>> GetAnswersOfUser(long userId, MySqlConnection? connectionInput = null)
        {
            List<Answer> answers = new List<Answer>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT * FROM answers WHERE UserId = {userId};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    object rateRead = reader.GetValue(4);
                                    object commentRead = reader.GetValue(5);
                                    object teacherRead = reader.GetValue(6);
                                    int? rate = rateRead == DBNull.Value ? null : (int)rateRead;
                                    string? comment = commentRead == DBNull.Value ? null : (string)commentRead;
                                    long? teacherId = teacherRead == DBNull.Value ? null : (long)teacherRead;
                                    answers.Add(new Answer(reader.GetInt64(0),
                                            reader.GetInt64(1),
                                            new Question(reader.GetInt32(2)),
                                            reader.GetTextReader(3).ReadToEnd(),
                                            rate, comment, teacherId));
                                }
                            }
                        }
                    }
                    List<Question> questions = new List<Question>();
                    foreach (Answer answer in answers)
                    {
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = $"SELECT ThemeId,Text FROM questions WHERE Id = {answer.Question.Id};";
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows)
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        answer.Question.Theme = new Theme(reader.GetInt32(0));
                                        answer.Question.Text = reader.GetTextReader(1).ReadToEnd();
                                        questions.Add(answer.Question);
                                    }
                                }
                            }
                        }
                    }

                    foreach (Question question in questions)
                    {
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = $"SELECT Text FROM themes WHERE Id = {question.Theme.Id};";
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows)
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        question.Theme.Name = reader.GetTextReader(0).ReadToEnd();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM answers WHERE UserId = {userId};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                object rateRead = reader.GetValue(4);
                                object commentRead = reader.GetValue(5);
                                object teacherRead = reader.GetValue(6);
                                int? rate = rateRead == DBNull.Value ? null : (int)rateRead;
                                string? comment = commentRead == DBNull.Value ? null : (string)commentRead;
                                long? teacherId = teacherRead == DBNull.Value ? null : (long)teacherRead;
                                answers.Add(new Answer(reader.GetInt64(0),
                                        reader.GetInt64(1),
                                        new Question(reader.GetInt32(2)),
                                        reader.GetTextReader(3).ReadToEnd(),
                                        rate, comment, teacherId));
                            }
                        }
                    }
                }
                List<Question> questions = new List<Question>();
                foreach (Answer answer in answers)
                {
                    using (MySqlCommand command = connectionInput.CreateCommand())
                    {
                        command.CommandText = $"SELECT ThemeId,Text FROM questions WHERE Id = {answer.Question.Id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    answer.Question.Theme = new Theme(reader.GetInt32(0));
                                    answer.Question.Text = reader.GetTextReader(1).ReadToEnd();
                                    questions.Add(answer.Question);
                                }
                            }
                        }
                    }
                }

                foreach (Question question in questions)
                {
                    using (MySqlCommand command = connectionInput.CreateCommand())
                    {
                        command.CommandText = $"SELECT Text FROM themes WHERE Id = {question.Theme.Id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    question.Theme.Name = reader.GetTextReader(0).ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
            return answers;
        }

        /// <summary>
        /// Добавляет инфорамцию об ответе пользователя в БД
        /// </summary>
        /// <param name="answer">Ответ для добавления в БД</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task AddAnswer(Answer answer, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO answers (UserId, QuestionId, Text, Rate, Comment, TeacherId) VALUES ({answer.UserId}, {answer.Question.Id}, '{answer.Text}', null, null, null);";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO answers (UserId, QuestionId, Text, Rate, Comment, TeacherId) VALUES ({answer.UserId}, {answer.Question.Id}, '{answer.Text}', null, null, null);";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Обновляет информацию об ответе пользователя
        /// </summary>
        /// <param name="work">Объект ответа пользователя для обновления данных</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task UpdateAnswer(Answer work, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE answers SET Rate={work.Rate}, Comment='{work.Comment}', TeacherId={work.TeacherId} WHERE id={work.Id};";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"UPDATE answers SET Rate={work.Rate}, Comment='{work.Comment}', TeacherId={work.TeacherId} WHERE id={work.Id};";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Получет первый попавшийся непроверенный ответ
        /// </summary>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Первый попавшийся непроверенный ответ или null, если его нет</returns>
        public async Task<Answer?> GetFirstAnswer(MySqlConnection? connectionInput = null)
        {
            Answer? answer = null;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    List<long> ids = new List<long>();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT CurrentAnswer FROM teachers WHERE CurrentAnswer is not null";
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    ids.Add(reader.GetInt64(0));
                                }
                            }
                        }
                    }

                    List<Answer> answers = new List<Answer>();

                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT * FROM answers WHERE Rate is null";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                object idRead = reader.GetValue(0);
                                object userIdRead = reader.GetValue(1);
                                object QuestionIdRead = reader.GetValue(2);
                                object textOfUserRead = reader.GetValue(3);
                                long id = (long)idRead;
                                long userId = (long)userIdRead;
                                int QuestionId = (int)QuestionIdRead;
                                string textOfUser = (string)textOfUserRead;
                                answers.Add(new Answer(id, userId, new Question(QuestionId), textOfUser, null, null, null));
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }

                    answer = answers.Where(x => !ids.Contains(x.Id)).FirstOrDefault();

                    if (answer == null)
                    {
                        return null;
                    }

                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT ThemeId,Text FROM questions WHERE id = {answer.Question.Id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                answer.Question.Theme = new Theme(reader.GetInt32(0));
                                answer.Question.Text = reader.GetTextReader(1).ReadToEnd();
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT Text FROM themes WHERE id = {answer.Question.Theme.Id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                answer.Question.Theme.Name = reader.GetTextReader(0).ReadToEnd();
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM answers WHERE Rate is null";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            object idRead = reader.GetValue(0);
                            object userIdRead = reader.GetValue(1);
                            object QuestionIdRead = reader.GetValue(2);
                            object textOfUserRead = reader.GetValue(3);
                            long id = (long)idRead;
                            long userId = (long)userIdRead;
                            int QuestionId = (int)QuestionIdRead;
                            string textOfUser = (string)textOfUserRead;
                            answer = new Answer(id, userId, new Question(QuestionId), textOfUser, null, null, null);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT ThemeId,Text FROM questions WHERE id = {answer.Question.Id};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            answer.Question.Theme = new Theme(reader.GetInt32(0));
                            answer.Question.Text = reader.GetTextReader(1).ReadToEnd();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT Text FROM themes WHERE id = {answer.Question.Theme.Id};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            answer.Question.Theme.Name = reader.GetTextReader(0).ReadToEnd();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            return answer;
        }

        /// <summary>
        /// Сичтывает информацию об ответе пользователя по идентификатору
        /// </summary>
        /// <param name="answerId">Индентификатор ответа для поиска</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Объект ответа или null, если ответа с таким идентификатором не существует</returns>
        public async Task<Answer?> GetAnswer(long answerId, MySqlConnection? connectionInput = null)
        {
            Answer? answer = null;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT * FROM answers WHERE id={answerId};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                object rateRead = reader.GetValue(4);
                                object commentRead = reader.GetValue(5);
                                object teacherRead = reader.GetValue(6);
                                int? rate = rateRead == DBNull.Value ? null : (int)rateRead;
                                string? comment = commentRead == DBNull.Value ? null : (string)commentRead;
                                long? teacherId = teacherRead == DBNull.Value ? null : (long)teacherRead;
                                answer = new Answer(reader.GetInt64(0),
                                        reader.GetInt64(1),
                                        new Question(reader.GetInt32(2)),
                                        reader.GetTextReader(3).ReadToEnd(),
                                        rate, comment, teacherId);
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT ThemeId,Text FROM questions WHERE id = {answer.Question.Id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                answer.Question.Theme = new Theme(reader.GetInt32(0));
                                answer.Question.Text = reader.GetTextReader(1).ReadToEnd();
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT Text FROM themes WHERE id = {answer.Question.Theme.Id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                answer.Question.Theme.Name = reader.GetTextReader(0).ReadToEnd();
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM answers WHERE id={answerId};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            object rateRead = reader.GetValue(4);
                            object commentRead = reader.GetValue(5);
                            object teacherRead = reader.GetValue(6);
                            int? rate = rateRead == DBNull.Value ? null : (int)rateRead;
                            string? comment = commentRead == DBNull.Value ? null : (string)commentRead;
                            long? teacherId = teacherRead == DBNull.Value ? null : (long)teacherRead;
                            answer = new Answer(reader.GetInt64(0),
                                    reader.GetInt64(1),
                                    new Question(reader.GetInt32(2)),
                                    reader.GetTextReader(3).ReadToEnd(),
                                    rate, comment, teacherId);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT ThemeId,Text FROM questions WHERE id = {answer.Question.Id};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            answer.Question.Theme = new Theme(reader.GetInt32(0));
                            answer.Question.Text = reader.GetTextReader(1).ReadToEnd();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT Text FROM themes WHERE id = {answer.Question.Theme.Id};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            answer.Question.Theme.Name = reader.GetTextReader(0).ReadToEnd();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            return answer;
        }

        /// <summary>
        /// Находит рейтинг пользователя, основвываясь на его ответах
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Рейтинг пользователя</returns>
        public async Task<decimal> GetUserRating(long userId, MySqlConnection? connectionInput = null)
        {
            object? result;
            decimal rating = 0;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT AVG(Rate) AS RateAVG FROM answers WHERE UserId={userId}";
                        result = await command.ExecuteScalarAsync();
                        rating = (result == null || result == DBNull.Value) ? 0 : (decimal)result;
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT AVG(Rate) AS RateAVG FROM answers WHERE UserId={userId}";
                    result = await command.ExecuteScalarAsync();
                    rating = (result == null || result == DBNull.Value) ? 0 : (decimal)result;
                }
            }
            return rating;
        }

        /// <summary>
        /// Даёт всем пользователям возможность ответить сегодня
        /// </summary>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task SetAllUsersReady(MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE users SET QuestionReady = 1;";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"UPDATE users SET QuestionReady = 1;";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Читает из базы все темы
        /// </summary>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Список тем из базы</returns>
        public async Task<List<Theme>> GetAllThemes(MySqlConnection? connectionInput = null)
        {
            List<Theme> themes = new List<Theme>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection())
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM themes;";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    themes.Add(new Theme(reader.GetInt32(0), reader.GetTextReader(1).ReadToEnd()));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM themes;";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                themes.Add(new Theme(reader.GetInt32(0), reader.GetTextReader(1).ReadToEnd()));
                            }
                        }
                    }
                }
            }
            return themes;
        }

        /// <summary>
        /// Читает все вопросы определённой темы
        /// </summary>
        /// <param name="themeId">Идентификатор темы</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Список вопросов из темы</returns>
        public async Task<List<Question>> GetQuesionsFromTheme(int themeId, MySqlConnection? connectionInput = null)
        {
            List<Question> questions = new List<Question>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection())
                {
                    await connection.OpenAsync();
                    Theme? theme = null;
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT Text FROM themes WHERE Id = {themeId};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                theme = new Theme(themeId, reader.GetTextReader(0).ReadToEnd());
                            }
                            else
                            {
                                return questions;
                            }
                        }
                    }
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Id,QuestionType,Text FROM questions;";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    questions.Add(new Question(reader.GetInt32(0), theme, (QuestionType)reader.GetInt32(2), reader.GetTextReader(3).ReadToEnd()));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Theme? theme = null;
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT Text FROM themes WHERE Id = {themeId};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            theme = new Theme(themeId, reader.GetTextReader(0).ReadToEnd());
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = "SELECT Id,Type,Text FROM questions;";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                questions.Add(new Question(reader.GetInt32(0), theme, (QuestionType)reader.GetInt32(1), reader.GetTextReader(2).ReadToEnd()));
                            }
                        }
                    }
                }
            }
            return questions;
        }

        /// <summary>
        /// Получает любимые темы пользователя
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Список любимых тем пользователя</returns>
        public async Task<List<Theme>> GetFavoriteThemesOfUser(long userId, MySqlConnection? connectionInput = null)
        {
            List<Theme> themes = new List<Theme>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection())
                {
                    await connection.OpenAsync();
                    await SetUTF8Async(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT DISTINCT(ThemeId) FROM users_themes WHERE UserId = {userId};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    themes.Add(new Theme(reader.GetInt32(0)));
                                }
                            }
                        }
                    }
                    foreach (Theme theme in themes)
                    {
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = $"SELECT Text FROM themes WHERE Id = {theme.Id};";
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows)
                                {
                                    await reader.ReadAsync();
                                    theme.Name = reader.GetTextReader(0).ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                await SetUTF8Async(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT ThemeId FROM users_themes WHERE UserId = {userId};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                themes.Add(new Theme(reader.GetInt32(0)));
                            }
                        }
                    }
                }
                foreach (Theme theme in themes)
                {
                    using (MySqlCommand command = connectionInput.CreateCommand())
                    {
                        command.CommandText = $"SELECT Text FROM themes WHERE Id = {theme.Id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                theme.Name = reader.GetTextReader(0).ReadToEnd();
                            }
                        }
                    }
                }
            }
            return themes;
        }

        /// <summary>
        /// Считывает количество любимых тем пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Количество любимых тем пользователя</returns>
        public async Task<long> GetCountOfFavoriteThemes(long userId, MySqlConnection? connectionInput = null)
        {
            object? result;
            long amount;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(DISTINCT(ThemeId)) FROM users_themes WHERE UserId = {userId};";
                        result = await command.ExecuteScalarAsync();
                        amount = (result == null || result == DBNull.Value) ? 0 : (long)result;
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(DISTINCT(ThemeId)) FROM users_themes WHERE UserId = {userId};";
                    result = await command.ExecuteScalarAsync();
                    amount = (result == null || result == DBNull.Value) ? 0 : (long)result;
                }
            }
            return amount;
        }

        /// <summary>
        /// Удаление любимой темы у пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="themeId">Идентификатор темы</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task RemoveFavoriteTheme(long userId, int themeId, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"DELETE FROM users_themes WHERE UserId = {userId} AND ThemeId = {themeId};";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"DELETE FROM users_themes WHERE UserId = {userId} AND ThemeId = {themeId};";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Добавление новой любимой темы пользователю
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="themeId">Идентификатор темы</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task AddFavoriteTheme(long userId, int themeId, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO users_themes(UserId,ThemeId) VALUES({userId},{themeId});";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO users_themes(UserId,ThemeId) VALUES({userId},{themeId});";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Читает из базы тему по её идентификатору
        /// </summary>
        /// <param name="themeId">Идентификатор темы</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Тема или null, если найти не удалось</returns>
        public async Task<Theme?> GetTheme(int themeId, MySqlConnection? connectionInput = null)
        {
            Theme? theme;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT Id,Text FROM themes WHERE Id = {themeId};";
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                theme = new Theme(reader.GetInt32(0), reader.GetTextReader(1).ReadToEnd());
                            }
                            else
                            {
                                theme = null;
                            }
                        }
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT Id,Text FROM themes WHERE Id = {themeId};";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            theme = new Theme(reader.GetInt32(0), reader.GetTextReader(1).ReadToEnd());
                        }
                        else
                        {
                            theme = null;
                        }
                    }
                }
            }
            return theme;
        }

        /// <summary>
        /// Отправляет жалобу
        /// </summary>
        /// <param name="answerId">Идентификатор ответа</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        public async Task AddReport(long userId, long answerId, MySqlConnection? connectionInput = null)
        {
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO reports(UserId, AnswerId) VALUES({userId}, {answerId});";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO reports(UserId, AnswerId) VALUES({userId}, {answerId});";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Получет количетсво жалоб от указанного пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="connectionInput">Объект подключения, если выполняется извне</param>
        /// <returns>Кол-во жалоб от пользователя</returns>
        public async Task<long> GetCountOfUserReports(long userId, MySqlConnection? connectionInput = null)
        {
            object? result;
            long amount;
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(DISTINCT(AnswerId)) FROM reports WHERE UserId={userId};";
                        result = await command.ExecuteScalarAsync();
                        amount = (result == null || result == DBNull.Value) ? 0 : (long)result;
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(DISTINCT(AnswerId)) FROM reports WHERE UserId={userId};";
                    result = await command.ExecuteScalarAsync();
                    amount = (result == null || result == DBNull.Value) ? 0 : (long)result;
                }
            }
            return amount;
        }

        public async Task<List<Question>> GetAllQuestions(MySqlConnection? connectionInput = null)
        {
            List<Question> questions = new List<Question>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    List<Theme> themes = await GetAllThemes(connection);
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Id,ThemeId,Type,Text FROM questions;";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    Theme? themeFound = themes.FirstOrDefault(x => x.Id == reader.GetInt32(1));
                                    if (themeFound != null)
                                    {
                                        questions.Add(new Question(reader.GetInt32(0),
                                                    themeFound,
                                                    (QuestionType)reader.GetInt32(2),
                                                    reader.GetTextReader(3).ReadToEnd()));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                List<Theme> themes = await GetAllThemes(connectionInput);
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = "SELECT Id,ThemeId,QuesionType,Text FROM questions;";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                Theme? themeFound = themes.FirstOrDefault(x => x.Id == reader.GetInt32(1));
                                if (themeFound != null)
                                {
                                    questions.Add(new Question(reader.GetInt32(0),
                                                themeFound,
                                                (QuestionType)reader.GetInt32(2),
                                                reader.GetTextReader(3).ReadToEnd()));
                                }
                            }
                        }
                    }
                }
            }
            return questions;
        }

        public async Task<List<int>> GetUserUsedQuestionIdsFromTheme(long userId, int themeId, MySqlConnection? connectionInput = null)
        {
            List<int> ids = new List<int>();
            if (connectionInput == null)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT QuestionId FROM answers WHERE UserId = {userId} AND ThemeId = {themeId}";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    ids.Add(reader.GetInt32(0));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                using (MySqlCommand command = connectionInput.CreateCommand())
                {
                    command.CommandText = $"SELECT QuestionId FROM answers WHERE UserId = {userId};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                ids.Add(reader.GetInt32(0));
                            }
                        }
                    }
                }
            }
            return ids;
        }
    }
}
