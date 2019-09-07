using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Text.RegularExpressions;
using System.Web;
using System.Data.SQLite;
using System.Data;
using System.Security.Cryptography;

namespace Yibi.SQLiteMembershipProvider
{
    public class CustomMembershipProvider : MembershipProvider
    {
        private static string Sql_Applications_Id = string.Format("select ApplicationId from {0} where LoweredApplicationName = @LoweredApplicationName", SU.AspnetApplications);
        private static string Sql_SelectPasswordWithFormat = string.Format("select m.Password,m.PasswordFormat,m.PasswordSalt,m.FailedPasswordAttemptCount,m.IsApproved,m.LastLoginDate,u.LastActivityDate from {0} m,{1} u where m.UserId = u.UserId and u.UserName = @UserName and u.ApplicationId = @ApplicationId", SU.AspnetMembership, SU.AspnetUsers);
        private static string Sql_Users_CheckUserName = string.Format("select 1 from {0} where ApplicationId = @ApplicationId and LoweredUserName = @LoweredUserName", SU.AspnetUsers);
        private static string Sql_Users_UpdateLastActivityDate = string.Format("update {0} set LastActivityDate = @LastActivityDate where LoweredUserName = @LoweredUserName and ApplicationId = @ApplicationId", SU.AspnetUsers);
        //private static string Sql_UpdateFailedPassword = ""; 待完成
        private static string Sql_Users_Insert = string.Format("insert into {0} (ApplicationId,UserId,UserName,LoweredUserName,MobileAlias,IsAnonymous,LastActivityDate) values (@ApplicationId,@UserId,@UserName,@LoweredUserName,@MobileAlias,@IsAnonymous,@LastActivityDate)", SU.AspnetUsers);
        private static string Sql_Membership_Insert = string.Format(@"insert into {0} (ApplicationId,UserId,Password,PasswordFormat,PasswordSalt,MobilePIN,Email,LoweredEmail,PasswordQuestion,PasswordAnswer,IsApproved,IsLockedOut,CreateDate,LastLoginDate,LastPasswordChangedDate,LastLockoutDate,FailedPasswordAttemptCount,FailedPasswordAttemptWindowStart,FailedPasswordAnswerAttemptCount,FailedPasswordAnswerAttemptWindowStart,Comment) 
            values (@ApplicationId,@UserId,@Password,@PasswordFormat,@PasswordSalt,@MobilePIN,@Email,@LoweredEmail,@PasswordQuestion,@PasswordAnswer,@IsApproved,@IsLockedOut,@CreateDate,@LastLoginDate,@LastPasswordChangedDate,@LastLockoutDate,@FailedPasswordAttemptCount,@FailedPasswordAttemptWindowStart,@FailedPasswordAnswerAttemptCount,@FailedPasswordAnswerAttemptWindowStart,@Comment)", SU.AspnetMembership);
        private static string Sql_Users_Mem_FindUserInfoByName = string.Format(@"select Password,PasswordFormat,PasswordSalt from {0} u,{1} m where u.UserId = m.UserId and u.LoweredUserName = @LoweredUserName", SU.AspnetUsers, SU.AspnetMembership);

        private string _sqlConnectionString;
        private bool _enablePasswordRetrieval;
        private bool _enablePasswordReset;
        private bool _requiresQuestionAndAnswer;
        private bool _requiresUniqueEmail;
        private int _maxInvalidPasswordAttempts;
        private int _commandTimeout;
        private int _passwordAttemptWindow;
        private int _minRequiredPasswordLength;
        private int _minRequiredNonalphanumericCharacters;
        private string _passwordStrengthRegularExpression;
        private int _schemaVersionCheck;
        private MembershipPasswordFormat _passwordFormat;

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");
            if (String.IsNullOrEmpty(name))
                name = "SQLiteMembershipProvider";
            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", SM.GetString(SM.MembershipSqlProvider_description));
            }
            base.Initialize(name, config);

            _schemaVersionCheck = 0;

