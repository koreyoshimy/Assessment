using Assessment.DTOs;
using Assessment.Models;
using Assessment.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Assessment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FreelancerController : ControllerBase
    {
        private readonly FreelancerService _service;
        private readonly ISkillsetRepository _skillsets;

        public FreelancerController(FreelancerService service, ISkillsetRepository skillsets)
        {
            _service = service;
            _skillsets = skillsets;
        }

        // GET /api/freelancer?search=jane
        [HttpGet("search")]
        public async Task<IActionResult> GetFreelancers([FromQuery] string? search)
        {
            var freelancers = await _service.GetFreelancersAsync(search);
            return Ok(freelancers);
        }

        // POST /api/freelancer
        [HttpPost]
        public async Task<ActionResult<FreelancerDto>> Create([FromBody] CreateFreelancerDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: ex.InnerException?.Message ?? ex.Message,
                    title: ex.GetType().Name
                );
            }
        }

        // GET /api/freelancer/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FreelancerDto>> GetByIdAsync(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        // DELETE /api/freelancer/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteFreelancer(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound($"Freelancer with ID {id} not found.");

            return NoContent();
        }

        // PUT /api/freelancer/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutFreelancerAggregate(int id, [FromBody] FreelancerUpsertDto dto)
        {
            if (dto is null)
                return BadRequest("Body required.");

            if (dto.Id != id)
                return BadRequest("Route/body mismatch.");

            // Ensure child DTOs inherit the freelancer ID
            if (dto.Hobbies is not null)
            {
                foreach (var hobby in dto.Hobbies)
                {
                    hobby.FreelancerId = dto.Id;
                }
            }
            
            if (dto.Skillsets is not null)
            {
                foreach (var skillset in dto.Skillsets)
                {
                    skillset.FreelancerId = dto.Id;
                }
            }

            var result = await _service.PutAggregateMergeAsync(id, dto);

            if (!result.Found)
                return NotFound();

            return NoContent();
        }

        // PATCH /api/freelancer/{id}/archive
        [HttpPatch("{id:int}/archive")]
        public async Task<IActionResult> Archive(int id)
        {
            var updated = await _service.ArchiveAsync(id);
            return updated ? NoContent() : NotFound();
        }

        // PATCH /api/freelancer/{id}/unarchive
        [HttpPatch("{id:int}/unarchive")]
        public async Task<IActionResult> Unarchive(int id)
        {
            var updated = await _service.UnarchiveAsync(id);
            return updated ? NoContent() : NotFound();
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<FreelancerDto>>> Get(
            [FromQuery] string? query,
            [FromQuery] bool? archived,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "Id",
            [FromQuery] string sortDir = "asc")
        {
            var result = await _service.ListPagedAsync(page, pageSize, query, archived);

            return Ok(result);
        }
    }
}