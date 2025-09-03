namespace Assessment.DTOs
{
    public class HobbyChangeResult
    {
        public List<int> InsertedIds { get; set; } = new();
        public List<int> UpdatedIds { get; set; } = new();
        public List<int> DeletedIds { get; set; } = new();
    }
}
