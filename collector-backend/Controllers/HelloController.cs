using Microsoft.AspNetCore.Mvc;

namespace CollectorBackend.Controllers;

/// <summary>Provides the hello endpoint.</summary>
[ApiController]
[Route("api/hello")]
public sealed class HelloController : ControllerBase
{
    /// <summary>Returns the hello world message as plain text.</summary>
    [HttpGet]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ContentResult Get()
    {
        return Content("hello world", "text/plain");
    }
}
