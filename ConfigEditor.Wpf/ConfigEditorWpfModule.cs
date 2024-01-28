using ConfigEditor.ViewModels;
using ConfigEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Ty;

namespace ConfigEditor
{
    public class ConfigEditorWpfModule : ModuleBase
    {
        public override void DependsOn()
        {
            AddDepend<TyWPFBaseModule>();
        }
        public override Task ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddTransientView<ConfigEditViewModel, ConfigEditView>();
            return Task.CompletedTask;
        }
    }
}
