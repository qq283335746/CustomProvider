using System;
using System.Data.SQLite;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections;

namespace TygaSoft.DBUtility
{
    public sealed class SQLiteHelper
    {
        public static readonly string AspnetDbConnString = ConfigurationManager.ConnectionStrings["AspnetDbConnString"].ConnectionString;

        #region private utility methods & constructors

        private static void AttachParameters(SQLiteCommand command, SQLiteParameter[] commandParameters)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (commandParameters != null)
            {
                foreach (SQLiteParameter p in commandParameters)
                {
                    if (p != null)
                    {
                        if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && (p.Value == null))
                        {
                            p.Value = DBNull.Value;
                        }
                        command.Parameters.Add(p);
                    }
                }
            }
        }

        private static void AssignParameterValues(SQLiteParameter[] commandParameters, DataRow dataRow)
        {
            if ((commandParameters == null) || (dataRow == null))
            {
                return;
            }

            int i = 0;
            foreach (SQLiteParameter commandParameter in commandParameters)
            {
                if (commandParameter.ParameterName == null ||
                 commandParameter.ParameterName.Length <= 1)
                    throw new Exception(
                     string.Format("请提供参数{0}一个有效的名称{1}.", i, commandParameter.ParameterName));
                // 从dataRow的表中获取为参数数组中数组名称的列的索引.
                // 如果存在和参数名称相同的列,则将列值赋给当前名称的参数.
                if (dataRow.Table.Columns.IndexOf(commandParameter.ParameterName.Substring(1)) != -1)
                    commandParameter.Value = dataRow[commandParameter.ParameterName.Substring(1)];
                i++;
            }
        }

        private static void AssignParameterValues(SQLiteParameter[] commandParameters, object[] parameterValues)
        {
            if ((commandParameters == null) || (parameterValues == null))
            {
                return;
            }

            if (commandParameters.Length != parameterValues.Length)
            {
                throw new ArgumentException("参数值个数与参数不匹配.");
            }

            for (int i = 0, j = commandParameters.Length; i < j; i++)
            {
                if (parameterValues[i] is IDbDataParameter)
                {
                    IDbDataParameter paramInstance = (IDbDataParameter)parameterValues[i];
                    if (paramInstance.Value == null)
                    {
                        commandParameters[i].Value = DBNull.Value;
                    }
                    else
                    {
                        commandParameters[i].Value = paramInstance.Value;
                    }
                }
                else if (parameterValues[i] == null)
                {
                    commandParameters[i].Value = DBNull.Value;
                }
                else
                {
                    commandParameters[i].Value = parameterValues[i];
                }
            }
        }

        private static void PrepareCommand(SQLiteCommand command, SQLiteConnection connection, SQLiteTransaction transaction, CommandType commandType, string commandText, SQLiteParameter[] commandParameters, out bool mustCloseConnection)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (commandText == null || commandText.Length == 0) throw new ArgumentNullException("commandText");

            if (connection.State != ConnectionState.Open)
            {
                mustCloseConnection = true;
                connection.Open();
            }
            else
            {
                mustCloseConnection = false;
            }

            command.Connection = connection;

            command.CommandText = commandText;

            if (transaction != null)
            {
                if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
                command.Transaction = transaction;
            }

            command.CommandType = commandType;

            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }
            return;
        }

        #endregion private utility methods & constructors

        #region ExecuteNonQuery

        public static int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(connectionString, commandType, commandText, (SQLiteParameter[])null);
        }

        public static int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connectionString == null || connectionString.Length == 0) throw new ArgumentNullException("connectionString");

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                return ExecuteNonQuery(connection, commandType, commandText, commandParameters);
            }
        }

        public static int ExecuteNonQuery(SQLiteConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(connection, commandType, commandText, (SQLiteParameter[])null);
        }

        public static int ExecuteNonQuery(SQLiteConnection connection, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            SQLiteCommand cmd = new SQLiteCommand();
            bool mustCloseConnection = false;
            PrepareCommand(cmd, connection, (SQLiteTransaction)null, commandType, commandText, commandParameters, out mustCloseConnection);

            int retval = cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            if (mustCloseConnection) connection.Close();
            return retval;
        }

        public static int ExecuteNonQuery(SQLiteTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(transaction, commandType, commandText, (SQLiteParameter[])null);
        }

        public static int ExecuteNonQuery(SQLiteTransaction transaction, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            SQLiteCommand cmd = new SQLiteCommand();
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            int retval = cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            return retval;
        }

        #endregion ExecuteNonQuery
        #region ExecuteReader

        private enum SQLiteConnectionOwnership
        {
            /// <summary>Connection is owned and managed by SqlHelper</summary>
            Internal,
            /// <summary>Connection is owned and managed by the caller</summary>
            External
        }

        public static SQLiteDataReader ExecuteReader(string connectionString, CommandType commandType, string commandText)
        {
            return ExecuteReader(connectionString, commandType, commandText, (SQLiteParameter[])null);
        }

        public static SQLiteDataReader ExecuteReader(string connectionString, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connectionString == null || connectionString.Length == 0) throw new ArgumentNullException("connectionString");
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(connectionString);
                connection.Open();

                return ExecuteReader(connection, null, commandType, commandText, commandParameters, SQLiteConnectionOwnership.Internal);
            }
            catch
            {
                if (connection != null) connection.Close();
                throw;
            }
        }

        private static SQLiteDataReader ExecuteReader(SQLiteConnection connection, SQLiteTransaction transaction, CommandType commandType, string commandText, SQLiteParameter[] commandParameters, SQLiteConnectionOwnership connectionOwnership)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            bool mustCloseConnection = false;
            SQLiteCommand cmd = new SQLiteCommand();
            try
            {
                PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

                SQLiteDataReader dataReader;
                //Call ExecuteReader with the appropriate CommandBehavior
                if (connectionOwnership == SQLiteConnectionOwnership.External)
                {
                    dataReader = cmd.ExecuteReader();
                }
                else
                {
                    dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }

                // 清除参数,以便再次使用..
                // HACK: There is a problem here, the output parameter values are fletched 
                // when the reader is closed, so if the parameters are detached from the command
                // then the SqlReader can set its values. 
                // When this happen, the parameters can be used again in other command.
                bool canClear = true;
                foreach (SQLiteParameter commandParameter in cmd.Parameters)
                {
                    if (commandParameter.Direction != ParameterDirection.Input)
                        canClear = false;
                }

                if (canClear)
                {
                    cmd.Parameters.Clear();
                }

                return dataReader;
            }
            catch
            {
                if (mustCloseConnection)
                    connection.Close();
                throw;
            }
        }

        public static SQLiteDataReader ExecuteReader(SQLiteConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteReader(connection, commandType, commandText, (SQLiteParameter[])null);
        }

        public static SQLiteDataReader ExecuteReader(SQLiteConnection connection, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            return ExecuteReader(connection, (SQLiteTransaction)null, commandType, commandText, commandParameters, SQLiteConnectionOwnership.External);
        }

        public static SQLiteDataReader ExecuteReader(SQLiteTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteReader(transaction, commandType, commandText, (SQLiteParameter[])null);
        }

        public static SQLiteDataReader ExecuteReader(SQLiteTransaction transaction, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            return ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, SQLiteConnectionOwnership.External);
        }

        #endregion ExecuteReader
        #region ExecuteScalar

        public static object ExecuteScalar(string connectionString, CommandType commandType, string commandText)
        {
            return ExecuteScalar(connectionString, commandType, commandText, (SQLiteParameter[])null);
        }

        public static object ExecuteScalar(string connectionString, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connectionString == null || connectionString.Length == 0) throw new ArgumentNullException("connectionString");
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                return ExecuteScalar(connection, commandType, commandText, commandParameters);
            }
        }

        public static object ExecuteScalar(SQLiteConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteScalar(connection, commandType, commandText, (SQLiteParameter[])null);
        }

        public static object ExecuteScalar(SQLiteConnection connection, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            SQLiteCommand cmd = new SQLiteCommand();

            bool mustCloseConnection = false;
            PrepareCommand(cmd, connection, (SQLiteTransaction)null, commandType, commandText, commandParameters, out mustCloseConnection);

            object retval = cmd.ExecuteScalar();

            cmd.Parameters.Clear();

            if (mustCloseConnection)
                connection.Close();

            return retval;
        }

        public static object ExecuteScalar(SQLiteTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteScalar(transaction, commandType, commandText, (SQLiteParameter[])null);
        }

        public static object ExecuteScalar(SQLiteTransaction transaction, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            SQLiteCommand cmd = new SQLiteCommand();
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            object retval = cmd.ExecuteScalar();

            cmd.Parameters.Clear();
            return retval;
        }

        #endregion ExecuteScalar
        #region ExecuteDataset

        public static DataSet ExecuteDataset(string connectionString, CommandType commandType, string commandText)
        {
            return ExecuteDataset(connectionString, commandType, commandText, (SQLiteParameter[])null);
        }

        public static DataSet ExecuteDataset(string connectionString, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connectionString == null || connectionString.Length == 0) throw new ArgumentNullException("connectionString");

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                return ExecuteDataset(connection, commandType, commandText, commandParameters);
            }
        }

        public static DataSet ExecuteDataset(SQLiteConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteDataset(connection, commandType, commandText, (SQLiteParameter[])null);
        }

        public static DataSet ExecuteDataset(SQLiteConnection connection, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            SQLiteCommand cmd = new SQLiteCommand();
            bool mustCloseConnection = false;
            PrepareCommand(cmd, connection, (SQLiteTransaction)null, commandType, commandText, commandParameters, out mustCloseConnection);

            using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
            {
                DataSet ds = new DataSet();

                da.Fill(ds);

                cmd.Parameters.Clear();

                if (mustCloseConnection)
                    connection.Close();

                return ds;
            }
        }

        public static DataSet ExecuteDataset(SQLiteTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteDataset(transaction, commandType, commandText, (SQLiteParameter[])null);
        }

        public static DataSet ExecuteDataset(SQLiteTransaction transaction, CommandType commandType, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            SQLiteCommand cmd = new SQLiteCommand();
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
            {
                DataSet ds = new DataSet();
                da.Fill(ds);
                cmd.Parameters.Clear();
                return ds;
            }
        }

        #endregion ExecuteDataset
    }

    public sealed class SQLiteHelperParameterCache
    {
        #region 

        private SQLiteHelperParameterCache() { }

        private static Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        private static SQLiteParameter[] CloneParameters(SQLiteParameter[] originalParameters)
        {
            SQLiteParameter[] clonedParameters = new SQLiteParameter[originalParameters.Length];

            for (int i = 0, j = originalParameters.Length; i < j; i++)
            {
                clonedParameters[i] = (SQLiteParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return clonedParameters;
        }

        #endregion

        #region CacheParameter

        public static void CacheParameterSet(string connectionString, string commandText, params SQLiteParameter[] commandParameters)
        {
            if (connectionString == null || connectionString.Length == 0) throw new ArgumentNullException("connectionString");
            if (commandText == null || commandText.Length == 0) throw new ArgumentNullException("commandText");

            string hashKey = connectionString + ":" + commandText;

            paramCache[hashKey] = commandParameters;
        }

        public static SQLiteParameter[] GetCachedParameterSet(string connectionString, string commandText)
        {
            if (connectionString == null || connectionString.Length == 0) throw new ArgumentNullException("connectionString");
            if (commandText == null || commandText.Length == 0) throw new ArgumentNullException("commandText");

            string hashKey = connectionString + ":" + commandText;

            SQLiteParameter[] cachedParameters = paramCache[hashKey] as SQLiteParameter[];
            if (cachedParameters == null)
            {
                return null;
            }
            else
            {
                return CloneParameters(cachedParameters);
            }
        }

        #endregion

    }
}
