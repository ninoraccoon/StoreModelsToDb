using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Xml;
using StoreModelsToDb.Tools;

namespace StoreModelsToDb.Control
{
    public class ControlModel
    {
        Queue<Modelo> pendToUp;
        string fildtoExclude = "";

        public Queue<Modelo> PendToUp
        {
            get { return pendToUp; }
            set { pendToUp = value; }
        }
       
        public ControlModel() {
            pendToUp = new Queue<Modelo>();
        }
        public ControlModel(string vfildToexclude)
        {
            pendToUp = new Queue<Modelo>();
            fildtoExclude = vfildToexclude;
        }
        public void ChechMail(IMailApi mailApi,string serverName, string userMail, string mailPass, int port, string subjectLike)
        {
            
            Evento.SetEvento( DateTime.Now, DateTime.Now, "Intentado conectar con el servido de correo:" + serverName + " usuario:" +userMail
                + " que contegan asunto:" + subjectLike + "desde el elemento:" + mailApi.lastMailCheck.ToString());
            Evento.Save();
            mailApi.Connect(serverName, userMail, mailPass, port, false);
            mailApi.SetCurrentFolder("INBOX");
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Conectado al servido de correo:" + serverName + " usuario:" + userMail
                + " que contegan asunto:" + subjectLike + "desde el elemento:" + mailApi.lastMailCheck.ToString());
            Evento.Save();
           
            mailApi.LoadRecentMessages(0);
          
            int errorSequenceId = 0;
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Se detectaron " + mailApi.Messages.Count.ToString() + " mensajes a procesar.");
            Evento.Save();
            // To read all my Messages loaded:
            mailApi.Messages.ForEach(message =>
            {
                try
                {
                    if (message.Subject.Contains(subjectLike))
                    {
                     
                        Evento.SetEvento(DateTime.Now, DateTime.Now, "Se encontro el mensaje: " + message.Id.ToString()
                       + " de remitente " + message.Subject + " con asunto " + message.Subject
                       + " recivido " + message.RecibedTime.ToString() + " cantidad de adjuntos " + message.Attachments.Count);
                        Evento.Save();
                        int adjuntoIndex = 0;
                        foreach (var item in message.Attachments)
                        {
                            Evento.SetEvento(DateTime.Now, DateTime.Now, "Procesado adjunto  " + item.file);
                            Evento.Save();
                            Modelo mModelo = new Modelo(this.ProcesAttachment(item), fildtoExclude);

                            mModelo.IdOrden = int.Parse(message.Id.ToString() + adjuntoIndex.ToString());
                            mModelo.IdMensaje = (int)message.Id;
                            mModelo.FechaEmail = message.RecibedTime;
                            mModelo.SenderEmail = message.SendBy;
                            mModelo.FechaProcesado = DateTime.Now;
                            pendToUp.Enqueue(mModelo);
                            adjuntoIndex++;
                            Evento.SetEvento(DateTime.Now, DateTime.Now, "Adjunto " + item.file + " procesado correctamente.");
                            Evento.Save();
                        }
                        Evento.SetEvento(DateTime.Now, DateTime.Now, "Fin del procesameinto del mensaje: " + message.Id.ToString());
                        Evento.Save();
                    }
                }
                catch (Exception e)
                {
                    errorSequenceId = (int)message.Id;
                    Evento.SetEvento(DateTime.Now, DateTime.Now, "No se pudo procesar mensaje" + message.Id.ToString() + ". Error:" + e.Message);
                    Evento.Save();
                }
                if (errorSequenceId != 0)
                {
                    mailApi.LoadRecentMessages((int)(message.Id + (long)1));
                    errorSequenceId = 0;
                }
            });
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Fin del procesamiento de mensajes");
            Evento.Save();
        }
        public void ProcesMail()
        {

        }
        public DataTable  ProcesAttachment(MailAtachment atachFile)
        {
            DataTable atachData = null;
           // System.Windows.Forms.MessageBox.Show(atachFile.Text);
            if (atachFile.file.Substring(atachFile.file.Length -3,3).ToLower() == "dbf")
            {
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseando DBF");
                Evento.Save();
                atachData = ParseDBF.ReadDBF(atachFile.data);
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseado Exitoso");
                Evento.Save();
               //System.Windows.Forms.MessageBox.Show(atachData.Columns.Count.ToString() + "dbf");
            }
            if (atachFile.file.Substring(atachFile.file.Length - 3, 3).ToLower() == "xls" || atachFile.file.Substring(atachFile.file.Length - 4, 4).ToLower() == "xlsx")
            {
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseando Excel");
                Evento.Save();
                ExcelDataReader.ExcelDataReader rd;
                rd = new ExcelDataReader.ExcelDataReader(atachFile.data);
                atachData = rd.TruColumnsTransform(rd.WorkbookData.Tables[0]);
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseado Exitoso");
                Evento.Save();
               // System.Windows.Forms.MessageBox.Show(atachData.Columns.Count.ToString() + "Excel");
            }
            if (atachFile.file.Substring(atachFile.file.Length - 3, 3).ToLower() == "xml")
            {
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseando Xml");
                Evento.Save();
                atachData = ParseXml(atachFile.data);
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseado Exitoso");
                Evento.Save();
            }
            if (atachFile.file.Substring(atachFile.file.Length - 4, 4).ToLower() == "gzip")
            {
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Descomprimiendo");
                Evento.Save();
                MemoryStream input = new MemoryStream();
                input.Write(atachFile.data, 0, atachFile.data.Length);
                input.Seek(0, SeekOrigin.Begin);
                GZipStream gzip = new GZipStream(input, CompressionMode.Decompress, true);
                MemoryStream output = new MemoryStream();

                byte[] buff = new byte[64];
                int read = -1;
                read = gzip.Read(buff, 0, buff.Length);
               
                while (read > 0)
                {
                    output.Write(buff, 0, read);
                    read = gzip.Read(buff, 0, buff.Length);
                }
                gzip.Close();
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Descomprimiendo Exitoso");
                Evento.Save();
                atachData = ParseXml(output.ToArray());
            }
            return atachData;
        }

        public DataTable ParseXml(byte[] atachFile)
        {
            DataTable atachData = null;
            Stream stream = new MemoryStream(atachFile);

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                doc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                StringReader StringStream = new StringReader(stringWriter.GetStringBuilder().ToString());
                DataSet ds = new DataSet();
                ds.ReadXml(StringStream);
                atachData = ds.Tables[0];
            }
            return atachData;
        }

        public void LogOperation()
        {

        }
    }
}
