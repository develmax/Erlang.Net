using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record NestedTupleRepack(string[][] Fields, int[] Output)
        {
            internal int Count => Output.Length;
        }

        private static NestedTupleRepack? ReadNestedTupleRepack(Pattern input, Expr.Tuple output)
        {
            if (input is not Pattern.Tuple { Items: [Pattern.Tuple left, Pattern.Tuple right] }
                || left.Items.Count == 0 || right.Items.Count == 0
                || left.Items.Count + right.Items.Count != output.Items.Count
                || output.Items.Count + 2 > FirstReservedXRegister
                || left.Items.Concat(right.Items).Any(field => field is not Pattern.Variable { Name: not VariableScopeNames.Wildcard }))
            {
                return null;
            }

            string[][] fields = [left.Items.Cast<Pattern.Variable>().Select(field => field.Name).ToArray(),
                right.Items.Cast<Pattern.Variable>().Select(field => field.Name).ToArray()];
            string[] names = [.. fields[0], .. fields[1]];
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length
                || output.Items.Any(item => item is not Expr.Variable))
            {
                return null;
            }

            int[] order = output.Items.Cast<Expr.Variable>().Select(item => Array.IndexOf(names, item.Name)).ToArray();

            return order.Contains(-1) || order.Distinct().Count() != order.Length ? null : new(fields, order);
        }

        private void EmitNestedTupleShape(
            NestedTupleRepack plan,
            int source,
            int left,
            int right,
            int failure
        )
        {
            EmitTupleShapeTest(
                XRegisterTag,
                source,
                plan.Fields.Length,
                failure
            );
            owner.EmitTuplePart(source, 0, left);
            EmitTupleShapeTest(
                XRegisterTag,
                left,
                plan.Fields[0].Length,
                failure
            );
            owner.EmitTuplePart(source, 1, right);
            EmitTupleShapeTest(
                XRegisterTag,
                right,
                plan.Fields[1].Length,
                failure
            );
        }

        private void EmitWxOptionRepack(WxOption option, bool sharedField, int failure)
        {
            var plan = option.Repack!;
            if (!sharedField)
            {
                owner.EmitTuplePart(0, 1, 2);
            }

            EmitNestedTupleShape(
                plan,
                2,
                1,
                2,
                failure
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, plan.Count + 4);
            owner.Operand(BeamWire.UnsignedTag, 3);
            var registers = new List<int>();
            int next = 3;
            for (int group = 0; group < plan.Fields.Length; group++)
            {
                int source = group + 1;
                int count = plan.Fields[group].Length;
                for (int field = 0; field < count; field++)
                {
                    int destination = field == count - 1 ? source : registers.Count == 0 ? 0 : next++;
                    owner.EmitTuplePart(source, field, destination);
                    registers.Add(destination);
                }
            }

            EmitRepackedTuple(
                plan,
                registers,
                XRegisterTag,
                0
            );
            owner.EmitTupleHeader(XRegisterTag, 2, 0);
            owner.LiteralOperand(option.Tag);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
        }

        private void EmitRepackedTuple(
            NestedTupleRepack plan,
            IReadOnlyList<int> registers,
            int tag,
            int destination
        )
        {
            owner.EmitTupleHeader(XRegisterTag, plan.Count, destination);
            foreach (int field in plan.Output)
            {
                owner.Operand(tag, registers[field]);
            }
        }

        private bool TryEmitWxRepackedTupleCommand(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Patterns: [var receiver, _, ..],
                    Guard: { } guard,
                    Body: Expr.Sequence
                    {
                        Items: [
                        var classCheck,
                        Expr.Call
                        {
                            Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                            Arguments: [Expr.Variable forwarded, .., Expr.Case environment,
                                Expr.Literal { Value: Integer command }]
                        } queue,
                        ..
                    ]
                    } sequence
                } clause] || ReadWxClassUnionObject(receiver) is not { } wx
                || wx.Object == VariableScopeNames.Wildcard || wx.Type == VariableScopeNames.Wildcard
                || wx.Object == wx.Type || forwarded.Name != wx.Object
                || ReadWxClassTest(classCheck, wx.Type) is not { } check
                || !IsWxEnvironment(environment) || queue.Arguments.Count != clause.Patterns.Count + 2)
            {
                return false;
            }

            Expr.Call? receive = null;
            if (sequence.Items.Count != 2)
            {
                if (sequence.Items is not [_, _, Expr.Call
                    {
                        Module: "wxe_util", Function: "rec", DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Integer response }]
                    } reply] || !command.Equals(response))
                {
                    return false;
                }

                receive = reply;
            }

            var plans = new List<NestedTupleRepack>();
            for (int register = 1; register < clause.Patterns.Count; register++)
            {
                if (queue.Arguments[register] is not Expr.Tuple output
                    || ReadNestedTupleRepack(clause.Patterns[register], output) is not { } plan)
                {
                    return false;
                }

                plans.Add(plan);
            }

            string[][] groups = plans.SelectMany(plan => plan.Fields).ToArray();
            string[] fields = groups.SelectMany(group => group).ToArray();
            string environmentBinding = ((Expr.Variable)environment.Clauses[0].Body).Name;
            if (fields.Length + clause.Patterns.Count > FirstReservedXRegister
                || fields.Distinct(StringComparer.Ordinal).Count() != fields.Length
                || fields.Contains(wx.Object) || fields.Contains(wx.Type) || fields.Contains(environmentBinding)
                || environmentBinding == wx.Object || environmentBinding == wx.Type
                || ReadForwardGuard(guard, fields) is not { } readGuard)
            {
                return false;
            }

            var tests = FlattenForwardConjunction(readGuard).ToArray();
            if (tests.Length != fields.Length || tests.Any(test => test is not ForwardGuard.Test { Opcode: IsInteger }))
            {
                return false;
            }

            int[] order = tests.Cast<ForwardGuard.Test>().Select(test => test.Register).ToArray();
            if (order.Distinct().Count() != fields.Length)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            EmitDetailRecordTest(
                0,
                4,
                Term.A("wx_ref"),
                failure
            );
            int arity = clause.Patterns.Count;
            for (int index = 0; index < plans.Count; index++)
            {
                int left = arity + index * 2;
                EmitNestedTupleShape(
                    plans[index],
                    index + 1,
                    left,
                    left + 1,
                    failure
                );
            }

            int[] registers = new int[fields.Length];
            int[] starts = new int[groups.Length];
            int[] fieldGroups = new int[fields.Length];
            int start = 0;
            for (int group = 0; group < groups.Length; group++)
            {
                starts[group] = start;
                Array.Fill(
                    fieldGroups,
                    group,
                    start,
                    groups[group].Length
                );
                start += groups[group].Length;
            }

            int[] remaining = groups.Select(group => group.Length).ToArray();
            int next = arity + groups.Length;
            foreach (int field in order)
            {
                int group = fieldGroups[field];
                int source = group + arity;
                int destination = --remaining[group] == 0 ? source : next++;
                owner.EmitTuplePart(source, field - starts[group], destination);
                owner.Op(IsInteger, LabelTag, failure);
                owner.Operand(XRegisterTag, destination);
                registers[field] = destination;
            }

            int frame = fields.Length + 1;
            EmitFrameAllocation(next, frame);
            int[] stack = new int[fields.Length];
            int slot = 0;
            for (int group = groups.Length - 1; group >= 0; group--)
            {
                for (int field = starts[group]; field < starts[group] + groups[group].Length; field++)
                {
                    stack[field] = slot;
                    owner.MoveRegister(
                        XRegisterTag,
                        registers[field],
                        YRegisterTag,
                        slot++
                    );
                }
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                slot
            );
            int validated = Label();
            owner.EmitTuplePart(0, 2, 0);
            owner.Op(ExactNotEqualTest, LabelTag, validated);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(check.Class);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(Term.A("parent_class"), 2);
            owner.EmitConstant(check.Class, 0);
            owner.EmitCallLine(check.Call, location);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            Mark(validated);

            int missingEnvironment = Label();
            EmitWxEnvironmentCheck(0, missingEnvironment);
            owner.Op(TestHeap, BeamWire.UnsignedTag, fields.Length + plans.Count);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = 0; index < plans.Count; index++)
            {
                int offset = starts[index * 2];
                EmitRepackedTuple(
                    plans[index],
                    stack[offset..(offset + plans[index].Count)],
                    YRegisterTag,
                    index + 1
                );
            }

            owner.EmitTuplePart(0, 1, arity);
            owner.EmitConstant(command, arity + 1);
            owner.MoveRegister(
                YRegisterTag,
                slot,
                XRegisterTag,
                0
            );
            if (receive is null)
            {
                EmitPreparedFrameTail(new EntryCall(
                    queue,
                    queue.Module,
                    [],
                    []
                ), CallLocation(check.Call, location), frame);
            }
            else
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.EmitCallLine(queue, CallLocation(check.Call, location));
                Call(queue.Module, queue.Function, queue.Arguments.Count);
                owner.EmitConstant(command, 0);
                EmitPreparedFrameTail(new EntryCall(
                    receive,
                    receive.Module,
                    [],
                    []
                ), CallLocation(queue, location), 0);
            }

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            var error = (Expr.Call)environment.Clauses[1].Body;
            var previous = CallLocation(receive ?? queue, location);
            EmitPreparedFrameTail(new EntryCall(
                error,
                ErlangModuleNames.Module,
                [],
                []
            ), previous, frame);

            return true;
        }
    }
}
