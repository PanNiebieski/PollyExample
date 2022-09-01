using Polly;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var retryPolicy = 
    Policy.HandleResult<HttpResponseMessage>
    (r => !r.IsSuccessStatusCode).RetryAsync(3);


builder.Services.AddHttpClient("DaysApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7010/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddPolicyHandler(retryPolicy);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

int _requestCount = 0;

app.MapGet("/todolist/{id}", (int id) =>
{
    _requestCount++;

    if (_requestCount % 4 == 0) 
    // tylko jedno na cztery zapytań wykonają się poprawnie
    {
        if (id == 1)
            return Results.Ok("Zrobić zupę");
        else
            return Results.Ok("Zrobić podcast");
    }


    System.Console.WriteLine($"{_requestCount} Coś poszło nie tak");
    return Results.Problem("Something went wrong");
});

app.MapGet("/Day/{id}", async (int id, 
    IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient("DaysApi");

    string requestEndpoint = $"todolist/{id}";

    HttpResponseMessage response = await httpClient.
    GetAsync(requestEndpoint);

    if (response.IsSuccessStatusCode)
    {
        string? toDoList = await response.Content.
        ReadFromJsonAsync<string>();
        return Results.Ok(toDoList);
    }
    return Results.Problem("Coś poszło nie tak");

});

app.Run();




