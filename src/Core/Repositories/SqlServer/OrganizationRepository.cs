using System;
using Bit.Core.Models.Table;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Linq;
using System.Collections.Generic;

namespace Bit.Core.Repositories.SqlServer
{
    public class OrganizationRepository : Repository<Organization, Guid>, IOrganizationRepository
    {
        public OrganizationRepository(GlobalSettings globalSettings)
            : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
        { }

        public OrganizationRepository(string connectionString, string readOnlyConnectionString)
            : base(connectionString, readOnlyConnectionString)
        { }

        public async Task<ICollection<Organization>> GetManyByUserIdAsync(Guid userId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<Organization>(
                    "[dbo].[Organization_ReadByUserId]",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);

                return results.ToList();
            }
        }

        public async Task<ICollection<Organization>> SearchAsync(string name, string userEmail,
            int skip, int take)
        {
            using(var connection = new SqlConnection(ReadOnlyConnectionString))
            {
                var results = await connection.QueryAsync<Organization>(
                    "[dbo].[Organization_Search]",
                    new { Name = name, UserEmail = userEmail, Skip = skip, Take = take },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                return results.ToList();
            }
        }
    }
}
