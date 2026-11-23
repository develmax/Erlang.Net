using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TerminalMapDefaultPlan(
        int Map,
        (int? Register, Term? Literal) Key,
        (int? Register, Term? Literal) Default,
        (Atom Tag, int Arity, int Index)? Projection
    );

    private TerminalMapDefaultPlan? ReadTerminalMapDefault(
        FunctionDefinition function,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!CoreMapCallsEnabled || function.Clauses is not
            [
            { Guard: null, Body: Expr.Call { DynamicRemote: false, Function: MapModuleNames.Get } source } clause
            ]
            || ResolveImportedCall(source, entries) is not
            {
                Module: MapModuleNames.Module, Arguments: [var key, Expr.Variable map, var fallback]
            })
        {
            return null;
        }

        var parameters = new Dictionary<string, int>(StringComparer.Ordinal);
        (Atom Tag, int Arity, int Index)? projection = null;
        for (int register = 0; register < clause.Patterns.Count; register++)
        {
            if (clause.Patterns[register] is Pattern.Variable parameter)
            {
                if (parameter.Name != VariableScopeNames.Wildcard && !parameters.TryAdd(parameter.Name, register))
                {
                    return null;
                }

                continue;
            }

            if (projection is not null || clause.Patterns[register] is not
                Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple)
            {
                return null;
            }

            int fieldIndex = -1;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is not Pattern.Variable field)
                {
                    return null;
                }

                if (field.Name != VariableScopeNames.Wildcard)
                {
                    if (field.Name != map.Name || fieldIndex >= 0 || !parameters.TryAdd(field.Name, register))
                    {
                        return null;
                    }

                    fieldIndex = index;
                }
            }

            if (fieldIndex < 0)
            {
                return null;
            }

            projection = (tag, tuple.Items.Count, fieldIndex);
        }

        if (!parameters.TryGetValue(map.Name, out int mapRegister)
            || ReadOperand(key) is not { } keyOperand || ReadOperand(fallback) is not { } defaultOperand)
        {
            return null;
        }

        return new TerminalMapDefaultPlan(
            mapRegister,
            keyOperand,
            defaultOperand,
            projection
        );

        (int? Register, Term? Literal)? ReadOperand(Expr expression)
        {
            if (expression is Expr.Variable variable && parameters.TryGetValue(variable.Name, out int register))
            {
                return (register, null);
            }

            return AttributeLiteral.TryNormalizeConstant(expression, out Term literal) ? (null, literal) : null;
        }
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTerminalMapDefault(FunctionDefinition function)
        {
            if (recursiveClosure is not null || owner.ReadTerminalMapDefault(function, entries) is not { } plan)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            if (plan.Projection is { } field)
            {
                owner.EmitRecordTest(
                    functionInfo,
                    XRegisterTag,
                    plan.Map,
                    field.Tag,
                    field.Arity
                );
                owner.EmitTuplePart(plan.Map, field.Index, plan.Map);
            }

            int absent = Label();
            int badMap = Label();
            EmitMapTypeTest(plan.Map, badMap);
            owner.Op(GetMapElements, LabelTag, absent);
            if (owner.SsaTypeInferenceEnabled)
            {
                owner.EmitTypedRegister(plan.Map, MapTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, plan.Map);
            }

            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            WriteKey(plan.Key);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            Mark(absent);
            if (plan.Default.Register is int register)
            {
                if (register != 0)
                {
                    owner.MoveToX0(XRegisterTag, register);
                }
            }
            else
            {
                owner.EmitConstant(plan.Default.Literal!, 0);
            }

            owner.instructions.WriteByte(Return);

            // This synthetic match failure has no Core source annotation.
            Mark(badMap);
            EmitBadMapTuple(plan.Map);
            owner.EmitLine(default);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangModuleNames.Error, 1));

            return true;

            void WriteKey((int? Register, Term? Literal) operand)
            {
                if (operand.Register is int key)
                {
                    owner.Operand(XRegisterTag, key);
                }
                else
                {
                    owner.LiteralOperand(operand.Literal!);
                }
            }
        }
    }
}
