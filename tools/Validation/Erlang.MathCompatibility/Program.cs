if (args.Length != 3 || args[0] is not (MathCompatibilityNames.Managed or MathCompatibilityNames.Beam))
{
    Console.Error.WriteLine(MathCompatibilityDiagnostics.Usage);

    return 2;
}
try
{
    return await MathCompatibilityRunner.Run(args);
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 2;
}
