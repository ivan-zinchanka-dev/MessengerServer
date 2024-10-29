namespace MessengerCoreLibrary.Infrastructure
{
    public enum QueryHeader : byte
    {
        None = 0,
        SignIn = 1,
        SignUp = 2,
        PostMessage = 3,
        UpdateChat = 4,
        Quit = 10,
    }
}