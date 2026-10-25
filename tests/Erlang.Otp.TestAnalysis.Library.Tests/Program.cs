using Erlang.Otp.TestAnalysis.Library.Tests;

try
{
    await TestAnalysisLibraryChecks.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
