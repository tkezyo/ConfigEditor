using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Test1;
using Ty.Services;
using Ty.ViewModels;

namespace ConfigEditor.ViewModels;

public class ConfigEditViewModel : ViewModelBase
{
    private readonly IMessageBoxManager _messageBoxManager;
    private readonly ConfigManager _configManager;

    public ConfigEditViewModel(IMessageBoxManager messageBoxManager, ConfigManager configManager)
    {
        this._messageBoxManager = messageBoxManager;
        this._configManager = configManager;
        LoadConfigCommand = ReactiveCommand.CreateFromTask(LoadConfig);
        CreateTestCommand = ReactiveCommand.CreateFromTask(CreateTest);
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

    public ReactiveCommand<Unit, Unit> CreateTestCommand { get; }
    public async Task CreateTest()
    {
        var config = await _configManager.Read<DemoConfig>("./Configs", "democonfig");
        var model = await _configManager.ReadDefinition<DemoConfig>("./Configs", "democonfig");
    }
}

public class ConfigViewModel(string name) : ReactiveObject
{
    public string Name { get; set; } = name;
    [Reactive]
    public ConfigModelType Type { get; set; }
    [Reactive]
    public int Order { get; set; }
    [Reactive]
    public string? SubType { get; set; }
    [Reactive]
    public string? DisplayName { get; set; }
    [Reactive]
    public string? GroupName { get; set; }
    [Reactive]
    public string? Description { get; set; }
    [Reactive]
    public string? Prompt { get; set; }
    [Reactive]
    public int? Minimum { get; set; }
    [Reactive]
    public int? Maximum { get; set; }
    [Reactive]
    public ObservableCollection<string>? AllowedValues { get; set; }
    [Reactive]
    public ObservableCollection<string>? DeniedValues { get; set; }
    [Reactive]
    public bool Required { get; set; }
    [Reactive]
    public string? RegularExpression { get; set; }

    public ObservableCollection<ConfigViewModel> Properties { get; set; } = [];
}