namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal RuntimeNativeMetadataImage? NativeMetadataImage527 { get; }

    internal Term NativeLoadModuleErrorLocation527
        => NativeMetadataImage527?.LoadModuleErrorLocation527 ?? PortErrorSourceLocations.ErrorWithInfo;
}
