internal static class DiagnosticNames
{
    internal const string TestType = "Erlang.Tests.HaltPortHostChecks";
    internal const string TestAssembly = "Erlang.Tests.dll";
    internal const string Pattern = "*.dll";
    internal const string Run = "Run";
    internal const string Held = "HeldOwnerCancellation";
    internal const string Start = "START";
    internal const string Pass = "PASS";
    internal const string Fail = "FAIL";
    internal const string Go = "RUN";
    internal const string Invalid = "Expected sealed DLL directory, exact target, fresh outcome";
    internal const int CanonicalSeconds = 15;
    internal static readonly IReadOnlyDictionary<string, (string Method, bool? Argument)> Targets =
        new Dictionary<string, (string, bool?)>(StringComparer.Ordinal)
        {
            ["ordered"] = ("OrderedDrain", null),
            ["immediate"] = ("ImmediateDiscard", null),
            ["deadline"] = ("Deadline", null),
            ["preempt"] = ("ImmediatePreemptsOrderly", null),
            ["held-orderly"] = (Held, false),
            ["held-deadline"] = (Held, true),
            [Run] = (Run, null)
        };
}
