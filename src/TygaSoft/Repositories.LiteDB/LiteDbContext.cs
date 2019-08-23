using System;
using LiteDB;
using Yibi.Core.Entities;

namespace Yibi.Repositories.LiteDB
{
    public class LiteDbContext
    {
        public readonly LiteDatabase Context;

        public LiteDbContext(string filePath)
        {
            var db = new LiteDatabase(filePath);
            if (db != null)
                Context = db;
        }

        public LiteCollection<ApplicationInfo> Applications => Context.GetCollection<ApplicationInfo>("Applications");
    }
}
