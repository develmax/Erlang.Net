using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Start, int Insert, bool Initialize, int[] Existing, BeamOperand[] Reads)?
        ReadProjectedArgumentCallSlotUses(
            IReadOnlyList<BeamInstruction> body,
            int entry,
            int call
        )
    {
        if (body[call] is not
            {
                Definition.Opcode: ExternalCall,
                Operands: [var arity, _]
            } || arity.Value < 3
            || body[call + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null }]
            } || !IsBareX0(result))
        {
            return null;
        }

        int index = call - 1;
        while (index > entry && body[index].Definition.Opcode == Line)
        {
            index--;
        }

        bool initialize = body[index].Definition.Opcode == InitYRegisters;
        int insert = initialize ? index : index + 1;
        int[] existing = initialize
            ? body[index--].Operands.Skip(1).Select(operand => (int)operand.Value).ToArray()
            : [];
        if (body[index] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, { Kind: BeamOperandParameters.X }]
            })
        {
            return null;
        }

        var reads = new HashSet<BeamOperand>();
        for (; index > entry && body[index] is
        {
            Definition.Opcode: Move,
            Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } target]
        }; index--)
        {
            if (target.Value >= arity.Value)
            {
                return null;
            }

            if (source.Kind == BeamOperandParameters.Y)
            {
                reads.Add(source);
            }
        }

        if (index <= entry + 1 || reads.Count < 2 || body[index] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } tuple,
                    var secondField, { Kind: BeamOperandParameters.X, TypeIndex: null } secondDestination]
            } || body[index - 1] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var sameTuple, var firstField,
                { Kind: BeamOperandParameters.X, TypeIndex: null } firstDestination]
            } || sameTuple != tuple || firstField == secondField || firstDestination == secondDestination
            || firstDestination.Value >= arity.Value || secondDestination.Value >= arity.Value
            || body[index - 2] is not { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, _] })
        {
            return null;
        }

        // Projection and argument setup have already read the initialized slots.
        // The caller will clear only roots proved dead on every continuation.
        reads.Add(tuple);

        return (index - 1, insert, initialize, existing, reads.ToArray());
    }
}
