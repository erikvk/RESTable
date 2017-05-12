using System;
using Starcounter;

namespace RESTar.Operations
{
    internal static class Patcher
    {
        internal static int PATCH(object entity, string json, IRequest request)
        {
            try
            {
                var count = 0;
                Db.TransactAsync(() =>
                {
                    JsonSerializer.PopulateObject(json, entity);
                    var validatableResult = entity as IValidatable;
                    if (validatableResult != null)
                    {
                        string reason;
                        if (!validatableResult.Validate(out reason))
                            throw new ValidatableException(reason);
                    }
                    count = request.Resource.Update(entity.MakeList(request.Resource.TargetType), request);
                });
                return count;
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e);
            }
        }
    }
}