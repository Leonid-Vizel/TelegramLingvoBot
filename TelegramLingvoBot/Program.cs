using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramLingvoBot;
using System.Timers;
using System.Collections.Concurrent;
using System.Diagnostics;

#region BaseLoading
bool AdditionalFlag = true;
ConcurrentBag<Answer> answerBagForModel = new ConcurrentBag<Answer>();
ParseMode ParseMode = Telegram.Bot.Types.Enums.ParseMode.Markdown;
DataBaseInteractions dbInteract = new DataBaseInteractions("Server=wpl36.hosting.reg.ru;Database=u1615366_LingvoHack;User Id=u1615366_LingvoHack;Password=y21e&B4a;charset=utf8;");
List<AwaitingAsnwer> awaitingAsnwers = new List<AwaitingAsnwer>();
List<TelegramLingvoBot.User> Users;
List<TelegramLingvoBot.Teacher> Teachers;
using (var connection = dbInteract.GetConnection())
{
    await connection.OpenAsync();
    Users = await dbInteract.GetAllUsers(connection);
    Teachers = await dbInteract.GetAllTeachers(connection);
    (await dbInteract.GetAnswersForGrammarCheck(connection)).ForEach(x => answerBagForModel.Add(x));
}
if (System.IO.File.Exists($"{Environment.CurrentDirectory}\\model\\end.txt"))
{
    System.IO.File.Delete($"{Environment.CurrentDirectory}\\model\\end.txt");
}
Thread thread = new Thread(new ThreadStart(WorkWithModel));
thread.Start();
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

using (var connection = dbInteract.GetConnection())
{
    await connection.OpenAsync();
    foreach (TelegramLingvoBot.User user in Users.Where(x => x.Position == DialogPosition.WaitingForResponce))
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Приносим свои извинения.\nПроизошла критическая ошибка бота и Ваш перевод был сброшен. Попробуйте снова. Ваши оплаченные переводы не убавились.", cancellationToken: cts.Token, replyMarkup: ButtonBank.UserMainMenuButtons);
        await user.SetPosition(dbInteract, DialogPosition.MainMenu, connection);
    }

    foreach (TelegramLingvoBot.Teacher teacher in Teachers.Where(x => (int)x.Position >= 13 && (int)x.Position <= 18))
    {
        if (teacher.CurrentAnswer != null)
        {
            teacher.CurrentAnswer.TeacherId = teacher.Id;
            await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerEquivalence, connection);
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"Есть работа на проверку!\n*Исходный*: {teacher.CurrentAnswer.Question.Text}\n*Перевод ученика*: {teacher.CurrentAnswer.Text}", cancellationToken: cts.Token, parseMode: ParseMode);
            if (teacher.CurrentAnswer.CheckedByModel != null)
            {
                await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"*Автокоррекция грамматики:*\n{teacher.CurrentAnswer.CheckedByModel}", cancellationToken: cts.Token, parseMode: ParseMode);
            }
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"Оцените точность перевода: ", cancellationToken: cts.Token, replyMarkup: ButtonBank.RateForAnswerButtons);
        }
        else
        {
            IReplyMarkup teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds;
            if (teacher.Balance < 100)
            {
                teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds;
            }
            await teacher.SetPosition(dbInteract, DialogPosition.TeacherMainMenu, connection);
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"Выберите опцию ☑️: ", cancellationToken: cts.Token, replyMarkup: teacherMainMenuButtons);
        }
    }
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
mainTimer.AutoReset = true;
mainTimer.Start();
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();
System.IO.File.Create($"{Environment.CurrentDirectory}\\model\\end.txt").Close();
AdditionalFlag = false;
#endregion

