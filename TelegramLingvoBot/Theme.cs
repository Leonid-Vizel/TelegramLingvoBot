using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLingvoBot
{
    internal class Theme
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Theme(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public Theme(int id)
        {
            Id = id;
        }
    }
}
