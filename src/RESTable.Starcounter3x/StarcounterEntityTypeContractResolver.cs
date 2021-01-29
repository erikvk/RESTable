using System;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Resources;
using Starcounter.Database;
using Starcounter.Database.Data;

namespace RESTable.Starcounter3x
{
    public class StarcounterEntityTypeContractResolver : IEntityTypeContractResolver
    {
        public void ResolveContract(EntityTypeContract contract)
        {
            if (!contract.EntityType.IsStarcounterDatabaseType())
                return;

            contract.Properties.Add(new OidProperty(contract.EntityType));

            dynamic inserter = typeof(IDatabaseContext)
                .GetMethod("Insert")?
                .MakeGenericMethod(contract.EntityType)
                .CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IDatabaseContext), contract.EntityType));

            contract.CustomCreator = () =>
            {
                var currentTransaction = Transaction.Current ?? Transaction.Create();
                return currentTransaction.Run(db => inserter(db));
            };

            foreach (var property in contract.Properties)
            {
                if (property is DeclaredProperty declaredProperty && declaredProperty.IsStarcounterQueryable())
                    property.Flags.Add(Constants.StarcounterQueryableFlag);
            }
        }
    }
}