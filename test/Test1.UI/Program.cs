﻿using Microsoft.Extensions.Hosting;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using System;
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
           .Enrich.FromLogContext();

            Log.Logger = configuration.CreateLogger();

            var host = await IModule.CreateHost<Test1UIModule>(args) ?? throw new Exception();

            //在STA线程上运行
            Thread thread = new(() =>
            {
                using (host)
                {
                    host.Start();
                    host.WaitForShutdown();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);

            thread.Start();
            thread.Join();

        }
    }
}
