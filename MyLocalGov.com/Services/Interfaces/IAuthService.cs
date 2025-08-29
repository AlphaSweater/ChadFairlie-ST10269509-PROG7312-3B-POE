using Microsoft.AspNetCore.Identity;
using MyLocalGov.com.Models;
using MyLocalGov.com.ViewModels;
using System.Threading.Tasks;

namespace MyLocalGov.com.Services.Interfaces
{
	public interface IAuthService
	{
		Task<IdentityResult> RegisterAsync(RegisterViewModel model);
		Task<SignInResult> LoginAsync(LoginViewModel model);
		Task LogoutAsync();
	}
}