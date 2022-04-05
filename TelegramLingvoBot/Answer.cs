using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLingvoBot
{
    internal class Answer
    {
        public long Id { get; private set; }
        public long UserId { get; private set; }
        public Question Question { get; private set; }
        public string Text { get; private set; }
        public int? Rate { get; private set; }
        public string? Comment { get; private set; }
        public long? TeacherId { get; private set; }

        public Answer(long id, long userId, Question question, string text, int? rate, string? comment, long? teacherId)
        {
            Id = id;
            UserId = userId;
            Question = question;
            Text = text;
            Rate = rate;
            Comment = comment;
            TeacherId = teacherId;
        }
    }
}
