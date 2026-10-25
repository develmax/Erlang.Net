using Erlang.NifNativeBackend;

try
{
    await NifLinuxCallbackEntry.Run(args);
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
