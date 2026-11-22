using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDeferredRecordListTailChecks
{
    internal const string TestName = "compiler/beam/projects-retained-record-list-tails-after-the-call";
    private const string File = "beam_deferred_record_list_tail.erl";
    private const string WrongLayout = "The checked record's old list tail crossed the wrong call boundary.";
    private const string WrongValue = "A deferred list tail changed construction, input fallback, or call order.";
    private const string Source = """
        -module(beam_deferred_record_list_tail).
        -export([build/2, eager/2, seen/0]).
        -record(state, {tags, contents}).

        build({open, Key, Name, Tail}, #state{tags=T, contents=D}=State) ->
            Val = touch(Key, Name),
            State#state{tags=[Val|T], contents=[Tail|D]};
        build({noop, _, _}, State) -> State;
        build(_, State) -> State.

        eager({open, Key, Name, Tail}, #state{tags=T, contents=D}=State) ->
            Val = touch(Key, {Name,T}),
            State#state{tags=[Val|T], contents=[Tail|D]};
        eager({noop, _, _}, State) -> State;
        eager(_, State) -> State.

        touch(Key, Value) -> erlang:put(Key, Value), Value.
        seen() -> erlang:get(log).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, File);
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("build", []);
        Check("eager", [1]);
        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("log");
            Term name = Term.A("name");
            Term tail = Term.A("tail");
            Term tag = Term.A("state");
            Term tags = Term.List(Term.I(1));
            Term contents = Term.List(Term.I(2));
            Term incoming = Term.Tuple(tag, tags, contents);
            Term evt = Term.Tuple(
                Term.A("open"),
                key,
                name,
                tail
            );
            Term eagerValue = Term.Tuple(name, tags);
            if (!Term.Tuple(tag, Term.List(name, Term.I(1)), Term.List(tail, Term.I(2))).Equals(await module.Invoke(
                context,
                "build",
                evt,
                incoming
            )) || !name.Equals(await module.Invoke(context, "seen"))
                || !Term.Tuple(tag, Term.List(eagerValue, Term.I(1)), Term.List(tail, Term.I(2)))
                    .Equals(await module.Invoke(
                        context,
                        "eager",
                        evt,
                        incoming
                    ))
                || !eagerValue.Equals(await module.Invoke(context, "seen")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term wrong = Term.Tuple(Term.A("wrong"), tags, contents);
            if (!wrong.Equals(await module.Invoke(
                context,
                "build",
                evt,
                wrong
            )) || !eagerValue.Equals(await module.Invoke(context, "seen")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });

        void Check(string function, int[] earlier)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)Value(Items(instruction, "Operands").ElementAt(1))] == function
            );
            if (info < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            object[] body = instructions.Skip(info + 1).TakeWhile(op => Opcode(op) != FunctionInfo).ToArray();
            int call = Array.FindIndex(body, op => Opcode(op) == LocalCall);
            if (call < 0 || !body.Take(call).Where(op => ReadField(op, "x", 1)).Select(op => (int)Value(
                Items(op, "Operands").ElementAt(1)
            )).Order().SequenceEqual(earlier)
                || !body.Skip(call + 1).Any(op => ReadField(op, "y", register: null)))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }
    }

    private static bool ReadField(object instruction, string kind, int? register)
    {
        if (Opcode(instruction) != GetTupleElement
            || Items(instruction, "Operands").ToArray() is not [var input, var field, _]
            || (string)Property(input, "Kind") != kind || register is int index && Value(input) != index)
        {
            return false;
        }

        return Value(field) == 1 || Value(field) == 2;
    }

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object instance, string name) => ((IEnumerable)Property(instance, name)).Cast<object>();
}
