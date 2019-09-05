using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yibi.Core;
using Yibi.Core.Entities;

namespace Yibi.Repositories.LiteDB
{
    public class AspnetSecurityService
    {
        private LiteDbContext _db;

        public AspnetSecurityService(LiteDbContext db)
        {
            _db = db;
        }

        public ApplicationInfo GetApplication(string applicationName)
        {
            return _db.Applications.FindOne(m => m.Name.Equals(applicationName));
        }

        public Guid Insert(ApplicationInfo model)
        {
            var effect = _db.Applications.Insert(model);

            return effect.AsGuid;
        }

        public bool ValidateUser(Guid applicationId, PasswordFormatOptions passwordFormat, string username, string password)
        {
            if (passwordFormat == PasswordFormatOptions.Clear)
            {
                return _db.Users.Exists(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase) && m.Password.Equals(password));
            }
            else if (passwordFormat == PasswordFormatOptions.Hashed)
            {
                UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (userInfo != null)
                {
                    return EncryptHelper.EncodePassword(password, PasswordFormatOptions.Hashed, userInfo.PasswordSalt) == userInfo.Password;
                }
            }
            else if (passwordFormat == PasswordFormatOptions.Encrypted)
            {
                UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (userInfo != null)
                {
                    return EncryptHelper.UnEncodePassword(password, PasswordFormatOptions.Hashed, userInfo.PasswordSalt) == userInfo.Password;
                }
            }

            return false;
        }

        public UsersInfo CreateUser(Guid applicationId, PasswordFormatOptions passwordFormat, string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey)
        {
            if (IsExistUser(applicationId, username)) return null;

            PasswordInfo passwordInfo = GetPasswordInfo(passwordFormat,password);
            DateTime currentTime = DateTime.Now;

            var userInfo = new UsersInfo
            {
                ApplicationId = applicationId,
                Id = providerUserKey == null ? Guid.NewGuid() : (Guid)providerUserKey,
                Name = username,
                Password = passwordInfo.Password,
                PasswordSalt = passwordInfo.PasswordSalt,
                PasswordFormat = passwordInfo.PasswordFormat,
                Email = email,
                CreatedDate = currentTime,
                LastUpdatedDate = currentTime
            };

            _db.Users.Insert(userInfo);

            return userInfo;
        }

        public bool DeleteUser(Guid applicationId, string username, bool deleteAllRelatedData)
        {
            return _db.Users.Delete(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        public IEnumerable<UsersInfo> FindUsersByEmail(Guid applicationId, string email, int pageIndex, int pageSize, out int totalRecords)
        {
            var users = _db.Users.Find(m => m.ApplicationId.Equals(applicationId) && m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (users != null)
            {
                totalRecords = users.Count();
            }
            else totalRecords = 0;

            return users.Skip(pageIndex * pageSize).Take(pageSize);
        }

        public IEnumerable<UsersInfo> FindUsersByName(Guid applicationId, string username, int pageIndex, int pageSize, out int totalRecords)
        {
            var users = _db.Users.Find(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (users != null)
            {
                totalRecords = users.Count();
            }
            else totalRecords = 0;

            return users.Skip(pageIndex * pageSize).Take(pageSize);
        }

        public IEnumerable<UsersInfo> GetAllUsers(Guid applicationId, int pageIndex, int pageSize, out int totalRecords)
        {
            var users = _db.Users.FindAll();
            totalRecords = users.Count();

            return users.Skip(pageIndex * pageSize).Take(pageSize);
        }

        public UsersInfo GetUser(Guid applicationId, object providerUserKey, bool userIsOnline)
        {
            return _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Id.Equals((Guid)providerUserKey));
        }

        public UsersInfo GetUser(Guid applicationId, string username, bool userIsOnline)
        {
            return _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username,StringComparison.OrdinalIgnoreCase));
        }

        public string GetUserNameByEmail(Guid applicationId, string email)
        {
            UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (userInfo == null) return null;

            return userInfo.Name;
        }

        public string GetPassword(Guid applicationId, string username, string answer)
        {
            UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (userInfo == null) return null;

            return userInfo.Password;
        }

        public bool ChangePassword(Guid applicationId, PasswordFormatOptions passwordFormat, string username, string oldPassword, string newPassword)
        {
            UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (userInfo == null) return false;

            if (!PasswordChecked(userInfo,oldPassword)) return false;

            return PasswordUpdated(userInfo, newPassword, passwordFormat);
        }

        public string ResetPassword(Guid applicationId, PasswordFormatOptions passwordFormat, string username, string answer)
        {
            UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (userInfo == null) return null;

            PasswordInfo passwordInfo = GeneratePassword(passwordFormat);
            userInfo.Password = passwordInfo.Password;
            userInfo.PasswordFormat = passwordFormat;
            userInfo.PasswordSalt = passwordInfo.PasswordSalt;

            if (_db.Users.Update(userInfo)) return userInfo.Password;

            return null;
        }

        public bool UnlockUser(Guid applicationId, string username)
        {
            UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (userInfo == null) return false;

            userInfo.IsLockedOut = false;
            userInfo.LastLockoutDate = DateTime.Now;

            return _db.Users.Update(userInfo);
        }

        public void UpdateUser(UsersInfo userInfo)
        {
            UsersInfo oldUserInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(userInfo.ApplicationId) && m.Name.Equals(userInfo.Name, StringComparison.OrdinalIgnoreCase));
            oldUserInfo.IsApproved = userInfo.IsApproved;
            oldUserInfo.IsLockedOut = userInfo.IsLockedOut;
            oldUserInfo.LastActivityDate = userInfo.LastActivityDate;
            oldUserInfo.LastLoginDate = userInfo.LastLoginDate;
            oldUserInfo.LastLockoutDate = userInfo.LastLockoutDate;
            oldUserInfo.LastUpdatedDate = DateTime.Now;

            _db.Users.Update(oldUserInfo);
        }

