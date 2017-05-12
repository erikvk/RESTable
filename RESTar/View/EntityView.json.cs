using System;
using System.Collections.Generic;
using Starcounter;
using IResource = RESTar.Internal.IResource;

namespace RESTar.View
{
    partial class EntityView : Json, IRESTarView, IBound<object>
    {
        public IResource Resource { get; set; }
        public IRequest Request { get; set; }

        protected override void OnData()
        {
            base.OnData();
            Entity = Data.ToEntity();
        }

        public void Handle(Input.Save action)
        {
            this.Commit();
        }

        public void Handle(Input.Close action)
        {
        }

        internal static EntityView Make(IRequest request, object data)
        {
            var view = new EntityView
            {
                Request = request,
                Resource = request.Resource,
                Data = data,
                ResourceName = request.Resource.Name
            };
            view.Html = request.Resource.EntityViewHtmlPath ?? view.Html;
            return view;
        }
    }
}