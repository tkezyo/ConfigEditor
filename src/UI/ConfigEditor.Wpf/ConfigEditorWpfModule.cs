using ConfigEditor.ViewModels;
using ConfigEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ty;

namespace ConfigEditor
{
    public class ConfigEditorWpfModule : ModuleBase
    {
        public override void DependsOn()
        {
            AddDepend<ConfigEditorUIBaseModule>();
            AddDepend<TyWPFBaseModule>();
        }

        public override Task ConfigureServices(IHostApplicationBuilder builder)
        {
            builder.Services.AddTransientView<ConfigEditViewModel, ConfigEditView>();
            return Task.CompletedTask;
        }
    }
}
