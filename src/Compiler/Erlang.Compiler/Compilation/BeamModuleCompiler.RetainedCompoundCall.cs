using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRetainedCompoundCall(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [{ Guard: null, Body: Expr.Sequence body } clause]
                || function.Arity == 0 || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var bindings = parameters.ToHashSet(StringComparer.Ordinal);
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            var source = body.Items.SkipWhile(item => item is Expr.Literal).ToArray();
            int index = 0;
            while (index < source.Length && source[index] is Expr.Match { Pattern: Pattern.Variable assigned } assignment
                && Constant(assignment.Value) is { } constant)
            {
                if (!Bind(assigned))
                {
                    return false;
                }

                constants.Add(assigned.Name, constant);
                index++;
            }

            if (source[index..] is not
                [
                    Expr.Match { Pattern: Pattern.Variable list, Value: Expr.List { Tail: null, Items: var items } },
                    Expr.Match { Pattern: Pattern.Variable identity, Value: Expr.Tuple { Items: [var tag, Expr.Variable first] } },
                    Expr.Match
                {
                    Pattern: Pattern.Variable descriptor,
                    Value: Expr.Tuple { Items: var descriptorItems }
                },
                    Expr.Case
                {
                    Value: Expr.Call { Arguments: [var subject, Expr.Variable spec] } invoke,
                    Clauses:
                        [
                        {
                            Guard: null,
                            Patterns: [var pattern],
                            Body: Expr.Call { Arguments: [var retrySubject, Expr.Variable retryId] } retry
                        },
                        { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable returned }
                        ]
                }
                ]
                || descriptorItems.ToArray() is not
                [
                    Expr.Variable id,
                    Expr.Tuple { Items: [var moduleAtom, var functionAtom, Expr.Variable arguments] },
                    .. var fields
                ]
                || !Bind(list) || !Bind(identity) || !Bind(descriptor) || !Bind(fallback)
                || !items.OfType<Expr.Variable>().Select(item => item.Name).SequenceEqual(parameters, StringComparer.Ordinal)
                || items.Count != parameters.Length || first.Name != parameters[0] || id.Name != identity.Name
                || arguments.Name != list.Name || descriptor.Name != spec.Name || retryId.Name != identity.Name
                || fallback.Name != returned.Name || Constant(tag) is not Atom idTag
                || Constant(moduleAtom) is not Atom callbackModule || Constant(functionAtom) is not Atom callbackFunction
                || Constant(subject) is not Atom invocationTarget || Constant(retrySubject) is not Atom retryTarget
                || ReadRetainedCompoundPattern(pattern) is not { } key
                || !TryResolveOrdinaryCall(invoke, out string? invocationModule) || invocationModule is null
                || !TryResolveOrdinaryCall(retry, out string? retryModule) || retryModule is null)
            {
                return false;
            }

            var members = new Term?[fields.Length];
            (Expr.Call Call, string Module, Term Argument)? pending = null;
            for (int field = 0; field < fields.Length; field++)
            {
                if (fields[field] is Expr.Call { Arguments: [var argument] } call
                    && pending is null && Constant(argument) is { } value
                    && TryResolveOrdinaryCallTarget(call, out string? module) && module is not null)
                {
                    pending = (call, module, value);
                }
                else if (Constant(fields[field]) is { } literal)
                {
                    members[field] = literal;
                }
                else
                {
                    return false;
                }
            }

            if (pending is not { } effect)
            {
                return false;
            }

            // The entry list and identity survive the first call. The nested call
            // descriptor can be built after that call, along with its outer tuple.
            const int frame = 2;
            const int identitySlot = 0;
            const int listSlot = 1;
            const int identityArity = 2;
            const int callbackArity = 3;
            EmitFrameAllocation(parameters.Length, frame, heapWords: parameters.Length * ConsHeapWords + identityArity + 1);
            for (int register = parameters.Length - 1; register >= 0; register--)
            {
                owner.Op(PutList, XRegisterTag, register);
                if (register == parameters.Length - 1)
                {
                    owner.Operand(AtomTag, NilAtomIndex);
                }
                else
                {
                    owner.Operand(XRegisterTag, register + 1);
                }

                owner.Operand(register == 0 ? YRegisterTag : XRegisterTag, register == 0 ? listSlot : register);
            }

            owner.EmitTupleHeader(YRegisterTag, identityArity, identitySlot);
            owner.LiteralOperand(idTag);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(effect.Argument, 0);
            owner.EmitCallLine(effect.Call, location);
            Call(effect.Module, effect.Call.Function, 1);

            int descriptorArity = members.Length + 2;
            owner.Op(TestHeap, BeamWire.UnsignedTag, callbackArity + descriptorArity + 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, callbackArity, 1);
            owner.LiteralOperand(callbackModule);
            owner.LiteralOperand(callbackFunction);
            owner.Operand(YRegisterTag, listSlot);
            owner.EmitTupleHeader(XRegisterTag, descriptorArity, 1);
            owner.Operand(YRegisterTag, identitySlot);
            owner.Operand(XRegisterTag, 1);
            foreach (var member in members)
            {
                if (member is null)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.LiteralOperand(member);
                }
            }

            owner.InitStackSlot(listSlot);
            owner.EmitConstant(invocationTarget, 0);
            owner.EmitCallLine(invoke, location);
            Call(invocationModule, invoke.Function, 2);

            int passthrough = Label();
            owner.Op(ExactEqualTest, LabelTag, passthrough);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(key);
            owner.MoveRegister(
                YRegisterTag,
                identitySlot,
                XRegisterTag,
                1
            );
            owner.EmitConstant(retryTarget, 0);
            owner.EmitCallLine(retry, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(retryModule, retry.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, frame);
            Mark(passthrough);
            owner.EmitFrameReturn(frame);

            return true;

            Term? Constant(Expr expression) =>
                AttributeLiteral.TryNormalizeConstant(expression, out Term value, constants) ? value : null;

            bool Bind(Pattern.Variable variable) =>
                variable.Name != VariableScopeNames.Wildcard && bindings.Add(variable.Name);
        }

        private static TupleTerm? ReadRetainedCompoundPattern(Pattern pattern)
        {
            if (pattern is Pattern.Literal { Value: TupleTerm tuple })
            {
                return tuple;
            }
            if (pattern is not Pattern.Tuple common || !common.Items.All(item => item is Pattern.Literal))
            {
                return null;
            }

            return new(common.Items.Cast<Pattern.Literal>().Select(item => item.Value).ToArray());
        }
    }
}
