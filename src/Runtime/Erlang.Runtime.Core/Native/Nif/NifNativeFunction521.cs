namespace Erlang;

internal readonly record struct NifNativeFunction521(nint Pointer, DirtyNativeKind419 Kind)
{
    internal static DirtyNativeKind419 ReadKind(
        uint flags,
        string module,
        string function,
        uint arity
    ) => flags switch
    {
        NifNativeScheduling521Names.NormalFlag => DirtyNativeKind419.Normal,
        NifNativeScheduling521Names.CpuFlag => DirtyNativeKind419.Cpu,
        NifNativeScheduling521Names.IoFlag => DirtyNativeKind419.Io,
        _ => throw new NifNativeEntryAdmissionException(
            NifNativeDiagnostics.UntrustedFunction,
            new NifNativeLoadReason(
                NifNativeLoadReasonKind.IllegalFlags521,
                module,
                string.Empty,
                function,
                arity,
                0
            )
            { Flags521 = flags }
        )
    };
}
