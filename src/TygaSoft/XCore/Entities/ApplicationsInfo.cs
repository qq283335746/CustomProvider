using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yibi.Core.Entities
{
    public partial class ApplicationsInfo
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
