namespace ConfigEditor;

public class ConfigModel
{
    public required string TypeName { get; set; }
    /// <summary>
    /// 如果是主类型，那么主类型中的属性将会被展开
    /// </summary>
    public bool MainType { get; set; }
    public List<PropertyModel> PropertyModels { get; set; } = [];
}
public class PropertyModel(string name)
{
    public string Name { get; set; } = name;

    public int? Order { get; set; }
    public ConfigModelType Type { get; set; }
    public ConfigModelType? SubType { get; set; }
    /// <summary>
    /// 如果是数组类型，那么这个值表示数组的维度
    /// </summary>
    public int? Dim { get; set; }
    public string? SubTypeName { get; set; }
    /// <summary>
    /// 各维度的长度
    /// </summary>
    public int[]? DimLength { get; set; }


    public string? DisplayName { get; set; }
    public string? GroupName { get; set; }
    public string? Description { get; set; }
    public string? Prompt { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public List<string>? AllowedValues { get; set; }
    public List<string>? DeniedValues { get; set; }
    public bool? Required { get; set; }
    public string? RegularExpression { get; set; }
    public List<KeyValuePair<string, string>>? Options { get; set; }
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
