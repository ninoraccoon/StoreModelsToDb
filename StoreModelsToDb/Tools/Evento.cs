using System;
using System.Data;
//using System.Linq;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace StoreModelsToDb.Tools
{
    public enum TipEvento { Revisar_Mail = 0, Cargar_Adjunto = 1, Porcesar_Adjunto = 2,Parse_DBF = 3, Parse_Excel = 4,Subir_Modelo = 4 }
    public static class Evento
    {
        public static string IdProces;
        static TipEvento env;
        static DateTime fechaIni;
        static DateTime fechaFin;
        static string detalle;
        static SqlConnection pcon;
        static int orden = 1;

        public static SqlConnection Pcon
        {
            get { return Evento.pcon; }
            set { Evento.pcon = value; }
        }

        public static void SetEvento(DateTime vfechaIni, DateTime vfechaFin,string vdetalle) {
            fechaIni = vfechaIni;
            fechaFin = vfechaFin;
            detalle = vdetalle;
           
            
        }
        public static void Save()
        {
            orden++;
            SqlCommand com = pcon.CreateCommand();
            com.CommandText = "INSERT INTO [O_ModeloLog] ([idProces],[fechaIni],[fechaFin],[detalle],[orden]) VALUES (@idProces,@fechaIni,@fechaFin,@detalle, @orden)";
            com.CommandType = System.Data.CommandType.Text;
            com.Parameters.Add("@idProces", System.Data.SqlDbType.VarChar,50).Value = IdProces;
            com.Parameters.Add("@fechaIni", System.Data.SqlDbType.DateTime ).Value = fechaIni;
            com.Parameters.Add("@fechaFin", System.Data.SqlDbType.DateTime).Value = fechaFin;
            com.Parameters.Add("@detalle", System.Data.SqlDbType.VarChar).Value = detalle;
            com.Parameters.Add("@orden", System.Data.SqlDbType.Int).Value = orden;

            if (pcon.State != ConnectionState.Open)
            {
                pcon.Open();
            }
            SqlContext.Pipe.ExecuteAndSend(com);
            pcon.Close();
        }

        
        
    }
}
