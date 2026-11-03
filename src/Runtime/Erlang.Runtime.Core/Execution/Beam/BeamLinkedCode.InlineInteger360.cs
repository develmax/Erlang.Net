using System.Collections.Frozen;
using System.Numerics;

namespace Erlang;

internal sealed partial record BeamLinkedCode
{
    private FrozenDictionary<BigInteger, Integer>? inlineIntegerLiterals360;

    internal Integer[] CaptureInlineIntegerTemplates360()
        => Instructions.SelectMany(instruction => instruction.Operands)
            .Where(operand => operand.Kind == BeamOperandParameters.Integer)
            .Select(operand => operand.Value).Distinct()
            .Select(value => integerConstants.GetOrAdd(value, static number => new Integer(number)))
            .Where(value => !ProcessTermHeap506.IsImmediateInteger559(value)).ToArray();

    internal void BindNativeInlineIntegers360(FrozenDictionary<BigInteger, Integer> actual, ModuleLiteralDomain800 domain)
    {
        Integer[] expected = CaptureInlineIntegerTemplates360();
        if (actual.Count != expected.Length)
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
        foreach (Integer template in expected)
            if (!actual.TryGetValue(template.Value, out Integer? value)
                || !domain.IsPreparedLiteral800(value) || value.Value != template.Value)
                throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
        if (Interlocked.CompareExchange(ref inlineIntegerLiterals360, actual, null) is not null)
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
    }

    private Integer ReadIntegerConstant360(BigInteger value)
        => Volatile.Read(ref inlineIntegerLiterals360) is { } actual && actual.TryGetValue(value, out Integer? literal)
            ? literal : integerConstants.GetOrAdd(value, static number => new Integer(number));
}
