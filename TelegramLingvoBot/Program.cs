﻿using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramLingvoBot;
using System.Timers;

#region BaseLoading
DataBaseInteractions dbInteract = new DataBaseInteractions("Server=wpl36.hosting.reg.ru;Database=u1615366_LingvoHack;User Id=u1615366_LingvoHack;Password=y21e&B4a;charset=utf8;");
List<AwaitingAsnwer> awaitingAsnwers = new List<AwaitingAsnwer>();
List<TelegramLingvoBot.User> Users = await dbInteract.GetAllUsers();
List<TelegramLingvoBot.Teacher> Teachers = await dbInteract.GetAllTeachers();
System.Timers.Timer mainTimer;
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
foreach (TelegramLingvoBot.User user in Users.Where(x => x.Position == DialogPosition.WaitingForResponce))
{
    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Приносим свои извинения.\nПроизошла критическая ошибка бота и Ваш вопрос был сброшен. Попробуйте снова. Ваши оплаченные вопросы не убавились.", cancellationToken: cts.Token, replyMarkup: ButtonBank.UserMainMenuButtons);
    await user.SetPosition(dbInteract, DialogPosition.MainMenu);
}


DateTime TimeToExecuteTask;
if (DateTime.Now.Hour < 10)
{
    TimeToExecuteTask = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0);
}
else
{
    TimeToExecuteTask = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, 10, 0, 0);
}
mainTimer = new System.Timers.Timer((TimeToExecuteTask - DateTime.Now).TotalMilliseconds);
mainTimer.Elapsed += ResetAllUsers;
mainTimer.Start();
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();
#endregion

async Task ProcessingUserMainMenuShop(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Добро пожаловать в магазин! Доступно {await dbInteract.GetUserAvailibleQuestionsAmount(user.Id)} вопросов.\nВыберите кол - во вопросов которое хотите купить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons);
    await user.SetPosition(dbInteract, DialogPosition.ShopAmount);
}

async Task ProcessAllUserWorks(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        StringBuilder worksBuilder = new StringBuilder("Список ваших работ:\n");
        List<Answer> answersOfUser = await dbInteract.GetAnswersOfUser(user.Id, connection);
        if (answersOfUser.Count > 0)
        {
            foreach (Answer answer in answersOfUser)
            {
                string checkedString = answer.Rate == null ? "Не проверен" : "Проверен";
                if (answer.Question.Type == QuestionType.GeneralQuestion)
                {
                    worksBuilder.AppendLine($"{answer.Id}) {answer.Question.Text} - {checkedString}");
                }
                else
                {
                    worksBuilder.AppendLine($"{answer.Id}) Перевод текста - {checkedString}");
                }
            }
            await botClient.SendTextMessageAsync(chatId: user.Id, text: worksBuilder.ToString(), cancellationToken: cancellationToken, replyMarkup: null);
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "Введите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
            await user.SetPosition(dbInteract, DialogPosition.ChooseWorkId, connection);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "У тебя пока нет написанных работ.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            await user.SetPosition(dbInteract, DialogPosition.MainMenu, connection);
        }
    }
}

async Task ProcessngUserMainMenuUserIsReady(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    if (user.QuestionReady)
    {
        if (user.QuestionAmount > 0)
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "Выберите тип вопроса:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.AnswerTypeButtons);
            await user.SetPosition(dbInteract, DialogPosition.AnswerTypeSelect);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "У вас на аккаунте не имеется оплаченных вопросов.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
    }
    else
    {
        if (user.QuestionAmount > 0)
        {
            if (DateTime.Now.Hour < 10)
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Вы уже использовали вопрос!\nСледующий вопрос придёт Вам {DateTime.Now.ToString("dd.MM.yyy")} в 10:00 по Московскому времени.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Вы уже использовали вопрос!\nСледующий вопрос придёт Вам {new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1).ToString("dd.MM.yyy")} в 10:00 по Московскому времени.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "У вас на аккаунте не имеется оплаченных вопросов.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
    }
}

async Task ProcessingUserMainMenuProfile(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    StringBuilder builder = new StringBuilder();
    builder.AppendLine($"Ваш Id в системе: {user.Id}");
    builder.AppendLine($"Ваш рейтинг: {await dbInteract.GetUserRating(user.Id)}");
    builder.AppendLine($"Количество оплаченных вопросов: {user.QuestionAmount}");
    await user.SetPosition(dbInteract, DialogPosition.UserProfileMenu);
    await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserProfileMenuButtons);
}

