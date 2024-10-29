namespace MessengerCoreLibrary.Models;

public class Message
{
    public string SenderNickname { get; init; }
    public string ReceiverNickname { get; init; }
    public string Text { get; init; }
    public DateTime PostDateTime { get; init; }
    
    public override string ToString()
    {
        return Text;
    }
}