            _enablePasswordRetrieval = SU.GetBooleanValue(config, "enablePasswordRetrieval", false);
            _enablePasswordReset = SU.GetBooleanValue(config, "enablePasswordReset", true);
            _requiresQuestionAndAnswer = SU.GetBooleanValue(config, "requiresQuestionAndAnswer", true);
            _requiresUniqueEmail = SU.GetBooleanValue(config, "requiresUniqueEmail", true);
            _maxInvalidPasswordAttempts = SU.GetIntValue(config, "maxInvalidPasswordAttempts", 5, false, 0);
            _passwordAttemptWindow = SU.GetIntValue(config, "passwordAttemptWindow", 10, false, 0);
            _minRequiredPasswordLength = SU.GetIntValue(config, "minRequiredPasswordLength", 7, false, 128);
            _minRequiredNonalphanumericCharacters = SU.GetIntValue(config, "minRequiredNonalphanumericCharacters", 1, true, 128);

            _passwordStrengthRegularExpression = config["passwordStrengthRegularExpression"];
            if (_passwordStrengthRegularExpression != null)
            {
                _passwordStrengthRegularExpression = _passwordStrengthRegularExpression.Trim();
                if (_passwordStrengthRegularExpression.Length != 0)
                {
                    try
                    {
                        Regex regex = new Regex(_passwordStrengthRegularExpression);
                    }
                    catch (ArgumentException e)
                    {
                        throw new ProviderException(e.Message, e);
                    }
                }
            }
            else
            {
                _passwordStrengthRegularExpression = string.Empty;
            }
            if (_minRequiredNonalphanumericCharacters > _minRequiredPasswordLength)
                throw new HttpException(SM.GetString(SM.MinRequiredNonalphanumericCharacters_can_not_be_more_than_MinRequiredPasswordLength));

            _commandTimeout = SU.GetIntValue(config, "commandTimeout", 30, true, 0);
            _ApplicationName = config["applicationName"];
            if (string.IsNullOrEmpty(_ApplicationName))
                _ApplicationName = SU.GetDefaultAppName();

            if (_ApplicationName.Length > 256)
            {
                throw new ProviderException(SM.GetString(SM.Provider_application_name_too_long));
            }

            string strTemp = config["passwordFormat"];
            if (strTemp == null)
                strTemp = "Hashed";

            switch (strTemp)
            {
                case "Clear":
                    _passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                case "Encrypted":
                    _passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Hashed":
                    _passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                default:
                    throw new ProviderException(SM.GetString(SM.Provider_bad_password_format));
            }

            if (PasswordFormat == MembershipPasswordFormat.Hashed && EnablePasswordRetrieval)
                throw new ProviderException(SM.GetString(SM.Provider_can_not_retrieve_hashed_password));
            //if (_PasswordFormat == MembershipPasswordFormat.Encrypted && MachineKeySection.IsDecryptionKeyAutogenerated)
            //    throw new ProviderException(SM.GetString(SM.Can_not_use_encrypted_passwords_with_autogen_keys));

            string temp = config["connectionStringName"];
            if (temp == null || temp.Length < 1)
                throw new ProviderException(SM.GetString(SM.Connection_name_not_specified));
            _sqlConnectionString = SQLiteConnectionHelper.GetConnectionString(temp, true, true);
            if (_sqlConnectionString == null || _sqlConnectionString.Length < 1)
            {
                throw new ProviderException(SM.GetString(SM.Connection_string_not_found, temp));
            }

            config.Remove("connectionStringName");
            config.Remove("enablePasswordRetrieval");
            config.Remove("enablePasswordReset");
            config.Remove("requiresQuestionAndAnswer");
            config.Remove("applicationName");
            config.Remove("requiresUniqueEmail");
            config.Remove("maxInvalidPasswordAttempts");
            config.Remove("passwordAttemptWindow");
            config.Remove("commandTimeout");
            config.Remove("passwordFormat");
            config.Remove("name");
            config.Remove("minRequiredPasswordLength");
            config.Remove("minRequiredNonalphanumericCharacters");
            config.Remove("passwordStrengthRegularExpression");
            if (config.Count > 0)
            {
                string attribUnrecognized = config.GetKey(0);
                if (!String.IsNullOrEmpty(attribUnrecognized))
                    throw new ProviderException(SM.GetString(SM.Provider_unrecognized_attribute, attribUnrecognized));
            }
        }

