using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstructedListGeneratorHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-constructed-list-generator-inputs";
    private const string Fixture = "beam_constructed_list_generator_helper.erl";
    private const string ClosedHelper = "-constructed/2-lc$^0/1-0-";
    private const string OpenHelper = "-open_tail/1-lc$^0/1-0-";
    private const string TupleOwner = "dispatch_child";
    private const string TupleHelper = "-dispatch_child/5-lc$^0/1-0-";
    private const string WrongHelpers = "Constructed generator helper entries or proper-list exits differed.";
    private const string WrongResult = "Constructed generator input order, failure, or source line differed.";

    private const string Source = """
        -module(beam_constructed_list_generator_helper).
        -export([constructed/2, open_tail/1, reset/0, events/0, nested/1, dispatch_child/5]).
        constructed(Left, Right) ->
            [mapped(Value) || Value <- [produce(Left), produce(Right)]].
        open_tail(Tail) ->
            [mapped(Value) || Value <-
                  [produce(head)|Tail]].
        nested(Values) ->
            [[mapped(Value) || Value <- [Item]] || Item <- Values].
        dispatch_child(Id, Mod, Fun, Args, Options) ->
            [First, Second, Third, Fourth] =
                [option_source:lookup(Key, Options, Default)
                 || {Key, Default} <- [{first, 0}, {second, 1}, {third, 2}, {fourth, [Mod]}]],
            case child_sink:register(peer, {Id, {Mod, Fun, Args}, First, Second, Third, Fourth}) of
                {error, registered} -> child_sink:restart(peer, Id);
                Result -> Result
            end.
        reset() -> put(events, []), ok.
        events() -> get(events).
        produce(fail) -> event({produce,fail}), erlang:error(rejected_input);
        produce(Value) -> event({produce,Value}), Value.
        mapped(Value) -> event({mapped,Value}), Value.
        event(Value) -> put(events, [Value|get(events)]).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(beam);
        object[] functions = Items(code, "Functions");
        object? closed = functions.SingleOrDefault(function => (string)Property(function, "Name") == ClosedHelper);
        object? open = functions.SingleOrDefault(function => (string)Property(function, "Name") == OpenHelper);
        object? tupleOwner = functions.SingleOrDefault(function => (string)Property(function, "Name") == TupleOwner);
        object? tupleHelper = functions.SingleOrDefault(function => (string)Property(function, "Name") == TupleHelper);
        if (closed is null || open is null || HasNilTest(code, closed) || !HasNilTest(code, open)
            || tupleOwner is null || tupleHelper is null || !HasTupleMappingSchedule(
                code,
                tupleOwner,
                tupleHelper
            ))
        {
            throw new InvalidOperationException(WrongHelpers);
        }

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term fail = Term.A("fail");
            Term head = Term.A("head");
            Term improper = Term.A("improper");

            await module.Invoke(context, "reset");
            RequireEqual(Term.List(left, right), await module.Invoke(
                context,
                "constructed",
                left,
                right
            ));
            RequireEqual(
                Term.List(
                    Event("mapped", right),
                    Event("mapped", left),
                    Event("produce", right),
                    Event("produce", left)
                ),
                await module.Invoke(context, "events")
            );

            await module.Invoke(context, "reset");
            try
            {
                await module.Invoke(
                    context,
                    "constructed",
                    left,
                    fail
                );
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("rejected_input")))
            {
                RequireEqual(Term.List(Event("produce", fail), Event("produce", left)), await module.Invoke(context, "events"));
            }

            await module.Invoke(context, "reset");
            try
            {
                await module.Invoke(context, "open_tail", improper);
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error)
            {
                RequireEqual(Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), improper), error.Reason);
                if (error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, Atom name, _, var location] } }
                    || name.Name != OpenHelper || !Cons.Items(location).Any(item => item.Equals(Term.Tuple(Term.A("line"), Term.I(7)))))
                {
                    throw new InvalidOperationException(WrongResult);
                }

                RequireEqual(Term.List(Event("mapped", head), Event("produce", head)), await module.Invoke(context, "events"));
            }

            await module.Invoke(context, "reset");
            RequireEqual(Term.List(Term.List(left), Term.List(right)), await module.Invoke(context, "nested", Term.List(left, right)));

            return Nil.Value;
        });
    }

    private static bool HasTupleMappingSchedule(object code, object parent, object worker)
    {
        object[] body = Body(code, parent);
        object? allocation = body.FirstOrDefault(instruction => Opcode(instruction) == BeamEmissionFormat.AllocateHeap);
        object[] helper = Body(code, worker);

        return allocation is not null && (BigInteger)Property(Items(allocation, "Operands")[0], "Value") == 4
            && helper.Count(instruction => Opcode(instruction) == BeamEmissionFormat.GetTupleElement) == 2
            && !helper.Any(instruction => Opcode(instruction) is BeamEmissionFormat.IsTuple or BeamEmissionFormat.TestArity);
    }

    private static bool HasNilTest(object code, object function) =>
        Body(code, function).Any(instruction => Opcode(instruction) == BeamEmissionFormat.IsNil);

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Body(object code, object function)
    {
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions")[first..end];
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static Term Event(string operation, Term value) => Term.Tuple(Term.A(operation), value);

    private static void RequireEqual(Term expected, Term actual)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(WrongResult + " " + actual);
        }
    }
}
