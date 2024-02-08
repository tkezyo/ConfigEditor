using Microsoft.Extensions.Hosting;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ty;

namespace Test1
{
    class Program
    {
        [STAThread]
        public static async Task Main(string[] args)
        {
            var configuration = new LoggerConfiguration()
#if DEBUG
           .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
           .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
           //输出到文件
           .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
           .Enrich.FromLogContext();

            var host = await IModule.CreateHost<Test1UIModule>(args, skipVerification: true) ?? throw new Exception();

            Start(host);
        }
        public static void Start(IHost host)
        {
            //var host = Host.CreateDefaultBuilder()
            //    .ConfigureServices((context, services) =>
            //    {
            //        services.AddHostedService<Worker>();
            //    })
            //    .UseSerilog()
            //    .Build();

            //host.Run();
            //在STA线程上运行
            Thread thread = new(() =>
            {
                using (host)
                {
                    host.Run();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);

            thread.Start();
            thread.Join();

        }
    }

}
