namespace Erlang.Tests;

internal static class TermOrderHostNames
{
    internal const string ChildSwitch = "--term-order-probe";
    internal const string DotnetHost = "dotnet";
    internal const string WindowsExecutableExtension = ".exe";
    internal const string ArtifactsRoot = "artifacts";
    internal const string GuidFormat = "N";
    internal const string LoadedAssemblyPath = "TermsAssemblyPath";
    internal const string LoadedAssemblyHash = "TermsAssemblySha256";
    internal const string DeepCompareTest = "runtime/term-order/deep-compare";
    internal const string DeepHashTest = "runtime/term-order/deep-hash";
    internal const string ShallowTest = "runtime/term-order/shallow-fold-and-categories";
    internal const string SnapshotTest = "runtime/term-order/retained-node-snapshot";
    internal const string ArtifactDirectory = "term-order-children";
    internal const string StandardOutputFile = "stdout.txt";
    internal const string StandardErrorFile = "stderr.txt";
    internal const string ReceiptFile = "receipt.json";
    internal const string BrokenChild = "TermOrder child control failed; raw capture: ";
    internal const int ChildArguments = 4;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan JoinDeadline = TimeSpan.FromSeconds(10);
    internal const int MaximumDepth = 32768;
    internal const string Tuple = "tuple";
    internal const string ListHead = "list-head";
    internal const string MapValue = "map-value";
    internal const string FunctionEnvironment = "fun-environment";
    internal const string NativeRecord = "native-record";
    internal const string Compare = "compare";
    internal const string Hash = "hash";
    internal const string Shallow = "shallow";
    internal const string Snapshot = "snapshot";
    internal const string Constructed = "CONSTRUCTED";
    internal const string Passed = "PASS";
    internal const string Leaf = "leaf";
    internal const string Module = "order_probe";
    internal const string Function = "captured";
    internal const string Record = "probe";
    internal const string Field = "value";
    internal const string BeforeNode = "a@host";
    internal const string AfterNode = "b@host";
    internal const string IdentityType = "Erlang.LocalNodeIdentity";
    internal const string ContextType = "Erlang.NodeComparisonContext";
    internal const string CompareSnapshot = "CompareSnapshot";
    internal const string IdentityProperty = "Identity";
    internal const string Publish = "Publish";
}
