# Getting started

RESTable is distributed as a [.NET package on NuGet](https://www.nuget.org/packages/RESTable), and an easy way to install it in an active Visual Studio project is by entering the following into the NuGet Package Manager console:

```
Install-Package RESTable
```

If you're running Starcounter 2.4 or later, there is a version of RESTable for that as well. Use the following:

```
Install-Package RESTable_2.4
```

The installation will also download some other packages that are required for RESTable to work. RESTable itself, however, is contained within a single assembly, `RESTable.dll`.

When RESTable is installed, add a call to the [`RESTable.RESTableConfig.Init()`](RESTableConfig.Init) method somewhere in your application logic, preferably so it runs once every time the app starts. This method will set up the RESTable HTTP handlers, and collect all your [registered REST resources](#registering-resources). Below is a simple RESTable application, picked from the [RESTable Tutorial Repository](https://github.com/Mopedo/RESTable.Tutorial):

```csharp
namespace RESTableTutorial
{
    using RESTable;
    public class TutorialApp
    {
        public static void Main()
        {
            RESTableConfig.Init(port: 8282, uri: "/api");
            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
        }
    }
}
```

See [this page](RESTableConfig.Init) for details on what can be specified in the call to `RESTableConfig.Init()`.
