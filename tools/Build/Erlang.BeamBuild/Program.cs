using Erlang.BeamBuild;

try
{
    if (args.Length == 2 && args[0] == BeamApplicationBuildNames.CliMode)
        await BeamApplicationOrchestrator.Run(BeamApplicationRequest.Read(args[1]), args[1]);
    else if (args.Length == 1)
        await BeamBuildOrchestrator.Run(BeamBuildRequest.Read(args[0]));
    else
        throw new ArgumentException(BeamBuildNames.Usage);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
