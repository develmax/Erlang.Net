namespace Erlang;

internal sealed class EtsAllBinding420
{
    private readonly ModuleRegistry registry;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsAllBinding420(ProcessRuntime runtime, ModuleRegistry registry)
    {
        Runtime = runtime;
        this.registry = registry;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry)) throw new ArgumentException(EtsAllNames420.Foreign);
        if (arguments.Count != 0) throw new ArgumentException(EtsAllNames420.Foreign);
        context.Cancellation.ThrowIfCancellationRequested();

        return ValueTask.FromResult<Term>(Runtime.EtsFoundation.CoreRequestAll420(context));
    }
}