async Task ProcessGoBackToMainMenu(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
    await user.SetPosition(dbInteract, DialogPosition.MainMenu);
}

async Task ProcessingUserChooseWorkId(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (long.TryParse(update.Message.Text, out long result))
    {
        using (var connection = dbInteract.GetConnection())
        {
            await connection.OpenAsync();
            Answer? answer = await dbInteract.GetAnswer(result, connection);
            if (answer == null || answer.UserId != user.Id)
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Работа с таким Id не была найдена! 🤖🤖🤖\nВведите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
            }
            else
            {
                StringBuilder builder = new StringBuilder($"Работа #{answer.Id}\nТема: {answer.Question.Theme.Name}\nВопрос: {answer.Question.Text}\n");
                if (answer.Rate == null)
                {
                    builder.AppendLine("Работа проверена: Нет");
                }
                else
                {
                    builder.AppendLine("Работа проверена: Да");
                }
                builder.AppendLine($"Текст работы:\n{answer.Text}");
                if (answer.Rate != null)
                {
                    builder.AppendLine($"Оценка эксперта: {answer.Rate}/10\nКомментарий эксперта:\n{answer.Comment}");
                }
                await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
                await user.SetPosition(dbInteract, DialogPosition.WorkShown, connection);
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.WorkShownButtons);
            }
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Кажется Вы ввели что-то неправильно 🤖🤖🤖\nВведите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
    }
}

async Task ProcessingUserShopAmount(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    List<LabeledPrice> myList = new List<LabeledPrice>();
    switch (update.Message.Text)
    {
        case "10":
            myList.Add(new LabeledPrice("Цена", 50000));
            await botClient.SendInvoiceAsync(chatId: user.Id, title: "Покупка вопросов", description: "10 вопросов", payload: $"{user.Id}-10", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "50":
            myList.Add(new LabeledPrice("Цена", 230000));
            await botClient.SendInvoiceAsync(chatId: user.Id, title: "Покупка вопросов", description: "50 вопросов", payload: $"{user.Id}-50", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "100":
            myList.Add(new LabeledPrice("Цена", 450000));
            await botClient.SendInvoiceAsync(chatId: user.Id, title: "Покупка вопросов", description: "100 вопросов", payload: $"{user.Id}-100", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "Назад":
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            await user.SetPosition(dbInteract, DialogPosition.MainMenu);
            break;
        default:
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите кол - во вопросов которое хотите купить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons);
            break;
    }
}

async Task ProcessingUserAnswerTypeSelectCommonQuestions(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        List<Question> questions = await dbInteract.GetQuestionsByType(QuestionType.GeneralQuestion, connection);
        List<int> userQuestionIds = await dbInteract.GetUserUsedQuestionIdsWithType(user.Id, QuestionType.GeneralQuestion, connection);
        Question question;
        if (questions.Count <= userQuestionIds.Count)
        {
            Random random = new Random(Guid.NewGuid().GetHashCode());
            question = questions[random.Next(0, questions.Count)];
        }
        else
        {
            questions = questions.Where(x => !userQuestionIds.Contains(x.Id)).ToList();
            question = questions[0];
        }
        await user.SetPosition(dbInteract, DialogPosition.WaitingForResponce, connection);
        awaitingAsnwers.Add(new AwaitingAsnwer(botClient, cancellationToken, awaitingAsnwers, dbInteract, user, question));
        await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Ваш сегодняшний вопрос:\n{question.Text}\nНа ответ даётся 5 минут.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    }
}

async Task ProcessingUserAnswerTypeSelectTranslateText(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        List<Question> translates = await dbInteract.GetQuestionsByType(QuestionType.GeneralQuestion, connection);
        List<int> userTranslateIds = await dbInteract.GetUserUsedQuestionIdsWithType(user.Id, QuestionType.GeneralQuestion, connection);
        Question translate;
        if (translates.Count <= userTranslateIds.Count)
        {
            Random random = new Random(Guid.NewGuid().GetHashCode());
            translate = translates[random.Next(0, translates.Count)];
        }
        else
        {
            translates = translates.Where(x => !userTranslateIds.Contains(x.Id)).ToList();
            translate = translates[0];
        }
        await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Ваше сегодняшнее задание:\n{translate.Text}\nНа перевод даётся 8 минут.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        await user.SetPosition(dbInteract, DialogPosition.WaitingForResponce, connection);
        awaitingAsnwers.Add(new AwaitingAsnwer(botClient, cancellationToken, awaitingAsnwers, dbInteract, user, translate));
    }
}

