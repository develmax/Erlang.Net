using DistributionTraceObservations;

try
{
    await TraceExporter.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
