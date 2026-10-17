using Erlang.NifNativeBackend;

try
{
    await NifLinuxManagedControlEntry.Run(args);
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
