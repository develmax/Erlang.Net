using Erlang.Otp.Security.Library.Tests;

try
{
    await SecurityLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
