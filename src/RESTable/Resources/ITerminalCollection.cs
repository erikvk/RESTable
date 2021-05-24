using System.Collections.Generic;

namespace RESTable.Resources
{
    public interface ITerminalCollection<T> : IEnumerable<T> where T : Terminal { }
}