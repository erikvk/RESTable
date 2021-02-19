using System;
using System.Net;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A 200 result that encodes a content
    /// </summary>
    public abstract class Content : OK
    {
        /// <summary>
        /// Is this content locked, for example due to it being tied to a certain
        /// Websocket streaming operation?
        /// </summary>
        internal bool IsLocked { get; set; }

        [RESTableMember(ignore: true)] 
        public Type ResourceType { get; }

        /// <summary>
        /// Sets the ContentDisposition header to a unique file name of a given extension
        /// </summary>
        public void SetContentDisposition(string extension)
        {
            if (extension == null) return;
            Headers["Content-Disposition"] =
                $"attachment;filename={Request.Resource}_{DateTime.UtcNow:yyMMddHHmmssfff}{extension}";
        }

        /// <inheritdoc />
        protected Content(IRequest request) : base(request)
        {
            ResourceType = request.Resource.Type;
        }

        public void MakeNoContent()
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            Headers.Info = "No entities found matching request.";
            if (Request.Headers.Metadata == "full")
                Headers.Metadata = Metadata;
        }
    }
}