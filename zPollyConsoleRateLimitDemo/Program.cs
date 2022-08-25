using Newtonsoft.Json;
using Polly;
using Polly.RateLimit;
using System.Globalization;

var rateLimit = Policy.RateLimitAsync(10, TimeSpan.FromSeconds(10), 10);
List<string> results = new List<string>();

for (int i = 1; i < 61; i++)
{
    results.Add(await SearchAsync(i, rateLimit));
}

foreach (var item in results)
{
    Console.WriteLine(item);
}

Console.WriteLine("Czeka...");
await Task.Delay(600);
Console.WriteLine(await SearchAsync(62, rateLimit));

async Task<string> SearchAsync(int query, AsyncRateLimitPolicy rateLimit)
{

    try
    {
        var result = await rateLimit.ExecuteAsync(() => TextSearchAsync(query));

        var json = JsonConvert.SerializeObject(result);

        return json;
    }
    catch (RateLimitRejectedException ex)
    {
        return "Try After : " + ex.RetryAfter.Milliseconds.ToString() + " Milliseconds";
    }
}

async Task<TextResult> TextSearchAsync(int query)
{
    await Task.Delay(500);
    return new TextResult() { Value = "-- DONE -- QueryId : " + query };
}

