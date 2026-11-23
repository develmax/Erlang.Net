using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalTupleCaseChecks
{
    internal const string TestName = "compiler/beam/places-terminal-tuple-constructions-on-case-edges";
    private const string IncorrectLayout = "The terminal tuple must be constructed independently on each successful edge.";
    private const string IncorrectResult = "The terminal case changed a value, callback, or error.";
    private const string Source = """
        -module(beam_terminal_tuple_case).
        -export([make/2, keep_map/3, wide/3]).

        make(M, Fallback) ->
            fun({field, Key}) ->
                    {field, case M of
                                #{Key := Found} -> Found;
                                _ -> Fallback(Key)
                            end};
               (Other) -> Other
            end.

        keep_map(Key, M, Fallback) ->
            {M, case M of
                    #{Key := Found} -> Found;
                    _ -> Fallback(Key)
                end}.

        wide(Key, M, Fallback) ->
            {one, two, three, case M of
                                  #{Key := Found} -> Found;
                                  _ -> Fallback(Key)
                              end}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_terminal_tuple_case.erl");
        object code = Code(bytes);
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        object[] body = ReadFunction(name => name.StartsWith("-make/2-", StringComparison.Ordinal));
        int call = Array.FindIndex(body, operation => Opcode(operation) == CallFun);
        int heap = Array.FindIndex(body, operation => Opcode(operation) == TestHeap);
        object[] frames = body.Where(operation => Opcode(operation) is Allocate or AllocateHeap).ToArray();
        if (body.Length < 4 || Opcode(body[0]) != IsTaggedTuple
            || Opcode(body[1]) != GetTupleElement || Opcode(body[2]) != IsMap || Opcode(body[3]) != GetMapElements
            || call < 0 || heap < 4 || frames is not [var allocation] || OperandValue(allocation, 0) != 0
            || Array.IndexOf(body, allocation) <= heap || Array.IndexOf(body, allocation) >= call
            || body.Count(operation => Opcode(operation) == PutTuple) != 2)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] kept = ReadFunction(name => name == "keep_map");
        object extraction = kept.Single(operation => Opcode(operation) == GetMapElements);
        object[] mapOperands = ((IEnumerable)Property(extraction, "Operands")).Cast<object>().ToArray();
        if (OperandValue(extraction, 1) == OperandValue(extraction, mapOperands.Length - 1))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term field = Term.A("field");
            Term key = Term.I(1);
            Term original = Term.Tuple(Term.A("unknown"), key);
            Term found = Term.A("found");
            Term invalid = Term.A("invalid");
            Term tagged = Term.Tuple(field, key);
            var good = new MapTerm([new(key, found)]);
            var empty = new MapTerm([]);
            int calls = 0;
            var callback = new FunctionTerm(1, (_, arguments) =>
            {
                calls++;

                return ValueTask.FromResult(arguments[0]);
            });

            if (!Term.Tuple(field, found).Equals(await Apply(good, invalid, tagged))
                || !original.Equals(await Apply(empty, invalid, original))
                || calls != 0
                || !Term.Tuple(field, key).Equals(await Apply(empty, callback, tagged))
                || calls != 1
                || !Term.Tuple(field, key).Equals(await Apply(Nil.Value, callback, tagged))
                || calls != 2
                || !Term.Tuple(good, found).Equals(await image.Invoke(
                    context,
                    "keep_map",
                    key,
                    good,
                    invalid
                ))
                || !Term.Tuple(
                    Term.A("one"),
                    Term.A("two"),
                    Term.A("three"),
                    key
                ).Equals(await image.Invoke(
                    context,
                    "wide",
                    key,
                    empty,
                    callback
                ))
                || calls != 3)
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            try
            {
                await Apply(empty, invalid, tagged);
                throw new InvalidOperationException(IncorrectResult);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badfun"), invalid)))
            {
            }

            return Nil.Value;

            async ValueTask<Term> Apply(Term map, Term fun, Term input)
            {
                var closure = (FunctionTerm)await image.Invoke(
                    context,
                    "make",
                    map,
                    fun
                );

                return await closure.Invoke(context, [input]);
            }
        });

        object[] ReadFunction(Func<string, bool> matches)
        {
            int info = Array.FindIndex(instructions, operation => Opcode(operation) == FunctionInfo
                && matches(atoms[(int)OperandValue(operation, 1)]));
            if (info < 0)
            {
                throw new InvalidOperationException(IncorrectLayout);
            }

            return instructions.Skip(info + 2).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        }
    }

    private static BigInteger OperandValue(object instruction, int position) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(position), "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}
