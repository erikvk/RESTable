#if !NETSTANDARD2_0
using System;
using System.Runtime.CompilerServices;

namespace RESTable.Requests
{
    public readonly struct ConfiguredEntityBufferTask<T> where T : class
    {
        private EntityBufferTask<T> BufferTask { get; }
        private bool ContinueOnCapturedContext { get; }

        public ConfiguredEntityBufferTask(EntityBufferTask<T> bufferTask, bool continueOnCapturedContext)
        {
            BufferTask = bufferTask;
            ContinueOnCapturedContext = continueOnCapturedContext;
        }

        public ConfiguredValueTaskAwaitable<ReadOnlyMemory<T>>.ConfiguredValueTaskAwaiter GetAwaiter()
        {
            return BufferTask
                .AsReadOnlyMemoryAsync()
                .ConfigureAwait(ContinueOnCapturedContext)
                .GetAwaiter();
        }
    }
}
#endif
