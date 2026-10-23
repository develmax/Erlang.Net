namespace Erlang;

internal static class BeamOnLoadDiagnostics
{
    internal const string ExitedBeforeBody = "The callback owner terminated before entering the pending body";
    internal const string InvalidState = "The pending BEAM image is not in the required state";
    internal const string ForeignRuntime = "The callback runtime does not own this pending image";
    internal const string BusyModule = "The module already has a pending BEAM callback";
    internal const string OnLoadRequired = "The BEAM image has no on_load callback";
    internal const string RequiresStaging = "An on_load image requires private callback staging";
    internal const string OldGeneration = "The module has current and old code";
    internal const string Frame = "The BEAM frame or Y-register ownership is unsupported";
    internal const string UnknownImport = "The BEAM external import is outside the supported host subset";
    internal const string GenerationChanged = "The current module generation changed while the callback was pending";
    internal const string CallbackNotTerminated = "The pending callback owner has not terminated";
    internal const string Metadata = "The on_load image metadata is outside the independently compiled subset";
}
