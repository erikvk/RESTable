using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Resources.Operations;

internal class DelegateSet<TResource> : IEntityResourceOperationDefinition<TResource> where TResource : class
{
    private Selector<TResource>? SyncSelector { get; set; }
    private Inserter<TResource>? SyncInserter { get; set; }
    private Updater<TResource>? SyncUpdater { get; set; }
    private Deleter<TResource>? SyncDeleter { get; set; }
    private Authenticator<TResource>? SyncAuthenticator { get; set; }
    private Counter<TResource>? SyncCounter { get; set; }

    // All non-async operations are transformed into async delegates on resolve

    private AsyncSelector<TResource>? AsyncSelector { get; set; }
    private AsyncInserter<TResource>? AsyncInserter { get; set; }
    private AsyncUpdater<TResource>? AsyncUpdater { get; set; }
    private AsyncDeleter<TResource>? AsyncDeleter { get; set; }
    private AsyncAuthenticator<TResource>? AsyncAuthenticator { get; set; }
    private AsyncCounter<TResource>? AsyncCounter { get; set; }

    private Validator<TResource>? Validator { get; set; }

    public bool RequiresAuthentication => AsyncAuthenticator is not null;
    public bool CanSelect => AsyncSelector is not null;
    public bool CanInsert => AsyncInserter is not null;
    public bool CanUpdate => AsyncUpdater is not null;
    public bool CanDelete => AsyncDeleter is not null;
    public bool CanCount => AsyncCounter is not null;

    public IAsyncEnumerable<TResource> SelectAsync(IRequest<TResource> request, CancellationToken cancellationToken)
    {
        return AsyncSelector!(request, cancellationToken);
    }

    public IAsyncEnumerable<TResource> InsertAsync(IRequest<TResource> request, CancellationToken cancellationToken)
    {
        return AsyncInserter!(request, cancellationToken);
    }

    public IAsyncEnumerable<TResource> UpdateAsync(IRequest<TResource> request, CancellationToken cancellationToken)
    {
        return AsyncUpdater!(request, cancellationToken);
    }

    public ValueTask<long> DeleteAsync(IRequest<TResource> request, CancellationToken cancellationToken)
    {
        return AsyncDeleter!(request, cancellationToken);
    }

    public ValueTask<AuthResults> AuthenticateAsync(IRequest<TResource> request, CancellationToken cancellationToken)
    {
        return AsyncAuthenticator!(request, cancellationToken);
    }

    public ValueTask<long> CountAsync(IRequest<TResource> request, CancellationToken cancellationToken)
    {
        return AsyncCounter!(request, cancellationToken);
    }

    public async IAsyncEnumerable<TResource> Validate(IAsyncEnumerable<TResource>? entities, RESTableContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (entities is null) yield break;
        if (Validator is null)
        {
            await foreach (var entity in entities.ConfigureAwait(false))
                yield return entity;
            yield break;
        }

        var index = -1L;

        await foreach (var entity in entities.ConfigureAwait(false))
        {
            index += 1;
            var invalidMembers = Validator(entity, context).ToList();
            if (invalidMembers.Count > 0)
                throw new InvalidInputEntity(new InvalidEntity(index, invalidMembers));
            yield return entity;
        }
    }

#pragma warning disable 1998

    private static async IAsyncEnumerable<TResource> CallAsync(IEnumerable<TResource>? entities)
    {
        if (entities is null) yield break;
        foreach (var item in entities) yield return item;
    }

#pragma warning restore 1998

    /// <summary>
    ///     Transforms synchronous delegates to async where null
    /// </summary>
    internal DelegateSet<TResource> SetAsyncDelegatesToSyncWhereNull()
    {
        if (AsyncSelector is null && SyncSelector is Selector<TResource> selector)
            AsyncSelector = (request, _) => CallAsync(selector(request));
        if (AsyncInserter is null && SyncInserter is Inserter<TResource> inserter)
            AsyncInserter = (request, _) => inserter(request).ToAsyncEnumerable();
        if (AsyncUpdater is null && SyncUpdater is Updater<TResource> updater)
            AsyncUpdater = (request, _) => updater(request).ToAsyncEnumerable();
        if (AsyncDeleter is null && SyncDeleter is Deleter<TResource> deleter)
            AsyncDeleter = (request, _) => new ValueTask<long>(deleter(request));
        if (AsyncAuthenticator is null && SyncAuthenticator is Authenticator<TResource> authenticator)
            AsyncAuthenticator = (request, _) => new ValueTask<AuthResults>(authenticator(request));
        if (AsyncCounter is null && SyncCounter is Counter<TResource> counter)
            AsyncCounter = (request, _) => new ValueTask<long>(counter(request));
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
        Selector<TResource>? selector = null,
        Inserter<TResource>? inserter = null,
        Updater<TResource>? updater = null,
        Deleter<TResource>? deleter = null,
        Authenticator<TResource>? authenticator = null,
        Counter<TResource>? counter = null,
        Validator<TResource>? validator = null,
        AsyncSelector<TResource>? asyncSelector = null,
        AsyncInserter<TResource>? asyncInserter = null,
        AsyncUpdater<TResource>? asyncUpdater = null,
        AsyncDeleter<TResource>? asyncDeleter = null,
        AsyncAuthenticator<TResource>? asyncAuthenticator = null,
        AsyncCounter<TResource>? asyncCounter = null
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

    private static T? AsImplemented<T>(T? @delegate) where T : Delegate
    {
        return @delegate?.Method.IsImplemented() == true ? @delegate : null;
    }
}