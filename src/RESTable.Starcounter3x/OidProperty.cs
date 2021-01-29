using System;
using RESTable.Requests;
using RESTable.Resources;
using Starcounter.Database;

namespace RESTable.Meta.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Special properties are properties that are not strictly members, but still
    /// important parts of class definitions. For example Starcounter ObjectID and 
    /// ObjectNo.
    /// </summary>
    internal class OidProperty : DeclaredProperty
    {
        private const string Oid = nameof(Oid);
        private static readonly int OidMetadataToken;

        static OidProperty()
        {
            OidMetadataToken = typeof(IDatabaseObjectContext)
                .GetMethod(nameof(IDatabaseObjectContext.GetOid), new[] {typeof(object)})?
                .MetadataToken ?? throw new NullReferenceException();
        }

        private static object GetOid(object target) => DbProxy.GetContext(target).GetOid(target);

        internal OidProperty(Type owner) : base
        (
            metadataToken: OidMetadataToken,
            name: Oid,
            actualName: Oid,
            type: typeof(ulong),
            order: int.MaxValue - 1,
            attributes: new[] {new KeyAttribute()},
            skipConditions: false,
            hidden: false,
            hiddenIfNull: false,
            isEnum: false,
            allowedConditionOperators: Operators.All,
            customDateTimeFormat: null,
            getter: GetOid,
            owner: owner,
            setter: null
        ) { }
    }
}