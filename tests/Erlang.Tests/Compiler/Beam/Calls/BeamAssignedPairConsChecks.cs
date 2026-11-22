using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAssignedPairConsChecks
{
    internal const string TestName = "compiler/beam/retains-checked-call-pair-in-recursive-cons";
    private const string WrongSchedule = "The checked call pair was rebuilt or an unequal tuple was reused.";
    private const string WrongResult = "The checked call pair changed the selected result or failed payload.";
    private const string Source = """
        -module(beam_assigned_pair_cons).
        -export([pair/1, reversed/1, filter_pair/3, filter_swapped/3]).

        pair(Input) -> pair(Input, []).
        pair([], Acc) -> lists:reverse(Acc);
        pair([Search | Rest], Acc) ->
            {Key, Value} = lists:keyfind(Search, 1, [{"keep", 1}, {"special", 2}, {"skip", 3}, {"malformed", 4, 5}]),
            case Key of
                "keep" -> pair(Rest, [{Key, Value} | Acc]);
                "special" ->
                    case lists:member(Value, [2, 3]) of
                        true -> pair(Rest, [{Key, Value} | Acc]);
                        false -> pair(Rest, Acc)
                    end;
                _ -> pair(Rest, Acc)
            end.

        reversed(Input) -> reversed(Input, []).
        reversed([], Acc) -> lists:reverse(Acc);
        reversed([Search | Rest], Acc) ->
            {Key, Value} = lists:keyfind(Search, 1, [{"keep", 1}]),
            case Key of
                "keep" -> reversed(Rest, [{Value, Key} | Acc]);
                _ -> reversed(Rest, Acc)
            end.

        filter_pair([], _Exclude, Acc) -> Acc;
        filter_pair([{Key, Value} | Rest], Exclude, Acc) ->
            case lists:member(Key, Exclude) of
                true -> filter_pair(Rest, Exclude, Acc);
                false -> filter_pair(Rest, Exclude, [{Key, Value} | Acc])
            end.

        filter_swapped([], _Exclude, Acc) -> Acc;
        filter_swapped([{Key, Value} | Rest], Exclude, Acc) ->
            case lists:member(Key, Exclude) of
                true -> filter_swapped(Rest, Exclude, Acc);
                false -> filter_swapped(Rest, Exclude, [{Value, Key} | Acc])
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_assigned_pair_cons.erl");
        CheckLayout(image);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([ListModuleNames.Module]));
        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed(ErlangModuleNames.Module), RuntimeModuleSelection.Managed(ListModuleNames.Module)]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(
            selection,
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term keep = Term.String("keep");
            Term special = Term.String("special");
            Term missing = Term.String("missing");
            Term malformed = Term.String("malformed");
            Term expected = Term.List(Term.Tuple(keep, Term.I(1)), Term.Tuple(special, Term.I(2)));
            if (!expected.Equals(await module.Invoke(context, "pair", Term.List(keep, Term.String("skip"), special)))
                || !Term.List(Term.Tuple(Term.I(1), keep)).Equals(await module.Invoke(context, "reversed", Term.List(keep))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term input = Term.List(Term.Tuple(keep, Term.I(1)), Term.Tuple(special, Term.I(2)));
            Term excluded = Term.List(special);
            if (!Term.List(Term.Tuple(keep, Term.I(1))).Equals(
                    await module.Invoke(
                        context,
                        "filter_pair",
                        input,
                        excluded,
                        Nil.Value
                    )
                )
                || !Term.List(Term.Tuple(Term.I(1), keep)).Equals(
                    await module.Invoke(
                        context,
                        "filter_swapped",
                        input,
                        excluded,
                        Nil.Value
                    )
                ))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await Failure(missing, Term.A("false"));
            await Failure(malformed, Term.Tuple(malformed, Term.I(4), Term.I(5)));

            return Nil.Value;

            async Task Failure(Term input, Term rejected)
            {
                try
                {
                    _ = await module.Invoke(context, "pair", Term.List(input));
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), rejected)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] filtered = Body("filter_pair", 3);
        if (Body("pair").Any(IsPair) || !Body("reversed").Any(IsPair)
            || filtered.Any(IsPair) || !Body("filter_swapped", 3).Any(IsPair)
            || filtered.Count(instruction => Opcode(instruction) == GetTupleElement) != 1)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] Body(string function, int arity = 2)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operand(instruction, 2) == arity && atoms[Operand(instruction, 1)] == function
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }

        static bool IsPair(object instruction) => Opcode(instruction) == PutTuple
            && Operand(instruction, 1) == 2;
    }

    private static int Operand(object instruction, int index) =>
        (int)(BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
