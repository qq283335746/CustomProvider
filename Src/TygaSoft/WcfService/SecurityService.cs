using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Security;
using System.Web;
using System.Transactions;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TygaSoft.CustomProvider;
using TygaSoft.Model;
using TygaSoft.Model.WcfModel;

namespace TygaSoft.WcfService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class SecurityService:ISecurity
    {
        #region UsersAndRoles

        public string ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) return ResponseResult.Response(false, MC.Login_InvalidAccount,null);

            bool res = Membership.ValidateUser(username, password);

            return ResponseResult.Response(res, res ? MC.Login_Success : MC.Login_InvalidAccount, null);
        }

        //[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public string SaveUser(string username, string password, string email, bool isApproved)
        {
            try
            {
                //if (!HttpContext.Current.User.IsInRole("Administrators")) throw new ArgumentException(MC.Role_InvalidError);

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return ResponseResult.Response(false, MC.Submit_Params_InvalidError, "");
                }

                MembershipCreateStatus status;
                MembershipUser user;

                user = Membership.CreateUser(username, password, email, null, null, isApproved, out status);

                //using (TransactionScope scope = new TransactionScope())
                //{
                //    user = Membership.CreateUser(model.UserName, model.Password, model.Email, null, null, model.IsApproved, out status);
                //    if (roles != null && roles.Length > 0)
                //    {
                //        Roles.AddUserToRoles(model.UserName, roles);
                //    }

                //    scope.Complete();
                //}

                if (user == null)
                {
                    return ResponseResult.Response(false, EnumMembershipCreateStatus.GetStatusMessage(status), null);
                }

                return ResponseResult.Response(true, MC.Response_Ok, null);
            }
            catch (MembershipCreateUserException ex)
            {
                return ResponseResult.Response(false, EnumMembershipCreateStatus.GetStatusMessage(ex.StatusCode), null);
            }
            catch (HttpException ex)
            {
                return ResponseResult.Response(false, "" + MC.AlertTitle_Ex_Error + "：" + ex.Message, null);
            }
        }

        public string CreateRole(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName)) return ResponseResult.Response(false, MC.Request_Params_InvalidError, null);

                Roles.CreateRole(roleName);

                return ResponseResult.Response(true, MC.Response_Ok, null);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, null);
            }
        }

        public string AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            try
            {
                if ((usernames == null || !usernames.Any()) || (roleNames == null || !roleNames.Any())) return ResponseResult.Response(false, MC.Request_Params_InvalidError, null);
                
                Roles.AddUsersToRoles(usernames, roleNames);

                return ResponseResult.Response(true, MC.Response_Ok, null);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, null);
            }
        }

        public string SaveUserInRole(string userName, string roleName, bool isInRole)
        {
            try
            {
                if (!HttpContext.Current.User.IsInRole("Administrators")) throw new ArgumentException(MC.Role_InvalidError);

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return ResponseResult.Response(false, MC.GetString(MC.Request_InvalidArgument, "用户名"), "");
                }
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    return ResponseResult.Response(false, MC.GetString(MC.Request_InvalidArgument, "角色"), "");
                }

                if (isInRole)
                {
                    if (!Roles.IsUserInRole(userName, roleName))
                    {
                        Roles.AddUserToRole(userName, roleName);
                    }
                }
                else
                {
                    if (Roles.IsUserInRole(userName, roleName))
                    {
                        Roles.RemoveUserFromRole(userName, roleName);
                    }
                }

                return ResponseResult.Response(true, "调用成功", "");
            }
            catch (System.Configuration.Provider.ProviderException pex)
            {
                return ResponseResult.Response(false, pex.Message, "");
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }
        }

        public string SaveIsLockedOut(string userName)
        {
            try
            {
                if (!HttpContext.Current.User.IsInRole("Administrators")) throw new ArgumentException(MC.Role_InvalidError);

                MembershipUser user = Membership.GetUser(userName);
                if (user == null)
                {
                    return ResponseResult.Response(false, "当前用户不存在，请检查", "");
                }
                if (user.IsLockedOut)
                {
                    if (user.UnlockUser())
                    {
                        return ResponseResult.Response(false, "", "0");
                    }
                    else
                    {
                        return ResponseResult.Response(false, "操作失败，请联系管理员", "");
                    }
                }

                return ResponseResult.Response(false, "只有“已锁定”的用户才能执行此操作", "");
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }
        }

        public string SaveIsApproved(string userName)
        {
            try
            {
                if (!HttpContext.Current.User.IsInRole("Administrators")) throw new ArgumentException(MC.Role_InvalidError);

                MembershipUser user = Membership.GetUser(userName);
                if (user == null)
                {
                    return ResponseResult.Response(false, "当前用户不存在，请检查", "");
                }
                if (user.IsApproved)
                {
                    user.IsApproved = false;
                }
                else
                {
                    user.IsApproved = true;
                }

                Membership.UpdateUser(user);

                return ResponseResult.Response(user.IsApproved, user.IsApproved ? "调用成功" : "", user.IsApproved ? "1" : "0");
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }
        }

        public string GetUserList(ListModel model)
        {
            try
            {
                var totalRecord = 0;
                var users = Membership.GetAllUsers((model.PageIndex - 1), model.PageSize, out totalRecord);
                var list = new List<ComboboxInfo>();
                foreach (MembershipUser user in users)
                {
                    list.Add(new ComboboxInfo(user.ProviderUserKey.ToString(), user.UserName));
                }
                return ResponseResult.Response(true, "", "{\"total\":" + totalRecord + ",\"rows\":" + JsonConvert.SerializeObject(list) + "}");
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }

        }

        public string GetAllRoles()
        {
            object[] data = {
                           Roles.GetAllRoles()
                       };
            return ResponseResult.Response(true, MC.Response_Ok, data);
        }

        public string GetRolesForUser(string userName)
        {
            try
            {
                //MenusDataProxy.ValidateAccess((int)EnumData.EnumOperationAccess.浏览, true);

                string[] roles = Roles.GetRolesForUser(userName);
                if (roles.Length == 0) return ResponseResult.Response(false, "", "");

                return ResponseResult.Response(true, "调用成功", string.Join(",", roles));
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }
        }

        public string GetUsersInRole(string roleName)
        {
            try
            {
                var result = Roles.GetUsersInRole(roleName);
                return ResponseResult.Response(true, MC.Response_Ok, result);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, null);
            }
        }

        public string FindUsersInRole(string roleName, string usernameToMatch)
        {
            try
            {
                var result = Roles.FindUsersInRole(roleName, usernameToMatch);
                return ResponseResult.Response(true, MC.Response_Ok, result);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, null);
            }
        }

        public string RoleExists(string roleName)
        {
            try
            {
                var result = Roles.RoleExists(roleName);
                return ResponseResult.Response(true, MC.Response_Ok, result);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, null);
            }
        }

        public string IsUserInRole(string username, string roleName)
        {
            try
            {
                var result = Roles.IsUserInRole(username, roleName);
                return ResponseResult.Response(true, MC.Response_Ok, result);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, null);
            }
        }

        public string DeleteUser(string userName)
        {
            try
            {
                if (!HttpContext.Current.User.IsInRole("Administrators")) throw new ArgumentException(MC.Role_InvalidError);

                if (!Membership.DeleteUser(userName)) return ResponseResult.Response(false, MC.M_Save_Error, "");

                return ResponseResult.Response(true, "", "");
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, "" + MC.AlertTitle_Ex_Error + "：" + ex.Message, "");
            }
        }

        public string DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            try
            {
                var result = Roles.DeleteRole(roleName, throwOnPopulatedRole);

                return ResponseResult.Response(result, result ? MC.Response_Ok : MC.M_Save_Error, null);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, null);
            }
        }

        public string RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            try
            {
                Roles.RemoveUsersFromRoles(usernames, roleNames);

                return ResponseResult.Response(true, MC.Response_Ok, null);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, "" + MC.AlertTitle_Ex_Error + "：" + ex.Message, "");
            }
        }

        public string ResetPassword(string username)
        {
            try
            {
                if (!Membership.EnablePasswordReset)
                {
                    return ResponseResult.Response(false, "系统不允许重置密码操作，请联系管理员", "");
                }
                var user = Membership.GetUser(username);
                if (user == null)
                {
                    return ResponseResult.Response(false, "用户【" + username + "】不存在或已被删除，请检查", "");
                }
                string rndPsw = new Random().Next(100000, 999999).ToString();
                if (!user.ChangePassword(user.ResetPassword(), rndPsw))
                {
                    return ResponseResult.Response(false, "重置密码失败，请稍后再重试", "");
                }

                return ResponseResult.Response(true, "调用成功", rndPsw);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }
        }

        public string CheckUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return ResponseResult.Response(false, "参数不能为空字符串", "-1");
            }

            try
            {
                MembershipUser user = Membership.GetUser(userName);
                if (user != null)
                {
                    return ResponseResult.Response(true, "调用成功", 1);
                }

                return ResponseResult.Response(true, "调用成功", 0);
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }
        }

        public string ChangePassword(string username, string oldPassword, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username)) username = HttpContext.Current.User.Identity.Name;
                if (!Regex.IsMatch(password, Membership.PasswordStrengthRegularExpression)) return ResponseResult.Response(false, MC.Login_InvalidPassword, "");
                if (!Membership.ValidateUser(username, oldPassword)) return ResponseResult.Response(false, MC.Login_InvalidOldPsw, "");
                if (!Membership.GetUser(username).ChangePassword(oldPassword, password)) return ResponseResult.Response(false, MC.M_Save_Error, "");

                return ResponseResult.Response(true, "", "");
            }
            catch (Exception ex)
            {
                return ResponseResult.Response(false, ex.Message, "");
            }
        }

        #endregion
    }
}
