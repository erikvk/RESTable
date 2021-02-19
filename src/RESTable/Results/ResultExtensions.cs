using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace RESTable.Results
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Serializes the result into an instance of ISerializedResult
        /// </summary>
        public static async Task<ISerializedResult> Serialize(this IResult result, Stream customOutputStream = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var serializedResult = new SerializedResult(result, customOutputStream);
            try
            {
                if (serializedResult.Body == null)
                    return serializedResult;
                await serializedResult.Body.Serialize(serializedResult);
                return serializedResult;
            }
            catch (Exception exception)
            {
                await serializedResult.DisposeAsync();
                return await exception.AsResultOf(result.Request).Serialize(customOutputStream);
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