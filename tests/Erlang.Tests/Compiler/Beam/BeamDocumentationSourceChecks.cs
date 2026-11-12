using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamDocumentationSourceChecks
{
    internal const string TestName = "compiler/source/otp-documentation-forms";
    private const string Failure = "OTP documentation source fixture differed.";

    internal static Task Run()
    {
        // String values captured with erl_scan:string on OTP 29.1.1.
        const string triple = "\"\"\"\n  a\\n\n\n  b\n  \"\"\"";
        const string fourQuotes = "\"\"\"\"\r\n  \"\"\"\r\n  x\r\n  \"\"\"\"";
        Require(Lexer.Scan(triple)[0].Text == "a\\n\n\nb");
        Require(Lexer.Scan(fourQuotes)[0].Text == "\"\"\"\r\nx");
        string source = "-module(documentation). -moduledoc false. -doc " + triple + ". f() -> ok.";
        var module = new Parser(source).ParseModule();
        Require(module.Attributes.Count == 0);
        Require(module.Documentation.Count == 2);
        Require(module.Documentation[0].Value.Equals(Term.A("false")));
        Require(module.Documentation[1].Value.Equals(Term.String("a\\n\n\nb")));
        var metadata = new Parser("-module(doc_meta). -moduledoc(#{since => ~\"OTP 29\"}). -doc(#{equiv => f(X)}). f() -> ok.").ParseModule();
        Require(metadata.Documentation[0].Value is MapTerm);
        Require(metadata.Documentation[1].EquivalentCall is { Function: "f", Arguments: [Expr.Variable { Name: "X" }] });
        try
        {
            CodeGeneration.CompileBeamModule("-module(documentation). -moduledoc(#{since => \"29\"}).", "documentation.erl");
        }
        catch (NotSupportedException)
        {
            return Task.CompletedTask;
        }

        throw new InvalidOperationException(Failure);
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(Failure);
        }
    }
}
