using System.Text.Json;
namespace Erlang.Tests;

internal static class AtomicsOriginal413ChildMain
{
    internal static async Task<int> Run(string[] arguments)
    {
        if (arguments.Length != 2 || arguments[0] != AtomicsOriginal413Names.ChildSwitch || !Path.IsPathFullyQualified(arguments[1]))
        {
            Console.Error.WriteLine(AtomicsOriginal413Names.InvalidArguments);

            return 2;
        }
        try
        {
            await AtomicsOriginal413Checks.Run(arguments[1]);

            return 0;
        }
        catch (Exception error)
        {
            Directory.CreateDirectory(arguments[1]);
            File.WriteAllText(Path.Combine(arguments[1], AtomicsOriginal413Names.FailureFile), JsonSerializer.Serialize(new { Failure = error.ToString() }));

            return 1;
        }
    }
}