async Task ProcessingUserMainMenuShop(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Вам доступно {user.QuestionAmount} проверок работ.\nВыберите количество, которое хотите приобрести:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons);
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
                string checkedString = answer.AccuracyRate == null ? "Не проверен" : "Проверен";
                worksBuilder.AppendLine($"{answer.Id}) Перевод текста({answer.Question.Theme.Name}) - {checkedString}");
            }
            await user.SetPosition(dbInteract, DialogPosition.ChooseWorkId, connection);
            await botClient.SendTextMessageAsync(chatId: user.Id, text: worksBuilder.ToString(), cancellationToken: cancellationToken, replyMarkup: null);
            if (await dbInteract.GetCountOfUserReports(user.Id, connection) < 5)
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Введите *ID* работы, которую хотите посмотреть или выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ChooseWorkButtons, parseMode: ParseMode);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Введите *ID* работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "У вас пока нет написанных работ 🤷🏽‍♂‍", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
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
            using (var connection = dbInteract.GetConnection())
            {
                await connection.OpenAsync();
                StringBuilder builder = new StringBuilder("Выберите тему перевода:\n");
                foreach (Theme theme in await dbInteract.GetFavoriteThemesOfUser(user.Id, connection))
                {
                    builder.AppendLine($"*{theme.Id}*) _{theme.Name}_");
                }
                builder.Append("Введите *ID* темы или нажмите на кнопку '_Случайная тема_', для выбора перевода из случайной темы:");
                await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.ChooseThemeButtons, parseMode: ParseMode);
                await user.SetPosition(dbInteract, DialogPosition.AnswerThemeSelect, connection);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "У вас на аккаунте не имеется оплаченных переводов.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
    }
    else
    {
        if (user.QuestionAmount > 0)
        {
            if (DateTime.Now.Hour < 10)
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Вы уже использовали ежедневный перевод!\nСледующий перевод придёт Вам {DateTime.Now.ToString("dd.MM.yyy")} в 10:00 по Московскому времени.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Вы уже использовали ежедневный перевод!\nСледующий перевод придёт Вам {new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1).ToString("dd.MM.yyy")} в 10:00 по Московскому времени.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "У вас на аккаунте не имеется оплаченных переводов.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
    }
}

async Task ProcessingUserMainMenuProfile(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    StringBuilder builder = new StringBuilder();
    builder.AppendLine($"Ваш ID 🆔: *{user.Id}*");
    builder.AppendLine($"Средний балл 📊: *{await dbInteract.GetUserRating(user.Id)}*");
    builder.AppendLine($"Количество оплаченных переводов 💰: *{user.QuestionAmount}*");
    await user.SetPosition(dbInteract, DialogPosition.UserProfileMenu);
    await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserProfileMenuButtons, parseMode: ParseMode);
}

