using System;
using Yibi.LiteDbMembershipProvider.Enums;

namespace Yibi.LiteDbMembershipProvider.Entities
{
    public class PasswordInfo
    {
        public string Password { get; set; }

        public string PasswordSalt { get; set; }

        public PasswordFormatOptions PasswordFormat { get; set; }
    }
}
