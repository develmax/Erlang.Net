using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedListPredicateChecks
{
    internal const string TestName = "compiler/beam/keeps-nested-list-predicates-in-x-registers";
    private const string Filename = "beam_nested_list_predicate.erl";
    private const string Failure = "Only the proven private call domain may omit the empty-list fallback.";

    private const string Source = """
        -module(beam_nested_list_predicate).
        -export([closed/1, open/1, guarded/2]).

        closed([]) -> empty;
        closed(Data) ->
            Result = inner(Data),
            {wrapped, Result}.

        inner([[X | _] | _]) -> not is_integer(X);
        inner(Data) when is_list(Data) -> false.

        open([[X | _] | _]) -> not is_integer(X);
        open(Data) when is_list(Data) -> false.

        guarded([], Mode) when is_atom(Mode) -> empty;
        guarded(Data, _) ->
            Result = maybe_empty(Data),
            {wrapped, Result}.

        maybe_empty([[X | _] | _]) -> not is_integer(X);
        maybe_empty(Data) when is_list(Data) -> false.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Filename);
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        if (Body("inner").Contains(IsNil) || !Body("open").Contains(IsNil) || !Body("maybe_empty").Contains(IsNil))
        {
            throw new InvalidOperationException(Failure);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term nestedAtom = Cons.From([Cons.From([Term.A("character")])]);
            Term nestedInteger = new Cons(Cons.From([Term.I(7)]), Term.A("improper"));
            Term plain = Cons.From([Term.A("non_nested")]);
            Term yes = Term.A("true");
            Term no = Term.A("false");
            if (!Term.Tuple(Term.A("wrapped"), yes).Equals(await module.Invoke(context, "closed", nestedAtom))
                || !no.Equals(await module.Invoke(context, "open", nestedInteger))
                || !no.Equals(await module.Invoke(context, "open", plain))
                || !no.Equals(await module.Invoke(context, "open", Nil.Value))
                || !Term.Tuple(Term.A("wrapped"), no).Equals(await module.Invoke(
                    context,
                    "guarded",
                    Nil.Value,
                    Term.I(0)
                )))
            {
                throw new InvalidOperationException(Failure);
            }

            return Nil.Value;
        });

        byte[] Body(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var atom, _] && atoms[Value(atom)] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(Failure);
            }

            byte[] opcodes = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
            if (opcodes.Count(opcode => opcode == GetHead) != 2 || !opcodes.Contains(BifOne)
                || opcodes.Any(opcode => opcode is Allocate or AllocateHeap or ExternalCall))
            {
                throw new InvalidOperationException(Failure);
            }

            return opcodes;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
