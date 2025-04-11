using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Data;
using WebApp.Dtos;
using System.Security.Claims;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IdeaController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IdeaDTO>>> GetIdeas()
        {
            var ideas = await _context.Ideas
                .Select(i => new IdeaDTO
                {
                    Id = i.Id,
                    Title = i.Title,
                    Description = i.Description
                })
                .ToListAsync();

            return Ok(ideas);
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Idea>> PostIdea(IdeaDTO ideaDTO)
        {
            if (string.IsNullOrWhiteSpace(ideaDTO.Title) || string.IsNullOrWhiteSpace(ideaDTO.Description))
            {
                return BadRequest("Başlık ve açıklama boş olamaz.");
            }


            // Admin'in kimliği, yani `CreatedBy`'yi alıyoruz.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var idea = new Idea
            {
                Title = ideaDTO.Title,
                Description = ideaDTO.Description,
                CreatedById = userId // Admin'in ID'sini buraya atıyoruz.
            };

            _context.Ideas.Add(idea);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetIdeas), new { id = idea.Id }, idea);
        }

    }
    }
