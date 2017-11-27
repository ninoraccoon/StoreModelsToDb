using System;
using System.Collections.Generic;
using EAGetMail;

namespace StoreModelsToDb.Tools
{
    class EAGetMailMailApi:IMailApi
    {
        private MailServer oServer;
        private MailClient oClient;
        public List<MailMessage> Messages { get; set; }
        public bool IsConnected { get; set; }
        public MailProtocol mprotocol { get; set; }
        public long lastMailCheck { get; set; }

        public EAGetMailMailApi(MailProtocol ppMailProtocol, int lastMail = 0)
        {
            mprotocol = ppMailProtocol;
            lastMailCheck = lastMail;
            Messages = new List<MailMessage>();
        }

        public void Connect(string server, string User, string pass, int port, bool useSSl)
        {
            oServer = new MailServer(server,User,pass,useSSl,ServerAuthType.AuthLogin, mprotocol==MailProtocol.pop3?ServerProtocol.Pop3 : ServerProtocol.Imap4);
            oClient = new MailClient("TryIt");
            oServer.Port = port;
            oClient.Connect(oServer);

        }

        public void Disconnect()
        {
            oClient.Close();
        }

        public int GetMessagesCount()
        {
            throw new NotImplementedException();
        }

        public void LoadMessages()
        {
            throw new NotImplementedException();
        }

        public void LoadMessages(string start, string end)
        {
            throw new NotImplementedException();
        }

        public void LoadRecentMessages(int lastSequenceNumber)
        {
            if (lastSequenceNumber != 0 && lastSequenceNumber != null)
                lastMailCheck = lastSequenceNumber;

            MailInfo[] infos = oClient.GetMailInfos();
           
            for (int i = 0; i < infos.Length; i++)
            {
                MailInfo info = infos[i];
                if (info.Index > lastMailCheck)
                {
                    Mail mail = oClient.GetMail(info);
                    MailMessage message = new MailMessage()
                    {
                        Id = info.Index,
                        RecibedTime = mail.SentDate.Date,
                        SendBy = mail.From.Name + "-" + mail.From.Address,
                        Subject = mail.Subject,
                    };
                    foreach (var mailAttachment in mail.Attachments)
                    {
                        message.Attachments.Add(new MailAtachment()
                        {
                            file = mailAttachment.Name,
                            data = mailAttachment.Content
                        });
                    }
                   Messages.Add(message);
                }
                if (lastMailCheck < info.Index)
                {
                    lastMailCheck = info.Index;
                }
            }
            
            }

        public void SetCurrentFolder(string folder)
        {
            ;
        }
    }
}
