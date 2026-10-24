using Erlang.Otp.Boot.Library.Tests;

try
{
    await BootLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
