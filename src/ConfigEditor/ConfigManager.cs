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
        /// 读取json
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
        /// 生成json,及definition.json
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
                        configModel.Description = displayAttribute.Description;
                        configModel.GroupName = displayAttribute.GroupName;
                        configModel.Order = displayAttribute.Order;
                        configModel.Prompt = displayAttribute.Prompt;
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
                    configModel.Type = ConfigModelType.String;
                    configModel.AllowedValues.AddRange([.. Enum.GetNames(property.PropertyType)]);
                }
                //如果是Array 或者是List
                else if (property.PropertyType.IsArray || property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    configModel.Type = ConfigModelType.Array;
                    configModel.SubType = property.PropertyType.GetGenericArguments()[0].Name;
                    GenerateConfigModel(property.PropertyType.GetGenericArguments()[0], typeModels, false);
                }
                //如果是Object
                else if (property.PropertyType.IsClass)
                {
                    configModel.Type = ConfigModelType.Object;
                    configModel.SubType = property.PropertyType.Name;
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
