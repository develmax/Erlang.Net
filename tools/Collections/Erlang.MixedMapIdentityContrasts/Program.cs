using MixedMapIdentityContrasts;

try
{
    await MixedExporter.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
