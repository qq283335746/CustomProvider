using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;

namespace LiteDbMembershipProviderTest
{
    class Program
    {
        private const string DefaultUserName = "admin";
        private const string DefaultRoleName = "Administrators";

        static void Main(string[] args)
        {
            var roles = Roles.GetAllRoles();
            if (!roles.Any(m => m.Equals(DefaultRoleName, StringComparison.OrdinalIgnoreCase)))
            {
                Roles.CreateRole(DefaultRoleName);
            }

            if (!Membership.ValidateUser(DefaultUserName, DefaultUserName + "123456"))
            {
                var user = Membership.GetUser(DefaultUserName);
                if (user != null)
                {
                    Membership.DeleteUser(DefaultUserName, true);
                }

                user = Membership.CreateUser(DefaultUserName, DefaultUserName + "123456");
            }
            if (!Roles.IsUserInRole(DefaultUserName, DefaultRoleName))
            {
                Roles.AddUserToRole(DefaultUserName, DefaultRoleName);
            }
        }
    }
}
