using System;
using Yibi.Core.Entities;

namespace Yibi.Repositories.LiteDB
{
    public class ApplicationService
    {
        private LiteDbContext _db;

        public ApplicationService(LiteDbContext db)
        {
            _db = db;
        }

        public ApplicationInfo GetApplication(string applicationName)
        {
            return _db.Applications.FindOne(m => m.Name.Equals(applicationName));
        }

        public Guid Insert(ApplicationInfo model)
        {
            var effect = _db.Applications.Insert(model);

            return effect.AsGuid;
        }
    }
}
