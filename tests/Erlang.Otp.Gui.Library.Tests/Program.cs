using Erlang.Otp.Gui.Library.Tests;

try
{
    await GuiLibraryChecks.Run(args);
    await GuiComparison482Checks.Run(args);
    await GuiReceive483Checks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
