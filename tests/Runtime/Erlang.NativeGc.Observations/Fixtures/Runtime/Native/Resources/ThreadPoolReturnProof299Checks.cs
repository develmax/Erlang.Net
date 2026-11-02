using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text.Json;

// Read-only IL inspection. This program never executes the inspected queue method.
namespace Erlang.Tests;

internal static class ThreadPoolReturnProof299Checks
{
    private static readonly Dictionary<ushort, OpCode> Codes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode))
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(c => unchecked((ushort)c.Value));

    internal static void Run()
    {
        var instructions = new List<ThreadPoolReturnProof299Instruction>();
        try
        {
            if (Environment.Version != new Version(10, 0, 11))
                throw new InvalidOperationException(ThreadPoolReturnProof299Names.RuntimeMismatch);
            Assembly core = typeof(ThreadPool).Assembly;
            string path = Path.GetFullPath(core.Location);
            string sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            MethodInfo method = typeof(ThreadPool).GetMethod(
                ThreadPoolReturnProof299Names.MethodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(IThreadPoolWorkItem), typeof(bool)],
                null
            )
                ?? throw new MissingMethodException(ThreadPoolReturnProof299Names.MissingMethod);
            if (method.ReturnType != typeof(bool)) throw new InvalidOperationException(ThreadPoolReturnProof299Names.ReturnType);
            MethodBody body = method.GetMethodBody() ?? throw new InvalidOperationException(ThreadPoolReturnProof299Names.MissingBody);
            if (body.ExceptionHandlingClauses.Count != 0)
                throw new InvalidOperationException(ThreadPoolReturnProof299Names.ExceptionRegion);
            byte[] il = body.GetILAsByteArray() ?? throw new InvalidOperationException(ThreadPoolReturnProof299Names.MissingIl);
            Decode(method, il, instructions);
            var offsets = instructions.Select(i => i.Offset).ToHashSet();
            foreach (ThreadPoolReturnProof299Instruction instruction in instructions)
                foreach (int target in instruction.Targets)
                    if (!offsets.Contains(target)) throw new InvalidOperationException(ThreadPoolReturnProof299Names.InvalidTarget);
            var incoming = instructions.SelectMany(i => i.Targets).ToHashSet();
            int returns = 0;
            for (int index = 0; index < instructions.Count; index++)
            {
                ThreadPoolReturnProof299Instruction instruction = instructions[index];
                if (instruction.Opcode == ThreadPoolReturnProof299Names.JumpOpcode) throw new InvalidOperationException(ThreadPoolReturnProof299Names.JumpRefused);
                if (instruction.Opcode != ThreadPoolReturnProof299Names.ReturnOpcode) continue;
                returns++;
                if (index == 0 || instructions[index - 1].End != instruction.Offset
                    || instructions[index - 1].Opcode != ThreadPoolReturnProof299Names.TrueOpcode || incoming.Contains(instruction.Offset))
                    throw new InvalidOperationException(ThreadPoolReturnProof299Names.NonConstantReturn);
            }
            if (returns == 0) throw new InvalidOperationException(ThreadPoolReturnProof299Names.NoReturns);
            Console.WriteLine(JsonSerializer.Serialize(
                new
                {
                    Status = ThreadPoolReturnProof299Names.ProvedStatus,
                    CoreLib = path,
                    Sha256 = sha,
                    RuntimeVersion = Environment.Version.ToString(),
                    AssemblyVersion = core.GetName().Version?.ToString(),
                    Method = method.ToString(),
                    MethodToken = $"0x{method.MetadataToken:X8}",
                    IlSha256 = Convert.ToHexString(SHA256.HashData(il)),
                    Returns = returns,
                    ExceptionRegions = body.ExceptionHandlingClauses.Count,
                    Instructions = instructions,
                    Scope = ThreadPoolReturnProof299Names.ProofScope
                },
                new JsonSerializerOptions { WriteIndented = true }
            ));

            return;
        }
        catch (Exception error)
        {
            Console.WriteLine(JsonSerializer.Serialize(
                new
                {
                    Status = ThreadPoolReturnProof299Names.RefusedStatus,
                    Error = error.ToString(),
                    Instructions = instructions
                },
                new JsonSerializerOptions { WriteIndented = true }
            ));
            throw new InvalidOperationException(ThreadPoolReturnProof299Names.ProofRefused, error);
        }
    }

    private static void Decode(MethodInfo method, byte[] il, List<ThreadPoolReturnProof299Instruction> result)
    {
        int position = 0;
        while (position < il.Length)
        {
            int offset = position;
            ushort key = il[position++];
            if (key == 0xFE)
            {
                Require(il, position, 1);
                key = (ushort)(0xFE00 | il[position++]);
            }
            if (!Codes.TryGetValue(key, out OpCode code)) throw new InvalidOperationException(ThreadPoolReturnProof299Names.UnknownOpcode + offset);
            string? operand = null;
            int[] targets = [];
            switch (code.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget:
                    Require(il, position, 1);
                    int shortDelta = unchecked((sbyte)il[position++]);
                    targets = [checked(position + shortDelta)];
                    operand = $"IL_{targets[0]:X4}";
                    break;
                case OperandType.InlineBrTarget:
                    int delta = Read32(il, ref position);
                    targets = [checked(position + delta)];
                    operand = $"IL_{targets[0]:X4}";
                    break;
                case OperandType.InlineSwitch:
                    int count = Read32(il, ref position);
                    if (count < 0 || count > (il.Length - position) / 4) throw new InvalidOperationException(ThreadPoolReturnProof299Names.InvalidSwitch);
                    int origin = checked(position + 4 * count);
                    targets = new int[count];
                    for (int i = 0; i < count; i++) targets[i] = checked(origin + Read32(il, ref position));
                    operand = string.Join(ThreadPoolReturnProof299Names.TargetSeparator, targets.Select(t => $"IL_{t:X4}"));
                    break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                    int token = Read32(il, ref position);
                    operand = $"0x{token:X8} " + Resolve(method.Module, code.OperandType, token);
                    break;
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    Require(il, position, 1);
                    operand = il[position++].ToString();
                    break;
                case OperandType.InlineVar:
                    Require(il, position, 2);
                    operand = BitConverter.ToUInt16(il, position).ToString(); position += 2;
                    break;
                case OperandType.InlineI:
                    operand = Read32(il, ref position).ToString(); break;
                case OperandType.InlineI8:
                    Require(il, position, 8); operand = BitConverter.ToInt64(il, position).ToString(); position += 8; break;
                case OperandType.ShortInlineR:
                    Require(il, position, 4); operand = BitConverter.ToSingle(il, position).ToString(ThreadPoolReturnProof299Names.RoundTripFormat); position += 4; break;
                case OperandType.InlineR:
                    Require(il, position, 8); operand = BitConverter.ToDouble(il, position).ToString(ThreadPoolReturnProof299Names.RoundTripFormat); position += 8; break;
                default: throw new InvalidOperationException(ThreadPoolReturnProof299Names.UnsupportedOperand + code.OperandType);
            }
            result.Add(new(
                offset,
                position,
                code.Name ?? throw new InvalidOperationException(ThreadPoolReturnProof299Names.UnnamedOpcode),
                operand,
                targets
            ));
        }
    }

    private static string Resolve(Module module, OperandType kind, int token) => kind switch
    {
        OperandType.InlineString => JsonSerializer.Serialize(module.ResolveString(token)),
        OperandType.InlineSig => Convert.ToHexString(module.ResolveSignature(token)),
        _ => module.ResolveMember(token)?.ToString() ?? throw new InvalidOperationException(ThreadPoolReturnProof299Names.UnresolvedToken)
    };

    private static int Read32(byte[] bytes, ref int position)
    {
        Require(bytes, position, 4);
        int value = BitConverter.ToInt32(bytes, position);
        position += 4;

        return value;
    }

    private static void Require(byte[] bytes, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > bytes.Length - count)
            throw new InvalidOperationException(ThreadPoolReturnProof299Names.TruncatedOperand);
    }
}
