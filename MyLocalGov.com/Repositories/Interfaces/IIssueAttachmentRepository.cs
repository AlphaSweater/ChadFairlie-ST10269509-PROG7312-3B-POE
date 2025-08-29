using MyLocalGov.com.Models;

namespace MyLocalGov.com.Repositories.Interfaces
{
	public interface IIssueAttachmentRepository : IRepository<IssueAttachmentModel>
	{
		Task<IEnumerable<IssueAttachmentModel>> GetByIssueIdAsync(int issueId);
	}
}