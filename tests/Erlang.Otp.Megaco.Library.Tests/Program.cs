using Erlang.Otp.Megaco.Library.Tests;

try
{
    await MegacoLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
