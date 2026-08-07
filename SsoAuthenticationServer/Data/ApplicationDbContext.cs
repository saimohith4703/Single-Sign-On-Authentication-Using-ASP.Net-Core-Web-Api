using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SsoAuthenticationServer.Models;

namespace SsoAuthenticationServer.Data
{
	public class ApplicationDbContext:IdentityDbContext<IdentityUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options)
		{
			
		}

		public DbSet<SSOToken> SSOTokens{ get; set; }
	}
}
