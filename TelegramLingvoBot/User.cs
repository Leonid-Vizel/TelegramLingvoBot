using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLingvoBot
{
    internal class User
    {
        public long Id { get; private set; }
        public int QuestionAmount { get; private set; }
        public DialogPosition Position { get; private set; }
        public bool QuestionReady { get; private set; }

        public User(long id, DialogPosition position = DialogPosition.MainMenu, int questionAmount = 1, bool questionReady = true)
        {
            Id = id;
            Position = position;
            QuestionAmount = questionAmount;
            QuestionReady = questionReady;
        }

        public void DecrementQuestion(DataBaseInteractions dbInteract)
        {
            QuestionAmount--;
            dbInteract.UpdateUser(this);
        }
        public void AddQuestions(DataBaseInteractions dbInteract, byte amount)
        {
            QuestionAmount += amount;
            dbInteract.UpdateUser(this);
        }


        public void SetReady(DataBaseInteractions dbInteract, bool ready)
        {
            QuestionReady = ready;
            dbInteract.UpdateUser(this);
        }

        public void SetPosition(DataBaseInteractions dbInteract, DialogPosition position)
        {
            Position = position;
            dbInteract.UpdateUser(this);
        }
    }

    internal class Teacher
    {
        public long Id { get; private set; }
        public decimal Balance { get; private set; }
        public DialogPosition Position { get; private set; }

        public Teacher(long id, decimal balance, DialogPosition position)
        {
            Id = id;
            Balance = balance;
            Position = position;
        }

        public void AddBalance(DataBaseInteractions dbInteract, decimal money)
        {
            Balance += money;
            dbInteract.UpdateTeacher(this);
        }

        public void SetPosition(DataBaseInteractions dbInteract, DialogPosition position)
        {
            Position = position;
            dbInteract.UpdateTeacher(this);
        }
    }

    internal enum DialogPosition
    {
        MainMenu,
        ChooseWorkId,
        WorkShown,
        ShopAmount,
        ProfileShown,
        TeacherMainMenu,
        TeacherWorkCheckComment,
        TeacherWorkCheckRate,
        AnswerTypeSelect,
        WaitingForResponce,
    }
}
