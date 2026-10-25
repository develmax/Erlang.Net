using Erlang.Otp.Management.Library.Tests;

try
{
    await ManagementLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
