using System;
using LiteDB;
using Yibi.LiteMembershipProvider.Entities;

namespace Yibi.LiteMembershipProvider
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

        public LiteCollection<ApplicationsInfo> Applications => Context.GetCollection<ApplicationsInfo>("Applications");

        public LiteCollection<UsersInfo> Users => Context.GetCollection<UsersInfo>("Users");

        public LiteCollection<RolesInfo> Roles => Context.GetCollection<RolesInfo>("Roles");

        public LiteCollection<UsersInRolesInfo> UsersInRoles => Context.GetCollection<UsersInRolesInfo>("UsersInRoles");
    }
}
