using ConfigEditor;
using System.ComponentModel.DataAnnotations;

namespace Test1;

public class DemoConfig
{
    [AllowedValues("123", "123")]
    [Required]
    [Length(1, 10)]
    public string? String { get; set; }
    [Range(1, 10)]
    [Required]
    [Display(Name = "数字")]
    [DeniedValues(1, 2)]
    public int Int { get; set; }
    public bool Bool { get; set; }
    [Option("11", "1")]
    [Option("22", "2")]
    public double Double { get; set; }
    [RegularExpression("123")]
    public float Float { get; set; }
    [Required]
    public decimal Decimal { get; set; }
    public long Long { get; set; }
    public ulong ULong { get; set; }
    public uint UInt { get; set; }
    public short Short { get; set; }
    public ushort UShort { get; set; }
    public byte Byte { get; set; }
    public sbyte SByte { get; set; }
    public char Char { get; set; }
    public DateTime DateTime { get; set; }
    public DateOnly DateOnly { get; set; }
    public TimeOnly TimeOnly { get; set; }
    public DemoEnum Enum { get; set; }
    public Demo2? Demo2 { get; set; }
    public List<Demo2> Demo2s { get; set; } = [];
    public List<int> Ints { get; set; } = [];
    public List<List<int>> IntInts { get; set; } = [];
    public double[][][] Doubledoubledoubles { get; set; } = [];
}

public class Demo2
{
    public string? String1 { get; set; }
    public string? String2 { get; set; }
    public int Int { get; set; }
    public bool Bool { get; set; }
    public List<Demo2> Demo2s { get; set; } = [];
}

public enum DemoEnum
{
    Value1,
    Value2,
    Value3
}
