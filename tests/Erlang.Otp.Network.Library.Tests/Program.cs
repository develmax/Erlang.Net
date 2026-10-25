using Erlang.Otp.Network.Library.Tests;

try
{
    await NetworkLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
