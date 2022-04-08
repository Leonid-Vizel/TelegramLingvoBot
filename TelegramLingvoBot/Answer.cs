﻿namespace TelegramLingvoBot
{
    /// <summary>
    /// Класс пользовательского ответа на вопрос
    /// </summary>
    internal class Answer
    {
        /// <summary>
        /// Идентификатор ответа
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// Вопрос, на который дан ответ
        /// </summary>
        public Question Question { get; set; }
        /// <summary>
        /// Ответ пользователя
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Оценка работы в баллах или null если не проверено
        /// </summary>
        public int? Rate { get; set; }
        /// <summary>
        /// Комментарий от учителя или null если не проверено
        /// </summary>
        public string? Comment { get; set; }
        /// <summary>
        /// Идетификатор учителя, проверившего работу или null если не проверено
        /// </summary>
        public long? TeacherId { get; set; }

        /// <summary>
        /// Основной конструктор класса
        /// </summary>
        /// <param name="id">Идентификатор ответа</param>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="question">Вопрос, на который дан ответ</param>
        /// <param name="text">Ответ пользователя</param>
        /// <param name="rate">Оценка работы в баллах или null если не проверено</param>
        /// <param name="comment">Комментарий от учителя или null если не проверено</param>
        /// <param name="teacherId">Идетификатор учителя, проверившего работу или null если не проверено</param>
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
