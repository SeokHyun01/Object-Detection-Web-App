using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Data
{
	public class AppDbContext : IdentityDbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{

		}

		public DbSet<Camera> Cameras { get; set; }
		public DbSet<Event> Events { get; set; }
		public DbSet<BoundingBox> BoundingBoxes { get; set; }
		public DbSet<AppUser> AppUsers { get; set; }
	}
}
