using Microsoft.Extensions.DependencyInjection;
using Ty;

namespace ConfigEditor
{
    public class ConfigEditorUIBaseModule : ModuleBase
    {
        public override Task ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddTransient<ConfigManager>();
            return Task.CompletedTask;
        }
    }
}
