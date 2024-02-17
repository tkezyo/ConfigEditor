using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
        SaveConfigCommand = ReactiveCommand.CreateFromTask(SaveConfig);
        AddPropertyCommand = ReactiveCommand.Create<ConfigViewModel>(AddProperty);
    }

    [Reactive]
    public string? Path { get; set; }

    public ObservableCollection<ConfigViewModel> Configs { get; set; } = [];
    public ReactiveCommand<Unit, Unit> LoadConfigCommand { get; }
    private List<ConfigModel>? _configModels;
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
        //读取配置文件
        var configStr = await File.ReadAllTextAsync(file);
        //读取定义文件
        var definitionStr = await File.ReadAllTextAsync(file.Replace(".json", ".definition.json"));
        //解析定义文件
        var definition = JsonSerializer.Deserialize<List<ConfigModel>>(definitionStr);
        _configModels = definition;
        // 配置信息转换为JsonObject
        var config = JsonSerializer.Deserialize<JsonObject>(configStr);

        //转换为ViewModel
        var main = definition?.FirstOrDefault(c => c.MainType);




        SetConfigViewModel(main, config, definition, Configs);
    }

    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }
    public async Task SaveConfig()
    {
        if (Path is null)
        {
            return;
        }
        var config = new JsonObject();

        SetConfig(config, Configs);
        await File.WriteAllTextAsync(Path, config.ToString());
    }

    static void SetConfig(JsonObject config, ObservableCollection<ConfigViewModel> configViewModel)
    {
        foreach (var property in configViewModel)
        {
            if (property.Type == ConfigModelType.Object)
            {
                var subConfig = new JsonObject();
                SetConfig(subConfig, property.Properties);
                config[property.Name] = subConfig;
            }
            else if (property.Type == ConfigModelType.Array)
            {
                var array = new JsonArray();
                foreach (var sub in property.Properties)
                {
                    var subConfig = new JsonObject();
                    SetConfig(subConfig, sub.Properties);
                    array.Add(subConfig);
                }
                config[property.Name] = array;
            }
            else
            {
                if (property.Required)
                {
                    if (property.Value is null)
                    {
                        throw new Exception($"{property.Name}是必填项");
                    }
                }
                if (property.AllowedValues is not null && !property.AllowedValues.Contains(property.Value ?? string.Empty))
                {
                    throw new Exception($"{property.Name}的值不在允许的范围内");
                }
                if (property.DeniedValues is not null && property.DeniedValues.Contains(property.Value ?? string.Empty))
                {
                    throw new Exception($"{property.Name}的值在禁止的范围内");
                }

                //验证数据是否合法
                if (property.Type == ConfigModelType.Number)
                {
                    decimal v = 0;
                    if (property.Value is not null && !decimal.TryParse(property.Value, out v))
                    {
                        throw new Exception($"{property.Name}的值不是数字");
                    }
                    if (property.Maximum.HasValue)
                    {
                        if (v > property.Maximum)
                        {
                            throw new Exception($"{property.Name}的值大于最大值");
                        }
                    }
                    if (property.Minimum.HasValue)
                    {
                        if (v < property.Minimum)
                        {
                            throw new Exception($"{property.Name}的值小于最小值");
                        }
                    }
                }
                else if (property.Type == ConfigModelType.Boolean)
                {
                    if (property.Value is not null && !bool.TryParse(property.Value, out _))
                    {
                        throw new Exception($"{property.Name}的值不是布尔值");
                    }
                }
                else if (property.Type == ConfigModelType.DateTime)
                {
                    if (property.Value is not null && !DateTime.TryParse(property.Value, out _))
                    {
                        throw new Exception($"{property.Name}的值不是日期时间");
                    }
                }
                else if (property.Type == ConfigModelType.DateOnly)
                {
                    if (property.Value is not null && !DateOnly.TryParse(property.Value, out _))
                    {
                        throw new Exception($"{property.Name}的值不是日期");
                    }
                }
                else if (property.Type == ConfigModelType.TimeOnly)
                {
                    if (property.Value is not null && !TimeOnly.TryParse(property.Value, out _))
                    {
                        throw new Exception($"{property.Name}的值不是时间");
                    }
                }
                else if (property.Type == ConfigModelType.String)
                {
                    if (property.Value is not null && property.RegularExpression is not null && !Regex.IsMatch(property.Value, property.RegularExpression))
                    {
                        throw new Exception($"{property.Name}的值不符合正则表达式");
                    }
                    if (property.Value?.Length > property.Maximum)
                    {
                        throw new Exception($"{property.Name}的值超长");
                    }
                    if (property.Value?.Length < property.Minimum)
                    {
                        throw new Exception($"{property.Name}的值过短");
                    }
                }

                //如果是布尔值，需要转换为布尔值
                if (property.Type == ConfigModelType.Boolean)
                {
                    config[property.Name] = bool.Parse(property.Value ?? "false");
                }
                //如果是数字，需要转换为数字
                else if (property.Type == ConfigModelType.Number)
                {
                    config[property.Name] = decimal.Parse(property.Value ?? "0");
                }
                else
                {
                    config[property.Name] = property.Value;
                }

            }
        }
    }

    static void SetConfigViewModel(ConfigModel? configModel, JsonObject? config, List<ConfigModel>? definition, ObservableCollection<ConfigViewModel> configViewModel)
    {
        if (configModel != null)
        {
            foreach (var propertyModel in configModel.PropertyModels)
            {
                var configViewModelProperty = new ConfigViewModel(propertyModel.Name)
                {
                    Type = propertyModel.Type,
                    SubTypeName = propertyModel.SubTypeName,
                    SubType = propertyModel.SubType,
                    DisplayName = propertyModel.DisplayName ?? propertyModel.Name,
                    GroupName = propertyModel.GroupName,
                    Description = propertyModel.Description,
                    Prompt = propertyModel.Prompt,
                    Minimum = propertyModel.Minimum,
                    Maximum = propertyModel.Maximum,
                    AllowedValues = propertyModel.AllowedValues is not null ? new ObservableCollection<string>(propertyModel.AllowedValues) : null,
                    DeniedValues = propertyModel.DeniedValues is not null ? new ObservableCollection<string>(propertyModel.DeniedValues) : null,
                    Options = new ObservableCollection<KeyValuePair<string, string>>(propertyModel.Options ?? []),
                    Required = propertyModel.Required,
                    RegularExpression = propertyModel.RegularExpression,
                };
                if (config is not null)
                {
                    if (propertyModel.Type == ConfigModelType.Object && config[propertyModel.Name] is not null)
                    {
                        var sub = definition?.FirstOrDefault(c => !c.MainType && c.TypeName == propertyModel.SubTypeName);

                        if (sub is not null)
                        {
                            SetConfigViewModel(sub, config[propertyModel.Name] as JsonObject, definition, configViewModelProperty.Properties);
                        }
                    }
                    //如果是数组
                    else if (propertyModel.Type == ConfigModelType.Array && config[propertyModel.Name] is JsonArray array)
                    {
                        foreach (var item in array)
                        {
                            var sub = definition?.FirstOrDefault(c => !c.MainType && c.TypeName == propertyModel.SubTypeName);
                            if (sub is not null)
                            {
                                var subConfigViewModel = new ConfigViewModel(propertyModel.SubTypeName ?? "");
                                subConfigViewModel.Type = propertyModel.SubType ?? ConfigModelType.Object;
                                if (propertyModel.SubType == ConfigModelType.Object)
                                {
                                    subConfigViewModel.Value = "1";
                                }
                                SetConfigViewModel(sub, item as JsonObject, definition, subConfigViewModel.Properties);
                                configViewModelProperty.Properties.Add(subConfigViewModel);
                            }
                        }
                    }
                    else if (config[propertyModel.Name] is not null)
                    {
                        configViewModelProperty.Value = config[propertyModel.Name]?.ToString();
                    }
                }
                configViewModel.Add(configViewModelProperty);
            }
        }
    }
    public ReactiveCommand<ConfigViewModel, Unit> AddPropertyCommand { get; }
    public void AddProperty(ConfigViewModel configViewModel)
    {
        var property = _configModels?.FirstOrDefault(c => c.TypeName == configViewModel.SubTypeName);
        if (property is null)
        {
            return;
        }
        if (configViewModel.SubType.HasValue)
        {
            ConfigViewModel configViewModel1 = new(string.Empty)
            {
                DisplayName = configViewModel.DisplayName + "[]",
                Type = configViewModel.SubType.Value
            };
            if (configViewModel.SubType == ConfigModelType.Object)
            {
                configViewModel1.Value = "1";
            }
            SetConfigViewModel(property, [], _configModels, configViewModel1.Properties);
            configViewModel.Properties.Add(configViewModel1);
        }

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
    public ConfigModelType? SubType { get; set; }
    [Reactive]
    public string? SubTypeName { get; set; }
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
    public ObservableCollection<KeyValuePair<string, string>>? Options { get; set; }
    [Reactive]
    public bool Required { get; set; }
    [Reactive]
    public string? RegularExpression { get; set; }
    [Reactive]
    public string? Value { get; set; }

    [Reactive]
    public ObservableCollection<ConfigViewModel> Properties { get; set; } = [];
}