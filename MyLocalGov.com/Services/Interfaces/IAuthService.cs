using Microsoft.AspNetCore.Identity;
using MyLocalGov.com.ViewModels;

namespace MyLocalGov.com.Services.Interfaces
{
	/// <summary>
	/// Interface for authentication and registration services.
	/// </summary>
	public interface IAuthService
	{
		Task<IdentityResult> RegisterAsync(RegisterViewModel model);

		Task<SignInResult> LoginAsync(LoginViewModel model);

		Task LogoutAsync();
	}
}