using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;
using System.Reactive.Linq;
using Ty.Services;
using Ty.ViewModels;

namespace ConfigEditor.ViewModels
{
    public class ConfigEditViewModel : ViewModelBase
    {
        private readonly IMessageBoxManager _messageBoxManager;

        public ConfigEditViewModel(IMessageBoxManager messageBoxManager)
        {
            this._messageBoxManager = messageBoxManager;
            LoadConfigCommand = ReactiveCommand.CreateFromTask(LoadConfig);
        }

        [Reactive]
        public string? Path { get; set; }

        public ReactiveCommand<Unit, Unit> LoadConfigCommand { get; }
        //读取definition.json
        public async Task LoadConfig()
        {
            var file = await _messageBoxManager.OpenFiles.Handle(new OpenFilesInfo
            {
                Filter = "*.json",
                FilterName = "配置文件",
                Multiselect = false,
                Title = "打开配置"
            });
            if (file.Length == 0)
            {
                return;
            }

        }
    }
}
