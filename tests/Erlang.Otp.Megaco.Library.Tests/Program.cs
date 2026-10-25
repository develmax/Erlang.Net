using Erlang.Otp.Megaco.Library.Tests;

try
{
    await MegacoLibraryChecks.Run(args);
    await MegacoReceive484Checks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
