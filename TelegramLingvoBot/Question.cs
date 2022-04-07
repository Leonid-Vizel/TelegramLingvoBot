using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLingvoBot
{
    internal class Question
    {
        public int Id { get; set; }
        public Theme Theme { get; set; }
        public QuestionType Type { get; set; }
        public string Text { get; set; }

        public Question(int id, Theme theme, QuestionType Type, string text)
        {
            Id = id;
            Theme = theme;
            Text = text;
        }

        public Question(int id)
        {
            Id = id;
        }
    }

    enum QuestionType
    {
        GeneralQuestion,
        Translation
    }
}
