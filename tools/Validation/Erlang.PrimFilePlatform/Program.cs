try
{
    await Erlang.Tests.PrimFileDeletePlatformChecks.Mapping();
    await Erlang.Tests.PrimFileDeletePlatformChecks.Host();
    System.Console.WriteLine(Erlang.Tests.PrimFilePlatformDiagnostics.Passed);

    return 0;
}
catch (System.Exception error)
{
    System.Console.Error.WriteLine(error);

    return 1;
}
