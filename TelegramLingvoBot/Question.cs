namespace TelegramLingvoBot
{
    /// <summary>
    /// Вопрос к пользователю от системы
    /// </summary>
    internal class Question
    {
        /// <summary>
        /// Идентификатор вопроса
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// Тема, к которой принадлежит вопрос
        /// </summary>
        public Theme Theme { get; set; }
        /// <summary>
        /// Тип вопроса
        /// </summary>
        public QuestionType Type { get; private set; }
        /// <summary>
        /// Текст вопроса
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Основной конструктор
        /// </summary>
        /// <param name="id">Идентификатор вопросов</param>
        /// <param name="theme">Тема, к кторой принадлежит вопрос</param>
        /// <param name="Type">Тип вопроса</param>
        /// <param name="text">Текст вопроса</param>
        public Question(int id, Theme theme, QuestionType Type, string text)
        {
            Id = id;
            Theme = theme;
            Text = text;
        }

        /// <summary>
        /// Запасной конструктор для неполной инициализации темы
        /// </summary>
        /// <param name="id">Идентификатор вопроса</param>
        public Question(int id)
        {
            Id = id;
            Theme = null;
            Type = QuestionType.GeneralQuestion;
            Text = string.Empty;
        }
    }

    /// <summary>
    /// Тип вопроса
    /// </summary>
    enum QuestionType
    {
        /// <summary>
        /// Общий вопрос по теме
        /// </summary>
        GeneralQuestion,
        /// <summary>
        /// Задание по переводу
        /// </summary>
        Translation
    }
}
