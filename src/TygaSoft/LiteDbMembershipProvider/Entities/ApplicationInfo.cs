using System;

namespace Yibi.LiteDbMembershipProvider.Entities
{
    public partial class ApplicationInfo
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
