namespace Erlang;

public sealed partial class ModuleRegistry
{
    private ProcessRuntime? owningRuntime;

    internal void AttachRuntime(ProcessRuntime runtime)
    {
        lock (gate)
        {
            if (owningRuntime is not null && !ReferenceEquals(owningRuntime, runtime))
                throw new ArgumentException(RuntimeExternalTermDiagnostics.ForeignRegistry, nameof(runtime));
            runtime.Functions.AttachSourceRegistry(this);
            owningRuntime = runtime;
        }
    }

    internal EtfDecodeOptions PreparationDecodeOptions()
    {
        ProcessRuntime? runtime = owningRuntime;
        if (runtime is null)
            return new();
        var options = new EtfDecodeOptions
        {
            LocalSeed = runtime.LocalTermSeed,
            ExternalFunctionFactory = (identity, safe) => runtime.Functions.External(runtime, identity, safe)
        };

        return options with { NodeAdmission = epochs => runtime.AdmitExternalDecode(options, epochs) };
    }

    private void ObserveDescriptor(ModuleCodeDescriptor descriptor)
    {
        ProcessRuntime? runtime = owningRuntime;
        if (runtime is null)
            return;
        runtime.ObserveIncomingTerm(descriptor.Compilation);
        if (descriptor.Attributes is not null)
            runtime.ObserveIncomingTerm(descriptor.Attributes);
        foreach (var record in descriptor.NativeRecords)
            foreach (var field in record.Fields)
                if (field.Value is not null)
                    runtime.ObserveIncomingTerm(field.Value);
    }
}
