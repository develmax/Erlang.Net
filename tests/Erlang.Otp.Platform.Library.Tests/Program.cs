using Erlang.Otp.Platform.Library.Tests;

try
{
    await PlatformLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
