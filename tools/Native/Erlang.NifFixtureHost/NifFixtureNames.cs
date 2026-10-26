namespace Erlang.NifFixtureHost;

internal static class NifFixtureNames
{
    internal const string Module = "memory_allocator_nif_fixture";
    internal const string LibraryExtension = ".dll";
    internal const string Initializer = "nif_init";
    internal const string UnloadCounter = "memory_allocator_fixture_unload_count";
    internal const string Variant = "beam.vanilla";
    internal const string Increment = "increment";
    internal const string Greeting = "greeting";
    internal const string GreetingText = "native hello";
    internal const string NonInteger = "not_an_integer";
    internal const int FunctionCount = 2;
    internal const uint IncrementArity = 1;
    internal const uint GreetingArity = 0;
    internal const int LoadMarker = 42;
    internal const int RejectedLoadMarker = 17;
    internal const int EnvironmentInputInteger = 41;
    internal const int EnvironmentConstructedInteger = 42;
    internal const int FirstPrivateData = 123;
    internal const int SecondPrivateData = 456;
    internal const int AllocationBytes = 32;
    internal const int InternalArgumentCount = 0;
    internal const int NativeArgumentCount = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal const string InternalSuccess = "NIF internal checks passed; native fixture NOT executed.";
    internal const string NativeSuccess = "NIF internal and native fixture checks passed.";
}
