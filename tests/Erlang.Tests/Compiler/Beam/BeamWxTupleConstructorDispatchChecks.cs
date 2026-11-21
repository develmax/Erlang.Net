using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxTupleConstructorDispatchChecks
{
    internal const string TestName = "compiler/beam/dispatches-wx-tuple-constructor-alternatives";
    private const string Fixture = "beam_wx_tuple_constructor.erl";
    private const string WrongDispatch = "Expected a single tuple width decision after the non-tuple constructor alternatives.";
    private const string WrongFrames = "Expected three option closures and a one-slot tuple constructor frame.";
    private const string WrongBinding = "The tuple dispatch subsumed a repeated field or an existing environment comparison.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_tuple_constructor).
        -export([simple/1,construct/1,construct/2]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).

        simple(Text) when is_list(Text) orelse is_binary(Text) ->
            simple(Text,[]);
        simple({Width,Height}=Size) when is_integer(Width),is_integer(Height) ->
            simple(Size,[]).
        simple(Value,Options) -> {Value,Options}.

        construct(Text) when is_list(Text) orelse is_binary(Text) ->
            construct(Text,[]);
        construct({Width,Height}=Size) when is_integer(Width),is_integer(Height) ->
            construct(Size,[]);
        construct(#wx_ref{type=Class}=Image) ->
            First = try ?CLASS(Class,wxImage) catch _:_ -> false end,
            Second = try ?CLASS(Class,wxBitmap) catch _:_ -> false end,
            Selected = if First -> wxImage;
                          Second -> wxBitmap;
                          true -> erlang:error({badarg,Class}) end,
            wxe_util:queue_cmd(wx:typeCast(Image,Selected),?ENV,1830),
            wxe_util:rec(1830).

        construct(Width,Height) when is_integer(Width),is_integer(Height) ->
            construct(Width,Height,[]);
        construct(Text,Options) when (is_list(Text) orelse is_binary(Text)),is_list(Options) ->
            Converted = unicode:characters_to_binary(Text),
            Mapper = fun({type,_}=Entry) -> Entry; (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Converted,Effective,?ENV,1831),
            wxe_util:rec(1831);
        construct({Width,Height}=Size,Options) when is_integer(Width),is_integer(Height),is_list(Options) ->
            Mapper = fun({depth,_}=Entry) -> Entry; (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Size,Effective,?ENV,1832),
            wxe_util:rec(1832);
        construct(#wx_ref{type=Class}=Image,Options) when is_list(Options) ->
            ?CLASS(Class,wxImage),
            Mapper = fun({depth,_}=Entry) -> Entry; (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Image,Effective,?ENV,1833),
            wxe_util:rec(1833).

        construct(Width,Height,Options) -> {Width,Height,Options}.
        """;

    internal static Task Run()
    {
        var bodies = ReadFunctions(Source);
        object[] simple = bodies("simple", 1).Body;
        if (simple.Count(instruction => Opcode(instruction) == TailLocalCall) != 2
            || simple.Count(instruction => Opcode(instruction) == TestArity) != 1)
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        foreach (int arity in new[] { 1, 2 })
        {
            object[] body = bodies("construct", arity).Body;
            if (body.Count(instruction => Opcode(instruction) == SelectTupleArity) != 1
                || body.Count(instruction => Opcode(instruction) == GetTupleElement) < 3)
            {
                throw new InvalidOperationException(WrongDispatch);
            }
        }

        object[] binary = bodies("construct", 2).Body;
        object[] makers = binary.Where(instruction => Opcode(instruction) == MakeFun).ToArray();
        if (makers.Length != 3 || !binary.Any(instruction =>
            Opcode(instruction) == AllocateHeap && Value(Operands(instruction)[0]) == 1))
        {
            throw new InvalidOperationException(WrongFrames);
        }

        string repeated = Source.Replace(
            "{Width,Height}=Size) when is_integer(Width),is_integer(Height) ->",
            "{Width,Width}=Size) when is_integer(Width) ->",
            StringComparison.Ordinal
        );
        var guarded = ReadFunctions(repeated);
        if (guarded("construct", 1).Body.Any(instruction => Opcode(instruction) == SelectTupleArity)
            || guarded("simple", 1).Body.All(instruction => Opcode(instruction) != ExactEqualTest))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        string bound = Source.Replace(
            "queue_cmd(Size,Effective,?ENV,1832)",
            "queue_cmd(Size,Effective,case erlang:get(wx_env) of {wx_env,Width,_,_} -> Width; _ -> erlang:error({wx,unknown_env}) end,1832)",
            StringComparison.Ordinal
        );
        if (ReadFunctions(bound)("construct", 2).Body.Any(instruction => Opcode(instruction) == SelectTupleArity))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }

    private static Func<string, int, (object[] Body, int Failure)> ReadFunctions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return (name, arity) =>
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );
            if (entry < 2)
            {
                throw new InvalidOperationException(Missing);
            }

            return (instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray(),
                Value(Operands(instructions[entry - 2])[0]));
        };
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
