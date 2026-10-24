using Erlang.Otp.Documentation.Library.Tests;

try
{
    await DocumentationLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
