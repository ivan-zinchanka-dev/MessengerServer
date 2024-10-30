namespace MessengerCoreLibrary.Models
{
    public class User
    {
        public string Nickname { get; private set; }
        public string Password { get; private set; }

        public User(string nickname, string password)
        {
            Nickname = nickname;
            Password = password;
        }
    }
}