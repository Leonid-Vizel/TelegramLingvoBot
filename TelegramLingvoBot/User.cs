using MySql.Data.MySqlClient;

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
        public async Task DecrementQuestion(DataBaseInteractions dbInteract, MySqlConnection? connectionInput = null)
        {
            QuestionAmount--;
            await dbInteract.UpdateUserQuestionAmount(this, connectionInput);
        }

        /// <summary>
        /// Увеличивает количество оплаченных вопросов
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="amount">Количество, которое надо добавить</param>
        public async Task AddQuestions(DataBaseInteractions dbInteract, byte amount, MySqlConnection? connectionInput = null)
        {
            QuestionAmount += amount;
            await dbInteract.UpdateUserQuestionAmount(this, connectionInput);
        }

        /// <summary>
        /// Задаёт всем пользователям готовность к получению нового вопроса
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="users">Список пользователей, к которым надо применить</param>
        public static async Task ResetReadyAllUsers(DataBaseInteractions dbInteract, List<User> users, MySqlConnection? connectionInput = null)
        {
            users.ForEach(u => u.QuestionReady = true);
            await dbInteract.SetAllUsersReady(connectionInput);
        }

        /// <summary>
        /// Задаёт значение готовности ежедневного вопроса для пользователя
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="ready">Новое значение</param>
        public async Task SetReady(DataBaseInteractions dbInteract, bool ready, MySqlConnection? connectionInput = null)
        {
            if (QuestionReady != ready)
            {
                QuestionReady = ready;
                await dbInteract.UpdateUser(this, connectionInput);
            }
        }

        /// <summary>
        /// Меняет положение пользователя в диалоге
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="position">Новое положение пользователя</param>
        public async Task SetPosition(DataBaseInteractions dbInteract, DialogPosition position, MySqlConnection? connectionInput = null)
        {
            if (Position != position)
            {
                Position = position;
                await dbInteract.UpdateUser(this, connectionInput);
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
        public async Task AddBalance(DataBaseInteractions dbInteract, decimal money, MySqlConnection? connectionInput = null)
        {
            if (money > 0)
            {
                Balance += money;
                await dbInteract.UpdateTeacher(this, connectionInput);
            }
        }

        /// <summary>
        /// Изменяет текущую позицию диалога учителя
        /// </summary>
        /// <param name="dbInteract">Объект взаимодейтсвия с базой</param>
        /// <param name="position">Новая позиция</param>
        public async Task SetPosition(DataBaseInteractions dbInteract, DialogPosition position, MySqlConnection? connectionInput = null)
        {
            if (Position != position)
            {
                Position = position;
                await dbInteract.UpdateTeacher(this, connectionInput);
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
        /// Выбор типа работы, перед отправкой
        /// </summary>
        AnswerThemeSelect,
        /// <summary>
        /// Ожидание ответа пользователя на вопрос
        /// </summary>
        WaitingForResponce,
        /// <summary>
        /// Меню профиля пользователя
        /// </summary>
        UserProfileMenu,
        /// <summary>
        /// Меню любимых тем пользователя
        /// </summary>
        UserThemesMenu,
        /// <summary>
        /// Меню любимых тем пользователя
        /// </summary>
        UserThemesDecrease,
        /// <summary>
        /// Меню добавления темы в 
        /// </summary>
        UserThemesIncrease,
        /// <summary>
        /// Ввод жалобы пользователем
        /// </summary>
        UserWaitingForReportNumber,
        /// <summary>
        /// Помощь пользователю
        /// </summary>
        UserHelp,
        /// <summary>
        /// Главное меню учителя
        /// </summary>
        TeacherMainMenu,
        /// <summary>
        /// Ввод учителем комментария к работе
        /// </summary>
        TeacherCheckAnswerEquivalence,
        ///<summary>
        ///Меню проверки адекватности
        ///</summary>
        TeacherCheckAnswerAdequacy,
        ///<summary>
        ///Меню проверки грамматики
        ///</summary>
        TeacherCheckAnswerGrammar,
        ///<summary>
        ///Меню проверки орфографии
        ///</summary>
        TeacherCheckAnswerSpelling,
        ///<summary>
        ///Меню проверки стиля
        ///</summary>
        TeacherCheckAnswerStyle,
        ///<summary>
        ///Меню проверки эквивалентности
        ///</summary>
        TeacherWorkCheckComment,
        ///<summary>
        ///Меню помощи учителю
        ///</summary>
        TeacherHelp
    }
}
