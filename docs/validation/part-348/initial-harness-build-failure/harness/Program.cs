using Erlang;
using System.Reflection;

if (args.Length != 3 || !int.TryParse(args[2], out int depth) || depth < 0 || depth > TermOrderProbeNames.MaximumDepth)
    throw new ArgumentException(nameof(args));
string family = args[0], operation = args[1];
string assemblyPath = typeof(Term).Assembly.Location;
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {
    TermsAssemblyPath = assemblyPath,
    TermsAssemblySha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(assemblyPath)))
}));
Console.Out.Flush();
if (operation == TermOrderProbeNames.Shallow)
{
    TermOrderProbeChecks.Shallow();
}
else if (operation == TermOrderProbeNames.Snapshot)
{
    TermOrderProbeChecks.Snapshot();
}
else
{
    var left = TermOrderProbeChecks.Build(family, depth, 0);
    var right = TermOrderProbeChecks.Build(family, depth, 0);
    var greater = TermOrderProbeChecks.Build(family, depth, 1);
    if (ReferenceEquals(left, right))
        throw new InvalidOperationException(nameof(ReferenceEquals));
    Console.WriteLine(TermOrderProbeNames.Constructed);
    Console.Out.Flush();
    if (operation == TermOrderProbeNames.Compare)
    {
        if (!left.Equals(right) || left.CompareTo(right) != 0 || left.CompareTo(greater) >= 0 || greater.CompareTo(left) <= 0)
            throw new InvalidOperationException(TermOrderProbeNames.Compare);
    }
    else if (operation == TermOrderProbeNames.Hash)
    {
        if (left.GetHashCode() != right.GetHashCode())
            throw new InvalidOperationException(TermOrderProbeNames.Hash);
    }
    else
        throw new ArgumentException(nameof(operation));
}
Console.WriteLine(TermOrderProbeNames.Passed);
