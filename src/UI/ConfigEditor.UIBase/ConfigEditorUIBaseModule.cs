using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ty;

namespace ConfigEditor
{
    public class ConfigEditorUIBaseModule : ModuleBase
    {

        public override Task ConfigureServices(IHostApplicationBuilder builder)
        {
            builder.Services.AddTransient<ConfigManager>();
            return Task.CompletedTask;
        }
    }
}
