using NodeIdentityContrasts;
using System.Globalization;

namespace AdmissionOracle;

internal static class AdmissionEvaluation
{
    internal static string Run(string module, string output) => string.Format(
        CultureInfo.InvariantCulture,
        AdmissionNames.EvaluationFormat,
        module,
        NodeErlangString.Quote(output),
        NodeErlangString.Quote(Path.Combine(output, AdmissionNames.ExceptionFile))
    );
}
