using System.Collections.Generic;

namespace RESTable.Json.Tests;

public class StaticTestsClass
{
    public string String { get; set; }
    public string ReadonlyString => String;

    public int Integer { get; set; }
    public int ReadonlyInteger => Integer;

    public StaticTestsClass Inner { get; set; }
    public StaticTestsClass ReadonlyInner => Inner;

    public IEnumerable<string> Enumerable { get; set; }
    public IEnumerable<string> ReadonlyEnumerable => Enumerable;

    public Dictionary<string, object> Dictionary { get; set; }
    public Dictionary<string, object> ReadonlyDictionary => Dictionary;
}
