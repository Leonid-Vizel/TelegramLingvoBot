using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramLingvoBot
{
    internal static class ButtonBank
    {
        /// <summary>
        /// Кнопка регистрации пользоваетля
        /// </summary>
        internal static IReplyMarkup RegisterButton
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Регистрация")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Главное меню пользователя
        /// </summary>
        internal static IReplyMarkup UserMainMenuButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Магазин"),
                            new KeyboardButton("Работы"),
                            new KeyboardButton("Я готов"),
                            new KeyboardButton("Профиль")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Меню профиля
        /// </summary>
        internal static IReplyMarkup UserProfileMenuButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Меню любимых тем"),
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Меню любимых тем
        /// </summary>
        internal static IReplyMarkup UserThemesMenuButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Добавить"),
                            new KeyboardButton("Убрать"),
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Меню любимых тем без кнопки убрать
        /// </summary>
        internal static IReplyMarkup UserThemesMenuButtonsWithoutRemove
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Добавить"),
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Меню любимых тем без кнопки добавить
        /// </summary>
        internal static IReplyMarkup UserThemesMenuButtonsWithoutAdd
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Убрать"),
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Кнопки Да/Нет
        /// </summary>
        internal static IReplyMarkup YesNoButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Да"),
                            new KeyboardButton("Нет")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Кнопка Назад
        /// </summary>
        internal static IReplyMarkup JustBackButton
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Кнопки с количествами вопросов для магазина
        /// </summary>
        internal static IReplyMarkup ShopButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("10"),
                            new KeyboardButton("50"),
                            new KeyboardButton("100"),
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Кнопки после показа материалов работы
        /// </summary>
        internal static IReplyMarkup WorkShownButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Назад к моим работам"),
                            new KeyboardButton("Назад в главное меню"),
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Выбор типа вопроса 
        /// </summary>
        internal static IReplyMarkup AnswerTypeButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Общий вопрос"),
                            new KeyboardButton("Перевод текста"),
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }

        /// <summary>
        /// Выбор типа вопроса 
        /// </summary>
        internal static IReplyMarkup ChooseWorkButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Отправить жалобу"),
                            new KeyboardButton("Назад")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Кнопки главного меню учителя без кнопки вывода средств
        /// </summary>
        internal static IReplyMarkup TeacherMainMenuButtonsWithoutWithdrawalOfFunds
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Профиль"),
                            new KeyboardButton("Проверить"),
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Кнопки главного меню учителя с кнопкой вывода средств
        /// </summary>
        internal static IReplyMarkup TeacherMainMenuButtonsWithWithdrawalOfFunds
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Профиль"),
                            new KeyboardButton("Проверить"),
                            new KeyboardButton("Вывод средств"),
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Кнопки для оценки работы пользователя (1-10)
        /// </summary>
        internal static IReplyMarkup RateForAnswerButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("1"),
                            new KeyboardButton("2"),
                            new KeyboardButton("3"),
                            new KeyboardButton("4"),
                            new KeyboardButton("5"),
                            new KeyboardButton("6"),
                            new KeyboardButton("7"),
                            new KeyboardButton("8"),
                            new KeyboardButton("9"),
                            new KeyboardButton("10"),
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
        /// <summary>
        /// Убирает все кнопки
        /// </summary>
        internal static IReplyMarkup EmptyButtons
            => new ReplyKeyboardRemove() { };
    }
}