using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Models.Table;

namespace Bit.Core.Repositories
{
    public interface IOrganizationRepository : IRepository<Organization, Guid>
    {
        Task<ICollection<Organization>> GetManyByUserIdAsync(Guid userId);
        Task<ICollection<Organization>> SearchAsync(string name, string userEmail, int skip, int take);
    }
}
