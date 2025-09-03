using Assessment.Models;
using Assessment.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Assessment.Repository
{
    public sealed class HobbyRepository : IHobbyRepository
    {
        private readonly string _connectionString;

        public HobbyRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<IEnumerable<Hobby>> GetAllAsync()
        {
            const string sql = @"SELECT Id, FreelancerId, Name FROM Hobbies;";
            using var conn = CreateConnection();
            return await conn.QueryAsync<Hobby>(sql);
        }

        public async Task<Hobby?> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT TOP 1 Id, FreelancerId, Name
                FROM Hobbies
                WHERE Name = @Name;";
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Hobby>(sql, new { Name = name });
        }

        public async Task<Hobby?> GetByNameForFreelancerAsync(string name, int freelancerId)
        {
            const string sql = @"
                SELECT TOP 1 Id, FreelancerId, Name
                FROM Hobbies
                WHERE FreelancerId = @FreelancerId AND Name = @Name;";
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Hobby>(
                sql, new { FreelancerId = freelancerId, Name = name.Trim() });
        }

        public async Task<IEnumerable<Hobby>> GetAllForFreelancerAsync(int freelancerId)
        {
            const string sql = @"
                SELECT Id, FreelancerId, Name
                FROM Hobbies
                WHERE FreelancerId = @FreelancerId
                ORDER BY Name;";
            using var conn = CreateConnection();
            return await conn.QueryAsync<Hobby>(sql, new { FreelancerId = freelancerId });
        }

        public async Task<Hobby> CreateAsync(Hobby hobby)
        {
            const string sql = @"
                INSERT INTO Hobbies (FreelancerId, Name)
                OUTPUT INSERTED.Id, INSERTED.FreelancerId, INSERTED.Name
                VALUES (@FreelancerId, @Name);";
            using var conn = CreateConnection();
            // PASS BOTH parameters; this removes the @FreelancerId error
            return await conn.QuerySingleAsync<Hobby>(sql, new
            {
                hobby.FreelancerId,
                hobby.Name
            });
        }

        public async Task<int> DeleteByFreelancerAsync(int freelancerId)
        {
            const string sql = "DELETE FROM Hobbies WHERE FreelancerId = @FreelancerId;";
            using var conn = CreateConnection();
            return await conn.ExecuteAsync(sql, new { FreelancerId = freelancerId });
        }
    }
}
