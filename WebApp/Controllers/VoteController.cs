using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Dtos;
using WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class VoteController(AppDbContext context, UserManager<ApplicationUser> userManager) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;


        // POST: api/vote
        [HttpPost]
        public async Task<IActionResult> Vote([FromBody] VoteDto voteDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Aynı kullanıcı aynı fikre daha önce oy vermiş mi?
            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.UserId == userId && v.IdeaId == voteDto.IdeaId);

            if (existingVote != null)
            {
                return BadRequest("Bu fikre zaten oy verdiniz.");
            }

            // 2. Fikrin gerçekten var olduğuna emin olalım (isteğe bağlı ama sağlam olur)
            var ideaExists = await _context.Ideas.AnyAsync(i => i.Id == voteDto.IdeaId);
            if (!ideaExists)
            {
                return NotFound("Fikir bulunamadı.");
            }

            // 3. Oy nesnesini oluştur
            var vote = new Vote
            {
                UserId = userId,
                IdeaId = voteDto.IdeaId
            };

            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            return Ok("Oyunuz başarıyla kaydedildi.");
        }


        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllIdeasWithVotes()
        {
            var ideasWithVotes = await _context.Ideas
                .Select(idea => new
                {
                    IdeaId = idea.Id,
                    idea.Title,
                    idea.Description,
                    VoteCount = _context.Votes.Count(v => v.IdeaId == idea.Id)
                })
                .ToListAsync();

            return Ok(ideasWithVotes);
        }



        // GET: api/vote/idea/{ideaId}
        [HttpGet("idea/{ideaId}")]
        public async Task<IActionResult> GetVoteCountForIdea(int ideaId)
        {
            var ideaExists = await _context.Ideas.AnyAsync(i => i.Id == ideaId);
            if (!ideaExists)
            {
                return NotFound("Fikir bulunamadı.");
            }

            var voteCount = await _context.Votes.CountAsync(v => v.IdeaId == ideaId);
            return Ok(new { IdeaId = ideaId, VoteCount = voteCount });
        }


        // GET: api/vote/my-votes
        [HttpGet("my-votes")]
        public async Task<IActionResult> GetMyVotedIdeas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var votedIdeas = await _context.Votes
                .Where(v => v.UserId == userId)
                .Include(v => v.Idea)
                .Select(v => new
                {
                    IdeaId = v.IdeaId,
                    Title = v.Idea.Title,
                    Description = v.Idea.Description
                })
                .ToListAsync();

            return Ok(votedIdeas);
        }


    }


}
