using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedBooleanListResultChecks
{
    internal const string TestName = "compiler/beam/keeps-call-result-in-x-for-closed-boolean-list-return";
    private const string Fixture = "beam_closed_boolean_list_result_probe.erl";
    private const string WrongLayout = "The closed Boolean return lost its call-result root or retained an unnecessary frame slot.";
    private const string WrongValue = "The Boolean return changed the list, the original flag, or the preceding call effect.";

    private const string Source = """
        -module(beam_closed_boolean_list_result_probe).
        -export([start/2, open/4]).

        start(Forms, Name) -> rewrite(Forms, [], Name, true).
        rewrite([{form,_,skip,0,_}=F | Fs], As, Name, _Flag) ->
            rewrite(Fs, [F | As], Name, false);
        rewrite([F | Fs], As, Name, Flag) ->
            rewrite(Fs, [F | As], Name, Flag);
        rewrite([], As, Name, Flag) ->
            L = stamp(0),
            {if Flag ->
                [{form,L,check,0,
                    [{clause,L,[],[],
                        [{call,L,{remote,L,{atom,L,checks},{atom,L,check}},
                            [{atom,L,Name}]}]}]},
                 {attribute,L,doc,false} | As];
                true -> As
             end, Flag}.

        open([], As, Name, Flag) ->
            L = stamp(0),
            {if Flag ->
                [{form,L,check,0,
                    [{clause,L,[],[],
                        [{call,L,{remote,L,{atom,L,checks},{atom,L,check}},
                            [{atom,L,Name}]}]}]},
                 {attribute,L,doc,false} | As];
                true -> As
             end, Flag}.

        stamp(Value) -> erlang:put(checked, Value), Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term name = Term.A("name");
            Term marker = Term.A("checked");
            Term zero = Term.I(0);
            Term skip = Term.Tuple(
                Term.A("form"),
                zero,
                Term.A("skip"),
                zero,
                Nil.Value
            );
            Term skipped = Term.List(skip);
            Term accepted = await module.Invoke(
                context,
                "start",
                Nil.Value,
                name
            );
            Term rejected = await module.Invoke(
                context,
                "start",
                skipped,
                name
            );
            Term unknown = Term.A("unknown");
            Term openResult = await module.Invoke(
                context,
                "open",
                Nil.Value,
                skipped,
                name,
                unknown
            );
            Term expectedCall = Term.Tuple(
                Term.A("call"),
                zero,
                Term.Tuple(
                    Term.A("remote"),
                    zero,
                    Term.Tuple(Term.A("atom"), zero, Term.A("checks")),
                    Term.Tuple(Term.A("atom"), zero, Term.A("check"))
                ),
                Term.List(Term.Tuple(Term.A("atom"), zero, name))
            );
            Term clause = Term.Tuple(
                Term.A("clause"),
                zero,
                Nil.Value,
                Nil.Value,
                Term.List(expectedCall)
            );
            Term form = Term.Tuple(
                Term.A("form"),
                zero,
                Term.A("check"),
                zero,
                Term.List(clause)
            );
            Term attribute = Term.Tuple(
                Term.A("attribute"),
                zero,
                Term.A("doc"),
                Term.A(ErlangBooleanAtoms.False)
            );
            if (!accepted.Equals(Term.Tuple(Term.List(form, attribute), Term.A(ErlangBooleanAtoms.True)))
                || !rejected.Equals(Term.Tuple(skipped, Term.A(ErlangBooleanAtoms.False)))
                || !openResult.Equals(Term.Tuple(skipped, unknown))
                || !context.Dictionary.TryGetValue(marker, out Term? effect) || !zero.Equals(effect))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var name, var arity] && atoms[Value(name)] == "rewrite" && Value(arity) == 4
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        if (frame < 0 || Operands(body[frame]) is not [var size, var live] || Value(size) != 3 || Value(live) != 4)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int guard = Array.FindIndex(body, frame + 1, instruction => Opcode(instruction) == ExactEqualTest);
        var heaps = body.Skip(guard + 1).Where(instruction => Opcode(instruction) == TestHeap)
            .Select(Operands).Select(operands => (Value(operands[0]), Value(operands[1]))).ToArray();
        if (guard < 0 || !heaps.SequenceEqual([(52, 1), (3, 0)])
            || body.Skip(guard + 1).Count(instruction => Opcode(instruction) == Deallocate) != 2
            || body.Skip(frame + 1).Take(guard - frame - 1).Any(instruction => Opcode(instruction) == InitYRegisters))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
