using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamConditionalSourceChecks
{
    internal const string TestName = "compiler/source/otp-preprocessor-conditional-errors";
    private const string AcceptedMalformed = "BEAM preprocessing accepted a malformed or unsupported conditional.";

    internal static Task Run()
    {
        Reject("-else.", BeamMacroDiagnostics.UnexpectedConditional("else"));
        Reject("-ifdef(X).", BeamMacroDiagnostics.UnclosedConditional);
        Reject("-ifdef(X). -else. -else. -endif.", BeamMacroDiagnostics.UnexpectedConditional("else"));
        Reject("-ifdef(X). -endif(extra).", BeamMacroDiagnostics.InvalidDirective("endif"));
        Reject("-if(unsupported()). -endif.", BeamMacroDiagnostics.UnsupportedCondition);
        Reject("-define(M(A,A), A).", BeamMacroDiagnostics.InvalidDefinition);
        Reject("-define(M, 0). -define(M(A), A). f() -> ?M().", BeamMacroDiagnostics.ArgumentMismatch("M"));
        Reject("-define(M(A), A). f() -> ?M(1,).", BeamMacroDiagnostics.InvalidArguments);

        return Task.CompletedTask;
    }

    private static void Reject(string forms, string expected)
    {
        try
        {
            CodeGeneration.CompileBeamModule("-module(conditional_error). " + forms, "conditional_error.erl");
        }
        catch (CompileException error) when (error.Message == expected)
        {
            return;
        }

        throw new InvalidOperationException(AcceptedMalformed);
    }
}
