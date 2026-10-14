using NodeIdentityContrasts;
using System.Globalization;

namespace CreationBoundaryContrasts;

internal static class CreationEvaluation
{
    internal static string Run(string scenario, string output) => string.Format(
        CultureInfo.InvariantCulture,
        CreationNames.EvaluationFormat,
        CreationNames.Module,
        scenario,
        NodeErlangString.Quote(output),
        NodeErlangString.Quote(Path.Combine(output, CreationNames.ExceptionFile))
    );
}
