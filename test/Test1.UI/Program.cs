using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.Reactive.Linq;
using Ty;

namespace Test1
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var configuration = new LoggerConfiguration()
#if DEBUG
           .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
           .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
           .Enrich.FromLogContext();

            Log.Logger = configuration.CreateLogger();

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(async services =>
                {
                    await IModule.ConfigureServices<Test1UIModule>(services);
                })
                .Build();

            using (host)
            {
                host.Start();
                host.WaitForShutdown();
            }
        }
    }
}
