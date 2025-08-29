using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using MyLocalGov.com.Data;
using MyLocalGov.com.Repositories.Interfaces;

namespace MyLocalGov.com.Repositories
{
	/// <summary>
	/// UnitOfWork implementation.
	/// Provides access to all repositories and coordinates saving changes.
	/// Ensures that multiple changes are saved together atomically.
	/// </summary>
	public class UnitOfWork : IUnitOfWork
	{
		private readonly MyLocalGovDbContext _context;

		// Exposed repositories (injected via DI)
		public IUserProfileRepository UserProfiles { get; }
		public IIssueRepository Issues { get; }
		public IIssueAttachmentRepository IssueAttachments { get; }

		/// <summary>
		/// Constructor. All repositories are injected here so you can access them through UnitOfWork.
		/// </summary>
		public UnitOfWork(
			MyLocalGovDbContext context,
			IUserProfileRepository userProfiles,
			IIssueRepository issues,
			IIssueAttachmentRepository issueAttachments)
		{
			_context = context;
			UserProfiles = userProfiles;
			Issues = issues;
			IssueAttachments = issueAttachments;
		}

		/// <summary>
		/// Save all pending changes to the database in one call.
		/// </summary>
		public async Task<int> SaveAsync()
		{
			return await _context.SaveChangesAsync();
		}

		/// <summary>
		/// Manually begin a transaction. 
		/// Useful when you want to group multiple SaveAsync() calls together.
		/// Example: Create an Issue, add Attachments, and update Reporter points in one transaction.
		/// </summary>
		public async Task<IDbContextTransaction> BeginTransactionAsync()
		{
			return await _context.Database.BeginTransactionAsync();
		}

		/// <summary>
		/// Dispose of the DbContext when UnitOfWork is disposed.
		/// </summary>
		public void Dispose()
		{
			_context.Dispose();
		}
	}
}

/*
=========================================================
 EXAMPLE USAGE OF UNIT OF WORK
=========================================================

// 1) Inject IUnitOfWork into your Service or Controller
private readonly IUnitOfWork _unitOfWork;

public IssueService(IUnitOfWork unitOfWork)
{
    _unitOfWork = unitOfWork;
}

// 2) Basic usage without explicit transaction
public async Task ReportIssueAsync(IssueModel issue, IEnumerable<IssueAttachmentModel> attachments)
{
    await _unitOfWork.Issues.AddAsync(issue);

    foreach (var attachment in attachments)
    {
        attachment.IssueID = issue.IssueID;
        await _unitOfWork.IssueAttachments.AddAsync(attachment);
    }

    // ✅ Commit all changes at once
    await _unitOfWork.SaveAsync();
}

// 3) Advanced usage with manual transaction
public async Task<bool> ReportIssueWithTransactionAsync(IssueModel issue, IEnumerable<IssueAttachmentModel> attachments)
{
    using var transaction = await _unitOfWork.BeginTransactionAsync();
    try
    {
        await _unitOfWork.Issues.AddAsync(issue);

        foreach (var attachment in attachments)
        {
            attachment.IssueID = issue.IssueID;
            await _unitOfWork.IssueAttachments.AddAsync(attachment);
        }

        // You could also update Reporter reputation here
        var reporter = await _unitOfWork.UserProfiles.GetByUserIdAsync(issue.ReporterUserID);
        reporter.ReputationPoints += 10;
        await _unitOfWork.UserProfiles.UpdateAsync(reporter);

        // ✅ Save changes
        await _unitOfWork.SaveAsync();

        // ✅ Commit transaction
        await transaction.CommitAsync();
        return true;
    }
    catch
    {
        // ❌ Rollback transaction if anything fails
        await transaction.RollbackAsync();
        throw; // Re-throw the exception
    }
}

=========================================================
*/