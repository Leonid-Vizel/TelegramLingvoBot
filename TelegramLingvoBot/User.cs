namespace TelegramLingvoBot
{
    internal class User
    {
        /// <summary>
        /// Идентификатор пользователя 
        /// </summary>
        public long Id { get; private set; }
        /// <summary>
        /// Количетсво оплаченных пользователем вопросов (При создании акканута равно 1)
        /// </summary>
        public int QuestionAmount { get; private set; }
        /// <summary>
        /// Позиция диалога
        /// </summary>
        public DialogPosition Position { get; private set; }
        /// <summary>
        /// Обозначает, может ли сегодня пользователь решить вопрос
        /// </summary>
        public bool QuestionReady { get; private set; }

        /// <summary>
        /// Основной конструктор класса
        /// </summary>
        /// <param name="id">Идентификатор пользователя (Чата с пользователем)</param>
        /// <param name="position">Позиция диалога. По умолчанию - гланвое меню пользователя</param>
        /// <param name="questionAmount">Количество оплаченных вопросов. По умолчанию - 1</param>
        /// <param name="questionReady">Можно ли пользователю сегодня ответить. По умолчанию - Может(true)</param>
        public User(long id, DialogPosition position = DialogPosition.MainMenu, int questionAmount = 1, bool questionReady = true)
        {
            Id = id;
            Position = position;
            QuestionAmount = questionAmount;
            QuestionReady = questionReady;
        }

        /// <summary>
        /// Уменьшает количетсво оплаченных вопросов пользователя на 1
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        public void DecrementQuestion(DataBaseInteractions dbInteract)
        {
            QuestionAmount--;
            dbInteract.UpdateUser(this);
        }

        /// <summary>
        /// Увеличивает количество оплаченных вопросов
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="amount">Количество, которое надо добавить</param>
        public void AddQuestions(DataBaseInteractions dbInteract, byte amount)
        {
            QuestionAmount += amount;
            dbInteract.UpdateUser(this);
        }

        /// <summary>
        /// Задаёт всем пользователям готовность к получению нового вопроса
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="users">Список пользователей, к которым надо применить</param>
        public static void ResetReadyAllUsers(DataBaseInteractions dbInteract, List<User> users)
        {
            users.ForEach(u => u.QuestionReady = true);
            dbInteract.SetAllUsersReady();
        }

        /// <summary>
        /// Задаёт значение готовности ежедневного вопроса для пользователя
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="ready">Новое значение</param>
        public void SetReady(DataBaseInteractions dbInteract, bool ready)
        {
            if (QuestionReady != ready)
            {
                QuestionReady = ready;
                dbInteract.UpdateUser(this);
            }
        }

        /// <summary>
        /// Меняет положение пользователя в диалоге
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="position">Новое положение пользователя</param>
        public void SetPosition(DataBaseInteractions dbInteract, DialogPosition position)
        {
            if (Position != position)
            {
                Position = position;
                dbInteract.UpdateUser(this);
            }
        }
    }

    internal class Teacher
    {
        /// <summary>
        /// Идентификатор учителя (Чат с ним)
        /// </summary>
        public long Id { get; private set; }
        /// <summary>
        /// Баланс учителя в рублях
        /// </summary>
        public decimal Balance { get; private set; }
        /// <summary>
        /// Позиция дислога с учителем
        /// </summary>
        public DialogPosition Position { get; private set; }
        /// <summary>
        /// Вопрос, который учитель проверяет в данный момент
        /// </summary>
        public Answer? CurrentAnswer { get; set; }

        /// <summary>
        /// Основной конструктор класса
        /// </summary>
        /// <param name="id">Идентификатор учителя (Чата с ним)</param>
        /// <param name="balance">Баланс в рублях</param>
        /// <param name="position">Позиция диалога</param>
        public Teacher(long id, decimal balance, DialogPosition position)
        {
            Id = id;
            Balance = balance;
            Position = position;
        }

        /// <summary>
        /// Добавляет определённое количетсво рублей на баланс учителя
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="money">Количетсво денег, которое платим</param>
        public void AddBalance(DataBaseInteractions dbInteract, decimal money)
        {
            if (money > 0)
            {
                Balance += money;
                dbInteract.UpdateTeacher(this);
            }
        }

        /// <summary>
        /// Изменяет текущую позицию диалога учителя
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="position">Новая позиция</param>
        public void SetPosition(DataBaseInteractions dbInteract, DialogPosition position)
        {
            if (Position != position)
            {
                Position = position;
                dbInteract.UpdateTeacher(this);
            }
        }
    }

    internal enum DialogPosition
    {
        /// <summary>
        /// Главное меню пользователя
        /// </summary>
        MainMenu,
        /// <summary>
        /// Выбор работы пользователя
        /// </summary>
        ChooseWorkId,
        /// <summary>
        /// Работа показана. Выбор пути назад
        /// </summary>
        WorkShown,
        /// <summary>
        /// Выбор количетсва вопросов для покупки
        /// </summary>
        ShopAmount,
        /// <summary>
        /// Главное меню учителя
        /// </summary>
        TeacherMainMenu,
        /// <summary>
        /// Ввод учителем комментария к работе
        /// </summary>
        TeacherWorkCheckComment,
        /// <summary>
        /// Ввод учителем оценки за работу
        /// </summary>
        TeacherWorkCheckRate,
        /// <summary>
        /// Выбор типа работы, перед отправкой
        /// </summary>
        AnswerTypeSelect,
        /// <summary>
        /// Ожидание ответа пользователя на вопрос
        /// </summary>
        WaitingForResponce
    }
}
