using System.ComponentModel.DataAnnotations;

namespace Assessment.DTOs
{
    public sealed class FreelancerUpsertDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public bool? IsArchived { get; set; }
        public List<HobbyUpsertDto>? Hobbies { get; set; }
        public List<SkillsetUpsertDto>? Skillsets { get; set; }
    }


    public sealed class HobbyUpsertDto
    {
        public int Id { get; set; }                          // >0 for existing rows; <=0 ignored in PUT
        public int FreelancerId { get; set; }                // must match route id
        public string? Name { get; set; }                    // only updated if not null
    }

    public sealed class SkillsetUpsertDto
    {
        public int Id { get; set; }
        public int FreelancerId { get; set; }
        public string? Skill { get; set; }                     // keep nullable for partial update
    }
}
