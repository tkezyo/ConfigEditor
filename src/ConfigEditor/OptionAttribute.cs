namespace ConfigEditor;

[System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public sealed class OptionAttribute(string displayName, string value) : Attribute
{
    // See the attribute guidelines at 
    //  http://go.microsoft.com/fwlink/?LinkId=85236
    readonly string displayName = displayName;
    readonly string value = value;

    public string DisplayName
    {
        get { return displayName; }
    }
    public string Value
    {
        get { return value; }
    }
}

[System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ConfigPathAttribute : Attribute
{
    // See the attribute guidelines at 
    //  http://go.microsoft.com/fwlink/?LinkId=85236
    readonly string path;

    // This is a positional argument
    public ConfigPathAttribute(string path)
    {
        this.path = path;
    }

    public string Path
    {
        get { return path; }
    }
}