using System;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Meta;

/// <inheritdoc />
/// <summary>
///     Describes a property of a resource
/// </summary>
public abstract class Property : Member
{
    protected Property(Type? owner) : base(owner) { }

    /// <summary>
    ///     Is this property a dynamic member?
    /// </summary>
    public abstract bool IsDynamic { get; }

    /// <summary>
    ///     Is this property a declared member?
    /// </summary>
    public bool IsDeclared => !IsDynamic;

    /// <summary>
    ///     The allowed condition operators for this property
    /// </summary>
    public Operators AllowedConditionOperators { get; protected set; } = Operators.All;

    /// <summary>
    /// </summary>
    internal Setter? Setter { get; set; }

    /// <summary>
    /// </summary>
    internal Getter? Getter { get; set; }

    /// <summary>
    ///     Is this property marked as read-only?
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    public override bool IsReadable => Getter is not null;

    /// <inheritdoc />
    public override bool IsWritable => !ReadOnly && Setter is not null;

    /// <summary>
    ///     Gets the value of this property, for a given target object
    /// </summary>
    public virtual async ValueTask<object?> GetValue(object target)
    {
        if (Getter is null)
            return default;
        var value = await Getter.Invoke(target).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    ///     Sets the value of this property, for a given target object and a given value
    /// </summary>
    public virtual async ValueTask SetValue(object target, object? value)
    {
        if (Setter is null)
            return;
        await Setter.Invoke(target, value).ConfigureAwait(false);
    }

    /// <summary>
    ///     Sets the value of this property, expecting it to be a synchronous operation, and
    ///     blocking the thread until done if it isn't.
    /// </summary>
    public void SetValueOrBlock(object target, object? value)
    {
        var setValueTask = SetValue(target, value);
        if (setValueTask.IsCompleted)
            setValueTask.GetAwaiter().GetResult();
        else setValueTask.AsTask().Wait();
    }
}
