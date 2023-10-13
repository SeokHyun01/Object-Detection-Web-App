using Common;
using DataAccess;
using DataAccess.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebServer.Service.IService;

namespace WebServer.Service
{
	public class DBInitializer : IDBInitializer
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly AppDbContext _db;
		private readonly ILogger<DBInitializer> _logger;


		public DBInitializer(UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager,
			AppDbContext db,
			ILogger<DBInitializer> logger)
		{
			_db = db;
			_roleManager = roleManager;
			_userManager = userManager;
			_logger = logger;
		}

		public async ValueTask Initialize()
		{
			try
			{
				if (_db.Database.GetPendingMigrations().Count() > 0)
				{
					_db.Database.Migrate();
				}

				if (!await _roleManager.RoleExistsAsync(SD.ROLE_ADMIN) || !await _roleManager.RoleExistsAsync(SD.ROLE_CLIENT))
				{
					await _roleManager.CreateAsync(new IdentityRole(SD.ROLE_ADMIN));
					await _roleManager.CreateAsync(new IdentityRole(SD.ROLE_CLIENT));
				}
				else
				{
					return;
				}

				var user = new IdentityUser()
				{
					UserName = "admin@admin.com",
					Email = "admin@admin.com",
					EmailConfirmed = true
				};
				await _userManager.CreateAsync(user, "95fur6u?_!deQ%8");
				await _userManager.AddToRoleAsync(user, SD.ROLE_ADMIN);
				await _userManager.AddToRoleAsync(user, SD.ROLE_CLIENT);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex.StackTrace);
				_logger.LogError(ex.Message);
			}
		}
	}
}
