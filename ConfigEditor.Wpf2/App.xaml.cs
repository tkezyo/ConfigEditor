using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace ConfigEditor.Wpf2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var services = new ServiceCollection();

            // Register application services and views
            services.AddSingleton<ConfigEditor.ConfigManager>();
            services.AddTransient<Views.ConfigEditView>();
            services.AddTransient<ViewModels.ConfigEditViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (ServiceProvider is IDisposable d)
            {
                d.Dispose();
            }
            base.OnExit(e);
        }
    }
}
