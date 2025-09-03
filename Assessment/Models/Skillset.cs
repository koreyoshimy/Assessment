using System.ComponentModel.DataAnnotations.Schema;

namespace Assessment.Models
{
    public class Skillset
    {
        public int Id { get; set; }
        [Column("Skill")]
        public string Skill { get; set; } = string.Empty;   // column name is Skill
        [Column("Skill")]
        public string Name { get; set; } = null!;
        public int FreelancerId { get; set; }
    }
}
