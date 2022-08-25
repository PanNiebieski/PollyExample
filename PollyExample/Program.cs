using Polly.Timeout;
using Polly;

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("Oto kombinacja polityki Retry i TimeOut");
Console.WriteLine("\r\nWciśnij 'a' lub 'A' aby odwołać operacje..."); 
Console.ResetColor();


//is not preserving the synchronization context?
Task.Factory.StartNew(async () => await ExecuteTask(cancellationToken));
//await ExecuteTask(cancellationToken);

//Wyślij token wycofania w wątku UI
char ch = ' ';
do
{
    ch = Console.ReadKey().KeyChar;
    if (ch == 'a' || ch == 'A')
    {
        cancellationTokenSource.Cancel();
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("\nWysłałeś prośbę anulowania i zaraz zostanie ona zrealizowana");
        Console.ResetColor();
    }

} while(ch != 'a' && ch != 'A');
Console.ForegroundColor = ConsoleColor.DarkGreen;
Console.WriteLine("\r\nKONIEC...");
Console.ResetColor();
Console.ReadKey();



static async Task ExecuteTask(CancellationToken cancellationToken)
{
    var maxRetryAttempts = 4;
    var pauseBetweenFailures = TimeSpan.FromSeconds(1);
    var timeoutInSec = 5;

    //Retry Policy
    var retryPolicy = Policy
        .Handle<Exception>()
        //.Or<AnyOtherException>()
        .WaitAndRetryAsync(
            maxRetryAttempts,
            i => pauseBetweenFailures,
            (exception, timeSpan, retryCount, context) =>
            Tools.ConsoleWriteRetryException(exception, timeSpan, retryCount, context));

    //TimeOut Policy
    var timeOutPolicy = Policy
        .TimeoutAsync(
            timeoutInSec,
            TimeoutStrategy.Pessimistic,
            (context, timeSpan, task) => 
            Tools.ConsoleWriteTimeoutException(context, timeSpan, task));

    //Połącz dwie polityki
    var policyWrap = Policy.WrapAsync(retryPolicy, timeOutPolicy);

    //Wykonajny jakieś zadanie w tle
    await policyWrap.ExecuteAsync(async (context, token) =>
    {
        Console.WriteLine("\r\nWykonywanie zadania...");

      
        var result = await Tools.MyOperationThatWillFourTimesFail(token);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Zwrócony napis brzmi : {result}");
        Console.ResetColor();

    }, new Dictionary<string, object>() { { "Nazwa operacji", "Opis..." } }, 
    cancellationToken);

    return;
}


public static class Tools
{
    public static void ConsoleWriteRetryException(Exception exception, TimeSpan timeSpan,
        int retryCount, Context context)
    {
        var action = context != null ? context.First().Key : "nieznana metoda";
        var actionDescription = context != null ? context.First().Value : "nieznany opis";
        var msg = 
            $"Próba numer : ({retryCount}) --czego-> {action} " +
            $"({actionDescription}) : {exception.Message}";
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static Task ConsoleWriteTimeoutException(Context context, 
        TimeSpan timeSpan, Task task)
    {
        var action = context != null ? context.First().Key : "nieznana metoda";
        var actionDescription = context != null ? context.First().Value : "nieznany opis";

        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var msg = $"Akcja {action} ({actionDescription})" +
                $" przekroczyła czas po {timeSpan.TotalSeconds} sekundach," +
                $" została on zakończona z: {t.Exception}.";
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
            else if (t.IsCanceled)
            {
                var msg = $"Akcja {action} ({actionDescription})" +
                $" przekroczyła czas po {timeSpan.TotalSeconds}" +
                $" sekundach, i została anulowana.";
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        });

        return task;
    }

    private static int Number = 0;

    public static async Task<string> MyOperationThatWillFourTimesFail(CancellationToken ct)
    {
        //Już została wysłana prośba wycofania?
        if (ct.IsCancellationRequested == true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine
                ("Task został anulowany zanim się rozpoczął. Prawodpobonie przez Ciebie");
            Console.ResetColor();
            ct.ThrowIfCancellationRequested();
        }

        await Task.Delay(1500);

        //Wycofaj operacje jeśli była taka prośba
        if (ct.IsCancellationRequested == true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine
                ("Task został anulowany w drugime etapie. Prawodpobonie przez Ciebie");
            Console.ResetColor();
            ct.ThrowIfCancellationRequested();
        }

        //Losowo wygenerowane
        Number++;
        if (Number <= 4)
        {
            Random random = new Random();
            if (random.Next(1, 10) < 3)
                await Task.Delay(5000);
        }

        //Sprawdzenie czy w tym momencie bo być może poleciał TimeOut
        if (ct.IsCancellationRequested == true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine
                ("Task został anulowany w trzecim etapie! Zapewne operacja trwała zbyt długo");
            Console.ResetColor();
            ct.ThrowIfCancellationRequested();
        }
 
        if (Number <= 4)
        {
            //Wyrzuć błąd aby zobaczyć działanie polityki Retry
            throw new Exception("500 SeverNullErrorReference");
        }


        return "---- ZROBIONE ----";
    }




}

