namespace Erlang;

internal static class RuntimeNativeProviderMetadataNames
{
    internal static readonly (string Function, int Arity)[] Signatures =
    [
        (BeamPreparationNames.ModuleInfo, 0),
        (BeamPreparationNames.ModuleInfo, 1)
    ];
}
