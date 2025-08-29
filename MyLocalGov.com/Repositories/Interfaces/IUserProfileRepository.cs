using MyLocalGov.com.Models;

namespace MyLocalGov.com.Repositories.Interfaces
{
	public interface IUserProfileRepository : IRepository<UserProfileModel>
	{
		Task<UserProfileModel?> GetByUserIdAsync(string userId);
	}
}