using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using System.Web.Security;
using Yibi.LiteDbMembershipProvider.Entities;
using Yibi.LiteDbMembershipProvider.Enums;

namespace Yibi.LiteDbMembershipProvider
{
    public class LiteDbMembershipProvider : MembershipProvider
    {
        private AspnetSecurityService _membershipService;
        private MembershipPasswordFormat _passwordFormat;

        private string _connectionString;
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
        private string _description;

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            if (string.IsNullOrEmpty(name))
                name = "LiteDbMembershipProvider";

            base.Initialize(name, config);

            _schemaVersionCheck = 0;

            _connectionString = ConfigurationManager.ConnectionStrings[config["connectionStringName"]].ConnectionString;

            _applicationName = config["applicationName"];
            _description = config["description"];
            _passwordStrengthRegularExpression = config["passwordStrengthRegularExpression"];

            bool.TryParse(config["enablePasswordRetrieval"], out _enablePasswordRetrieval);
            bool.TryParse(config["enablePasswordReset"], out _enablePasswordReset);
            bool.TryParse(config["requiresQuestionAndAnswer"], out _requiresQuestionAndAnswer);
            bool.TryParse(config["requiresUniqueEmail"], out _requiresUniqueEmail);
            int.TryParse(config["maxInvalidPasswordAttempts"], out _maxInvalidPasswordAttempts);
            int.TryParse(config["passwordAttemptWindow"], out _passwordAttemptWindow);
            int.TryParse(config["minRequiredPasswordLength"], out _minRequiredPasswordLength);
            int.TryParse(config["minRequiredNonalphanumericCharacters"], out _minRequiredNonalphanumericCharacters);

            string passwordFormat = config["passwordFormat"];
            if (passwordFormat == null) passwordFormat = "Hashed";

            switch (passwordFormat)
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
                    _passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
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

            _membershipService = new AspnetSecurityService(new LiteDbContext(_connectionString));

            var oldApplicationInfo = _membershipService.GetApplication(ApplicationName);
            if(oldApplicationInfo == null)
            {
                _membershipService.Insert(new ApplicationInfo { Id = Guid.NewGuid(), Name = ApplicationName });
            }
        }

        public override bool EnablePasswordRetrieval => _enablePasswordRetrieval;

        public override bool EnablePasswordReset => _enablePasswordReset;

        public override bool RequiresQuestionAndAnswer => _requiresQuestionAndAnswer;

