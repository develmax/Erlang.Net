using Erlang.NifNativeBackend;
try
{
    await NifOrdinaryLoaderEntry.Run(args);
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
