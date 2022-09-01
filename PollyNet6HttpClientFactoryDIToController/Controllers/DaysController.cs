using Microsoft.AspNetCore.Mvc;

namespace PollyNet6HttpClientFactoryDIToController.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DaysController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public DaysController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var httpClient = _httpClientFactory.CreateClient("DaysApi");
            string requestEndpoint = $"todolist/{id}";

            HttpResponseMessage response = await httpClient.GetAsync(requestEndpoint);

            if (response.IsSuccessStatusCode)
            {
                string toDoList = await response.Content.
                    ReadFromJsonAsync<string>();
                return Ok(toDoList);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }
    }
}
