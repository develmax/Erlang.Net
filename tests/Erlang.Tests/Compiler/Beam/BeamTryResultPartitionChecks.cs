using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTryResultPartitionChecks
{
    internal const string TestName = "compiler/beam/partitions-a-protected-result-before-saving-it";
    private const string Module = "beam_try_result_partition";
    private const string WrongDispatch = "Expected a tuple lane and an atom lane with the original unmatched protected result.";
    private const string Source = """
        -module(beam_try_result_partition).
        -export([probe/2, guarded/2]).
        probe(Mode, Value) ->
            try produce(Mode, Value) of
                done -> ok;
                fail -> erlang:error(Value);
                retry -> again;
                Error = {error, _} -> Error
            catch
                _:Reason -> {caught, Reason}
            end.
        guarded(Mode, Value) ->
            try produce(Mode, Value) of
                done when is_integer(Value) -> ok;
                {error, _} = Error -> Error
            catch
                _:Reason -> {caught, Reason}
            end.
        produce(raise, Value) -> erlang:error(Value);
        produce(wrong, Value) -> {other, Value};
        produce(problem, Value) -> {error, Value};
        produce(Mode, _) -> Mode.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(image);
        var probe = Opcodes(code, "probe");
        int normal = Array.IndexOf(probe, TryEnd);
        int handler = Array.IndexOf(probe, TryCase);
        int mismatch = Array.IndexOf(probe, TryCaseEnd);
        var guarded = Opcodes(code, "guarded");
        int guardedEnd = Array.IndexOf(guarded, TryEnd);
        if (normal < 0 || handler < normal || mismatch <= handler
            || probe[(normal + 1)..handler] is not [IsTuple, IsTaggedTuple, ..]
            || !probe[(normal + 1)..handler].Contains(SelectValue)
            || guardedEnd < 0 || guarded[guardedEnd + 1] == IsTuple)
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("marker");
            if (!Term.A("ok").Equals(await module.Invoke(
                context,
                "probe",
                Term.A("done"),
                value
            ))
                || !Term.A("again").Equals(await module.Invoke(
                    context,
                    "probe",
                    Term.A("retry"),
                    value
                ))
                || !Term.Tuple(Term.A("error"), value).Equals(await module.Invoke(
                    context,
                    "probe",
                    Term.A("problem"),
                    value
                ))
                || !Term.Tuple(Term.A("caught"), value).Equals(await module.Invoke(
                    context,
                    "probe",
                    Term.A("raise"),
                    value
                )))
            {
                throw new InvalidOperationException(WrongDispatch);
            }

            await RequireFailure("probe", "fail", value);
            await RequireFailure("probe", "wrong", Term.Tuple(Term.A("try_clause"), Term.Tuple(Term.A("other"), value)));
            await RequireFailure("probe", "unknown", Term.Tuple(Term.A("try_clause"), Term.A("unknown")));
            await RequireFailure("guarded", "done", Term.Tuple(Term.A("try_clause"), Term.A("done")));

            return Nil.Value;

            async Task RequireFailure(string function, string mode, Term reason)
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        Term.A(mode),
                        value
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongDispatch);
            }
        });
    }

    private static byte[] Opcodes(object code, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions").Skip(start).Take(end - start)
            .Select(item => (byte)Property(Property(item, "Definition"), "Opcode"))
            .Where(opcode => opcode is not (Label or Line or FunctionInfo)).ToArray();
    }

    private static IEnumerable<object> Items(object owner, string property) => ((IEnumerable)Property(owner, property)).Cast<object>();
}