        private string _applicationName;
        public override string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                _applicationName = value;
            }
        }

        public override int MaxInvalidPasswordAttempts => _maxInvalidPasswordAttempts;

        public override int PasswordAttemptWindow => _passwordAttemptWindow;

        public override bool RequiresUniqueEmail => _requiresUniqueEmail;

        public override MembershipPasswordFormat PasswordFormat => _passwordFormat;

        private Guid _applicationId;
        public Guid ApplicationId
        {
            get
            {
                if (_applicationId == null || _applicationId.Equals(Guid.Empty))
                {
                    ApplicationInfo applicationInfo = _membershipService.GetApplication(ApplicationName);
                    if (applicationInfo != null) _applicationId = applicationInfo.Id;
                }

                return _applicationId;
            }
        }

        public PasswordFormatOptions PasswordFormatOptions
        {
            get
            {
                switch (PasswordFormat)
                {
                    case MembershipPasswordFormat.Clear:
                        return PasswordFormatOptions.Clear;
                    case MembershipPasswordFormat.Hashed:
                        return PasswordFormatOptions.Hashed;
                    case MembershipPasswordFormat.Encrypted:
                        return PasswordFormatOptions.Encrypted;
                    default:
                        return PasswordFormatOptions.Clear;
                }
            }
        }

        public override int MinRequiredPasswordLength => _minRequiredPasswordLength;

        public override int MinRequiredNonAlphanumericCharacters => _minRequiredNonalphanumericCharacters;

        public override string PasswordStrengthRegularExpression => _passwordStrengthRegularExpression;

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            return _membershipService.ChangePassword(ApplicationId, PasswordFormatOptions, username, oldPassword, newPassword);
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            UsersInfo userInfo = _membershipService.CreateUser(ApplicationId, PasswordFormatOptions, username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey);
            if(userInfo == null)
            {
                status = MembershipCreateStatus.ProviderError;

                return null;
            }

            status = MembershipCreateStatus.Success;

            return FromUsersInfo(userInfo);
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            return _membershipService.DeleteUser(ApplicationId, username, deleteAllRelatedData);
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection muc = new MembershipUserCollection();

            IEnumerable<UsersInfo> users = _membershipService.FindUsersByEmail(ApplicationId, emailToMatch, pageIndex, pageSize, out totalRecords);
            foreach(UsersInfo userInfo in users)
            {
                muc.Add(FromUsersInfo(userInfo));
            }

            return muc;
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection muc = new MembershipUserCollection();

            IEnumerable<UsersInfo> users = _membershipService.FindUsersByName(ApplicationId, usernameToMatch, pageIndex, pageSize, out totalRecords);
            foreach (UsersInfo userInfo in users)
            {
                muc.Add(FromUsersInfo(userInfo));
            }

            return muc;
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection muc = new MembershipUserCollection();

            IEnumerable<UsersInfo> users = _membershipService.GetAllUsers(ApplicationId, pageIndex, pageSize, out totalRecords);
            foreach (UsersInfo userInfo in users)
            {
                muc.Add(FromUsersInfo(userInfo));
            }

            return muc;
        }

        public override int GetNumberOfUsersOnline()
        {
            return _membershipService.GetNumberOfUsersOnline();
        }

        public override string GetPassword(string username, string answer)
        {
            return _membershipService.GetPassword(ApplicationId, username, answer);
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            return FromUsersInfo(_membershipService.GetUser(ApplicationId, providerUserKey, userIsOnline));
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            return FromUsersInfo(_membershipService.GetUser(ApplicationId, username, userIsOnline));
        }

        public override string GetUserNameByEmail(string email)
        {
            return _membershipService.GetUserNameByEmail(ApplicationId, email);
        }

        public override string ResetPassword(string username, string answer)
        {
            return _membershipService.ResetPassword(ApplicationId, PasswordFormatOptions, username, answer);
        }

        public override bool UnlockUser(string userName)
        {
            return _membershipService.UnlockUser(ApplicationId, userName);
        }

        public override void UpdateUser(MembershipUser user)
        {
            _membershipService.UpdateUser(ToUsersInfo(user));
        }

        public override bool ValidateUser(string username, string password)
        {
            return _membershipService.ValidateUser(ApplicationId,PasswordFormatOptions, username, password);
        }

        private MembershipUser FromUsersInfo(UsersInfo userInfo)
        {
            if (userInfo == null) return null;
            return new MembershipUser(nameof(LiteDbMembershipProvider), userInfo.Name, userInfo.Id, userInfo.Email, string.Empty, string.Empty, userInfo.IsApproved, userInfo.IsLockedOut, userInfo.CreatedDate, userInfo.LastLoginDate, userInfo.LastActivityDate, userInfo.LastUpdatedDate, userInfo.LastLockoutDate);
        }

        private UsersInfo ToUsersInfo(MembershipUser user)
        {
            return new UsersInfo { ApplicationId = this.ApplicationId, Id = (Guid)user.ProviderUserKey, Name = user.UserName, Email = user.Email, IsLockedOut = user.IsLockedOut, IsApproved = user.IsApproved, LastActivityDate = user.LastActivityDate, LastLoginDate = user.LastLoginDate, LastPasswordChangedDate = user.LastPasswordChangedDate, LastLockoutDate = user.LastLockoutDate };
        }
    }
}
