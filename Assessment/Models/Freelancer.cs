using System.ComponentModel.DataAnnotations.Schema;

namespace Assessment.Models
{
    public sealed class Freelancer
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<Skillset> Skillsets { get; set; } = new();  // One-to-many relation
        public List<Hobby> Hobbies { get; set; } = new();
        public string? SkillsetsInput { get; set; }
        public string? HobbiesInput { get; set; }
    }
       
        // REQUIRED: initialize to avoid null refs
        //public List<FreelancerSkillset> FreelancerSkillsets { get; set; } = new();
        //public List<Hobby> FreelancerHobbies { get; set; } = new(); // if hobbies used
}
