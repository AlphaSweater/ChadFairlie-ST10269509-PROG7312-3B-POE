using System.Threading;
using System.Threading.Tasks;

namespace MyLocalGov.com.Services.Interfaces
{
	public interface IAddressValidationService
	{
		Task<string> ValidateAsync(string requestJson, CancellationToken ct = default);
	}
}
