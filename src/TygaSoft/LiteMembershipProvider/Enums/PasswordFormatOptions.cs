using System;

namespace Yibi.LiteMembershipProvider.Enums
{
    public enum PasswordFormatOptions
    {
        Clear = 0,
        Hashed = 1,
        //摘要:密码使用由 machineKey Element (ASP.NET Settings Schema) 元素配置确定的加密设置进行加密。  
        Encrypted = 2
    }
}
