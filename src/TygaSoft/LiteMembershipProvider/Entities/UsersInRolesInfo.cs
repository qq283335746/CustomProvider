using LiteDB;
using System;

namespace Yibi.LiteMembershipProvider.Entities
{
    public class UsersInRolesInfo
    {
        [BsonId]
        public Guid UserId { get; set; }

        [BsonId]
        public Guid RoleId { get; set; }
    }
}
