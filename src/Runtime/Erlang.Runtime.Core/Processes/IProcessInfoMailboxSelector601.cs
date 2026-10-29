namespace Erlang;

// Only the sealed private ProcessContext system-reply matcher implements this.
internal interface IProcessInfoMailboxSelector601<T> where T : class
{
    ProcessContext Context { get; }

    ValueTask<T?> Select(Term value);
}