async Task ProcessingUserWaitingForResponce(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    AwaitingAsnwer? awaitingAsnwer = awaitingAsnwers.FirstOrDefault(x => x.User.Id == user.Id);
    if (awaitingAsnwer == null)
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Приносим свои извинения.\nПроизошла критическая ошибка бота и Ваш вопрос был сброшен. Попробуйте снова. Ваши оплаченные вопросы не убавились.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        await user.SetPosition(dbInteract, DialogPosition.MainMenu);
    }
    else
    {
        using (var connection = dbInteract.GetConnection())
        {
            awaitingAsnwer.StopDestroy();
            await user.SetPosition(dbInteract, DialogPosition.MainMenu, connection);
            await user.SetReady(dbInteract, false, connection);
            await user.DecrementQuestion(dbInteract, connection);
            await dbInteract.AddAnswer(awaitingAsnwer.ToAnswer(update.Message.Text), connection);
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "Отлично!\nВаша работы была отправлена на проверку. Как только работа будет проверена, мы оповестим Вас.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
    }
}

async Task ProcessingUserThemesMenu(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    List<Theme> userThemes;
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        userThemes = await dbInteract.GetFavoriteThemesOfUser(user.Id, connection);
        await user.SetPosition(dbInteract, DialogPosition.UserThemesMenu, connection);
    }
    if (userThemes.Count == 0)
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "У Вас пока не выбраны любимые темы.\nХотите добавить новые темы?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserThemesMenuButtonsWithoutRemove);
    }
    else
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Ваши любимые темы:");
        foreach (Theme theme in userThemes)
        {
            builder.AppendLine(theme.Name);
        }
        builder.Append("Хотите добавить новые темы или убрать текущие?");
        await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserThemesMenuButtons);
    }
}

async Task ProcessingIncreaseUserThemesMenu(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    List<Theme> allThemes;
    List<Theme> userThemesToExcept;
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        allThemes = await dbInteract.GetAllThemes(connection);
        userThemesToExcept = await dbInteract.GetFavoriteThemesOfUser(user.Id, connection);

        StringBuilder builder = new StringBuilder();
        foreach (Theme theme in allThemes.Where(x => !userThemesToExcept.Any(y => y.Id == x.Id)))
        {
            builder.AppendLine($"{theme.Id}) {theme.Name}");
        }
        builder.AppendLine("Выберите ID темы, которую хотите доавбить:");
        await user.SetPosition(dbInteract, DialogPosition.UserThemesIncrease, connection);
        await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
    }
}

async Task ProcessingDecreaseUserThemesMenu(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        List<Theme> userThemes = await dbInteract.GetFavoriteThemesOfUser(user.Id, connection);
        StringBuilder builder = new StringBuilder();
        foreach (Theme theme in userThemes)
        {
            builder.AppendLine($"{theme.Id}) {theme.Name}");
        }
        builder.AppendLine("Выберите ID темы, которую хотите убрать:");
        await user.SetPosition(dbInteract, DialogPosition.UserThemesDecrease, connection);
        await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
    }
}

