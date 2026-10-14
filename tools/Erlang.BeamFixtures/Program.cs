using BeamFixtureExport;

try
{
    await FixtureExporter.Run(args);
    if (args.Length == FixtureArgumentLayout.ArgumentsWithExecution)
    {
        await OnLoadObserverExporter.Append(args[FixtureArgumentLayout.OracleRoot], args[FixtureArgumentLayout.NewOutput]);
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);

    return 1;
}
