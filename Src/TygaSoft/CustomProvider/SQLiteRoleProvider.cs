using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Data;
using System.Data.SQLite;
using TygaSoft.DBUtility;

namespace TygaSoft.CustomProvider
{
    public class SQLiteRoleProvider : RoleProvider
    {
        private static string Sql_Roles_Insert = string.Format(@"insert into {0} (ApplicationId,RoleId,RoleName,LoweredRoleName,Description) values (@ApplicationId,@RoleId,@RoleName,@LoweredRoleName,@Description)", SU.AspnetRoles);
        private static string Sql_Roles_Delete = string.Format(@"delete from {0} where ApplicationId = @ApplicationId and LoweredRoleName = @LoweredRoleName",SU.AspnetRoles);
        private static string Sql_Roles_SelectId = string.Format(@"select RoleId from {0} r where r.ApplicationId = @ApplicationId and r.LoweredRoleName = @LoweredRoleName",SU.AspnetRoles);
        private static string Sql_Roles_IsExistName = string.Format(@"select 1 from {0} where ApplicationId = @ApplicationId and LoweredRoleName = @LoweredRoleName", SU.AspnetRoles);
        private static string Sql_Roles_SelectAll = string.Format(@"select RoleName from {0} where ApplicationId = @ApplicationId ", SU.AspnetRoles);
        private static string Sql_Roles_IsExist = string.Format(@"select 1 from {0} where ApplicationId = @ApplicationId and LoweredRoleName = @LoweredRoleName", SU.AspnetRoles);
        private static string Sql_IsUserInRole = string.Format(@"select 1 from {0} r inner join {1} ur on r.RoleId = ur.RoleId inner join {2} u on ur.UserId = u.UserId where r.ApplicationId = @ApplicationId and LoweredUserName = @LoweredUserName and LoweredRoleName = @LoweredRoleName ", SU.AspnetRoles, SU.AspnetUsersInRoles, SU.AspnetUsers);
        private static string Sql_SelectUsersInRole = string.Format(@"select u.UserName from {0} r inner join {1} ur on r.RoleId = ur.RoleId inner join {2} u on ur.UserId = u.UserId where r.ApplicationId = @ApplicationId and LoweredRoleName = @LoweredRoleName", SU.AspnetRoles, SU.AspnetUsersInRoles, SU.AspnetUsers);
        private static string Sql_SelectRolesForUser = string.Format(@"select r.RoleName from {0} r inner join {1} ur on r.RoleId = ur.RoleId inner join {2} u on ur.UserId = u.UserId where r.ApplicationId = @ApplicationId and LoweredUserName = @LoweredUserName", SU.AspnetRoles, SU.AspnetUsersInRoles, SU.AspnetUsers);
        private static string Sql_FindUsersInRole = string.Format(@"select u..UserName from {0} r inner join {1} ur on r.RoleId = ur.RoleId inner join {2} u on ur.UserId = u.UserId where r.ApplicationId = @ApplicationId and LoweredRoleName = @LoweredRoleName and LoweredUserName like @LoweredUserName", SU.AspnetRoles, SU.AspnetUsersInRoles, SU.AspnetUsers);
        private static string Sql_IsExistUserInRole = string.Format(@"select 1 from {0} r inner join {1} ur on r.RoleId = ur.RoleId inner join {2} u on ur.UserId = u.UserId where r.ApplicationId = @ApplicationId and LoweredRoleName = @LoweredRoleName ", SU.AspnetRoles, SU.AspnetUsersInRoles, SU.AspnetUsers);
        private static string Sql_DeleteUsersByRoleName = string.Format(@"delete from {0} where RoleId in (select RoleId from {1} r where r.ApplicationId = @ApplicationId and r.LoweredRoleName = @LoweredRoleName) ", SU.AspnetUsersInRoles, SU.AspnetRoles);
        private static string Sql_RemoveUserFromRole = string.Format(@"delete from {0} where UserId = (select UserId from {1} where ApplicationId = @ApplicationId and LoweredUserName = @LoweredUserName) and RoleId = (select RoleId from {2} where ApplicationId = @ApplicationId and LoweredRoleName = @LoweredRoleName)", SU.AspnetUsersInRoles, SU.AspnetUsers, SU.AspnetRoles);

