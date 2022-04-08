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
        /// <param name="user">Объект пользователя, которого хотите добавить в базу</param>
        public async Task AddUser(User user)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                await SetUTF8Async(connection);
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO users(id, QuestionAmount, DialogPosition, QueestionReady) VALUES({ user.Id}, {user.QuestionAmount}, {(int)user.Position}, {Convert.ToInt32(user.QuestionReady)});";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Читает из базы всех пользователей
        /// </summary>
        /// <returns>Список пользователей, сохранённых в базе</returns>
        public async Task<List<User>> GetAllUsers()
        {
            List<User> users = new List<User>();
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
        /// <returns>Список учителей, сохранённых в базе</returns>
        public async Task<List<Teacher>> GetAllTeachers()
        {
            List<Teacher> users = new List<Teacher>();
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
                                users.Add(new Teacher(
                                        reader.GetInt64(0),
                                        reader.GetDecimal(1),
                                        (DialogPosition)reader.GetInt32(2)));
                            }
                        }
                    }
                }
            }
            return users;
        }

        /// <summary>
        /// Обновляет запись о пользователе в базе данных
        /// </summary>
        /// <param name="user">Данные для обновления</param>
        public async Task UpdateUser(User user)
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

        /// <summary>
        /// Обновляет запись об учителе в базе данных
        /// </summary>
        /// <param name="user">Данные для обновления</param>
        public async Task UpdateTeacher(Teacher teacher)
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

        /// <summary>
        /// Считывает из базы сначала количетство вопросов впринципе, потом количетсво вопросов, которое пользователь уже решил
        /// </summary>
        /// <param name="userId">Идентиикатор пользователя для поиска в БД</param>
        /// <returns>Количество доступных пользователю вопросов из БД</returns>
        public async Task<long> GetUserAvailibleQuestionsAmount(long userId)
        {
            long total = 0;
            long used = 0;
            object? result;
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
            return total - used;
        }

        /// <summary>
        /// Считывает количество ответов, которое было проверено учителем
        /// </summary>
        /// <param name="teacherId">Идентификатор учителя для посика в БД</param>
        /// <returns>Количество ответов, которое было проверено учителем </returns>
        public async Task<long> GetCountOfVerifiedAnswersOfTeacher(long teacherId)
        {
            long countOfVerifiedAnswers = 0;
            object? result;
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
            return countOfVerifiedAnswers;
        }

        /// <summary>
        /// Считывает все ответы, пользователя.
        /// </summary>
        /// <param name="userId">Индентификатор пользователя для поиска в БД</param>
        /// <returns>Список всех ответов пользователя</returns>
        public async Task<List<Answer>> GetAnswersOfUser(long userId)
        {
            List<Answer> answers = new List<Answer>();
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
                        command.CommandText = $"SELECT Text FROM questions WHERE Id = {question.Theme.Id};";
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
        public async Task AddAnswer(Answer answer)
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

        /// <summary>
        /// Обновляет информацию об ответе пользователя
        /// </summary>
        /// <param name="work">Объект ответа пользователя для обновления данных</param>
        public async Task UpdateAnswer(Answer work)
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

        /// <summary>
        /// Получет первый попавшийся непроверенный ответ
        /// </summary>
        /// <returns>Первый попавшийся непроверенный ответ или null, если его нет</returns>
        public async Task<Answer?> GetFirstAnswer()
        {
            Answer? answer = null;
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                await SetUTF8Async(connection);
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
                            answer = new Answer(id, userId, new Question(QuestionId), textOfUser, null, null, null);
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
            return answer;
        }

        /// <summary>
        /// Сичтывает информацию об ответе пользователя по идентификатору
        /// </summary>
        /// <param name="answerId">Индентификатор ответа для поиска</param>
        /// <returns>Объект ответа или null, если ответа с таким идентификатором не существует</returns>
        public async Task<Answer?> GetAnswer(long answerId)
        {
            Answer? answer = null;
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
            return answer;
        }

        /// <summary>
        /// Находит рейтинг пользователя, основвываясь на его ответах
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns>Рейтинг пользователя</returns>
        public async Task<decimal> GetUserRating(long userId)
        {
            object? result;
            decimal rating = 0;
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
            return rating;
        }

        /// <summary>
        /// Находит все вопросы из базы, которые имеют соответствующий тип
        /// </summary>
        /// <param name="type">Тип вопросов который ищем</param>
        /// <returns>Список вопросов соответствующего типа</returns>
        public async Task<List<Question>> GetQuestionsByType(QuestionType type)
        {
            List<Question> questions = new List<Question>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                await SetUTF8Async(connection);
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM questions WHERE Type = {(int)type};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                questions.Add(new Question(reader.GetInt32(0), new Theme(reader.GetInt32(1)), (QuestionType)reader.GetInt32(3), reader.GetTextReader(2).ReadToEnd()));
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
                                await reader.ReadAsync();
                                question.Theme.Name = reader.GetTextReader(0).ReadToEnd();
                            }
                        }
                    }
                }

                return questions;
            }
        }

        /// <summary>
        /// Находит идентификаторы всех вопросов
        /// </summary>
        /// <param name="userId">Идентификатор пользователя для поиска в БД</param>
        /// <param name="type">Тип вопроса для поиска в БД</param>
        /// <returns></returns>
        public async Task<List<int>> GetUserUsedQuestionIdsWithType(long userId, QuestionType type)
        {
            List<int> returnList = new List<int>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                await SetUTF8Async(connection);
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT DISTINCT QuestionId FROM answers WHERE UserId = {userId};";
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                returnList.Add(reader.GetInt32(0));
                            }
                        }
                    }
                }
                List<int> correctIds = new List<int>();
                foreach (int id in returnList)
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT Type FROM questions WHERE Id = {id};";
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    if ((QuestionType)reader.GetInt32(0) == type)
                                    {
                                        correctIds.Add(id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return returnList;
        }

        /// <summary>
        /// Даёт всем пользователям возможность ответить сегодня
        /// </summary>
        public async Task SetAllUsersReady()
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"UPDATE users SET QuestionReady = 1;";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
