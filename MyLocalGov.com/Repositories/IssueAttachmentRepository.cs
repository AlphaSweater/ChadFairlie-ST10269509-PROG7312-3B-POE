using Microsoft.EntityFrameworkCore;
using MyLocalGov.com.Data;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;

namespace MyLocalGov.com.Repositories
{
	public class IssueAttachmentRepository : Repository<IssueAttachmentModel>, IIssueAttachmentRepository
	{
		private readonly MyLocalGovDbContext _context;

		public IssueAttachmentRepository(MyLocalGovDbContext context) : base(context)
		{
			_context = context;
		}

		public async Task<IEnumerable<IssueAttachmentModel>> GetByIssueIdAsync(int issueId)
		{
			return await _context.IssueAttachments
				.Where(a => a.IssueID == issueId)
				.ToListAsync();
		}
	}
}