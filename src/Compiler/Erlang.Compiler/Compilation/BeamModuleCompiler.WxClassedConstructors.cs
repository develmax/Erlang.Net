using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record WxClassedConstructorArm(
            (Atom Class, Expr.Call Call)[] Classes,
            Expr.Call Error,
            Expr.Call Cast,
            Expr.Call Queue,
            Expr.Call Receive,
            Integer Command,
            (Atom Class, Expr.Call First, Expr.Call Second, Expr.Call Final)? Flags,
            (Expr.Fun Function, Expr.Call Map)? Mapper
        );

        private bool TryEmitWxClassedConstructorOverload(
            FunctionDefinition function,
            GuardedLocalForward forward,
            int failure
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != 2 || function.Clauses is not [_, var record, var mapped]
                || forward.Shapes.Count != 0 || !forward.AddsEmptyList
                || FlattenForwardConjunction(forward.Tests).ToArray() is not
                [
                    ForwardGuard.Test { Opcode: IsInteger, Register: 0 },
                    ForwardGuard.Test { Opcode: IsInteger, Register: 1 }
                ]
                || ReadWxClassedConstructorArm(record, hasFlags: true) is not { } flags
                || ReadWxClassedConstructorArm(mapped, hasFlags: false) is not { } options)
            {
                return false;
            }

            int records = Label();
            EmitGuardedLocalForward(forward, records);
            Mark(records);
            var tag = Term.A("wx_ref");
            EmitDetailRecordTest(
                0,
                4,
                tag,
                failure
            );
            owner.EmitTuplePart(0, 2, 2);
            // The record branch has two protected calls and seven further control-flow edges.
            int list = owner.nextLabel + 15;
            EmitDetailRecordTest(
                1,
                4,
                tag,
                list
            );
            EmitWxClassedConstructorArm(flags);
            Mark(Label());
            EmitForwardGuard(new ForwardGuard.Test(IsList, 1), failure);
            EmitWxClassedConstructorArm(options);

            return true;
        }

        private WxClassedConstructorArm? ReadWxClassedConstructorArm(Clause clause, bool hasFlags)
        {
            if (clause.Patterns is not [var objectPattern, var alternative]
                || ReadWxClassUnionObject(objectPattern) is not { } input
                || clause.Body is not Expr.Sequence sequence || sequence.Items.Count != (hasFlags ? 6 : 7)
                || ReadProtectedWxClassTest(sequence.Items[0], input.Type) is not { } first
                || ReadProtectedWxClassTest(sequence.Items[1], input.Type) is not { } second
                || sequence.Items[2] is not Expr.Match
                {
                    Pattern: Pattern.Variable selected,
                    Value: Expr.If { Clauses: [var preferred, var fallback, var rejection] }
                } || rejection is not
                {
                    Patterns: [], Guard: Expr.Literal { Value: Atom { Name: "true" } },
                    Body: Expr.Call
                    {
                        Function: "error", DynamicRemote: false,
                        Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "badarg" } }, Expr.Variable type] }]
                    } error
                }
                || type.Name != input.Type || !owner.ResolvesErlangBif(error)
                || sequence.Items[^2] is not Expr.Call
                {
                    Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                    Arguments: [
                        Expr.Call
                    {
                        Module: "wx", Function: "typeCast", DynamicRemote: false,
                        Arguments: [Expr.Variable castInput, Expr.Variable castType]
                    } cast,
                        Expr.Variable queued,
                        Expr.Case environment,
                        Expr.Literal { Value: Integer command }
                    ]
                } queue || castInput.Name != input.Object || castType.Name != selected.Name
                || sequence.Items[^1] is not Expr.Call
                {
                    Module: "wxe_util", Function: "rec", DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Integer received }]
                } receive || !command.Equals(received))
            {
                return null;
            }

            var bindings = new List<string> { input.Object, input.Type, selected.Name, first.Result, second.Result };
            var classes = new[] { (first.Class, first.Call), (second.Class, second.Call) };
            (Atom Class, Expr.Call First, Expr.Call Second, Expr.Call Final)? flags = null;
            (Expr.Fun Function, Expr.Call Map)? mapper = null;
            if (hasFlags)
            {
                if (clause.Guard is not null || ReadWxClassUnionObject(alternative) is not { } flag
                    || queued.Name != flag.Object || ReadWxClassTest(sequence.Items[3], flag.Type) is not { } finalCheck
                    || ReadWxClassedChoice(
                        preferred,
                        first.Result,
                        first.Class,
                        flag.Type
                    ) is not { } firstCheck || ReadWxClassedChoice(
                        fallback,
                        second.Result,
                        second.Class,
                        flag.Type
                    ) is not { } secondCheck || !finalCheck.Class.Equals(firstCheck.Class)
                    || !finalCheck.Class.Equals(secondCheck.Class))
                {
                    return null;
                }

                flags = (finalCheck.Class, firstCheck.Call, secondCheck.Call, finalCheck.Call);
                bindings.AddRange([flag.Object, flag.Type]);
            }
            else
            {
                if (alternative is not Pattern.Variable options || clause.Guard is not { } guard
                    || ReadForwardGuard(guard, [input.Object, options.Name])
                        is not ForwardGuard.Test { Opcode: IsList, Register: 1 }
                    || preferred is not
                    {
                        Patterns: [], Guard: Expr.Variable preferredFlag,
                        Body: Expr.Literal { Value: Atom preferredClass }
                    } || preferredFlag.Name != first.Result || !preferredClass.Equals(first.Class)
                    || fallback is not
                    {
                        Patterns: [], Guard: Expr.Variable fallbackFlag,
                        Body: Expr.Literal { Value: Atom fallbackClass }
                    } || fallbackFlag.Name != second.Result || !fallbackClass.Equals(second.Class)
                    || sequence.Items[3] is not Expr.Match { Pattern: Pattern.Variable mapperBinding, Value: Expr.Fun { SelfName: null } lambda }
                    || sequence.Items[4] is not Expr.Match
                    {
                        Pattern: Pattern.Variable mapped,
                        Value: Expr.Call
                        {
                            Module: "lists", Function: "map", DynamicRemote: false,
                            Arguments: [Expr.Variable mapperArgument, Expr.Variable mapInput]
                        } map
                    } || mapped.Name != queued.Name || mapperArgument.Name != mapperBinding.Name
                    || mapInput.Name != options.Name || !ReadWxOptions(lambda.Clauses, out _, out _))
                {
                    return null;
                }

                bindings.AddRange([options.Name, mapperBinding.Name, mapped.Name]);
                mapper = (lambda, map);
            }

            if (bindings.Contains(VariableScopeNames.Wildcard) || bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Count
                || !UsesFreshWxEnvironment(environment, bindings)
                || mapper is { } optionMapper && ClosureCaptures.Capture(
                    optionMapper.Function,
                    bindings.ToDictionary(name => name, _ => (Term)Nil.Value)
                ).Count != 0)
            {
                return null;
            }

            return new(
                classes,
                error,
                cast,
                queue,
                receive,
                command,
                flags,
                mapper
            );
        }

        private static (Atom Class, Expr.Call Call)? ReadWxClassedChoice(
            Clause choice,
            string result,
            Atom required,
            string secondaryType
        ) =>
            choice is
            {
                Patterns: [], Guard: Expr.Variable checkedFlag,
                Body: Expr.Sequence { Items: [var secondaryCheck, Expr.Literal { Value: Atom selection }] }
            } && checkedFlag.Name == result && selection.Equals(required)
                ? ReadWxClassTest(secondaryCheck, secondaryType) : null;

        private void EmitWxClassedConstructorArm(WxClassedConstructorArm arm)
        {
            const int frame = 5;
            const int token = 4;
            const int input = 3;
            const int secondary = 2;
            const int type = 1;
            const int preferred = 0;
            EmitFrameAllocation(3, frame);
            EmitFrameInitialization(0, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                type
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                secondary
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                input
            );
            BeamSourceLocation previous = location;
            EmitProtectedWxClass(
                token,
                XRegisterTag,
                2,
                preferred,
                arm.Classes[0].Class,
                arm.Classes[0].Call,
                ref previous
            );
            EmitProtectedWxClass(
                token,
                YRegisterTag,
                type,
                -1,
                arm.Classes[1].Class,
                arm.Classes[1].Call,
                ref previous
            );

            int missing;
            int invalid;
            if (arm.Flags is { } flags)
            {
                owner.Op(GetTupleElement, YRegisterTag, secondary);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(YRegisterTag, token);
                int firstValid = Label();
                int secondChoice = Label();
                int secondValid = Label();
                int selected = Label();
                int finalValid = Label();
                missing = Label();
                invalid = Label();
                EmitTrue(YRegisterTag, preferred, secondChoice);
                EmitFlagCheck(
                    flags.Class,
                    flags.First,
                    firstValid,
                    final: false
                );
                owner.EmitConstant(arm.Classes[0].Class, type, YRegisterTag);
                JumpTo(selected);
                Mark(secondChoice);
                EmitTrue(XRegisterTag, 0, invalid);
                EmitFlagCheck(
                    flags.Class,
                    flags.Second,
                    secondValid,
                    final: false
                );
                owner.EmitConstant(arm.Classes[1].Class, type, YRegisterTag);
                Mark(selected);
                EmitFlagCheck(
                    flags.Class,
                    flags.Final,
                    finalValid,
                    final: true
                );
                owner.MoveRegister(
                    YRegisterTag,
                    type,
                    XRegisterTag,
                    1
                );
                owner.MoveRegister(
                    YRegisterTag,
                    input,
                    XRegisterTag,
                    0
                );
                owner.MoveRegister(
                    YRegisterTag,
                    secondary,
                    YRegisterTag,
                    token
                );
                EmitTrim(4, 1);
            }
            else
            {
                int secondChoice = Label();
                int selected = Label();
                missing = Label();
                invalid = Label();
                EmitTrue(YRegisterTag, preferred, secondChoice);
                owner.EmitConstant(arm.Classes[0].Class, token, YRegisterTag);
                JumpTo(selected);
                Mark(secondChoice);
                EmitTrue(XRegisterTag, 0, invalid);
                owner.EmitConstant(arm.Classes[1].Class, token, YRegisterTag);
                Mark(selected);

                var (mapper, map) = arm.Mapper!.Value;
                owner.EmitClosureAllocation(0, 0, 1);
                EmitWxOptionMapper(mapper, 0);
                owner.MoveRegister(
                    YRegisterTag,
                    secondary,
                    XRegisterTag,
                    1
                );
                EmitTrim(3, 2);
                owner.EmitCallLine(map, previous);
                previous = CallLocation(map, previous);
                owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("lists", "map", 2));
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    2
                );
                owner.MoveRegister(
                    YRegisterTag,
                    1,
                    XRegisterTag,
                    1
                );
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    1
                );
                EmitTrim(1, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    XRegisterTag,
                    0
                );
            }

            owner.EmitCallLine(arm.Cast, previous);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wx", "typeCast", 2));
            EmitWxEnvironmentCheck(1, missing);
            owner.EmitTuplePart(1, 1, 2);
            owner.EmitConstant(arm.Command, 3);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            EmitTrim(1, 0);
            owner.EmitCallLine(arm.Queue, CallLocation(arm.Cast, previous));
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", 4));
            owner.EmitConstant(arm.Command, 0);
            owner.EmitCallLine(arm.Receive, CallLocation(arm.Queue, previous));
            Tail("wxe_util", "rec", 0);

            Mark(missing);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            owner.EmitCallLine(arm.Queue, CallLocation(arm.Receive, previous));
            Tail(ErlangModuleNames.Module, "error", frame);
            Mark(invalid);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(Term.A("badarg"));
            owner.Operand(YRegisterTag, type);
            owner.EmitCallLine(arm.Error, CallLocation(arm.Queue, previous));
            Tail(ErlangModuleNames.Module, "error", frame);

            void EmitFlagCheck(
                Atom expected,
                Expr.Call call,
                int valid,
                bool final
            )
            {
                owner.Op(ExactNotEqualTest, LabelTag, valid);
                owner.Operand(YRegisterTag, token);
                owner.LiteralOperand(expected);
                owner.MoveRegister(
                    YRegisterTag,
                    token,
                    XRegisterTag,
                    1
                );
                owner.EmitConstant(Term.A("parent_class"), 2);
                if (final)
                {
                    EmitFrameInitialization(token, token + 1);
                }
                else
                {
                    EmitFrameInitialization(0, 2);
                }

                owner.EmitConstant(expected, 0);
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
                Mark(valid);
            }

            void EmitTrue(int tag, int register, int failure)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(tag, register);
                owner.LiteralOperand(Term.A("true"));
            }

            void EmitTrim(int count, int remaining)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, count);
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }

            void Tail(string module, string function, int remaining)
            {
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, function, 1));
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }
        }
    }
}
