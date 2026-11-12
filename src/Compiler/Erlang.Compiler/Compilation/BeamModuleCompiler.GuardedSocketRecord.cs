using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedSocketRecord(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Patterns: [
                        Pattern.List { Tail: null, Items: [Pattern.Variable pid] }, AliasPattern transportPattern,
                        AliasPattern socketPattern, Pattern.Variable callback, Pattern.Variable table
                    ], Body: Expr.RecordConstruction firstRecord
                } first,
                {
                    Patterns: [
                        Pattern.List { Tail: null, Items: [Pattern.Variable otherPid] }, Pattern.Variable otherTransport,
                        Pattern.Variable otherSocket, Pattern.Variable otherCallback, Pattern.Variable otherTable
                    ], Body: Expr.RecordConstruction secondRecord
                } second
            ] || !TryReadPatternAlias(transportPattern, out var transport, out var literal)
                || literal is not Pattern.Literal { Value: Atom selected }
                || !TryReadPatternAlias(socketPattern, out var socket, out var nested)
                || nested is not Pattern.Tuple { Items: [
                    Pattern.Tuple { Items: [Pattern.Variable host, Pattern.Variable port] }, Pattern.Variable handle
                ] }
                || !DistinctBindings(pid, transport, socket, callback, table)
                || !DistinctBindings(otherPid, otherTransport, otherSocket, otherCallback, otherTable)
                || !DistinctBindings(
                    [pid, transport, socket, callback, table, .. new[] { host, port, handle }
                        .Where(variable => variable.Name != VariableScopeNames.Wildcard)]
                )
                || first.Guard is not Expr.Binary { Operator: ErlangOperators.ExactNotEqual,
                    Left: Expr.Variable firstInput, Right: Expr.Literal { Value: Atom forbidden } }
                || second.Guard is not Expr.Binary { Operator: ErlangOperators.ExactNotEqual,
                    Left: Expr.Variable secondInput, Right: Expr.Literal { Value: Atom otherForbidden } }
                || firstInput.Name != table.Name || secondInput.Name != otherTable.Name || !forbidden.Equals(otherForbidden))
            {
                return false;
            }

            var firstFields = ReadSocketRecordFields(firstRecord, pid, transport, socket, callback, table);
            var secondFields = ReadSocketRecordFields(secondRecord, otherPid, otherTransport, otherSocket, otherCallback, otherTable);
            if (firstFields is null || secondFields is null || !firstFields.SequenceEqual(secondFields))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int general = Label();
            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(0, XRegisterTag, 5, XRegisterTag, 6);
            owner.EmitNilTest(XRegisterTag, 6, failure);
            owner.Op(ExactEqualTest, LabelTag, general);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(selected);
            owner.EmitTupleCheck(2, 2, general);
            owner.EmitTuplePart(2, 0, 6);
            owner.EmitTupleCheck(6, 2, general);
            EmitRecord(selected);
            Mark(general);
            EmitRecord(null);

            return true;

            void EmitRecord(Atom? selectedTransport)
            {
                owner.Op(ExactNotEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 4);
                owner.LiteralOperand(forbidden);
                owner.Op(TestHeap, BeamWire.UnsignedTag, firstFields.Length + 1);
                owner.Operand(BeamWire.UnsignedTag, 6);
                owner.EmitTupleHeader(XRegisterTag, firstFields.Length);
                foreach (var (register, value) in firstFields)
                {
                    if (register == 1 && selectedTransport is not null)
                    {
                        owner.LiteralOperand(selectedTransport);
                    }
                    else if (value is not null)
                    {
                        owner.LiteralOperand(value);
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, register);
                    }
                }

                owner.instructions.WriteByte(Return);
            }
        }

        private static bool TryReadPatternAlias(AliasPattern alias, out Pattern.Variable variable, out Pattern shape)
        {
            if (alias.Right is Pattern.Variable right)
            {
                variable = right;
                shape = alias.Left;

                return true;
            }

            if (alias.Left is Pattern.Variable left)
            {
                variable = left;
                shape = alias.Right;

                return true;
            }

            variable = null!;
            shape = null!;

            return false;
        }

        private static (int Register, Term? Value)[]? ReadSocketRecordFields(
            Expr.RecordConstruction record,
            Pattern.Variable pid,
            Pattern.Variable transport,
            Pattern.Variable socket,
            Pattern.Variable callback,
            Pattern.Variable table
        )
        {
            var registers = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [pid.Name] = 5,
                [transport.Name] = 1,
                [socket.Name] = 2,
                [callback.Name] = 3,
                [table.Name] = 4
            };
            var fields = RecordExpressions.Create(record).Items;
            var result = new (int Register, Term? Value)[fields.Count];
            for (int index = 0; index < fields.Count; index++)
            {
                if (fields[index] is Expr.Variable variable && registers.TryGetValue(variable.Name, out int register))
                {
                    result[index] = (register, null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(fields[index], out Term value))
                {
                    result[index] = (-1, value);
                }
                else
                {
                    return null;
                }
            }

            return result;
        }
    }
}
