using LiteDB;
using System;

namespace Yibi.LiteDbMembershipProvider.Entities
{
    public class UsersInRolesInfo
    {
        [BsonId]
        public Guid UserId { get; set; }

        [BsonId]
        public Guid RoleId { get; set; }
    }
}
