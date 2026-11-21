using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxClassIndexOverloadChecks
{
    internal const string TestName = "compiler/beam/shares-class-projections-across-numeric-reference-overloads";
    private const string Fixture = "beam_wx_class_index.erl";
    private const string WrongProjection = "The shared receiver type was projected after splitting the argument alternatives.";
    private const string WrongMappedDispatch = "Mapped options lost their shared receiver prologue or branch-specific mapper.";
    private const string WrongDimensions = "A retained dimension was not moved before trimming the checked class frame.";
    private const string WrongBinding = "A repeated dimension binding disappeared from the entry match.";

    private const string Source = """
        -module(beam_wx_class_index).
        -export([attach/2,attach/3,attach/4,show/3]).
        -record(wx_ref,{ref,type,state=[]}).
        -record(wx_env,{ref,sv,debug=0}).
        -define(CLASS(T,C), ((T) =:= C) orelse (T):parent_class(C)).
        -define(CLASS_T(T,C), try (((T) =:= C) orelse (T):parent_class(C)) catch _:_ -> false end).
        -define(ENV(), case erlang:get(wx_env) of #wx_env{ref=Context} -> Context; _ -> erlang:error({wx,unknown_env}) end).
        -define(SELECT(T),
            Window = ?CLASS_T(T,wxWindow),
            Sizer = ?CLASS_T(T,wxSizer),
            Selected = if Window -> wxWindow; Sizer -> wxSizer; true -> erlang:error({badarg,T}) end).

        attach(This=#wx_ref{type=Owner},Other=#wx_ref{type=Child}) ->
            ?CLASS(Owner,wxSizer), ?SELECT(Child),
            wxe_util:queue_cmd(This,wx:typeCast(Other,Selected),?ENV(),20),
            wxe_util:rec(20);
        attach(This=#wx_ref{type=Owner},Index) when is_integer(Index) ->
            ?CLASS(Owner,wxSizer),
            wxe_util:queue_cmd(This,Index,?ENV(),21),
            wxe_util:rec(21).

        attach(This=#wx_ref{type=Owner},Other=#wx_ref{type=Child},{Width,Height}=Size)
          when is_integer(Width),is_integer(Height) ->
            ?CLASS(Owner,wxSizer), ?SELECT(Child),
            wxe_util:queue_cmd(This,wx:typeCast(Other,Selected),Size,?ENV(),22),
            wxe_util:rec(22);
        attach(This=#wx_ref{type=Owner},Index,{Width,Height}=Size)
          when is_integer(Index),is_integer(Width),is_integer(Height) ->
            ?CLASS(Owner,wxSizer),
            wxe_util:queue_cmd(This,Index,Size,?ENV(),23),
            wxe_util:rec(23).

        attach(This=#wx_ref{type=Owner},Other=#wx_ref{type=Child},Width,Height)
          when is_integer(Width),is_integer(Height) ->
            ?CLASS(Owner,wxSizer), ?SELECT(Child),
            wxe_util:queue_cmd(This,wx:typeCast(Other,Selected),Width,Height,?ENV(),24),
            wxe_util:rec(24);
        attach(This=#wx_ref{type=Owner},Index,Width,Height)
          when is_integer(Index),is_integer(Width),is_integer(Height) ->
            ?CLASS(Owner,wxSizer),
            wxe_util:queue_cmd(This,Index,Width,Height,?ENV(),25),
            wxe_util:rec(25).

        show(This=#wx_ref{type=Owner},Other=#wx_ref{type=Child},Options)
          when is_list(Options) ->
            ?CLASS(Owner,wxSizer), ?SELECT(Child),
            Map = fun({show,_Show}=Arg) -> Arg; ({recursive,_Recursive}=Arg) -> Arg;
                     (Bad) -> erlang:error({badoption,Bad}) end,
            Mapped = lists:map(Map,Options),
            wxe_util:queue_cmd(This,wx:typeCast(Other,Selected),Mapped,?ENV(),26),
            wxe_util:rec(26);
        show(This=#wx_ref{type=Owner},Index,Options)
          when is_integer(Index),is_list(Options) ->
            ?CLASS(Owner,wxSizer),
            Map = fun({show,_Show}=Arg) -> Arg; (Bad) -> erlang:error({badoption,Bad}) end,
            Mapped = lists:map(Map,Options),
            wxe_util:queue_cmd(This,Index,Mapped,?ENV(),27),
            wxe_util:rec(27).
        """;

    internal static Task Run()
    {
        foreach (int arity in new[] { 2, 3, 4 })
        {
            object[] body = Read(Source, arity);
            int tag = Array.FindIndex(body, instruction => Opcode(instruction) == IsTaggedTuple);
            if (tag < 0 || Opcode(body[tag + 1]) != GetTupleElement || Opcode(body[tag + 2]) != IsTaggedTuple
                || Value(Operands(body[tag + 1])[2]) != arity)
            {
                throw new InvalidOperationException(WrongProjection);
            }

            if (!body.Where(instruction => Opcode(instruction) == TrimFrame)
                .Any(instruction => Value(Operands(instruction)[0]) == 4 && Value(Operands(instruction)[1]) == arity - 1))
            {
                throw new InvalidOperationException(WrongDimensions);
            }
        }

        var mapped = Read(Source, 3, "show");
        int mappedTag = Array.FindIndex(mapped, instruction => Opcode(instruction) == IsTaggedTuple);
        if (mappedTag < 0 || Opcode(mapped[mappedTag + 1]) != GetTupleElement
            || Opcode(mapped[mappedTag + 2]) != IsTaggedTuple || Opcode(mapped[mappedTag + 3]) != IsList
            || mapped.Count(instruction => Opcode(instruction) == MakeFun) != 2)
        {
            throw new InvalidOperationException(WrongMappedDispatch);
        }
        string bound = Source.Replace("{Width,Height}=Size", "{Width,Width}=Size", StringComparison.Ordinal);
        string[] definitions = bound.Split("\n\n", StringSplitOptions.None);
        for (int index = 0; index < definitions.Length; index++)
        {
            if (definitions[index].Contains("{Width,Width}=Size", StringComparison.Ordinal))
            {
                definitions[index] = definitions[index].Replace("is_integer(Height)", "is_integer(Width)", StringComparison.Ordinal);
            }
        }

        bound = string.Join("\n\n", definitions);
        if (!Read(bound, 3).Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands).Any(operands => operands.Skip(1)
                .All(operand => (string)Property(operand, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }

    private static object[] Read(string source, int arity, string function = "attach")
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Value(Operands(instruction)[2]) == arity && atoms[Value(Operands(instruction)[1])] == function
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(Fixture);
        }

        return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();
}
