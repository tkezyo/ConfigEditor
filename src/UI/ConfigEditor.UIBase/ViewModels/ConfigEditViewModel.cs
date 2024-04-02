using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Globalization;
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
        SaveConfigCommand = ReactiveCommand.CreateFromTask(SaveConfig, this.ValidationContext.Valid);
        AddPropertyCommand = ReactiveCommand.Create<ConfigViewModel>(AddProperty);
        SetArrayCommand = ReactiveCommand.Create<ConfigViewModel>(SetArray);
        SetObjectCommand = ReactiveCommand.Create<ConfigViewModel>(SetObject);
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

        if (main is not null)
        {
            foreach (var item in main.PropertyModels)
            {
                SetConfigViewModel(item, config, definition, Configs);
            }
        }

        //订阅所有属性的ValidationContext，当有属性的ValidationContext发生变化时，重新计算HasErrors
        this.Configs.ToObservableChangeSet().ActOnEveryObject(c =>
        {
            this.ValidationContext.Add(c.ValidationContext);
        }, c =>
        {
            this.ValidationContext.Remove(c.ValidationContext);
        });
    }

    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }
    public async Task SaveConfig()
    {
        if (Path is null)
        {
            return;
        }
        var config = new JsonObject();

        foreach (var item in Configs)
        {
            config[item.Name] = SetJsonNode(item);
        }

        await File.WriteAllTextAsync(Path, config.ToString());
        await _messageBoxManager.Alert.Handle(new AlertInfo("保存成功"));
    }


    static JsonNode SetJsonNode(ConfigViewModel config)
    {
        if (config.Required)
        {
            if (config.Value is null)
            {
                throw new Exception($"{config.Name}是必填项");
            }
        }
        if (config.AllowedValues is not null && !config.AllowedValues.Contains(config.Value ?? string.Empty))
        {
            throw new Exception($"{config.Name}的值不在允许的范围内");
        }
        if (config.DeniedValues is not null && config.DeniedValues.Contains(config.Value ?? string.Empty))
        {
            throw new Exception($"{config.Name}的值在禁止的范围内");
        }

        //验证数据是否合法
        if (config.Type == ConfigModelType.Number)
        {
            decimal v = 0;
            if (config.Value is not null && !decimal.TryParse(config.Value, out v))
            {
                throw new Exception($"{config.Name}的值不是数字");
            }
            if (config.Maximum.HasValue)
            {
                if (v > config.Maximum)
                {
                    throw new Exception($"{config.Name}的值大于最大值");
                }
            }
            if (config.Minimum.HasValue)
            {
                if (v < config.Minimum)
                {
                    throw new Exception($"{config.Name}的值小于最小值");
                }
            }
        }
        else if (config.Type == ConfigModelType.Boolean)
        {
            if (config.Value is not null && !bool.TryParse(config.Value, out _))
            {
                throw new Exception($"{config.Name}的值不是布尔值");
            }
        }
        else if (config.Type == ConfigModelType.DateTime)
        {
            if (config.Value is not null && !DateTime.TryParse(config.Value, out _))
            {
                throw new Exception($"{config.Name}的值不是日期时间");
            }
        }
        else if (config.Type == ConfigModelType.DateOnly )
        {
            if (config.Value is not null && !DateOnly.TryParse(config.Value.Split(' ')[0], out _))
            {
                throw new Exception($"{config.Name}的值不是日期");
            }
        }
        else if (config.Type == ConfigModelType.TimeOnly)
        {
            if (config.Value is not null && !TimeOnly.TryParse(config.Value.Split(' ')[1], out _))
            {
                throw new Exception($"{config.Name}的值不是时间");
            }
        }
        else if (config.Type == ConfigModelType.String)
        {
            if (config.Value is not null && config.RegularExpression is not null && !Regex.IsMatch(config.Value, config.RegularExpression))
            {
                throw new Exception($"{config.Name}的值不符合正则表达式");
            }
            if (config.Value?.Length > config.Maximum)
            {
                throw new Exception($"{config.Name}的值超长");
            }
            if (config.Value?.Length < config.Minimum)
            {
                throw new Exception($"{config.Name}的值过短");
            }
        }

        if (config.Type == ConfigModelType.Object)
        {
            JsonObject jsonObj = new JsonObject();
            foreach (var item in config.Properties)
            {
                jsonObj[item.Name] = SetJsonNode(item);
            }
            return jsonObj;
        }
        else if (config.Type == ConfigModelType.Array)
        {
            var array = new JsonArray();
            //如果是数组，需要确认维度
            if (config.Dim > 0)
            {
                for (var i = 0; i < config.Properties.Count; i++)
                {
                    var property = config.Properties[i];
                    //计算当前元素的维度索引
                    int[] index = new int[config.DimLength.Count];
                    int temp = i;
                    for (int j = config.DimLength.Count - 1; j >= 0; j--)
                    {
                        index[j] = temp % config.DimLength[j].Length;
                        temp /= config.DimLength[j].Length;
                    }

                    //如果index的值为0,0，那么从array中取出对应的数组，如果没有则创建一个新的数组
                    JsonArray GetArray(JsonArray array, int[] index)
                    {
                        JsonArray? array1 = array;
                        for (int i = 0; i < index.Length; i++)
                        {
                            if (array1.Count <= index[i] && i != (index.Length - 1))
                            {
                                array1.Add(new JsonArray());
                            }
                            if (i != (index.Length - 1))
                            {
                                array1 = array1[index[i]] as JsonArray;
                            }
                        }
                        return array1;
                    }

                    var array1 = GetArray(array, index);

                    array1.Add(SetJsonNode(property));
                }
            }
            return array;
        }
        //如果是布尔值，需要转换为布尔值
        else if (config.Type == ConfigModelType.Boolean)
        {
            return (JsonNode)bool.Parse(config.Value ?? "false");
        }
        //如果是数字，需要转换为数字
        else if (config.Type == ConfigModelType.Number)
        {
            return (JsonNode)decimal.Parse(config.Value ?? "0");
        }
        else if (config.Type == ConfigModelType.DateTime)
        {
            //config.value的格式为 4/2/2024 10:19:02 PM 需要转换为 json的格式
            return (JsonNode)DateTime.Parse(config.Value ?? string.Empty).ToString("yyyy-MM-ddTHH:mm:ss");
        }
        else if (config.Type == ConfigModelType.DateOnly&& !string.IsNullOrEmpty(config.Value))
        {
            //config.value的格式为 4/2/2024 10:19:02 PM 需要转换为 json的格式
            return (JsonNode)DateTime.Parse(config.Value.Split(' ')[0] ?? string.Empty).ToString("yyyy-MM-dd");
        }
        else if (config.Type == ConfigModelType.TimeOnly && !string.IsNullOrEmpty(config.Value))
        {
            //config.value的格式为 4/2/2024 10:19:02 PM 需要转换为 json的格式
            return (JsonNode)DateTime.Parse(config.Value.Split(' ')[1] ?? string.Empty).ToString("HH:mm:ss");
        }
        else
        {
            return (JsonNode)(config.Value ?? string.Empty);
        }
    }

    static void SetConfigViewModel(PropertyModel propertyModel, JsonObject? config, List<ConfigModel>? definition, ObservableCollection<ConfigViewModel> configViewModel)
    {
        string GetDisplayName()
        {
            var name = propertyModel.DisplayName ?? propertyModel.Name;
            //如果是数组，打印数组长度，并打印出每个维度的长度
            if (propertyModel.Type == ConfigModelType.Array)
            {
                name += $"[{propertyModel.Dim}]";
            }

            return name;
        }
        var configViewModelProperty = new ConfigViewModel(propertyModel.Name)
        {
            Type = propertyModel.Type,
            SubTypeName = propertyModel.SubTypeName,
            SubType = propertyModel.SubType,
            DisplayName = GetDisplayName(),
            GroupName = propertyModel.GroupName,
            Description = propertyModel.Description,
            Prompt = propertyModel.Prompt,
            Minimum = propertyModel.Minimum,
            Maximum = propertyModel.Maximum,
            AllowedValues = propertyModel.AllowedValues is not null ? new ObservableCollection<string>(propertyModel.AllowedValues) : null,
            DeniedValues = propertyModel.DeniedValues is not null ? new ObservableCollection<string>(propertyModel.DeniedValues) : null,
            Options = new ObservableCollection<KeyValuePair<string, string>>(propertyModel.Options ?? []),
            Required = propertyModel.Required ?? false,
            RegularExpression = propertyModel.RegularExpression,
            Dim = propertyModel.Dim,
            Order = propertyModel.Order,
        };
        if (propertyModel.Type == ConfigModelType.Array)
        {
            //如果是数组，需要确认维度
            if (propertyModel.Dim > 0)
            {
                if (propertyModel.DimLength is not null)
                {
                    //设置维度, propertyModel.DimLength如果不满足维度，需要补充
                    for (int i = 0; i < propertyModel.Dim; i++)
                    {
                        if (propertyModel.DimLength.Length > i)
                        {

                            configViewModelProperty.DimLength.Add(new DimViewModel(propertyModel.DimLength[i], "Dim" + (i + 1)));
                        }
                        else
                        {
                            configViewModelProperty.DimLength.Add(new DimViewModel(0, "Dim" + (i + 1)));
                        }
                    }

                    //删除多余的维度
                    if (propertyModel.DimLength.Length > propertyModel.Dim)
                    {
                        for (int i = propertyModel.Dim.Value; i < propertyModel.DimLength.Length; i++)
                        {
                            configViewModelProperty.DimLength.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < propertyModel.Dim; i++)
                    {
                        configViewModelProperty.DimLength.Add(new DimViewModel(0, "Dim" + (i + 1)));
                    }
                }
            }
        }

        if (config is not null)
        {
            if (propertyModel.Type == ConfigModelType.Object && config[propertyModel.Name] is JsonObject jsonObj)
            {
                var sub = definition?.FirstOrDefault(c => !c.MainType && c.TypeName == propertyModel.SubTypeName);

                if (sub is not null)
                {
                    foreach (var item in sub.PropertyModels)
                    {
                        SetConfigViewModel(item, jsonObj, definition, configViewModelProperty.Properties);
                    }
                }
            }
            //如果是数组
            else if (propertyModel.Type == ConfigModelType.Array && config[propertyModel.Name] is JsonArray array)
            {
                foreach (var item in array)
                {
                    var subConfigViewModel = new ConfigViewModel(propertyModel.SubTypeName ?? "")
                    {
                        Type = propertyModel.SubType ?? ConfigModelType.Object
                    };
                    if (subConfigViewModel.Type == ConfigModelType.Object)
                    {
                        var sub = definition?.FirstOrDefault(c => !c.MainType && c.TypeName == propertyModel.SubTypeName);
                        if (sub is not null)
                        {
                            if (propertyModel.SubType == ConfigModelType.Object)
                            {
                                subConfigViewModel.Value = "create";
                            }
                            foreach (var item1 in sub.PropertyModels)
                            {
                                SetConfigViewModel(item1, item as JsonObject, definition, subConfigViewModel.Properties);
                            }
                        }
                    }



                    configViewModelProperty.Properties.Add(subConfigViewModel);
                }
            }
            else if (config[propertyModel.Name] is not null)
            {
                configViewModelProperty.Value = config[propertyModel.Name]?.ToString();
            }
        }
        configViewModel.Add(configViewModelProperty);
    }

    public ReactiveCommand<ConfigViewModel, Unit> SetArrayCommand { get; }
    public void SetArray(ConfigViewModel configViewModel)
    {
        //如果是数组，需要确认维度
        if (configViewModel.Type != ConfigModelType.Array || !configViewModel.SubType.HasValue)
        {
            return;
        }

        int arrayCount = configViewModel.DimLength.Select(c => c.Length).Aggregate((a, b) => a * b);

        if (configViewModel.Properties.Count > arrayCount)
        {
            //删除多余的属性
            for (int i = arrayCount; i < configViewModel.Properties.Count; i++)
            {
                configViewModel.Properties.RemoveAt(i);
            }
        }
        else if (configViewModel.Properties.Count < arrayCount)
        {
            //添加缺少的属性
            for (int i = configViewModel.Properties.Count; i < arrayCount; i++)
            {
                ConfigViewModel configViewModel1 = new(configViewModel.Name)
                {
                    Type = configViewModel.SubType.Value,
                    AllowedValues = configViewModel.AllowedValues,
                    DeniedValues = configViewModel.DeniedValues,
                    Options = configViewModel.Options,
                    Required = configViewModel.Required,
                    RegularExpression = configViewModel.RegularExpression,
                    Order = configViewModel.Order,
                    GroupName = configViewModel.GroupName,
                    Description = configViewModel.Description,
                    Prompt = configViewModel.Prompt,
                    Minimum = configViewModel.Minimum,
                    Maximum = configViewModel.Maximum,
                    DisplayName = configViewModel.DisplayName,
                };
                if (configViewModel.SubType == ConfigModelType.Object)
                {
                    var property = _configModels?.FirstOrDefault(c => c.TypeName == configViewModel.SubTypeName);
                    if (property is null)
                    {
                        return;
                    }
                    configViewModel1.Value = "create";
                    foreach (var item in property.PropertyModels)
                    {
                        SetConfigViewModel(item, [], _configModels, configViewModel1.Properties);
                    }
                }
                configViewModel.Properties.Add(configViewModel1);
            }
        }

        for (int i = 0; i < arrayCount; i++)
        {
            int index = i;  // 一维索引

            int[] indices = new int[configViewModel.DimLength.Count];

            for (int j = configViewModel.DimLength.Count - 1; j >= 0; j--)
            {
                indices[j] = (index % configViewModel.DimLength[j].Length) + 1;
                index /= configViewModel.DimLength[j].Length;
            }
            configViewModel.Properties[i].DisplayName = configViewModel.Properties[i].Name + "(" + string.Join(",", indices) + ")";
        }
    }

    public ReactiveCommand<ConfigViewModel, Unit> SetObjectCommand { get; }
    public void SetObject(ConfigViewModel configViewModel)
    {
        var sub = _configModels?.FirstOrDefault(c => !c.MainType && c.TypeName == configViewModel.SubTypeName);

        configViewModel.Value = "create";

        if (sub is not null)
        {
            foreach (var item in sub.PropertyModels)
            {
                SetConfigViewModel(item, [], _configModels, configViewModel.Properties);
            }
        }
    }

    public ReactiveCommand<ConfigViewModel, Unit> AddPropertyCommand { get; }
    public void AddProperty(ConfigViewModel configViewModel)
    {
        if (configViewModel.SubType.HasValue)
        {
            ConfigViewModel configViewModel1 = new(string.Empty)
            {
                Type = configViewModel.SubType.Value
            };
            if (configViewModel.SubType == ConfigModelType.Object)
            {
                var property = _configModels?.FirstOrDefault(c => c.TypeName == configViewModel.SubTypeName);
                if (property is null)
                {
                    return;
                }
                configViewModel1.Value = "create";
                foreach (var item in property.PropertyModels)
                {
                    SetConfigViewModel(item, [], _configModels, configViewModel1.Properties);
                }
            }
            configViewModel.Properties.Add(configViewModel1);
        }

    }
}

