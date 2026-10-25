using Erlang.Otp.Transport.Library.Tests;

try
{
    await TransportLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
