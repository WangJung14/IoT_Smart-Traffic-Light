using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("MeoMeo")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Hello World!!!");
    }
}