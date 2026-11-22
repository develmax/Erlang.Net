using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedCallArgumentFieldsChecks
{
    internal const string TestName = "compiler/beam/loads-checked-single-use-fields-into-call-registers";
    private const string WrongSchedule = "Expected checked fields to reach the outgoing argument registers after the guard.";
    private const string WrongResult = "The checked record branch changed its selected call or failure.";
    private const string Source = """
        -module(beam_checked_call_argument_fields).
        -export([direct/1, retained/1, read_later/1, computed/1]).
        -record(options, {key, port, first, second, saved}).

        direct(Input) ->
            case fetch(Input) of
                {found, #options{key=K,port=0,first=A,second=B}} when is_atom(K) ->
                    case observe(A,K,0,B) of
                        {seen, Value} -> {direct, Value};
                        Other -> Other
                    end;
                Other -> {rejected, Other}
            end.

        retained(Input) ->
            case fetch(Input) of
                {found, #options{key=K,port=0,first=A,second=B}=R} when is_atom(K) ->
                    case observe(A,K,0,B) of
                        {seen, Value} -> {saved, R#options.saved, Value};
                        Other -> Other
                    end;
                Other -> {rejected, Other}
            end.

        read_later(Input) ->
            case fetch(Input) of
                {found, #options{key=K,port=0,first=A,second=B}} when is_atom(K) ->
                    case observe(A,K,0,B) of
                        {seen, {A,K,0,B}} -> matched;
                        Other -> {unmatched, Other}
                    end;
                Other -> {rejected, Other}
            end.


        computed(Input) ->
            case fetch(Input) of
                {found, #options{key=K,port=P,first=A,second=B}} when is_atom(K) ->
                    case observe(A,K,prepare(P),B) of
                        {seen, Value} -> {computed, Value};
                        Other -> Other
                    end;
                Other -> {rejected, Other}
            end.

        prepare(Value) ->
            erlang:put(computed, Value),
            {ready, Value}.

        fetch(Input) -> {found, Input}.
        observe(A, K, N, B) -> {seen, {A, K, N, B}}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_checked_call_argument_fields.erl");
        CheckSchedule(bytes);
        CheckComputedSchedule(bytes);
        BeamModule beam = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("options");
            Term key = Term.A("key");
            Term zero = Term.I(0);
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term saved = Term.A("saved");
            Term terms = Term.Tuple(
                first,
                key,
                zero,
                second
            );
            Term input = Term.Tuple(
                tag,
                key,
                zero,
                first,
                second,
                saved
            );
            Term badKey = Term.Tuple(
                tag,
                Term.I(1),
                zero,
                first,
                second,
                saved
            );
            Term badPort = Term.Tuple(
                tag,
                key,
                Term.I(1),
                first,
                second,
                saved
            );
            if (!Term.Tuple(Term.A("direct"), terms).Equals(await beam.Invoke(context, "direct", input))
                || !Term.Tuple(Term.A("saved"), saved, terms).Equals(await beam.Invoke(context, "retained", input))
                || !Term.A("matched").Equals(await beam.Invoke(context, "read_later", input))
                || !Reject(badKey).Equals(await beam.Invoke(context, "direct", badKey))
                || !Reject(badPort).Equals(await beam.Invoke(context, "retained", badPort)))
            {
                throw new InvalidOperationException(WrongResult);
            }


            Term marker = Term.A("computed");
            Term computedTerms = Term.Tuple(
                first,
                key,
                Term.Tuple(Term.A("ready"), zero),
                second
            );
            if (!Term.Tuple(marker, computedTerms).Equals(await beam.Invoke(context, "computed", input))
                || !context.Dictionary.TryGetValue(marker, out Term? prepared) || !zero.Equals(prepared))
            {
                throw new InvalidOperationException(WrongResult);
            }

            context.Dictionary.Remove(marker);
            if (!Reject(badKey).Equals(await beam.Invoke(context, "computed", badKey))
                || context.Dictionary.ContainsKey(marker))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });

        static Term Reject(Term input) => Term.Tuple(Term.A("rejected"), Term.Tuple(Term.A("found"), input));
    }

    private static void CheckSchedule(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (string function in new[] { "direct", "retained" })
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == function);
            if (start < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            object[] body = instructions.Skip(start + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .ToArray();
            int guard = Array.FindIndex(body, instruction => Opcode(instruction) == IsAtom);
            int target = Array.FindIndex(body, instruction => Opcode(instruction) == LocalCall
                && Value(Operands(instruction)[0]) == 4);
            if (guard < 0 || target <= guard)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            object[][] projections = body[(guard + 1)..target]
                .Where(instruction => Opcode(instruction) == GetTupleElement)
                .Select(Operands).ToArray();
            if (projections.Length != 2 || Value(projections[0][1]) != 3 || Value(projections[1][1]) != 4
                || !IsX(projections[0][2], 0) || !IsX(projections[1][2], 3))
            {
                throw new InvalidOperationException(WrongSchedule);
            }
        }
    }


    private static void CheckComputedSchedule(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "computed");
        if (start < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] body = instructions.Skip(start + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .ToArray();
        int guard = Array.FindIndex(body, instruction => Opcode(instruction) == IsAtom);
        int computed = guard < 0 ? -1 : Array.FindIndex(body, guard + 1, instruction =>
            Opcode(instruction) == LocalCall && Value(Operands(instruction)[0]) == 1);
        int consumer = computed < 0 ? -1 : Array.FindIndex(body, computed + 1, instruction =>
            Opcode(instruction) == LocalCall && Value(Operands(instruction)[0]) == 4);
        if (computed < 0 || consumer < 0
            || !body[(guard + 1)..computed].Where(instruction => Opcode(instruction) == GetTupleElement)
                .Select(instruction => Value(Operands(instruction)[1])).SequenceEqual([2])
            || !body[(computed + 1)..consumer].Where(instruction => Opcode(instruction) == GetTupleElement)
                .Select(instruction => Value(Operands(instruction)[1])).SequenceEqual([3, 4]))
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static bool IsX(object operand, int index) =>
        (string)Property(operand, "Kind") == "x" && Value(operand) == index;

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
