using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaughtListHeadEntryChecks
{
    internal const string TestName = "compiler/beam/checks-caught-list-heads-before-allocating-the-frame";
    internal const string SharedPayloadTestName = "compiler/beam/shares-a-checked-pair-after-closing-catch";
    private const string SharedPayloadError = "The closed catch did not share the selected pair payload.";
    private const string Module = "beam_caught_list_head_entry";
    private const string ShapeError = "A checked entry head retained its framed failure relay.";

    private const string Source = """
        -module(beam_caught_list_head_entry).
        -export([checked/3, plain/3]).

        checked(Options = [{_,_}|_], Timeout, Debug) ->
            case catch service:validate(Options) of
                {ok, Checked} ->
                    Address = proplists:get_value(address, Checked),
                    Port = proplists:get_value(port, Checked),
                    Profile = proplists:get_value(profile, Checked, default),
                    Name = service:name(Address, Port, Profile),
                    service:start(Name, [Checked,Timeout,Debug,Address,Port]);
                {error, Reason} ->
                    service:report(Reason),
                    {stop,Reason}
            end.

        plain(Options = [_|_], Timeout, Debug) ->
            case catch service:validate(Options) of
                {ok, Checked} -> service:start(Checked, [Timeout,Debug]);
                {error, Reason} -> {stop,Reason}
            end.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasCheckedPrefix("checked") || HasCheckedPrefix("plain"))
        {
            throw new InvalidOperationException(ShapeError);
        }

        bool HasCheckedPrefix(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Children(instruction, "Operands")[1])] == name);

            return start >= 0 && instructions.Skip(start + 1).Take(6).Select(Opcode)
                .SequenceEqual([Label, IsNonemptyList, GetHead, IsTuple, TestArity, Allocate]);
        }
    }

    internal static void CheckSharedPayload()
    {
        string alternate = Source.Replace("service:report(Reason),", "service:report(refused),", StringComparison.Ordinal)
            .Replace("{stop,Reason}", "{stop,refused}", StringComparison.Ordinal);
        if (alternate == Source || !HasSharedPayload(Source) || HasSharedPayload(alternate))
        {
            throw new InvalidOperationException(SharedPayloadError);
        }
    }

    private static bool HasSharedPayload(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        for (int index = 0; index + 5 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == CatchEnd && Opcode(instructions[index + 1]) == IsTuple
                && Opcode(instructions[index + 2]) == TestArity && Opcode(instructions[index + 3]) == GetTupleElement
                && Opcode(instructions[index + 4]) == GetTupleElement && Opcode(instructions[index + 5]) == SelectValue
                && Children(instructions[index + 4], "Operands")[^1].Equals(Children(instructions[index], "Operands")[0]))
            {
                return true;
            }
        }

        return false;
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
