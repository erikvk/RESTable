using System;
using System.Collections.Generic;

namespace RESTar.SQLite
{
    [RESTar]
    public class SQLiteIndex : ISelector<SQLiteIndex>, IInserter<SQLiteIndex>, IUpdater<SQLiteIndex>, IDeleter<SQLiteIndex>
    {
        public IEnumerable<SQLiteIndex> Select(IRequest<SQLiteIndex> request)
        {

            throw new NotImplementedException();
        }

        public int Insert(IEnumerable<SQLiteIndex> entities, IRequest<SQLiteIndex> request)
        {
            throw new NotImplementedException();
        }

        public int Update(IEnumerable<SQLiteIndex> entities, IRequest<SQLiteIndex> request)
        {
            throw new NotImplementedException();
        }

        public int Delete(IEnumerable<SQLiteIndex> entities, IRequest<SQLiteIndex> request)
        {
            throw new NotImplementedException();
        }
    }
}
