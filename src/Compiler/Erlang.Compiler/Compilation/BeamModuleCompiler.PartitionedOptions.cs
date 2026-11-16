using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private (Atom Key, Expr.Call Validate, Expr.Call Delete)? ReadRetainedOption(Clause clause, string function)
        {
            if (clause is not { Guard: null, Patterns: [
                Pattern.List { Items: [AliasPattern alias], Tail: Pattern.Variable tail },
                Pattern.Variable native, Pattern.Variable emulated
            ], Body: Expr.Sequence { Items: [
                Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Literal { Value: Atom key }, Expr.Variable checkedValue] } validate,
                Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                    Expr.Variable rest, Expr.Variable carried,
                    Expr.List { Items: [Expr.Variable retained], Tail: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Atom deletedKey }, Expr.Variable original] } delete }
                ] } recurse
            ] } } || !TryReadPatternAlias(alias, out var option, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom matched }, Pattern.Variable value] }
                || !DistinctBindings(option, value, tail, native, emulated)
                || !matched.Equals(key) || !matched.Equals(deletedKey)
                || checkedValue.Name != value.Name || rest.Name != tail.Name || carried.Name != native.Name
                || retained.Name != option.Name || original.Name != emulated.Name || recurse.Function != function
                || owner.ResolvesErlangBif(delete))
            {
                return null;
            }

            return (key, validate, delete);
        }

        private (Atom Key, Atom Error, Atom Context, Atom Reason, Expr.Call Call)? ReadRejectedOption(Clause clause)
        {
            if (clause is not { Guard: null, Patterns: [
                Pattern.List { Items: [AliasPattern alias], Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard } },
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
            ], Body: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: AutoImportNames.Throw,
                Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom error }, Expr.Tuple { Items: [
                    Expr.Literal { Value: Atom context }, Expr.Tuple { Items: [Expr.Literal { Value: Atom reason }, Expr.Variable rejected] }
                ] }] }]
            } call } || !TryReadPatternAlias(alias, out var option, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom key }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }
                || option.Name == VariableScopeNames.Wildcard || option.Name != rejected.Name || !owner.ResolvesErlangBif(call))
            {
                return null;
            }

            return (key, error, context, reason, call);
        }

        private bool TryEmitPartitionedOptions(FunctionDefinition function)
        {
            if (function.Clauses is not [var first, var error1, var second, var error2, var error3, {
                Guard: null, Patterns: [
                    Pattern.List { Items: [Pattern.Variable option], Tail: Pattern.Variable tail },
                    Pattern.Variable native, Pattern.Variable emulated
                ], Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                    Expr.Variable rest, Expr.List { Items: [Expr.Variable head], Tail: Expr.Variable nativeArg }, Expr.Variable emulatedArg
                ] } recurse
            }, {
                Guard: null, Patterns: [var nil, Pattern.Variable collected, Pattern.Variable extracted],
                Body: Expr.Tuple { Items: [
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable reversed] } reverse, Expr.Variable result
                ] }
            }] || ReadRetainedOption(first, function.Name) is not { } initial
                || ReadRetainedOption(second, function.Name) is not { } other
                || ReadRejectedOption(error1) is not { } rejected1 || ReadRejectedOption(error2) is not { } rejected2
                || ReadRejectedOption(error3) is not { } rejected3
                || !rejected1.Error.Equals(rejected2.Error) || !rejected1.Error.Equals(rejected3.Error)
                || !rejected1.Context.Equals(rejected2.Context) || !rejected1.Context.Equals(rejected3.Context)
                || !rejected1.Reason.Equals(rejected2.Reason) || !rejected1.Reason.Equals(rejected3.Reason)
                || new HashSet<Atom> { initial.Key, other.Key, rejected1.Key, rejected2.Key, rejected3.Key }.Count != 5
                || !DistinctBindings(option, tail, native, emulated) || !DistinctBindings(collected, extracted)
                || rest.Name != tail.Name || head.Name != option.Name || nativeArg.Name != native.Name || emulatedArg.Name != emulated.Name
                || recurse.Function != function.Name || !IsEmptyListPattern(nil) || reversed.Name != collected.Name || result.Name != extracted.Name
                || owner.ResolvesErlangBif(reverse)
                || !entries.TryGetValue((function.Name, 3), out int entry)
                || !entries.TryGetValue((initial.Validate.Function, 2), out int firstValidator)
                || !entries.TryGetValue((other.Validate.Function, 2), out int secondValidator))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int retained = Label();
            int rejected = Label();
            int secondRetained = Label();
            int ordinary = Label();
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(0, XRegisterTag, 3, XRegisterTag, 0);
            owner.EmitTupleCheck(3, 2, ordinary);
            owner.EmitTuplePart(3, 0, 4);
            owner.EmitTuplePart(3, 1, 5);
            owner.Op(SelectValue, XRegisterTag, 4);
            owner.Operand(LabelTag, ordinary);
            owner.instructions.WriteByte(ExtendedListOperand);
            (Atom Key, int Label)[] choices = [
                (initial.Key, retained), (rejected1.Key, rejected), (other.Key, secondRetained),
                (rejected2.Key, rejected), (rejected3.Key, rejected)
            ];
            owner.Operand(BeamWire.UnsignedTag, choices.Length * 2);
            foreach (var (key, label) in choices.OrderBy(choice => choice.Key.Name, StringComparer.Ordinal))
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, label);
            }

            Mark(retained);
            EmitRetained(initial.Key, initial.Validate, initial.Delete, firstValidator);
            Mark(rejected);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(rejected1.Reason.Name)), (XRegisterTag, 3));
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(rejected1.Context.Name)), (XRegisterTag, 0));
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(rejected1.Error.Name)), (XRegisterTag, 0));
            owner.EmitCallLine(rejected1.Call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Throw, 1));
            Mark(secondRetained);
            EmitRetained(other.Key, other.Validate, other.Delete, secondValidator);
            Mark(ordinary);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Op(PutList, XRegisterTag, 3);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);
            Mark(empty);
            owner.EmitNilTest(XRegisterTag, 0, failure);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            var previous = location;
            EmitWindowCall(reverse, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            owner.EmitFrameReturn(1);

            return true;

            void EmitRetained(Atom key, Expr.Call validate, Expr.Call delete, int validator)
            {
                owner.Op(Allocate, BeamWire.UnsignedTag, 4);
                owner.Operand(BeamWire.UnsignedTag, 6);
                owner.MoveRegister(XRegisterTag, 3, YRegisterTag, 0);
                SaveWindowResult(1);
                owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 2);
                owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 3);
                owner.MoveRegister(XRegisterTag, 5, XRegisterTag, 1);
                owner.EmitConstant(key, 0);
                var current = location;
                EmitWindowLocal(validate, validator, ref current);
                Load(2, 1);
                owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 2);
                TrimDetailFrame(1, 3);
                owner.EmitConstant(key, 0);
                EmitWindowCall(delete, ref current);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(PutList, YRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 2);
                Load(2, 1);
                Load(0);
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
                owner.Operand(LabelTag, entry);
                owner.Operand(BeamWire.UnsignedTag, 3);
            }
        }
    }
}
