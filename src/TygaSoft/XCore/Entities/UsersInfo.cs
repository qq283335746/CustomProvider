using System;
using System.Collections.Generic;
using System.Text;

namespace Yibi.Core.Entities
{
    public class UsersInfo
    {
        public Guid ApplicationId { get; set; }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public string PasswordSalt { get; set; }

        public PasswordFormatOptions PasswordFormat { get; set; }

        public string Email { get; set; }

        /// <summary>
        /// 获取一个值，该值指示成员资格用户是否因被锁定而无法进行验证
        /// </summary>
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// 获取或设置一个值，表示是否可以对成员资格用户进行身份验证
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// 获取或设置成员资格用户上次进行身份验证或访问应用程序的日期和时间
        /// </summary>
        public DateTime LastActivityDate { get; set; }

        /// <summary>
        /// 获取或设置用户上次进行身份验证的日期和时间。
        /// </summary>
        public DateTime LastLoginDate { get; set; }

        /// <summary>
        /// 获取上次更新成员资格用户的密码的日期和时间
        /// </summary>
        public DateTime LastPasswordChangedDate { get; set; }

        /// <summary>
        /// 获取最近一次锁定成员资格用户的日期和时间
        /// </summary>
        public DateTime LastLockoutDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastUpdatedDate { get; set; }
    }
}
