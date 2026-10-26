using DistributionSenderObservations;

try
{
    await SenderExporter.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