async Task ProcessingUserThemesMenuIncrease(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int themeId))
    {
        using (var connection = dbInteract.GetConnection())
        {
            await connection.OpenAsync();
            Theme? theme = await dbInteract.GetTheme(themeId, connection);
            if (theme != null)
            {
                List<Theme> userThemes = await dbInteract.GetFavoriteThemesOfUser(user.Id, connection);
                if (!userThemes.Any(x => x.Id == theme.Id))
                {
                    await dbInteract.AddFavoriteTheme(user.Id, theme.Id, connection);
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Тема успешно добавлена! Хотите добавить ещё одну?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, эта темя уже входит в список любимых тем 🤖🤖🤖.Выберите ID темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, тема с таким идентификатором не найдена 🤖🤖🤖\nВыберите ID темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
            }
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите ID темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
    }
}

async Task ProcessingUserThemesMenuDecrease(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int themeId))
    {
        using (var connection = dbInteract.GetConnection())
        {
            await connection.OpenAsync();
            Theme? theme = await dbInteract.GetTheme(themeId, connection);
            if (theme != null)
            {
                List<Theme> userThemes = await dbInteract.GetFavoriteThemesOfUser(user.Id, connection);
                if (userThemes.Any(x => x.Id == theme.Id))
                {
                    await dbInteract.RemoveFavoriteTheme(user.Id, theme.Id, connection);
                    if (userThemes.Count() - 1 > 0)
                    {
                        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Тема успешно убрана! Хотите убрать другую?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                    }
                    else
                    {
                        await ProcessingUserThemesMenu(user, botClient, cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, эта темя уже входит в список любимых тем 🤖🤖🤖.Выберите ID темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, тема с таким идентификатором не найдена 🤖🤖🤖\nВыберите ID темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
            }
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите ID темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
    }
}

async Task ProcessingTeacherMainMenuProfile(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, CancellationToken cancellationToken, IReplyMarkup teacherMainMenuButtons)
{
    long countOfVerifiedAnswersOfTeacher = await dbInteract.GetCountOfVerifiedAnswersOfTeacher(teacher.Id);
    StringBuilder TeacherProfileText = new StringBuilder();
    TeacherProfileText.AppendLine($"Ваш Id в системе: {teacher.Id}");
    TeacherProfileText.AppendLine($"Количество проверенных работ: {countOfVerifiedAnswersOfTeacher}");
    TeacherProfileText.AppendLine($"Баланс: {teacher.Balance}");
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: TeacherProfileText.ToString(), cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
}

async Task ProcessingTeacherMainMenuCheckAnswer(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, CancellationToken cancellationToken, IReplyMarkup teacherMainMenuButtons)
{
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        Answer? answer = await dbInteract.GetFirstAnswer(connection);
        if (answer == null && teacher.CurrentAnswer == null)
        {
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Работ на проверку пока нет", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
        }
        else
        {
            teacher.CurrentAnswer = answer;
            answer.TeacherId = teacher.Id;
            await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerEquivalence, connection);
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"Есть работа на проверку!\nВопрос: {answer.Question.Text}\nТекст: {answer.Text}", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"Оцените эквивалентность перевода: ", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);

        }
    }
}

async Task ProcessingTeacherCheckAnswerEquivalence(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Text.Equals(""))
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести комментарий!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        return;
    }
    teacher.CurrentAnswer.Comment = teacher.CurrentAnswer.Comment +  "\nЭквивалентность: " + update.Message.Text;
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Введите комментарий об адекватности текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerAdequacy);
}

async Task ProcessingTeacherCheckAnswerAdequacy(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Text.Equals(""))
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести комментарий!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        return;
    }
    teacher.CurrentAnswer.Comment = teacher.CurrentAnswer.Comment + "\nАдекватность: " + update.Message.Text;
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Введите комментарий об офоромлении текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerDesign);
}

async Task ProcessingTeacherCheckAnswerDesign(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Text.Equals(""))
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести комментарий!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        return;
    }
    teacher.CurrentAnswer.Comment = teacher.CurrentAnswer.Comment + "\nОформление: " + update.Message.Text;
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Введите комментарий о грамматике текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerGrammar);
}

async Task ProcessingTeacherCheckAnswerGrammar(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Text.Equals(""))
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести комментарий!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        return;
    }
    teacher.CurrentAnswer.Comment = teacher.CurrentAnswer.Comment + "\nГрамматика: " + update.Message.Text;
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Введите коментарий об орфографии текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerSpelling);
}

async Task ProcessingTeacherCheckAnswerSpelling(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Text.Equals(""))
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести комментарий!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        return;
    }
    teacher.CurrentAnswer.Comment = teacher.CurrentAnswer.Comment + "\nОрфография: " + update.Message.Text;
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Введите комментарий о соответствии стиля текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerStyle);
}

async Task ProcessingTeacherCheckAnswerStyle(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Text.Equals(""))
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести комментарий!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        return;
    }
    teacher.CurrentAnswer.Comment = teacher.CurrentAnswer.Comment + "\nСтиль: " + update.Message.Text;
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Оцените работу:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherWorkCheckRate);
}

async Task ProcessingTeacherWorkCheckRate(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int rate))
    {
        if (0 < rate && rate <= 10)
        {
            using (var connection = dbInteract.GetConnection())
            {
                teacher.CurrentAnswer.Rate = rate;
                await teacher.SetPosition(dbInteract, DialogPosition.TeacherWorkCheckComment);
                await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Введите комментарий:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
                teacher.SetPosition(dbInteract, DialogPosition.TeacherWorkCheckComment);
            }
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
    }
}

