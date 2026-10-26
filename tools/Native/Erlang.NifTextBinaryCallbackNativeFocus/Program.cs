using Erlang.NifNativeBackend;

try
{
    await NifTextBinaryCallbackEntry.Run(args);
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
