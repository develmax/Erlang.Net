using Erlang.NifNativeBackend;

try
{
    await NifLinuxCallbackEntry.Run(args);
}
catch (Exception failure)
{
    if (args.Length >= 3 && Directory.Exists(args[2]))
    {
        File.WriteAllText(Path.Combine(args[2], NifLinuxDiagnosticProgramNames.FailureFile), System.Text.Json.JsonSerializer.Serialize(new { Exception = failure.ToString(), DiagnosticOnly = true }));
    }
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
