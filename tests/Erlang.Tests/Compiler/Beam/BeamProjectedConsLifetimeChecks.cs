using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedConsLifetimeChecks
{
    internal const string TestName = "compiler/beam/keeps-checked-cons-projections-and-owned-call-results";
    private const string Fixture = "beam_projected_cons_lifetimes.erl";
    private const string WrongConstruction = "The checked singleton construction lost the input record or reconstructed the preserved call result.";
    private const string WrongCleanup = "The last cleanup call did not release the dead frame slot.";
    private const string WrongProjection = "The cons search projected its return value before a potentially failing key comparison.";
    private const string WrongCounter = "The guarded cons counter allocated a frame around a leaf arithmetic operation.";
    private const string WrongTupleChain = "The singleton argument no longer folds its tuple literal into the parent constructor.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_projected_cons_lifetimes).
        -export([add/3,cut/1,shape/1,counter/1,name/2,lookup/2,receive_spec/2,announce/3,scan/1,release/2,erase_name/2]).

        add(First, Second, Third) ->
            Source = ?MODULE:scan(queue),
            Id = max_id(Source, 3),
            [{Priority,_,_}] = ?MODULE:lookup(index,priority),
            Built = erlang:make_ref(),
            Options = [{level,Priority},{data,Third},fixed,more],
            Spec = {{identifier,Id},{provider,build,[Priority,First,Built,Options]},dynamic,300},
            case ?MODULE:receive_spec(queue,Spec) of
                {accepted,Child} ->
                    ?MODULE:announce(First,Second,Child),
                    {accepted,Child};
                Error -> Error
            end.

        cut(Pid) ->
            case scan_name(?MODULE:scan(queue), Pid) of
                absent -> missing;
                Name ->
                    ?MODULE:release(queue,Name),
                    ?MODULE:erase_name(queue,Name),
                    removed
            end.

        shape([Children]) ->
            Prefix = {mode,42},
            {package,{Children,Prefix}}.

        counter(Items) -> max_id(Items,3).
        name(Items, Key) -> scan_name(Items, Key).
        max_id([{_,{number,N},_} | Tail], Current) when N >= Current -> max_id(Tail,N+2);
        max_id([_|Tail], Current) -> max_id(Tail,Current);
        max_id([], Current) -> Current.

        scan_name([{Key,_,Name} | _], Key) -> Name;
        scan_name([_|Tail], Key) -> scan_name(Tail,Key);
        scan_name([], _) -> absent.

        scan(_Key) -> erlang:get(collection).
        lookup(_,_) -> erlang:get(index).
        receive_spec(Key, Spec) -> observe({spec,Key,Spec}), erlang:get(result).
        announce(P,S,C) -> observe({announce,P,S,C}).
        release(Q,N) -> observe({release,Q,N}).
        erase_name(Q,N) -> observe({erase,Q,N}).

        observe(Event) ->
            Previous = case erlang:get(observations) of undefined -> []; Other -> Other end,
            erlang:put(observations,[Event|Previous]),ok.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] constructor = Operations("add", 3);
        int nilTest = Array.FindIndex(constructor, instruction => Opcode(instruction) == IsNil);
        int tupleRead = Array.FindIndex(constructor, instruction => Opcode(instruction) == GetTupleElement);
        if (nilTest < 0 || tupleRead <= nilTest || !constructor.Any(instruction => Opcode(instruction) == TrimFrame)
            || constructor.Count(instruction => Opcode(instruction) == PutTuple) != 5)
        {
            throw new InvalidOperationException(WrongConstruction);
        }

        byte[] cleanup = Operations("cut", 1).Select(Opcode).ToArray();
        if (!cleanup.Contains(InitYRegisters) || !cleanup.Contains(TrimFrame))
        {
            throw new InvalidOperationException(WrongCleanup);
        }

        byte[] projection = Operations("scan_name", 2).Select(Opcode).ToArray();
        if (Array.IndexOf(projection, ExactEqualTest) < 0
            || Array.LastIndexOf(projection, GetTupleElement) <= Array.IndexOf(projection, ExactEqualTest))
        {
            throw new InvalidOperationException(WrongProjection);
        }

        byte[] counter = Operations("max_id", 2).Select(Opcode).ToArray();
        if (counter.Contains(Allocate) || !counter.Contains(GcBifTwo) || !counter.Contains(TailLocalCall))
        {
            throw new InvalidOperationException(WrongCounter);
        }

        byte[] chain = Operations("shape", 1).Select(Opcode).ToArray();
        if (chain.Count(opcode => opcode == PutTuple) != 2 || chain.Contains(Move))
        {
            throw new InvalidOperationException(WrongTupleChain);
        }

        return Task.CompletedTask;

        object[] Operations(string name, int arity)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(Missing);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
