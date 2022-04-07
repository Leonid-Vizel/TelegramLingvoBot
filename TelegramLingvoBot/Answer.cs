using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLingvoBot
{
    internal class Answer
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public Question Question { get; set; }
        public string Text { get; set; }
        public int? Rate { get; set; }
        public string? Comment { get; set; }
        public long? TeacherId { get; set; }

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
