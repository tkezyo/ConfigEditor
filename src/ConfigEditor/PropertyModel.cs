namespace ConfigEditor
{
    public class ConfigModel
    {
        public required string TypeName { get; set; }
        public bool MainType { get; set; }
        public List<PropertyModel> PropertyModels { get; set; } = [];
    }
    public class PropertyModel(string name)
    {
        public string Name { get; set; } = name;

        public int Order { get; set; }
        public ConfigModelType Type { get; set; }
        public string? SubType { get; set; }
        public string? DisplayName { get; set; }
        public string? GroupName { get; set; }
        public string? Description { get; set; }
        public string? Prompt { get; set; }
        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
        public List<string> AllowedValues { get; set; } = [];
        public List<string> DeniedValues { get; set; } = [];
        public bool Required { get; set; }
        public string? RegularExpression { get; set; }
    }

    public enum ConfigModelType
    {
        String,
        Number,
        Boolean,
        TimeSpan,
        DateTime,
        DateOnly,
        TimeOnly,
        Object,
        Array
    }
}
