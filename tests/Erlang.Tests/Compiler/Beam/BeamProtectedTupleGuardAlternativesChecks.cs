using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedTupleGuardAlternativesChecks
{
    internal const string TestName = "compiler/beam/joins-two-protected-tuple-guard-values-in-source-order";
    private const string WrongLayout = "Expected two protected boolean results and the original operand order.";
    private const string WrongValue = "The protected guard matrix changed how an input is accepted or rejected.";
    private const string Source = """
        -module(beam_protected_tuple_guard_alternatives).
        -export([scalar_first/1, tuple_first/1]).

        scalar_first(Input) ->
            case fetch(Input) of
                {option, Addr, Scalar}
                  when is_map(Addr);
                       (Scalar band (bnot 63)) =:= 0,
                       tuple_size(Addr) =:= 3 andalso
                          ((element(1,Addr) bor element(2,Addr) bor element(3,Addr)) band (bnot 15)) =:= 0;
                       (Scalar band (bnot 63)) =:= 0, Addr =:= unset -> accepted;
                _ -> rejected
            end.

        tuple_first(Input) ->
            case fetch(Input) of
                {option, Addr, Scalar}
                  when tuple_size(Addr) =:= 3 andalso
                          ((element(1,Addr) bor element(2,Addr) bor element(3,Addr)) band (bnot 15)) =:= 0,
                       (Scalar band (bnot 63)) =:= 0;
                       Addr =:= unset, (Scalar band (bnot 63)) =:= 0 -> accepted;
                _ -> rejected
            end.

        fetch(Input) -> Input.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_protected_tuple_guard_alternatives.erl");
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term option = Term.A("option");
            Term valid = Term.Tuple(Term.I(1), Term.I(2), Term.I(4));
            Term wrongArity = Term.Tuple(Term.I(1), Term.I(2));
            Term badElement = Term.Tuple(Term.I(1), Term.A("bad"), Term.I(4));
            Term wideElement = Term.Tuple(Term.I(16), Term.I(2), Term.I(4));
            Term port = Term.I(19);
            Term invalid = Term.A("invalid");
            Term unset = Term.A("unset");
            Term map = new MapTerm([]);

            foreach (string function in new[] { "scalar_first", "tuple_first" })
            {
                await Check(
                    function,
                    valid,
                    port,
                    true
                );
                await Check(
                    function,
                    unset,
                    port,
                    true
                );
                await Check(
                    function,
                    wrongArity,
                    port,
                    false
                );
                await Check(
                    function,
                    badElement,
                    port,
                    false
                );
                await Check(
                    function,
                    wideElement,
                    port,
                    false
                );
                await Check(
                    function,
                    valid,
                    invalid,
                    false
                );
                await Check(
                    function,
                    unset,
                    invalid,
                    false
                );
                await Check(
                    function,
                    unset,
                    Term.I(-1),
                    false
                );
            }

            await Check(
                "scalar_first",
                map,
                invalid,
                true
            );
            await Check(
                "tuple_first",
                map,
                invalid,
                false
            );

            return Nil.Value;

            async Task Check(
                string function,
                Term address,
                Term scalar,
                bool expected
            )
            {
                Term result = await module.Invoke(context, function, Term.Tuple(option, address, scalar));
                if (!result.Equals(Term.A(expected ? "accepted" : "rejected")))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] imports = Items(structure, "Imports");
        Check("scalar_first", firstBif: "band", gcRoots: [0, 3, 3, 2, 1]);
        Check("tuple_first", firstBif: "bor", gcRoots: [2, 2, 1, 1, 2]);

        void Check(string function, string firstBif, int[] gcRoots)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[Operand(Operands(item)[1])] == function);
            if (start < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
            object[][] gc = body.Where(item => Opcode(item) == GcBifTwo).Select(Operands).ToArray();
            int joins = body.Count(item => Opcode(item) == BifTwo && Import(Operands(item)[1]) == "and");
            if (joins != 2 || gc.Length == 0 || Import(gc[0][2]) != firstBif
                || !gc.Select(row => Operand(row[1])).SequenceEqual(gcRoots))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        string Import(object operand) => atoms[(int)Property(imports[Operand(operand)], "FunctionAtomIndex")];
    }

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static object[] Operands(object value) => Items(value, "Operands");

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Operand(object value) => (int)(BigInteger)Property(value, "Value");
}
