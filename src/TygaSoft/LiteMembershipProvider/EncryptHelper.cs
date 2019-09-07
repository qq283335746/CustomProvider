using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Yibi.LiteMembershipProvider.Enums;

namespace Yibi.LiteMembershipProvider
{
    public class EncryptHelper
    {
        private const string _sourceDefault = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static List<char> _sourceDefaultDatas => _sourceDefault.ToCharArray().ToList();

        /// <summary>
        /// 密码加密
        /// </summary>
        /// <param name="pass"></param>
        /// <param name="passwordFormat">枚举PasswordFormat</param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static string EncodePassword(string password, PasswordFormatOptions formatOptions, string salt)
        {
            if (formatOptions == PasswordFormatOptions.Clear) return password;

            var hm = GetHashAlgorithm(formatOptions);

            //Hashed：不可逆，不能解密
            if (formatOptions == PasswordFormatOptions.Hashed)
            {
                byte[] bIn = Encoding.Unicode.GetBytes(password);
                byte[] bSalt = Convert.FromBase64String(salt);
                byte[] bRet = null;
                if (hm is KeyedHashAlgorithm)
                {
                    KeyedHashAlgorithm kha = (KeyedHashAlgorithm)hm;
                    if (kha.Key.Length == bSalt.Length)
                    {
                        kha.Key = bSalt;
                    }
                    else if (kha.Key.Length < bSalt.Length)
                    {
                        byte[] bKey = new byte[kha.Key.Length];
                        Buffer.BlockCopy(bSalt, 0, bKey, 0, bKey.Length);
                        kha.Key = bKey;
                    }
                    else
                    {
                        byte[] bKey = new byte[kha.Key.Length];
                        for (int iter = 0; iter < bKey.Length;)
                        {
                            int len = Math.Min(bSalt.Length, bKey.Length - iter);
                            Buffer.BlockCopy(bSalt, 0, bKey, iter, len);
                            iter += len;
                        }
                        kha.Key = bKey;
                    }
                    bRet = kha.ComputeHash(bIn);
                }
                else
                {
                    byte[] bAll = new byte[bSalt.Length + bIn.Length];
                    Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
                    Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);
                    bRet = hm.ComputeHash(bAll);
                }

                return Convert.ToBase64String(bRet);
            }
            else
            {
                return Convert.ToBase64String(hm.ComputeHash(Encoding.UTF8.GetBytes(password)));
            }
        }

        /// <summary>
        /// 密码解密
        /// </summary>
        /// <param name="pass"></param>
        /// <param name="passwordFormat">枚举PasswordFormat</param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static string UnEncodePassword(string pass, PasswordFormatOptions formatOptions, string salt)
        {
            switch (formatOptions)
            {
                case PasswordFormatOptions.Clear: // Clear:
                    return pass;
                case PasswordFormatOptions.Hashed: //Hashed:
                    throw new ArgumentException("UnEncodePassword.Hashed");
                default:
                    return pass;
            }
        }

        /// <summary>
        /// 获取加密哈希算法实现
        /// </summary>
        /// <param name="passwordFormat"></param>
        /// <returns></returns>
        public static HashAlgorithm GetHashAlgorithm(PasswordFormatOptions formatOptions)
        {
            return SHA256.Create();
        }

        /// <summary>
        /// 生成随机字节值的强密码序列。
        /// </summary>
        /// <returns></returns>
        public static string GenerateSalt()
        {
            byte[] buf = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buf);
            }
            return Convert.ToBase64String(buf);
        }

        public static string GeneratePassword(int n)
        {
            return string.Join("", CreateRandomCodes(_sourceDefaultDatas, n));
        }

        public static IEnumerable<char> CreateRandomCodes(List<char> datas, int n)
        {
            byte[] bytes = new byte[n];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var itemLength = datas.Count;

            foreach (var item in bytes)
            {
                var index = item % itemLength;

                yield return datas[index];
            }
        }

        /// <summary>
        /// 将字节数组转换为16进制字符串
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public static string ConvertByteToString(byte[] inputData)
        {
            var sb = new StringBuilder(inputData.Length * 2);
            foreach (byte b in inputData)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将16进制字符串转换为字节数组
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static byte[] ConvertStringToByte(string inputString)
        {
            if (inputString == null || inputString.Length < 2) return null;

            int l = inputString.Length / 2;
            byte[] result = new byte[l];
            for (int i = 0; i < l; ++i)
            {
                result[i] = Convert.ToByte(inputString.Substring(2 * i, 2), 16);
            }

            return result;
        }
    }
}
