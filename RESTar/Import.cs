namespace RESTar
{
//    [VirtualResource, RESTar(RESTarMethods.GET, RESTarMethods.PATCH, RESTarMethods.POST)]
//    public class Import
//    {
//        public string ObjectRef { get; set; }
//
//        [IgnoreDataMember]
//        public ActiveImports ActiveImports
//        {
//            get { return ImportProjectObjectNo.GetReference<ActiveImports>(); }
//            set { ImportProjectObjectNo = value.GetObjectNo(); }
//        }
//
//
//
//        [ObjectRef, DataMember(Name = "ImportProject")]
//        public ulong? ImportProjectObjectNo { get; set; }
//    }

//    [Database, RESTar(RESTarPresets.ReadOnly)]
//    public class ActiveImports
//    {
//        public bool Active { get; set; }
//        public bool ReferencesFixed { get; set; }
//        public bool Committed { get; set; }
//        public bool RolledBack { get; set; }
//        public long EntityCount => Imports.LongCount();
//
//        [IgnoreDataMember]
//        public IEnumerable<Type> Resources => Imports.Select(i => i.GetType());
//
//        [IgnoreDataMember]
//        public IEnumerable<Import> Imports => DB.All<Import>("ImportProject", this);
//    }
//
//
//    [Database, RESTar(RESTarPresets.ReadOnly)]
//    public class ImportEntity
//    {
//        [IgnoreDataMember]
//        public Import ActiveImports
//        {
//            get { return ImportObjectNo.GetReference<Import>(); }
//            set { ImportObjectNo = value.GetObjectNo(); }
//        }
//
//        [ObjectRef, DataMember(Name = "Import")]
//        public ulong? ImportObjectNo { get; set; }
//
//        public ulong? EntityObjectNo { get; set; }
//
//        public dynamic Entity => DbHelper.FromID(EntityObjectNo.GetValueOrDefault());
//
//        public static Response GET(IRequest request)
//        {
//            return null;
//        }
//    }
}