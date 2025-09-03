using Assessment.Controllers;
using Assessment.DTOs;
using Assessment.Models;
using Assessment.Services;
//using AutoMapper;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using static Assessment.Repository.SkillsetRepository;
using Microsoft.Extensions.Logging;

namespace Assessment.Repository
{
    public class SkillsetRepository: ISkillsetRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SkillsetRepository> _logger;
        public SkillsetRepository(IConfiguration configuration, ILogger<SkillsetRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<IEnumerable<Skillset>> GetAllAsync()
        {
            const string sql = "SELECT Id, Skill FROM Skillsets;";
            using var conn = CreateConnection();
            return await conn.QueryAsync<Skillset>(sql);
        }
        public async Task<Skillset?> GetBySkillForFreelancerAsync(string skill, int freelancerId)
        {
            const string sql = @"
        SELECT TOP 1 Id, FreelancerId, Skill
        FROM Skillsets
        WHERE FreelancerId = @FreelancerId AND Skill = @Skill;";
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Skillset>(
                sql, new { FreelancerId = freelancerId, Skill = skill.Trim() });
        }

        public async Task<Skillset?> GetByNameAsync(string skill)
        {
            const string sql = "SELECT TOP 1 Id, Skill FROM Skillsets WHERE SKill = @Skill;";
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Skillset>(sql, new { Skill = skill });
        }

        public async Task<Skillset> CreateAsync(Skillset skillset)
        {
            const string sql = @"
        INSERT INTO Skillsets (FreelancerId, Skill)
        OUTPUT INSERTED.Id, INSERTED.FreelancerId, INSERTED.Skill
        VALUES (@FreelancerId, @Skill);";
            using var conn = CreateConnection();
            return await conn.QuerySingleAsync<Skillset>(sql, new
            {
                skillset.FreelancerId,
                skillset.Skill
            });
        }

        //Update
        public async Task<int> DeleteByFreelancerAsync(int freelancerId)
        {
            const string sql = "DELETE FROM Skillsets WHERE FreelancerId = @FreelancerId;";
            using var conn = CreateConnection();
            return await conn.ExecuteAsync(sql, new { FreelancerId = freelancerId });
        }
        public async Task<Skillset?> GetByNameForFreelancerAsync(string skill, int freelancerId)
        {
            const string sql = @"
            SELECT TOP 1 Id, FreelancerId, Skill
            FROM Skillsets
            WHERE FreelancerId = @FreelancerId AND Skill = @Skill;";
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Skillset>(
                sql, new { FreelancerId = freelancerId, Skill = skill.Trim() });
        }
        public async Task<IEnumerable<Skillset>> GetAllForFreelancerAsync(int freelancerId)
        {
            const string sql = @"
        SELECT Id, FreelancerId, Skill
        FROM Skillsets
        WHERE FreelancerId = @FreelancerId
        ORDER BY Skill;";
            using var conn = CreateConnection();
            return await conn.QueryAsync<Skillset>(sql, new { FreelancerId = freelancerId });
        }
        public async Task<int> UpdateAsync(SkillsetUpsertDto dto)
        {
            const string sql = @"
UPDATE Skillsets
SET Skill = @Skill
WHERE Id = @Id AND FreelancerId = @FreelancerId;";

            using var conn = CreateConnection();
            var sw = Stopwatch.StartNew();
            await conn.OpenAsync();

            _logger.LogInformation("Connection open in {ms} ms", sw.ElapsedMilliseconds);

            var rows = await conn.ExecuteAsync(sql, new
            {
                dto.Skill,
                dto.Id,
                dto.FreelancerId
            }, commandTimeout: 5);

            return rows;
        }



        public async Task<SkillsetUpsertDto?> GetAsync(int id, int freelancerId)
        {
            const string sql = @"
SELECT Id, Skill, FreelancerId
FROM Skillsets
WHERE Id = @Id AND FreelancerId = @FreelancerId;";

            using var conn = CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<SkillsetUpsertDto>(sql, new
            {
                Id = id,
                FreelancerId = freelancerId
            });
        }
        public async Task<int> UpdateColumnsAsync(SkillsetUpsertDto dto, IReadOnlyList<string> columns)
        {
            if (columns is null || columns.Count == 0) return 0;

            // Build SET clause only for requested columns
            var setParts = new List<string>();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("FreelancerId", dto.FreelancerId);

            foreach (var col in columns)
            {
                switch (col)
                {
                    case "Skill":
                        setParts.Add("Skill = @Skill");
                        p.Add("Skill", dto.Skill);
                        break;

                        // Add more cases here when you add more columns to Skillsets:
                        // case "Description":
                        //     setParts.Add("Description = @Description");
                        //     p.Add("Description", dto.Description);
                        //     break;
                }
            }

            if (setParts.Count == 0) return 0;

            var sql = $@"
UPDATE dbo.Skillsets
SET {string.Join(", ", setParts)}
WHERE Id = @Id AND FreelancerId = @FreelancerId;";

            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.ExecuteAsync(sql, p, commandTimeout: 5);
        }
        //Delete certain skillsets
        public async Task<int> DeleteAsync(int id, int freelancerId)
        {
            const string sql = @"
DELETE FROM dbo.Skillsets
WHERE Id = @Id AND FreelancerId = @FreelancerId;";
            using var conn = CreateConnection();
            return await conn.ExecuteAsync(sql, new { Id = id, FreelancerId = freelancerId });
        }


    }
}
