using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.Services.Interfaces;

namespace MyLocalGov.com.Controllers
{
	[ApiController]
	[Route("api/address")]
	public class MapsController : ControllerBase
	{
		private readonly IAddressValidationService _service;

		public MapsController(IAddressValidationService service)
		{
			_service = service;
		}

		[HttpPost("validate")]
		public async Task<IActionResult> Validate([FromBody] JsonElement payload, CancellationToken ct)
		{
			try
			{
				var json = payload.GetRawText();
				var result = await _service.ValidateAsync(json, ct);
				return Content(result, "application/json");
			}
			catch (Exception ex)
			{
				// Hide internal error details from clients; log if needed.
				return Problem(title: "Address validation failed", detail: ex.Message, statusCode: 502);
			}
		}
	}
}
