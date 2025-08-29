using MyLocalGov.com.Models;

namespace MyLocalGov.com.Repositories.Interfaces
{
	public interface IIssueRepository : IRepository<IssueModel>
	{
		Task<IEnumerable<IssueModel>> GetByReporterAsync(string reporterUserId);
		Task<IEnumerable<IssueModel>> GetByStatusAsync(int statusId);
	}
}