        public bool IsAnonymous
        {
            get { return !HttpContext.Current.User.Identity.IsAuthenticated; }
        }

        public string SqlConnectionString
        {
            get { return _sqlConnectionString; }
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

        private string _ApplicationName;

        public override string ApplicationName
        {
            get { return _ApplicationName; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value");

                if (value.Length > 256)
                    throw new ProviderException(SM.GetString(SM.Provider_application_name_too_long));
                _ApplicationName = value;
            }
        }

        public override bool EnablePasswordReset
        {
            get { return _enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return _enablePasswordRetrieval; }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return _maxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return _minRequiredNonalphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return _minRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return _passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return _passwordFormat; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return _passwordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return _requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return _requiresUniqueEmail; }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) 
            {
                status = MembershipCreateStatus.UserRejected;
                return null;
            }

            string salt = GenerateSalt();
            string psw = EncodePassword(password, (int)PasswordFormat, salt);
            if (psw.Length > 128)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            SQLiteParameter[] parms1 = {
                                           SU.CreateInputParam("@ApplicationId", DbType.String, ApplicationId),
                                           SU.CreateInputParam("@LoweredUserName", DbType.String, username.ToLower())
                                       };
            var effect = SQLiteHelper.ExecuteScalar(SqlConnectionString, CommandType.Text, Sql_Users_CheckUserName, parms1);
            if (effect != null) 
            {
                status = MembershipCreateStatus.DuplicateUserName;
                return null;
            }
            Guid userId = Guid.Empty;
            if (providerUserKey != null)
            {
                Guid.TryParse(providerUserKey.ToString(), out userId);
            }
            else 
            {
                userId = Guid.NewGuid();
            }

            DateTime dt = RoundToSeconds(DateTime.Now);

            SQLiteParameter[] usersParms = {
                                 SU.CreateInputParam("@ApplicationId",DbType.String,ApplicationId),
                                 SU.CreateInputParam("@UserId",DbType.String,userId.ToString("N")),
                                 SU.CreateInputParam("@UserName",DbType.String,username),
                                 SU.CreateInputParam("@LoweredUserName",DbType.String,username.ToLower()),
                                 SU.CreateInputParam("@MobileAlias",DbType.String,string.Empty),
                                 SU.CreateInputParam("@IsAnonymous",DbType.Boolean,IsAnonymous),
                                 SU.CreateInputParam("@LastActivityDate",DbType.DateTime,dt),
                             };
            SQLiteParameter[] memberParms = {
                                 SU.CreateInputParam("@ApplicationId",DbType.String,ApplicationId),
                                 SU.CreateInputParam("@UserId",DbType.String,userId.ToString("N")),
                                 SU.CreateInputParam("@Password",DbType.String,psw),
                                 SU.CreateInputParam("@PasswordFormat",DbType.Int32,(int)PasswordFormat),
                                 SU.CreateInputParam("@PasswordSalt",DbType.String,salt),
                                 SU.CreateInputParam("@MobilePIN",DbType.String,string.Empty),
                                 SU.CreateInputParam("@Email",DbType.String,email),
                                 SU.CreateInputParam("@LoweredEmail",DbType.String,email.ToLower()),
                                 SU.CreateInputParam("@PasswordQuestion",DbType.String,string.Empty),
                                 SU.CreateInputParam("@PasswordAnswer",DbType.String,string.Empty),
                                 SU.CreateInputParam("@IsApproved",DbType.Boolean,isApproved),
                                 SU.CreateInputParam("@IsLockedOut",DbType.Boolean,false),
                                 SU.CreateInputParam("@CreateDate",DbType.DateTime,dt),
                                 SU.CreateInputParam("@LastLoginDate",DbType.DateTime,dt),
                                 SU.CreateInputParam("@LastPasswordChangedDate",DbType.DateTime,dt),
                                 SU.CreateInputParam("@LastLockoutDate",DbType.DateTime,DateTime.MinValue),
                                 SU.CreateInputParam("@FailedPasswordAttemptCount",DbType.Int32,0),
                                 SU.CreateInputParam("@FailedPasswordAttemptWindowStart",DbType.DateTime,DateTime.MinValue),
                                 SU.CreateInputParam("@FailedPasswordAnswerAttemptCount",DbType.Int32,0),
                                 SU.CreateInputParam("@FailedPasswordAnswerAttemptWindowStart",DbType.DateTime,DateTime.MinValue),
                                 SU.CreateInputParam("@Comment",DbType.String,string.Empty)
                             };

