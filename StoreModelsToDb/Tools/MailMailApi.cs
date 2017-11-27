using System;
using System.Collections.Generic;
using System.Text;
using Limilabs.Client.IMAP;
using Limilabs.Mail;
using LumiSoft.Net.IMAP;

namespace StoreModelsToDb.Tools
{
    public class MailMailApi : IMailApi
    {
        public List<MailMessage> Messages { get; set; }
        private Imap imap;

        public long lastMailCheck { get; set; }

        public MailMailApi()
        {
            imap = new Imap();
            Messages = new List<MailMessage>();
        }

        public MailMailApi(MailProtocol ppMailProtocol, int lastMail = 0)
        {
            mprotocol = ppMailProtocol;
            lastMailCheck = lastMail;
            Messages = new List<MailMessage>();
            if (mprotocol == MailProtocol.imap)
                imap = new Imap();
        }

        public bool IsConnected { get; set; }
        public MailProtocol mprotocol { get; set; }

        public void Connect(string _server, string _user, string _password, int port, bool useSSl)
        {
            if (mprotocol == MailProtocol.imap)
            {
                imap.Connect(_server, port, useSSl);
                imap.Login(_user, _password);
                IsConnected = true;
            }
        }

        public void Disconnect()
        {
            if (mprotocol == MailProtocol.imap)
            {
                IsConnected = false;
                imap.Close();
            }
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
            Messages.Clear();

            List<long> uids;

            if (lastSequenceNumber != 0 && lastSequenceNumber != null)
                lastMailCheck = lastSequenceNumber;

            if (lastMailCheck == 0 || lastMailCheck == null)
            {
                uids = imap.GetAll();
            }
            else
            {
                uids = imap.Search().Where(
                    Expression.UID(Range.From(lastMailCheck)));
                uids.Remove(lastMailCheck);
            }
           
            uids.ForEach(obj =>
            {
                
                if (lastMailCheck<obj)
                {
                    lastMailCheck = obj;
                }
                var eml = imap.GetMessageByUID(obj);
                IMail imail = new MailBuilder().CreateFromEml(eml);
                MailMessage mail = new MailMessage()
                {
                    RecibedTime = (DateTime) (imail.Date??DateTime.MinValue),
                    Id = obj,
                    SendBy = imail.From[0].Name + "-" + imail.From[0].Address,
                    Subject = imail.Subject
                };
               
                imail.Attachments.ForEach(objAttach =>
                    {
                        mail.Attachments.Add(new MailAtachment()
                        {
                            data = objAttach.Data,
                            file = objAttach.FileName
                        });
                    }
                );
                Messages.Add(mail);
            });
            

        }

        public void SetCurrentFolder(string folder)
        {
            if (mprotocol == MailProtocol.imap)
            {
                imap.SelectInbox();
            }
        }
    }
}