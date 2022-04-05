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
                                        (DialogPosition)reader.GetInt32(1),
                                        reader.GetInt32(2), true));
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
                    command.CommandText = "";
                    result = command.ExecuteScalar();
                    total = result == DBNull.Value ? 0 : (long)result;
                }
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Count(*) FROM questions;";
                    result = command.ExecuteScalar();
                    used = result == DBNull.Value ? 0 : (long)result;
                }
            }
            return total - used;
        }

        public List<Answer> GetAnswersOfUser(long userId)
        {
            throw new NotImplementedException();
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
                    rating = returnObject == DBNull.Value ? 0 : (long)returnObject;
                }
            }
            return rating;
        }
    }
}
