namespace SecondMonitor.Contracts.Fluent
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// A hack to hide methods defined on <see cref="object"/> for IntelliSense
    /// on fluent interfaces.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFluentSyntax
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Type GetType();

        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        string ToString();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object other);
    }
}