async Task ProcessUserGoBackToMainMenu(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
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
                if (await dbInteract.GetCountOfUserReports(user.Id, connection) < 5)
                {
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Работа с таким *ID* не была найдена! 🤖🤖🤖\nВведите Id работы, которую хотите посмотреть или выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ChooseWorkButtons, parseMode: ParseMode);

                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Работа с таким *ID* не была найдена! 🤖🤖🤖\nВведите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
                }
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"*Работа #{answer.Id}*");
                builder.AppendLine($"*Тема:* _{answer.Question.Theme.Name}_");
                builder.AppendLine($"*Текст:* {answer.Question.Text}");
                if (answer.AccuracyRate == null)
                {
                    builder.AppendLine("*Работа проверена:* Нет");
                }
                else
                {
                    builder.AppendLine("*Работа проверена:* Да");
                }
                builder.AppendLine($"*Текст работы:*\n{answer.Text}");
                if (answer.AccuracyRate != null)
                {
                    builder.AppendLine($"*Точность:* {answer.AccuracyRate}/10");
                    builder.AppendLine($"*Адекватность:* {answer.AdequacyRate}/10");
                    builder.AppendLine($"*Грамматика:* {answer.GrammarRate}/10");
                    builder.AppendLine($"*Орфография:* {answer.SpellingRate}/10");
                    builder.AppendLine($"*Соответствие стилю:* {answer.WritingStyleRate}/10");
                    builder.AppendLine($"*Общий комментарий:*\n{answer.Comment}");
                }
                await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons, parseMode: ParseMode);
                await user.SetPosition(dbInteract, DialogPosition.WorkShown, connection);
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.WorkShownButtons);
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
            await botClient.SendInvoiceAsync(chatId: user.Id, title: "Покупка переводов", description: "10 переводов", payload: $"{user.Id}-10", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "50":
            myList.Add(new LabeledPrice("Цена", 230000));
            await botClient.SendInvoiceAsync(chatId: user.Id, title: "Покупка переводов", description: "50 переводов", payload: $"{user.Id}-50", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "100":
            myList.Add(new LabeledPrice("Цена", 450000));
            await botClient.SendInvoiceAsync(chatId: user.Id, title: "Покупка переводов", description: "100 переводов", payload: $"{user.Id}-100", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "Назад":
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "Выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            await user.SetPosition(dbInteract, DialogPosition.MainMenu);
            break;
        default:
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите *количество* переводов которое хотите купить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons, parseMode: ParseMode);
            break;
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
            await dbInteract.UpdateTeacherAnswerId(teacher.Id, teacher.CurrentAnswer.Id, connection);
            await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerEquivalence, connection);
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"Есть работа на проверку!\nПереводы: {answer.Question.Text}\nТекст: {answer.Text}", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
            if (teacher.CurrentAnswer.CheckedByModel != null)
            {
                await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"*Автокоррекция грамматики:*\n{teacher.CurrentAnswer.CheckedByModel}", cancellationToken: cts.Token, parseMode: ParseMode);
            }
            await botClient.SendTextMessageAsync(chatId: teacher.Id, text: $"Оцените точность перевода: ", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        }
    }
}

async Task ProcessingTeacherWorkCheckComment(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, IReplyMarkup teacherMainMenuButtons)
{
    using (var connection = dbInteract.GetConnection())
    {
        teacher.CurrentAnswer.Comment = update.Message.Text;
        await connection.OpenAsync();
        await dbInteract.UpdateAnswer(teacher.CurrentAnswer, connection);
        await teacher.AddBalance(dbInteract, 40, connection);
        await botClient.SendTextMessageAsync(chatId: teacher.CurrentAnswer.UserId, text: $"Ваша работа (ID:{teacher.CurrentAnswer.Id}) проверена!\nВы моежете посмотреть свои результаты в разделе 'Работы'.", cancellationToken: cancellationToken);
        await dbInteract.UpdateTeacherAnswerId(teacher.Id, null, connection);
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherMainMenu, connection);
        teacher.CurrentAnswer = null;
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Отлично! Ваша проверка отправлена! На Ваш баланс добавлено: 40 рублей", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
    }
}

