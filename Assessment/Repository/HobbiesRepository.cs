using Assessment.DTOs;
using Assessment.Models;
using Dapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Logging;

namespace Assessment.Repository
{
    public sealed class HobbiesRepository : IHobbiesRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<HobbiesRepository> _logger;
        public HobbiesRepository(IConfiguration configuration, ILogger<HobbiesRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' was not found. Check appsettings.json.");
            _logger = logger;
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<int> UpdateAsync(HobbyDto dto)
        {
            const string sql = @"
UPDATE Hobbies
SET Name = @Name
WHERE Id = @Id AND FreelancerId = @FreelancerId;";

            using var conn = CreateConnection();
            return await conn.ExecuteAsync(sql, new { dto.Id, dto.FreelancerId, dto.Name });
        }
        public async Task<int> UpdateColumnsAsync(HobbyDto dto, IReadOnlyList<string> columns)
        {
            if (columns is null || columns.Count == 0) return 0;

            var setParts = new List<string>();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("FreelancerId", dto.FreelancerId);

            foreach (var col in columns)
            {
                switch (col)
                {
                    case "Name":
                        setParts.Add("Name = @Name");
                        p.Add("Name", dto.Name);
                        break;
                }
            }
               
            // touch UpdatedAt if you have that column:
            // setParts.Add("UpdatedAt = SYSUTCDATETIME()");

            if (setParts.Count == 0) return 0;

            var sql = $@"
UPDATE dbo.Hobbies
SET {string.Join(", ", setParts)}
WHERE Id = @Id AND FreelancerId = @FreelancerId;";

            using var conn = CreateConnection();
            await conn.OpenAsync();
            var rows = await conn.ExecuteAsync(sql, p, commandTimeout: 5);
            _logger.LogInformation("Hobbies partial update affected {rows} row(s)", rows);
            return rows;
        }
        public async Task<HobbyDto?> GetAsync(int id, int freelancerId)
        {
            const string sql = @"
SELECT Id, FreelancerId, Name
FROM dbo.Hobbies
WHERE Id = @Id AND FreelancerId = @FreelancerId;";

            using var conn = CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<HobbyDto>(
                sql, new { Id = id, FreelancerId = freelancerId });
        }
        public async Task<int> DeleteAsync(int id, int freelancerId)
        {
            const string sql = @"
DELETE FROM dbo.Hobbies
WHERE Id = @Id AND FreelancerId = @FreelancerId;";
            using var conn = CreateConnection();
            return await conn.ExecuteAsync(sql, new { Id = id, FreelancerId = freelancerId });
        }

    }
}
