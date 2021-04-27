using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Results
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Serializes the result into an instance of ISerializedResult
        /// </summary>
        public static async Task<ISerializedResult> Serialize(this IResult result, Stream customOutputStream = null, CancellationToken cancellationToken = new())
        {
            var serializedResult = new SerializedResult(result, customOutputStream);
            try
            {
                if (serializedResult.Body == null)
                    return serializedResult;
                await serializedResult.Body.Serialize(serializedResult, cancellationToken).ConfigureAwait(false);
                return serializedResult;
            }
            catch (Exception exception)
            {
                if (customOutputStream == null)
                    await serializedResult.DisposeAsync().ConfigureAwait(false);
                return await exception.AsResultOf(result.Request).Serialize(customOutputStream, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                result.Headers.Elapsed = result.TimeElapsed;
            }
        }
    }
}