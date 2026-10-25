using Erlang.NifNativeBackend;

try
{
    await NifMakeReferenceCallbackEntry.Run(args);
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