async Task ProcessingUserAnswerTypeSelectTranslateText(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Text.Equals("Случайная тема"))
    {
        using (var connection = dbInteract.GetConnection())
        {
            await connection.OpenAsync();
            List<Theme> themes = await dbInteract.GetAllThemes(connection);
            Random random = new Random(Guid.NewGuid().GetHashCode());
            Theme? theme = themes[random.Next(0, themes.Count)];
            if (theme != null)
            {
                List<Question> questions = await dbInteract.GetQuesionsFromTheme(theme.Id, connection);
                List<int> userTranslateIds = await dbInteract.GetUserUsedQuestionIdsFromTheme(user.Id, theme.Id, connection);
                List<Question> questionsProcessed = questions.Where(x => !userTranslateIds.Any()).ToList();
                Question question;
                if (questionsProcessed.Count > 0)
                {
                    question = questionsProcessed[0];
                }
                else
                {
                    question = questions[random.Next(0, questions.Count())];
                }
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Ваше сегодняшнее задание 📖:\n{question.Text}\n*На перевод даётся 15 минут.*", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons, parseMode: ParseMode);
                await user.SetPosition(dbInteract, DialogPosition.WaitingForResponce, connection);
                awaitingAsnwers.Add(new AwaitingAsnwer(botClient, cancellationToken, awaitingAsnwers, dbInteract, user, question));
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВведите *ID* темы или нажмите на кнопку 'Случайная тема', для выбора перевода из случайной темы: ", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ChooseThemeButtons, parseMode: ParseMode);
            }
        }
    }
    else
    {
        using (var connection = dbInteract.GetConnection())
        {
            await connection.OpenAsync();
            if (int.TryParse(update.Message.Text, out int themeId))
            {
                Theme? theme = await dbInteract.GetTheme(themeId, connection);
                if (theme != null)
                {
                    List<Question> questions = await dbInteract.GetQuesionsFromTheme(themeId, connection);
                    List<int> userTranslateIds = await dbInteract.GetUserUsedQuestionIdsFromTheme(user.Id, theme.Id, connection);
                    List<Question> questionsProcessed = questions.Where(x => !userTranslateIds.Any()).ToList();
                    Question question;
                    if (questionsProcessed.Count > 0)
                    {
                        question = questionsProcessed[0];
                    }
                    else
                    {
                        Random random = new Random(Guid.NewGuid().GetHashCode());
                        question = questions[random.Next(0, questions.Count())];
                    }
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Ваше сегодняшнее задание:\n{question.Text}\nНа перевод даётся 15 минут.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
                    await user.SetPosition(dbInteract, DialogPosition.WaitingForResponce, connection);
                    awaitingAsnwers.Add(new AwaitingAsnwer(botClient, cancellationToken, awaitingAsnwers, dbInteract, user, question));
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, такая тема не найдена 🤖🤖🤖\nВведите *ID* темы или нажмите на кнопку 'Случайная тема', для выбора перевода из случайной темы: ", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ChooseThemeButtons, parseMode: ParseMode);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВведите *ID* темы или нажмите на кнопку 'Случайная тема', для выбора перевода из случайной темы: ", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ChooseThemeButtons, parseMode: ParseMode);
            }
        }
    }
}

async Task ProcessingUserWaitingForResponce(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    AwaitingAsnwer? awaitingAsnwer = awaitingAsnwers.FirstOrDefault(x => x.User.Id == user.Id);
    if (awaitingAsnwer == null)
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Приносим свои извинения.\nПроизошла критическая ошибка бота и Ваш пее был сброшен. Попробуйте снова. Ваши оплаченные переводы не убавились.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        await user.SetPosition(dbInteract, DialogPosition.MainMenu);
    }
    else
    {
        using (var connection = dbInteract.GetConnection())
        {
            await connection.OpenAsync();
            awaitingAsnwer.StopDestroy();
            await user.SetPosition(dbInteract, DialogPosition.MainMenu, connection);
            await user.SetReady(dbInteract, false, connection);
            await user.DecrementQuestion(dbInteract, connection);
            Answer answ = awaitingAsnwer.ToAnswer(update.Message.Text);
            answerBagForModel.Add(answ);
            await dbInteract.AddAnswer(answ, connection);
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

        if (userThemes.Count == 0)
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "*У Вас пока не выбраны любимые темы*.\nХотите добавить новые темы?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserThemesMenuButtonsWithoutRemove, parseMode: ParseMode);
        }
        else
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("*Ваши любимые темы:*");
            foreach (Theme theme in userThemes.OrderBy(x => x.Id))
            {
                builder.AppendLine($"_{theme.Name}_");
            }
            if (userThemes.Count == (await dbInteract.GetAllThemes(connection)).Count())
            {
                builder.Append("*Хотите убрать какие-нибудь из текущих?*");
                await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserThemesMenuButtonsWithoutAdd, parseMode: ParseMode);
            }
            else
            {
                builder.Append("*Хотите добавить новые темы или убрать текущие?*");
                await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserThemesMenuButtons, parseMode: ParseMode);
            }
        }
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
        if (userThemesToExcept.Count == allThemes.Count)
        {
            await botClient.SendTextMessageAsync(chatId: user.Id, text: "*Все* темы бота уже добавлены в ваши любимые.", cancellationToken: cancellationToken, parseMode: ParseMode);
            await ProcessingUserThemesMenu(user, botClient, cancellationToken);
        }
        else
        {
            StringBuilder builder = new StringBuilder();
            foreach (Theme theme in allThemes.Where(x => !userThemesToExcept.Any(y => y.Id == x.Id)))
            {
                builder.AppendLine($"*{theme.Id}*) _{theme.Name}_");
            }
            builder.AppendLine("Выберите *ID* тем, которую хотите добавить:");
            await user.SetPosition(dbInteract, DialogPosition.UserThemesIncrease, connection);
            await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
        }
    }
}

