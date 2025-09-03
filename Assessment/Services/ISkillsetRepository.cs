using Assessment.DTOs;
using Assessment.Models;

public interface ISkillsetRepository
{
    Task<IEnumerable<Skillset>> GetAllAsync();
    Task<Skillset?> GetByNameAsync(string name); // if you keep it, make it query Skill
    Task<Skillset?> GetBySkillForFreelancerAsync(string skill, int freelancerId); // <—
    Task<IEnumerable<Skillset>> GetAllForFreelancerAsync(int freelancerId);
    Task<Skillset> CreateAsync(Skillset skillset);
    Task<int> DeleteByFreelancerAsync(int freelancerId);
    Task<int> UpdateAsync(SkillsetUpsertDto dto);
    Task<SkillsetUpsertDto?> GetAsync(int id, int freelancerId);
    Task<int> UpdateColumnsAsync(SkillsetUpsertDto dto, IReadOnlyList<string> columns);
    public Task<int> DeleteAsync(int id, int freelancerId);
}
