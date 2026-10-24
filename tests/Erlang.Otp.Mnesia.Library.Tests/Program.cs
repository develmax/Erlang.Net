using Erlang.Otp.Mnesia.Library.Tests;

try
{
    await MnesiaLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
