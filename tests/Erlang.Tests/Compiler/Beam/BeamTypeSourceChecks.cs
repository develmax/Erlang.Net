using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamTypeSourceChecks
{
    internal const string TestName = "compiler/source/otp-type-forms";
    private const string Failure = "OTP type form fixture differed.";

    internal static Task Run()
    {
        const string source = """
            -module(typed_probe).
            -export([answer/0]).
            -export_type([result/1]).
            -record(entry, {value = 0 :: integer(), flag :: boolean()}).
            -type result(T) :: {ok, T} | error.
            -spec answer() -> result(T) when T :: integer().
            answer() -> 42.
            """;
        var module = new Parser(source).ParseModule();
        if (module.TypeForms is not [{ Name: "export_type" }, { Name: "record" }, { Name: "record" }, { Name: "type" }, { Name: "spec" }]
            || module.Attributes.Count != 0)
        {
            throw new InvalidOperationException(Failure);
        }

        try
        {
            CodeGeneration.CompileBeamModule(source, "typed_probe.erl");
        }
        catch (NotSupportedException)
        {
            return Task.CompletedTask;
        }

        throw new InvalidOperationException(Failure);
    }
}
