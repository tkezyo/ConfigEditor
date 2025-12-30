using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Wpf.Ui.Appearance;

namespace ConfigEditor.Wpf2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);

            // get service provider from App
            _serviceProvider = (Application.Current as App)?.ServiceProvider ?? null!;

            // resolve and set content view
            var view = _serviceProvider?.GetService(typeof(Views.ConfigEditView)) as FrameworkElement;
            if (view is not null)
            {
                ContentHost.Content = view;
            }
        }
    }
}