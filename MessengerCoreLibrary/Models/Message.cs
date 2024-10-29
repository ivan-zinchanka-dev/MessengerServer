using System;

namespace MessengerCoreLibrary.Models
{
    public class Message
    {
        public string SenderNickname { get; set; }
        public string ReceiverNickname { get; set; }
        public string Text { get; set; }
        public DateTime PostDateTime { get; set; }
    
        public override string ToString()
        {
            return Text;
        }
    }
}