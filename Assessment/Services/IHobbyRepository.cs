using Assessment.Models;

namespace Assessment.Services
{
    public interface IHobbyRepository
    {
        Task<IEnumerable<Hobby>> GetAllAsync();
        Task<Hobby?> GetByNameAsync(string name);
        Task<Hobby> CreateAsync(Hobby hobby);
        Task<int> DeleteByFreelancerAsync(int freelancerId);
        Task<IEnumerable<Hobby> >GetAllForFreelancerAsync(int freelancerId);
        Task<Hobby?> GetByNameForFreelancerAsync(string name, int freelancerId);
    }
}
