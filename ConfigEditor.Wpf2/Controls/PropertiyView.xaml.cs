using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConfigEditor.Wpf2.ViewModels;

namespace ConfigEditor.Wpf2.Controls
{
    public partial class PropertiyView : UserControl
    {
        public PropertiyView()
        {
            InitializeComponent();

            // initialize built-in commands that operate on this control's DataContext
            // if consumer overrides the dependency properties via binding, those will replace these defaults
            RemoveCommand = new DelegateCommand(p => Remove(p as ConfigViewModel));
            UpCommand = new DelegateCommand(p => Up(p as ConfigViewModel));
            DownCommand = new DelegateCommand(p => Down(p as ConfigViewModel));
        }

        public static readonly DependencyProperty AddCommandProperty = DependencyProperty.Register(
            nameof(AddCommand), typeof(ICommand), typeof(PropertiyView));
        public ICommand? AddCommand
        {
            get => (ICommand?)GetValue(AddCommandProperty);
            set => SetValue(AddCommandProperty, value);
        }

        public static readonly DependencyProperty SetObjectCommandProperty = DependencyProperty.Register(
            nameof(SetObjectCommand), typeof(ICommand), typeof(PropertiyView));
        public ICommand? SetObjectCommand
        {
            get => (ICommand?)GetValue(SetObjectCommandProperty);
            set => SetValue(SetObjectCommandProperty, value);
        }

        public static readonly DependencyProperty CopyCommandProperty = DependencyProperty.Register(
            nameof(CopyCommand), typeof(ICommand), typeof(PropertiyView));
        public ICommand? CopyCommand
        {
            get => (ICommand?)GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }

        public static readonly DependencyProperty PasteCommandProperty = DependencyProperty.Register(
            nameof(PasteCommand), typeof(ICommand), typeof(PropertiyView));
        public ICommand? PasteCommand
        {
            get => (ICommand?)GetValue(PasteCommandProperty);
            set => SetValue(PasteCommandProperty, value);
        }

        public static readonly DependencyProperty RemoveCommandProperty = DependencyProperty.Register(
            nameof(RemoveCommand), typeof(ICommand), typeof(PropertiyView));
        public ICommand? RemoveCommand
        {
            get => (ICommand?)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }

        public static readonly DependencyProperty UpCommandProperty = DependencyProperty.Register(
            nameof(UpCommand), typeof(ICommand), typeof(PropertiyView));
        public ICommand? UpCommand
        {
            get => (ICommand?)GetValue(UpCommandProperty);
            set => SetValue(UpCommandProperty, value);
        }

        public static readonly DependencyProperty DownCommandProperty = DependencyProperty.Register(
            nameof(DownCommand), typeof(ICommand), typeof(PropertiyView));
        public ICommand? DownCommand
        {
            get => (ICommand?)GetValue(DownCommandProperty);
            set => SetValue(DownCommandProperty, value);
        }

        // Methods that perform operations on the DataContext (assumed to be ConfigViewModel)
        private void Remove(ConfigViewModel? configViewModel)
        {
            if (DataContext is ConfigViewModel config && configViewModel is not null)
            {
                config.Properties.Remove(configViewModel);
            }
        }

        private void Up(ConfigViewModel? configViewModel)
        {
            if (DataContext is ConfigViewModel config && configViewModel is not null)
            {
                var index = config.Properties.IndexOf(configViewModel);
                if (index <= 0)
                {
                    return;
                }
                config.Properties.Move(index, index - 1);
            }
        }

        private void Down(ConfigViewModel? configViewModel)
        {
            if (DataContext is ConfigViewModel config && configViewModel is not null)
            {
                var index = config.Properties.IndexOf(configViewModel);
                if (index < 0 || index >= config.Properties.Count - 1)
                {
                    return;
                }
                config.Properties.Move(index, index + 1);
            }
        }

        // Simple ICommand implementation for delegate commands
        private class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;

            public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}
