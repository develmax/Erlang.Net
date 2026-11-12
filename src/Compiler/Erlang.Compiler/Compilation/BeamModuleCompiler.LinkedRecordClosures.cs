using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLinkedRecordClosures(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable address, AliasPattern alias],
                Body: Expr.Sequence { Items: [
                    Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.Link, Arguments: [Expr.Variable linked] } link,
                    Expr.RecordConstruction { Wildcard: null } construction
                ] }
            } clause] || !owner.ResolvesErlangBif(link)
                || !TryReadPatternAlias(alias, out var socket, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } record)
            {
                return false;
            }

            Expr.Tuple result = RecordExpressions.Create(construction);
            if (!result.Items.All(item => item is Expr.Fun || IsClosureTupleOperand(item)))
            {
                return false;
            }

            int field = -1;
            for (int index = 1; index < record.Items.Count; index++)
            {
                if (record.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }
                if (field >= 0 || record.Items[index] is not Pattern.Variable variable || variable.Name != linked.Name)
                {
                    return false;
                }
                field = index;
            }
            var controller = new Pattern.Variable(linked.Name);
            if (field < 0 || !DistinctBindings(address, socket, controller))
            {
                return false;
            }

            var bindings = clause with { Patterns = [address, socket, controller] };
            if (result.Items.OfType<Expr.Variable>().Any(variable => !HasParameter(bindings, variable.Name)))
            {
                return false;
            }

            Expr.Fun[] callbacks = result.Items.OfType<Expr.Fun>().ToArray();
            if (callbacks.Length == 0 || callbacks.Length > FirstReservedXRegister)
            {
                return false;
            }

            var plans = new List<(LiftedClosure Closure, int[] Registers)>(callbacks.Length);
            int heapWords = result.Items.Count + 1;
            for (int index = 0; index < callbacks.Length; index++)
            {
                if (callbacks[index].Clauses is not [{ Guard: not null }]
                    || owner.ReadReturnedClosure(function, bindings, callbacks[index], location,
                        callbacks.Length - index - 1, index, null) is not { } plan)
                {
                    return false;
                }

                plans.Add(plan);
                heapWords += plan.Registers.Length;
            }

            // The three inputs must survive link/1 in the same order as the closure environment.
            if (Enumerable.Range(0, 3).Any(register => !plans.Any(plan => plan.Registers.Contains(register))
                && !result.Items.OfType<Expr.Variable>().Any(variable => ArgumentRegister(bindings, variable.Name) == register)))
            {
                return false;
            }

            EmitDetailRecordTest(1, record.Items.Count, tag, owner.nextLabel - 2);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 2);
            owner.Op(GetTupleElement, XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(YRegisterTag, 0);
            Load(0);
            owner.EmitCallLine(link, location);
            owner.EmitStaticRemoteCall(link with { Module = ErlangModuleNames.Module });
            owner.EmitClosureAllocation(heapWords, 0, plans.Count);
            for (int index = 0; index < plans.Count; index++)
            {
                var plan = plans[index];
                owner.Op(MakeFun, BeamWire.UnsignedTag, owner.closures.Count);
                owner.Operand(XRegisterTag, index);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, plan.Registers.Length);
                foreach (int register in plan.Registers)
                {
                    owner.Operand(YRegisterTag, 2 - register);
                }
                owner.closures.Add(plan.Closure);
            }

            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, result.Items.Count);
            int callbackIndex = 0;
            foreach (Expr item in result.Items)
            {
                switch (item)
                {
                    case Expr.Fun:
                        owner.Operand(XRegisterTag, callbackIndex++);
                        break;
                    case Expr.Variable variable:
                        owner.Operand(YRegisterTag, 2 - ArgumentRegister(bindings, variable.Name));
                        break;
                    case Expr.Literal literal:
                        owner.LiteralOperand(literal.Value);
                        break;
                }
            }
            owner.Op(Deallocate, BeamWire.UnsignedTag, 3);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
