using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace MyLocalGov.com.Repositories.Interfaces
{
	/// <summary>
	/// Unit of Work pattern coordinates multiple repositories 
	/// to ensure atomic database operations.
	/// It wraps SaveChanges in a single place, and optionally supports transactions.
	/// </summary>
	public interface IUnitOfWork : IDisposable
	{
		// Expose repositories here (one property per repository)
		IUserProfileRepository UserProfiles { get; }
		IIssueRepository Issues { get; }
		IIssueAttachmentRepository IssueAttachments { get; }

		/// <summary>
		/// Saves all changes made across repositories in a single call.
		/// Equivalent to DbContext.SaveChangesAsync().
		/// </summary>
		Task<int> SaveAsync();

		/// <summary>
		/// Starts a new database transaction manually.
		/// Normally not required, since EF Core wraps SaveChanges in a transaction,
		/// but useful if you want multiple SaveAsync() calls in the same transaction.
		/// </summary>
		Task<IDbContextTransaction> BeginTransactionAsync();
	}
}
