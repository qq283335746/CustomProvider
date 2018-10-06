using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.Configuration.Provider;
using System.Web.Hosting;
using System.Globalization;
using System.Xml;
using System.Configuration;
using TygaSoft.DBUtility;

namespace TygaSoft.CustomProvider
{
    internal static class SU
    {
        public const string AspnetApplications = "aspnet_Applications";
        public const string AspnetUsers = "aspnet_Users";
        public const string AspnetMembership = "aspnet_Membership";
        public const string AspnetRoles = "aspnet_Roles";
        public const string AspnetUsersInRoles = "aspnet_UsersInRoles";
        private static string Sql_Applications_Insert = string.Format("insert into {0} (ApplicationId,ApplicationName,LoweredApplicationName,Description) values (@ApplicationId,@ApplicationName,@LoweredApplicationName,@Description)", AspnetApplications);
        public static string Sql_Applications_Id = string.Format("select ApplicationId from {0} where LoweredApplicationName = @LoweredApplicationName", AspnetApplications);
        public static string Sql_UsersInRoles_Insert = string.Format(@"insert into {0} (UserId,RoleId) values (@UserId,@RoleId)", AspnetUsersInRoles);
        public static string Sql_Users_SelectIdByName = string.Format(@"select UserId from {0} where ApplicationId = @ApplicationId and LoweredUserName = @LoweredUserName",AspnetUsers);

        public static string GetApplicationId(string applicationName, string sqlConnectionString)
        {
            string applicationId = string.Empty;
            using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(sqlConnectionString, CommandType.Text, Sql_Applications_Id, CreateInputParam("@LoweredApplicationName", DbType.String, applicationName.ToLower())))
            {
                if (reader.Read())
                {
                    applicationId = reader.GetString(0);
                }
            }
            if (string.IsNullOrEmpty(applicationId))
            {
                var appId = Guid.NewGuid().ToString("N");
                SQLiteParameter[] appParms = {
                                                         CreateInputParam("@ApplicationId",DbType.String,appId),
                                                         CreateInputParam("@ApplicationName",DbType.String,applicationName),
                                                         CreateInputParam("@LoweredApplicationName",DbType.String,applicationName.ToLower()),
                                                         CreateInputParam("@Description",DbType.String,string.Empty)
                                                     };
                var effect = SQLiteHelper.ExecuteNonQuery(sqlConnectionString, CommandType.Text, Sql_Applications_Insert, appParms);
                if (effect > 0)
                {
                    applicationId = appId;
                }
                else
                {
                    throw new ProviderException(SM.Provider_Error);
                }
            }

            return applicationId;
        }

        public static SQLiteParameter CreateInputParam(string paramName, DbType dbType, object objValue)
        {
            SQLiteParameter param = new SQLiteParameter(paramName, dbType);

            if (objValue == null)
            {
                param.IsNullable = true;
                param.Value = DBNull.Value;
            }
            else
            {
                param.Value = objValue;
            }

            return param;
        }

        internal const int Infinite = Int32.MaxValue;
        internal static string GetDefaultAppName()
        {
            try
            {
                string appName = HostingEnvironment.ApplicationVirtualPath;
                if (String.IsNullOrEmpty(appName))
                {

                    appName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;

                    int indexOfDot = appName.IndexOf('.');
                    if (indexOfDot != -1)
                    {
                        appName = appName.Remove(indexOfDot);
                    }
                }

                if (String.IsNullOrEmpty(appName))
                {
                    return "/";
                }
                else
                {
                    return appName;
                }
            }
            catch
            {
                return "/";
            }
        }

        // We don't trim the param before checking with password parameters
        internal static bool ValidatePasswordParameter(ref string param, int maxSize)
        {
            if (param == null)
            {
                return false;
            }

            if (param.Length < 1)
            {
                return false;
            }

            if (maxSize > 0 && (param.Length > maxSize))
            {
                return false;
            }

            return true;
        }

        internal static bool ValidateParameter(ref string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize)
        {
            if (param == null)
            {
                return !checkForNull;
            }

            param = param.Trim();
            if ((checkIfEmpty && param.Length < 1) ||
                 (maxSize > 0 && param.Length > maxSize) ||
                 (checkForCommas && param.Contains(",")))
            {
                return false;
            }

            return true;
        }

        // We don't trim the param before checking with password parameters
        internal static void CheckPasswordParameter(ref string param, int maxSize, string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (param.Length < 1)
            {
                throw new ArgumentException(SM.GetString(SM.Parameter_can_not_be_empty, paramName), paramName);
            }

            if (maxSize > 0 && param.Length > maxSize)
            {
                throw new ArgumentException(SM.GetString(SM.Parameter_too_long, paramName, maxSize.ToString(CultureInfo.InvariantCulture)), paramName);
            }
        }

