using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLingvoBot
{
    internal class FileTXTInteractions
    {
        static string path = @".\..\..\..\data\";
        public static string ReadTXT(string nameOfFile)
        {
            return File.ReadAllText(path + nameOfFile + ".txt");
        }
    }
}