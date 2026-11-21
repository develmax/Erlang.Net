using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedRecordSelectorColumnChecks
{
    internal const string TestName = "compiler/beam/shared-checked-nested-selector-column";
    private const string MissingColumn = "Disjoint nested selectors did not dispatch before their frames.";
    private const string MissingWidth = "The nested arity groups did not share the outer tuple check.";
    private const string MissingChain = "The call chain did not release the delayed entry field at its last use.";
    private const string MissingFiniteCases = "Finite effects did not dispatch before spilling the selector.";
    private const string SharedOverlap = "Overlapping clause patterns were incorrectly treated as a disjoint selector column.";
    private const string Source = """
        -module(beam_nested_record_selector_column).
        -export([single/2, unions/2, overlap/2, finite/2, linear/2]).
        -record(state,{store,view}).
        single({wrapper,{event,open,V}}, State={state,_}) ->
            service:open(V), {ret,State};
        single({wrapper,{event,close,V}}, State={state,_}) ->
            service:close(V), {ret,State};
        single({wrapper,{event,refresh,V}}, State={state,_}) ->
            service:tune(V,State), {ret,State};
        single(Envelope={wrapper,_},State) ->
            service:unknown(Envelope), {ret,State}.
        unions({wrapper,{event,open,V}}, State) ->
            service:open(V), {ret,State};
        unions({wrapper,{event,close,V}}, State) ->
            service:close(V), {ret,State};
        unions({wrapper,{signal,close,V}}, State) ->
            service:signal(V), {ret,State};
        unions({wrapper,{notice,system,A,B}}, State) ->
            service:notice(A,B), {ret,State};
        unions(Envelope={wrapper,_},State) ->
            service:unknown(Envelope), {ret,State}.
        finite({wrapper,{event,open,V}}, State) ->
            case V of
                3 -> service:open(), service:tune(State), service:check(State);
                7 -> service:close(State);
                9 -> skip
            end,
            {ret,State};
        finite({wrapper,{event,close,V}}, State) ->
            service:close(V), {ret,State};
        finite(Envelope={wrapper,_},State) ->
            service:unknown(Envelope), {ret,State}.
        linear({wrapper,{event,open,V}}, State=#state{}) ->
            service:refresh(State#state.store),
            Key = service:find(V, keys()),
            Cell = service:new(Key),
            Result = service:fetch(Cell),
            service:show(State#state.view, Result),
            {ret,State#state{}};
        linear({wrapper,{event,close,V}}, State=#state{}) ->
            service:close(V), {ret,State};
        linear(Envelope={wrapper,_},State=#state{}) ->
            service:warn(State#state.store,"bad",[Envelope]), {ret,State}.
        keys() -> [one,two].
        overlap({wrapper,{event,open,V}},State) when is_atom(V) ->
            service:open(V), {ret,State};
        overlap({wrapper,{event,open,V}},State) ->
            service:tune(V,State), {ret,State};
        overlap({wrapper,{event,close,V}},State) ->
            service:close(V), {ret,State};
        overlap(Envelope={wrapper,_},State) ->
            service:unknown(Envelope), {ret,State}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_nested_record_selector_column.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        if (!BeforeFrame(Body("single")).Contains(SelectValue))
        {
            throw new InvalidOperationException(MissingColumn);
        }

        byte[] union = Body("unions");
        if (!BeforeFrame(union).Contains(SelectTupleArity) || union.Count(opcode => opcode == SelectValue) < 2)
        {
            throw new InvalidOperationException(MissingWidth);
        }

        byte[] finite = Body("finite");
        if (finite.Count(opcode => opcode == SelectValue) != 2 || finite[^1] != CaseEnd)
        {
            throw new InvalidOperationException(MissingFiniteCases);
        }

        byte[] linear = Body("linear");
        if (!linear.Contains(TrimFrame) || !linear.Contains(Swap) || !linear.Contains(AllocateHeap))
        {
            throw new InvalidOperationException(MissingChain);
        }

        if (BeforeFrame(Body("overlap")).Contains(SelectValue))
        {
            throw new InvalidOperationException(SharedOverlap);
        }

        byte[] Body(string name)
        {
            var selected = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(selected, "FirstInstructionIndex");
            int end = (int)Property(selected, "EndInstructionIndex");

            return instructions[start..end]
                .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        }
    }

    private static IEnumerable<byte> BeforeFrame(IEnumerable<byte> instructions) =>
        instructions.TakeWhile(opcode => opcode is not (Allocate or AllocateHeap));
}
