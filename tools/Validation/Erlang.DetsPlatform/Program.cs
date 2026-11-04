using Erlang;
try
{
    string repository = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
    string output = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(repository, "artifacts/dets-portable-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(output);
    await Erlang.Tests.DetsReviewRegressionChecks.Run();
    File.WriteAllText(Path.Combine(output, "review-regressions.json"), "{\"Passed\":true,\"Scenario\":\"growth-and-fixed-collection\"}\n");
    await DetsPlatformRunner.Run(repository, output, true);
    await DetsPlatformRunner.Run(repository, output, false);
    DetsPlatformInterchange.Verify(output);
    Console.WriteLine("PASS managed and original DETS platform contracts");

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    if (error is ErlangException guest) Console.Error.WriteLine(guest.StackTraceTerm);

    return 1;
}
