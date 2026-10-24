using Erlang.Otp.Asn1.Library.Tests;

try
{
    await Asn1LibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
