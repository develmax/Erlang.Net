namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Materialize build facts during owner construction, before actor/R entry.
    // Sharing the immutable assembly descriptor never shares logical VM state.
    private readonly RuntimeVmBuildDescriptor vmBuildDescriptor = RuntimeVmBuildDescriptor.Current;

    internal bool TryReadVmBuildMetadata(Term query, out Term value)
    {
        if (query is Atom { Name: RuntimeVmBuildMetadataNames.Version })
            value = Term.String(vmBuildDescriptor.Version);
        else if (query is Atom { Name: RuntimeVmBuildMetadataNames.SystemArchitecture })
            value = Term.String(vmBuildDescriptor.SystemArchitecture);
        else
        {
            value = Nil.Value;

            return false;
        }

        return true;
    }
}
