// Models/Admin.cs
using System;

namespace Assessment.Auth.Models
{
    public sealed class Admin
    {
        public Guid AdminId { get; set; }   // uniqueidentifier
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
