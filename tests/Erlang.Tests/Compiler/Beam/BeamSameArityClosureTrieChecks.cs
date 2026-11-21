using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSameArityClosureTrieChecks
{
    internal const string TestName = "compiler/beam/selects-single-arity-closures-and-checks-decoded-size-in-registers";
    private const string Fixture = "beam_same_arity_closure_trie_probe.erl";
    private const string IncorrectInstructions = "The single-arity factory or measured closure emitted the wrong register layout.";
    private const string IncorrectValue = "The single-arity factory selected the wrong closure or decoded value.";

    private const string Source = """
        -module(beam_same_arity_closure_trie_probe).
        -export([run/3]).

        run(Key, Mode, Input) -> (factory(Key))(Mode, Input, []).

        factory({1,2,7}) ->
            fun(Kind, Data, _) ->
                case Kind of payload ->
                    begin
                        Bytes = decode(Data, [1]),
                        Size = byte_size(Bytes),
                        if Size =:= 2 -> Bytes;
                           true -> exit({error,{asn1,bad_range}})
                        end
                    end
                end
            end;
        factory({1,2,9}) -> fun(_, _, _) -> next end;
        factory({1,3,5}) -> fun(_, _, _) -> third end;
        factory(_) -> fun(_, _, _) -> absent end.
        decode(Data, _) -> Data.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] factory = Read("factory");
        object[] checkedClosure = Read("-factory/1-fun-0-");
        if (factory.Any(instruction => Opcode(instruction) == Allocate)
            || factory.Count(instruction => Opcode(instruction) == SelectValue) != 2
            || checkedClosure.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray() is not [0]
            || checkedClosure.Count(instruction => Opcode(instruction) == GcBifOne) != 1)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term payload = new BitString([1, 2]);
            Term mode = Term.A("payload");
            foreach (var (key, expected) in new (Term, Term)[]
            {
                (Key(2, 7), payload),
                (Key(2, 9), Term.A("next")),
                (Key(3, 5), Term.A("third")),
                (Key(2, 5), Term.A("absent")),
                (Term.A("unmatched"), Term.A("absent"))
            })
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    "run",
                    key,
                    mode,
                    payload
                )))
                {
                    throw new InvalidOperationException(IncorrectValue);
                }
            }

            return Nil.Value;
        });

        object[] Read(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );

            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static Term Key(int area, int discriminator) => Term.Tuple(Term.I(1), Term.I(area), Term.I(discriminator));

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
