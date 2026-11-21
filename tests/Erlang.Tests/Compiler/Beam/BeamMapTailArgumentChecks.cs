using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMapTailArgumentChecks
{
    internal const string TestName = "compiler/beam/updates-map-in-place-for-forwarded-tail-arguments";
    internal const string Fixture = "beam_map_tail_argument.erl";
    private const string MissingInPlaceMap = "Forwarded map update did not retain its argument register in a frameless tail call.";
    private const string UnexpectedFramelessUpdate = "An effectful or reordered map update used the in-place tail path.";

    internal const string Source = """
        -module(beam_map_tail_argument).
        -export([update_last/3, update_first/3, reordered/3, effectful/3]).

        update_last(A, B, M) -> diameter_gen:encode_avps(A, B, M#{tag => A, count => B}).
        update_first(M, A, B) -> diameter_gen:decode_avps(M#{tag => A}, A, B).
        reordered(A, B, M) -> diameter_gen:encode_avps(B, A, M#{tag => A}).
        effectful(A, B, M) -> diameter_gen:encode_avps(A, B, M#{tag => put(map_tail_effect, A)}).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        for (int function = 0; function < 4; function++)
        {
            int first = entries[function];
            int length = entries[function + 1] - first;
            int update = Array.IndexOf(
                opcodes,
                PutMapAssoc,
                first,
                length
            );
            int tail = Array.IndexOf(
                opcodes,
                TailExternalCall,
                first,
                length
            );
            bool frameless = Array.IndexOf(
                opcodes,
                Allocate,
                first,
                length
            ) < 0
                && Array.IndexOf(
                    opcodes,
                    AllocateHeap,
                    first,
                    length
                ) < 0;
            if (function < 2)
            {
                if (update < first || tail <= update || !frameless)
                {
                    throw new InvalidOperationException(MissingInPlaceMap);
                }

                object[] operands = ((IEnumerable)Property(instructions[update], "Operands")).Cast<object>().ToArray();
                BigInteger register = function == 0 ? 2 : 0;
                if ((BigInteger)Property(operands[1], "Value") != register || (BigInteger)Property(operands[2], "Value") != register)
                {
                    throw new InvalidOperationException(MissingInPlaceMap);
                }
            }
            else if (update >= first && frameless && tail > update)
            {
                throw new InvalidOperationException(UnexpectedFramelessUpdate);
            }
        }

        return Task.CompletedTask;
    }
}
