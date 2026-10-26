using Erlang.NifNativeBackend;

try
{
    await NifMapSizeCallbackEntry.Run(args);
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
