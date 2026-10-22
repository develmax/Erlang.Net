namespace Erlang.Tests;

internal static class TestRunNames
{
    internal const string OptionPrefix = "--";
    internal const int InvalidArgumentsExitCode = 2;
    internal const string List = "--list";
    internal const string Block = "--block";
    internal const string Case = "--case";
    internal const string Report = "--report";
    internal const string Registry = "--registry";
    internal const string Terms = "terms";
    internal const string Compiler = "compiler";
    internal const string Processes = "processes";
    internal const string PortsDistribution = "ports-distribution";
    internal const string Otp = "otp";
    internal const string Native = "native";
    internal const string Tooling = "tooling";
    internal const string UnknownOption = "Unknown test runner option: ";
    internal const string MissingValue = "Missing test runner option value: ";
    internal const string DuplicateOption = "Duplicate test runner option: ";
    internal const string UnknownBlock = "Unknown regression block: ";
    internal const string UnknownCase = "Unknown exact test case: ";
    internal const string EmptySelection = "Regression selection must contain at least one test.";
    internal const string InvalidCatalog = "Test names must be nonempty and unique.";
    internal const string MixedArguments = "Named test options cannot be mixed with legacy positional report arguments.";
    internal const string PartialRegistry = "Partial or listing runs cannot write an API registry or readiness evidence.";
    internal const string PartialReport = "Partial and listing reports cannot overwrite canonical test, integration, registry or readiness evidence.";
    internal const string CompiledPrefix = "oracle/compiled-module/";
    internal const string CompilerPrefix = "compiler/";
    internal const string DocsSegment = "docs";
    internal const string ReadinessSegment = "readiness";
    internal const string ToolReadinessSegment = "tool-readiness";
    internal static readonly string[] CanonicalReportFiles =
    [
        "tests.json",
        "integration-results.json",
        "supported-mfas.json",
        "module-readiness.json",
        "MODULE_READINESS.md",
        "API_COMPATIBILITY.md"
    ];

    internal static readonly string[] Blocks = [Terms, Compiler, Processes, PortsDistribution, Otp, Native, Tooling];
    internal static readonly string[] NativeMarkers = ["nif", "native/"];
    internal static readonly string[] PortsMarkers = ["distribution", "connected", "socket", "epmd", "cookie", "open-port", "ports/", "runtime/port", "halt-port", ":port_", ":open_port/", ":ports/"];
    internal static readonly string[] OtpMarkers = ["queue/core/", "queue/higher-", "queue-ends-", "runtime/queue/", "mfa/queue:", "lists-", "lists_", "maps-", "maps_", "proplists-", "otp/", "proc-lib", "proc_lib", "gen-server", "gen_server", "supervisor", "mfa/lists:", "mfa/maps:", "mfa/proplists:", "logger", "error-logger"];
    internal static readonly string[] CompilerMarkers = ["compiler", "compile", "parser", "lexer", "beam", "source-", "source/", "binding", "code-loading", "module-"];
    internal static readonly string[] ProcessMarkers = ["process", "spawn", "mailbox", "receive", "suspend", "resume", "monitor", "trace", "scheduler", "timer", "mfa/erlang:self/", "mfa/erlang:exit/"];
    internal static readonly string[] TermMarkers = ["erlang-text", "arithmetic", "bits-", "list-", "map-", "text-", "term-", "term/", "runtime/term", "binary", "bitstring", "tuple", "integer", "float", "atom", "unicode", "length", "mfa/erlang:is_", "mfa/erlang:map_"];
}