        internal static void CheckParameter(ref string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            if (param == null)
            {
                if (checkForNull)
                {
                    throw new ArgumentNullException(paramName);
                }

                return;
            }

            param = param.Trim();
            if (checkIfEmpty && param.Length < 1)
            {
                throw new ArgumentException(SM.GetString(SM.Parameter_can_not_be_empty, paramName), paramName);
            }

            if (maxSize > 0 && param.Length > maxSize)
            {
                throw new ArgumentException(SM.GetString(SM.Parameter_too_long, paramName, maxSize.ToString(CultureInfo.InvariantCulture)), paramName);
            }

            if (checkForCommas && param.Contains(","))
            {
                throw new ArgumentException(SM.GetString(SM.Parameter_can_not_contain_comma, paramName), paramName);
            }
        }

        internal static void CheckArrayParameter(ref string[] param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (param.Length < 1)
            {
                throw new ArgumentException(SM.GetString(SM.Parameter_array_empty, paramName), paramName);
            }

            Hashtable values = new Hashtable(param.Length);
            for (int i = param.Length - 1; i >= 0; i--)
            {
                SU.CheckParameter(ref param[i], checkForNull, checkIfEmpty, checkForCommas, maxSize,
                    paramName + "[ " + i.ToString(CultureInfo.InvariantCulture) + " ]");
                if (values.Contains(param[i]))
                {
                    throw new ArgumentException(SM.GetString(SM.Parameter_duplicate_array_element, paramName), paramName);
                }
                else
                {
                    values.Add(param[i], param[i]);
                }
            }
        }

        internal static bool GetBooleanValue(NameValueCollection config, string valueName, bool defaultValue)
        {
            string sValue = config[valueName];
            if (sValue == null)
            {
                return defaultValue;
            }

            bool result;
            if (bool.TryParse(sValue, out result))
            {
                return result;
            }
            else
            {
                throw new ProviderException(SM.GetString(SM.Value_must_be_boolean, valueName));
            }
        }

        internal static int GetIntValue(NameValueCollection config, string valueName, int defaultValue, bool zeroAllowed, int maxValueAllowed)
        {
            string sValue = config[valueName];

            if (sValue == null)
            {
                return defaultValue;
            }

            int iValue;
            if (!Int32.TryParse(sValue, out iValue))
            {
                if (zeroAllowed)
                {
                    throw new ProviderException(SM.GetString(SM.Value_must_be_non_negative_integer, valueName));
                }

                throw new ProviderException(SM.GetString(SM.Value_must_be_positive_integer, valueName));
            }

            if (zeroAllowed && iValue < 0)
            {
                throw new ProviderException(SM.GetString(SM.Value_must_be_non_negative_integer, valueName));
            }

            if (!zeroAllowed && iValue <= 0)
            {
                throw new ProviderException(SM.GetString(SM.Value_must_be_positive_integer, valueName));
            }

            if (maxValueAllowed > 0 && iValue > maxValueAllowed)
            {
                throw new ProviderException(SM.GetString(SM.Value_too_big, valueName, maxValueAllowed.ToString(CultureInfo.InvariantCulture)));
            }

            return iValue;
        }

        private static bool IsDirectorySeparatorChar(char ch)
        {
            return (ch == '\\' || ch == '/');
        }

        internal static bool IsAbsolutePhysicalPath(string path)
        {
            if (path == null || path.Length < 3)
                return false;

            // e.g c:\foo
            if (path[1] == ':' && IsDirectorySeparatorChar(path[2]))
                return true;

            // e.g \\server\share\foo or //server/share/foo
            return IsUncSharePath(path);
        }

        internal static bool IsUncSharePath(string path)
        {
            // e.g \\server\share\foo or //server/share/foo
            if (path.Length > 2 && IsDirectorySeparatorChar(path[0]) && IsDirectorySeparatorChar(path[1]))
                return true;
            return false;

        }

        internal static XmlNode GetAndRemoveBooleanAttribute(XmlNode node, string attrib, ref bool val)
        {
            return GetAndRemoveBooleanAttributeInternal(node, attrib, false /*fRequired*/, ref val);
        }

        // input.Xml cursor must be at a true/false XML attribute
        private static XmlNode GetAndRemoveBooleanAttributeInternal(XmlNode node, string attrib, bool fRequired, ref bool val)
        {
            XmlNode a = GetAndRemoveAttribute(node, attrib, fRequired);
            if (a != null)
            {
                if (a.Value == "true")
                {
                    val = true;
                }
                else if (a.Value == "false")
                {
                    val = false;
                }
                else
                {
                    throw new ConfigurationErrorsException(
                                    SM.GetString(SM.Invalid_boolean_attribute, a.Name),
                                    a);
                }
            }

            return a;
        }

        private static XmlNode GetAndRemoveAttribute(XmlNode node, string attrib, bool fRequired)
        {
            XmlNode a = node.Attributes.RemoveNamedItem(attrib);

            // If the attribute is required and was not present, throw
            if (fRequired && a == null)
            {
                throw new ConfigurationErrorsException(
                    SM.GetString(SM.Missing_required_attribute, attrib, node.Name),
                    node);
            }

            return a;
        }

        internal static XmlNode GetAndRemoveNonEmptyStringAttribute(XmlNode node, string attrib, ref string val)
        {
            return GetAndRemoveNonEmptyStringAttributeInternal(node, attrib, false /*fRequired*/, ref val);
        }

