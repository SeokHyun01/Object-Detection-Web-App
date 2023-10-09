using Business.Repository.IRepository;
using Common;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Web_API.Helper;

namespace Web_API.Controllers
{
	[Route("api/[controller]/[action]")]
	[ApiController]
	public class AccountController : Controller
	{
		private readonly SignInManager<AppUser> _signInManager;
		private readonly UserManager<AppUser> _userManager;
		private readonly ILogger<AccountController> _logger;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly APISettings _apiSettings;
		private readonly IFCMInfoRepository _fcmInfoRepository;

		public AccountController(SignInManager<AppUser> signInManager,
			UserManager<AppUser> userManager,
			ILogger<AccountController> logger,
			RoleManager<IdentityRole> roleManager,
			IOptions<APISettings> options,
			IFCMInfoRepository fCMInfoRepository)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_logger = logger;
			_roleManager = roleManager;
			_apiSettings = options.Value;
			_fcmInfoRepository = fCMInfoRepository;
		}

		[HttpPost]
		public async ValueTask<IActionResult> SignUp([FromBody] SignUpRequestDTO signUpRequest)
		{
			var user = new AppUser
			{
				Email = signUpRequest.Email,
				EmailConfirmed = true,

				UserName = signUpRequest.Email,
			};
			var result = await _userManager.CreateAsync(user, signUpRequest.Password);
			if (!result.Succeeded)
			{
				return BadRequest(new SignUpResponseDTO()
				{
					IsSucceeded = false,
					Errors = result.Errors.Select(u => u.Description)
				});
			}

			var createdUser = await _userManager.FindByEmailAsync(signUpRequest.Email);
			var createdUserId = createdUser?.Id;
			var createdFCMInfo = await _fcmInfoRepository.Create(new FCMInfoDTO()
			{
				UserId = createdUserId,
				Token = signUpRequest.FCMToken,
				DeviceNickname = signUpRequest.FCMDeviceNickname,
			});

			var roleResult = await _userManager.AddToRoleAsync(user, SD.ROLE_CLIENT);
			if (!roleResult.Succeeded)
			{
				return BadRequest(new SignUpResponseDTO
				{
					IsSucceeded = false,
					Errors = roleResult.Errors.Select(u => u.Description),
				});
			}

			return new OkObjectResult(new SignUpResponseDTO()
			{
				IsSucceeded = true,
				User = createdUser,
				FCMInfo = createdFCMInfo,
			});
		}

		[HttpPost]
		public async ValueTask<IActionResult> SignIn([FromBody] SignInRequestDTO signInRequest)
		{
			var result = await _signInManager.PasswordSignInAsync(signInRequest.Email, signInRequest.Password, false, false);
			if (result.Succeeded)
			{
				var user = await _userManager.FindByNameAsync(signInRequest.Email);
				if (user == null)
				{
					return Unauthorized(new SignInResponseDTO
					{
						IsSucceeded = false,
						Errors = new List<string> { "Id 또는 비밀번호를 잘못 입력했습니다." },
					});
				}

				var newFcmInfo = new FCMInfoDTO()
				{
					UserId = user.Id,
					Token = signInRequest.FCMToken,
					DeviceNickname = signInRequest.FCMDeviceNickname,
				};
				var fcmInfos = await _fcmInfoRepository.GetAllByUserId(user.Id);
				if (!fcmInfos.Any(x => x.Token == newFcmInfo.Token))
				{
					var createdFCMInfo = await _fcmInfoRepository.Create(newFcmInfo);
					_logger.LogInformation($"FCM Token: {createdFCMInfo.Token}, 기기가 등록됐습니다.");
				}

				var claims = await GetClaims(user);
				var signInCredentials = GetSigningCredentials();
				var tokenOptions = new JwtSecurityToken(
					issuer: _apiSettings.ValidIssuer,
					audience: _apiSettings.ValidAudience,
					claims: claims,
					expires: DateTime.Now.AddDays(30),
					signingCredentials: signInCredentials);
				var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

				return Ok(new SignInResponseDTO()
				{
					IsSucceeded = true,
					Token = token,
					User = user,
				});
			}
			else
			{
				return Unauthorized(new SignInResponseDTO
				{
					IsSucceeded = false,
					Errors = new List<string> { "Id 또는 비밀번호를 잘못 입력했습니다." },
				});
			}
		}

		private SigningCredentials GetSigningCredentials()
		{
			var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_apiSettings.SecretKey));

			return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
		}

		private async ValueTask<List<Claim>> GetClaims(IdentityUser user)
		{
			var claims = new List<Claim>
			{
				new Claim("Id", user.Id),
				new Claim(ClaimTypes.Name, user.Email),
				new Claim(ClaimTypes.Email, user.Email),
			};

			var roles = await _userManager.GetRolesAsync(await _userManager.FindByEmailAsync(user.Email));
			foreach (var role in roles)
			{
				claims.Add(new Claim(ClaimTypes.Role, role));
			}

			return claims;
		}
	}
}
