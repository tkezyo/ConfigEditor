using ConfigEditor.ViewModels;
using ConfigEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Ty;

namespace ConfigEditor
{
    public class ConfigEditorWpfModule : ModuleBase
    {
        public override Task ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddBaseViews();
            serviceDescriptors.AddTransientView<ConfigEditViewModel, ConfigEditView>();
            return Task.CompletedTask;
        }
    }
}
