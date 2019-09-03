using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Models.Data;
using Bit.Core.Models.Table;
using Dapper;

namespace Bit.Core.Repositories.SqlServer
{
    public class UserRepository : Repository<User, Guid>, IUserRepository
    {
        public UserRepository(GlobalSettings globalSettings)
            : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
        { }

        public UserRepository(string connectionString, string readOnlyConnectionString)
            : base(connectionString, readOnlyConnectionString)
        { }
        
        public async Task<User> GetByEmailAsync(string email)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<User>(
                    $"[{Schema}].[{Table}_ReadByEmail]",
                    new { Email = email },
                    commandType: CommandType.StoredProcedure);

                return results.SingleOrDefault();
            }
        }

        public async Task<UserKdfInformation> GetKdfInformationByEmailAsync(string email)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<UserKdfInformation>(
                    $"[{Schema}].[{Table}_ReadKdfByEmail]",
                    new { Email = email },
                    commandType: CommandType.StoredProcedure);

                return results.SingleOrDefault();
            }
        }

        public async Task<ICollection<User>> SearchAsync(string email, int skip, int take)
        {
            using(var connection = new SqlConnection(ReadOnlyConnectionString))
            {
                var results = await connection.QueryAsync<User>(
                    $"[{Schema}].[{Table}_Search]",
                    new { Email = email, Skip = skip, Take = take },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                return results.ToList();
            }
        }

        public async Task<string> GetPublicKeyAsync(Guid id)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<string>(
                    $"[{Schema}].[{Table}_ReadPublicKeyById]",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);

                return results.SingleOrDefault();
            }
        }

        public async Task<DateTime> GetAccountRevisionDateAsync(Guid id)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<DateTime>(
                    $"[{Schema}].[{Table}_ReadAccountRevisionDateById]",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);

                return results.SingleOrDefault();
            }
        }

        public override async Task ReplaceAsync(User user)
        {
            await base.ReplaceAsync(user);
        }

        public override async Task DeleteAsync(User user)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(
                    $"[{Schema}].[{Table}_DeleteById]",
                    new { Id = user.Id },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180);
            }
        }
    }
}
