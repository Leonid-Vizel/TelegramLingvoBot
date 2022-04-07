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
                            new KeyboardButton("Профиль")
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

        internal static InlineKeyboardMarkup JustBackButtonInline
        {
            get
            {
                InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(
                    new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            new InlineKeyboardButton("Назад")
                        }
                    });
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
    }
}
