using System;

namespace RESTable.Json.Tests;

public class TypeTestsClass
{
    public string String { get; set; }
    public decimal Decimal { get; set; }
    public byte Byte { get; set; }
    public sbyte SByte { get; set; }
    public short Short { get; set; }
    public ushort Ushort { get; set; }
    public int Int { get; set; }
    public uint Uint { get; set; }
    public long Long { get; set; }
    public ulong Ulong { get; set; }
    public float Single { get; set; }
    public double Double { get; set; }
    public EnumType Enum { get; set; }
    public object Null { get; set; }
    public bool Bool { get; set; }
    public char Char { get; set; }
    public DateTime DateTime { get; set; }

    public static TypeTestsClass CreatePopulatedInstance(byte value)
    {
        return new()
        {
            DateTime = DateTime.UtcNow,
            Char = 'X',
            Bool = true,
            Null = null,
            Enum = EnumType.B,
            Double = value,
            Single = value,
            Ulong = value,
            Long = value,
            Uint = value,
            Int = value,
            Ushort = value,
            Short = value,
            SByte = (sbyte) value,
            Byte = value,
            Decimal = value,
            String = value.ToString()
        };
    }

    protected bool Equals(TypeTestsClass other)
    {
        return String == other.String &&
               Decimal == other.Decimal &&
               Byte == other.Byte &&
               SByte == other.SByte &&
               Short == other.Short &&
               Ushort == other.Ushort &&
               Int == other.Int &&
               Uint == other.Uint &&
               Long == other.Long &&
               Ulong == other.Ulong &&
               Single.Equals(other.Single) &&
               Double.Equals(other.Double) &&
               Enum == other.Enum &&
               Equals(Null, other.Null) &&
               Bool == other.Bool &&
               Char == other.Char &&
               DateTime.Equals(other.DateTime);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TypeTestsClass) obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(String);
        hashCode.Add(Decimal);
        hashCode.Add(Byte);
        hashCode.Add(SByte);
        hashCode.Add(Short);
        hashCode.Add(Ushort);
        hashCode.Add(Int);
        hashCode.Add(Uint);
        hashCode.Add(Long);
        hashCode.Add(Ulong);
        hashCode.Add(Single);
        hashCode.Add(Double);
        hashCode.Add((int) Enum);
        hashCode.Add(Null);
        hashCode.Add(Bool);
        hashCode.Add(Char);
        hashCode.Add(DateTime);
        return hashCode.ToHashCode();
    }
}
