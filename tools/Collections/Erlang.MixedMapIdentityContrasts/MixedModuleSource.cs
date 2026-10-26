using NodeIdentityContrasts;

namespace MixedMapIdentityContrasts;

internal static class MixedModuleSource
{
    internal static string Evaluate(string directory) =>
        MixedNames.Module + ":run(" + NodeErlangString.Quote(directory) + " ).";
}
