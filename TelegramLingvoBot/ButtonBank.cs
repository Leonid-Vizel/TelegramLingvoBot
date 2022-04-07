using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramLingvoBot
{
    internal static class ButtonBank
    {
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

        internal static IReplyMarkup TeacherMainMenuButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>
                        {
                            new KeyboardButton("Проверить"),
                            new KeyboardButton("Профиль"),
                            new KeyboardButton("Вывод средств")
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }

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
                        }
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }

        internal static IReplyMarkup EmptyButtons
        {
            get
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>()
                    });
                keyboard.ResizeKeyboard = true;
                return keyboard;
            }
        }
    }
}
