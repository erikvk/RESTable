using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Resources;
using RESTable.Resources.Templates;

namespace RESTable.Admin
{
    /// <inheritdoc cref="OptionsTerminal" />
    /// <summary>
    /// Provides access to commmon admin tasks
    /// </summary>
    [RESTable(Description = description)]
    internal class Utilities : OptionsTerminal
    {
        private const string description = "The Utilities resource gives access to commonly used " +
                                           "tools for administrating a RESTable instance, in form of " +
                                           "views that can be used as static methods.";

        protected override IEnumerable<Option> GetOptions()
        {
            yield return new Option
            (
                command: "ReloadConfigFile",
                description: "Reloads the configuration file and updates the access rights for API keys",
                action: _ => Services.GetRequiredService<RESTableConfigurator>().UpdateConfiguration()
            );
        }
    }
}