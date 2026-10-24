using Erlang.Otp.Syntax.Library.Tests;

try
{
    await SyntaxLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
