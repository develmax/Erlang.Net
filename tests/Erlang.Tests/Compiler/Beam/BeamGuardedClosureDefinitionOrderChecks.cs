using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedClosureDefinitionOrderChecks
{
    internal const string TestName = "compiler/beam/keeps-source-order-for-guarded-closure-definitions";
    private const string Fixture = "beam_guarded_closure_definition_order_probe.erl";
    private const string WrongOrder = "The default or source-ordered guarded closure definitions were reordered.";
    private const string WrongChoice = "The implementation changed which closure a guarded branch calls.";
    private const string Source = """
        -module(beam_guarded_closure_definition_order_probe).
        -export([choose/1, triple/1, apply_choice/3]).
        choose(K) when K =:= {head,9} ->
            fun(X) -> {right,X} end;
        choose(K) when K =:= {head,1} ->
            fun(X) -> {left,X} end;
        choose(_) ->
            fun(X) -> {fallback,X} end.

        triple(K) when K =:= {head,4} ->
            fun(X) -> {first,X} end;
        triple(K) when K =:= {head,2} ->
            fun(X) -> {second,X} end;
        triple(K) when K =:= {head,7} ->
            fun(X) -> {third,X} end;
        triple(_) ->
            fun(X) -> {fallback,X} end.

        apply_choice(two, Key, Value) ->
            Callback = choose(Key),
            Callback(Value);
        apply_choice(three, Key, Value) ->
            Callback = triple(Key),
            Callback(Value).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        var closures = new List<string>();
        foreach (var instruction in ((IEnumerable)Property(code, "Instructions")).Cast<object>())
        {
            if ((byte)Property(Property(instruction, "Definition"), "Opcode") != FunctionInfo
                || ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray() is not [_, var atom, _])
            {
                continue;
            }
            string name = atoms[(int)(BigInteger)Property(atom, "Value")];
            if (name.StartsWith("-choose/", StringComparison.Ordinal) || name.StartsWith("-triple/", StringComparison.Ordinal))
            {
                closures.Add(name);
            }
        }

        if (closures is not
            [
                "-triple/1-fun-3-",
                "-triple/1-fun-0-",
                "-triple/1-fun-1-",
                "-triple/1-fun-2-",
                "-choose/1-fun-2-",
                "-choose/1-fun-0-",
                "-choose/1-fun-1-"
            ])
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            var cases = new (string Family, Term Key, string Tag)[]
            {
                ("two", Key(9), "right"),
                ("two", Key(1), "left"),
                ("two", Term.A("other"), "fallback"),
                ("three", Key(4), "first"),
                ("three", Key(2), "second"),
                ("three", Key(7), "third"),
                ("three", Term.Tuple(Term.A("head"), new FloatTerm(7.0)), "fallback")
            };
            foreach (var (family, key, tag) in cases)
            {
                Term data = Term.Tuple(Term.A("payload"), Term.I(12));
                Term value = await module.Invoke(
                    context,
                    "apply_choice",
                    Term.A(family),
                    key,
                    data
                );
                if (!Term.Tuple(Term.A(tag), data).Equals(value))
                {
                    throw new InvalidOperationException(WrongChoice);
                }
            }

            return Nil.Value;
        });
    }

    private static TupleTerm Key(int value) => Term.Tuple(Term.A("head"), Term.I(value));
}
