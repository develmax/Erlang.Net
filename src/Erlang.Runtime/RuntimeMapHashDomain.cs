using System.Runtime.CompilerServices;

namespace Erlang;

internal sealed class RuntimeMapHashDomain
{
    private static readonly ConditionalWeakTable<ProcessRuntime, RuntimeMapHashDomain> domains = new();
    private readonly object gate = new();
    private readonly ConditionalWeakTable<FunctionTerm, RuntimeMapHashWord> exports = new();
    private readonly ConditionalWeakTable<FunctionTerm, RuntimeMapHashHostEntry> hosts = new();
    private ulong nextExportWord;

    internal static RuntimeMapHashDomain For(ProcessRuntime runtime) => domains.GetValue(runtime, _ => new());

    internal ulong ExportWord(ProcessRuntime runtime, ExternalFunctionIdentity identity)
    {
        // This is the runtime's existing permanent canonical export entry,
        // including unloaded functions and across module code replacement.
        FunctionTerm entry = runtime.Functions.External(runtime, identity);

        return exports.GetValue(entry, _ => AllocateExportWord()).Value;
    }

    internal RuntimeMapHashHostEntry HostEntry(FunctionTerm function) => hosts.GetValue(function,
        value => new(value.Identity, value.Arity, AllocateExportWord().Value));

    private RuntimeMapHashWord AllocateExportWord()
    {
        lock (gate)
        {
            if (nextExportWord > ulong.MaxValue - RuntimeMapHashNames.PointerAlignment)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            nextExportWord += RuntimeMapHashNames.PointerAlignment;

            return new(nextExportWord);
        }
    }
}
