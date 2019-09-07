using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Configuration;
using System.Data.SQLite;

namespace Yibi.SQLiteMembershipProvider
{
    internal static class SQLiteConnectionHelper
    {
        internal const string s_strUpperDataDirWithToken = "|DATADIRECTORY|";
        private static object s_lock = new object();

        /// <summary>
        /// 获取有效的数据库连接对象
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="revertImpersonation"></param>
        /// <returns></returns>
        internal static SQLiteConnectionHolder GetConnection(string connectionString, bool revertImpersonation)
        {
            string strTempConnection = connectionString.ToUpperInvariant();
            //Commented out for source code release.
            //if (strTempConnection.Contains(s_strUpperDataDirWithToken))
            //    EnsureSqlExpressDBFile( connectionString );

            SQLiteConnectionHolder holder = new SQLiteConnectionHolder(connectionString);
            bool closeConn = true;
            try
            {
                try
                {
                    holder.Open(null, revertImpersonation);
                    closeConn = false;
                }
                finally
                {
                    if (closeConn)
                    {
                        holder.Close();
                        holder = null;
                    }
                }
            }
            catch
            {
                throw;
            }
            return holder;
        }

        /// <summary>
        /// 获取有效的数据库连接字符串
        /// </summary>
        /// <param name="specifiedConnectionString"></param>
        /// <param name="lookupConnectionString"></param>
        /// <param name="appLevel"></param>
        /// <returns></returns>
        internal static string GetConnectionString(string specifiedConnectionString, bool lookupConnectionString, bool appLevel)
        {
            if (string.IsNullOrEmpty(specifiedConnectionString))
                return null;

            string connectionString = null;

            /////////////////////////////////////////
            // Step 1: Check <connectionStrings> config section for this connection string
            if (lookupConnectionString)
            {
                ConnectionStringSettings connObj = ConfigurationManager.ConnectionStrings[specifiedConnectionString];
                if (connObj != null)
                    connectionString = connObj.ConnectionString;

                if (connectionString == null)
                    return null;
            }
            else
            {
                connectionString = specifiedConnectionString;
            }

            return connectionString;
        }
    }

    /// <summary>
    /// 有效的数据库连接对象
    /// </summary>
    internal sealed class SQLiteConnectionHolder
    {
        internal SQLiteConnection _Connection;
        private bool _Opened;

        internal SQLiteConnection Connection
        {
            get { return _Connection; }
        }

        /// <summary>
        /// 创建一个数据库连接对象
        /// </summary>
        /// <param name="connectionString"></param>
        internal SQLiteConnectionHolder(string connectionString)
        {
            try
            {
                _Connection = new SQLiteConnection(connectionString);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(SM.GetString(SM.SqlError_Connection_String), "connectionString", e);
            }
        }

        internal void Open(HttpContext context, bool revertImpersonate)
        {
            if (_Opened)
                return; // Already opened

            if (revertImpersonate)
            {
                using (HostingEnvironment.Impersonate())
                {
                    Connection.Open();
                }
            }
            else
            {
                Connection.Open();
            }

            _Opened = true; // Open worked!
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        internal void Close()
        {
            if (!_Opened) // Not open!
                return;
            // Close connection
            Connection.Close();
            _Opened = false;
        }
    }
}
