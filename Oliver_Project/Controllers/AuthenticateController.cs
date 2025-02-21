using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Oliver_Project.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Oliver_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        private const string AdminRole = "Admin";
        private const string MemberRole = "Member";

        public AuthenticateController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // Register user without assigning a role
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { message = "User already exists" });

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = "User registration failed", errors = result.Errors });

            return Ok(new { message = "User registered successfully" });
        }

        // Register admin user
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { message = "User already exists" });

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = "Admin registration failed", errors = result.Errors });

            // Ensure the Admin role exists
            if (!await _roleManager.RoleExistsAsync(AdminRole))
                await _roleManager.CreateAsync(new IdentityRole(AdminRole));

            await _userManager.AddToRoleAsync(user, AdminRole);

            return Ok(new { message = "Admin registered successfully" });
        }

        // Login and return JWT token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(2),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        // Grant "Member" role to a registered user (Admin only)
        [HttpPost("grant-user-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GrantUserRole([FromBody] RoleAssignmentModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return BadRequest(new { message = $"Role '{model.RoleName}' does not exist." });

            // Remove all current roles
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Any())
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, userRoles);
                if (!removeRolesResult.Succeeded)
                    return BadRequest(new { message = "Failed to remove existing roles." });
            }

            // Assign the new role
            var addRoleResult = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!addRoleResult.Succeeded)
                return BadRequest(new { message = "Failed to assign new role." });

            return Ok(new { message = $"User role updated to '{model.RoleName}' successfully." });
        }
    }
}
