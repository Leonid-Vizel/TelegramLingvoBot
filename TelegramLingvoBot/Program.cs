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
    TimeToExecuteTask = new DateTime(DateTime.Now.Year, DateTime.Now.Month,DateTime.Now.Day + 1,10,0,0);
}
mainTimer = new System.Timers.Timer((TimeToExecuteTask - DateTime.Now).TotalMilliseconds);
mainTimer.Elapsed += ResetAllUsers;
mainTimer.Start();
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();
#endregion

async Task ProcessingUserMainMenuShop(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    await botClient.SendTextMessageAsync(chatId: chatId, text: $"Добро пожаловать в магазин! Доступно {await dbInteract.GetUserAvailibleQuestionsAmount(chatId)} вопросов.\nВыберите кол - во вопросов которое хотите купить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons);
    await user.SetPosition(dbInteract, DialogPosition.ShopAmount);
}
async Task ProcessingUserMainMenuWork(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    StringBuilder worksBuilder = new StringBuilder("Список ваших работ:\n");
    List<Answer> answersOfUser = await dbInteract.GetAnswersOfUser(chatId);
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
        await botClient.SendTextMessageAsync(chatId: chatId, text: worksBuilder.ToString(), cancellationToken: cancellationToken, replyMarkup: null);
        await botClient.SendTextMessageAsync(chatId: chatId, text: "Введите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
        await user.SetPosition(dbInteract, DialogPosition.ChooseWorkId);
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: chatId, text: "У тебя пока нет написанных работ.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        await user.SetPosition(dbInteract, DialogPosition.MainMenu);
    }
}
async Task ProcessngUserMainMenuUserIsReady(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    if (user.QuestionReady)
    {
        if (user.QuestionAmount > 0)
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите тип вопроса:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.AnswerTypeButtons);
            await user.SetPosition(dbInteract, DialogPosition.AnswerTypeSelect);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: "У вас на аккаунте не имеется оплаченных вопросов.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
    }
    else
    {
        if (user.QuestionAmount > 0)
        {
            if (DateTime.Now.Hour < 10)
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Вы уже использовали вопрос!\nСледующий вопрос придёт Вам {DateTime.Now.ToString("dd.MM.yyy")} в 10:00 по Московскому времени.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Вы уже использовали вопрос!\nСледующий вопрос придёт Вам {new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1).ToString("dd.MM.yyy")} в 10:00 по Московскому времени.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: "У вас на аккаунте не имеется оплаченных вопросов.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
    }
}
async Task ProcessingUserMainMenuProfile(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    StringBuilder builder = new StringBuilder();
    builder.AppendLine($"Ваш Id в системе: {user.Id}");
    builder.AppendLine($"Ваш рейтинг: {await dbInteract.GetUserRating(user.Id)}");
    builder.AppendLine($"Количество оплаченных вопросов: {user.QuestionAmount}");
    await botClient.SendTextMessageAsync(chatId: chatId, text: builder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
}
async Task ProcessingUserChooseWorkIdBack(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
    await user.SetPosition(dbInteract, DialogPosition.MainMenu);
}
async Task ProcessingUserChooseWorkIdIsNotFound(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    if (long.TryParse(update.Message.Text, out long result))
    {
        Answer? answer = await dbInteract.GetAnswer(result);
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
            await user.SetPosition(dbInteract, DialogPosition.WorkShown);
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: chatId, text: "Кажется Вы ввели что-то неправильно 🤖🤖🤖\nВведите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
    }
}
async Task ProcessingUserBackToUserWork(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    StringBuilder worksBuilder = new StringBuilder("Список ваших работ:\n");
    List<Answer> answersOfUser = await dbInteract.GetAnswersOfUser(chatId);
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
    await botClient.SendTextMessageAsync(chatId: chatId, text: worksBuilder.ToString(), cancellationToken: cancellationToken, replyMarkup: ButtonBank.JustBackButton);
    await botClient.SendTextMessageAsync(chatId: chatId, text: "Введите Id работы, которую хотите посмотреть:", cancellationToken: cancellationToken, replyMarkup: null);
    await user.SetPosition(dbInteract, DialogPosition.ChooseWorkId);
}
async Task ProcessingUserBackToMainMenuWork(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
    await user.SetPosition(dbInteract, DialogPosition.MainMenu);
}
async Task ProcessingUserShopAmount(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    List<LabeledPrice> myList = new List<LabeledPrice>();
    switch (update.Message.Text)
    {
        case "10":
            myList.Add(new LabeledPrice("Цена", 50000));
            await botClient.SendInvoiceAsync(chatId: chatId, title: "Покупка вопросов", description: "10 вопросов", payload: $"{chatId}-10", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "50":
            myList.Add(new LabeledPrice("Цена", 230000));
            await botClient.SendInvoiceAsync(chatId: chatId, title: "Покупка вопросов", description: "50 вопросов", payload: $"{chatId}-50", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "100":
            myList.Add(new LabeledPrice("Цена", 450000));
            await botClient.SendInvoiceAsync(chatId: chatId, title: "Покупка вопросов", description: "100 вопросов", payload: $"{chatId}-100", providerToken: "381764678:TEST:35685", currency: "RUB", myList);
            break;
        case "Назад":
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Выберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            await user.SetPosition(dbInteract, DialogPosition.MainMenu);
            break;
        default:
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите кол - во вопросов которое хотите купить:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.ShopButtons);
            break;
    }
}
async Task ProcessingTeacherMainMenuProfile(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, IReplyMarkup teacherMainMenuButtons)
{
    long countOfVerifiedAnswersOfTeacher = await dbInteract.GetCountOfVerifiedAnswersOfTeacher(teacher.Id);
    StringBuilder TeacherProfileText = new StringBuilder();
    TeacherProfileText.AppendLine($"Ваш Id в системе: {teacher.Id}");
    TeacherProfileText.AppendLine($"Количество проверенных работ: {countOfVerifiedAnswersOfTeacher}");
    TeacherProfileText.AppendLine($"Баланс: {teacher.Balance}");
    await botClient.SendTextMessageAsync(chatId: chatId, text: TeacherProfileText.ToString(), cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
}
async Task ProcessingTeacherMainMenuCheckAnswer(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, IReplyMarkup teacherMainMenuButtons)
{
    Answer? answer = await dbInteract.GetFirstAnswer();
    if (answer == null && teacher.CurrentAnswer == null)
    {
        await botClient.SendTextMessageAsync(chatId: chatId, text: "Работ на проверку пока нет", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
    }
    else
    {
        teacher.CurrentAnswer = answer;
        answer.TeacherId = teacher.Id;
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherWorkCheckComment);
        await botClient.SendTextMessageAsync(chatId: chatId, text: $"Есть работа на проверку!\nВопрос: {answer.Question.Text}\nТекст: {answer.Text}\nВведите комментарий: ", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    }
}
async Task ProcessingTeacherWorkCheckComment(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    if (update.Message.Text != "")
    {
        teacher.CurrentAnswer.Comment = update.Message.Text;
        await teacher.SetPosition(dbInteract, DialogPosition.TeacherWorkCheckRate);
        await botClient.SendTextMessageAsync(chatId: chatId, text: "Укажите оценку от 1 до 10:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
    }
}
async Task ProcessingTeacherWorkCheckRate(TelegramLingvoBot.Teacher? teacher, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, IReplyMarkup teacherMainMenuButtons)
{
    teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds;
    long countOfVerifiedAnswersOfTeacher = await dbInteract.GetCountOfVerifiedAnswersOfTeacher(teacher.Id);
    if (teacher.Balance < 100)
    {
        teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds;
    }
    if (int.TryParse(update.Message.Text, out int rate))
    {
        if (0 < rate && rate <= 10)
        {
            teacher.CurrentAnswer.Rate = rate;
            await dbInteract.UpdateAnswer(teacher.CurrentAnswer);
            await teacher.AddBalance(dbInteract, 25);
            await botClient.SendTextMessageAsync(chatId: teacher.CurrentAnswer.UserId, text: $"Ваша работа (ID:{teacher.CurrentAnswer.Id}) проверена!\nВы моежете посмотреть свои результаты в разделе 'Работы'.", cancellationToken: cancellationToken);
            teacher.CurrentAnswer = null;
            await teacher.SetPosition(dbInteract, DialogPosition.TeacherMainMenu);
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Отлично! Ваша проверка отправлена! На Ваш баланс добавлено: 25 рублей", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RateForAnswerButtons);
    }
}
async Task ProcessingUserAnswerTypeSelectCommonQuestions(TelegramLingvoBot.Teacher? teacher, TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    List<Question> questions = await dbInteract.GetQuestionsByType(QuestionType.GeneralQuestion);
    List<int> userQuestionIds = await dbInteract.GetUserUsedQuestionIdsWithType(chatId, QuestionType.GeneralQuestion);
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
    await botClient.SendTextMessageAsync(chatId: chatId, text: $"Ваш сегодняшний вопрос:\n{question.Text}\nНа ответ даётся 5 минут.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    await user.SetPosition(dbInteract, DialogPosition.WaitingForResponce);
    awaitingAsnwers.Add(new AwaitingAsnwer(botClient, cancellationToken, awaitingAsnwers, dbInteract, user, question));
}
async Task ProcessingUserAnswerTypeSelectTranslateText(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    List<Question> translates = await dbInteract.GetQuestionsByType(QuestionType.GeneralQuestion);
    List<int> userTranslateIds = await dbInteract.GetUserUsedQuestionIdsWithType(chatId, QuestionType.GeneralQuestion);
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
    await botClient.SendTextMessageAsync(chatId: chatId, text: $"Ваше сегодняшнее задание:\n{translate.Text}\nНа перевод даётся 8 минут.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.EmptyButtons);
    await user.SetPosition(dbInteract, DialogPosition.WaitingForResponce);
    awaitingAsnwers.Add(new AwaitingAsnwer(botClient, cancellationToken, awaitingAsnwers, dbInteract, user, translate));
}
async Task ProcessingUserWaitingForResponce(TelegramLingvoBot.User? user, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
{
    if (update.Message.Text != null)
    {
        AwaitingAsnwer? awaitingAsnwer = awaitingAsnwers.FirstOrDefault();
        if (awaitingAsnwer == null)
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Приносим свои извинения.\nПроизошла критическая ошибка бота и Ваш вопрос был сброшен. Попробуйте снова. Ваши оплаченные вопросы не убавились.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
            await user.SetPosition(dbInteract, DialogPosition.MainMenu);
        }
        else
        {
            awaitingAsnwer.StopDestroy();
            await user.SetPosition(dbInteract, DialogPosition.MainMenu);
            await user.SetReady(dbInteract, false);
            await user.DecrementQuestion(dbInteract);
            await dbInteract.AddAnswer(awaitingAsnwer.ToAnswer(update.Message.Text));
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Отлично!\nВаша работы была отправлена на проверку. Как только работа будет проверена, мы оповестим Вас.", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
        }
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
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Привет! Зарегайся пж)", cancellationToken: cancellationToken, replyMarkup: ButtonBank.RegisterButton);
            }
        }
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
                    await ProcessingUserMainMenuShop(user, botClient, update, cancellationToken, chatId);
                    break;
                case "Работы":
                    await ProcessingUserMainMenuWork(user, botClient, update, cancellationToken, chatId);
                    break;
                case "Я готов":
                    await ProcessngUserMainMenuUserIsReady(user, botClient, update, cancellationToken, chatId);
                    break;
                case "Профиль":
                    await ProcessingUserMainMenuProfile(user, botClient, update, cancellationToken, chatId);
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
                    await ProcessingUserChooseWorkIdBack(user, botClient, update, cancellationToken, chatId);
                    break;
                default:
                    await ProcessingUserChooseWorkIdIsNotFound(user, botClient, update, cancellationToken, chatId);
                    break;
            }
            break;
        case DialogPosition.WorkShown:
            switch (update.Message.Text)
            {
                case "Назад к моим работам":
                    await ProcessingUserBackToUserWork(user, botClient, update, cancellationToken, chatId);
                    break;
                case "Назад в главное меню":
                    await ProcessingUserBackToMainMenuWork(user, botClient, update, cancellationToken, chatId);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите опцию:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.UserMainMenuButtons);
                    break;
            }
            break;
        case DialogPosition.ShopAmount:
            await ProcessingUserShopAmount(user, botClient, update, cancellationToken, chatId);
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
                    await ProcessingTeacherMainMenuProfile(teacher, botClient, update, cancellationToken, chatId, teacherMainMenuButtons);
                    break;
                case "Проверить":
                    await ProcessingTeacherMainMenuCheckAnswer(teacher, botClient, update, cancellationToken, chatId, teacherMainMenuButtons);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖", cancellationToken: cancellationToken, replyMarkup: teacherMainMenuButtons);
                    break;
            }
            break;
        case DialogPosition.TeacherWorkCheckComment:
            await ProcessingTeacherWorkCheckComment(teacher, botClient, update, cancellationToken, chatId);
            break;
        case DialogPosition.TeacherWorkCheckRate:
            teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithWithdrawalOfFunds;
            if (teacher.Balance < 100)
            {
                teacherMainMenuButtons = ButtonBank.TeacherMainMenuButtonsWithoutWithdrawalOfFunds;
            }
            await ProcessingTeacherWorkCheckRate(teacher, botClient, update, cancellationToken, chatId, teacherMainMenuButtons);
            break;
        case DialogPosition.AnswerTypeSelect:
            switch (update.Message.Text)
            {
                case "Общий вопрос":
                    await ProcessingUserAnswerTypeSelectCommonQuestions(teacher, user, botClient, update, cancellationToken, chatId);
                    break;
                case "Перевод текста":
                    await ProcessingUserAnswerTypeSelectTranslateText(user, botClient, update, cancellationToken, chatId);
                    break;
                case "Назад":
                    await ProcessingUserBackToMainMenuWork(user, botClient, update, cancellationToken, chatId);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Извините, я Вас не понял 🤖🤖🤖\nВыберите тип вопроса:", cancellationToken: cancellationToken, replyMarkup: ButtonBank.AnswerTypeButtons);
                    break;
            }
            break;
        case DialogPosition.WaitingForResponce:
            await ProcessingUserWaitingForResponce(user, botClient, update, cancellationToken, chatId);
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
    IEnumerable<TelegramLingvoBot.User> usersUsedYesterday = Users.Where(x=>!x.QuestionReady).Where(x => x.QuestionAmount > 0);
    await TelegramLingvoBot.User.ResetReadyAllUsers(dbInteract,Users);
    foreach(TelegramLingvoBot.User user in usersUsedYesterday)
    {
        await botClient.SendTextMessageAsync(chatId: user.Id, text: "Доброе утро! Работа готова", cancellationToken: token);
    }
}