async Task ProcessingTeacherWorkCheckComment(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, IReplyMarkup teacherMainMenuButtons)
{
    using (var connection = dbInteract.GetConnection())
    {
        teacher.CurrentAnswer.Comment = teacher.CurrentAnswer.Comment + "\nКомментарий эксперта:" + update.Message.Text;
        await connection.OpenAsync();
        await dbInteract.UpdateAnswer(teacher.CurrentAnswer, connection);
        await teacher.AddBalance(dbInteract, 25, connection);
        await botClient.SendTextMessageAsync(chatId: teacher.CurrentAnswer.UserId, text: $"Ваша работа (ID:{teacher.CurrentAnswer.Id}) проверена!\nВы моежете посмотреть свои результаты в разделе 'Работы'.", cancellationToken: cancellationToken);
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherMainMenu, connection);
        teacher.CurrentAnswer = null;
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Отлично! Ваша проверка отправлена! На Ваш баланс добавлено: 25 рублей", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
    }
}

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Type == UpdateType.PreCheckoutQuery && update.PreCheckoutQuery != null)
    {
        string[] decodeArray = update.PreCheckoutQuery.InvoicePayload.Split('-');
        long userId = long.Parse(decodeArray[0]);
        byte amount = byte.Parse(decodeArray[1]);
        TelegramLingvoBot.User? payUser = Users.FirstOrDefault(x => x.Id == userId);
        if (payUser != null)
        {
            await payUser.AddQuestions(dbInteract, amount);
            await botClient.SendTextMessageAsync(chatId: userId, text: $"Спасибо за покупку! На ваш акканут добавлено {amount} вопросов!", cancellationToken: cancellationToken);
            await botClient.AnswerPreCheckoutQueryAsync(update.PreCheckoutQuery.Id);
        }
    }
    else if (update.Type != UpdateType.Message)
    {
        return;
    }

    if (update.Message == null || update.Message.Text == null)
    {
        return;
    }

    long chatId = update.Message.Chat.Id;
    TelegramLingvoBot.User? user = Users.FirstOrDefault(x => x.Id == chatId);
    TelegramLingvoBot.Teacher? teacher = null;
    if (user == null)
    {
        teacher = Teachers.FirstOrDefault(x => x.Id == chatId);
        if (teacher == null)
        {
            if (update.Message.Text.Equals("Регистрация"))
            {
                user = new TelegramLingvoBot.User(chatId);
                Users.Add(user);
                await dbInteract.AddUser(user);
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Спасибо за регистрацию! У Вас Есть 1 пробный вопрос.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Привет! Пожалуйста зарегестрируйся)", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RegisterButton);
            }
        }
    }
    if (update.Message.Text.Equals("/start"))
    {
        if (user != null)
        {
            await user.SetPosition(dbInteract, DialogPosition.MainMenu);
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
        else
        {
            await teacher.SetPosition(dbInteract, DialogPosition.TeacherMainMenu);
            if (teacher.Balance < 100)
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds);
            }
        }
        return;
    }

    DialogPosition dialogPosition;
    if (user != null)
    {
        dialogPosition = user.Position;
    }
    else
    {
        dialogPosition = teacher.Position;
    }

    switch (dialogPosition)
    {
        case DialogPosition.MainMenu:
            switch (update.Message.Text)
            {
                case "Магазин":
                    await ProcessingUserMainMenuShop(user, botClient, cancellationToken);
                    break;
                case "Работы":
                    await ProcessAllUserWorks(user, botClient, cancellationToken);
                    break;
                case "Я готов":
                    await ProcessngUserMainMenuUserIsReady(user, botClient, cancellationToken);
                    break;
                case "Профиль":
                    await ProcessingUserMainMenuProfile(user, botClient, cancellationToken);
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
                    await ProcessGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await ProcessingUserChooseWorkId(user, botClient, update, cancellationToken);
                    break;
            }
            break;
        case DialogPosition.WorkShown:
            switch (update.Message.Text)
            {
                case "Назад к моим работам":
                    await ProcessAllUserWorks(user, botClient, cancellationToken);
                    break;
                case "Назад в главное меню":
                    await ProcessGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    break;
            }
            break;
        case DialogPosition.ShopAmount:
            await ProcessingUserShopAmount(user, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherMainMenu:
            IReplyMarkup teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds;
            if (teacher.Balance < 100)
            {
                teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds;
            }
            switch (update.Message.Text)
            {
                case "Профиль":
                    await ProcessingTeacherMainMenuProfile(teacher, botClient, cancellationToken, teacherMainMenuButtons);
                    break;
                case "Проверить":
                    await ProcessingTeacherMainMenuCheckAnswer(teacher, botClient, cancellationToken, teacherMainMenuButtons);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
                    break;
            }
            break;
        case DialogPosition.TeacherCheckAnswerEquivalence:
            await ProcessingTeacherCheckAnswerEquivalence(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherCheckAnswerAdequacy:
            await ProcessingTeacherCheckAnswerAdequacy(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherCheckAnswerDesign:
            await ProcessingTeacherCheckAnswerDesign(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherCheckAnswerGrammar:
            await ProcessingTeacherCheckAnswerGrammar(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherCheckAnswerSpelling:
            await ProcessingTeacherCheckAnswerSpelling(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherCheckAnswerStyle:
            await ProcessingTeacherCheckAnswerStyle(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherWorkCheckRate:
            await ProcessingTeacherWorkCheckRate(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherWorkCheckComment:
            teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds;
            if (teacher.Balance < 100)
            {
                teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds;
            }
            await ProcessingTeacherWorkCheckComment(teacher, botClient, update, cancellationToken, teacherMainMenuButtons);
            break;

        case DialogPosition.AnswerTypeSelect:
            switch (update.Message.Text)
            {
                case "Общий вопрос":
                    await ProcessingUserAnswerTypeSelectCommonQuestions(user, botClient, cancellationToken);
                    break;
                case "Перевод текста":
                    await ProcessingUserAnswerTypeSelectTranslateText(user, botClient, cancellationToken);
                    break;
                case "Назад":
                    await ProcessGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите тип вопроса:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.AnswerTypeButtons);
                    break;
            }
            break;
        case DialogPosition.WaitingForResponce:
            await ProcessingUserWaitingForResponce(user, botClient, update, cancellationToken);
            break;
        case DialogPosition.UserProfileMenu:
            switch (update.Message.Text)
            {
                case "Редактировать любимые темы":
                    await ProcessingUserThemesMenu(user, botClient, cancellationToken);
                    break;
                case "Назад":
                    await ProcessGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите тип вопроса:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserProfileMenuButtons);
                    break;
            }
            break;
        case DialogPosition.UserThemesMenu:
            switch (update.Message.Text)
            {
                case "Добавить":
                    await ProcessingIncreaseUserThemesMenu(user, botClient, cancellationToken);
                    break;
                case "Убрать":
                    await ProcessingDecreaseUserThemesMenu(user, botClient, cancellationToken);
                    break;
                case "Назад":
                    await ProcessingUserMainMenuProfile(user, botClient, cancellationToken);
                    break;
                default:
                    long count = await dbInteract.GetCountOfFavoriteThemes(user.Id);
                    if (count == 0)
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nХотите добавить новые темы?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserThemesMenuButtonsWithoutRemove);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nХотите добавить новые темы или убрать текущие?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserThemesMenuButtons);
                    }
                    break;
            }
            break;
        case DialogPosition.UserThemesIncrease:
            switch (update.Message.Text)
            {
                case "Назад":
                    await ProcessingUserThemesMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await ProcessingUserThemesMenuIncrease(user, botClient, update, cancellationToken);
                    break;
            }
            break;
        case DialogPosition.UserThemesDecrease:
            switch (update.Message.Text)
            {
                case "Назад":
                    await ProcessingUserThemesMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await ProcessingUserThemesMenuDecrease(user, botClient, update, cancellationToken);
                    break;
            }
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

async void ResetAllUsers(object? sender, ElapsedEventArgs e)
{
    CancellationToken token = (CancellationToken)sender;
    IEnumerable<TelegramLingvoBot.User> usersUsedYesterday = Users.Where(x => !x.QuestionReady).Where(x => x.QuestionAmount > 0);
    await TelegramLingvoBot.User.ResetReadyAllUsers(dbInteract, Users);
    foreach (TelegramLingvoBot.User user in usersUsedYesterday)
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Доброе утро! Работа готова", cancellationToken: token);
    }
}