        private int _schemaVersionCheck;
        private string _sqlConnectionString;
        private int _commandTimeout;

        public override void Initialize(string name, NameValueCollection config)
        {
            // Remove CAS from sample: HttpRuntime.CheckAspNetHostingPermission (AspNetHostingPermissionLevel.Low, SR.Feature_not_supported_at_this_level);
            if (config == null)
                throw new ArgumentNullException("config");

            if (String.IsNullOrEmpty(name))
                name = "SQLiteRoleProvider";
            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", SM.GetString(SM.RoleSqlProvider_description));
            }
            base.Initialize(name, config);

            _schemaVersionCheck = 0;

            _commandTimeout = SU.GetIntValue(config, "commandTimeout", 30, true, 0);

            string temp = config["connectionStringName"];
            if (temp == null || temp.Length < 1)
                throw new ProviderException(SM.GetString(SM.Connection_name_not_specified));
            _sqlConnectionString = SQLiteConnectionHelper.GetConnectionString(temp, true, true);
            if (_sqlConnectionString == null || _sqlConnectionString.Length < 1)
            {
                throw new ProviderException(SM.GetString(SM.Connection_string_not_found, temp));
            }

            _applicationName = config["applicationName"];
            if (string.IsNullOrEmpty(_applicationName))
                _applicationName = SU.GetDefaultAppName();

            if (_applicationName.Length > 256)
            {
                throw new ProviderException(SM.GetString(SM.Provider_application_name_too_long));
            }

            config.Remove("connectionStringName");
            config.Remove("applicationName");
            config.Remove("commandTimeout");
            if (config.Count > 0)
            {
                string attribUnrecognized = config.GetKey(0);
                if (!String.IsNullOrEmpty(attribUnrecognized))
                    throw new ProviderException(SM.GetString(SM.Provider_unrecognized_attribute, attribUnrecognized));
            }
        }

        private string _applicationId;

        public string ApplicationId
        {
            get
            {
                if (string.IsNullOrEmpty(_applicationId))
                {
                    _applicationId = SU.GetApplicationId(ApplicationName, SqlConnectionString);
                }

                return _applicationId;
            }
        }

        public string SqlConnectionString
        {
            get { return _sqlConnectionString; }
        }

        public string _applicationName;

        public override string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                _applicationName = value;

