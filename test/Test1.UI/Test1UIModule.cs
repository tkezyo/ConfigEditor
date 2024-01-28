using ConfigEditor;
using ConfigEditor.ViewModels;
using ConfigEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Threading.Tasks;
using Test1.UI;
using Ty;
using Ty.Views;

namespace Test1
{
    public class Test1UIModule : ModuleBase
    {
        public override void DependsOn()
        {
            AddDepend<ConfigEditorWpfModule>();
        }
        public override Task ConfigureServices(IServiceCollection serviceDescriptors)
        {

            serviceDescriptors.AddSingleton<App>();
            serviceDescriptors.AddTransient<MainWindow>();
            serviceDescriptors.AddHostedService<WpfHostedService<App, MainWindow>>();
            serviceDescriptors.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));


            //serviceDescriptors.AddAutoMapper(typeof(ConfigProfile).Assembly);

            serviceDescriptors.Configure<PageOptions>(options =>
            {
                options.FirstLoadPage = typeof(ConfigEditViewModel);
                options.Title = "配置编辑器";
            });
      
            return Task.CompletedTask;
        }
    }
}
