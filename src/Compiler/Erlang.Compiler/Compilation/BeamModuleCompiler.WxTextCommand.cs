using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record WxTextCommand(
            ForwardGuard Guard,
            Expr.Call[] Conversions,
            (Expr.Fun Mapper, Expr.Call Map)? Options,
            Expr.Call Queue,
            Expr.Call? Receive,
            Expr.Call EnvironmentError,
            Integer Command);

        private bool TryEmitWxTextCommand(FunctionDefinition function)
        {
            int failure = owner.nextLabel - 2;
            if (function.Clauses is [var single] && ReadWxTextCommand(single) is { } command)
            {
                EmitWxTextCommand(command, failure);
                return true;
            }

            if (function.Clauses is not [var scalar, var text] || function.Arity != 2
                || ReadWxTextCommand(text) is not { Options: null, Receive: null } fallback
                || scalar.Guard is null || !HasUnconditionalParameters(scalar))
            {
                return false;
            }

            var names = scalar.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var first = function with { Clauses = [scalar] };
            if (ReadForwardGuard(scalar.Guard, names) is not { } guard
                || FlattenForwardConjunction(guard).ToArray() is not [var name, ForwardGuard.Test { Opcode: IsInteger, Register: 1 }]
                || !IsWxTextGuard(name, 0) || !TryEmitWxStaticCommand(first, validateOnly: true))
            {
                return false;
            }

            // The first clause uses two text-guard labels and an environment failure label.
            int next = owner.nextLabel + 3;
            TryEmitWxStaticCommand(first, guardFailure: next);
            Mark(Label());
            EmitWxTextCommand(fallback, failure);
            return true;
        }

        private WxTextCommand? ReadWxTextCommand(Clause clause)
        {
            if (!HasUnconditionalParameters(clause) || clause.Guard is null
                || clause.Patterns.Count is not (2 or 3) || clause.Body is not Expr.Sequence
                {
                    Items: [Expr.Match { Value: Expr.Call { Module: "unicode", Function: "characters_to_binary" } }, ..]
                } sequence)
            {
                return null;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            if (parameters.Contains(VariableScopeNames.Wildcard) || ReadForwardGuard(clause.Guard, parameters) is not { } guard)
            {
                return null;
            }

            var tests = FlattenForwardConjunction(guard).ToArray();
            if (tests.Length != parameters.Length)
            {
                return null;
            }

            var bound = new HashSet<string>(parameters, StringComparer.Ordinal);
            var forwarded = parameters.ToArray();
            var conversions = new List<Expr.Call>();
            foreach (Expr item in sequence.Items.Take(2))
            {
                int input = conversions.Count;
                if (item is not Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } output,
                        Value: Expr.Call { Module: "unicode", Function: "characters_to_binary", DynamicRemote: false,
                            Arguments: [Expr.Variable argument] } conversion
                    } || argument.Name != parameters[input] || !IsWxTextGuard(tests[input], input)
                    || !bound.Add(output.Name))
                {
                    break;
                }

                conversions.Add(conversion);
                forwarded[input] = output.Name;
            }

            int cursor = conversions.Count;
            (Expr.Fun Mapper, Expr.Call Map)? options = null;
            if (cursor == parameters.Length - 1 && cursor > 0 && tests[cursor] is ForwardGuard.Test { Opcode: IsList } list
                && list.Register == cursor && sequence.Items.Skip(cursor).Take(2).ToArray() is [
                    Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } mapper,
                        Value: Expr.Fun { SelfName: null } lambda },
                    Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } mapped,
                        Value: Expr.Call { Module: "lists", Function: "map", DynamicRemote: false,
                            Arguments: [Expr.Variable mapperInput, Expr.Variable optionsInput] } map }]
                && optionsInput.Name == parameters[cursor] && mapperInput.Name == mapper.Name
                && ReadWxOptions(lambda.Clauses, out _, out _)
                && ClosureCaptures.Capture(lambda, bound.ToDictionary(binding => binding, _ => (Term)Nil.Value)).Count == 0
                && bound.Add(mapper.Name) && bound.Add(mapped.Name))
            {
                forwarded[cursor] = mapped.Name;
                options = (lambda, map);
                cursor += 2;
            }
            else if (conversions.Count != 2 || parameters.Length != 2)
            {
                return null;
            }

            int arity = parameters.Length;
            if (sequence.Items.Count - cursor is not (1 or 2) || sequence.Items[cursor] is not Expr.Call
                {
                    Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false
                } queue || queue.Arguments.Count != arity + 2
                || queue.Arguments[arity] is not Expr.Case environment || !IsWxEnvironment(environment)
                || bound.Contains(((Expr.Variable)environment.Clauses[0].Body).Name)
                || queue.Arguments[arity + 1] is not Expr.Literal { Value: Integer command }
                || !queue.Arguments.Take(arity).Select(argument => (argument as Expr.Variable)?.Name).SequenceEqual(forwarded))
            {
                return null;
            }

            Expr.Call? receive = null;
            if (sequence.Items.Count == cursor + 2)
            {
                if (sequence.Items[cursor + 1] is not Expr.Call
                    {
                        Module: "wxe_util", Function: "rec", DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Integer replied }]
                    } reply || !replied.Equals(command))
                {
                    return null;
                }

                receive = reply;
            }

            return new(guard, conversions.ToArray(), options, queue, receive, (Expr.Call)environment.Clauses[1].Body, command);
        }

        private static bool IsWxTextGuard(ForwardGuard guard, int register) =>
            guard is ForwardGuard.Disjunction
            {
                Left: ForwardGuard.Test { Opcode: IsList } list,
                Right: ForwardGuard.Test { Opcode: IsBinary } binary
            } && list.Register == register && binary.Register == register;

        private void EmitWxTextCommand(WxTextCommand plan, int failure)
        {
            int arity = plan.Queue.Arguments.Count - 2;
            int frame = arity - 1;
            EmitForwardGuard(plan.Guard, failure);
            EmitFrameAllocation(arity, frame);
            for (int input = arity - 1; input > 0; input--)
            {
                owner.MoveRegister(XRegisterTag, input, YRegisterTag, arity - 1 - input);
            }

            BeamSourceLocation previous = location;
            for (int index = 0; index < plan.Conversions.Length; index++)
            {
                if (index != 0)
                {
                    owner.Op(Swap, YRegisterTag, frame - 1);
                    owner.Operand(XRegisterTag, 0);
                }

                EmitCall(plan.Conversions[index]);
            }

            if (plan.Options is { } options)
            {
                owner.EmitClosureAllocation(0, 1, 1);
                EmitWxOptionMapper(options.Mapper, 1);
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 2);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
                EmitCall(options.Map);
            }

            int missingEnvironment = Label();
            EmitWxEnvironmentCheck(1, missingEnvironment);
            owner.EmitTuplePart(1, 1, arity);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, arity - 1);
            if (arity == 3)
            {
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
            }

            owner.EmitConstant(plan.Command, arity + 1);
            owner.MoveRegister(YRegisterTag, frame - 1, XRegisterTag, 0);
            if (plan.Receive is { } reply)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
                owner.Operand(BeamWire.UnsignedTag, 0);
                EmitCall(plan.Queue);
                owner.EmitConstant(plan.Command, 0);
                EmitTail(reply, 0);
            }
            else
            {
                EmitTail(plan.Queue, frame);
            }

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            previous = CallLocation(plan.Receive ?? plan.Queue, previous);
            EmitTail(plan.EnvironmentError, frame);

            void EmitCall(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                Call(call.Module, call.Function, call.Arguments.Count);
            }

            void EmitTail(Expr.Call call, int slots)
            {
                owner.EmitCallLine(call, previous);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, call.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, call.Arguments.Count));
                owner.Operand(BeamWire.UnsignedTag, slots);
            }
        }
    }
}