            int iStatus = SQLiteHelper.ExecuteNonQuery(SqlConnectionString, CommandType.Text, Sql_Users_Insert, usersParms);
            if (iStatus > 0) SQLiteHelper.ExecuteNonQuery(SqlConnectionString, CommandType.Text, Sql_Membership_Insert, memberParms);

            if (iStatus < 1) 
            {
                status = MembershipCreateStatus.UserRejected;
                return null;
            }
            status = MembershipCreateStatus.Success;

            return new MembershipUser(this.Name,
                                       username,
                                       userId,
                                       email,
                                       string.Empty,
                                       null,
                                       isApproved,
                                       false,
                                       dt,
                                       dt,
                                       dt,
                                       dt,
                                       new DateTime(1754, 1, 1));
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) return false;

            string psw = string.Empty;
            int passwordFormat = 0;
            string passwordSalt = string.Empty;

            using (SQLiteDataReader reader = SQLiteHelper.ExecuteReader(SqlConnectionString, CommandType.Text, Sql_Users_Mem_FindUserInfoByName, SU.CreateInputParam("@LoweredUserName", DbType.String, username)))
            {
                if(!reader.Read())
                {
                    return false;
                }
                psw = reader.GetString(0);
                passwordFormat = reader.GetInt32(1);
                passwordSalt = reader.GetString(2);
            }
            if(EncodePassword(password,passwordFormat,passwordSalt) != psw) return false;

            return true;
        }

        private DateTime RoundToSeconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
        }

        internal string GenerateSalt()
        {
            byte[] buf = new byte[16];
            (new RNGCryptoServiceProvider()).GetBytes(buf);
            return Convert.ToBase64String(buf);
        }

        internal string EncodePassword(string pass, int passwordFormat, string salt)
        {
            if (passwordFormat == 0) // MembershipPasswordFormat.Clear
                return pass;

            byte[] bIn = Encoding.Unicode.GetBytes(pass);
            byte[] bSalt = Convert.FromBase64String(salt);
            byte[] bAll = new byte[bSalt.Length + bIn.Length];
            byte[] bRet = null;

            Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
            Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);
            if (passwordFormat == 1)
            {
                // MembershipPasswordFormat.Hashed
                HashAlgorithm s = HashAlgorithm.Create(Membership.HashAlgorithmType);
                bRet = s.ComputeHash(bAll);
            }
            else
            {
                bRet = EncryptPassword(bAll);
            }

            return Convert.ToBase64String(bRet);
        }

        internal string UnEncodePassword(string pass, int passwordFormat)
        {
            switch (passwordFormat)
            {
                case 0: // MembershipPasswordFormat.Clear:
                    return pass;
                case 1: // MembershipPasswordFormat.Hashed:
                    throw new ProviderException(SM.GetString(SM.Provider_can_not_decode_hashed_password));
                default:
                    byte[] bIn = Convert.FromBase64String(pass);
                    byte[] bRet = DecryptPassword(bIn);
                    if (bRet == null)
                        return null;
                    return Encoding.Unicode.GetString(bRet, 16, bRet.Length - 16);
            }
        }
    }
}
