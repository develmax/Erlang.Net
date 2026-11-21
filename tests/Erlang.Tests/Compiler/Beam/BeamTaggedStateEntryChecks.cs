using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedStateEntryChecks
{
    internal const string TestName = "compiler/beam/partitions-tagged-state-inside-tuple-arity";
    private const string Fixture = "beam_tagged_state_entry.erl";
    private const string WrongLayout = "The inner state table did not preserve its independently checked frame owners.";
    private const string WrongValue = "Nested state selection changed head failure, the original fallback arguments or the selected effect.";

    private const string Source = """
        -module(beam_tagged_state_entry).
        %% options
        -export([dispatch/4, read_effect/1]).
        -record(session, {value}).

        dispatch(event, {msg, Key}, {read, client}, State = #session{value = Value}) ->
            erlang:put(Key, Value),
            {sent, State};
        dispatch(event, {msg, _}, {wait, client}, State) ->
            {queued, State};
        dispatch(event, {single}, _, State) ->
            {single, State};
        dispatch(Event, Message, State, Data) ->
            {fallback, Event, Message, State, Data}.

        read_effect(Key) -> erlang:get(Key).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        byte[] overlapping = CodeGeneration.CompileBeamModule(
            Source.Replace("{wait, client}", "{read, client}", StringComparison.Ordinal),
            Fixture
        );
        if (!HasOwnedStateBranches(image) || HasOwnedStateBranches(disabled) || HasOwnedStateBranches(overlapping))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        var rejected = BeamModule.Prepare(overlapping);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Atom kind = Term.A("event");
            Atom key = Term.A("entry_effect");
            Atom payload = Term.A("payload");
            Atom wrong = Term.A("wrong");
            Term message = Term.Tuple(Term.A("msg"), key);
            Term read = Term.Tuple(Term.A("read"), Term.A("client"));
            Term wait = Term.Tuple(Term.A("wait"), Term.A("client"));
            Term missed = Term.Tuple(Term.A("read"), wrong);
            Term data = Term.Tuple(Term.A("session"), payload);
            Term queued = Term.Tuple(Term.A("queued"), wrong);
            if (!await Matches(
                module,
                Term.A("undefined"),
                "read_effect",
                key
            )
                || !await Matches(
                    module,
                    queued,
                    "dispatch",
                    kind,
                    message,
                    wait,
                    wrong
                )
                || !await Matches(
                    module,
                    Fallback(
                        kind,
                        message,
                        missed,
                        data
                    ),
                    "dispatch",
                    kind,
                    message,
                    missed,
                    data
                )
                || !await Matches(
                    module,
                    Fallback(
                        kind,
                        message,
                        read,
                        wrong
                    ),
                    "dispatch",
                    kind,
                    message,
                    read,
                    wrong
                )
                || !await Matches(
                    module,
                    Fallback(
                        kind,
                        message,
                        wrong,
                        data
                    ),
                    "dispatch",
                    kind,
                    message,
                    wrong,
                    data
                )
                || !await Matches(
                    rejected,
                    queued,
                    "dispatch",
                    kind,
                    message,
                    read,
                    wrong
                )
                || !await Matches(
                    module,
                    Term.A("undefined"),
                    "read_effect",
                    key
                )
                || !await Matches(
                    module,
                    Term.Tuple(Term.A("sent"), data),
                    "dispatch",
                    kind,
                    message,
                    read,
                    data
                )
                || !await Matches(
                    module,
                    payload,
                    "read_effect",
                    key
                ))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task<bool> Matches(
                BeamModule target,
                Term expected,
                string name,
                params Term[] arguments
            ) =>
                expected.Equals(await target.Invoke(context, name, arguments));
        });
    }

    private static bool HasOwnedStateBranches(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = Items(code, "Instructions").SkipWhile(instruction =>
            Opcode(instruction) != FunctionInfo || atoms[Value(Operands(instruction)[1])] != "dispatch")
            .Skip(1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        foreach (object instruction in body.Where(instruction => Opcode(instruction) == SelectValue))
        {
            if (Operands(instruction) is not [_, var failure, _, var first, var read, var second, var wait]
                || !IsKey(first, "read") || !IsKey(second, "wait"))
            {
                continue;
            }

            object[] effect = Block(read);
            object[] plain = Block(wait);

            return effect.Length >= 3 && Opcode(effect[0]) == ExactEqualTest
                && Opcode(effect[1]) == IsTaggedTuple && Opcode(effect[2]) == Allocate
                && Value(Operands(effect[0])[0]) == Value(failure)
                && Value(Operands(effect[1])[0]) == Value(failure)
                && Value(Operands(effect[2])[0]) == 1
                && plain.Length >= 2 && Opcode(plain[0]) == ExactEqualTest
                && Value(Operands(plain[0])[0]) == Value(failure)
                && plain.All(item => Opcode(item) is not (Allocate or AllocateHeap));
        }

        return false;

        bool IsKey(object operand, string name) => Equals(Property(operand, "Kind"), "atom") && atoms[Value(operand)] == name;

        object[] Block(object label) => body.SkipWhile(instruction =>
            Opcode(instruction) != Label || Value(Operands(instruction)[0]) != Value(label))
            .Skip(1).TakeWhile(instruction => Opcode(instruction) != Label).ToArray();
    }

    private static Term Fallback(
        Term kind,
        Term message,
        Term state,
        Term data
    ) => Term.Tuple(
        Term.A("fallback"),
        kind,
        message,
        state,
        data
    );

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
