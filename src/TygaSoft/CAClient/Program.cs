using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TygaSoft.CAClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var sResult = string.Empty;

            //var authClient = new AuthenticationServiceClient();
            //var result = authClient.Login("admin", "admin111",string.Empty,true);

            var secService = new SecurityClient();
            //sResult = secService.SaveUser("283335746", "283335746", "283335746@qq.com", true);
            //sResult = secService.ValidateUser("admin", "admin111");

            //secService.SaveRole("Administrator");
            //sResult = secService.CreateRole("System");
            //sResult = secService.DeleteRole("System", false);
            //sResult = secService.RoleExists("Administrator");
            //sResult = secService.GetAllRoles();

            string[] usernames = {"admin","283335746"};
            string[] roleNames = {"Administrator"};
            //sResult = secService.AddUsersToRoles(usernames, roleNames);
            //sResult = secService.GetUsersInRole("Administrator");
            //sResult = secService.IsUserInRole("admin", "Administrator");
            sResult = secService.GetRolesForUser("admin");
            
        }
    }
}