async Task ProcessingDecreaseUserThemesMenu(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    using (var connection = dbInteract.GetConnection())
    {
        await connection.OpenAsync();
        List<Theme> userThemes = await dbInteract.GetFavoriteThemesOfUser(user.Id, connection);
        if (userThemes.Count() > 0)
        {
            StringBuilder builder = new StringBuilder();
            foreach (Theme theme in userThemes)
            {
                builder.AppendLine($"{theme.Id}) {theme.Name}");
            }
            builder.AppendLine("Выберите *ID* темы, которую хотите убрать:");
            await user.SetPosition(dbInteract, DialogPosition.UserThemesDecrease, connection);
            await botClient.SendTextMessageAsync(chatId: user.Id, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
        }
        else
        {
            await ProcessingUserThemesMenu(user, botClient, cancellationToken);
        }
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
                    if (userThemes.Count() + 1 == (await dbInteract.GetAllThemes(connection)).Count)
                    {
                        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Тема успешно добавлена!", cancellationToken: cancellationToken);
                        await ProcessingUserThemesMenu(user, botClient, cancellationToken);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Тема успешно добавлена! Хотите добавить ещё одну?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, эта темя уже входит в список любимых тем 🤖🤖🤖.Выберите *ID* темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, тема с таким идентификатором не найдена 🤖🤖🤖\nВыберите *ID* темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
            }
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите *ID* темы, которую хотите добавить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
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
                        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Тема успешно убрана! Хотите убрать ещё одну?", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Тема успешно убрана!", cancellationToken: cancellationToken);
                        await ProcessingUserThemesMenu(user, botClient, cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, эта тема уже входит в список любимых тем 🤖🤖🤖\nВыберите *ID* темы, которую хотите убрать:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, тема с таким идентификатором не найдена 🤖🤖🤖\nВыберите *ID* темы, которую хотите убрать:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
            }
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите *ID* темы, которую хотите убрать:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
    }
}

async Task ProcessingTeacherCheckAnswerAccuracy(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int result) && result >= 1 && result <= 10)
    {
        teacher.CurrentAnswer.AccuracyRate = result;
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Оцените адекватность текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerAdequacy);
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести оценку!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        return;
    }
}

async Task ProcessingTeacherCheckAnswerAdequacy(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int result) && result >= 1 && result <= 10)
    {
        teacher.CurrentAnswer.AdequacyRate = result;
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Оцените грамматику текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerGrammar);
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести оценку!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        return;
    }
}

async Task ProcessingTeacherCheckAnswerGrammar(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int result) && result >= 1 && result <= 10)
    {
        teacher.CurrentAnswer.GrammarRate = result;
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Оцените орфографию текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerSpelling);
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести оценку!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        return;
    }
}

async Task ProcessingTeacherCheckAnswerSpelling(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int result) && result >= 1 && result <= 10)
    {
        teacher.CurrentAnswer.SpellingRate = result;
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Оцените соответствие стиля текста:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherCheckAnswerStyle);
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести оценку!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        return;
    }
}

