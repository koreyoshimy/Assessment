using Assessment.DTOs;
using Assessment.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Data;

namespace Assessment.Repository
{
    public class FreelancerRepository
    {
        private readonly string _connectionString;

        public FreelancerRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found. Check appsettings.json.");
        }

        private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        // Low-level create (transactional)
        public async Task<int> AddAsync(Freelancer freelancer)
        {
            const string insertSql = @"
INSERT INTO Freelancers (Username, Email, Phone, IsArchived)
VALUES (@Username, @Email, @Phone, 0);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            const string insertSkillSql = @"
INSERT INTO Skillsets (FreelancerId, Skill) VALUES (@FreelancerId, @Skill);";

            const string insertHobbySql = @"
INSERT INTO Hobbies (FreelancerId, Name) VALUES (@FreelancerId, @Name);";

            using var conn = (SqlConnection)CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                var id = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(insertSql, new
                    {
                        freelancer.Username,
                        freelancer.Email,
                        freelancer.Phone
                    }, tx));

                if (freelancer.Skillsets != null && freelancer.Skillsets.Count > 0)
                {
                    foreach (var s in freelancer.Skillsets)
                    {
                        await conn.ExecuteAsync(
                            new CommandDefinition(insertSkillSql, new { FreelancerId = id, Skill = s.Skill }, tx));
                    }
                }

                if (freelancer.Hobbies != null && freelancer.Hobbies.Count > 0)
                {
                    foreach (var h in freelancer.Hobbies)
                    {
                        await conn.ExecuteAsync(
                            new CommandDefinition(insertHobbySql, new { FreelancerId = id, Name = h.Name }, tx));
                    }
                }

                tx.Commit();
                return id;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // POST create and return
        public async Task<Freelancer> CreateAsync(Freelancer freelancer)
        {
            const string sql = @"
INSERT INTO Freelancers (Username, Email, Phone, IsArchived, CreatedAt, UpdatedAt)
OUTPUT INSERTED.Id
VALUES (@Username, @Email, @Phone, @IsArchived, @CreatedAt, @UpdatedAt);";

            using var conn = CreateConnection();
            var id = await conn.ExecuteScalarAsync<int>(sql, new
            {
                freelancer.Username,
                freelancer.Email,
                freelancer.Phone,
                freelancer.IsArchived,
                freelancer.CreatedAt,
                freelancer.UpdatedAt
            });

            freelancer.Id = id;
            return freelancer;
        }

        // Read
        public async Task<IReadOnlyList<Freelancer>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
        {
            var sql = includeArchived
                ? "SELECT Id, Username, Email, Phone, IsArchived FROM dbo.Freelancers;"
                : "SELECT Id, Username, Email, Phone, IsArchived FROM dbo.Freelancers WHERE IsArchived = 0;";

            using var conn = CreateConnection();
            var cmd = new CommandDefinition(sql, cancellationToken: ct);
            var data = await conn.QueryAsync<Freelancer>(cmd);
            return data.ToList();
        }

        public async Task<(IReadOnlyList<Freelancer> Items, int Total, List<SkillDto> Skills, List<HobbyDto> Hobbies)> ListPagedAsync(
    int page, int pageSize, string? search, bool? archived)
        {
            using var conn = CreateConnection();
            conn.Open();

            var pattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.ToLower()}%";
            var offset = (page - 1) * pageSize;

            const string F = "[dbo].[Freelancers]";
            const string S = "[dbo].[Skillsets]";
            const string H = "[dbo].[Hobbies]";

            var sqlParents = $@"
            SELECT f.Id, f.Username, f.Email, f.Phone, f.IsArchived, f.CreatedAt, f.UpdatedAt
            FROM {F} f
            WHERE (@pattern IS NULL OR LOWER(f.Username) LIKE @pattern OR LOWER(f.Email) LIKE @pattern)
              AND (@archived IS NULL OR f.IsArchived = @archived)
            ORDER BY f.Id
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM {F} f
            WHERE (@pattern IS NULL OR LOWER(f.Username) LIKE @pattern OR LOWER(f.Email) LIKE @pattern)
              AND (@archived IS NULL OR f.IsArchived = @archived);
            ";

            using var multi = await conn.QueryMultipleAsync(sqlParents, new { pattern, archived, offset, pageSize });
            var items = (await multi.ReadAsync<Freelancer>()).ToList();
            var total = await multi.ReadSingleAsync<int>();

