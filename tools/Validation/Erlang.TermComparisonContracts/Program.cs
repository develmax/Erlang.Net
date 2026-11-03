using System.Globalization;
using Erlang.Tools;

if (args is not [TermComparisonProbeNames.ManagedSwitch or TermComparisonProbeNames.BeamSwitch, var shardText]
    || !int.TryParse(shardText, NumberStyles.None, CultureInfo.InvariantCulture, out int shard))
{
    Console.Error.WriteLine(TermComparisonProbeNames.Usage);

    return TermComparisonProbeNames.InvalidArgumentsExitCode;
}
try
{
    await Erlang.Tests.OrdsetsTermContractChecks.Run(shard, args[0] == TermComparisonProbeNames.BeamSwitch);
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return TermComparisonProbeNames.FailureExitCode;
}
Console.WriteLine(TermComparisonProbeNames.Completed);

return 0;
