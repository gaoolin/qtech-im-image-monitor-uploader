using ImMonitorUploader.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;

namespace ImMonitorUploader.Repositories
{
    public class LogRepository
    {
        private readonly string _connectionString;

        public LogRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["AaGlueLogDb"].ConnectionString;
        }

        /// <summary>
        /// 将日志写入数据库
        /// </summary>
        public void InsertLog(LogEntry logEntry)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    string sql = "INSERT INTO IM_AA_GLUE_IMAGE_LOGS (LOG_DATE, MESSAGE) VALUES (:LogDate, :Message)";
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("LogDate", logEntry.LogDate));
                        cmd.Parameters.Add(new OracleParameter("Message", logEntry.Message));
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("数据库日志写入失败: " + ex.Message);
            }
        }
    }
}
