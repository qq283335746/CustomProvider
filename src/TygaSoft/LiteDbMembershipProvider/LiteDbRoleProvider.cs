using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web.Security;
using Yibi.LiteDbMembershipProvider.Entities;

namespace Yibi.LiteDbMembershipProvider
{
    public class LiteDbRoleProvider : RoleProvider
    {
        private AspnetSecurityService _rolesService;
        public string _applicationName;
        private string _description;

        private string _connectionString;
        private int _commandTimeout;
        private int _schemaVersionCheck;

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            if (string.IsNullOrEmpty(name))
                name = "LiteDbRoleProvider";

            base.Initialize(name, config);

            _connectionString = ConfigurationManager.ConnectionStrings[config["connectionStringName"]].ConnectionString;

            _applicationName = config["applicationName"];
            _description = config["description"];

            config.Remove("connectionStringName");
            config.Remove("applicationName");
            config.Remove("commandTimeout");

            _rolesService = new AspnetSecurityService(new LiteDbContext(_connectionString));
        }

        private Guid _applicationId;
        public Guid ApplicationId
        {
            get
            {
                if (_applicationId == null || _applicationId.Equals(Guid.Empty))
                {
                    ApplicationInfo applicationInfo = _rolesService.GetApplication(ApplicationName);
                    if (applicationInfo != null) _applicationId = applicationInfo.Id;
                }

                return _applicationId;
            }
        }

        public override string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                _applicationName = value;
            }
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            _rolesService.AddUsersToRoles(ApplicationId, usernames, roleNames);
        }

        public override void CreateRole(string roleName)
        {
            _rolesService.CreateRole(ApplicationId, roleName);
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            return _rolesService.DeleteRole(roleName, throwOnPopulatedRole);
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            return _rolesService.FindUsersInRole(ApplicationId, roleName, usernameToMatch);
        }

        public override string[] GetAllRoles()
        {
            return _rolesService.GetAllRoles(ApplicationId);
        }

        public override string[] GetRolesForUser(string username)
        {
            return _rolesService.GetRolesForUser(ApplicationId,username);
        }

        public override string[] GetUsersInRole(string roleName)
        {
            return _rolesService.GetUsersInRole(ApplicationId, roleName);
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            return _rolesService.IsUserInRole(ApplicationId, username, roleName);
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            _rolesService.RemoveUsersFromRoles(ApplicationId, usernames, roleNames);
        }

        public override bool RoleExists(string roleName)
        {
            return _rolesService.RoleExists(ApplicationId, roleName);
        }
    }
}
