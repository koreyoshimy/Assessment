namespace Assessment.DTOs
{
    public class FreelancerDto
    {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public bool IsArchived { get; set; }
            public List<string> Skillsets { get; set; } = new();
            public List<string> Hobbies { get; set; } = new();
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
        public class CreateFreelancerDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public List<string> Skillsets { get; set; } = new();
            public List<string> Hobbies { get; set; } = new();
        }

        public class UpdateFreelancerDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public List<SkillDto> Skillsets { get; set; } = new();
            public List<HobbyDto> Hobbies { get; set; } = new();
        }

    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Total { get; init; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
    public class SkillDto
    {
        public int Id { get; set; } 
        public int FreelancerId { get; set; } 
        public string Skill { get; set; } = string.Empty;
    }

    public class HobbyDto
    {
        public int Id { get; set; }
        public int FreelancerId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
