using Erlang.Otp.Gui.Library.Tests;

try
{
    await GuiLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
