using System;
using System.Collections.Generic;

namespace StoreModelsToDb.Tools
{
    public enum MailProtocol
    {
        imap = 0,
        pop3 = 1
    }
    public interface IMailApi
    {
        List<MailMessage> Messages { get; set; }
        bool IsConnected { get; set; }
        MailProtocol mprotocol { get; set; }
        long lastMailCheck { get; set; }
        void Connect(string server, string User, string pass, int port, bool useSSl);
        void Disconnect();
        int GetMessagesCount();
        void LoadMessages();
        void LoadMessages(string start, string end);
        void LoadRecentMessages(int lastSequenceNumber);
        void SetCurrentFolder(string folder);
    }

    public  class MailAtachment
    {
        public string file { get; set; }
        public byte[] data { get; set; }
    }
    public class MailMessage
    {
        public string SendBy { get ; set; }
        public DateTime RecibedTime { get; set; }
        public long Id { get; set; }
        public string Subject { get; set; }
        public List<MailAtachment> Attachments { get; set; }

        public MailMessage()
        {
            Attachments = new List<MailAtachment>();
        }

    }
}
