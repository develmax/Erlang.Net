using CreationBoundaryContrasts;

try
{
    await CreationExporter.Run(args);

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);

    return 1;
}
