using ConfigEditor.ViewModels;
using ReactiveUI;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ConfigEditor.Controls
{
    /// <summary>
    /// PropertiyView.xaml 的交互逻辑
    /// </summary>
    public partial class PropertiyView : UserControl
    {
        //绑定属性 AddCommand
        public static readonly DependencyProperty AddCommandProperty = DependencyProperty.Register("AddCommand", typeof(ICommand), typeof(PropertiyView));
        public ICommand AddCommand
        {
            get { return (ICommand)GetValue(AddCommandProperty); }
            set { SetValue(AddCommandProperty, value); }
        }

        //绑定属性 RemoveCommand
        public ReactiveCommand<ConfigViewModel, Unit> RemoveCommand { get; set; }
        public void Remove(ConfigViewModel configViewModel)
        {
            if (DataContext is ConfigViewModel config)
            {
                config.Properties.Remove(configViewModel);
            }
        }
        public PropertiyView()
        {
            RemoveCommand = ReactiveCommand.Create<ConfigViewModel>(Remove);
            InitializeComponent();
        }
    }
}
