namespace RESTable.Meta;

/// <inheritdoc />
/// <summary>
///     A non-generic interface for RESTable resource views
/// </summary>
public interface IView : ITarget
{
    /// <summary>
    ///     The resource of the view
    /// </summary>
    IEntityResource EntityResource { get; }
}