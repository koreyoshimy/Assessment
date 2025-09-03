using System.ComponentModel.DataAnnotations.Schema;

namespace Assessment.Models
{
    public class Hobby
    {
        public int Id { get; set; }
        [Column("Hobby")]
        public string Name { get; set; } = null!;   // e.g., "Graphic Design", "C# Programming"
        public int FreelancerId { get; set; }  // Optional: scale 1–5
    }
}