public class ConfigViewModel : ReactiveValidationObject
{
    public ConfigViewModel(string name)
    {
        this.Name = name;
        this.ValidationRule(
           viewModel => viewModel.Value,
           name =>
           {
               if (Required && string.IsNullOrWhiteSpace(name))
               {
                   return false;
               }
               return true;
           }, "必填");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (AllowedValues is not null && !AllowedValues.Contains(name ?? string.Empty))
                {
                    return false;
                }
                return true;
            }, "不在允许的范围内");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (DeniedValues is not null && DeniedValues.Contains(name ?? string.Empty))
                {
                    return false;
                }
                return true;
            }, "在禁止的范围内");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (Type == ConfigModelType.Number)
                {
                    decimal v = 0;
                    if (name is not null && !decimal.TryParse(name, out v))
                    {
                        return false;
                    }
                    if (Maximum.HasValue)
                    {
                        if (v > Maximum)
                        {
                            return false;
                        }
                    }
                    if (Minimum.HasValue)
                    {
                        if (v < Minimum)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }, "不是数字");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (Type == ConfigModelType.Boolean)
                {
                    if (name is not null && !bool.TryParse(name, out _))
                    {
                        return false;
                    }
                }
                return true;
            }, "不是布尔值");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (Type == ConfigModelType.DateTime)
                {
                    if (name is not null && !DateTime.TryParse(name, out _))
                    {
                        return false;
                    }
                }
                return true;
            }, "不是日期时间");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (Type == ConfigModelType.DateOnly)
                {
                    if (name is not null && !DateTime.TryParse(name, out _))
                    {
                        return false;
                    }
                }
                return true;
            }, "不是日期");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (Type == ConfigModelType.TimeOnly)
                {
                    if (name is not null && !DateTime.TryParse(name, out _))
                    {
                        return false;
                    }
                }
                return true;
            }, "不是时间");

        this.ValidationRule(
            viewModel => viewModel.Value,
            name =>
            {
                if (Type == ConfigModelType.String)
                {
                    if (name is not null && RegularExpression is not null && !Regex.IsMatch(name, RegularExpression))
                    {
                        return false;
                    }
                    if (name?.Length > Maximum)
                    {
                        return false;
                    }
                    if (name?.Length < Minimum)
                    {
                        return false;
                    }
                }
                return true;
            }, "不符合正则表达式");

        this.Properties.ToObservableChangeSet().ActOnEveryObject(c =>
        {
            this.ValidationContext.Add(c.ValidationContext);
        }, c =>
        {
            this.ValidationContext.Remove(c.ValidationContext);
        });
    }
    [Reactive]
    public string Name { get; set; }
    [Reactive]
    public ConfigModelType Type { get; set; }
    [Reactive]
    public int? Order { get; set; }
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
    public decimal? Minimum { get; set; }
    [Reactive]
    public decimal? Maximum { get; set; }
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
    public int? Dim { get; set; }
    [Reactive]
    public ObservableCollection<DimViewModel> DimLength { get; set; } = [];

    [Reactive]
    public ObservableCollection<ConfigViewModel> Properties { get; set; } = [];
}

public class DimViewModel : ReactiveObject
{
    public DimViewModel(int length, string displayName)
    {
        if (length < 1)
        {
            length = 1;
            ReadOnly = false;
        }
        else
        {
            ReadOnly = true;
        }
        Length = length;
        DisplayName = displayName;
        if (!ReadOnly)
        {
            DisplayName += "*";
        }
    }
    [Reactive]
    public int Length { get; set; }

    [Reactive]
    public bool ReadOnly { get; set; }

    [Reactive]
    public string DisplayName { get; set; }
}