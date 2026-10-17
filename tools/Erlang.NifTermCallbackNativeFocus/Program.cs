using Erlang.NifNativeBackend;

try
{
    await NifTermCallbackEntry.Run(args);
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
