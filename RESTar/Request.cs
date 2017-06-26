using System;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar
{
    public static class Request<T> where T : class
    {
        #region GET

        public static IEnumerable<T> GET((string key, Operator @operator, dynamic value)?[] conditions,
            bool @unsafe, int limit, (string key, bool descending)? orderBy)
        {
            var ar = new AppRequest<T>
            {
                Unsafe = @unsafe,
                Limit = limit
            };
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.GET();
        }

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition = null,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition}, @unsafe, limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition1, condition2}, @unsafe, limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition1, condition2, condition3}, @unsafe, limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition1, condition2, condition3, condition4}, @unsafe, limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition1, condition2, condition3, condition4, condition5}, @unsafe, limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition1, condition2, condition3, condition4, condition5, condition6}, @unsafe, limit,
                orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7}, @unsafe,
                limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8},
                @unsafe, limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9
            }, @unsafe, limit, orderBy);

        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9, condition10
            }, @unsafe, limit, orderBy);

        #endregion

        public static int POST(Func<T> inserter) => new AppRequest<T>().POST(inserter);
        public static int POST(Func<IEnumerable<T>> inserter) => new AppRequest<T>().POST(inserter);

        #region PATCH

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)?[] conditions,
            bool @unsafe, int limit, (string key, bool descending)? orderBy = null)
        {
            var ar = new AppRequest<T>
            {
                Unsafe = @unsafe,
                Limit = limit
            };
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.PATCH(updater);
        }

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition = null,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[] {condition}, @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[] {condition1, condition2}, @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[] {condition1, condition2, condition3}, @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[] {condition1, condition2, condition3, condition4}, @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[] {condition1, condition2, condition3, condition4, condition5}, @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[] {condition1, condition2, condition3, condition4, condition5, condition6}, @unsafe,
                limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7},
                @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater,
                new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8},
                @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9
            }, @unsafe, limit);

        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10,
            bool @unsafe = false, int limit = -1) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9, condition10
            }, @unsafe, limit);

        #endregion

        #region PUT

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)?[] conditions,
            int limit = -1, (string key, bool descending)? orderBy = null)
        {
            var ar = new AppRequest<T> {Limit = limit};
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.PUT(inserter, updater);
        }

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition = null) =>
            PUT(inserter, updater, new[] {condition});

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2) =>
            PUT(inserter, updater, new[] {condition1, condition2});

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3) =>
            PUT(inserter, updater, new[] {condition1, condition2, condition3});

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4) =>
            PUT(inserter, updater, new[] {condition1, condition2, condition3, condition4});

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5) =>
            PUT(inserter, updater, new[] {condition1, condition2, condition3, condition4, condition5});

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6) =>
            PUT(inserter, updater, new[] {condition1, condition2, condition3, condition4, condition5, condition6});

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7) =>
            PUT(inserter, updater,
                new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7}
            );

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8) =>
            PUT(inserter, updater,
                new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8}
            );

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9
            });

        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9, condition10
            });

        #endregion

        #region DELETE

        public static int DELETE((string key, Operator @operator, dynamic value)?[] conditions,
            bool @unsafe, int limit, (string key, bool descending)? orderBy)
        {
            var ar = new AppRequest<T>
            {
                Unsafe = @unsafe,
                Limit = limit
            };
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.DELETE();
        }

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition = null,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[] {condition}, @unsafe, limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[] {condition1, condition2}, @unsafe, limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[] {condition1, condition2, condition3}, @unsafe, limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[] {condition1, condition2, condition3, condition4}, @unsafe, limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[] {condition1, condition2, condition3, condition4, condition5}, @unsafe, limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[] {condition1, condition2, condition3, condition4, condition5, condition6}, @unsafe, limit,
                orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7}, @unsafe,
                limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(
                new[] {condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8},
                @unsafe, limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9
            }, @unsafe, limit, orderBy);

        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5, condition6, condition7, condition8,
                condition9, condition10
            }, @unsafe, limit, orderBy);

        #endregion
    }
}