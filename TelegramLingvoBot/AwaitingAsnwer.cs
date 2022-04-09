using System.Timers;
using Telegram.Bot;

namespace TelegramLingvoBot
{
    internal class AwaitingAsnwer
    {
        private ITelegramBotClient BotClient { get; set; }
        private CancellationToken Token { get; set; }
        private List<AwaitingAsnwer> ListStorage { get; set; }
        private DataBaseInteractions dbInteract { get; set; }
        public User User { get; set; }
        public Question Question { get; set; }
        public System.Timers.Timer timer { get; set; }

        public AwaitingAsnwer(ITelegramBotClient BotClient, CancellationToken Token, List<AwaitingAsnwer> ListStorage, DataBaseInteractions dbInteract, User User, Question Question)
        {
            this.BotClient = BotClient;
            this.User = User;
            this.Question = Question;
            this.ListStorage = ListStorage;
            this.dbInteract = dbInteract;
            this.Token = Token;
            timer = new System.Timers.Timer(840000);
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
        }

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            timer.Stop();
            timer.Dispose();
            await BotClient.SendTextMessageAsync(chatId: User.Id, text: "Поторопись! У тебя отсалась одна минута!", cancellationToken: Token);
            timer = new System.Timers.Timer(60000);
            timer.Elapsed += OnFullTimeEnds;
            timer.Start();
        }

        private async void OnFullTimeEnds(object? sender, ElapsedEventArgs e)
        {
            timer.Stop();
            timer.Dispose();
            await BotClient.SendTextMessageAsync(chatId: User.Id, text: "К сожалению, Вы не успели уложится во время. Попробовать снова мы можете нажав на кнопку 'Я готов'", cancellationToken: Token, replyMarkup: ButtonBank.UserMainMenuButtons);
            ListStorage.Remove(this);
            await User.SetPosition(dbInteract, DialogPosition.MainMenu);
        }

        public void StopDestroy()
        {
            timer.Stop();
            timer.Dispose();
            ListStorage.Remove(this);
        }

        public Answer ToAnswer(string answer) => new Answer(0, User.Id, Question, answer, null, null);
    }
}
