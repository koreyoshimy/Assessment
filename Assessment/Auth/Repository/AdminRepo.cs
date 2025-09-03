namespace Assessment.Auth.Repository
{
    using System.Data.SqlClient;
    using Dapper;

    public class AdminRepo
    {
        private readonly string _connectionString;

        public AdminRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Admin?> FindByEmailAsync(string email)
        {
            const string sql = "SELECT TOP 1 * FROM Admins WHERE Email = @Email";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<Admin>(sql, new { Email = email });
        }

        public async Task UpdateLastLoginAsync(Guid id, DateTime utcNow)
        {
            const string sql = "UPDATE Admins SET LastLoginAt = @Now WHERE AdminId = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { Id = id, Now = utcNow });
        }
    }

}
