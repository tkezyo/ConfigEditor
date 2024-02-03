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
        private readonly ConfigManager _configManager;

        public ConfigEditViewModel(IMessageBoxManager messageBoxManager, ConfigManager configManager)
        {
            this._messageBoxManager = messageBoxManager;
            this._configManager = configManager;
            LoadConfigCommand = ReactiveCommand.CreateFromTask(LoadConfig);
        }

        [Reactive]
        public string? Path { get; set; }


        public ReactiveCommand<Unit, Unit> LoadConfigCommand { get; }
        //读取 definition.json
        public async Task LoadConfig()
        {

            var files = await _messageBoxManager.OpenFiles.Handle(new OpenFilesInfo
            {
                Filter = "*.json",
                FilterName = "配置文件",
                Multiselect = false,
                Title = "打开配置"
            });
            if (files.Length == 0)
            {
                return;
            }
            var file = files[0];
            if (file.Contains("definition"))
            {
                file = file.Replace("definition.", "");
            }
            Path = file;

        }
    }
}
