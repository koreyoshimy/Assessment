using Assessment.DTOs;
using Assessment.Models;
namespace Assessment.Repository
{
    public interface IHobbiesRepository
    {
        Task<int> UpdateAsync(HobbyDto dto);                // rows affected
        Task<HobbyDto?> GetAsync(int id, int freelancerId);    // for read-back
        Task<int> UpdateColumnsAsync(HobbyDto dto, IReadOnlyList<string> columns);
        Task<int> DeleteAsync(int id, int freelancerId);

    }
}
