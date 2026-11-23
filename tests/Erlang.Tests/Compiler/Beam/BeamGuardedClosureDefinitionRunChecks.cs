using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedClosureDefinitionRunChecks
{
    internal const string TestName = "compiler/beam/orders-guarded-closure-definitions-across-declared-references";
    private const string Fixture = "beam_guarded_closure_definition_run_probe.erl";
    private const string WrongOrder = "The guarded factory changed its physical anonymous definition order.";
    private const string WrongResult = "The guarded factory returned the wrong closure.";

    private const string Source = """
        -module(beam_guarded_closure_definition_run_probe).
        -export([run/2]).

        run(Id, Value) -> (factory(Id))(Value).

        factory(Key) when Key =:= {1,0} -> fun(Arg) -> {first,Arg} end;
        factory(Key) when Key =:= {1,1} -> fun(Arg) -> {second,Arg} end;
        factory(Key) when Key =:= {1,2} -> fun(Arg) -> {third,Arg} end;
        factory(Key) when Key =:= {2,0} -> fun declared/1;
        factory(Key) when Key =:= {2,1} -> fun other/1;
        factory(_) -> fun(Arg) -> {fallback,Arg} end.

        declared(Arg) -> {declared,Arg}.
        other(Arg) -> {other,Arg}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        string[] definitions = Items(code, "Instructions")
            .Where(instruction => Opcode(instruction) == FunctionInfo)
            .Select(instruction => Items(instruction, "Operands").ElementAt(1))
            .Select(atom => atoms[(int)(BigInteger)Property(atom, "Value")])
            .Where(name => name.StartsWith("-factory/1-fun-", StringComparison.Ordinal))
            .ToArray();
        if (definitions is not ["-factory/1-fun-3-", "-factory/1-fun-0-", "-factory/1-fun-1-", "-factory/1-fun-2-"])
        {
            throw new InvalidOperationException(WrongOrder);
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term argument = Term.A("argument");
            foreach (var (id, tag) in new (Term, string)[]
            {
                (Key(1, 0), "first"),
                (Key(1, 1), "second"),
                (Key(1, 2), "third"),
                (Key(2, 0), "declared"),
                (Key(2, 1), "other"),
                (Key(2, 2), "fallback")
            })
            {
                Term result = await module.Invoke(
                    context,
                    "run",
                    id,
                    argument
                );
                if (!Term.Tuple(Term.A(tag), argument).Equals(result))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            return Nil.Value;
        });
    }

    private static Term Key(int group, int member) => Term.Tuple(Term.I(group), Term.I(member));

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
