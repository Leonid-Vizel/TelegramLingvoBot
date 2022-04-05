using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLingvoBot
{
    internal class Question
    {
        public int Id { get; private set; }
        public Theme Theme { get; private set; }
        public QuestionType Type { get; private set; }
        public string Text { get; private set; }

        public Question(int id, Theme theme, QuestionType Type, string text)
        {
            Id = id;
            Theme = theme;
            Text = text;
        }
    }

    enum QuestionType
    {
        GeneralQuestion,
        Translation
    }
}