async Task ProcessingTeacherCheckAnswerStyle(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (int.TryParse(update.Message.Text, out int result) && result >= 1 && result <= 10)
    {
        teacher.CurrentAnswer.WritingStyleRate = result;
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Общий комментарий:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherWorkCheckComment);
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Требуется ввести оценку!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
        return;
    }
}
async Task ProcessTeacherGoBackToMainMenu(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, CancellationToken cancellationToken, IReplyMarkup buttons)
{
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: "Выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: buttons);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherMainMenu);
}

async Task ProcessSendReportSelectWork(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    await user.SetPosition(dbInteract, DialogPosition.UserWaitingForReportNumber);
    await botClient.SendTextMessageAsync(chatId: user.Id, text: "Введите *ID* работы для подачи жалобы:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
}

async Task ProcessSendReport(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (long.TryParse(update.Message.Text, out long result))
    {
        using (var connection = dbInteract.GetConnection())
        {
            await connection.OpenAsync();
            Answer? answer = await dbInteract.GetAnswer(result, connection);
            if (answer == null || answer.UserId != user.Id)
            {
                await botClient.SendTextMessageAsync(chatId: user.Id, text: "Работа с таким *ID* не была найдена! 🤖🤖🤖\nВведите *ID* работы, на которую хотите подать жалобу:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
            }
            else
            {
                await dbInteract.AddReport(user.Id, answer.Id, connection);
                await botClient.SendTextMessageAsync(chatId: user.Id, text: $"Ваша жалоба на проверку работы *#{answer.Id}* была успешно отправлена!\nОбратите внимание, если Вы будете отправлять сшком много жалоб, то эта функция станет недоступна!", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons, parseMode: ParseMode);
                await ProcessUserGoBackToMainMenu(user, botClient, cancellationToken);
            }
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Кажется Вы ввели что-то неправильно 🤖🤖🤖\nВведите *ID* работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
    }
}

async Task ProcessingSendUserHelp(TelegramLingvoBot.User? user, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(chatId: user.Id, text: FileTXTInteractions.ReadTXT("help"), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
    await user.SetPosition(dbInteract, DialogPosition.MainMenu);
}
async Task ProcessingSendTeacherHelp(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(chatId: teacher.Id, text: FileTXTInteractions.ReadTXT("helpForTeacher"), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton, parseMode: ParseMode);
    await teacher.SetPosition(dbInteract, DialogPosition.TeacherHelp);
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
            await botClient.SendTextMessageAsync(chatId: userId, text: $"Спасибо за покупку! На ваш акканут добавлено *{amount}* переводов!", cancellationToken: cancellationToken, parseMode: ParseMode);
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
                await botClient.SendTextMessageAsync(chatId: chatId, text: FileTXTInteractions.ReadTXT("textAfterRegistration"), cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons, parseMode: ParseMode);
                return;
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: FileTXTInteractions.ReadTXT("textFirstMessage"), cancellationToken: cancellationToken, replyMarkup: ButtonBank.RegisterButton, parseMode: ParseMode);
                return;
            }
        }
    }
    if (update.Message.Text.Equals("/start"))
    {
        if (user != null)
        {
            if (user.Position != DialogPosition.WaitingForResponce)
            {
                await user.SetPosition(dbInteract, DialogPosition.MainMenu);
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
        }
        else
        {
            if ((int)teacher.Position < 13 || (int)teacher.Position > 18 )
            {
                await teacher.SetPosition(dbInteract, DialogPosition.TeacherMainMenu);
                if (teacher.Balance < 100)
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds);
                }
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
                case "Назад":
                    await ProcessUserGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
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
                case "Помощь":
                    await ProcessingSendUserHelp(user, botClient, cancellationToken);
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
                    await ProcessUserGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                case "Отправить жалобу":
                    await ProcessSendReportSelectWork(user, botClient, cancellationToken);
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
                case "Главное меню":
                    await ProcessUserGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите опцию ☑️:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.WorkShownButtons);
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
                case "Назад":
                    await ProcessTeacherGoBackToMainMenu(teacher, botClient, cancellationToken, teacherMainMenuButtons);
                    break;
                case "Профиль":
                    await ProcessingTeacherMainMenuProfile(teacher, botClient, cancellationToken, teacherMainMenuButtons);
                    break;
                case "Проверить":
                    await ProcessingTeacherMainMenuCheckAnswer(teacher, botClient, cancellationToken, teacherMainMenuButtons);
                    break;
                case "Помощь":
                    await ProcessingSendTeacherHelp(teacher, botClient, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
                    break;
            }
            break;
        case DialogPosition.TeacherCheckAnswerEquivalence:
            await ProcessingTeacherCheckAnswerAccuracy(teacher, botClient, update, cancellationToken);
            break;
        case DialogPosition.TeacherCheckAnswerAdequacy:
            await ProcessingTeacherCheckAnswerAdequacy(teacher, botClient, update, cancellationToken);
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
        case DialogPosition.TeacherWorkCheckComment:
            teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds;
            if (teacher.Balance < 100)
            {
                teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds;
            }
            await ProcessingTeacherWorkCheckComment(teacher, botClient, update, cancellationToken, teacherMainMenuButtons);
            break;
        case DialogPosition.AnswerThemeSelect:
            switch (update.Message.Text)
            {
                case "Назад":
                    await ProcessUserGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await ProcessingUserAnswerTypeSelectTranslateText(user, botClient, update, cancellationToken);
                    break;
            }
            break;
        case DialogPosition.WaitingForResponce:
            await ProcessingUserWaitingForResponce(user, botClient, update, cancellationToken);
            break;
        case DialogPosition.UserProfileMenu:
            switch (update.Message.Text)
            {
                case "Любимые темы":
                    await ProcessingUserThemesMenu(user, botClient, cancellationToken);
                    break;
                case "Назад":
                    await ProcessUserGoBackToMainMenu(user, botClient, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserProfileMenuButtons);
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
        case DialogPosition.TeacherHelp:
            teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds;
            if (teacher.Balance < 100)
            {
                teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds;
            }
            switch (update.Message.Text)
            {
                case "Назад":
                    await ProcessTeacherGoBackToMainMenu(teacher, botClient, cancellationToken, teacherMainMenuButtons);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
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
        case DialogPosition.UserWaitingForReportNumber:
            switch (update.Message.Text)
            {
                case "Назад":
                    await ProcessAllUserWorks(user, botClient, cancellationToken);
                    break;
                default:
                    await ProcessSendReport(user, botClient, update, cancellationToken);
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

void WorkWithModel()
{
    ProcessStartInfo startInfo = new ProcessStartInfo("python");
    Process process = new Process();

    string directory = $"{Environment.CurrentDirectory}\\model\\";
    string script = "script.py";

    startInfo.WorkingDirectory = directory;
    startInfo.Arguments = script;
    startInfo.UseShellExecute = false;
    startInfo.CreateNoWindow = true;
    startInfo.RedirectStandardError = true;
    startInfo.RedirectStandardOutput = true;

    process.StartInfo = startInfo;
    process.Start();

    while (AdditionalFlag)
    {
        if (answerBagForModel.Count > 0)
        {
            if (answerBagForModel.TryTake(out Answer answer))
            {
                Thread.Sleep(500);
                System.IO.File.WriteAllText("model\\text.txt", answer.Text);
                while (!System.IO.File.Exists("model\\prediction.txt")) { }
                Thread.Sleep(500);
                string read = System.IO.File.ReadAllText("model\\prediction.txt");
                if (!read.Equals("He *is* driving a car."))
                {
                    answer.CheckedByModel = read;
                    System.IO.File.Delete("model\\prediction.txt");
                    dbInteract.SetCheckedByModel(answer).Wait();
                    Console.WriteLine($"CHECKED! OUTPUT: {answer.CheckedByModel}");
                }
            }
        }
    }
}