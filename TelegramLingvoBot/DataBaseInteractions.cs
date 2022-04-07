using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TelegramLingvoBot
{
    internal class DataBaseInteractions
    {
        public string ConnectionString { get; private set; }

        public DataBaseInteractions(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void AddUser(User user)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO users(id, QuestionAmount, DialogPosition) VALUES({ user.Id}, {user.QuestionAmount}, {(int)user.Position});";
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<User> GetAllUsers()
        {
            List<User> users = new List<User>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM users;";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
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

        public List<Teacher> GetAllTeachers()
        {
            List<Teacher> users = new List<Teacher>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM teachers";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
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

        public void UpdateUser(User user)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"UPDATE users SET QuestionAmount = {user.QuestionAmount}, DialogPosition = {(int)user.Position} WHERE id={user.Id}";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateTeacher(Teacher teacher)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"UPDATE teachers SET Balance = {teacher.Balance}, DialogPosition = {(int)teacher.Position} WHERE id={teacher.Id}";
                    command.ExecuteNonQuery();
                }
            }
        }

        public long GetUserAvailibleQuestionsAmount(long userId)
        {
            long total = 0;
            long used = 0;
            object result;
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Count(*) FROM questions;";
                    result = command.ExecuteScalar();
                    total = result == DBNull.Value ? 0 : (long)result;
                }
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM answers WHERE UserId = {userId};";
                    result = command.ExecuteScalar();
                    used = result == DBNull.Value ? 0 : (long)result;
                }
            }
            return total - used;
        }
        public long GetCountOfVerifiedAnswersOfTeacher(long teacherId)
        {
            long countOfVerifiedAnswers = 0;
            object result;
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using(MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM answers WHERE TeacherId={teacherId}";
                    result = command.ExecuteScalar();
                    countOfVerifiedAnswers = result == DBNull.Value ? 0 : (long)result;
                }
            }
            return countOfVerifiedAnswers;
        }
        public List<Answer> GetAnswersOfUser(long userId)
        {
            List<Answer> answers = new List<Answer>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM answers WHERE UserId = {userId};";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while(reader.Read())
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
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
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
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
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

        public void AddAnswer(Answer answer)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO answers (UserId, QuestionId, Text, Rate, Comment, TeacherId) VALUES ({answer.UserId}, {answer.Question.Id}, '{answer.Text}', {answer.Rate}, '{answer.Comment}', {answer.TeacherId});";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateAnswer(Answer work)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"UPDATE answers SET UserId={work.UserId}, QuestionId={work.Question.Id}, Text='{work.Text}', Rate={work.Rate}, Comment='{work.Comment}', TeacherId={work.TeacherId} WHERE id={work.UserId};";
                    command.ExecuteNonQuery();
                }
            }
        }
        public Answer? GetFirstAnswer()
        {
            Answer? answer = null;
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM answers WHERE Rate=Null";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            object idRead = reader.GetValue(0);
                            object userIdRead = reader.GetValue(1);
                            object QuestionIdRead = reader.GetValue(2);
                            object textOfUserRead = reader.GetValue(3);
                            long id = (long)idRead;
                            long userId = (long)userIdRead;
                            long QuestionId = (long)QuestionIdRead;
                            string textOfUser = (string)textOfUserRead;
                            answer = new Answer(id, userId, null, textOfUser, null, null, null);

                        }
                    }
                }
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT ThemeId,Text FROM questions WHERE id = {answer.Question.Id};";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
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
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
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
        public Answer? GetAnswer(long answerId)
        {
            Answer? answer = null;
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM answers WHERE id={answerId};";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
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
                                    rate,comment,teacherId);
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
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
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
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
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

        public decimal GetUserRating(long userId)
        {
            object returnObject;
            decimal rating = 0;
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT AVG(Rate) AS RateAVG FROM answers WHERE UserId={userId}";
                    returnObject = command.ExecuteScalar();
                    returnObject = command.ExecuteScalar();
                    rating = returnObject == DBNull.Value ? 0 : (decimal)returnObject;
                }
            }
            return rating;
        }
    }
}
