using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedResultPayloadChecks
{
    internal const string TestName = "compiler/beam/shares-the-checked-call-result-payload";
    internal const string HeaderHeapTestName = "compiler/beam/packs-the-terminal-header-list-heap";
    private const string HeaderHeapError = "The terminal header list did not use its single live heap reservation.";
    internal const string ConstructionTrimTestName = "compiler/beam/trims-owned-terminal-construction-frames";
    private const string ConstructionTrimError = "A terminal construction retained dead slots across a call or dropped a used slot.";
    internal const string OwnedCallTestName = "compiler/beam/clears-projected-call-arguments-and-orders-owned-failures";
    private const string OwnedCallError = "The checked result kept a consumed call slot or misplaced an owned failure.";
    private const string Module = "beam_checked_result_payload";
    private const string ShapeError = "The checked result payload was not shared within its owning branches.";

    private const string Source = """
        -module(beam_checked_result_payload).
        -export([read/1]).
        -record(request, {data, config, uri}).
        -record(file_info, {size, type, access, atime, mtime, ctime, mode,
                            links, major_device, minor_device, inode, uid, gid}).

        read(Info = #request{}) -> result(Info).

        result(Info) ->
            Path = mod_alias:path(Info#request.data, Info#request.config, Info#request.uri),
            Suffix = httpd_util:strip_extension_dot(Path),
            case file:read_file_info(Path) of
                {ok, #file_info{type = directory}} ->
                    Status = httpd_file:handle_error(eacces, "access", Info, Path),
                    {answer, [{status,Status}|Info#request.data]};
                {ok, FileInfo} ->
                    Mime = httpd_util:lookup_mime_default(Info#request.config, Suffix, "text/plain"),
                    Length = io_lib:write(FileInfo#file_info.size),
                    Head = [{content_type,Mime},{content_length,Length},{code,200}],
                    {answer,[{response,{response,Head,nobody}}|Info#request.data]};
                {error, Reason} ->
                    Status = httpd_file:handle_error(Reason, "access", Info, Path),
                    {answer, [{status,Status}|Info#request.data]}
            end.
        """;

    internal static void Run()
    {
        const string pairTag = "{ok, ";
        string alternate = Source.Replace(pairTag, "{ok, acknowledged, ", StringComparison.Ordinal)
            .Replace("{error, ", "{error, acknowledged, ", StringComparison.Ordinal);
        if (!HasSharedSelection(Source) || HasSharedSelection(alternate))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    internal static void CheckOwnedCallsAndFailures()
    {
        const string original = "{answer,[{response,{response,Head,nobody}}|Info#request.data]}";
        string alternate = Source.Replace(original,
            "{answer,[{response,{response,Head,nobody}},{kept,Suffix}|Info#request.data]}", StringComparison.Ordinal);
        object[] same = ReadInstructions(Source);
        object[] retained = ReadInstructions(alternate);
        int caseEnd = Array.FindIndex(same, instruction => Opcode(instruction) == CaseEnd);
        int badRecord = Array.FindLastIndex(same, instruction => Opcode(instruction) == BadRecord);
        if (alternate == Source || !HasClearedArguments(same) || HasClearedArguments(retained)
            || badRecord < 0 || caseEnd <= badRecord)
        {
            throw new InvalidOperationException(OwnedCallError);
        }
    }

    private static bool HasClearedArguments(object[] instructions)
    {
        for (int index = 0; index + 4 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == GetTupleElement
                && Opcode(instructions[index + 1]) == Move && Value(Operands(instructions[index + 1])[1]) == 2
                && Opcode(instructions[index + 2]) == Move && Value(Operands(instructions[index + 2])[1]) == 1
                && Opcode(instructions[index + 3]) == InitYRegisters && Operands(instructions[index + 3]).Length == 3)
            {
                return true;
            }
        }

        return false;
    }

    private static object[] ReadInstructions(string source) =>
        Children(Code(CodeGeneration.CompileBeamModule(source, Module + ".erl")), "Instructions");

    internal static void CheckConstructionTrim()
    {
        const string original = "{answer,[{response,{response,Head,nobody}}|Info#request.data]}";
        string alternate = Source.Replace(original,
            "{answer,[{response,{response,Head,nobody}},{kept,FileInfo}|Info#request.data]}", StringComparison.Ordinal);
        if (alternate == Source || CountTrims(Source, [3, 1]) != 2 || CountTrims(Source, [2, 2]) != 1
            || CountTrims(alternate, [2, 2]) != 0)
        {
            throw new InvalidOperationException(ConstructionTrimError);
        }
    }

    private static int CountTrims(string source, int[] operands)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));

        return Children(code, "Instructions").Count(instruction => Opcode(instruction) == TrimFrame
            && Operands(instruction).Select(Value).SequenceEqual(operands));
    }

    internal static void CheckHeaderHeap()
    {
        const string defaultTail = "{code,200}";
        string alternate = Source.Replace(defaultTail, "{variant,Mime}, {code,200}", StringComparison.Ordinal);
        if (alternate == Source || !HasPackedHeaders(Source) || HasPackedHeaders(alternate))
        {
            throw new InvalidOperationException(HeaderHeapError);
        }
    }

    private static bool HasPackedHeaders(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        for (int index = 1; index + 4 < instructions.Length; index++)
        {
            if (Opcode(instructions[index - 1]) == ExternalCall && Opcode(instructions[index]) == TestHeap
                && Operands(instructions[index]).Select(Value).SequenceEqual([22, 1])
                && IsTaggedPair(instructions[index + 1], "content_length", 0)
                && Opcode(instructions[index + 2]) == PutList
                && IsTaggedPair(instructions[index + 3], "content_type", 1)
                && Opcode(instructions[index + 4]) == PutList)
            {
                return true;
            }
        }

        return false;

        bool IsTaggedPair(object instruction, string tag, int register) =>
            Opcode(instruction) == PutTuple
            && Operands(instruction) is [var destination, _, var atom, _]
            && Value(destination) == register && atoms[Value(atom)] == tag;
    }

    private static bool HasSharedSelection(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        for (int index = 0; index + 6 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == IsTuple
                && Opcode(instructions[index + 1]) == TestArity
                && Operands(instructions[index + 1]).Select(Value).Last() == 2
                && Opcode(instructions[index + 2]) == GetTupleElement
                && Opcode(instructions[index + 3]) == GetTupleElement
                && Operands(instructions[index + 3]) is [var tuple, var field, var saved]
                && Value(field) == 1 && Value(tuple) == 0
                && Opcode(instructions[index + 4]) == SelectValue
                && Opcode(instructions[index + 5]) == Label
                && Opcode(instructions[index + 6]) == IsTaggedTuple
                && Operands(instructions[index + 6])[1].Equals(saved))
            {
                return true;
            }
        }

        return false;
    }

    private static object[] Operands(object instruction) => Children(instruction, "Operands");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
