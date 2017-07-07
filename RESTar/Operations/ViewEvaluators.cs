namespace RESTar.Operations
{
    //internal static class ViewEvaluators
    //{
    //    internal void POST<T>(RESTRequest<T> request, string json) where T : class
    //    {
    //        UserCheck();
    //        if (MethodAllowed(RESTarMethods.POST))
    //        {
    //            try
    //            {
    //                RESTEvaluators<T>.POST(json, request);
    //                Success = true;
    //            }
    //            catch (AbortedInserterException e)
    //            {
    //                SetMessage(e.InnerException?.Message ?? e.Message, e.ErrorCode, error);
    //            }
    //        }
    //        else SetMessage($"You are not allowed to insert into the '{Resource}' resource", NotAuthorized, error);
    //    }

    //    /// <summary>
    //    /// </summary>
    //    protected void PATCH(string json)
    //    {
    //        UserCheck();
    //        if (MethodAllowed(RESTarMethods.PATCH))
    //        {
    //            try
    //            {
    //                RESTEvaluators<TResource>.PATCH(RESTarData, json, Request);
    //                Success = true;
    //            }
    //            catch (AbortedUpdaterException e)
    //            {
    //                SetMessage(e.InnerException?.Message ?? e.Message, e.ErrorCode, error);
    //            }
    //        }
    //        else SetMessage($"You are not allowed to update the '{Resource}' resource", NotAuthorized, error);
    //    }

    //    /// <summary>
    //    /// </summary>
    //    protected void DELETE(object item)
    //    {
    //        UserCheck();
    //        if (MethodAllowed(RESTarMethods.DELETE))
    //        {
    //            try
    //            {
    //                RESTEvaluators<TResource>.DELETE(item, Request);
    //                Success = true;
    //            }
    //            catch (AbortedDeleterException e)
    //            {
    //                SetMessage(e.InnerException?.Message ?? e.Message, e.ErrorCode, error);
    //            }
    //        }
    //        else SetMessage($"You are not allowed to delete from the '{Resource}' resource", NotAuthorized, error);
    //    }

    //    /// <summary>
    //    /// </summary>
    //    protected bool MethodAllowed(RESTarMethods method) => MethodCheck(method, Resource, Request.AuthToken);
    //}
}