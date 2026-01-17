using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WP25G10.Models.Dto;

namespace WP25G10.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        public AuthApiController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _userManager.FindByNameAsync(request.UserName)
                           ?? await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return BadRequest(new { message = "User with same username or email already exists." });

            var user = new IdentityUser
            {
                UserName = request.UserName,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            await _userManager.AddToRoleAsync(user, "User");

            var token = await GenerateJwtToken(user);

            return Ok(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var userNameOrEmail = request.UserNameOrEmail?.Trim();

            IdentityUser? user = null;

            if (!string.IsNullOrEmpty(userNameOrEmail))
            {
                if (userNameOrEmail.Contains("@"))
                    user = await _userManager.FindByEmailAsync(userNameOrEmail);
                else
                    user = await _userManager.FindByNameAsync(userNameOrEmail);
            }

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized(new { message = "Invalid credentials." });

            var token = await GenerateJwtToken(user);
            return Ok(token);
        }

        private async Task<AuthResponse> GenerateJwtToken(IdentityUser user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                new(JwtRegisteredClaimNames.Email, user.Email ?? "")
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var expires = DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponse
            {
                Token = tokenString,
                ExpiresAt = expires,
                UserName = user.UserName ?? "",
                Email = user.Email ?? ""
            };
        }
    }
}