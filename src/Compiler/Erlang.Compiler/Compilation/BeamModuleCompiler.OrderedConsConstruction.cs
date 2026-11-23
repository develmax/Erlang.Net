using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record OrderedConsHead(
            int? Source,
            Term? Literal,
            EntryConstructedArgument? Tuple
        );

        private sealed record OrderedConsConstruction(
            OrderedConsHead[] Heads,
            EntryConstructedArgument? OuterTuple,
            int ResultSource
        );

        private ConsCallResult? ReadOrderedConsCallResult(Expr body, string[] inputs)
        {
            Expr.List list;
            EntryConstructedArgument? outer = null;
            int resultSource = inputs.Length;
            if (body is Expr.List { Items.Count: > 1 } direct)
            {
                list = direct;
            }
            else if (body is Expr.Sequence
            {
                Items:
                [
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                    Value: Expr.List { Items.Count: > 0 } value
                },
                    Expr.Tuple tuple
                ]
            } && !inputs.Contains(binding.Name, StringComparer.Ordinal))
            {
                outer = ReadEntryConstructedArgument(tuple, 0, [.. inputs, binding.Name]);
                if (outer is null || outer.Operands.Count(operand => operand.Source == resultSource) != 1)
                {
                    return null;
                }

                list = value;
            }
            else
            {
                return null;
            }

            if (list.Tail is not Expr.Call call || ReadEntryCall(call, inputs) is not { } invocation)
            {
                return null;
            }

            if (ReadOrderedConsConstruction(list, inputs, outer) is not { } ordered)
            {
                return null;
            }

            return new(
                invocation,
                ordered.Sources,
                null,
                null,
                false,
                Ordered: ordered.Construction
            );
        }

        private static (OrderedConsConstruction Construction, int[] Sources)? ReadOrderedConsConstruction(
            Expr.List list,
            string[] inputs,
            EntryConstructedArgument? outer
        )
        {
            // v3_core keeps unsafe prerequisites in source order. These heads
            // and outer operands have only incoming values and folded terms.
            var heads = new OrderedConsHead[list.Items.Count];
            var sources = new HashSet<int>();
            bool hasDynamicTuple = false;
            for (int index = 0; index < list.Items.Count; index++)
            {
                Expr head = list.Items[index];
                if (head is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                    && Array.IndexOf(inputs, variable.Name) is int source && source >= 0)
                {
                    heads[index] = new(source, null, null);
                    sources.Add(source);
                }
                else if (AttributeLiteral.TryNormalizeConstant(head, out Term literal))
                {
                    heads[index] = new(null, literal, null);
                }
                else if (ReadEntryConstructedArgument(head, 1, inputs) is { IsTuple: true } construction)
                {
                    heads[index] = new(null, null, construction);
                    hasDynamicTuple = true;
                    RememberOperands(construction);
                }
                else
                {
                    return null;
                }
            }

            if (!hasDynamicTuple)
            {
                return null;
            }

            if (outer is not null)
            {
                RememberOperands(outer);
            }

            return (new(heads, outer, inputs.Length), sources.OrderDescending().ToArray());

            void RememberOperands(EntryConstructedArgument tuple)
            {
                foreach (var (source, _) in tuple.Operands)
                {
                    if (source is int register && register != inputs.Length)
                    {
                        sources.Add(register);
                    }
                }
            }
        }

        private bool TryEmitOrderedBranchCons(Expr body)
        {
            if (body is not Expr.List { Items.Count: > 1 } list
                || ReadLiveOrderedConsConstruction(list) is not { } plan)
            {
                return false;
            }

            EmitLiveOrderedConsConstruction(plan.Construction, plan.Tail, plan.Slots);

            return true;
        }

        private (OrderedConsConstruction Construction, Expr.Call Tail, int[] Slots)? ReadLiveOrderedConsConstruction(
            Expr.List list
        )
        {
            if (owner.ModuleOptimizationDisabled || owner.receiveScratchRegisters is not null
                || list.Tail is not Expr.Call tail
                || !TryResolveOrdinaryCall(tail, out string? module) || module == ErlangModuleNames.Module
                || tail.Arguments.OfType<Expr.Variable>().Any(argument => !variables.ContainsKey(argument.Name)))
            {
                return null;
            }

            var bindings = variables.ToArray();
            var names = bindings.Select(binding => binding.Key).ToArray();
            if (ReadOrderedConsConstruction(list, names, outer: null) is not { } ordered)
            {
                return null;
            }

            return (ordered.Construction, tail, bindings.Select(binding => binding.Value).ToArray());
        }

        private void EmitLiveOrderedConsConstruction(OrderedConsConstruction construction, Expr.Call tail, int[] slots)
        {
            // All dynamic operands already occupy stable Y slots. Ordinary call
            // setup and the post-call construction leave those bindings intact.
            Expression(tail);
            EmitOrderedConsConstruction(construction, source => slots[source]);
        }

        private void EmitOrderedConsConstruction(OrderedConsConstruction construction, int[] saved) =>
            EmitOrderedConsConstruction(construction, source => Array.IndexOf(saved, source));

        private void EmitOrderedConsConstruction(OrderedConsConstruction construction, Func<int, int> stackSlot)
        {
            int heap = construction.Heads.Length * ConsHeapWords
                + construction.Heads.Sum(head => head.Tuple?.HeapWords ?? 0)
                + (construction.OuterTuple?.HeapWords ?? 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, heap);
            owner.Operand(BeamWire.UnsignedTag, 1);

            // beam_core_to_ssa forces the tail atomic before the pure head.
            // Construct each head in the same register as the cons chain grows.
            for (int index = construction.Heads.Length - 1; index >= 0; index--)
            {
                OrderedConsHead head = construction.Heads[index];
                if (head.Tuple is { } tuple)
                {
                    EmitTuple(tuple);
                    owner.Op(PutList, XRegisterTag, tuple.Register);
                }
                else
                {
                    owner.instructions.WriteByte(PutList);
                    EmitOperand(head.Source, head.Literal);
                }

                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            if (construction.OuterTuple is { } outer)
            {
                EmitTuple(outer);
            }

            void EmitTuple(EntryConstructedArgument tuple)
            {
                owner.EmitTupleHeader(XRegisterTag, tuple.Operands.Length, tuple.Register);
                foreach (var (source, constant) in tuple.Operands)
                {
                    EmitOperand(source, constant);
                }
            }

            void EmitOperand(int? source, Term? constant)
            {
                if (source == construction.ResultSource)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else if (source is int register)
                {
                    owner.Operand(YRegisterTag, stackSlot(register));
                }
                else
                {
                    owner.LiteralOperand(constant!);
                }
            }
        }
    }
}
