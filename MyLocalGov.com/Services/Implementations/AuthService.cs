using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.ViewModels;
using System.Threading.Tasks;

namespace MyLocalGov.com.Services.Implementations
{
	public class AuthService : IAuthService
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly IUnitOfWork _unitOfWork;

		public AuthService(
			UserManager<IdentityUser> userManager,
			SignInManager<IdentityUser> signInManager,
			IUnitOfWork unitOfWork)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_unitOfWork = unitOfWork;
		}

		public async Task<SignInResult> LoginAsync(LoginViewModel model)
		{
			return await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
		}

		public async Task<IdentityResult> RegisterAsync(RegisterViewModel model)
		{
			var user = new IdentityUser { UserName = model.Email, Email = model.Email };
			var result = await _userManager.CreateAsync(user, model.Password);

			if (!result.Succeeded)
				return result;

			try
			{
				using var transaction = await _unitOfWork.BeginTransactionAsync();
				// Create user profile
				var profile = new UserProfileModel(model, user);
				await _unitOfWork.UserProfiles.CreateProfileForUserAsync(profile);
				await _unitOfWork.SaveAsync();
				// Commit transaction
				await transaction.CommitAsync();

				// Only assign role after profile creation succeeds
				await _userManager.AddToRoleAsync(user, "Citizen");
			}
			catch
			{
				// Rollback user creation if profile creation fails
				await _userManager.DeleteAsync(user);
				var profileError = new IdentityError
				{
					Code = "ProfileCreationFailed",
					Description = "User account was created, but profile creation failed. The account has been removed."
				};
				return IdentityResult.Failed(profileError);
			}

			return result;
		}

		public async Task LogoutAsync()
		{
			await _signInManager.SignOutAsync();
		}
	}
}