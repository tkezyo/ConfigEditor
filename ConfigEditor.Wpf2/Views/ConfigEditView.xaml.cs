using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigEditor.Wpf2.Views
{
    public partial class ConfigEditView : UserControl
    {
        public ConfigEditView()
        {
            InitializeComponent();

            // resolve viewmodel from app service provider
            var sp = (System.Windows.Application.Current as App)?.ServiceProvider;
            if (sp is not null)
            {
                var vm = sp.GetService<ViewModels.ConfigEditViewModel>();
                this.DataContext = vm;
            }
        }
    }
}
