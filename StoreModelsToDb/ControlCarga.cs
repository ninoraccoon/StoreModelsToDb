using System;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using System.IO.Compression;
using Microsoft.SqlServer.Server;
using StoreModelsToDb.Control;
using StoreModelsToDb.Tools;
using System.Security.Cryptography;


namespace StoreModelsToDb
{
    public class ControlCarga
    {

        private static readonly byte[] initVectorBytes = Encoding.ASCII.GetBytes("tu89geji340t89u2");
        public static string idGlobalOperation;
        private const int keysize = 256;
        [SqlProcedure]
        public static Int32 existsConWithMailServer() {
            string serverName, userMail, mailPass, mailProtocol, subjectLike, fildToexclude,tempDir;
            int port, lastId;
            GetConfig(out serverName, out userMail, out mailPass, out mailProtocol, out port, out subjectLike, out lastId, out fildToexclude, out tempDir);
            IMailApi mailapi = new EAGetMailMailApi(mailProtocol.ToLower() == "pop3" ? MailProtocol.pop3 : MailProtocol.imap, lastId);
            try
            {
                mailapi.Connect(serverName,userMail,mailPass,port,false);
                mailapi.Disconnect();
                return 1;
            }
            catch
            {
                return 0;
            }
        }
        [SqlProcedure]
        public static void UpModels()
        {
          
                SqlConnection mcon = new SqlConnection("context connection=true");
                try
                {
               
                idGlobalOperation = DateTime.Now.ToString() + "-" + (new Random(DateTime.Now.Second)).Next().ToString();
                Evento.IdProces = idGlobalOperation;
                Evento.Pcon = mcon;
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Inicio del proceso de carga de Modelos");
                Evento.Save();
                string serverName, userMail, mailPass, mailProtocol, subjectLike, fildToexclude, tempDirectory;
                int port, lastId;
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Obteniendo configuracion");
                Evento.Save();
                GetConfig(out serverName, out userMail, out mailPass, out mailProtocol, out port, out subjectLike, out lastId, out fildToexclude, out tempDirectory);
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Comienzo del proceso de chequeo de mensajes en servidor:" + serverName + " usuario:" + userMail
                     + " que contegan asunto:" + subjectLike + " desde el elemento:" + lastId.ToString());
                Evento.Save();
                // GetConfig();


                ControlModel cm = new ControlModel(fildToexclude);
                IMailApi mailapi = new EAGetMailMailApi(mailProtocol.ToLower()=="pop3"?MailProtocol.pop3 : MailProtocol.imap,lastId);
                cm.ChechMail(mailapi, serverName, userMail, mailPass, port, subjectLike);
                mailapi.Disconnect();
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Cargando Mensajes al Servidor de datos");
                Evento.Save();
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Se detectaron:" + cm.PendToUp.Count.ToString() + " modelos a subir.");
                Evento.Save();
                int sequecenumber = 0;
                bool firstBnadera = false;
                while (cm.PendToUp.Count > 0)
                {

                    Modelo vmodel = cm.PendToUp.Dequeue();
                    Evento.SetEvento(DateTime.Now, DateTime.Now, "Subiendo modelo:" + vmodel.Mmodelo + " de " + vmodel.SenderEmail + " mensaje id"
                        + vmodel.IdOrden.ToString() + " fecha de modelo " + vmodel.Fecha.ToString());
                    Evento.Save();
                    //insertamos el modelos y actulizamos la ultima secuencia para tenr que revisarlo nuevamente
                    InsertModel(vmodel, mcon);
                    Evento.SetEvento(DateTime.Now, DateTime.Now, "Modelo Subido exitosamente");
                    Evento.Save();
                    //---------------------------------------------------
                    Evento.SetEvento(DateTime.Now, DateTime.Now, "actulizando secuencia");
                    Evento.Save();
                    //cuando se completa un mensaje se actuliza para no proecasrlo mas

                    if (sequecenumber < vmodel.IdMensaje)
                    {
                        sequecenumber = vmodel.IdMensaje;
                        SetLastSequenceEmail(sequecenumber, mcon);
                    }
                   
                    Evento.SetEvento(DateTime.Now, DateTime.Now, "secuencia actulizada");
                    Evento.Save();
                }
                Evento.SetEvento(DateTime.Now, DateTime.Now, "chequeando secuencia");
                Evento.Save();
                if (sequecenumber < mailapi.lastMailCheck && mailapi.Messages.Count>0)
                {
                    sequecenumber = (int)mailapi.lastMailCheck;
                    SetLastSequenceEmail(sequecenumber, mcon);
                    Evento.SetEvento(DateTime.Now, DateTime.Now, "secuencia corregida");
                    Evento.Save();
                }
                Evento.SetEvento(DateTime.Now, DateTime.Now, "Fin del proceso de carga de Modelos");
                Evento.Save();
            }
            catch (Exception ex) {
                Evento.SetEvento(DateTime.Now, DateTime.Now, "!!!Algo ha pasado!!!, detalles:" + ex.Message + ". Desde " + ex.Source + ". Traza" + ex.StackTrace );
                Evento.Save();
            }
            
        }
        public static void InsertModel(Modelo pmodelo, SqlConnection pcon)
        {
            SqlCommand com = pcon.CreateCommand();
           
            com.CommandText = "INSERT INTO [dbo].[O_ModeloCarga] ([idOrden],[modelo],[fechaModelo],[cuerpo],[esquema]) VALUES (@id,@pmodelo,@pfechaModelo,@pcuerpo,@pesquema)";
            com.CommandType = System.Data.CommandType.Text;
            com.Parameters.Add("@id", System.Data.SqlDbType.Int).Value = pmodelo.IdOrden;
            com.Parameters.Add("@pmodelo",System.Data.SqlDbType.VarChar,15).Value = pmodelo.Mmodelo;
            com.Parameters.Add("@pfechaModelo", System.Data.SqlDbType.DateTime).Value = pmodelo.Fecha;
            com.Parameters.Add("@pcuerpo", System.Data.SqlDbType.VarChar).Value = pmodelo.getCuerpoToXml();
            com.Parameters.Add("@pesquema", System.Data.SqlDbType.VarChar).Value = pmodelo.getSquema();
            pcon.Open();
            SqlContext.Pipe.ExecuteAndSend(com);
            pcon.Close();
        }
        public static void SetLastSequenceEmail(int plastSquence,SqlConnection pcon){
            SqlCommand com = pcon.CreateCommand();
            com.CommandType = System.Data.CommandType.Text;
            com.CommandText = "update O_ModeloConfig set lastEmailSquence = @plastSquence";
            com.Parameters.Add("@plastSquence", System.Data.SqlDbType.Int).Value = plastSquence;
            pcon.Open();
            SqlContext.Pipe.ExecuteAndSend(com);
            pcon.Close();
        }
        public static string Encrypt(string plainText, string passPhrase)
        {
             
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                byte[] cipherTextBytes = memoryStream.ToArray();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }
         public static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using(RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using(ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                    {
                        using(MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using(CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }
         [SqlProcedure]
         public static void SetConfig(string pmailServer, string pmailuser, string pmailPass, string pmailProtocol, int pmailPort, string pemailSubjectLike, string pfildToexclude, string pTempDirectory)
         {
             string vpmailServer,  vpmailuser,  vpmailPass,  vpmailProtocol, vpemailSubjectLike,  vpfildToexclude, vtempDirectory;
             int vpmailPort,lastMailSequence;
             GetConfig(out vpmailServer, out  vpmailuser, out  vpmailPass, out  vpmailProtocol, out  vpmailPort,out vpemailSubjectLike, out lastMailSequence, out  vpfildToexclude, out vtempDirectory);
             if (String.IsNullOrEmpty(pmailServer))
                 pmailServer = vpmailServer;
             if (String.IsNullOrEmpty(pmailuser))
                 pmailuser = vpmailuser;
             if (String.IsNullOrEmpty(pmailPass))
                 pmailPass = vpmailPass;
             if (String.IsNullOrEmpty(pmailProtocol))
                 pmailProtocol = vpmailProtocol;
             if (pmailPort ==0)
                 pmailPort = vpmailPort;
             if (String.IsNullOrEmpty(pemailSubjectLike))
                 pemailSubjectLike = vpemailSubjectLike;
             if (String.IsNullOrEmpty(pfildToexclude))
                 pfildToexclude = vpfildToexclude;
            if (String.IsNullOrEmpty(pTempDirectory))
                pTempDirectory = vtempDirectory;
            SqlConnection mcon = new SqlConnection("context connection=true");
             SqlCommand com = mcon.CreateCommand();
                com.CommandType = System.Data.CommandType.Text;
                com.CommandText = "delete from O_ModeloConfig";
                mcon.Open();
                SqlContext.Pipe.ExecuteAndSend(com);
                mcon.Close();
             //insertamos
                com = mcon.CreateCommand();
                com.CommandType = System.Data.CommandType.Text;
                com.CommandText = "insert into O_ModeloConfig ([emailServer] ,[emailUser] ,[emailPass] ,[emailProtocol] ,[emailPort], [emailSubjectLike], [excludeFieldFromModel], [tempDirectory]) values (@pmailServer,@pmailuser,@pmailPass,@pmailProtocol,@pmailPort,@pemailSubjectLike,@pfildToexclude,@ptempDirectory)";
                com.Parameters.Add("@pmailServer", System.Data.SqlDbType.VarChar, 100).Value = Encrypt(pmailServer, "jklhajkshd56673gd8/*#GL@");
                com.Parameters.Add("@pmailuser", System.Data.SqlDbType.VarChar, 100).Value = Encrypt(pmailuser, "jklhajkshd56673gd8/*#GL@");
                com.Parameters.Add("@pmailPass", System.Data.SqlDbType.VarChar, 50).Value = Encrypt(pmailPass, "jklhajkshd56673gd8/*#GL@");
                com.Parameters.Add("@pmailProtocol", System.Data.SqlDbType.VarChar, 100).Value = Encrypt(pmailProtocol, "jklhajkshd56673gd8/*#GL@");
                com.Parameters.Add("@pmailPort", System.Data.SqlDbType.Int).Value = pmailPort;
                com.Parameters.Add("@pemailSubjectLike", System.Data.SqlDbType.VarChar, 2000).Value = Encrypt(pemailSubjectLike, "jklhajkshd56673gd8/*#GL@");
                com.Parameters.Add("@pfildToexclude", System.Data.SqlDbType.VarChar, 2000).Value = Encrypt(pfildToexclude, "jklhajkshd56673gd8/*#GL@");
                com.Parameters.Add("@ptempDirectory", System.Data.SqlDbType.VarChar, 2000).Value = Encrypt(pTempDirectory, "jklhajkshd56673gd8/*#GL@");
            mcon.Open();
                SqlContext.Pipe.ExecuteAndSend(com);
                mcon.Close();    
               // com.CommandText = "update O_ModeloConfig set emailServer = @pmailServer, emailUser=@pemailUser, emailUser=@pemailUser, emailProtocol = @pemailProtocol, emailPort=@pemailPort";
                     
         }
        [SqlProcedure]
         public static void GetConfig(out string pmailServer, out string pmailuser, out string pmailPass, out string pmailProtocol, out int pmailPort, out string emailSubjectLike, out int lastEmailSquence,out string fildToexclude, out string tempDirectory)
         {
             SqlConnection mcon = new SqlConnection("context connection=true");
             SqlCommand com = mcon.CreateCommand();
             com.CommandType = System.Data.CommandType.Text;
             com.CommandText = "select * from O_ModeloConfig";
             mcon.Open();
             SqlDataReader msqlr = com.ExecuteReader();
             if (msqlr.HasRows)
             {
                 msqlr.Read();
                 pmailServer = Decrypt((string) msqlr[0], "jklhajkshd56673gd8/*#GL@");
                 pmailuser = Decrypt((string)msqlr[1], "jklhajkshd56673gd8/*#GL@");
                // pmailuser = "";
                 pmailPass = Decrypt((string)msqlr[2], "jklhajkshd56673gd8/*#GL@");
                // pmailPass = (string)msqlr[2];
                // pmailPass = "";
                 pmailProtocol = Decrypt((string)msqlr[3], "jklhajkshd56673gd8/*#GL@");
                // pmailProtocol = "";
                 pmailPort = (int)msqlr[4];
                 emailSubjectLike = Decrypt((string)msqlr[5], "jklhajkshd56673gd8/*#GL@");
                // if msqlr[6]
                 lastEmailSquence = (int)msqlr[6];
                 fildToexclude = Decrypt((string)msqlr[7], "jklhajkshd56673gd8/*#GL@");
                 tempDirectory = Decrypt((string)msqlr[8], "jklhajkshd56673gd8/*#GL@");
                // emailSubjectLike = "";

            }
             else
             { 
                 pmailServer ="";
                 pmailuser = "";
                 pmailPass = "";
                 pmailProtocol = "";
                 pmailPort = 0;
                 emailSubjectLike = "";
                 lastEmailSquence = 0;
                 fildToexclude = "";
                 tempDirectory = "";
             }
             msqlr.Close();
             mcon.Close();
         }

        [SqlProcedure]
        public static void CompressStringToFile(string pcompressString, string dirpath = null, string fileName = null)
        {
            if (String.IsNullOrEmpty(dirpath))
            {
                string serverName, userMail, mailPass, mailProtocol, subjectLike, fildToexclude, tempDir;
                int port, lastId;
                GetConfig(out serverName, out userMail, out mailPass, out mailProtocol, out port, out subjectLike, out lastId, out fildToexclude, out tempDir);
                dirpath = tempDir;
            }
            byte[] bytesTocompress = new byte[pcompressString.Length];
            int cont = 0;
            foreach (char c in pcompressString)
            {
                bytesTocompress[cont] = (byte) c;
                cont++;
            }
            bytesTocompress = Compress(bytesTocompress);
            if (dirpath.Substring(dirpath.Length - 1, 1) != @"\")
                dirpath = dirpath + @"\";
            FileStream destFileStream = File.Create(dirpath + fileName);
            destFileStream.Write(bytesTocompress,0,bytesTocompress.Length);
            destFileStream.Close();

        }
        public static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();

            GZipStream gzip = new GZipStream(output, CompressionMode.Compress, true);
            gzip.Write(data, 0, data.Length);
            gzip.Close();

            return output.ToArray();
        }

    }

  }
