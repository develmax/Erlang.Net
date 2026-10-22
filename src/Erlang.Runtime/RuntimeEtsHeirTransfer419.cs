namespace Erlang;

internal sealed class RuntimeEtsHeirTransfer419(
    RuntimeEtsFoundationTable table,
    RuntimeEtsOwnerTables group,
    RuntimeEtsHeir419 heir,
    RuntimeEtsReadLease? read
) : IDisposable
{
    internal RuntimeEtsFoundationTable Table { get; } = table;
    internal RuntimeEtsOwnerTables Group { get; } = group;
    internal RuntimeEtsHeir419 Heir { get; } = heir;
    internal RuntimeEtsReadLease? Read { get; } = read;

    public void Dispose() => Read?.Dispose();
}
