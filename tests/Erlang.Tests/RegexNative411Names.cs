using System.Text;

namespace Erlang.Tests;

internal static class RegexNative411Names
{
    internal const string NativeResultsTest = "runtime/re412/native-results-and-errors";
    internal const string OwnershipTest = "runtime/re412/ownership-and-installation-rollback";
    internal const string UnicodeTest = "runtime/re412/large-unicode-native-resumption";
    internal const string ConcurrentTest = "runtime/re412/concurrent-runs-and-held-capability";
    internal const string CancellationTest = "runtime/re412/active-actor-cancellation";
    internal const string DisposeTest = "runtime/re412/dispose-close";
    internal const string HaltTest = "runtime/re412/halt-close";
    internal const string InitializerTest = "runtime/re412/initializer-close";
    internal const string MatchAtom = "match";
    internal const string PrefixPattern = "^[a-z]+";
    internal const string PrefixSubject = "abc12";
    internal const string NonMatchSubject = "123";
    internal const string InvalidPattern = "[";
    internal const string ErrorAtom = "error";
    internal const string ExpectedVersion = "10.47 2025-10-21";
    internal const string InvalidAtom = "invalid";
    internal const string SingleValue = "x";
    internal const string OffsetSubject = "ax";
    internal const string RepeatedSubject = "xxx";
    internal const string BacktrackPattern = "(a|aa)+$";
    internal const string BacktrackSuffix = "b";
    internal const string UnicodeAllPattern = "^.*$";
    internal const string BadOption = "not_a_re_option";
    internal const string BeamImage = "beam.smp.dll";
    internal const string Module = "re";
    internal const string Compile = "compile";
    internal const string Run = "run";
    internal const string Version = "version";
    internal const string AdmissionRelative = "artifacts/library376-official-compiler/official-admission.json";
    internal const string Compiled = "re_pattern";
    internal const string Ok = "ok";
    internal const string NoMatch = "nomatch";
    internal const string Unicode = "unicode";
    internal const string Killed = "killed";
    internal const string Kill = "kill";
    internal const string Caseless = "caseless";
    internal const string Failed = "Regex411 real native HOST control failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);

    internal static BitString Bytes(string text) => new(Encoding.UTF8.GetBytes(text));
}
