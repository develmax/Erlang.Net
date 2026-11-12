using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record WxTupleInput(int Register, int Width, ForwardGuard.FieldTest[] Fields);

        private WxTupleInput? ReadWxTupleInput(Clause clause, int register, string? receiverType = null)
        {
            if (clause.Guard is not { } guard || ReadForwardParameters(clause) is not { } inputs
                || inputs.Shapes is not [var shape] || shape.Register != register
                || inputs.Fields.Count != shape.Arity
                || receiverType is not null && (inputs.Fields.ContainsKey(receiverType) || inputs.Parameters.Contains(receiverType))
                || ReadForwardGuard(guard, inputs.Parameters, inputs.Fields) is not { } plan)
            {
                return null;
            }

            var tests = FlattenForwardConjunction(plan).ToArray();
            if (tests.Length != shape.Arity || tests.Any(test => test is not ForwardGuard.FieldTest { Opcode: IsInteger }))
            {
                return null;
            }

            var fields = tests.Cast<ForwardGuard.FieldTest>().ToArray();

            return fields.Select(field => field.Index).Distinct().Count() == shape.Arity
                ? new(register, shape.Arity, fields) : null;
        }

        private bool TryEmitWxStaticTupleCommand(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: { } guard } clause]
                || ReadForwardParameters(clause) is not { } inputs || inputs.Shapes.Count == 0
                || ReadForwardGuard(guard, inputs.Parameters, inputs.Fields) is not { } tests)
            {
                return false;
            }

            var ordered = FlattenForwardConjunction(tests).ToArray();
            if (ordered.Any(test => test is not ForwardGuard.FieldTest { Opcode: IsInteger })
                || ordered.Length != inputs.Shapes.Sum(shape => shape.Arity)
                || ordered.Distinct().Count() != ordered.Length)
            {
                return false;
            }

            var command = function with
            {
                Clauses = [clause with
                {
                    Patterns = inputs.Parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray(),
                    Guard = null
                }]
            };
            if (!TryEmitWxStaticCommand(command, validateOnly: true))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            foreach (var (register, width) in inputs.Shapes)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    register,
                    width,
                    failure
                );
            }

            foreach (var test in ordered)
            {
                EmitForwardGuard(test, failure);
            }

            return TryEmitWxStaticCommand(command, failure);
        }

        private bool TryEmitWxTupleOverload(FunctionDefinition function)
        {
            if (function.Clauses is not [var first, var second] || function.Arity is not (1 or 2))
            {
                return false;
            }

            bool hasReceiver = function.Arity == 2;
            var receiver = ReadWxClassUnionObject(first.Patterns[0]);
            var alternateReceiver = ReadWxClassUnionObject(second.Patterns[0]);
            if (hasReceiver && (receiver is null || alternateReceiver is null))
            {
                return false;
            }

            int argument = function.Arity - 1;
            var firstInputs = hasReceiver ? first with
            {
                Patterns = [new Pattern.Variable(receiver!.Value.Object), first.Patterns[1]]
            } : first;
            if (ReadWxTupleInput(firstInputs, argument, receiver?.Type) is not { } tuple)
            {
                return hasReceiver && TryEmitWxClassUnionTupleOverload(function, first, second);
            }

            var firstCommand = function with { Clauses = [first with { Guard = null }] };
            if (!hasReceiver)
            {
                var names = ReadForwardParameters(first)!.Value.Parameters;
                firstCommand = function with
                {
                    Clauses = [first with { Patterns = [new Pattern.Variable(names[0])], Guard = null }]
                };
            }

            int failure = owner.nextLabel - 2;
            if (hasReceiver && alternateReceiver is { } other && ReadWxTupleInput(second with
            {
                Patterns = [new Pattern.Variable(other.Object), second.Patterns[1]]
            }, argument, other.Type) is { } alternate
                && alternate.Width != tuple.Width)
            {
                var secondCommand = function with { Clauses = [second with { Guard = null }] };
                if (!ValidateTupleCommand(firstCommand) || !ValidateTupleCommand(secondCommand))
                {
                    return false;
                }

                EmitDetailRecordTest(
                    0,
                    4,
                    Term.A("wx_ref"),
                    failure
                );
                owner.Op(IsTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, argument);
                owner.EmitTuplePart(0, 2, 2);
                int firstEntry = Label();
                int secondEntry = owner.nextLabel + 2;
                var branches = new[] { (tuple, firstCommand), (alternate, secondCommand) }.OrderByDescending(branch => branch.Item1.Width).ToArray();
                owner.instructions.WriteByte(SelectTupleArity);
                owner.EmitTypedRegister(argument, TupleTypeBits);
                owner.Operand(LabelTag, failure);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 4);
                owner.Operand(BeamWire.UnsignedTag, branches[1].Item1.Width);
                owner.Operand(LabelTag, secondEntry);
                owner.Operand(BeamWire.UnsignedTag, branches[0].Item1.Width);
                owner.Operand(LabelTag, firstEntry);
                Mark(firstEntry);
                EmitTupleFields(branches[0].Item1, failure);
                EmitTupleCommand(branches[0].Item2, failure);
                Mark(Label());
                EmitTupleFields(branches[1].Item1, failure);

                return EmitTupleCommand(branches[1].Item2, failure);
            }

            var fallback = function with { Clauses = [second] };
            int tag = hasReceiver ? 3 : 1;
            int valueType = tag + 1;
            if (tuple.Width != 4 || second.Guard is not null || ReadWxClassUnionObject(second.Patterns[argument]) is null
                || !(hasReceiver ? ValidateTupleCommand(firstCommand) : TryEmitWxStaticCommand(firstCommand, validateOnly: true))
                || !TryEmitWxObjectCommand(
                    fallback,
                    hasReceiver ? 2 : valueType,
                    validateOnly: true,
                    preparedSecondClass: hasReceiver ? valueType : null,
                    preparedTupleOverload: true
                ))
            {
                return false;
            }

            if (hasReceiver)
            {
                EmitDetailRecordTest(
                    0,
                    4,
                    Term.A("wx_ref"),
                    failure
                );
            }

            EmitTupleShapeTest(
                XRegisterTag,
                argument,
                tuple.Width,
                failure
            );
            if (hasReceiver)
            {
                owner.EmitTuplePart(0, 2, 2);
            }

            owner.EmitTuplePart(argument, 0, tag);
            owner.EmitTuplePart(argument, 2, valueType);
            int next = owner.nextLabel + (hasReceiver ? 2 : 1);
            EmitTupleFields(
                tuple,
                next,
                tag,
                valueType
            );
            if (hasReceiver)
            {
                EmitTupleCommand(firstCommand, failure);
            }
            else
            {
                TryEmitWxStaticCommand(firstCommand, failure);
            }

            Mark(Label());
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, tag);
            owner.LiteralOperand(Term.A("wx_ref"));

            return TryEmitWxObjectCommand(
                fallback,
                hasReceiver ? 2 : valueType,
                functionFailure: failure,
                preparedSecondClass: hasReceiver ? valueType : null,
                preparedTupleOverload: true
            );
        }

        private bool ValidateTupleCommand(FunctionDefinition command) =>
            TryEmitWxObjectCommand(
                command,
                2,
                validateOnly: true,
                preparedTupleOverload: true
            );

        private bool EmitTupleCommand(FunctionDefinition command, int failure) =>
            TryEmitWxObjectCommand(
                command,
                2,
                functionFailure: failure,
                preparedTupleOverload: true
            );

        private bool TryEmitWxClassUnionTupleOverload(FunctionDefinition function, Clause first, Clause second)
        {
            if (ReadWxClassUnionObject(second.Patterns[0]) is not { } receiver
                || ReadWxTupleInput(second with
                {
                    Patterns = [new Pattern.Variable(receiver.Object), second.Patterns[1]]
                }, 1, receiver.Type) is not { Width: 4 } tuple || tuple.Fields[0].Index != 0
                || first.Body is not Expr.Sequence
                {
                    Items: [_, Expr.Match { Value: Expr.Try }, Expr.Match { Value: Expr.Try },
                    Expr.Match { Value: Expr.If { Clauses.Count: 3 } }, ..]
                })
            {
                return false;
            }

            var union = function with { Clauses = [first] };
            var rectangle = function with { Clauses = [second with { Guard = null }] };
            if (!TryEmitWxClassUnionCommand(union, validateOnly: true, preparedClasses: (2, 4))
                || !ValidateTupleCommand(rectangle))
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
            EmitTupleShapeTest(
                XRegisterTag,
                1,
                tuple.Width,
                failure
            );
            owner.EmitTuplePart(0, 2, 2);
            owner.EmitTuplePart(1, 0, 3);
            owner.EmitTuplePart(1, 2, 4);
            // A receiver check, two protected checks and the class selection use thirteen labels.
            int rectangleEntry = owner.nextLabel + 13;
            owner.Op(ExactEqualTest, LabelTag, rectangleEntry);
            owner.Operand(XRegisterTag, 3);
            owner.LiteralOperand(Term.A("wx_ref"));
            TryEmitWxClassUnionCommand(union, preparedClasses: (2, 4));
            Mark(Label());
            EmitTupleFields(
                tuple,
                failure,
                3,
                4,
                scratch: 3
            );

            return EmitTupleCommand(rectangle, failure);
        }

        private void EmitTupleFields(
            WxTupleInput tuple,
            int failure,
            int? tag = null,
            int? type = null,
            int? scratch = null
        )
        {
            foreach (var field in tuple.Fields)
            {
                if (field.Index == 0 && tag is int first)
                {
                    owner.Op(IsInteger, LabelTag, failure);
                    owner.Operand(XRegisterTag, first);
                }
                else if (field.Index == 2 && type is int third)
                {
                    owner.Op(IsInteger, LabelTag, failure);
                    owner.Operand(XRegisterTag, third);
                }
                else
                {
                    EmitForwardGuard(field with { Scratch = scratch ?? (type is int last ? last + 1 : 3) }, failure);
                }
            }
        }
    }
}
