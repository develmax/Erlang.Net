using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCoveredTaggedCallPairChecks
{
    internal const string TestName = "compiler/beam/projects-a-covered-tagged-call-pair-before-selecting";
    private const string Module = "beam_covered_tagged_call_pair";
    private const string Maps = "maps";
    private const string WrongCase = "Expected one checked payload projection with the original case-clause failure value.";
    private const string WrongGuard = "Expected the covered guard to join tuple BIF rejection before checking the alternative.";
    private const string Source = """
        -module(beam_covered_tagged_call_pair).
        -export([complete/1, partial/1, aliased/1]).
        complete(Box) ->
            case maps:get(value, Box) of
                {error, Reason} -> {bad, Reason};
                {ok, {item, Value, 0}}
                when tuple_size(Value) =:= 2, element(1, Value) =:= local;
                     Value =:= undefined -> {good, Value};
                {ok, _} -> retry
            end.
        aliased(Box) ->
            case maps:get(value, Box) of
                {error, Reason} -> {bad, Reason};
                {ok, {item, Value, 0} = Whole}
                when tuple_size(Value) =:= 2, element(1, Value) =:= local;
                     Value =:= undefined -> {good, Whole};
                {ok, _} -> retry
            end.
        partial(Box) ->
            case maps:get(value, Box) of
                {error, Reason} -> {bad, Reason};
                {ok, {item, Value}}
                when tuple_size(Value) =:= 2, element(1, Value) =:= local;
                     Value =:= undefined -> {good, Value}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(image);
        if (!HasSharedPayload(code, "complete") || HasSharedPayload(code, "partial"))
        {
            throw new InvalidOperationException(WrongCase);
        }

        if (!HasProtectedTupleGuard(code, "complete") || HasProtectedTupleGuard(code, "partial")
            || !ChecksLiteralBeforeGuardField(code, "complete") || !ChecksLiteralBeforeGuardField(code, "aliased"))
        {
            throw new InvalidOperationException(WrongGuard);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([Maps]));
        var selection = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed(Maps)
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term address = Term.Tuple(Term.A("local"), Term.A("path"));
            Term undefined = Term.A("undefined");
            Term other = Term.A("other");
            Term malformed = Term.Tuple(Term.A("unknown"), other);
            Term rejected = Term.Tuple(Term.A("ok"), other);
            Term acceptedItem = Term.Tuple(Term.A("item"), address, Term.I(0));
            Term accepted = Term.Tuple(Term.A("ok"), acceptedItem);
            Term missingAddress = Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("item"), undefined, Term.I(0)));
            Term rejectedLiteral = Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("item"), undefined, Term.I(1)));
            Term error = Term.Tuple(Term.A("error"), other);
            if (!Term.Tuple(Term.A("good"), address).Equals(await module.Invoke(context, "complete", Box(accepted)))
                || !Term.Tuple(Term.A("good"), undefined).Equals(await module.Invoke(context, "complete", Box(missingAddress)))
                || !Term.A("retry").Equals(await module.Invoke(context, "complete", Box(rejected)))
                || !Term.A("retry").Equals(await module.Invoke(context, "complete", Box(rejectedLiteral)))
                || !Term.A("retry").Equals(await module.Invoke(context, "aliased", Box(rejectedLiteral)))
                || !Term.Tuple(Term.A("good"), acceptedItem).Equals(await module.Invoke(context, "aliased", Box(accepted)))
                || !Term.Tuple(Term.A("bad"), other).Equals(await module.Invoke(context, "complete", Box(error))))
            {
                throw new InvalidOperationException(WrongCase);
            }

            foreach (Term badAddress in new Term[]
            {
                other,
                Term.Tuple(),
                Term.Tuple(Term.A("local")),
                Term.Tuple(Term.A("local"), address, other),
                Term.Tuple(other, address)
            })
            {
                Term badItem = Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("item"), badAddress, Term.I(0)));
                if (!Term.A("retry").Equals(await module.Invoke(context, "complete", Box(badItem))))
                {
                    throw new InvalidOperationException(WrongGuard);
                }
            }

            await RequireFailure("complete", malformed);
            await RequireFailure("complete", other);
            await RequireFailure("partial", rejected);

            return Nil.Value;

            async Task RequireFailure(string function, Term result)
            {
                try
                {
                    await module.Invoke(context, function, Box(result));
                }
                catch (ErlangException failure) when (failure.Reason.Equals(Term.Tuple(Term.A("case_clause"), result)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongCase);
            }
        });
    }

    private static MapTerm Box(Term result) => new([new(Term.A("value"), result)]);

    private static bool HasProtectedTupleGuard(object code, string name)
    {
        object[] body = FunctionInstructions(code, name);
        int size = Array.FindIndex(body, instruction => Opcode(instruction) == BifOne);
        if (size < 0 || size + 4 >= body.Length)
        {
            return false;
        }

        object[] sizeOperands = Items(body[size], "Operands").ToArray();
        object[] comparison = Items(body[size + 1], "Operands").ToArray();
        object[] element = Items(body[size + 2], "Operands").ToArray();

        return sizeOperands is [var rejected, _, _, _] && Value(rejected) != 0
            && Opcode(body[size + 1]) == BifTwo && comparison is [_, _, var integer, _, _]
            && Property(integer, "TypeIndex") is not null
            && Opcode(body[size + 2]) == BifTwo && element is [var elementRejected, _, _, var tuple, _]
            && Value(rejected) == Value(elementRejected) && Property(tuple, "TypeIndex") is not null
            && Opcode(body[size + 3]) == BifTwo && Opcode(body[size + 4]) == BifTwo;
    }

    private static bool ChecksLiteralBeforeGuardField(object code, string name)
    {
        object[] body = FunctionInstructions(code, name);
        int check = Array.FindIndex(body, instruction => Opcode(instruction) == IsTaggedTuple);

        return check >= 0 && check + 3 < body.Length
            && Opcode(body[check + 1]) == GetTupleElement
            && Items(body[check + 1], "Operands").ToArray() is [var literalCarrier, var literal, var temporary]
            && Value(literal) == 2 && (string)Property(temporary, "Kind") == "x" && Value(temporary) == 0
            && Opcode(body[check + 2]) == ExactEqualTest
            && Opcode(body[check + 3]) == GetTupleElement
            && Items(body[check + 3], "Operands").ToArray() is [var guardCarrier, var guarded, _]
            && literalCarrier.Equals(guardCarrier) && Value(guarded) == 1;
    }

    private static bool HasSharedPayload(object code, string name)
    {
        object[] body = FunctionInstructions(code, name);
        int select = Array.FindIndex(body, item => Opcode(item) == SelectValue);

        return select >= 2
            && Projection(body[select - 2], 0, "x") && Projection(body[select - 1], 1, "y");
    }

    private static object[] FunctionInstructions(object code, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
    }

    private static bool Projection(object instruction, int field, string destinationKind) =>
        Opcode(instruction) == GetTupleElement
        && Items(instruction, "Operands").ToArray() is [var source, var index, var destination]
        && (string)Property(source, "Kind") == "x" && Value(source) == 0
        && Value(index) == field && (string)Property(destination, "Kind") == destinationKind;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string property) => ((IEnumerable)Property(owner, property)).Cast<object>();
}
