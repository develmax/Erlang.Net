using DistributionQueueContrasts;

try
{
    await QueueExporter.Run(args);

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);

    return 1;
}
