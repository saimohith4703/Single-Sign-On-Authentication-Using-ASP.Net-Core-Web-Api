using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SsoAuthenticationServer.Data;
using SsoAuthenticationServer.Models;
using System.Text;
using System.Security.Cryptography;

namespace SsoAuthenticationServer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly ApplicationDbContext _context;
		private readonly IConfiguration _configuration;

		public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext context, IConfiguration configuration)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_context = context;
			_configuration = configuration;
		}

		[HttpPost("Register")]
		public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
		{
			if(!ModelState.IsValid)
			{
				return BadRequest();
			}

			var user = new IdentityUser { UserName = registerModel.Username, Email = registerModel.Email };
			var result = await _userManager.CreateAsync(user, registerModel.Password);
			if(result.Succeeded)
			{
				return Ok(new { Result = "User register successfully" });
			}

			return BadRequest(result.Errors);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginModel login)
		{
			if(!ModelState.IsValid)
			{
				return BadRequest(login);
			}

			var user = await _userManager.FindByNameAsync(login.Username);
			if (user != null && await _userManager.CheckPasswordAsync(user, login.Password))
			{
				var token = GenerateJwtToken(user);
				return Ok(new { Token = token });
			}
			return Unauthorized("Invalid username or password");
		}

		[HttpPost("generate-sso-token")]
		[Authorize]
		public async Task<IActionResult> GenerateSSOToken()
		{
			try
			{
				var userId = User.FindFirstValue("User_Id");
				var user = await _userManager.FindByIdAsync(userId);
				if(user==null)
				{
					return NotFound("user not found");
				}

				var ssotoken = new SSOToken
				{
					UserId = user.Id,
					Token = Guid.NewGuid().ToString(),
					ExpiryDate = DateTime.UtcNow.AddMinutes(30),
					IsUsed=false
				};
				_context.SSOTokens.Add(ssotoken);
				await _context.SaveChangesAsync();

				return Ok(new { SSOToken = ssotoken });
			}
			catch(Exception ex)
			{
				return StatusCode(500, $"Internal Server Error {ex.Message}");
			}
		}

		[HttpPost("ValidateSSOToken")]
		public async Task<IActionResult> ValidateSSOToken([FromBody] ValidateSSOTokenRequest request)
		{
			try
			{
				if(!ModelState.IsValid)
				{
					return BadRequest(request);
				}
				var ssotoken = await _context.SSOTokens.SingleOrDefaultAsync(s => s.Token == request.SSOToken);
				if(ssotoken==null || ssotoken.IsUsed || ssotoken.ExpiryDate<DateTime.UtcNow)
				{
					return BadRequest("Invalid or expired sso token");
				}
				ssotoken.IsUsed = true;
				_context.Update(ssotoken);
				await _context.SaveChangesAsync();

				var user = await _userManager.FindByIdAsync(ssotoken.UserId);
				var newJwtToken = GenerateJwtToken(user);

				return Ok(new {
					Token = newJwtToken,
					UserDetails = new {
						UserName = user.UserName,
						Email = user.Email,
						UserId = user.Id
					}
				});
			}
			catch(Exception ex)
			{
				return StatusCode(500, $"Internal server error:{ex.Message}");
			}
		}

		private string GenerateJwtToken(IdentityUser user)
		{
			var claims = new List<Claim>
			{
				new Claim("User_Id",user.Id.ToString()),
				new Claim(ClaimTypes.NameIdentifier,user.UserName),
				new Claim(ClaimTypes.Email,user.Email),
				new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString())
			};
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
			var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var token = new JwtSecurityToken(
				issuer: _configuration["Jwt:Issuer"],
				audience: _configuration["Jwt:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(30),
				signingCredentials: credentials);

			return new JwtSecurityTokenHandler().WriteToken(token);

		}
	}
}
