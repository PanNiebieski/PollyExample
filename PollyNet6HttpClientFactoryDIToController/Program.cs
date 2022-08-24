using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("DaysApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7226/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>
 (r => !r.IsSuccessStatusCode).RetryAsync(3));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
