namespace Erlang;

// Genuine runtime alias operation. No caller supplied action is accepted.
internal sealed class PendingAliasAdmission817
{
    internal ProcessRuntime Runtime { get; }
    internal ProcessContext Owner { get; }
    internal ReferenceTerm Reference { get; }
    internal bool Priority { get; }
    internal Func<bool?> Callback { get; }

    internal PendingAliasAdmission817(
        ProcessRuntime runtime,
        ProcessContext owner,
        ReferenceTerm reference,
        bool priority
    )
    {
        Runtime = runtime;
        Owner = owner;
        Reference = reference;
        Priority = priority;
        Callback = Invoke;
    }

    private bool? Invoke() => Runtime.AdmitAlias817(this);
}
