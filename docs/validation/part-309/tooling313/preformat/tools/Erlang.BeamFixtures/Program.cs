using BeamFixtureExport;

try
{
    bool metadataMutations = args.Length != 0 && args[^1] == MetadataMutationNames.Option;
    string[] fixtureArguments = metadataMutations ? args[..^1] : args;
    await FixtureExporter.Run(fixtureArguments);
    await LoadNifOpcodeFixtureExporter.Append(fixtureArguments[FixtureArgumentLayout.OracleRoot], fixtureArguments[FixtureArgumentLayout.NewOutput]);
    if (fixtureArguments.Length == FixtureArgumentLayout.ArgumentsWithExecution)
    {
        await OnLoadObserverExporter.Append(fixtureArguments[FixtureArgumentLayout.OracleRoot], fixtureArguments[FixtureArgumentLayout.NewOutput]);
    }

    if (metadataMutations)
        await MetadataMutationExporter.Append(fixtureArguments[FixtureArgumentLayout.OracleRoot], fixtureArguments[FixtureArgumentLayout.NewOutput]);

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);

    return 1;
}
