using Assessment.DTOs;
using Assessment.Models;
using Assessment.Repository;

namespace Assessment.Services
{
    public sealed class PutAggregateResult
    {
        public bool Found { get; init; }
        public bool Changed { get; init; }
    }

    public class FreelancerService
    {
        private readonly FreelancerRepository _repo;
        private readonly ISkillsetRepository _skillsetRepository;
        private readonly IHobbyRepository _hobbyRepository;

        public FreelancerService(
            FreelancerRepository repo,
            ISkillsetRepository skillsetRepository,
            IHobbyRepository hobbyRepository)
        {
            _repo = repo;
            _skillsetRepository = skillsetRepository;
            _hobbyRepository = hobbyRepository;
        }

        // Create (low-level)
        public async Task<int> AddAsync(Freelancer freelancer)
        {
            var id = await _repo.AddAsync(freelancer);
            return id;
        }

        public Task AddSkillsetsAsync(IEnumerable<Skillset> skillsets)
            => _repo.AddSkillsetsAsync(skillsets);

        public Task AddHobbiesAsync(IEnumerable<Hobby> hobbies)
            => _repo.AddHobbiesAsync(hobbies);

        // Read (list)
        public async Task<IReadOnlyList<Freelancer>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
        {
            var items = await _repo.GetAllAsync(includeArchived, ct);
            return items.ToList();
        }

        // POST create
        public async Task<FreelancerDto> CreateAsync(CreateFreelancerDto dto)
        {
            var freelancer = new Freelancer
            {
                Username = dto.Username,
                Email = dto.Email,
                Phone = dto.Phone,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(freelancer);

            // Skillsets
            foreach (var skillsetName in dto.Skillsets ?? Enumerable.Empty<string>())
            {
                var existing = await _skillsetRepository.GetBySkillForFreelancerAsync(skillsetName, created.Id)
                             ?? await _skillsetRepository.CreateAsync(
                                    new Skillset { Skill = skillsetName, FreelancerId = created.Id });

                if (!created.Skillsets.Any(s =>
                        s.Skill.Equals(existing.Skill, StringComparison.OrdinalIgnoreCase)))
                {
                    created.Skillsets.Add(existing);
                }
            }

            // Hobbies
            foreach (var hobbyName in dto.Hobbies ?? Enumerable.Empty<string>())
            {
                var hobby = await _hobbyRepository.GetByNameForFreelancerAsync(hobbyName, created.Id)
                         ?? await _hobbyRepository.CreateAsync(
                                new Hobby { Name = hobbyName, FreelancerId = created.Id });

                if (!created.Hobbies.Any(h =>
                        h.Name.Equals(hobby.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    created.Hobbies.Add(hobby);
                }
            }

            return new FreelancerDto
            {
                Id = created.Id,
                Username = created.Username,
                Email = created.Email,
                Phone = created.Phone,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt,
                Skillsets = created.Skillsets.Select(x => x.Skill).ToList(),
                Hobbies = created.Hobbies.Select(h => h.Name).ToList()
            };
        }
        public Task<Freelancer?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<IReadOnlyList<Freelancer>> GetFreelancersAsync(string? search, CancellationToken ct = default)
        {
            var items = await _repo.GetFreelancersAsync(search, ct);
            return items.ToList();
        }

        // PUT full update (parent + children reset)
        public async Task<FreelancerDto?> UpdateAsync(int id, UpdateFreelancerDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null) return null;

            existing.Username = dto.Username;
            existing.Email = dto.Email;
            existing.Phone = dto.Phone;
            existing.UpdatedAt = DateTime.UtcNow;

            existing.Skillsets.Clear();
            existing.Hobbies.Clear();

            await _repo.UpdateAsync(id, dto);

            await _skillsetRepository.DeleteByFreelancerAsync(id);
            foreach (var s in (dto.Skillsets ?? Enumerable.Empty<SkillDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Skill))
                .Select(x => x.Skill.Trim()))
            {
                await _skillsetRepository.CreateAsync(
                    new Skillset { Skill = s, FreelancerId = id });
            }

            await _hobbyRepository.DeleteByFreelancerAsync(id);
            foreach (var h in (dto.Hobbies ?? Enumerable.Empty<HobbyDto>())
                                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                                .Select(x => x.Name.Trim()))
            {
                await _hobbyRepository.CreateAsync(
                    new Hobby { Name = h, FreelancerId = id });
            }

            var refreshed = await _repo.GetByIdAsync(id);
            if (refreshed is null) return null;

            var skillsets = await _skillsetRepository.GetAllForFreelancerAsync(id);
            var hobbies = await _hobbyRepository.GetAllForFreelancerAsync(id);

            return new FreelancerDto
            {
                Id = refreshed.Id,
                Username = refreshed.Username,
                Email = refreshed.Email,
                Phone = refreshed.Phone,
                CreatedAt = refreshed.CreatedAt,
                UpdatedAt = refreshed.UpdatedAt,
                Skillsets = skillsets.Select(s => s.Skill).ToList(),
                Hobbies = hobbies.Select(h => h.Name).ToList()
            };
        }

        // PUT: Hobby (service-level guardrails kept)
        public async Task<int> UpdateAsync(HobbyDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (dto.Id <= 0) throw new ArgumentOutOfRangeException(nameof(dto.Id));
            if (dto.FreelancerId <= 0) throw new ArgumentOutOfRangeException(nameof(dto.FreelancerId));
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required.", nameof(dto.Name));

            var current = await _repo.GetAsync(dto.Id, dto.FreelancerId);
            if (current is null) return 0;

            var merged = new HobbyDto
            {
                Id = dto.Id,
                FreelancerId = dto.FreelancerId,
                Name = string.IsNullOrWhiteSpace(dto.Name) ? current.Name : dto.Name
            };

            return await _repo.UpdateAsync(merged);
        }

        // PUT: Skillset (service-level guardrails kept)
        public async Task<int> UpdateAsync(SkillsetUpsertDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (dto.Id <= 0) throw new ArgumentOutOfRangeException(nameof(dto.Id));
            if (dto.FreelancerId <= 0) throw new ArgumentOutOfRangeException(nameof(dto.FreelancerId));
            if (string.IsNullOrWhiteSpace(dto.Skill))
                throw new ArgumentException("Name is required.", nameof(dto.Skill));

            var current = await _skillsetRepository.GetAsync(dto.Id, dto.FreelancerId);
            if (current is null) return 0;

            var targetSkill = string.IsNullOrWhiteSpace(dto.Skill) ? current.Skill : dto.Skill;
            if (string.Equals(current.Skill, targetSkill, StringComparison.Ordinal))
                return 1;

            var merged = new SkillsetUpsertDto
            {
                Id = dto.Id,
                FreelancerId = dto.FreelancerId,
                Skill = targetSkill
            };
            return await _skillsetRepository.UpdateAsync(merged);
        }

        // Delete
        public async Task<bool> DeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id); // assuming repo returns bool
        }


