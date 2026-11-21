using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamIfFailureLocationChecks
{
    internal const string TestName = "compiler/beam/if-clause-uses-the-source-if-location";
    private const string File = "beam_if_exhaustion.erl";
    private const string MappedFile = "logical_if_probe.erl";
    private const string WrongResult = "An if result or its failure location differed from the source if.";
    private const string EffectError = "A discarded if changed the guarded call or kept an unused branch result.";
    private const string IfClause = "if_clause";
    private const string Source = """
        -module(beam_if_exhaustion).
        -export([select/1, exhaustive/1, caught/1, following/1, explicit/0, remapped/1, discard/2, use/2, effect/1]).

        select(Value) ->
            if
                Value =:= first -> matched
            end.

        exhaustive(Value) ->
            if Value =:= first -> matched; true -> fallback end.

        caught(Value) ->
            try
                Result = if
                    Value =:= first -> matched
                end,
                {ok, Result}
            catch error:if_clause -> handled
            end.

        following(Value) ->
            Result = if
                Value =:= first -> matched
            end,
            {wrapped, Result}.

        explicit() ->
            erlang:error(if_clause).

        -file("logical_if_probe.erl", 70).
        remapped(Value) ->
            if
                Value =:= first -> matched
            end.

        discard(Flag, Value) ->
            if Flag =:= enabled -> ?MODULE:effect(Value); true -> fallback end,
            {finished, Flag}.

        use(Flag, Value) ->
            if Flag =:= enabled -> ?MODULE:effect(Value); true -> fallback end.

        effect(fail) -> erlang:error(effect_failure);
        effect(Value) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, File);
        VerifyEffectContinuation(image);
        var module = BeamModule.Prepare(image, new BeamDependencySelection(["beam_if_exhaustion"]));
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            module.Name,
            "effect",
            1,
            (context, arguments) => module.Invoke(context, "effect", arguments[0])
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term wrong = Term.A("wrong");
            Term first = Term.A("first");
            Term matched = Term.A("matched");
            await CheckFailure(
                "select",
                File,
                5,
                [wrong]
            );
            await CheckFailure(
                "following",
                File,
                22,
                [wrong]
            );
            await CheckFailure(
                "remapped",
                MappedFile,
                72,
                [wrong]
            );
            await CheckFailure(
                "explicit",
                File,
                28,
                []
            );

            if (!(await module.Invoke(context, "select", first)).Equals(matched)
                || !(await module.Invoke(context, "following", first)).Equals(Term.Tuple(Term.A("wrapped"), matched))
                || !(await module.Invoke(context, "caught", first)).Equals(Term.Tuple(Term.A("ok"), matched))
                || !(await module.Invoke(context, "caught", wrong)).Equals(Term.A("handled"))
                || !(await module.Invoke(context, "exhaustive", wrong)).Equals(Term.A("fallback")))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term disabled = Term.A("disabled");
            Term enabled = Term.A("enabled");
            Term fail = Term.A("fail");
            if (!Term.Tuple(Term.A("finished"), disabled).Equals(await module.Invoke(
                context,
                "discard",
                disabled,
                fail
            )) || !Term.Tuple(Term.A("finished"), enabled).Equals(await module.Invoke(
                context,
                "discard",
                enabled,
                first
            )) || !Term.A("fallback").Equals(await module.Invoke(
                context,
                "use",
                disabled,
                fail
            )))
            {
                throw new InvalidOperationException(EffectError);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "discard",
                    enabled,
                    fail
                );
                throw new InvalidOperationException(EffectError);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("effect_failure")))
            {
            }

            return Nil.Value;

            async Task CheckFailure(
                string function,
                string file,
                int line,
                Term[] arguments
            )
            {
                try
                {
                    _ = await module.Invoke(context, function, arguments);
                }
                catch (ErlangException error)
                {
                    if (!error.Reason.Equals(Term.A(IfClause))
                        || error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, Atom name, _, var position] } }
                        || name.Name != function
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("file"), Term.String(file)))
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
                    {
                        throw new InvalidOperationException(WrongResult, error);
                    }

                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void VerifyEffectContinuation(byte[] image)
    {
        object code = Code(image);
        var atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string?>().ToArray();
        var operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>();
        var body = operations.SkipWhile(operation => GetOpcode(operation) != FunctionInfo
            || atoms[(int)(BigInteger)Property(((IEnumerable)Property(operation, "Operands")).Cast<object>().ElementAt(1), "Value")]
                != "discard").Skip(1).TakeWhile(operation => GetOpcode(operation) != FunctionInfo).ToArray();
        if (!body.Any(operation => GetOpcode(operation) == ExternalCall)
            || body.Any(operation => GetOpcode(operation) == Jump))
        {
            throw new InvalidOperationException(EffectError);
        }

        static byte GetOpcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
    }
}
