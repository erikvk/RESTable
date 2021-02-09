using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace RESTable.Results
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Serializes the result into an instance of ISerializedResult
        /// </summary>
        public static async Task<ISerializedResult> Serialize(this IResult result)
        {
            var stopwatch = Stopwatch.StartNew();
            var serializedResult = new SerializedResult(result);
            if (serializedResult.Body == null)
            {
                result.Headers.Elapsed = result.TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
                return serializedResult;
            }
            try
            {
                await serializedResult.Body.Serialize(result);
                return serializedResult;
            }
            catch (Exception exception)
            {
                await serializedResult.DisposeAsync();
                return await exception.AsResultOf(result.Request).Serialize();
            }
            finally
            {
                stopwatch.Stop();
                result.TimeElapsed += stopwatch.Elapsed;
                result.Headers.Elapsed = result.TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}