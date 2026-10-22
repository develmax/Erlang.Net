using System.Text.Json;

namespace Erlang.Tests;

internal static class StandardBootPersistent417ChildMain
{
    internal static async Task<int> Run(string[] arguments)
    {
        if (arguments.Length != 3 || arguments[0] != StandardBootPersistent417Names.ChildSwitch
            || !Path.IsPathFullyQualified(arguments[1]) || !Path.IsPathFullyQualified(arguments[2]))
        {
            Console.Error.WriteLine(StandardBootOriginal412Names.InvalidArguments);

            return StandardBootOriginal412Names.ArgumentsExit;
        }
        try
        {
            await StandardBootOriginal412Checks.Run(
                arguments[1],
                arguments[2],
                includeCollectors: true,
                includeWholePersistent: true
            );

            return StandardBootOriginal412Names.SuccessExit;
        }
        catch (Exception error)
        {
            Directory.CreateDirectory(arguments[1]);
            File.WriteAllText(
                Path.Combine(arguments[1], StandardBootOriginal412Names.FailureFile),
                JsonSerializer.Serialize(new { Failure = error.ToString() })
            );

            return StandardBootOriginal412Names.FailureExit;
        }
    }
}
