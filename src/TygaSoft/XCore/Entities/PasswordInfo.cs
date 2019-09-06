using System;
using System.Collections.Generic;
using System.Text;

namespace Yibi.Core.Entities
{
    public class PasswordInfo
    {
        public string Password { get; set; }

        public string PasswordSalt { get; set; }

        public PasswordFormatOptions PasswordFormat { get; set; }
    }
}
