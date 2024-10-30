using System;

namespace MessengerCoreLibrary.Models
{
    public class Message
    {
        public string SenderNickname { get; private set; }
        public string ReceiverNickname { get; private set; }
        public string Text { get; private set; }
        public DateTime PostDateTime { get; private set; }
    
        public override string ToString()
        {
            return Text;
        }

        public Message(string senderNickname, string receiverNickname, string text, DateTime postDateTime)
        {
            SenderNickname = senderNickname;
            ReceiverNickname = receiverNickname;
            Text = text;
            PostDateTime = postDateTime;
        }
    }
}