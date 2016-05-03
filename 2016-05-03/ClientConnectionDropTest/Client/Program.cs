using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.CommandLineUtils;
using System.Threading;

namespace Client
{
    public class Program
    {
        private const string DefaultServerUri = "http://localhost:5000/";
        private const int DefaultTimeoutMillis = 1000;
        private const int DefaultIterationCount = 1;
        private const int DefaultConcurrency = 1;
        
        private static string _serverUri = DefaultServerUri;
        private static int _timeoutMillis = DefaultTimeoutMillis;
        private static int _iterationCount = DefaultIterationCount;
        
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "Client disconnect test",
                Description = "Client application to test disconnecting client http connections against a server"
            };
            app.HelpOption("-h|--help");
            var serverUriOption = app.Option("--uri|-u", $"The URI to target, defaults to {DefaultServerUri}", CommandOptionType.SingleValue);
            var timeoutOption = app.Option("--timeout|-t", $"The time in milliseconds the client will wait before closing the connection, defaults to {DefaultTimeoutMillis}, must be a positive int", CommandOptionType.SingleValue);
            var iterationCountOption = app.Option("--iterations|-i", $"The amount of sequential times to execute the test, defaults to {DefaultIterationCount}, must be a positive int", CommandOptionType.SingleValue);
            var concurrencyOption = app.Option("--concurrency|-c", $"The amount of sequential clients to have, defaults to {DefaultConcurrency}, must be a positive int", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (serverUriOption.HasValue())
                {
                    var serverUriString = serverUriOption.Value();
                    if (serverUriString == null)
                    {
                        app.ShowHelp();
                        return 1;
                    }
                    _serverUri = serverUriString;
                }
                
                if (timeoutOption.HasValue())
                {
                    if (!int.TryParse(timeoutOption.Value(), out _timeoutMillis) || _timeoutMillis < 0)
                    {
                        app.ShowHelp();
                        return 2;
                    }
                }
                
                if (iterationCountOption.HasValue())
                {
                    if (!int.TryParse(iterationCountOption.Value(), out _iterationCount) || _iterationCount < 0)
                    {
                        app.ShowHelp();
                        return 4;
                    }
                }
                
                var concurrency = DefaultConcurrency;
                if (concurrencyOption.HasValue())
                {
                    if (!int.TryParse(concurrencyOption.Value(), out concurrency) || concurrency < 0)
                    {
                        app.ShowHelp();
                        return 8;
                    }
                }
                
                Console.WriteLine($"Server URI: {_serverUri}, client timeout: {_timeoutMillis}, iteration count: {_iterationCount}, concurrency: {concurrency}");
                
                if(!VerifyConnectionWorks())
                {
                    Console.Error.WriteLine($"Couldn't connect successfully to {_serverUri}");
                    return 16;
                }
                
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                var executionTasks = new Task[concurrency];
                for(int i = 0; i < concurrency; ++i)
                {
                    executionTasks[i] = Task.Run(() => RunTest());
                }
                
                Task.WhenAll(executionTasks).Wait();
                
                sw.Stop();                
                Console.WriteLine($"Done in {sw.ElapsedMilliseconds} ms");
                return 0;
            });
            
            return app.Execute(args);
        }
        
        private static bool VerifyConnectionWorks()
        {
            using(var client = new HttpClient())
            {
                try
                {
                    var result = client.GetAsync(_serverUri).Result;
                    result.EnsureSuccessStatusCode();
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
        
        private static void RunTest()
        {
            do
            {
                var client = new HttpClient();
                var requestTask = client.GetAsync(_serverUri);
                Task.Delay(_timeoutMillis).Wait();
                client.Dispose();
                
                try
                {
                    requestTask.Wait();
                    Console.WriteLine("Request completed");
                }
                catch(AggregateException a)
                {
                    foreach(var e in a.InnerExceptions)
                    {
                        if(e is TaskCanceledException)
                        {
                            //"A task was canceled" exception, this is expected
                            Console.WriteLine("Request cancelled successfully");
                        }
                        else
                        {
                            // anything else:
                            Console.Error.WriteLine(e);
                        }
                    }
                }
                catch(Exception e)
                {
                    // anything else:
                    Console.Error.WriteLine(e);
                }
            } while (--_iterationCount > 0);
        }
    }
}
