using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record EntryTupleMaskGuard(
            Pattern.Variable[] Parameters,
            Pattern.Variable[] Fields,
            int Tuple,
            int Scalar,
            Integer TupleMask,
            Integer ScalarMask,
            Expr FirstTest
        );

        private bool TryEmitEntryTupleMaskTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Body: Expr.Call { Module: not (null or ErlangModuleNames.Module) } call } clause]
                || ReadEntryTupleMaskGuard(function, clause) is not { } plan
                || call.Arguments.Count != plan.Parameters.Length
                || ReadEntryCall(call, plan.Parameters.Select(parameter => parameter.Name).ToArray()) is not
                { Module: { } module, Moves.Count: 0, Constants.Count: 0 })
            {
                return false;
            }

            var previous = EmitEntryTupleMaskGuard(plan, owner.nextLabel - 2);
            owner.EmitCallLine(call, previous);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, call.Arguments.Count));

            return true;
        }

        private bool TryEmitEntryTupleMaskCaseGuard(
            FunctionDefinition function,
            Clause clause,
            int failure,
            out Clause checkedClause
        )
        {
            checkedClause = clause;
            if (clause.Body is not Expr.Case
                {
                    Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false }
                }
                || ReadEntryTupleMaskGuard(function, clause) is not { } plan
                || ReadUsedEntryParameters(new Clause([.. plan.Parameters, .. plan.Fields], null, clause.Body)) is not { } used
                || plan.Fields.Any(field => used.Contains(field.Name)))
            {
                return false;
            }

            // Only the whole caller arguments reach the body. The checked
            // field values and accumulators are temporary X operands.
            EmitEntryTupleMaskGuard(plan, failure);
            checkedClause = clause with { Patterns = plan.Parameters, Guard = null };

            return true;
        }

        private EntryTupleMaskGuard? ReadEntryTupleMaskGuard(FunctionDefinition function, Clause clause)
        {
            if (recursiveClosure is not null || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity + 1 >= FirstReservedXRegister || function.Clauses.Count != 1
                || clause.Guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } guard
                || ReadZeroMask(guard.Left) is not { } tupleMask
                || ReadZeroMask(guard.Right) is not { Input: Expr.Variable scalar } scalarMask)
            {
                return null;
            }

            var parameters = new Pattern.Variable[function.Arity];
            Pattern.Variable[]? fields = null;
            int tuple = -1;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable variable)
                {
                    parameters[index] = variable;
                    continue;
                }

                if (tuple >= 0 || clause.Patterns[index] is not AliasPattern alias
                    || !TryReadPatternAlias(alias, out Pattern.Variable binding, out Pattern shape)
                    || shape is not Pattern.Tuple { Items.Count: >= 2 and < CoreAliasDepthLimit } members
                    || members.Items.Any(item => item is not global::Erlang.Pattern.Variable))
                {
                    return null;
                }

                parameters[index] = binding;
                fields = members.Items.Cast<Pattern.Variable>().ToArray();
                tuple = index;
            }

            int scalarRegister = Array.FindIndex(parameters, parameter => parameter.Name == scalar.Name);
            if (fields is null || scalarRegister < 0 || scalarRegister == tuple
                || !DistinctBindings([.. parameters, .. fields])
                || !IsOrderedFold(tupleMask.Input, fields, fields.Length - 1))
            {
                return null;
            }

            return new(
                parameters,
                fields,
                tuple,
                scalarRegister,
                tupleMask.Mask,
                scalarMask.Mask,
                guard.Left
            );
        }

        private BeamSourceLocation EmitEntryTupleMaskGuard(EntryTupleMaskGuard plan, int failure)
        {
            int accumulator = plan.Parameters.Length;
            int field = accumulator + 1;
            owner.EmitTupleCheck(plan.Tuple, plan.Fields.Length, failure);
            owner.EmitTuplePart(plan.Tuple, 0, accumulator);
            int union = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseOr, 2);
            for (int index = 1; index < plan.Fields.Length; index++)
            {
                owner.EmitTuplePart(plan.Tuple, index, field);
                EmitGuardBif(failure, field + 1, union);
                if (index == 1)
                {
                    owner.Operand(XRegisterTag, accumulator);
                }
                else
                {
                    owner.EmitTypedRegister(accumulator, IntegerTypeBits);
                }

                owner.Operand(XRegisterTag, field);
                owner.Operand(XRegisterTag, accumulator);
            }

            var previous = location;
            if (plan.FirstTest is Expr.Binary { SourceLine: > 0 } maskCheck)
            {
                var maskLocation = new BeamSourceLocation(location.File, maskCheck.SourceLine);
                if (maskLocation != previous)
                {
                    owner.EmitLine(maskLocation);
                    previous = maskLocation;
                }
            }

            int mask = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2);
            EmitGuardBif(0, accumulator + 1, mask);
            owner.EmitTypedRegister(accumulator, IntegerTypeBits);
            owner.LiteralOperand(plan.TupleMask);
            EmitMaskResult();

            // The scalar is an untouched entry argument. A non-integer still
            // follows the original clause failure edge.
            EmitGuardBif(failure, accumulator, mask);
            owner.Operand(XRegisterTag, plan.Scalar);
            owner.LiteralOperand(plan.ScalarMask);
            EmitMaskResult();

            return previous;

            void EmitGuardBif(int rejected, int live, int import)
            {
                owner.Op(GcBifTwo, LabelTag, rejected);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, import);
            }

            void EmitMaskResult()
            {
                owner.Operand(XRegisterTag, accumulator);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.EmitTypedRegister(accumulator, IntegerTypeBits);
                owner.Operand(BeamWire.IntegerTag, 0);
            }
        }
    }
}
