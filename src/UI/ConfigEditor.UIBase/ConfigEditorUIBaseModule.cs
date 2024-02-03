using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
