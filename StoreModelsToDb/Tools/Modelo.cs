using System;
using System.Data;
using System.Collections.Generic;
//using System.Linq;

namespace StoreModelsToDb.Tools
{

    public class Modelo
    {
        private static Dictionary<Type, string> Mappings;
        int idOrden;
        int idMensaje;

        public int IdMensaje
        {
            get { return idMensaje; }
            set { idMensaje = value; }
        }

        public int IdOrden
        {
            get { return idOrden; }
            set { idOrden = value; }
        }
        string mmodelo;

        public string Mmodelo
        {
            get { return mmodelo; }
            set { mmodelo = value; }
        }
        DateTime fecha;

        public DateTime Fecha
        {
            get { return fecha; }
            set { fecha = value; }
        }
        DataTable cuerpo;

        string senderEmail;

        public string SenderEmail
        {
            get { return senderEmail; }
            set { senderEmail = value; }
        }
        string filePath;

        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }
        DateTime fechaEmail;

        public DateTime FechaEmail
        {
            get { return fechaEmail; }
            set { fechaEmail = value; }
        }
        DateTime fechaProcesado;

        public DateTime FechaProcesado
        {
            get { return fechaProcesado; }
            set { fechaProcesado = value; }
        }
        string error;

        public string Error
        {
            get { return error; }
            set { error = value; }
        }
        public void loadTypesMapping(){
             Mappings = new Dictionary<Type, string>();

            Mappings.Add(typeof(double), "float");
            Mappings.Add(typeof(string), "varchar(max)");
            //Mappings.Add(typeof(String), "varchar(1000)");
            Mappings.Add(typeof(DateTime), "datetime");
            Mappings.Add(typeof(int), "int");
            Mappings.Add(typeof(Int64), "bigint");
            Mappings.Add(typeof(decimal), "decimal(16,2)"); 
        }
        public Modelo(string vmodelo, DateTime vfecha, DataTable vcuerpo)
        {
            mmodelo = vmodelo;
            fecha = vfecha;
            cuerpo = vcuerpo;
            loadTypesMapping();


            //Mappings.Add("bigint", typeof(Int64));
            //Mappings.Add("binary", typeof(Byte[]));
            //Mappings.Add("bit", typeof(Boolean));
            //Mappings.Add("char", typeof(String));
            //Mappings.Add("date", typeof(DateTime));
            //Mappings.Add("datetime", typeof(DateTime));
            //Mappings.Add("datetime2", typeof(DateTime));
            //Mappings.Add("datetimeoffset", typeof(DateTimeOffset));
            //Mappings.Add("decimal", typeof(Decimal));
            //Mappings.Add("float", typeof(Double));
            //Mappings.Add("image", typeof(Byte[]));
            //Mappings.Add("int", typeof(Int32));
            //Mappings.Add("money", typeof(Decimal));
            //Mappings.Add("nchar", typeof(String));
            //Mappings.Add("ntext", typeof(String));
            //Mappings.Add("numeric", typeof(Decimal));
            //Mappings.Add("nvarchar", typeof(String));
            //Mappings.Add("real", typeof(Single));
            //Mappings.Add("rowversion", typeof(Byte[]));
            //Mappings.Add("smalldatetime", typeof(DateTime));
            //Mappings.Add("smallint", typeof(Int16));
            //Mappings.Add("smallmoney", typeof(Decimal));
            //Mappings.Add("text", typeof(String));
            //Mappings.Add("time", typeof(TimeSpan));
            //Mappings.Add("timestamp", typeof(Byte[]));
            //Mappings.Add("tinyint", typeof(Byte));
            //Mappings.Add("uniqueidentifier", typeof(Guid));
            //Mappings.Add("varbinary", typeof(Byte[]));
            //Mappings.Add("varchar", typeof(String));
        }
        public Modelo(DataTable vcuerpo, string fildsToExclude)
        {
            loadTypesMapping();
            //vamos a identificar las columnas
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Reestructurando...");
            Evento.Save();
            int indexCodMod = -1;
            int indexFecModelo = -1;
            
            for (int i = 0; i < vcuerpo.Columns.Count; i++)
            {
                if (vcuerpo.Columns[i].ColumnName.ToLower() == "cod_modelo")
                {
                    indexCodMod = i;
                }
                if (vcuerpo.Columns[i].ColumnName.ToLower().Trim() == "fec_modelo")
                {
                    indexFecModelo = i;
                }
            }
            if (indexCodMod< 0 || indexFecModelo < 0)
                throw new Exception("Modelo debe tener codigo(cod_modelo) y fecha(fec_modelo)");
           
            if (indexCodMod >= 0)
            {
                mmodelo = vcuerpo.Rows[0][indexCodMod].ToString();
               
            }
            //Evento.SetEvento(DateTime.Now, DateTime.Now, fecha.ToString() + vcuerpo.Rows[0][vcuerpo.Columns.IndexOf("Fec_modelo")].ToString());
            //Evento.Save();
            //Evento.SetEvento(DateTime.Now, DateTime.Now, fecha.ToString() + vcuerpo.Rows[0][vcuerpo.Columns.IndexOf("fec_modelo")].ToString());
            //Evento.Save();
            if (indexFecModelo >= 0)
            {
                try
                {
                   
                    fecha = DateTime.Parse(vcuerpo.Rows[0][indexFecModelo].ToString());
                   
                }
                catch (Exception ex)
                {

                    double d = double.Parse(vcuerpo.Rows[0][indexFecModelo].ToString());
                    fecha = DateTime.FromOADate(d);
                }
            }
            if (indexCodMod>=0)
            {
                vcuerpo.Columns.RemoveAt(indexCodMod);
            }
            for (int i = 0; i < vcuerpo.Columns.Count; i++)
            {
                if (vcuerpo.Columns[i].ColumnName.ToLower().Trim() == "fec_modelo")
                {
                    indexFecModelo = i;
                }
            }
            if (indexFecModelo >= 0)
            {
                vcuerpo.Columns.RemoveAt(indexFecModelo);
            }
            if (!String.IsNullOrEmpty(fildsToExclude))
            {
                string vcolumnToDel = "";
                while (fildsToExclude.Length>0)
                {
                    vcolumnToDel = fildsToExclude.Substring(0, ((fildsToExclude.IndexOf(",") < 0) ? fildsToExclude.Length : fildsToExclude.IndexOf(",")));
                    fildsToExclude = fildsToExclude.Substring(((fildsToExclude.IndexOf(",") < 0) ? fildsToExclude.Length : fildsToExclude.IndexOf(",") + 1));
                    if (vcuerpo.Columns.IndexOf(vcolumnToDel) >= 0)
                    {
                        // mmodelo = vcuerpo.Rows[0][vcuerpo.Columns.IndexOf("cod_institut")].ToString();
                        vcuerpo.Columns.Remove(vcolumnToDel);
                    }
                }
            }
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Se identifico modelo:" + mmodelo + ", fecha del modelo: " + fecha.ToString());
            Evento.Save();
            cuerpo = vcuerpo;
            cuerpo.TableName = "modelo";
           // getCuerpoToXml();
        }
        public string getCuerpoToXml() {
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseando a XML");
            Evento.Save();
            String xmlCuerpo = "";
            //System.IO.StringWriter writer = new System.IO.StringWriter();
            //cuerpo.WriteXml(writer, XmlWriteMode.IgnoreSchema,false );
            //xmlCuerpo = writer.ToString();
           // System.Windows.Forms.MessageBox.Show(xmlCuerpo);
            xmlCuerpo = xmlCuerpo + "<modelo>";
            foreach (DataRow  item in cuerpo.Rows)
            {
                xmlCuerpo += "<row ";
                for (int i = 0; i < cuerpo.Columns.Count; i++)
                {
                    if (cuerpo.Columns[i].ColumnName.Substring(0, cuerpo.Columns[i].ColumnName.Length<6? cuerpo.Columns[i].ColumnName.Length:6).ToLower() != "column")
                    {
                        xmlCuerpo += cuerpo.Columns[i].ColumnName.ToLower() + "=" + @"""" +
                                     ((cuerpo.Columns[i].DataType == typeof(decimal))
                                         ? item[i].ToString().Replace(",", ".").Trim()
                                         : item[i].ToString().Replace("<", "&lt;").Trim()
                                             .Replace(">", "&gt;").Trim()
                                             .Replace(@"""", "&quot;").Trim()
                                             .Replace(@"""", "&quot;").Trim())
                                     + @"""" + " ";
                    }
                }
                xmlCuerpo += "/>";
            }
             xmlCuerpo = xmlCuerpo + "</modelo>";
             Evento.SetEvento(DateTime.Now, DateTime.Now, "Parseando a XML Exitoso");
             Evento.Save();
            return xmlCuerpo;
        }
        public string getSquema()
        {
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Obteniendo esquema");
            Evento.Save();
            string squema = "";
            for (int i = 0; i < cuerpo.Columns.Count ; i++)
            {
                if (cuerpo.Columns[i].ColumnName.Substring(0, cuerpo.Columns[i].ColumnName.Length < 6 ? cuerpo.Columns[i].ColumnName.Length : 6).ToLower() != "column")
                    squema += "!@!" + ".value('@" + cuerpo.Columns[i].ColumnName.ToLower() + "','" + ToClrType(cuerpo.Columns[i].DataType ) + "') as " + cuerpo.Columns[i].ColumnName.ToLower() + ",";
            }
            Evento.SetEvento(DateTime.Now, DateTime.Now, "Esquema exitoso");
            Evento.Save();
            return squema.Substring(0, squema.Length - 1);
        }
        public static string ToClrType(Type sqlType)
        {
            string datatype = "";
            if (Mappings.TryGetValue(sqlType, out datatype))
                return datatype;
            throw new TypeLoadException(string.Format("Can not load CLR Type from {0}", sqlType));
        }
    }
}
