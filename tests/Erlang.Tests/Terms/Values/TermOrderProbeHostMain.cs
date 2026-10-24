using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class TermOrderProbeHostMain
{
    internal static int Run(IReadOnlyList<string> arguments)
    {
        try
        {
            if (arguments.Count != TermOrderHostNames.ChildArguments ||
                !int.TryParse(arguments[3], out int depth) || depth < 0 || depth > TermOrderHostNames.MaximumDepth)
                throw new ArgumentException(nameof(arguments));
            string family = arguments[1], operation = arguments[2];
            string path = typeof(Term).Assembly.Location;
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                TermsAssemblyPath = path,
                TermsAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
            }));
            Console.Out.Flush();
            if (operation == TermOrderHostNames.Shallow)
                TermOrderHostCases.Shallow();
            else if (operation == TermOrderHostNames.Snapshot)
                TermOrderHostCases.Snapshot();
            else
            {
                var left = TermOrderHostCases.Build(family, depth, 0);
                var right = TermOrderHostCases.Build(family, depth, 0);
                var greater = TermOrderHostCases.Build(family, depth, 1);
                if (ReferenceEquals(left, right))
                    throw new InvalidOperationException(nameof(ReferenceEquals));
                Console.WriteLine(TermOrderHostNames.Constructed);
                Console.Out.Flush();
                if (operation == TermOrderHostNames.Compare)
                {
                    if (!left.Equals(right) || !left.NumericEquals(right) || left.CompareTo(right) != 0 ||
                        left.CompareTo(greater) >= 0 || greater.CompareTo(left) <= 0)
                        throw new InvalidOperationException(TermOrderHostNames.Compare);
                }
                else if (operation == TermOrderHostNames.Hash)
                {
                    if (left.GetHashCode() != right.GetHashCode())
                        throw new InvalidOperationException(TermOrderHostNames.Hash);
                }
                else
                    throw new ArgumentException(nameof(operation));
            }
            Console.WriteLine(TermOrderHostNames.Passed);

            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);

            return 1;
        }
    }
}
