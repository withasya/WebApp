using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApp.Dtos;
using WebApp.Models;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // Kullanıcı kaydı (Register)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest("Bu e-posta ile kayıtlı bir kullanıcı zaten var.");
            }

            var newUser = new ApplicationUser
            {
                UserName = registerDto.UserName, // UserName'i de alıyoruz
                Email = registerDto.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser, registerDto.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors);
            }

            // Rol belirleme ve atama
            var role = registerDto.IsAdmin ? "Admin" : "User";

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(newUser, role);

            return Ok(new { message = "Kullanıcı başarıyla oluşturuldu.", role });
        }

        // Kullanıcı Girişi (Login)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName); // Artık username üzerinden arıyoruz
            if (user == null) return Unauthorized();

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (result.Succeeded)
            {
                var token = GenerateJwtToken(user); // Token sadece burada üretilecek
                return Ok(new { Token = token });
            }

            return Unauthorized();
        }

        // Sadece Admin rolüne sahip kullanıcılar için endpoint
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-dashboard")]
        public IActionResult AdminDashboard()
        {
            return Ok("Admin paneline hoş geldiniz!");
        }

        // Sadece User rolüne sahip kullanıcılar için endpoint
        [Authorize(Roles = "User")]
        [HttpGet("user-dashboard")]
        public IActionResult UserDashboard()
        {
            return Ok("Kullanıcı paneline hoş geldiniz!");
        }

        // JWT Token üretme işlemi
        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
            };

            var roles = _userManager.GetRolesAsync(user).Result;
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
