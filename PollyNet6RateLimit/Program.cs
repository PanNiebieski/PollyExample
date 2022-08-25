using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Polly.RateLimit;
using Polly.Retry;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

int RateLimitRetryCount = 10;
IAsyncPolicy<HttpResponseMessage> ratePolicy = Policy
    .RateLimitAsync(RateLimitRetryCount, TimeSpan.FromSeconds(5), RateLimitRetryCount,
        (retryAfter, context) =>
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
            response.Headers.Add("Retry-After", retryAfter.Milliseconds.ToString().Replace(',','_'));
            return response;
        });

builder.Services.AddHttpClient("TextAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7251/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddPolicyHandler(ratePolicy); 


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapGet("/TextSearch/{query}", async 
    (HttpContext context,int query) =>
{
    //var rateLimit = Policy.RateLimitAsync(3, TimeSpan.FromMinutes(5), 1);

    //try
    //{
        var result = await TextSearchAsync(query);

        var json = JsonConvert.SerializeObject(result);

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    //}
    //catch (RateLimitRejectedException ex)
    //{
    //    string retryAfter = DateTimeOffset.UtcNow
    //        .Add(ex.RetryAfter)
    //        .ToUnixTimeSeconds()
    //        .ToString(CultureInfo.InvariantCulture);

    //    context.Response.StatusCode = 429;
    //    context.Response.Headers["Retry-After"] = retryAfter;
    //}
});

async Task<TextResult> TextSearchAsync(int query)
{
    await Task.Delay(100);
    return new TextResult() { Value = "-- DONE -- QueryId : " + query };
}

app.MapGet("/TestSequentially/{id}", async (int id, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient("TextAPI");
    List<string> jsonresult = new List<string>();

    for (int i = 1; i < 60; i++)
    {
        var newid = id + i;
        string requestEndpoint = $"TextSearch/{newid}";
        HttpResponseMessage response =
            await httpClient.GetAsync(requestEndpoint);
        try
        {
            response.EnsureSuccessStatusCode();

            TextResult textresult =
                    await response.Content.ReadFromJsonAsync<TextResult>();
                jsonresult.Add(textresult.Value);
              
            
        }
        catch (Exception ex)
        {
            jsonresult.Add(ex.Message);
        }
    }

    return Results.Ok(jsonresult);
});

app.MapGet("/TestConcurrently/{id}", async (int id, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient("TextAPI");
    List<int> ids = new List<int>();

    for (int i = 1; i < 60; i++)
    {
        ids.Add(id+i);
    }

    var tasks = ids
    .Select(async id => new
    {
        Response = await httpClient.GetAsync($"TextSearch/{id}")
    });

    var results = await Task.WhenAll(tasks);

    List<string> jsonresult = new List<string>();
    foreach (var result in results)
    {

        if (result.Response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            jsonresult.Add("Too Many Request : " + result.Response.Headers.ToString());

        if (result.Response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            TextResult textresult =
            await result.Response.Content.ReadFromJsonAsync<TextResult>();
            jsonresult.Add(textresult.Value);
        }
    }




    return Results.Ok(jsonresult);
});



app.Run();


// Allow up to 5 executions per second.
var p1 = Policy.RateLimit(5, TimeSpan.FromSeconds(1));

// Allow up to 5 executions per second with a burst of 10 executions.
var p2 = Policy.RateLimit(5, TimeSpan.FromSeconds(1), 10);

// Allow up to 5 executions per second, with a delegate to return the
// retry-after value to use if the rate limit is exceeded.
var p3 = Policy.RateLimit(5, TimeSpan.FromSeconds(1), (retryAfter, context) =>
{
    return retryAfter.Add(TimeSpan.FromSeconds(2));
});

// Allow up to 20 executions per second with a burst of 10 executions,
// with a delegate to return the retry-after value to use if the rate
// limit is exceeded.
var p4 = Policy.RateLimit(5, TimeSpan.FromSeconds(1), 10, (retryAfter, context) =>
{
    return retryAfter.Add(TimeSpan.FromSeconds(2));
});


//int RateLimitRetryCount = 2;
//IAsyncPolicy<HttpResponseMessage> ratePolicy = Policy
//    .RateLimitAsync(RateLimitRetryCount, TimeSpan.FromSeconds(5), RateLimitRetryCount,
//        (retryAfter, context) =>
//        {
//            var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
//            response.Headers.Add("Retry-After", retryAfter.TotalSeconds.ToString());
//            return response;
//        });

////Połącz dwie polityki
//var policyWrap = Policy.WrapAsync(retryPolicy, ratePolicy);