        private static XmlNode GetAndRemoveNonEmptyStringAttributeInternal(XmlNode node, string attrib, bool fRequired, ref string val)
        {
            XmlNode a = GetAndRemoveStringAttributeInternal(node, attrib, fRequired, ref val);
            if (a != null && val.Length == 0)
            {
                throw new ConfigurationErrorsException(
                    SM.GetString(SM.Empty_attribute, attrib),
                    a);
            }

            return a;
        }

        private static XmlNode GetAndRemoveStringAttributeInternal(XmlNode node, string attrib, bool fRequired, ref string val)
        {
            XmlNode a = GetAndRemoveAttribute(node, attrib, fRequired);
            if (a != null)
            {
                val = a.Value;
            }

            return a;
        }

        internal static void CheckForUnrecognizedAttributes(XmlNode node)
        {
            if (node.Attributes.Count != 0)
            {
                throw new ConfigurationErrorsException(
                                SM.GetString(SM.Config_base_unrecognized_attribute, node.Attributes[0].Name),
                                node.Attributes[0]);
            }
        }

        internal static void CheckForNonCommentChildNodes(XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType != XmlNodeType.Comment)
                {
                    throw new ConfigurationErrorsException(
                                    SM.GetString(SM.Config_base_no_child_nodes),
                                    childNode);
                }
            }
        }

        internal static XmlNode GetAndRemoveStringAttribute(XmlNode node, string attrib, ref string val)
        {
            return GetAndRemoveStringAttributeInternal(node, attrib, false /*fRequired*/, ref val);
        }

        internal static void CheckForbiddenAttribute(XmlNode node, string attrib)
        {
            XmlAttribute attr = node.Attributes[attrib];
            if (attr != null)
            {
                throw new ConfigurationErrorsException(
                                SM.GetString(SM.Config_base_unrecognized_attribute, attrib),
                                attr);
            }
        }

        // Returns whether the virtual path is relative.  Note that this returns true for
        // app relative paths (e.g. "~/sub/foo.aspx")
        internal static bool IsRelativeUrl(string virtualPath)
        {
            // If it has a protocol, it's not relative
            if (virtualPath.IndexOf(":", StringComparison.Ordinal) != -1)
                return false;

            return !IsRooted(virtualPath);
        }

        internal static bool IsRooted(String basepath)
        {
            return (String.IsNullOrEmpty(basepath) || basepath[0] == '/' || basepath[0] == '\\');
        }

        internal static void GetAndRemoveStringAttribute(NameValueCollection config, string attrib, string providerName, ref string val)
        {
            val = config.Get(attrib);
            config.Remove(attrib);
        }

        internal static void CheckUnrecognizedAttributes(NameValueCollection config, string providerName)
        {
            if (config.Count > 0)
            {
                string attribUnrecognized = config.GetKey(0);
                if (!String.IsNullOrEmpty(attribUnrecognized))
                    throw new ConfigurationErrorsException(
                                    SM.GetString(SM.Unexpected_provider_attribute, attribUnrecognized, providerName));
            }
        }

        internal static string GetStringFromBool(bool flag)
        {
            return flag ? "true" : "false";
        }
        internal static void GetAndRemovePositiveOrInfiniteAttribute(NameValueCollection config, string attrib, string providerName, ref int val)
        {
            GetPositiveOrInfiniteAttribute(config, attrib, providerName, ref val);
            config.Remove(attrib);
        }

        internal static void GetPositiveOrInfiniteAttribute(NameValueCollection config, string attrib, string providerName, ref int val)
        {
            string s = config.Get(attrib);
            int t;

            if (s == null)
            {
                return;
            }

            if (s == "Infinite")
            {
                t = Infinite;
            }
            else
            {
                try
                {
                    t = Convert.ToInt32(s, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    if (e is ArgumentException || e is FormatException || e is OverflowException)
                    {
                        throw new ConfigurationErrorsException(
                            SM.GetString(SM.Invalid_provider_positive_attributes, attrib, providerName));
                    }
                    else
                    {
                        throw;
                    }

                }

                if (t < 0)
                {
                    throw new ConfigurationErrorsException(
                        SM.GetString(SM.Invalid_provider_positive_attributes, attrib, providerName));

                }
            }

            val = t;
        }

        internal static void GetAndRemovePositiveAttribute(NameValueCollection config, string attrib, string providerName, ref int val)
        {
            GetPositiveAttribute(config, attrib, providerName, ref val);
            config.Remove(attrib);
        }

        internal static void GetPositiveAttribute(NameValueCollection config, string attrib, string providerName, ref int val)
        {
            string s = config.Get(attrib);
            int t;

            if (s == null)
            {
                return;
            }

            try
            {
                t = Convert.ToInt32(s, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                if (e is ArgumentException || e is FormatException || e is OverflowException)
                {
                    throw new ConfigurationErrorsException(
                        SM.GetString(SM.Invalid_provider_positive_attributes, attrib, providerName));
                }
                else
                {
                    throw;
                }

            }

            if (t < 0)
            {
                throw new ConfigurationErrorsException(
                    SM.GetString(SM.Invalid_provider_positive_attributes, attrib, providerName));

            }

            val = t;
        }
    }
}