                if (_applicationName.Length > 256)
                {
                    throw new ProviderException(SM.GetString(SM.Provider_application_name_too_long));
                }
            }
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            foreach (var username in usernames)
            {
                foreach (var rolename in roleNames) 
                {
                    if (!IsUserInRole(username, rolename)) 
                    {
                        AddUserToRole(username, rolename);
                    }
                }
            }
        }

        private void AddUserToRole(string username, string roleName)
        {
            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredUserName", DbType.String, username.ToLower()),
                                          SU.CreateInputParam("@LoweredRoleName", DbType.String, roleName.ToLower())
                                      };

            var userId = string.Empty;
            var roleId = string.Empty;
            var effect = 0;

            using(SQLiteConnection conn = SQLiteConnectionHelper.GetConnection(SqlConnectionString, true).Connection)
            {
                using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(conn, CommandType.Text, SU.Sql_Users_SelectIdByName, parms))
                {
                    if (reader.Read())
                    {
                        userId = reader.GetString(0);
                    }
                }
                using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(conn, CommandType.Text, Sql_Roles_SelectId, parms))
                {
                    if (reader.Read())
                    {
                        roleId = reader.GetString(0);
                    }
                }

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleId)) return;

                SQLiteParameter[] uirParms = {
                                                  SU.CreateInputParam("@UserId", DbType.String, userId),
                                                  SU.CreateInputParam("@RoleId", DbType.String, roleId)
                                             };

                effect = SQLiteHelper.ExecuteNonQuery(conn, CommandType.Text, SU.Sql_UsersInRoles_Insert, uirParms);
            }

            if (effect < 1) throw new ProviderException(SM.Provider_unknown_failure);
        }

        public override void CreateRole(string roleName)
        {
            SQLiteParameter[] isExistParms = {
                                                 SU.CreateInputParam("@ApplicationId",DbType.String,ApplicationId),
                                                 SU.CreateInputParam("@LoweredRoleName",DbType.String,roleName.ToLower())
                                             };
            using (SQLiteConnection conn = SQLiteConnectionHelper.GetConnection(SqlConnectionString, true).Connection)
            {
                var obj = SQLiteHelper.ExecuteScalar(conn, CommandType.Text, Sql_Roles_IsExistName, isExistParms);
                if (obj != null) return;

                SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId",DbType.String,ApplicationId),
                                          SU.CreateInputParam("@RoleId",DbType.String,Guid.NewGuid().ToString("N")),
                                          SU.CreateInputParam("@RoleName",DbType.String,roleName),
                                          SU.CreateInputParam("@LoweredRoleName",DbType.String,roleName.ToLower()),
                                          SU.CreateInputParam("@Description",DbType.String,string.Empty)
                                      };
                SQLiteHelper.ExecuteNonQuery(conn, CommandType.Text, Sql_Roles_Insert, parms);
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredRoleName", DbType.String, roleName.ToLower())
                                      };
            int effect = 0;

            using (SQLiteConnection conn = SQLiteConnectionHelper.GetConnection(SqlConnectionString, true).Connection)
            {
                if (throwOnPopulatedRole)
                {
                    var obj = SQLiteHelper.ExecuteScalar(conn, CommandType.Text, Sql_IsExistUserInRole, parms);
                    if (obj != null) throw new ProviderException(SM.GetString(SM.Role_is_not_empty));
                }
                effect = SQLiteHelper.ExecuteNonQuery(conn, CommandType.Text, Sql_Roles_Delete, parms);
                SQLiteHelper.ExecuteNonQuery(conn, CommandType.Text, Sql_DeleteUsersByRoleName, parms);
            }

            return effect > 0;
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            var arr = new List<string>();

            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredUserName", DbType.String, "%'"+usernameToMatch.ToLower()+"'%"),
                                          SU.CreateInputParam("@LoweredRoleName", DbType.String, roleName.ToLower())
                                      };

            using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(SqlConnectionString, CommandType.Text, Sql_FindUsersInRole, parms))
            {
                while (reader.Read())
                {
                    arr.Add(reader.GetString(0));
                }
            }

            return arr.ToArray();
        }

        public override string[] GetAllRoles()
        {
            var arr = new List<string>();
            using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(SqlConnectionString, CommandType.Text, Sql_Roles_SelectAll, SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId)))
            {
                while (reader.Read()) 
                {
                    arr.Add(reader.GetString(0));
                }
            }

            return arr.ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            var arr = new List<string>();

            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredUserName", DbType.String, username.ToLower())
                                      };

            using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(SqlConnectionString, CommandType.Text, Sql_SelectRolesForUser, parms))
            {
                while (reader.Read())
                {
                    arr.Add(reader.GetString(0));
                }
            }

            return arr.ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            var arr = new List<string>();

            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredRoleName", DbType.String, roleName.ToLower())
                                      };

            using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(SqlConnectionString, CommandType.Text, Sql_SelectUsersInRole, parms))
            {
                while (reader.Read())
                {
                    arr.Add(reader.GetString(0));
                }
            }

            return arr.ToArray();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredUserName",DbType.String,username.ToLower()),
                                          SU.CreateInputParam("@LoweredRoleName",DbType.String,roleName.ToLower())
                                      };
            var obj = SQLiteHelper.ExecuteScalar(SqlConnectionString, CommandType.Text, Sql_IsUserInRole, parms);
            return obj != null;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            foreach (var username in usernames)
            {
                foreach (var rolename in roleNames)
                {
                    RemoveUserFromRole(username, rolename);
                }
            }
        }

        private void RemoveUserFromRole(string username, string roleName)
        {
            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredUserName",DbType.String,username.ToLower()),
                                          SU.CreateInputParam("@LoweredRoleName",DbType.String,roleName.ToLower())
                                      };
            SQLiteHelper.ExecuteScalar(SqlConnectionString, CommandType.Text, Sql_RemoveUserFromRole, parms);
        }

        public override bool RoleExists(string roleName)
        {
            SQLiteParameter[] parms = {
                                          SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                          SU.CreateInputParam("@LoweredRoleName", DbType.String, roleName.ToLower())
                                      };
            var obj = SQLiteHelper.ExecuteScalar(SqlConnectionString, CommandType.Text, Sql_Roles_IsExist, parms);
            return obj != null;
        }
    }
}
