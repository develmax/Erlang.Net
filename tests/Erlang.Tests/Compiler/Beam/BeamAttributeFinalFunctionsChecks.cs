using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAttributeFinalFunctionsChecks
{
    internal const string TestName = "compiler/beam/retains-optional-foreign-pair-and-matches-closed-bit-padding";
    private const string Fixture = "beam_attribute_final_probe.erl";
    private const string Peer = "beam_attribute_final_peer";
    private const string Encode = "encode";
    private const string Check = "check";
    private const string Predicate = "predicate";
    private const string WrongLayout = "The retained pair and closed padding must select the pinned register layouts.";
    private const string WrongValue = "The retained pair or closed padding changed a visible value.";

    private const string Source = """
        -module(beam_attribute_final_probe).
        -export([encode/2, check/1, choose/2]).

        encode(Record, Tags) ->
            {_,First,Middle,Last} = Record,
            {A,ALen} = pack(First, [1]),
            {B,BLen} = case Middle of
                absent -> {<<>>,0};
                _ -> beam_attribute_final_peer:pack(Middle,[2])
            end,
            {C,CLen} = case Last of
                absent -> {<<>>,0};
                _ -> pack(Last,[3])
            end,
            Parts = [A,B,C],
            Size = ALen + BLen + CLen,
            finish(Tags,Parts,Size).

        pack(Value, _) -> {Value,1}.
        finish(Tags, Parts, Size) -> {Tags,Parts,Size}.

        choose(Key, Arg) -> (factory(Key))(Arg).
        factory(Key) when Key =:= {1,0} -> fun(Arg) -> {first,Arg} end;
        factory(Key) when Key =:= {2,0} -> fun declared/1;
        factory(Key) when Key =:= {1,1} -> fun(Arg) -> {second,Arg} end;
        factory(Key) when Key =:= {1,2} -> fun(Arg) -> {third,Arg} end;
        factory(_) -> fun(Arg) -> {fallback,Arg} end.
        declared(Arg) -> {declared,Arg}.

        check(Value) ->
            try predicate(Value,[a],<<1:2>>,2) of
                true -> true
            catch
                throw:false -> false
            end.

        predicate([_ | _] = Val, Names, _, _) ->
            case lists:sort(Val) of
                Names -> true;
                _ -> throw(false)
            end;
        predicate(Bs, _, Bs, _) -> true;
        predicate(Val, _, Bs, BsSize) ->
            Rest = bit_size(Val) - BsSize,
            case Val of
                <<Bs:BsSize/bits,0:Rest>> -> true;
                _ -> throw(false)
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] open = CodeGeneration.CompileBeamModule(
            Source.Replace("encode/2, check/1", "encode/2, check/1, predicate/4", StringComparison.Ordinal), Fixture
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer, "lists", "erlang"]));
        await using var runtime = new ProcessRuntime();
        Term bad = Term.A("bad_pair");
        runtime.Modules.Register(
            Peer,
            "pack",
            2,
            (_, args) => ValueTask.FromResult<Term>(args[0].Equals(bad) ? bad : Term.Tuple(args[0], Term.I(1)))
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("field");
            Term first = Term.A("first");
            Term middle = Term.A("middle");
            Term absent = Term.A("absent");
            Term third = Term.A("third");
            Term expected = Term.Tuple(tag, Term.List(first, middle, third), Term.I(3));
            if (!expected.Equals(await module.Invoke(
                context,
                Encode,
                Term.Tuple(
                    tag,
                    first,
                    middle,
                    third
                ),
                tag
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term expectedAbsent = Term.Tuple(tag, Term.List(first, new BitString([]), new BitString([])), Term.I(1));
            if (!expectedAbsent.Equals(await module.Invoke(
                context,
                Encode,
                Term.Tuple(
                    tag,
                    first,
                    absent,
                    absent
                ),
                tag
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    Encode,
                    Term.Tuple(
                        tag,
                        first,
                        bad,
                        third
                    ),
                    tag
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), bad)))
            {
            }

            foreach ((Term input, Term expectedValue) in new (Term, Term)[]
            {
                (new BitString([64], 2), Term.A("true")),
                (new BitString([64]), Term.A("true")),
                (new BitString([65]), Term.A("false")),
                (new BitString([0], 1), Term.A("false")),
                (Term.List(Term.A("a")), Term.A("true")),
                (Term.List(Term.A("b")), Term.A("false"))
            })
            {
                if (!expectedValue.Equals(await module.Invoke(context, Check, input)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            foreach ((int group, int code, string expectedTag) in new (int, int, string)[]
            {
                (1, 0, "first"),
                (2, 0, "declared"),
                (1, 1, "second"),
                (1, 2, "third"),
                (3, 0, "fallback")
            })
            {
                Term choice = await module.Invoke(
                    context,
                    "choose",
                    Term.Tuple(Term.I(group), Term.I(code)),
                    first
                );
                if (!choice.Equals(Term.Tuple(Term.A(expectedTag), first)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });

        if (!Frames(image, Encode).SequenceEqual([4]) || !Frames(image, Predicate).SequenceEqual([1])
            || Frames(open, Predicate).SequenceEqual([1]) || !HasFactoryOrder(image))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool HasFactoryOrder(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return Items(code, "Instructions")
            .Where(instruction => Opcode(instruction) == FunctionInfo)
            .Select(instruction => atoms[Value(Items(instruction, "Operands")[1])])
            .Where(name => name.StartsWith("-factory/1-fun-", StringComparison.Ordinal))
            .SequenceEqual(["-factory/1-fun-3-", "-factory/1-fun-0-", "-factory/1-fun-1-", "-factory/1-fun-2-"]);
    }

    private static int[] Frames(byte[] image, string function)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, _]
                && atoms[Value(name)] == function
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) == Allocate)
            .Select(instruction => Value(Items(instruction, "Operands")[0])).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
