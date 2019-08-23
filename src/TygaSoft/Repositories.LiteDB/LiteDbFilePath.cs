using System;
using System.Collections.Generic;
using System.Text;

namespace Yibi.Repositories.LiteDB
{
    public class LiteDbFilePath
    {
        //待测试其正确性
        public static string GetFilePath(string fileName)
        {
            var path = System.IO.Path.GetFullPath(
               AppDomain.CurrentDomain.BaseDirectory +
               string.Format("../App_Data/{0}.ldb", fileName)
               );

            return path;
        }
    }
}
