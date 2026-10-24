using System.Text.Json;

namespace Erlang.Tests;

internal static class CollectorHibernate415ChildMain
{
    internal static async Task<int> Run(string[] arguments)
    {
        if (arguments.Length != 2 || arguments[0] != CollectorHibernate415Names.ChildSwitch
            || !Path.IsPathFullyQualified(arguments[1]))
        {
            Console.Error.WriteLine(CollectorHibernate415Names.InvalidArguments);

            return CollectorHibernate415Names.ArgumentsExit;
        }
        try
        {
            await CollectorHibernate415Checks.Run(arguments[1]);

            return CollectorHibernate415Names.SuccessExit;
        }
        catch (Exception error)
        {
            Directory.CreateDirectory(arguments[1]);
            File.WriteAllText(
                Path.Combine(arguments[1], CollectorHibernate415Names.FailureFile),
                JsonSerializer.Serialize(new { Failure = error.ToString() })
            );

            return CollectorHibernate415Names.FailureExit;
        }
    }
}
