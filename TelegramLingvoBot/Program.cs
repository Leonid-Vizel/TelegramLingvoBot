using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramLingvoBot;

#region BaseLoading
DataBaseInteractions dbInteract = new DataBaseInteractions("Server=wpl36.hosting.reg.ru;Database=u1615366_LingvoHack;User Id=u1615366_LingvoHack;Password=y21e&B4a;");
List<TelegramLingvoBot.User> Users = dbInteract.GetAllUsers();
List<TelegramLingvoBot.Teacher> Teachers = dbInteract.GetAllTeachers();
#endregion

#region Starting the bot
string token = string.Empty;
if (System.IO.File.Exists("BotCode.token"))
{
    try
    {
        token = System.IO.File.ReadAllText("BotCode.token");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Ошибка чтения файла токена: {exception.Message}");
        Console.ReadKey();
        return;
    }
}
else
{
    Console.WriteLine($"Создайте файл токена (BotCode.token)!");
    Console.ReadKey();
    return;
}
TelegramBotClient botClient = new TelegramBotClient(token);
using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { } // receive all update types
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();
#endregion

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text || update.Message.Text == null)
    {
        return;
    }
    long chatId = update.Message.Chat.Id;
    string? messageText = update.Message.Text;
    TelegramLingvoBot.User? user = Users.FirstOrDefault(x => x.Id == chatId);
    if (user == null)
    {
        if (update.Message.Text.Equals("Регистрация"))
        {
            user = new TelegramLingvoBot.User(chatId);
            Users.Add(user);
            dbInteract.AddUser(user);
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Спасибо за регистрацию! У Вас Есть 1 пробный вопрос.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Привет! Зарегайся пж)", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RegisterButton);
        }
        return;
    }

    await botClient.SendTextMessageAsync(chatId: chatId, text: $"{user.Position}", cancellationToken: cancellationToken);

    switch (user.Position)
    {
        case DialogPosition.MainMenu:
            switch (update.Message.Text)
            {
                case "Магазин":
                    await botClient.SendTextMessageAsync(chatId: chatId, text: $"Добро пожаловать в магазин! Доступно {dbInteract.GetUserAvailibleQuestionsAmount(chatId)} вопросов.\nВыберите кол - во вопросов которое хотите купить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons);
                    user.SetPosition(dbInteract, DialogPosition.ShopAmount);
                    break;
                case "Работы":
                    StringBuilder worksBuilder = new StringBuilder("Список ваших работ:\n");
                    List<Answer> answersOfUser = dbInteract.GetAnswersOfUser(chatId);
                    if (answersOfUser.Count > 0)
                    {
                        foreach (Answer answer in answersOfUser)
                        {
                            if (answer.Question.Type == QuestionType.GeneralQuestion)
                            {
                                string checkedString = answer.Rate == null ? "Не проверен" : "Проверен";
                                worksBuilder.AppendLine($"{answer.Id}) {answer.Question.Text} - {checkedString}");
                            }
                            else
                            {
                                worksBuilder.AppendLine($"{answer.Id}) Перевод текста");
                            }
                        }
                        await botClient.SendTextMessageAsync(chatId: chatId, text: worksBuilder.ToString(), cancellationToken: cancellationToken, replyMarkup: null);
                        await botClient.SendTextMessageAsync(chatId: chatId, text: "Введите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                        user.SetPosition(dbInteract, DialogPosition.ChooseWorkId);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: "У тебя пока нет написанных работ.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                        user.SetPosition(dbInteract, DialogPosition.MainMenu);
                    }
                    break;
                case "Профиль":
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine($"Ваш Id в системе: {user.Id}");
                    builder.AppendLine($"Ваш рейтинг: {dbInteract.GetUserRating(user.Id)}");
                    builder.AppendLine($"Количество оплаченных вопросов: {user.QuestionAmount}");
                    await botClient.SendTextMessageAsync(chatId: chatId, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                    user.SetPosition(dbInteract, DialogPosition.ProfileShown);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    break;
            }
            break;
        case DialogPosition.ChooseWorkId:
            switch (update.Message.Text)
            {
                case "Назад":
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    user.SetPosition(dbInteract, DialogPosition.MainMenu);
                    break;
                default:
                    if (long.TryParse(update.Message.Text, out long result))
                    {
                        Answer? answer = dbInteract.GetAnswer(result);
                        if (answer == null)
                        {
                            await botClient.SendTextMessageAsync(chatId: chatId, text: "Работа с таким Id не была найдена! 🤖🤖🤖\nВведите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId: chatId, text: $"Работа #{answer.Id}", cancellationToken: cancellationToken);
                            await botClient.SendTextMessageAsync(chatId: chatId, text: $"Тема: {answer.Question.Theme.Name}", cancellationToken: cancellationToken);
                            await botClient.SendTextMessageAsync(chatId: chatId, text: $"Вопрос: {answer.Question.Text}", cancellationToken: cancellationToken);
                            if (answer.Rate == null)
                            {
                                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Работа проверена: Нет", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Работа проверена: Да", cancellationToken: cancellationToken);
                            }
                            await botClient.SendTextMessageAsync(chatId: chatId, text: $"Текст работы:\n{answer.Text}", cancellationToken: cancellationToken);
                            if (answer.Rate != null)
                            {
                                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Оценка эксперта: {answer.Rate}/10", cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Комментарий эксперта:\n{answer.Comment}", cancellationToken: cancellationToken);
                            }
                            await botClient.SendTextMessageAsync(chatId: chatId, text: $"Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.WorkShownButtons);
                            user.SetPosition(dbInteract, DialogPosition.WorkShown);
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: "Кажется Вы ввели что-то неправильно 🤖🤖🤖\nВведите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                    }
                    break;
            }
            break;
        case DialogPosition.WorkShown:
            switch (update.Message.Text)
            {
                case "Назад к моим работам":
                    StringBuilder worksBuilder = new StringBuilder("Список ваших работ:\n");
                    List<Answer> answersOfUser = dbInteract.GetAnswersOfUser(chatId);
                    foreach (Answer work in answersOfUser)
                    {
                        if (work.Question.Type == QuestionType.GeneralQuestion)
                        {
                            string checkedString = work.Rate == null ? "Не проверен" : "Проверен";
                            worksBuilder.AppendLine($"{work.Id}) {work.Question.Text} - {checkedString}");
                        }
                        else
                        {
                            worksBuilder.AppendLine($"{work.Id}) Перевод текста");
                        }
                    }
                    await botClient.SendTextMessageAsync(chatId: chatId, text: worksBuilder.ToString(), cancellationToken: cancellationToken, replyMarkup: null);
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Введите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: null);
                    user.SetPosition(dbInteract, DialogPosition.ChooseWorkId);
                    break;
                case "Назад в главное меню":
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    user.SetPosition(dbInteract, DialogPosition.MainMenu);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    break;
            }
            break;
        case DialogPosition.ShopAmount:
            switch (update.Message.Text)
            {
                case "10":
                    break;
                case "50":
                    break;
                case "100":
                    break;
                case "Назад":
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    user.SetPosition(dbInteract, DialogPosition.MainMenu);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите кол - во вопросов которое хотите купить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons);
                    break;
            }
            break;
        case DialogPosition.ShopConfirmation:
            break;
        case DialogPosition.ShopWaitingForPayment:
            break;
        case DialogPosition.ProfileShown:
            switch (update.Message.Text)
            {
                case "Назад":
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    user.SetPosition(dbInteract, DialogPosition.MainMenu);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                    break;
            }
            break;
        case DialogPosition.TeacherMainMenu:
            break;
        case DialogPosition.TeacherWorkCheckComment:
            break;
        case DialogPosition.TeacherWorkCheckRate:
            break;
        case DialogPosition.WaitingForResponce:
            break;

    }
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}