using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Resources.Operations
{
    public interface IEntityResourceOperationDefinition<TResource> where TResource : class
    {
        bool RequiresAuthentication { get; }
        bool CanSelect { get; }
        bool CanInsert { get; }
        bool CanUpdate { get; }
        bool CanDelete { get; }
        bool CanCount { get; }

        IAsyncEnumerable<TResource> SelectAsync(IRequest<TResource> request);
        Task<int> InsertAsync(IRequest<TResource> request);
        Task<int> UpdateAsync(IRequest<TResource> request);
        Task<int> DeleteAsync(IRequest<TResource> request);
        ValueTask<AuthResults> AuthenticateAsync(IRequest<TResource> request);
        Task<long> CountAsync(IRequest<TResource> request);
        IAsyncEnumerable<TResource> Validate(IAsyncEnumerable<TResource> entities);
    }

    internal class DelegateSet<TResource> : IEntityResourceOperationDefinition<TResource> where TResource : class
    {
        private Selector<TResource> SyncSelector { get; set; }
        private Inserter<TResource> SyncInserter { get; set; }
        private Updater<TResource> SyncUpdater { get; set; }
        private Deleter<TResource> SyncDeleter { get; set; }
        private Authenticator<TResource> SyncAuthenticator { get; set; }
        private Counter<TResource> SyncCounter { get; set; }

        // All operations are transformed into async delegates on resolve

        private AsyncSelector<TResource> AsyncSelector { get; set; }
        private AsyncInserter<TResource> AsyncInserter { get; set; }
        private AsyncUpdater<TResource> AsyncUpdater { get; set; }
        private AsyncDeleter<TResource> AsyncDeleter { get; set; }
        private AsyncAuthenticator<TResource> AsyncAuthenticator { get; set; }
        private AsyncCounter<TResource> AsyncCounter { get; set; }

        private Validator<TResource> Validator { get; set; }

        public bool RequiresAuthentication => AsyncAuthenticator != null;
        public bool CanSelect => AsyncSelector != null;
        public bool CanInsert => AsyncInserter != null;
        public bool CanUpdate => AsyncUpdater != null;
        public bool CanDelete => AsyncDeleter != null;
        public bool CanCount => AsyncCounter != null;

        public IAsyncEnumerable<TResource> SelectAsync(IRequest<TResource> request) => AsyncSelector(request);
        public Task<int> InsertAsync(IRequest<TResource> request) => AsyncInserter(request);
        public Task<int> UpdateAsync(IRequest<TResource> request) => AsyncUpdater(request);
        public Task<int> DeleteAsync(IRequest<TResource> request) => AsyncDeleter(request);
        public ValueTask<AuthResults> AuthenticateAsync(IRequest<TResource> request) => AsyncAuthenticator(request);
        public Task<long> CountAsync(IRequest<TResource> request) => AsyncCounter(request);

        public IAsyncEnumerable<TResource> Validate(IAsyncEnumerable<TResource> entities)
        {
            if (entities == null) return AsyncEnumerable.Empty<TResource>();
            if (Validator == null) return entities;
            return entities.Select(item =>
            {
                if (!Validator(item, out var invalidReason))
                    throw new FailedValidation(invalidReason);
                return item;
            });
        }

#pragma warning disable 1998

        private static async IAsyncEnumerable<TResource> CallAsync(IEnumerable<TResource> entities)
        {
            foreach (var item in entities)
            {
                yield return item;
            }
        }

#pragma warning restore 1998

        /// <summary>
        /// Transforms synchronous delegates to async where null
        /// </summary>
        internal DelegateSet<TResource> SetAsyncDelegatesToSyncWhereNull()
        {
            if (AsyncSelector == null && SyncSelector is Selector<TResource> selector)
                AsyncSelector = request => CallAsync(selector(request));
            if (AsyncInserter == null && SyncInserter is Inserter<TResource> inserter)
                AsyncInserter = request => Task.FromResult(inserter(request));
            if (AsyncUpdater == null && SyncUpdater is Updater<TResource> updater)
                AsyncUpdater = request => Task.FromResult(updater(request));
            if (AsyncDeleter == null && SyncDeleter is Deleter<TResource> deleter)
                AsyncDeleter = request => Task.FromResult(deleter(request));
            if (AsyncAuthenticator == null && SyncAuthenticator is Authenticator<TResource> authenticator)
                AsyncAuthenticator = request => new ValueTask<AuthResults>(authenticator(request));
            if (AsyncCounter == null && SyncCounter is Counter<TResource> counter)
                AsyncCounter = request => Task.FromResult(counter(request));
            return this;
        }

        internal DelegateSet<TResource> GetDelegatesFromTargetWhereNull<TTarget>()
        {
            SyncSelector ??= DelegateMaker.GetDelegate<Selector<TResource>>(typeof(TTarget));
            SyncInserter ??= DelegateMaker.GetDelegate<Inserter<TResource>>(typeof(TTarget));
            SyncUpdater ??= DelegateMaker.GetDelegate<Updater<TResource>>(typeof(TTarget));
            SyncDeleter ??= DelegateMaker.GetDelegate<Deleter<TResource>>(typeof(TTarget));
            SyncAuthenticator ??= DelegateMaker.GetDelegate<Authenticator<TResource>>(typeof(TTarget));
            SyncCounter ??= DelegateMaker.GetDelegate<Counter<TResource>>(typeof(TTarget));
            Validator ??= DelegateMaker.GetDelegate<Validator<TResource>>(typeof(TTarget));
            AsyncSelector ??= DelegateMaker.GetDelegate<AsyncSelector<TResource>>(typeof(TTarget));
            AsyncInserter ??= DelegateMaker.GetDelegate<AsyncInserter<TResource>>(typeof(TTarget));
            AsyncUpdater ??= DelegateMaker.GetDelegate<AsyncUpdater<TResource>>(typeof(TTarget));
            AsyncDeleter ??= DelegateMaker.GetDelegate<AsyncDeleter<TResource>>(typeof(TTarget));
            AsyncAuthenticator ??= DelegateMaker.GetDelegate<AsyncAuthenticator<TResource>>(typeof(TTarget));
            AsyncCounter ??= DelegateMaker.GetDelegate<AsyncCounter<TResource>>(typeof(TTarget));
            return this;
        }

        internal DelegateSet<TResource> SetDelegatesToDefaultsWhereNull
        (
            Selector<TResource> selector = null,
            Inserter<TResource> inserter = null,
            Updater<TResource> updater = null,
            Deleter<TResource> deleter = null,
            Authenticator<TResource> authenticator = null,
            Counter<TResource> counter = null,
            Validator<TResource> validator = null,
            AsyncSelector<TResource> asyncSelector = null,
            AsyncInserter<TResource> asyncInserter = null,
            AsyncUpdater<TResource> asyncUpdater = null,
            AsyncDeleter<TResource> asyncDeleter = null,
            AsyncAuthenticator<TResource> asyncAuthenticator = null,
            AsyncCounter<TResource> asyncCounter = null
        )
        {
            SyncSelector ??= selector;
            SyncInserter ??= inserter;
            SyncUpdater ??= updater;
            SyncDeleter ??= deleter;
            SyncAuthenticator ??= authenticator;
            SyncCounter ??= counter;
            Validator ??= validator;
            AsyncSelector ??= asyncSelector;
            AsyncInserter ??= asyncInserter;
            AsyncUpdater ??= asyncUpdater;
            AsyncDeleter ??= asyncDeleter;
            AsyncAuthenticator ??= asyncAuthenticator;
            AsyncCounter ??= asyncCounter;
            return this;
        }

        internal DelegateSet<TResource> SetDelegatesToNullWhereNotImplemented()
        {
            SyncSelector = AsImplemented(SyncSelector);
            SyncInserter = AsImplemented(SyncInserter);
            SyncUpdater = AsImplemented(SyncUpdater);
            SyncDeleter = AsImplemented(SyncDeleter);
            SyncCounter = AsImplemented(SyncCounter);
            SyncAuthenticator = AsImplemented(SyncAuthenticator);
            AsyncSelector = AsImplemented(AsyncSelector);
            AsyncInserter = AsImplemented(AsyncInserter);
            AsyncUpdater = AsImplemented(AsyncUpdater);
            AsyncDeleter = AsImplemented(AsyncDeleter);
            AsyncCounter = AsImplemented(AsyncCounter);
            AsyncAuthenticator = AsImplemented(AsyncAuthenticator);
            Validator = AsImplemented(Validator);
            return this;
        }

        private static T AsImplemented<T>(T @delegate) where T : Delegate => @delegate?
            .Method
            .HasAttribute<MethodNotImplementedAttribute>() == false
            ? @delegate
            : null;
    }
}