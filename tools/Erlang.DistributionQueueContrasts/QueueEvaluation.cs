using NodeIdentityContrasts;
using System.Globalization;

namespace DistributionQueueContrasts;

internal static class QueueEvaluation
{
    internal static string Run(string scenario, string output) => string.Format(
        CultureInfo.InvariantCulture,
        QueueNames.EvaluationFormat,
        QueueNames.ScenarioModules[scenario],
        scenario,
        NodeErlangString.Quote(output),
        NodeErlangString.Quote(Path.Combine(output, QueueNames.ExceptionFile))
    );
}
