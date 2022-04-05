using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramLingvoBot
{
    internal static class ButtonBank
    {
        internal static IReplyMarkup Register
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

        internal static IReplyMarkup UserMainMenu
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

        internal static IReplyMarkup TeacherMainMenu
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

        internal static IReplyMarkup JustBack
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
    }
}
