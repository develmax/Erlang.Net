namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeEmptyTupleDomain803? emptyTuple803;
    internal RuntimeEmptyTupleDomain803 EmptyTupleDomain803 => emptyTuple803
        ?? throw new InvalidOperationException(RuntimeEmptyTuple803Names.InvalidOwner);

    internal Task CloseEmptyTuple803() => emptyTuple803?.Close803() ?? Task.CompletedTask;

    internal bool IsEmptyTuple803(Term value)
        => value is TupleTerm { OwnedStorage506: RuntimeEmptyTupleStorage803 storage }
            && ReferenceEquals(storage.Domain803, emptyTuple803) && storage.Domain803.IsCanonical803(value);
}
