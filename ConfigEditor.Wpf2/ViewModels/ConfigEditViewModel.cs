using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ConfigEditor;

namespace ConfigEditor.Wpf2.ViewModels
{
    internal class ConfigEditViewModel : ObservableObject
    {
        private List<ConfigModel>? _configModels;

        public ConfigEditViewModel()
        {
            Configs = new ObservableCollection<ConfigViewModel>();
            ConfigInfo = new ObservableCollection<ConfigInfoViewModel>();

            LoadConfigCommand = new AsyncRelayCommand(LoadConfig);
            SaveConfigCommand = new AsyncRelayCommand(SaveConfig);
            AddPropertyCommand = new RelayCommand<ConfigViewModel>(AddProperty);
            AddArrayCommand = new RelayCommand<ConfigViewModel>(AddArray);
            SetObjectCommand = new RelayCommand<ConfigViewModel>(SetObject);
            CopyCommand = new RelayCommand<ConfigViewModel>(Copy);
            PasteCommand = new RelayCommand<ConfigViewModel>(Paste);
        }

        public ObservableCollection<ConfigViewModel> Configs { get; }
        public ObservableCollection<ConfigInfoViewModel> ConfigInfo { get; }

        public IAsyncRelayCommand LoadConfigCommand { get; }
        public IAsyncRelayCommand SaveConfigCommand { get; }

        public RelayCommand<ConfigViewModel> AddPropertyCommand { get; }
        public RelayCommand<ConfigViewModel> AddArrayCommand { get; }
        public RelayCommand<ConfigViewModel> SetObjectCommand { get; }
        public RelayCommand<ConfigViewModel> CopyCommand { get; }
        public RelayCommand<ConfigViewModel> PasteCommand { get; }

        private bool _cli;
        public bool Cli
        {
            get => _cli;
            set => SetProperty(ref _cli, value);
        }

        public async Task LoadConfig()
        {
            // Clear existing data to force full re-render
            ConfigInfo.Clear();
            Configs.Clear();

            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "*.json|*.json",
                Multiselect = true,
                Title = "打开配置"
            };
            var res = dlg.ShowDialog();
            string[] files;
            if (res == true)
            {
                files = dlg.FileNames;
            }
            else
            {
                return;
            }

            foreach (var item in files)
            {
                var file = item;
                if (file.Contains("definition"))
                {
                    file = file.Replace("definition.", "");
                }
                if (!File.Exists(file))
                {
                    continue;
                }

                if (ConfigInfo != null && System.Linq.Enumerable.Any(ConfigInfo, c => c.FilePath == file))
                {
                    continue;
                }

                var definitionFile = file.Replace(".json", ".definition.json");

                if (!File.Exists(definitionFile))
                {
                    MessageBox.Show("definition文件不存在");
                    return;
                }

                var configStr = await File.ReadAllTextAsync(file);
                var definitionStr = await File.ReadAllTextAsync(definitionFile);
                var definition = JsonSerializer.Deserialize<List<ConfigModel>>(definitionStr);
                _configModels = definition;
                var config = JsonSerializer.Deserialize<JsonObject>(configStr);

                var main = definition?.Find(c => c.MainType);

                var configInfoViewModel = new ConfigInfoViewModel
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    FilePath = file
                };

                if (main is not null)
                {
                    foreach (var property in main.PropertyModels)
                    {
                        SetConfigViewModel(property, config, definition, configInfoViewModel.Configs);
                    }
                }

