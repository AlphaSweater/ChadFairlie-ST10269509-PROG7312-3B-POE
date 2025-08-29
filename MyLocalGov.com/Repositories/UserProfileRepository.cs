using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;

namespace MyLocalGov.com.Repositories
{
	public class UserProfileRepository : Repository<UserProfileModel>, IUserProfileRepository
	{
		private readonly MyLocalGovDbContext _context;

		public UserProfileRepository(MyLocalGovDbContext context) : base(context)
		{
			_context = context;
		}

		public async Task<UserProfileModel?> GetByUserIdAsync(string userId)
		{
			return await _context.UserProfiles
								 .Include(u => u.User)
								 .FirstOrDefaultAsync(u => u.UserID == userId);
		}
	}
}
