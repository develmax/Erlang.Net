using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedSupervisorTailsChecks
{
    internal const string TestName = "compiler/beam/keeps-source-owned-supervisor-arguments-and-protected-results";
    private const string Fixture = "beam_owned_supervisor_tails.erl";
    private const string WrongUnwrap = "The argument list used a stack slot or lost its nested result checks.";
    private const string WrongLookup = "The protected key lookup kept unnecessary values across the calls.";
    private const string WrongInit = "The exact list lost its narrowed frame or constructed the maps before the option effect.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_owned_supervisor_tails).
        -export([unpack/3, first/1, second/1, build/1, producer/2, collect/1, note/2, option/5]).

        unpack(A, B, C) ->
            case ?MODULE:producer(unpack, [A,B,C]) of
                {outer,{inner,{bad_child,_,Result}}} -> {returned,Result};
                Other -> Other
            end.

        first(Pid) -> lookup(alpha,Pid).
        second(Pid) -> lookup(beta,Pid).

        lookup(Key, Pid) when is_pid(Pid) ->
            try
                {Key,Value,_,_} = lists:keyfind(Key, 1, ?MODULE:collect(Pid)),
                Value
            catch
                exit:{gone,_} -> {failed,gone};
                _:_ -> {failed,{lookup_failed,scope_label,Key}}
            end.

        build([Role, Id, Socket, Options]) ->
            ?MODULE:note(Role,{link,Socket}),
            Parent = self(),
            Flags = #{policy=>temporary},
            Children = [
                #{id=>worker,recipe=>{worker_mod,run,[Role,Id,Socket,
                    ?MODULE:option(user_key,[{parent,Parent}],Options,source_id,77)]}},
                #{id=>child,recipe=>{child_mod,run,[Options]}},
                #{id=>last,recipe=>{last_mod,run,[]}}],
            {built,{Flags,Children}}.

        producer(unpack, [one,B,C]) -> {outer,{inner,{bad_child,B,C}}};
        producer(unpack, [near,B,C]) -> {outer,{inner,{other_child,B,C}}};
        producer(unpack, Values) -> {direct,Values}.

        collect(Pid) when is_pid(Pid) ->
            case erlang:get(test_branch) of
                gone -> exit({gone,worker});
                empty -> [];
                short -> [{alpha,value}];
                error -> error(deliberate);
                _ -> [{alpha,first_value,one,two},{beta,second_value,three,four}]
            end.

        note(Role, Pair) -> erlang:put(note,{Role,Pair}), ok.
        option(Key, Values, Options, Module, Line) ->
            erlang:put(option,{Key,Values,Options,Module,Line}),
            {new_options,Values,Options}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] unpack = Operations("unpack", 3);
        object? listFrame = unpack.FirstOrDefault(instruction => Opcode(instruction) == AllocateHeap);
        if (listFrame is null || Value(Operands(listFrame)[0]) != 0
            || unpack.Count(instruction => Opcode(instruction) == PutList) != 3
            || unpack.Count(instruction => Opcode(instruction) == IsTaggedTuple) != 3)
        {
            throw new InvalidOperationException(WrongUnwrap);
        }

        object[] lookup = Operations("lookup", 2);
        object? protectedFrame = lookup.FirstOrDefault(instruction => Opcode(instruction) == Allocate);
        if (protectedFrame is null || Value(Operands(protectedFrame)[0]) != 2
            || lookup.Count(instruction => Opcode(instruction) == TryEnter) != 1
            || lookup.Count(instruction => Opcode(instruction) == TryEnd) != 1
            || lookup.Count(instruction => Opcode(instruction) == TryCase) != 1)
        {
            throw new InvalidOperationException(WrongLookup);
        }

        object[] init = Operations("build", 1);
        int body = Array.FindIndex(init, instruction => Opcode(instruction) == AllocateHeap);
        int lateStore = Array.FindIndex(init, instruction => Opcode(instruction) == TrimFrame);
        int firstMap = Array.FindIndex(init, instruction => Opcode(instruction) == PutMapAssoc);
        if (body < 0 || Value(Operands(init[body])[0]) != 5 || lateStore <= body || firstMap <= lateStore
            || init.Take(body).Count(instruction => Opcode(instruction) == GetList) != 4
            || init.Count(instruction => Opcode(instruction) == PutMapAssoc) != 2)
        {
            throw new InvalidOperationException(WrongInit);
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
