using System.Reflection;

namespace Erlang.Tests;

internal static class BeamLoaderDependencies410Names
{
    internal const string Positive = "HOST/loader410/source-declared-dependency-inherits-and-publishes";
    internal const string Undeclared = "HOST/loader410/undeclared-pure-import-refuses";
    internal const string Host = "HOST/loader410/host-native-image-cannot-lend-declarations";
    internal const string Foreign = "HOST/loader410/foreign-registry-source-cannot-lend-declarations";
    internal const string Ended = "HOST/loader410/ended-source-lease-cannot-lend-declarations";
    internal const string Nearest = "HOST/loader410/nearest-empty-source-shadows-broader-outer";
    internal const string OuterModule = "loader_outer410";
    internal const string InnerModule = "loader_inner410";
    internal const string TargetModule = "loader_target410";
    internal const string DependencyModule = "loader_dependency410";
    internal const string Answer = "answer";
    internal const string Run = "run";
    internal const string Erlang = "erlang";
    internal const string Atoms = "AtU8";
    internal const string Prepare = "prepare_loading";
    internal const string HasOnLoad = "has_prepared_code_on_load";
    internal const string Finish = "finish_loading";
    internal const string Ok = "ok";
    internal const string Gate = "gate";
    internal const string Entries = "entries";
    internal const string Failure = "BEAM loader410 dependency provenance control failed";
    internal const int Expected = 171;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
}
