using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace PollyNet6HttpClientFactoryDIToController.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ToDoListController : Controller
    {
        static int _requestCount = 0;

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            await Task.Delay(100);
            _requestCount++;

            if (_requestCount % 4 == 0) // only one of out four requests will succeed
            {
                if (id == 1)
                    return Ok("Zrobić zupę");
                else
                    return Ok("Zrobić podcast");
            }
            System.Console.WriteLine($"{_requestCount} Coś poszło nie tak");
            return StatusCode((int)HttpStatusCode.InternalServerError, "Coś poszło nie tak");
        }
    }
}
