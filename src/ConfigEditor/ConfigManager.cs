using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ConfigEditor
{
    public class ConfigManager
    {
        private JsonSerializerOptions JsonSerializerOptions { get; set; }
        public ConfigManager()
        {
            JsonSerializerOptions = new JsonSerializerOptions()
            {
                //格式化

                //显示中文编码


            };
        }
        /// <summary>
        /// 读取 json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<T> Read<T>(string path, string name)
            where T : class, new()
        {
            var configPath = Path.Combine(path, name + ".json");
            if (!File.Exists(configPath))
            {
                await Generate<T>(path, name);
            }
            var config = await File.ReadAllTextAsync(configPath);
            var result = JsonSerializer.Deserialize<T>(config, JsonSerializerOptions);

            if (result is null)
            {
                return new T();
            }

            return result;
        }
        /// <summary>
        /// 读取 definition.json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<ConfigModel>> ReadDefinition<T>(string path, string name)
        {
            var configPath = Path.Combine(path, name + ".definition.json");
            if (!File.Exists(configPath))
            {
                throw new Exception("definition.json 不存在");
            }
            var config = await File.ReadAllTextAsync(configPath);
            var result = JsonSerializer.Deserialize<List<ConfigModel>>(config, JsonSerializerOptions);

            if (result is null)
            {
                return [];
            }

            return result;
        }
        /// <summary>
        /// 生成 json,及 definition.json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task Generate<T>(string path, string name)
            where T : class, new()
        {
            List<ConfigModel> configs = [];
            GenerateConfigModel(typeof(T), configs);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            await File.WriteAllTextAsync(Path.Combine(path, name + ".definition.json"), JsonSerializer.Serialize(configs, typeof(List<ConfigModel>), JsonSerializerOptions));
            var configPath = Path.Combine(path, name + ".json");
            if (!File.Exists(configPath))
            {
                await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new T(), typeof(T), JsonSerializerOptions));
            }
        }
        /// <summary>
        /// 生成属性
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeModels"></param>
        /// <param name="mainType"></param>
        private void GenerateConfigModel(Type type, List<ConfigModel> typeModels, bool mainType = true)
        {
            if (typeModels.Any(c => c.TypeName == type.Name))
            {
                return;
            }
            ConfigModel typeModel = new() { TypeName = type.Name, MainType = mainType };
            typeModels.Add(typeModel);
            //使用反射遍历所有属性
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                var configModel = new PropertyModel(property.Name);
                //获取属性上的特性
                var attributes = property.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                {
                    //如果是Display
                    if (attribute is DisplayAttribute displayAttribute)
                    {
                        configModel.DisplayName = displayAttribute.Name;
                        configModel.Description = displayAttribute.GetDescription();
                        configModel.GroupName = displayAttribute.GetGroupName();
                        configModel.Order = displayAttribute.GetOrder() ?? 0;
                        configModel.Prompt = displayAttribute.GetPrompt();
                    }
                    //如果是Range
                    if (attribute is RangeAttribute rangeAttribute)
                    {
                        configModel.Minimum = (int)rangeAttribute.Minimum;
                        configModel.Maximum = (int)rangeAttribute.Maximum;
                    }
                    //如果是Required
                    if (attribute is RequiredAttribute requiredAttribute)
                    {
                        configModel.Required = true;
                    }
                    //如果是AllowedValues
                    if (attribute is AllowedValuesAttribute allowedValuesAttribute)
                    {
                        configModel.AllowedValues = allowedValuesAttribute.Values.Select(c => c?.ToString() ?? string.Empty).ToList();
                    }
                    //如果是DeniedValues
                    if (attribute is DeniedValuesAttribute deniedValuesAttribute)
                    {
                        configModel.DeniedValues = deniedValuesAttribute.Values.Select(c => c?.ToString() ?? string.Empty).ToList();
                    }
                    if (attribute is LengthAttribute lengthAttribute)
                    {
                        configModel.Minimum = lengthAttribute.MinimumLength;
                        configModel.Maximum = lengthAttribute.MaximumLength;
                    }
                    //MaxLength
                    if (attribute is MaxLengthAttribute maxLengthAttribute)
                    {
                        configModel.Maximum = maxLengthAttribute.Length;
                    }
                    //minLength
                    if (attribute is MinLengthAttribute minLengthAttribute)
                    {
                        configModel.Minimum = minLengthAttribute.Length;
                    }
                    //RegularExpression
                    if (attribute is RegularExpressionAttribute regularExpressionAttribute)
                    {
                        configModel.RegularExpression = regularExpressionAttribute.Pattern;
                    }
                    //Option
                    if (attribute is OptionAttribute optionAttribute)
                    {
                        configModel.Options.Add(new KeyValuePair<string, string>(optionAttribute.DisplayName, optionAttribute.Value));
                    }
                }

                //如果是String
                if (property.PropertyType == typeof(string))
                {
                    configModel.Type = ConfigModelType.String;
                }
                //如果是Number
                else if (property.PropertyType == typeof(int) ||
                        property.PropertyType == typeof(double) ||
                        property.PropertyType == typeof(float) ||
                        property.PropertyType == typeof(decimal) ||
                        property.PropertyType == typeof(long) ||
                        property.PropertyType == typeof(ulong) ||
                        property.PropertyType == typeof(uint) ||
                        property.PropertyType == typeof(short) ||
                        property.PropertyType == typeof(ushort) ||
                        property.PropertyType == typeof(byte) ||
                        property.PropertyType == typeof(sbyte) ||
                        property.PropertyType == typeof(char))
                {
                    configModel.Type = ConfigModelType.Number;
                }
                //如果是Boolean
                else if (property.PropertyType == typeof(bool))
                {
                    configModel.Type = ConfigModelType.Boolean;
                }
                //如果是Enum
                else if (property.PropertyType.IsEnum)
                {
                    configModel.Type = ConfigModelType.Number;
                    // 显示内容为枚举的名称，值为枚举的值
                    configModel.Options = property.PropertyType.GetEnumNames().Select(c => new KeyValuePair<string, string>(c, ((int)Enum.Parse(property.PropertyType, c)).ToString())).ToList();
                }
                //如果是Array 或者是List
                else if (property.PropertyType.IsArray || property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    configModel.Type = ConfigModelType.Array;
                    configModel.SubType = property.PropertyType.GetGenericArguments()[0] switch
                    {
                        Type t when t == typeof(string) => ConfigModelType.String,
                        Type t when t == typeof(int) || t == typeof(double) || t == typeof(float) || t == typeof(decimal) || t == typeof(long) || t == typeof(ulong) || t == typeof(uint) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte) || t == typeof(char) => ConfigModelType.Number,
                        Type t when t == typeof(bool) => ConfigModelType.Boolean,
                        Type t when t == typeof(DateTime) => ConfigModelType.DateTime,
                        Type t when t == typeof(TimeSpan) => ConfigModelType.TimeSpan,
                        Type t when t == typeof(DateOnly) => ConfigModelType.DateOnly,
                        Type t when t == typeof(TimeOnly) => ConfigModelType.TimeOnly,
                        Type t when t.IsEnum => ConfigModelType.Number,
                        _ => ConfigModelType.Object
                    };
                    int GetDim(Type type)
                    {
                        if (type.IsArray)
                        {
                            return GetDim(type.GetElementType()) + 1;
                        }
                        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            return GetDim(type.GetGenericArguments()[0]) + 1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    string GetSubTypeName(Type type)
                    {
                        if (type.IsArray)
                        {
                            return type.GetElementType()?.Name ?? string.Empty;
                        }
                        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            //如果多个List嵌套
                            if (type.GetGenericArguments()[0].IsGenericType && type.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(List<>))
                            {
                                return GetSubTypeName(type.GetGenericArguments()[0]);
                            }
                            return type.GetGenericArguments()[0].Name;
                        }
                        else
                        {
                            return type.Name;
                        }
                    }
                    configModel.Dim = GetDim(property.PropertyType);
                    configModel.SubTypeName = GetSubTypeName(property.PropertyType);
                    GenerateConfigModel(property.PropertyType.GetGenericArguments()[0], typeModels, false);
                }
                //如果是Object
                else if (property.PropertyType.IsClass)
                {
                    configModel.Type = ConfigModelType.Object;
                    configModel.SubTypeName = property.PropertyType.Name;
                    GenerateConfigModel(property.PropertyType, typeModels, false);
                }
                //如果是DateTime
                else if (property.PropertyType == typeof(DateTime))
                {
                    configModel.Type = ConfigModelType.DateTime;
                }
                //如果是TimeSpan
                else if (property.PropertyType == typeof(TimeSpan))
                {
                    configModel.Type = ConfigModelType.TimeSpan;
                }
                //如果是DateOnly
                else if (property.PropertyType == typeof(DateOnly))
                {
                    configModel.Type = ConfigModelType.DateOnly;
                }
                //如果是TimeOnly
                else if (property.PropertyType == typeof(TimeOnly))
                {
                    configModel.Type = ConfigModelType.TimeOnly;
                }
                else
                {
                    throw new NotImplementedException("不支持此类型 " + property.Name);
                }



                typeModel.PropertyModels.Add(configModel);
            }
        }
    }
}
