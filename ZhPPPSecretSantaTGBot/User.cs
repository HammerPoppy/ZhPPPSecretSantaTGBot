using System;
using System.IO;

namespace ZhPPPSecretSantaTGBot
{
    [Serializable]
    public class User
    {
        public int Id;
        public string Username;
        public string FirstName;
        public string LastName;
        public States State;
        public Stages Stage;

        public string OfficialName;
        public string Phone;
        public string Post;
        public string FanOf;
        public string Ban;

        public int TargetId;

        public User()
        {
        }

        public User(int id, string username, string firstName, string lastName)
        {
            Id = id;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            State = States.NewUser;
        }
    }

    public enum States
    {
        NewUser,
        RegistrationStarted,
        RegistrationCompleted,
        TargetChosen,
        TargetSent
    }
    
    public enum Stages
    {
        None,
        StageOffName,
        StagePhone,
        StagePost,
        StageFan,
        StageBan
    }
}