        public int GetNumberOfUsersOnline()
        {
            IEnumerable<UsersInfo> users = _db.Users.Find(m => (DateTime.Now - m.LastActivityDate).TotalMinutes <= 30);

            return users == null ? 0 : users.Count();
        }

        public string[] GetAllRoles(Guid applicationId)
        {
            return _db.Roles.Find(m=>m.ApplicationId.Equals(applicationId)).Select(m=>m.Name).ToArray();
        }

        public string[] FindUsersInRole(Guid applicationId,string roleName, string usernameToMatch)
        {
            RolesInfo roleInfo = _db.Roles.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (roleInfo == null) return null;

            Guid[] userids = _db.UsersInRoles.Find(m => m.RoleId.Equals(roleInfo.Id)).Select(m => m.UserId).Distinct().ToArray();

            if (userids == null || !userids.Any())
            {
                return null;
            }

            string[] usernames = _db.Users.Find(m => userids.Contains(m.Id)).Select(m => m.Name).ToArray();
            if (!string.IsNullOrEmpty(usernameToMatch))
            {
                string name = usernames.FirstOrDefault(m => m.Contains(usernameToMatch));
                if (!string.IsNullOrEmpty(name))
                {
                    return new string[] { name };
                }
            }

            return usernames;   
        }

        public string[] GetUsersInRole(Guid applicationId, string roleName)
        {
            RolesInfo roleInfo = _db.Roles.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            Guid[] userids = _db.UsersInRoles.Find(m => m.RoleId.Equals(roleInfo.Id)).Select(m => m.UserId).ToArray();
            if (userids == null || !userids.Any()) return null;

            return _db.Users.Find(m => userids.Contains(m.Id)).Select(m => m.Name).ToArray();
        }

        public string[] GetRolesForUser(Guid applicationId, string username)
        {
            UsersInfo userInfo = _db.Users.FindOne(m => m.Id.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            Guid[] roleids = _db.UsersInRoles.Find(m => m.UserId.Equals(userInfo.Id)).Select(m=>m.RoleId).ToArray();
            if (roleids == null || !roleids.Any()) return null;

            return _db.Roles.Find(m => roleids.Contains(m.Id)).Select(m => m.Name).ToArray();
        }

        public void CreateRole(Guid applicationId,string rolename)
        {
            _db.Roles.Insert(new RolesInfo { ApplicationId = applicationId, Name = rolename });
        }

        public bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            return _db.Roles.Delete(m => m.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        public bool IsUserInRole(Guid applicationId, string username, string roleName)
        {
            UsersInfo userInfo = _db.Users.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            RolesInfo roleInfo = _db.Roles.FindOne(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));

            return _db.UsersInRoles.FindOne(m => m.UserId.Equals(userInfo.Id) && m.RoleId.Equals(roleInfo.Id)) != null;
        }

        public void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {

        }

        private bool IsExistUser(Guid applicationId,string username)
        {
            return _db.Users.Exists(m => m.ApplicationId.Equals(applicationId) && m.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        private bool PasswordChecked(UsersInfo userInfo,string oldPassword)
        {
            if (userInfo.PasswordFormat == PasswordFormatOptions.Clear)
            {
                if (!userInfo.Password.Equals(oldPassword)) return false;
            }
            else if (userInfo.PasswordFormat == PasswordFormatOptions.Hashed)
            {
                if (!userInfo.Password.Equals(EncryptHelper.EncodePassword(oldPassword, userInfo.PasswordFormat, userInfo.PasswordSalt))) return false;
            }
            else if (userInfo.PasswordFormat == PasswordFormatOptions.Encrypted)
            {
                if (!userInfo.Password.Equals(EncryptHelper.UnEncodePassword(oldPassword, userInfo.PasswordFormat, userInfo.PasswordSalt))) return false;
            }

            return true;
        }

        private bool PasswordUpdated(UsersInfo userInfo,string password, PasswordFormatOptions passwordFormat)
        {
            if (passwordFormat == PasswordFormatOptions.Clear)
            {
                userInfo.Password = password;
                return _db.Users.Update(userInfo);
            }
            else
            {
                string salt = EncryptHelper.GenerateSalt();
                userInfo.Password = EncryptHelper.EncodePassword(password, passwordFormat, salt);
                userInfo.PasswordSalt = salt;
                userInfo.PasswordFormat = passwordFormat;

                return _db.Users.Update(userInfo);
            }
        }

        private PasswordInfo GetPasswordInfo(PasswordFormatOptions passwordFormat,string password)
        {
            if(passwordFormat == PasswordFormatOptions.Clear)
            {
                return new PasswordInfo { Password = password, PasswordSalt = string.Empty, PasswordFormat = passwordFormat };
            }
            else
            {
                string salt = EncryptHelper.GenerateSalt();

                return new PasswordInfo { Password = EncryptHelper.EncodePassword(password, passwordFormat,salt), PasswordSalt = salt, PasswordFormat = passwordFormat };
            }
        }

        private PasswordInfo GeneratePassword(PasswordFormatOptions passwordFormat)
        {
            string password = EncryptHelper.GeneratePassword(6);

            if (passwordFormat == PasswordFormatOptions.Clear)
            {
                return new PasswordInfo { PasswordFormat = passwordFormat, Password = password };
            }
            else
            {
                string salt = EncryptHelper.GenerateSalt();
                return new PasswordInfo { PasswordFormat = passwordFormat, Password = EncryptHelper.EncodePassword(password, passwordFormat, salt), PasswordSalt = salt };
            }
        }
    }
}
