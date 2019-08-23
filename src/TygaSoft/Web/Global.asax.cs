using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace TygaSoft.SecurityWeb
{
    public class Global : System.Web.HttpApplication
    {
        private const string DefaultUserName = "admin";
        private const string DefaultRoleName = "Administrators";

        protected void Application_Start(object sender, EventArgs e)
        {
            if (!Membership.ValidateUser(DefaultUserName, DefaultUserName + "123456"))
            {
                var user = Membership.GetUser(DefaultUserName);
                if (user != null)
                {
                    Membership.DeleteUser(DefaultUserName, true);
                }

                user = Membership.CreateUser(DefaultUserName, DefaultUserName + "123456");
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}