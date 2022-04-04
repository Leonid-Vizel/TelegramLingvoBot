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
            using (MySqlConnection mysqlconnection = new MySqlConnection(ConnectionString))
            {

            }
        }

        public List<User> GetAllUsers()
        {
            throw new NotImplementedException();
        }

        public List<Teacher> GetAllTeachers()
        {
            throw new NotImplementedException();
        }

        public void UpdateUser(User user)
        {
            throw new NotImplementedException();
        }

        public void UpdateTeacher(Teacher teacher)
        {
            throw new NotImplementedException();
        }

        public int GetUserAvailibleQuestionsAmount(long userId)
        {
            throw new NotImplementedException();
        }

        public List<Work> GetWorksOfUser(long userId)
        {
            throw new NotImplementedException();
        }
    }
}
