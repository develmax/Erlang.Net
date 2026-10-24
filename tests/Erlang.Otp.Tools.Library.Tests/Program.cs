using Erlang.Otp.Tools.Library.Tests;

try
{
    await ToolsLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