                ConfigInfo.Add(configInfoViewModel);
            }
        }

        public async Task SaveConfig()
        {
            foreach (var item in ConfigInfo)
            {
                var config = new JsonObject();
                foreach (var item2 in item.Configs)
                {
                    config[item2.Name] = SetJsonNode(item2);
                }
                await File.WriteAllTextAsync(item.FilePath, config.ToString());
            }
            MessageBox.Show("保存成功");
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

            if (config.Type == ConfigModelType.Number)
            {
                double v = 0;
                if (config.Value is not null && !double.TryParse(config.Value, out v))
                {
                    throw new Exception($"{config.Name}的值不是数字");
                }
                if (!double.IsNaN(config.Maximum))
                {
                    if (v > config.Maximum)
                    {
                        throw new Exception($"{config.Name}的值大于最大值");
                    }
                }
                if (!double.IsNaN(config.Minimum))
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
            else if (config.Type == ConfigModelType.DateOnly)
            {
                if (config.Value is not null && !DateOnly.TryParse(config.Value.Split(' ')[0], out _))
                {
                    throw new Exception($"{config.Name}的值不是日期");
                }
            }
            else if (config.Type == ConfigModelType.TimeOnly)
            {
                if (config.Value is null)
                {
                    return (JsonNode)string.Empty;
                }
                var timeStr = config.Value.Contains(' ') ? config.Value.Split(' ')[1] : config.Value;
                if (!TimeOnly.TryParse(timeStr, out var time))
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
                if (string.IsNullOrEmpty(config.Value))
                {
                    return JsonNode.Parse("null")!;
                }
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
                if (config.Dim > 0)
                {
                    for (var i = 0; i < config.Properties.Count; i++)
                    {
                        var item = config.Properties[i];

                        array.Add(SetJsonNode(item));
                    }
                }
                return array;
            }
            else if (config.Type == ConfigModelType.Boolean)
            {
                return (JsonNode)bool.Parse(config.Value ?? "false");
            }
            else if (config.Type == ConfigModelType.Number)
            {
                return (JsonNode)decimal.Parse(config.Value ?? "0");
            }
            else if (config.Type == ConfigModelType.DateTime)
            {
                return (JsonNode)DateTime.Parse(config.Value ?? string.Empty).ToString("yyyy-MM-ddTHH:mm:ss");
            }
            else if (config.Type == ConfigModelType.DateOnly && !string.IsNullOrEmpty(config.Value))
            {
                return (JsonNode)DateTime.Parse(config.Value.Split(' ')[0] ?? string.Empty).ToString("yyyy-MM-dd");
            }
            else if (config.Type == ConfigModelType.TimeOnly && !string.IsNullOrEmpty(config.Value))
            {
                return (JsonNode)DateTime.Parse(config.Value).ToString("HH:mm:ss");
            }
            else
            {
                return (JsonNode)(config.Value ?? string.Empty);
            }
        }

        static void SetConfigViewModel(PropertyModel propertyModel, JsonObject? config, List<ConfigModel>? definition, ObservableCollection<ConfigViewModel> configViewModel)
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
                Minimum = propertyModel.Minimum ?? double.NaN,
                Maximum = propertyModel.Maximum ?? double.NaN,
                AllowedValues = propertyModel.AllowedValues is not null ? new ObservableCollection<string>(propertyModel.AllowedValues) : null,
                DeniedValues = propertyModel.DeniedValues is not null ? new ObservableCollection<string>(propertyModel.DeniedValues) : null,
                Options = new ObservableCollection<KeyValuePair<string, string>>(propertyModel.Options ?? new List<KeyValuePair<string, string>>()),
                Required = propertyModel.Required ?? false,
                RegularExpression = propertyModel.RegularExpression,
                Dim = propertyModel.Dim,
                Order = propertyModel.Order,
                AllowedValuesErrorMessage = propertyModel.AllowedValuesErrorMessage,
                DeniedValuesErrorMessage = propertyModel.DeniedValuesErrorMessage,
                RegularExpressionErrorMessage = propertyModel.RegularExpressionErrorMessage,
                RequiredErrorMessage = propertyModel.RequiredErrorMessage,
                RangeErrorMessage = propertyModel.RangeErrorMessage,
                LengthErrorMessage = propertyModel.LengthErrorMessage,
            };

            configViewModelProperty.SetValidationRule();

            if (config is not null)
            {
                if (propertyModel.Type == ConfigModelType.Object && config[propertyModel.Name] is JsonObject jsonObj)
                {
                    var sub = definition?.Find(c => !c.MainType && c.TypeName == propertyModel.SubTypeName);

                    if (sub is not null)
                    {
                        configViewModelProperty.Value = "create";
                        foreach (var item in sub.PropertyModels)
                        {
                            SetConfigViewModel(item, jsonObj, definition, configViewModelProperty.Properties);
                        }
                    }
                }
                else if (propertyModel.Type == ConfigModelType.Array && config[propertyModel.Name] is JsonArray array)
                {
                    ConfigViewModel? Create(JsonNode jsonNode, int dim)
                    {
                        if (dim >= 1)
                        {
                            if (jsonNode is JsonArray jsonArray)
                            {
                                var subConfigViewModel = new ConfigViewModel(propertyModel.SubTypeName ?? "")
                                {
                                    Type = propertyModel.Type,
                                    SubType = propertyModel.SubType,
                                    SubTypeName = propertyModel.SubTypeName,
                                    Dim = dim,
                                    DisplayName = propertyModel.DisplayName ?? propertyModel.Name,
                                    GroupName = propertyModel.GroupName,
                                    Description = propertyModel.Description,
                                    Prompt = propertyModel.Prompt,
                                    Minimum = propertyModel.Minimum ?? double.NaN,
                                    Maximum = propertyModel.Maximum ?? double.NaN,
                                    AllowedValues = propertyModel.AllowedValues is not null ? new ObservableCollection<string>(propertyModel.AllowedValues) : null,
                                    DeniedValues = propertyModel.DeniedValues is not null ? new ObservableCollection<string>(propertyModel.DeniedValues) : null,
                                    Options = new ObservableCollection<KeyValuePair<string, string>>(propertyModel.Options ?? new List<KeyValuePair<string, string>>()),
                                    Required = propertyModel.Required ?? false,
                                    RegularExpression = propertyModel.RegularExpression,
                                    AllowedValuesErrorMessage = propertyModel.AllowedValuesErrorMessage,
                                    DeniedValuesErrorMessage = propertyModel.DeniedValuesErrorMessage,
                                    RegularExpressionErrorMessage = propertyModel.RegularExpressionErrorMessage,
                                    RequiredErrorMessage = propertyModel.RequiredErrorMessage,
                                    RangeErrorMessage = propertyModel.RangeErrorMessage,
                                    LengthErrorMessage = propertyModel.LengthErrorMessage,
                                };

                                foreach (var item in jsonArray)
                                {
                                    if (item is null)
                                    {
                                        continue;
                                    }
                                    var result = Create(item, dim - 1);
                                    if (result is not null)
                                    {
                                        subConfigViewModel.Properties.Add(result);
                                    }
                                }
                                return subConfigViewModel;
                            }
                        }
                        else
                        {
                            var subConfigViewModel = new ConfigViewModel(propertyModel.SubTypeName ?? "")
                            {
                                Type = propertyModel.SubType ?? ConfigModelType.Object,
                                DisplayName = propertyModel.DisplayName ?? propertyModel.Name,
                                GroupName = propertyModel.GroupName,
                                Description = propertyModel.Description,
                                Prompt = propertyModel.Prompt,
                                Minimum = propertyModel.Minimum ?? double.NaN,
                                Maximum = propertyModel.Maximum ?? double.NaN,
                                AllowedValues = propertyModel.AllowedValues is not null ? new ObservableCollection<string>(propertyModel.AllowedValues) : null,
                                DeniedValues = propertyModel.DeniedValues is not null ? new ObservableCollection<string>(propertyModel.DeniedValues) : null,
                                Options = new ObservableCollection<KeyValuePair<string, string>>(propertyModel.Options ?? new List<KeyValuePair<string, string>>()),
                                Required = propertyModel.Required ?? false,
                                RegularExpression = propertyModel.RegularExpression,
                                AllowedValuesErrorMessage = propertyModel.AllowedValuesErrorMessage,
                                DeniedValuesErrorMessage = propertyModel.DeniedValuesErrorMessage,
                                RegularExpressionErrorMessage = propertyModel.RegularExpressionErrorMessage,
                                RequiredErrorMessage = propertyModel.RequiredErrorMessage,
                                RangeErrorMessage = propertyModel.RangeErrorMessage,
                                LengthErrorMessage = propertyModel.LengthErrorMessage,

                            };

                            if (propertyModel.SubType == ConfigModelType.Object)
                            {
                                var sub = definition?.Find(c => !c.MainType && c.TypeName == propertyModel.SubTypeName);
                                if (sub is not null)
                                {
                                    subConfigViewModel.Value = "create";
                                    foreach (var item1 in sub.PropertyModels)
                                    {
                                        SetConfigViewModel(item1, jsonNode as JsonObject, definition, subConfigViewModel.Properties);
                                    }
                                }
                            }
                            else
                            {
                                subConfigViewModel.Value = jsonNode.ToJsonString();
                            }

                            return subConfigViewModel;
                        }
                        return null;
                    }

                    foreach (var item in array)
                    {
                        if (item is null)
                        {
                            continue;
                        }
                        var result = Create(item, (propertyModel.Dim ?? 0) - 1);
                        if (result is not null)
                        {
                            configViewModelProperty.Properties.Add(result);
                        }
                    }
                }
                else if (config[propertyModel.Name] is not null)
                {
                    if (propertyModel.Type == ConfigModelType.String)
                    {
                        if (config[propertyModel.Name]?.ToString() != "\u0000")
                        {
                            configViewModelProperty.Value = config[propertyModel.Name]?.ToString();
                        }
                        else
                        {
                            configViewModelProperty.Value = "0";
                        }
                    }
                    else
                    {
                        configViewModelProperty.Value = config[propertyModel.Name]?.ToString();
                    }
                }
            }
            configViewModel.Add(configViewModelProperty);
        }

        public void AddArray(ConfigViewModel configViewModel)
        {
            if (configViewModel.Type != ConfigModelType.Array || !configViewModel.SubType.HasValue)
            {
                return;
            }

            var subDim = configViewModel.Dim - 1;

            if (subDim >= 1)
            {
                ConfigViewModel configViewModel1 = new(configViewModel.Name)
                {
                    Type = configViewModel.Type,
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
                    DisplayName = configViewModel.DisplayName ?? configViewModel.Name,
                    Dim = subDim,
                    SubType = configViewModel.SubType,
                    SubTypeName = configViewModel.SubTypeName,
                    AllowedValuesErrorMessage = configViewModel.AllowedValuesErrorMessage,
                    DeniedValuesErrorMessage = configViewModel.DeniedValuesErrorMessage,
                    RegularExpressionErrorMessage = configViewModel.RegularExpressionErrorMessage,
                    RequiredErrorMessage = configViewModel.RequiredErrorMessage,
                    RangeErrorMessage = configViewModel.RangeErrorMessage,
                    LengthErrorMessage = configViewModel.LengthErrorMessage,

                };

                configViewModel1.SetValidationRule();

                configViewModel.Properties.Add(configViewModel1);
            }
            else
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
                    AllowedValuesErrorMessage = configViewModel.AllowedValuesErrorMessage,
                    DeniedValuesErrorMessage = configViewModel.DeniedValuesErrorMessage,
                    RegularExpressionErrorMessage = configViewModel.RegularExpressionErrorMessage,
                    RequiredErrorMessage = configViewModel.RequiredErrorMessage,
                    RangeErrorMessage = configViewModel.RangeErrorMessage,
                    LengthErrorMessage = configViewModel.LengthErrorMessage,
                };
                configViewModel1.SetValidationRule();

                if (configViewModel.SubType == ConfigModelType.Object)
                {
                    var property = _configModels?.Find(c => c.TypeName == configViewModel.SubTypeName);
                    if (property is null)
                    {
                        return;
                    }
                    configViewModel1.Value = "create";
                    foreach (var item in property.PropertyModels)
                    {
                        SetConfigViewModel(item, new JsonObject(), _configModels, configViewModel1.Properties);
                    }
                }
                configViewModel.Properties.Add(configViewModel1);
            }
        }

        public void SetObject(ConfigViewModel configViewModel)
        {
            var sub = _configModels?.Find(c => !c.MainType && c.TypeName == configViewModel.SubTypeName);
            if (configViewModel is null) return;
            if (configViewModel.Value == "create" || (configViewModel.Properties != null && configViewModel.Properties.Count > 0))
            {
                return;
            }
            configViewModel.Value = "create";

            if (sub is not null)
            {
                foreach (var item in sub.PropertyModels)
                {
                    SetConfigViewModel(item, new JsonObject(), _configModels, configViewModel.Properties);
                }
            }
        }

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
                    var property = _configModels?.Find(c => c.TypeName == configViewModel.SubTypeName);
                    if (property is null)
                    {
                        return;
                    }
                    configViewModel1.Value = "create";
                    foreach (var item in property.PropertyModels)
                    {
                        SetConfigViewModel(item, new JsonObject(), _configModels, configViewModel1.Properties);
                    }
                }
                configViewModel.Properties.Add(configViewModel1);
            }

        }

        public void Copy(ConfigViewModel configViewModel)
        {
            var json = SetJsonNode(configViewModel).ToJsonString();
            Clipboard.SetText(configViewModel.ToString() + "|" + json);
        }

        public void Paste(ConfigViewModel configViewModel)
        {
            var json = Clipboard.GetText();
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var index = json.IndexOf('|');
            if (index == -1)
            {
                return;
            }

            if (!json.StartsWith(configViewModel.ToString() + "|"))
            {
                return;
            }

            json = json[(index + 1)..];

            if (configViewModel.Type == ConfigModelType.Object)
            {
                var jsonObj = JsonSerializer.Deserialize<JsonObject>(json);

                var property = _configModels?.Find(c => c.TypeName == configViewModel.SubTypeName);

                if (property is null)
                {
                    return;
                }

                configViewModel.Properties.Clear();
                foreach (var item in property.PropertyModels)
                {
                    SetConfigViewModel(item, jsonObj, _configModels, configViewModel.Properties);
                }
            }
            else if (configViewModel.Type == ConfigModelType.Array)
            {
                var array = JsonSerializer.Deserialize<JsonArray>(json);
                ConfigViewModel? Create(JsonNode jsonNode, int dim)
                {
                    if (dim >= 1)
                    {
                        if (jsonNode is JsonArray jsonArray)
                        {
                            var subConfigViewModel = new ConfigViewModel(configViewModel.SubTypeName ?? "")
                            {
                                Type = configViewModel.Type,
                                SubType = configViewModel.SubType,
                                SubTypeName = configViewModel.SubTypeName,
                                Dim = dim,
                                DisplayName = configViewModel.DisplayName ?? configViewModel.Name,
                                GroupName = configViewModel.GroupName,
                                Description = configViewModel.Description,
                                Prompt = configViewModel.Prompt,
                                Minimum = configViewModel.Minimum,
                                Maximum = configViewModel.Maximum,
                                AllowedValues = configViewModel.AllowedValues is not null ? new ObservableCollection<string>(configViewModel.AllowedValues) : null,
                                DeniedValues = configViewModel.DeniedValues is not null ? new ObservableCollection<string>(configViewModel.DeniedValues) : null,
                                Options = configViewModel.Options is not null ? new ObservableCollection<KeyValuePair<string, string>>(configViewModel.Options) : null,
                                Required = configViewModel.Required,
                                RegularExpression = configViewModel.RegularExpression,
                                AllowedValuesErrorMessage = configViewModel.AllowedValuesErrorMessage,
                                DeniedValuesErrorMessage = configViewModel.DeniedValuesErrorMessage,
                                RegularExpressionErrorMessage = configViewModel.RegularExpressionErrorMessage,
                                RequiredErrorMessage = configViewModel.RequiredErrorMessage,
                                RangeErrorMessage = configViewModel.RangeErrorMessage,
                                LengthErrorMessage = configViewModel.LengthErrorMessage,
                            };

                            foreach (var item in jsonArray)
                            {
                                if (item is null)
                                {
                                    continue;
                                }
                                var result = Create(item, dim - 1);
                                if (result is not null)
                                {
                                    subConfigViewModel.Properties.Add(result);
                                }
                            }
                            return subConfigViewModel;
                        }
                    }
                    else
                    {
                        var subConfigViewModel = new ConfigViewModel(configViewModel.SubTypeName ?? "")
                        {
                            Type = configViewModel.SubType ?? ConfigModelType.Object,
                            DisplayName = configViewModel.DisplayName ?? configViewModel.Name,
                            GroupName = configViewModel.GroupName,
                            Description = configViewModel.Description,
                            Prompt = configViewModel.Prompt,
                            Minimum = configViewModel.Minimum,
                            Maximum = configViewModel.Maximum,
                            AllowedValues = configViewModel.AllowedValues is not null ? new ObservableCollection<string>(configViewModel.AllowedValues) : null,
                            DeniedValues = configViewModel.DeniedValues is not null ? new ObservableCollection<string>(configViewModel.DeniedValues) : null,
                            Options = configViewModel.Options is not null ? new ObservableCollection<KeyValuePair<string, string>>(configViewModel.Options) : null,
                            Required = configViewModel.Required,
                            RegularExpression = configViewModel.RegularExpression,
                            AllowedValuesErrorMessage = configViewModel.AllowedValuesErrorMessage,
                            DeniedValuesErrorMessage = configViewModel.DeniedValuesErrorMessage,
                            RegularExpressionErrorMessage = configViewModel.RegularExpressionErrorMessage,
                            RequiredErrorMessage = configViewModel.RequiredErrorMessage,
                            RangeErrorMessage = configViewModel.RangeErrorMessage,
                            LengthErrorMessage = configViewModel.LengthErrorMessage,

                        };

                        if (configViewModel.SubType == ConfigModelType.Object)
                        {
                            var sub = _configModels?.Find(c => !c.MainType && c.TypeName == configViewModel.SubTypeName);
                            if (sub is not null)
                            {
                                subConfigViewModel.Value = "create";
                                foreach (var item1 in sub.PropertyModels)
                                {
                                    SetConfigViewModel(item1, jsonNode as JsonObject, _configModels, subConfigViewModel.Properties);
                                }
                            }
                        }
                        else
                        {
                            subConfigViewModel.Value = jsonNode.ToJsonString();
                        }

                        return subConfigViewModel;
                    }
                    return null;
                }

                if (array is null)
                {
                    return;
                }

                configViewModel.Properties.Clear();


                foreach (var item in array)
                {
                    if (item is null)
                    {
                        continue;
                    }
                    var result = Create(item, (configViewModel.Dim ?? 0) - 1);
                    if (result is not null)
                    {
                        configViewModel.Properties.Add(result);
                    }
                }

            }
            else if (configViewModel.Type == ConfigModelType.String)
            {
                configViewModel.Value = json.Trim('"');
            }
            else
            {
                configViewModel.Value = json;
            }
        }
    }

    internal class ConfigInfoViewModel : ObservableObject, INotifyDataErrorInfo
    {
        private string _name = string.Empty;
        private string _filePath = string.Empty;
        public ConfigInfoViewModel()
        {
            Configs = new ObservableCollection<ConfigViewModel>();
            Configs.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (ConfigViewModel vm in e.NewItems)
                    {
                        vm.ErrorsChanged += Child_ErrorsChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (ConfigViewModel vm in e.OldItems)
                    {
                        vm.ErrorsChanged -= Child_ErrorsChanged;
                    }
                }
                RaiseErrorsChanged(string.Empty);
            };
        }

        private void Child_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            // bubble up
            RaiseErrorsChanged(string.Empty);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<ConfigViewModel> Configs { get; set; }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public bool HasErrors => Configs.Any(c => c.HasErrors);

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            // aggregate child errors
            var list = new List<string>();
            foreach (var c in Configs)
            {
                if (c.HasErrors)
                {
                    var errs = c.GetErrors(propertyName) as IEnumerable<string>;
                    if (errs != null)
                        list.AddRange(errs);
                }
            }
            return list;
        }
    }

    internal class ConfigViewModel : ObservableObject, INotifyDataErrorInfo
    {
        public ConfigViewModel(string name)
        {
            Name = name;
            Properties = new ObservableCollection<ConfigViewModel>();
            Properties.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (ConfigViewModel vm in e.NewItems)
                    {
                        vm.ErrorsChanged += Child_ErrorsChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (ConfigViewModel vm in e.OldItems)
                    {
                        vm.ErrorsChanged -= Child_ErrorsChanged;
                    }
                }
                Validate();
            };
        }

        private void Child_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            // parent validity depends on children
            Validate();
        }

        // Validation storage
        private readonly Dictionary<string, List<string>> _errors = new();

        public bool HasErrors
        {
            get
            {
                if (_errors.Any(kv => kv.Value?.Count > 0)) return true;
                // children
                return Properties.Any(p => p.HasErrors);
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.SelectMany(kv => kv.Value).ToList();
            }
            if (_errors.TryGetValue(propertyName!, out var list))
            {
                return list;
            }
            return new List<string>();
        }

        public void SetValidationRule()
        {
            // format messages that reference Name
            if (!string.IsNullOrEmpty(RequiredErrorMessage))
            {
                RequiredErrorMessage = string.Format(RequiredErrorMessage, Name);
            }
            if (!string.IsNullOrEmpty(AllowedValuesErrorMessage))
            {
                AllowedValuesErrorMessage = string.Format(AllowedValuesErrorMessage, Name);
            }
            if (!string.IsNullOrEmpty(DeniedValuesErrorMessage))
            {
                DeniedValuesErrorMessage = string.Format(DeniedValuesErrorMessage, Name);
            }
            if (!string.IsNullOrEmpty(RegularExpressionErrorMessage))
            {
                RegularExpressionErrorMessage = string.Format(RegularExpressionErrorMessage, Name);
            }
            if (!string.IsNullOrEmpty(RangeErrorMessage))
            {
                RangeErrorMessage = string.Format(RangeErrorMessage, Name);
            }
            if (!string.IsNullOrEmpty(LengthErrorMessage))
            {
                LengthErrorMessage = string.Format(LengthErrorMessage, Name);
            }

            Validate();
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.TryGetValue(propertyName, out var list))
            {
                list = new List<string>();
                _errors[propertyName] = list;
            }
            if (!list.Contains(error))
            {
                list.Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                RaiseErrorsChanged(propertyName);
            }
        }

        public void Validate()
        {
            // validate Value property
            ClearErrors(nameof(Value));

            // Required
            if (Required && string.IsNullOrWhiteSpace(Value))
            {
                AddError(nameof(Value), RequiredErrorMessage ?? "Required");
            }

            // AllowedValues
            if (AllowedValues is not null && !AllowedValues.Contains(Value ?? string.Empty))
            {
                AddError(nameof(Value), AllowedValuesErrorMessage ?? "Value not allowed");
            }

            // DeniedValues
            if (DeniedValues is not null && DeniedValues.Contains(Value ?? string.Empty))
            {
                AddError(nameof(Value), DeniedValuesErrorMessage ?? "Value denied");
            }

            // Type specific
            if (Type == ConfigModelType.Number)
            {
                if (Value is not null && !double.TryParse(Value, out var v))
                {
                    AddError(nameof(Value), RangeErrorMessage ?? "Not a number");
                }
                else
                {
                    if (!double.IsNaN(Maximum) && double.TryParse(Value, out var v2) && v2 > Maximum)
                    {
                        AddError(nameof(Value), RangeErrorMessage ?? "Exceeds maximum");
                    }
                    if (!double.IsNaN(Minimum) && double.TryParse(Value, out var v3) && v3 < Minimum)
                    {
                        AddError(nameof(Value), RangeErrorMessage ?? "Below minimum");
                    }
                }
            }
            else if (Type == ConfigModelType.Boolean)
            {
                if (Value is not null && !bool.TryParse(Value, out _))
                {
                    AddError(nameof(Value), "Not a boolean");
                }
            }
            else if (Type == ConfigModelType.DateTime)
            {
                if (Value is not null && !DateTime.TryParse(Value, out _))
                {
                    AddError(nameof(Value), "Not a valid datetime");
                }
            }
            else if (Type == ConfigModelType.DateOnly)
            {
                if (Value is not null && !DateOnly.TryParse(Value.Split(' ')[0], out _))
                {
                    AddError(nameof(Value), "Not a valid date");
                }
            }
            else if (Type == ConfigModelType.TimeOnly)
            {
                if (Value is not null)
                {
                    var timeStr = Value.Contains(' ') ? Value.Split(' ')[1] : Value;
                    if (!TimeOnly.TryParse(timeStr, out _))
                    {
                        AddError(nameof(Value), "Not a valid time");
                    }
                }
            }
            else if (Type == ConfigModelType.String)
            {
                if (Value is not null && RegularExpression is not null && !Regex.IsMatch(Value, RegularExpression))
                {
                    AddError(nameof(Value), RegularExpressionErrorMessage ?? "Not match regex");
                }
                if (Value?.Length > Maximum)
                {
                    AddError(nameof(Value), LengthErrorMessage ?? "Too long");
                }
                if (Value?.Length < Minimum)
                {
                    AddError(nameof(Value), LengthErrorMessage ?? "Too short");
                }
            }

            // raise errors changed for parent consumers
            RaiseErrorsChanged(nameof(Value));
        }

        public override string? ToString()
        {
            return $"{Type}:{SubType}:{SubTypeName}:{Dim}";
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private ConfigModelType _type;
        public ConfigModelType Type
        {
            get => _type;
            set
            {
                SetProperty(ref _type, value);
                Validate();
            }
        }

        public int? Order { get; set; }

        private ConfigModelType? _subType;
        public ConfigModelType? SubType
        {
            get => _subType;
            set => SetProperty(ref _subType, value);
        }
        private string? _subTypeName;
        public string? SubTypeName { get => _subTypeName; set => SetProperty(ref _subTypeName, value); }
        private string? _displayName;
        public string? DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
        public string? GroupName { get; set; }
        public string? Description { get; set; }
        public string? Prompt { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public ObservableCollection<string>? AllowedValues { get; set; }
        public ObservableCollection<string>? DeniedValues { get; set; }
        public ObservableCollection<KeyValuePair<string, string>>? Options { get; set; }
        public bool Required { get; set; }
        public string? RegularExpression { get; set; }

        private string? _value;
        public string? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    Validate();
                }
            }
        }

        public int? Dim { get; set; }
        public string? LengthErrorMessage { get; set; }
        public string? AllowedValuesErrorMessage { get; set; }
        public string? DeniedValuesErrorMessage { get; set; }
        public string? RegularExpressionErrorMessage { get; set; }
        public string? RequiredErrorMessage { get; set; }
        public string? RangeErrorMessage { get; set; }

        public ObservableCollection<ConfigViewModel> Properties { get; set; }
    }

    internal class DimViewModel : ObservableObject
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

        public int Length { get; set; }
        public bool ReadOnly { get; set; }
        public string DisplayName { get; set; }
    }
}
