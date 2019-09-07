using System;
using Yibi.LiteMembershipProvider.Enums;

namespace Yibi.LiteMembershipProvider.Entities
{
    public class PasswordInfo
    {
        public string Password { get; set; }

        public string PasswordSalt { get; set; }

        public PasswordFormatOptions PasswordFormat { get; set; }
    }
}
