using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAppendTupleBranches(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Body: Expr.Tuple { Items: [Expr.Variable first, var literal, Expr.Call left] } } simple,
                { Guard: null, Body: Expr.Tuple { Items: [Expr.Variable second,
                    Expr.RecordAccess { Value: Expr.Variable record } field, Expr.Call right] } } checkedClause
            ] || !HasUnconditionalParameters(checkedClause)
                || !AttributeLiteral.TryNormalizeConstant(literal, out Term middle)
                || simple.Patterns.Any(pattern => pattern is not (Erlang.Pattern.Literal or Erlang.Pattern.Variable))
                || simple.Patterns.Where(pattern => pattern is Pattern.Literal).Count() != 1)
            {
                return false;
            }

            int selector = Array.FindIndex(simple.Patterns.ToArray(), pattern => pattern is Pattern.Literal);
            var test = (Pattern.Literal)simple.Patterns[selector];
            if (test.Value is not Atom || test.Value.Equals(Term.A(field.Name))
                || checkedClause.Patterns[selector] is not Pattern.Variable named || named.Name != record.Name
                || record.Name == VariableScopeNames.Wildcard
                || !HasUnconditionalParameters(simple with
                    { Patterns = simple.Patterns.Select((pattern, index) => index == selector
                        ? new Pattern.Variable(VariableScopeNames.Wildcard) : pattern).ToArray() })
                || ReadAppendTupleBranch(simple, first, left) is not { } firstPlan
                || ReadAppendTupleBranch(checkedClause, second, right) is not { } secondPlan)
            {
                return false;
            }

            int recordBranch = Label();
            int badRecord = Label();
            owner.Op(ExactEqualTest, LabelTag, recordBranch);
            owner.Operand(XRegisterTag, selector);
            owner.LiteralOperand(test.Value);
            EmitBranch(firstPlan, null);
            Mark(recordBranch);
            owner.EmitRecordTest(badRecord, XRegisterTag, selector, Term.A(field.Name), field.Arity);
            EmitBranch(secondPlan, field);
            Mark(badRecord);
            owner.EmitLine(field.SourceLine == 0 ? location : new(field.PreprocessorFile, field.SourceLine));
            owner.Op(BadRecord, XRegisterTag, selector);
            return true;

            void EmitBranch(AppendTupleBranch plan, Expr.RecordAccess? projection)
            {
                bool keepsRecord = projection is not null;
                int frame = keepsRecord ? 3 : 2;
                int appendSlot = keepsRecord ? 1 : 0;
                EmitFrameAllocation(plan.Live, frame);
                if (keepsRecord)
                {
                    owner.MoveRegister(XRegisterTag, selector, YRegisterTag, 0);
                }

                owner.MoveRegister(XRegisterTag, plan.Retained, YRegisterTag, appendSlot);
                owner.MoveRegister(XRegisterTag, plan.Returned, YRegisterTag, appendSlot + 1);
                EmitEntryCallArguments(plan.Inner);
                var previous = location;
                owner.EmitArithmeticLine(plan.InnerLine, ref previous);
                Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);

                foreach (var step in plan.Steps)
                {
                    if (step.Prefix is { } prefix)
                    {
                        EmitAppendPrefixInX1(prefix);
                    }
                    else
                    {
                        owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
                    }

                    if (step.Left is { } constant)
                    {
                        owner.EmitConstant(constant, 0);
                    }
                    else
                    {
                        Load(appendSlot);
                        if (keepsRecord)
                        {
                            owner.InitStackSlot(appendSlot);
                        }
                        else
                        {
                            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                            owner.Operand(BeamWire.UnsignedTag, 1);
                            frame--;
                        }
                    }

                    owner.EmitArithmeticLine(step.Line, ref previous);
                    Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
                }

                owner.EmitCallLine(plan.Consumer, previous);
                Call(plan.Consumer.Module, plan.Consumer.Function, 1);
                owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
                owner.Operand(BeamWire.UnsignedTag, 1);
                if (projection is not null)
                {
                    EmitSavedRecordField(0, projection.Index, 1);
                }

                owner.EmitTupleHeader(XRegisterTag, 3, 0);
                owner.Operand(YRegisterTag, frame - 1);
                if (keepsRecord)
                {
                    owner.Operand(XRegisterTag, 1);
                }
                else
                {
                    owner.LiteralOperand(middle);
                }

                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(frame);
            }
        }

        private sealed record AppendTupleBranch(EntryCall Inner, int InnerLine, int Live, int Retained, int Returned,
            List<(Term? Left, Term[]? Prefix, int Line)> Steps, Expr.Call Consumer);

        private AppendTupleBranch? ReadAppendTupleBranch(Clause clause, Expr.Variable returned, Expr.Call consumer)
        {
            if (consumer is not { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [var body] })
            {
                return null;
            }

            string[] parameters = clause.Patterns.Select(pattern =>
                (pattern as Pattern.Variable)?.Name ?? VariableScopeNames.Wildcard).ToArray();
            int returnedRegister = Array.IndexOf(parameters, returned.Name);
            if (returnedRegister < 0 || returned.Name == VariableScopeNames.Wildcard)
            {
                return null;
            }

            var steps = new List<(Term? Left, Term[]? Prefix, int Line)>();
            int retained = -1;
            while (body is Expr.Binary { Operator: ErlangOperators.Append } append
                && append.Right is Expr.Binary { Operator: ErlangOperators.Append })
            {
                Term? left = null;
                if (append.Left is Expr.Variable { Name: not VariableScopeNames.Wildcard } value)
                {
                    if (retained >= 0 || (retained = Array.IndexOf(parameters, value.Name)) < 0)
                    {
                        return null;
                    }
                }
                else if (AppendPrefix(append.Left) is not null
                    || !AttributeLiteral.TryNormalizeConstant(append.Left, out left))
                {
                    return null;
                }

                Term[]? prefix = null;
                body = append.Right;
                if (body is Expr.Binary { Operator: ErlangOperators.Append,
                    Right: Expr.Binary { Operator: ErlangOperators.Append } remainder } folded
                    && AppendPrefix(folded.Left) is { Count: > 0 } items
                    && items.All(item => AttributeLiteral.TryNormalizeConstant(item, out _)))
                {
                    prefix = items.Select(item =>
                    {
                        AttributeLiteral.TryNormalizeConstant(item, out Term term);
                        return term;
                    }).ToArray();
                    body = remainder;
                }

                steps.Add((left, prefix, append.SourceLine));
            }

            if (retained < 0 || retained == returnedRegister
                || body is not Expr.Binary { Operator: ErlangOperators.Append } inner
                || AppendPrefix(inner.Left) is not null
                || ReadEntryCall(new Expr.Call(ErlangModuleNames.Module, ErlangOperators.Append, [inner.Left, inner.Right]),
                    parameters) is not { } first)
            {
                return null;
            }

            steps.Reverse();
            return new(first, inner.SourceLine, parameters.Length, retained, returnedRegister, steps, consumer);
        }

        private void EmitAppendPrefixInX1(IReadOnlyList<Term> prefix)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * prefix.Count);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = prefix.Count - 1; index >= 0; index--)
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(prefix[index]);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, index == 0 ? 1 : 0);
            }
        }
    }
}