        // Archive ops (kept for future use; not exposed in controller now)
        public Task<bool> ArchiveAsync(int id) => _repo.ArchiveAsync(id);
        public Task<bool> UnarchiveAsync(int id) => _repo.UnarchiveAsync(id);
        public Task<bool> ToggleArchiveAsync(int id) => _repo.ToggleArchiveAsync(id);

        // PUT aggregate merge
        public async Task<PutAggregateResult> PutAggregateMergeAsync(int id, FreelancerUpsertDto dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return new PutAggregateResult { Found = false, Changed = false };

            var anyChange = false;
            // parent merge + diff
            var mergedUsername = string.IsNullOrWhiteSpace(dto.Username) ? current.Username : dto.Username.Trim();
            var mergedEmail = string.IsNullOrWhiteSpace(dto.Email) ? current.Email : dto.Email.Trim();
            var mergedPhone = string.IsNullOrWhiteSpace(dto.Phone) ? current.Phone : dto.Phone.Trim();
            var mergedArchived = dto.IsArchived ?? current.IsArchived;

            var parentChangedCols = new List<string>();
            if (!string.Equals(current.Username, mergedUsername, StringComparison.Ordinal)) parentChangedCols.Add("Username");
            if (!string.Equals(current.Email, mergedEmail, StringComparison.Ordinal)) parentChangedCols.Add("Email");
            if (!string.Equals(current.Phone, mergedPhone, StringComparison.Ordinal)) parentChangedCols.Add("Phone");
            if (current.IsArchived != mergedArchived) parentChangedCols.Add("IsArchived");

            if (parentChangedCols.Count > 0)
            {
                await _repo.UpdateFreelancerColumnsAsync(
                    id: id,
                    columns: parentChangedCols,
                    username: mergedUsername,
                    email: mergedEmail,
                    phone: mergedPhone,
                    isArchived: mergedArchived
                );
                anyChange = true;
            }
            // HOBBIES: only touch if property present in payload
            if (dto.Hobbies is not null)
            {
                var desiredHobbies = dto.Hobbies
                    .Where(h => h is not null && (h.Id > 0 || !string.IsNullOrWhiteSpace(h.Name)))
                    .Select(h => new HobbyDto
                    {
                        Id = h.Id,
                        FreelancerId = id,
                        Name = h.Name?.Trim()
                    })
                    .ToList();

                var hobbyResult = await _repo.SyncHobbiesAsync(id, desiredHobbies, replaceAll: false);

                if (hobbyResult.InsertedIds.Any() || hobbyResult.UpdatedIds.Any() || hobbyResult.DeletedIds.Any())
                {
                    anyChange = true;
                }
            }
            // SKILLSETS: only touch if property present in payload
            if (dto.Skillsets is not null)
            {
                var desiredSkillsets = dto.Skillsets
                    .Where(s => s is not null && (s.Id > 0 || !string.IsNullOrWhiteSpace(s.Skill)))
                    .Select(s => new SkillsetUpsertDto
                    {
                        Id = s.Id,
                        FreelancerId = id,
                        Skill = s.Skill?.Trim()
                    })
                    .ToList();

                var skillResult = await _repo.SyncSkillsetsAsync(id, desiredSkillsets, replaceAll: false);

                if (skillResult.InsertedIds.Any() || skillResult.UpdatedIds.Any() || skillResult.DeletedIds.Any())
                {
                    anyChange = true;
                }
            }
            return new PutAggregateResult { Found = true, Changed = anyChange };
        }

        public async Task<PageResult<FreelancerDto>> ListPagedAsync(
             int page, int pageSize, string? search, bool? archived)
        {
            var (items, total, skills, hobbies) = await _repo.ListPagedAsync(page, pageSize, search, archived); // ✅ name aligned

            var list = items.Select(f => new FreelancerDto
            {
                Id = f.Id,
                Username = f.Username,
                Email = f.Email,
                Phone = f.Phone,
                IsArchived = f.IsArchived,
                Skillsets = skills.Where(s => s.FreelancerId == f.Id)
                  .Select(s => s.Skill)
                  .ToList(),

                Hobbies = hobbies.Where(h => h.FreelancerId == f.Id)
                 .Select(h => h.Name)
                 .ToList()
            }).ToList();

            return new PageResult<FreelancerDto>
            {
                Items = list,
                Page = page,
                PageSize = pageSize,
                Total = total
            };
        }
    }
}
