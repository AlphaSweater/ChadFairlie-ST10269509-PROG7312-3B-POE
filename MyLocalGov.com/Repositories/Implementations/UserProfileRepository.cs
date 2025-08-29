using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;

namespace MyLocalGov.com.Repositories.Implementations
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

		public async Task<UserProfileModel> CreateProfileForUserAsync(UserProfileModel newUserProfile)
		{
			await _context.UserProfiles.AddAsync(newUserProfile);
			await _context.SaveChangesAsync();

			return newUserProfile;
		}
	}
}
