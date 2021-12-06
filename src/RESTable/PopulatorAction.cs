using System.Threading.Tasks;

namespace RESTable;

public delegate ValueTask<object> PopulatorAction(object target);