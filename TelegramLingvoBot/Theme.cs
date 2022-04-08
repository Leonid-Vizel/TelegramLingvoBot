namespace TelegramLingvoBot
{
    /// <summary>
    /// Тема вопроса
    /// </summary>
    internal class Theme
    {
        /// <summary>
        /// Уникальный идентификатор темы
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Название темы
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Основной конструктор
        /// </summary>
        /// <param name="id">Идентификатор темы</param>
        /// <param name="name">Название темы</param>
        public Theme(int id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Запасной конструктор для неполной инициализации темы
        /// </summary>
        /// <param name="id">Идентификатор темы</param>
        public Theme(int id)
        {
            Id = id;
            Name = string.Empty;
        }
    }
}
