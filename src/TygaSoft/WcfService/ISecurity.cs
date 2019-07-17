using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using TygaSoft.Model;
using TygaSoft.Model.WcfModel;

namespace TygaSoft.WcfService
{
    [ServiceContract(Namespace = "http://TygaSoft.Services.SecurityService")]
    public interface ISecurity
    {
        #region UsersAndRoles

        [OperationContract(Name = "ValidateUser")]
        string ValidateUser(string username, string password);

        [OperationContract(Name = "CreateRole")]
        string CreateRole(string roleName);

        [OperationContract(Name = "SaveUser")]
        string SaveUser(string username, string password, string email, bool isApproved);

        [OperationContract(Name = "SaveUserInRole")]
        string SaveUserInRole(string userName, string roleName, bool isInRole);

        [OperationContract(Name = "SaveIsLockedOut")]
        string SaveIsLockedOut(string userName);

        [OperationContract(Name = "SaveIsApproved")]
        string SaveIsApproved(string userName);

        [OperationContract(Name = "AddUsersToRoles")]
        string AddUsersToRoles(string[] usernames, string[] roleNames);

        [OperationContract(Name = "GetAllRoles")]
        string GetAllRoles();

        [OperationContract(Name = "GetUserList")]
        string GetUserList(ListModel model);

        [OperationContract(Name = "GetRolesForUser")]
        string GetRolesForUser(string userName);

        [OperationContract(Name = "GetUsersInRole")]
        string GetUsersInRole(string roleName);

         [OperationContract(Name = "FindUsersInRole")]
        string FindUsersInRole(string roleName, string usernameToMatch);

        [OperationContract(Name = "RoleExists")]
        string RoleExists(string roleName);

        [OperationContract(Name = "IsUserInRole")]
        string IsUserInRole(string username, string roleName);

        [OperationContract(Name = "DeleteUser")]
        string DeleteUser(string userName);

        [OperationContract(Name = "DeleteRole")]
        string DeleteRole(string roleName, bool throwOnPopulatedRole);

        [OperationContract(Name = "RemoveUsersFromRoles")]
        string RemoveUsersFromRoles(string[] usernames, string[] roleNames);

        [OperationContract(Name = "ResetPassword")]
        string ResetPassword(string username);

        [OperationContract(Name = "CheckUserName")]
        string CheckUserName(string userName);

        [OperationContract(Name = "ChangePassword")]
        string ChangePassword(string username, string oldPassword, string password);

        #endregion
    }
}
