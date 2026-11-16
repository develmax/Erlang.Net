using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedParserResult(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable argument] } read,
                        Clauses: [var errorClause, var successClause, ..] } decision
                }
            ] || argument.Name != input.Name || errorClause is not
                {
                    Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom errorTag }, Pattern.Variable errorInfo, ..] } errorPattern],
                    Body: Expr.Call { Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom context },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom reason }, Expr.Variable info, ..] } details
                    ] }] } error
                } || info.Name != errorInfo.Name || !IsConfigurationError(error)
                || successClause is not
                {
                    Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom successTag }, var payload, ..] } successPattern],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable output] } next
                } || errorTag.Equals(successTag) || !entries.TryGetValue((next.Function, 1), out int nextEntry))
            {
                return false;
            }

            Expr.Call? badPayload = null;
            bool carriesLocation = decision.Clauses.Count == 2
                && errorPattern.Items is [_, _, Pattern.Variable errorLocation]
                && successPattern.Items is [_, Pattern.Variable successPayload, Pattern.Variable ignored]
                && details.Items is [_, _, Expr.Variable reportedLocation]
                && reportedLocation.Name == errorLocation.Name && output.Name == successPayload.Name
                && DistinctBindings(input, errorInfo, errorLocation)
                && DistinctBindings(input, successPayload)
                && (ignored.Name == VariableScopeNames.Wildcard || DistinctBindings(input, successPayload, ignored));
            if (!carriesLocation)
            {
                if (decision.Clauses is not [_, _, {
                    Guard: null, Patterns: [Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom fallbackTag }, Pattern.Variable other
                    ] }],
                    Body: Expr.Call { Arguments: [Expr.Tuple { Items: [
                        Expr.Literal { Value: Atom otherContext },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom otherReason }, Expr.Variable otherValue] }
                    ] }] } fallback
                }] || errorPattern.Items.Count != 2 || details.Items.Count != 2
                    || successPattern.Items.Count != 2 || payload is not Pattern.List { Tail: null, Items: [Pattern.Variable item] }
                    || item.Name != output.Name || !DistinctBindings(input, item) || !DistinctBindings(input, errorInfo)
                    || !DistinctBindings(input, other) || other.Name != otherValue.Name || !fallbackTag.Equals(successTag)
                    || !IsConfigurationError(fallback) || !context.Equals(otherContext) || !reason.Equals(otherReason))
                {
                    return false;
                }

                badPayload = fallback;
            }

            int success = Label();
            int malformed = carriesLocation ? 0 : Label();
            int failure = Label();
            int unmatched = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            var previous = location;
            EmitWindowCall(read, ref previous);
            owner.EmitTupleCheck(0, carriesLocation ? 3 : 2, unmatched);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            if (TermOrder.Compare(errorTag, successTag, exact: true) < 0)
            {
                owner.EmitAtomSelection(1, unmatched, errorTag.Name, failure, successTag.Name, success);
            }
            else
            {
                owner.EmitAtomSelection(1, unmatched, successTag.Name, success, errorTag.Name, failure);
            }

            Mark(success);
            if (carriesLocation)
            {
                owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
            }
            else
            {
                owner.EmitNonemptyList(2, malformed);
                owner.EmitListParts(2, XRegisterTag, 0, XRegisterTag, 1);
                owner.EmitNilTest(XRegisterTag, 1, malformed);
            }

            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, nextEntry);
            owner.Operand(BeamWire.UnsignedTag, 0);
            if (badPayload is not null)
            {
                Mark(malformed);
                EmitNestedConfigurationError(badPayload, context, reason, 2, 3, 0, previous);
            }

            Mark(failure);
            EmitNestedConfigurationError(error, context, reason, 2, 3, 0, previous, carriesLocation);
            Mark(unmatched);
            EmitConfigurationCaseFailure(decision, previous);

            return true;
        }

        private bool IsConfigurationError(Expr.Call call) =>
            call is { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Error, DynamicRemote: false }
            && owner.ResolvesErlangBif(call);

        private void EmitNestedConfigurationError(
            Expr.Call call,
            Atom context,
            Atom reason,
            int payload,
            int live,
            int slots,
            BeamSourceLocation previous,
            bool carriesLocation = false
        )
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, carriesLocation ? 7 : 6);
            owner.Operand(BeamWire.UnsignedTag, live);
            if (carriesLocation)
            {
                owner.EmitTuplePart(0, 2, 0);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(reason.Name)), (XRegisterTag, payload), (XRegisterTag, 0));
            }
            else
            {
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(reason.Name)), (XRegisterTag, payload));
            }

            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(context.Name)), (XRegisterTag, 0));
            EmitConfigurationTail(call with { Module = ErlangModuleNames.Module }, slots, previous);
        }

        private void EmitConfigurationCaseFailure(Expr.Case decision, BeamSourceLocation previous)
        {
            owner.EmitLine(new(decision.PreprocessorFile ?? previous.File, decision.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);
        }
    }
}
