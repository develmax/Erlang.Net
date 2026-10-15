namespace Erlang;

internal sealed class ModuleSourceMutationClosedException : OperationCanceledException
{
    internal ModuleRegistry Registry
    {
        get;
    }

    internal ModuleSourceMutationClosedException(ModuleRegistry registry, CancellationToken cancellation = default)
        : base(ModuleSourceMutationDiagnostics.RuntimeClosing, cancellation)
        => Registry = registry;
}
