using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EnableRewindBug
{
    public class Program
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
        public static void Main(string[] args)
        {
            PrintLine($"Running in { IntPtr.Size * 8 } bits");

            Task.Run(() =>
            {
                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseDefaultHostingConfiguration(args)
                    .UseIIS()
                    .UseStartup<Program>()
                    .Build();

                host.Run();
            });
            
            Task.Delay(TimeSpan.FromSeconds(20));
            PrintLine("Start");
            try
            {
                var client = new Client();
                
                for (var i = 1; i <= 1; ++i)
                {
                    PrintLine($"Iteration { i }");

                    // Scenario 0: Small text part + large text part: 100KB
                    PrintLine("Scenario 1");
                    client.SendLoad(client.Scenario0FileContentGenerator).Wait();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            PrintLine("Done.");
        }

        private static void PrintLine(string input, params object[] paramStrings)
        {
            Console.Write($"[{ DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }] ");
            Console.WriteLine(input, paramStrings);
        }
    }
}