            if (items.Count == 0) return (items, total, new List<SkillDto>(), new List<HobbyDto>());
            var ids = items.Select(p => p.Id).ToArray();
            var sqlChildren = $@"
            SELECT Id, FreelancerId, Skill
            FROM {S}
            WHERE FreelancerId IN @ids;

            SELECT Id, FreelancerId, [Name]
            FROM {H}
            WHERE FreelancerId IN @ids;
            ";
            using var multi2 = await conn.QueryMultipleAsync(sqlChildren, new { ids });
            var skills = (await multi2.ReadAsync<SkillDto>())?.ToList() ?? new List<SkillDto>();
            var hobbies = (await multi2.ReadAsync<HobbyDto>())?.ToList() ?? new List<HobbyDto>();
            return (items, total, skills, hobbies);
        }
        public async Task<Freelancer?> GetByIdAsync(int id)
        {
            const string freelancerSql = @"
SELECT Id, Username, Email, Phone, IsArchived, CreatedAt, UpdatedAt
FROM Freelancers
WHERE Id = @Id;";

            const string skillsSql = @"
SELECT Id, FreelancerId, Skill
FROM Skillsets
WHERE FreelancerId = @Id;";

            const string hobbiesSql = @"
SELECT Id, FreelancerId, Name
FROM Hobbies
WHERE FreelancerId = @Id;";

            using var conn = CreateConnection();
            var f = await conn.QuerySingleOrDefaultAsync<Freelancer>(
                new CommandDefinition(freelancerSql, new { Id = id }));

            if (f is null) return null;

            var skills = await conn.QueryAsync<Skillset>(
                new CommandDefinition(skillsSql, new { Id = id }));
            var hobbies = await conn.QueryAsync<Hobby>(
                new CommandDefinition(hobbiesSql, new { Id = id }));

            f.Skillsets = skills.AsList();
            f.Hobbies = hobbies.AsList();
            return f;
        }

        public async Task<IReadOnlyList<Freelancer>> GetFreelancersAsync(string? search, CancellationToken ct = default)
        {
            const string sql = @"
SELECT Id, Username, Email, Phone, IsArchived
FROM dbo.Freelancers
WHERE (@search IS NULL
       OR Username LIKE '%' + @search + '%'
       OR Email LIKE '%' + @search + '%');";

            using var conn = CreateConnection();
            var cmd = new CommandDefinition(sql, new { search }, cancellationToken: ct);
            var rows = await conn.QueryAsync<Freelancer>(cmd);
            return rows.ToList();
        }

        public async Task<int> UpdateAsync(int id, UpdateFreelancerDto dto)
        {
            const string updateSql = @"
UPDATE Freelancers
SET Username = @Username,
    Email    = @Email,
    Phone    = @Phone,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync(updateSql, new
            {
                Id = id,
                dto.Username,
                dto.Email,
                Phone = dto.Phone,
                UpdatedAt = DateTime.UtcNow
            });
            return rows;
        }

        public Task<Hobby?> GetHobbyAsync(int id, int freelancerId, IDbConnection? conn = null, IDbTransaction? tx = null)
        {
            const string sql = @"
SELECT Id, FreelancerId, Name
FROM dbo.Hobbies
WHERE Id = @Id AND FreelancerId = @FreelancerId;";
            return (conn ?? CreateConnection()).QuerySingleOrDefaultAsync<Hobby>(
                new CommandDefinition(sql, new { Id = id, FreelancerId = freelancerId }, tx));
        }

        public Task<Skillset?> GetSkillsetAsync(int id, int freelancerId, IDbConnection? conn = null, IDbTransaction? tx = null)
        {
            const string sql = @"
SELECT Id, FreelancerId, Skill
FROM dbo.Skillsets
WHERE Id = @Id AND FreelancerId = @FreelancerId;";
            return (conn ?? CreateConnection()).QuerySingleOrDefaultAsync<Skillset>(
                new CommandDefinition(sql, new { Id = id, FreelancerId = freelancerId }, tx));
        }

        public async Task<int> UpdateFreelancerColumnsAsync(
            int id,
            IReadOnlyList<string> columns,
            string? username,
            string? email,
            string? phone,
            bool? isArchived,
            IDbConnection? extConn = null,
            IDbTransaction? tx = null)
        {
            if (columns is null || columns.Count == 0) return 0;

            var set = new List<string>();
            var p = new DynamicParameters();
            p.Add("Id", id);

            foreach (var col in columns)
            {
                switch (col)
                {
                    case "Username":
                        set.Add("Username = @Username");
                        p.Add("Username", username);
                        break;
                    case "Email":
                        set.Add("Email = @Email");
                        p.Add("Email", email);
                        break;
                    case "Phone":
                        set.Add("Phone = @Phone");
                        p.Add("Phone", phone);
                        break;
                    case "IsArchived":
                        set.Add("IsArchived = @IsArchived");
                        p.Add("IsArchived", isArchived);
                        break;
                }
            }
            set.Add("UpdatedAt = SYSUTCDATETIME()");

            var sql = $@"
UPDATE dbo.Freelancers
SET {string.Join(", ", set)}
WHERE Id = @Id;";

            var mustDispose = extConn is null;
            var conn = extConn ?? CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            try
            {
                return await conn.ExecuteAsync(new CommandDefinition(sql, p, tx, commandTimeout: 5));
            }
            finally
            {
                if (mustDispose) conn.Dispose();
            }
        }
        public async Task<HobbyChangeResult> SyncHobbiesAsync(int freelancerId, IEnumerable<HobbyDto> desired, bool replaceAll = true)
        {
            using var conn = CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            var result = new HobbyChangeResult();

            var existing = (await conn.QueryAsync<Hobby>(
                "SELECT Id, FreelancerId, Name FROM dbo.Hobbies WHERE FreelancerId = @FreelancerId;",
                new { FreelancerId = freelancerId }, tx)).ToList();

            var desiredList = desired?.ToList() ?? new List<HobbyDto>();
            var desiredIds = desiredList.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
            // ✅ DELETE: remove records not in desired list if replaceAll is true
            if (replaceAll)
            {
                var toDelete = existing
                    .Where(e => !desiredIds.Contains(e.Id))
                    .Select(e => e.Id)
                    .ToList();

                if (toDelete.Count > 0)
                {
                    await conn.ExecuteAsync(
                        "DELETE FROM dbo.Hobbies WHERE Id IN @ids AND FreelancerId = @FreelancerId;",
                        new { ids = toDelete, FreelancerId = freelancerId }, tx);
                    result.DeletedIds.AddRange(toDelete);
                }
            }
            // ✅ UPDATE: allow blank names to overwrite existing ones
            foreach (var d in desiredList.Where(x => x.Id > 0))
            {
                var e = existing.FirstOrDefault(x => x.Id == d.Id);
                if (e is null) continue;

                // If the incoming name is blank, always overwrite (collapse to NULL)
                if (string.IsNullOrWhiteSpace(d.Name))
                {
                    await conn.ExecuteAsync(@"
            UPDATE dbo.Hobbies
            SET Name = @Name
            WHERE Id = @Id AND FreelancerId = @FreelancerId;",
                        new { Name = (string?)null, Id = d.Id, FreelancerId = freelancerId }, tx);

                    result.UpdatedIds.Add(d.Id);
                    continue;
                }

                // Otherwise, compare normalized values and update on mismatch
                var incoming = d.Name.Trim();
                var existingName = string.IsNullOrWhiteSpace(e.Name) ? null : e.Name.Trim();

                if (!string.Equals(existingName, incoming, StringComparison.OrdinalIgnoreCase))
                {
                    await conn.ExecuteAsync(@"
                    UPDATE dbo.Hobbies
                    SET Name = @Name
                    WHERE Id = @Id AND FreelancerId = @FreelancerId;",
                    new { Name = incoming, Id = d.Id, FreelancerId = freelancerId }, tx);

                    result.UpdatedIds.Add(d.Id);
                }
            }
            // INSERT new (this was missing)
            var existingNames = existing
                .Select(x => (x.Name ?? "").Trim().ToLowerInvariant())
                .ToHashSet();

            foreach (var d in desiredList.Where(x => x.Id == 0))
            {
                var incoming = (d.Name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(incoming)) continue;

                var key = incoming.ToLowerInvariant();
                if (existingNames.Contains(key)) continue; // de-dupe on normalized name

                var insertedId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.Hobbies (FreelancerId, Name)
                OUTPUT INSERTED.Id
                VALUES (@FreelancerId, @Name);",
                new { FreelancerId = freelancerId, Name = incoming }, tx);

                result.InsertedIds.Add(insertedId);
                existingNames.Add(key);
            }

            tx.Commit();
            return result;
        }
        public async Task<SkillsetChangeResult> SyncSkillsetsAsync(int freelancerId, IEnumerable<SkillsetUpsertDto> desired, bool replaceAll = true)
        {
            using var conn = CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            var result = new SkillsetChangeResult();

            var existing = (await conn.QueryAsync<SkillDto>(
                "SELECT Id, FreelancerId, Skill FROM dbo.Skillsets WHERE FreelancerId = @FreelancerId;",
                new { FreelancerId = freelancerId }, tx)).ToList();

            var desiredList = desired?.ToList() ?? new List<SkillsetUpsertDto>();
            var desiredIds = desiredList.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            // DELETE
            if (replaceAll)
            {
                var toDelete = existing.Where(e => !desiredIds.Contains(e.Id)).Select(e => e.Id).ToList();
                if (toDelete.Count > 0)
                {
                    await conn.ExecuteAsync(
                        "DELETE FROM dbo.Skillsets WHERE Id IN @ids AND FreelancerId = @FreelancerId;",
                        new { ids = toDelete, FreelancerId = freelancerId }, tx);
                    result.DeletedIds.AddRange(toDelete);
                }
            }

            // UPDATE
            foreach (var d in desiredList.Where(x => x.Id > 0))
            {
                var e = existing.FirstOrDefault(x => x.Id == d.Id);
                if (e is null) continue;

                var incoming = d.Skill?.Trim();
                if (!string.IsNullOrWhiteSpace(incoming) &&
                    !string.Equals((e.Skill ?? "").Trim(), incoming, StringComparison.OrdinalIgnoreCase))
                {
                    await conn.ExecuteAsync(@"
                    UPDATE dbo.Skillsets
                    SET Skill = @Skill
                    WHERE Id = @Id AND FreelancerId = @FreelancerId;",
                        new { Skill = incoming, Id = d.Id, FreelancerId = freelancerId }, tx);
                    result.UpdatedIds.Add(d.Id);
                }
            }

            // INSERT
            var existingSkills = existing.Select(x => (x.Skill ?? "").Trim().ToLowerInvariant()).ToHashSet();

            foreach (var d in desiredList.Where(x => x.Id == 0))
            {
                var incoming = (d.Skill ?? "").Trim();
                if (string.IsNullOrWhiteSpace(incoming)) continue;

                var key = incoming.ToLowerInvariant();
                if (existingSkills.Contains(key)) continue;

                var insertedId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.Skillsets (FreelancerId, Skill)
                OUTPUT INSERTED.Id
                VALUES (@FreelancerId, @Skill);",
                    new { FreelancerId = freelancerId, Skill = incoming }, tx);
                result.InsertedIds.Add(insertedId);
                existingSkills.Add(key);
            }
            tx.Commit();
            return result;
        }
        // INSERT (children)
        public async Task<int> InsertHobbyAsync(int freelancerId, string name, IDbConnection? extConn = null, IDbTransaction? tx = null)
        {
            const string sql = @"
INSERT INTO dbo.Hobbies (FreelancerId, Name)
VALUES (@FreelancerId, @Name);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            var mustDispose = extConn is null;
            var conn = extConn ?? CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sql, new { FreelancerId = freelancerId, Name = name }, tx));
            }
            finally
            {
                if (mustDispose) conn.Dispose();
            }
        }

        public async Task<int> InsertSkillsetAsync(int freelancerId, string skill, IDbConnection? extConn = null, IDbTransaction? tx = null)
        {
            const string sql = @"
INSERT INTO dbo.Skillsets (FreelancerId, Skill)
VALUES (@FreelancerId, @Skill);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            var mustDispose = extConn is null;
            var conn = extConn ?? CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sql, new { FreelancerId = freelancerId, Skill = skill }, tx));
            }
            finally
            {
                if (mustDispose) conn.Dispose();
            }
        }

        // Delete
        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = CreateConnection(); // however you get your DB connection

            var rowsAffected = await conn.ExecuteAsync(
                "DELETE FROM Freelancers WHERE Id = @Id",
                new { Id = id }
            );

            return rowsAffected > 0;
        }


        // PUT: Hobby
        public async Task<int> UpdateAsync(HobbyDto dto)
        {
            const string sql = @"
UPDATE Hobbies
SET Name = @Name
WHERE Id = @Id AND FreelancerId = @FreelancerId;";

            using var conn = CreateConnection();
            return await conn.ExecuteAsync(sql, new { dto.Id, dto.FreelancerId, dto.Name });
        }

        // Archive ops (retained; not exposed in controller)
        public async Task<bool> ArchiveAsync(int id)
        {
            const string sql = @"UPDATE dbo.Freelancers SET IsArchived = 1, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;";
            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }));
            return rows > 0;
        }

        public async Task<bool> UnarchiveAsync(int id)
        {
            const string sql = @"UPDATE dbo.Freelancers SET IsArchived = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;";
            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }));
            return rows > 0;
        }

        public async Task<bool> ToggleArchiveAsync(int id, CancellationToken ct = default)
        {
            const string sql = @"
UPDATE dbo.Freelancers
SET IsArchived = CASE WHEN IsArchived = 1 THEN 0 ELSE 1 END
WHERE Id = @Id;";
            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
            return rows > 0;
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

        public async Task<IReadOnlyList<Freelancer>> GetArchivedFreelancersAsync(CancellationToken ct = default)
        {
            const string sql = @"
SELECT Id, Username, Email, IsArchived
FROM dbo.Freelancers
WHERE IsArchived = 1;";
            using var conn = CreateConnection();
            var cmd = new CommandDefinition(sql, cancellationToken: ct);
            var rows = await conn.QueryAsync<Freelancer>(cmd);
            return rows.ToList();
        }
        public Task<Hobby?> GetAsync(int id, int freelancerId)
        {
            return GetHobbyAsync(id, freelancerId);
        }

        // BULK helpers (left as-is, corrected column names)
        public async Task AddSkillsetsAsync(IEnumerable<Skillset> skillsets)
        {
            if (skillsets == null) return;

            const string sql = @"INSERT INTO dbo.Skillsets (FreelancerId, Skill) VALUES (@FreelancerId, @Skill);";

            using var conn = CreateConnection();
            var list = skillsets.Where(s => s != null).ToList();
            if (list.Count == 0) return;

            foreach (var s in list)
            {
                var cmd = new CommandDefinition(sql, new { s.FreelancerId, Skill = s.Skill });
                await conn.ExecuteAsync(cmd);
            }
        }
//        public async Task SyncSkillsetsAsync(int freelancerId, IEnumerable<SkillsetUpsertDto> newSkillsets, IDbConnection? extConn = null, IDbTransaction? tx = null)
//        {
//            var mustDispose = extConn is null;
//            var conn = extConn ?? CreateConnection();
//            if (conn.State != ConnectionState.Open) conn.Open();

//            try
//            {
//                // Get current skills from DB
//                var existing = (await conn.QueryAsync<Skillset>(
//                    "SELECT Id, FreelancerId, Skill FROM dbo.Skillsets WHERE FreelancerId = @FreelancerId;",
//                    new { FreelancerId = freelancerId }, tx)).ToList();

//                var keepIds = newSkillsets.Where(s => s.Id > 0).Select(s => s.Id.Value).ToList();

//                // Delete removed skills
//                var toDelete = existing.Where(s => !keepIds.Contains(s.Id)).Select(s => s.Id).ToList();
//                if (toDelete.Any())
//                {
//                    await conn.ExecuteAsync(
//                        "DELETE FROM dbo.Skillsets WHERE Id IN @ids AND FreelancerId = @FreelancerId;",
//                        new { ids = toDelete, FreelancerId = freelancerId }, tx);
//                }

//                // Update existing
//                foreach (var dto in newSkillsets.Where(s => s.Id > 0))
//                {
//                    await conn.ExecuteAsync(@"
//UPDATE dbo.Skillsets
//SET Skill = @Skill
//WHERE Id = @Id AND FreelancerId = @FreelancerId;",
//                        new { dto.Skill, dto.Id, FreelancerId = freelancerId }, tx);
//                }

//                // Insert new
//                foreach (var dto in newSkillsets.Where(s => s.Id == null || s.Id == 0))
//                {
//                    await conn.ExecuteAsync(@"
//INSERT INTO dbo.Skillsets (FreelancerId, Skill) VALUES (@FreelancerId, @Skill);",
//                        new { FreelancerId = freelancerId, dto.Skill }, tx);
//                }
//            }
//            finally
//            {
//                if (mustDispose) conn.Dispose();
//            }
//        }

        public async Task AddHobbiesAsync(IEnumerable<Hobby> hobbies)
        {
            if (hobbies == null) return;

            const string sql = @"INSERT INTO dbo.Hobbies (FreelancerId, Name) VALUES (@FreelancerId, @Name);";

            using var conn = CreateConnection();
            var list = hobbies.Where(h => h != null).ToList();
            if (list.Count == 0) return;

            foreach (var h in list)
            {
                var cmd = new CommandDefinition(sql, new { h.FreelancerId, Name = h.Name });
                await conn.ExecuteAsync(cmd);
            }
        }
    }
}
