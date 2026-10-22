using System.Text.Json;

namespace Erlang.Tests;

internal static class PrimTtyOriginal412ChildMain
{
    internal static async Task<int> Run(string[] arguments)
    {
        if (arguments.Length != 3 || arguments[0] != PrimTtyOriginal412Names.ChildSwitch
            || !Path.IsPathFullyQualified(arguments[1]) || !Path.IsPathFullyQualified(arguments[2]))
        {
            Console.Error.WriteLine(PrimTtyOriginal412Names.InvalidArguments);

            return PrimTtyOriginal412Names.ArgumentsExit;
        }
        try
        {
            await PrimTtyOriginal412Checks.Run(arguments[1], arguments[2]);

            return PrimTtyOriginal412Names.SuccessExit;
        }
        catch (Exception error)
        {
            Directory.CreateDirectory(arguments[1]);
            File.WriteAllText(
                Path.Combine(arguments[1], PrimTtyOriginal412Names.ChildFailure),
                JsonSerializer.Serialize(new { Failure = error.ToString() })
            );

            return PrimTtyOriginal412Names.FailureExit;
        }
    }
}
