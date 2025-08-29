using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;

namespace MyLocalGov.com.Repositories
{
	public class IssueRepository : Repository<IssueModel>, IIssueRepository
	{
		private readonly MyLocalGovDbContext _context;

		public IssueRepository(MyLocalGovDbContext context) : base(context)
		{
			_context = context;
		}

		public async Task<IEnumerable<IssueModel>> GetByReporterAsync(string reporterUserId)
		{
			return await _context.Issues
				.Include(i => i.Reporter)
				.Include(i => i.Attachments)
				.Where(i => i.ReporterUserID == reporterUserId)
				.ToListAsync();
		}

		public async Task<IEnumerable<IssueModel>> GetByStatusAsync(int statusId)
		{
			return await _context.Issues
				.Include(i => i.Reporter)
				.Where(i => i.StatusID == statusId)
				.ToListAsync();
		}
	}
}