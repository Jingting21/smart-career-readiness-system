using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/registration-options")]
public sealed class RegistrationOptionsController(AutocompleteCatalogService catalogs) : ControllerBase
{
    [HttpGet("universities")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public IActionResult Universities([FromQuery] string? q) =>
        Ok(catalogs.SearchUniversities(q));

    [HttpGet("courses")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public IActionResult Courses([FromQuery] string? q) =>
        Ok(catalogs.SearchCourses(q));
}
