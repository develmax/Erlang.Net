using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAliasedListHeadGuardTails(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                {
                    Patterns: [AliasPattern first, Pattern.Variable firstOption, Pattern.Variable firstTags],
                    Guard: Expr.Call
                    {
                        Function: AutoImportNames.IsAtom, DynamicRemote: false,
                        Arguments: [Expr.Variable checkedHead]
                    } atomGuard,
                    Body: Expr.Call named
                },
                {
                    Patterns: [AliasPattern second, Pattern.Variable secondOption, Pattern.Variable secondTags],
                    Guard: null,
                    Body: Expr.Call tagged
                },
                {
                    Patterns: [
                            Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable emptyOption,
                            Pattern.Variable emptyTags
                        ],
                    Guard: null,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [{ } empty, Expr.Variable emptyTailTags]
                    } emptyTail
                },
                {
                    Patterns: [Pattern.Variable source, Pattern.Variable binaryOption, Pattern.Variable binaryTags],
                    Guard: Expr.Call
                    {
                        Function: ErlangModuleNames.IsBitString, DynamicRemote: false,
                        Arguments: [Expr.Variable checkedBinary]
                    } binaryGuard,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable binaryTailValue, Expr.Variable binaryTailTags]
                    } binaryTail
                }
                ]
                || !TryReadPatternAlias(first, out var namedList, out var namedPattern)
                || namedPattern is not Pattern.List
                {
                    Items: [Pattern.Variable head],
                    Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                }
                || !TryReadPatternAlias(second, out var taggedList, out var taggedPattern)
                || taggedPattern is not Pattern.List
                {
                    Items: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                    Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                }
                || !DistinctBindings(
                    namedList,
                    head,
                    firstOption,
                    firstTags
                )
                || !DistinctBindings(taggedList, secondOption, secondTags)
                || !DistinctBindings(source, binaryTags) || !DistinctBindings(emptyTags)
                || binaryOption.Name == source.Name || binaryOption.Name == binaryTags.Name || emptyOption.Name == emptyTags.Name
                || checkedHead.Name != head.Name || checkedBinary.Name != source.Name
                || !owner.ResolvesErlangBif(atomGuard) || !owner.ResolvesErlangBif(binaryGuard)
                || !Forwards(
                    named,
                    namedList,
                    firstOption,
                    firstTags
                ) || !Forwards(
                    tagged,
                    taggedList,
                    secondOption,
                    secondTags
                )
                || named.Function != tagged.Function || emptyTail.Function != binaryTail.Function
                || emptyTailTags.Name != emptyTags.Name || binaryTailTags.Name != binaryTags.Name
                || binaryTailValue.Name != source.Name || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyValue)
                || emptyValue is not BitString { BitLength: 0 }
                || !entries.TryGetValue((named.Function, 3), out int namedEntry)
                || !entries.TryGetValue((binaryTail.Function, 2), out int binaryEntry))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int taggedHead = Label();
            int emptyList = Label();
            int bitstring = Label();
            owner.Op(IsNonemptyList, LabelTag, emptyList);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetHead, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 3);
            owner.Op(IsAtom, LabelTag, taggedHead);
            owner.Operand(XRegisterTag, 3);
            EmitTailLocal(namedEntry, 3, null);

            Mark(taggedHead);
            owner.Op(IsTaggedTuple, LabelTag, bitstring);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            EmitTailLocal(namedEntry, 3, null);

            Mark(emptyList);
            owner.EmitNilTest(XRegisterTag, 0, bitstring);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                1
            );
            owner.EmitConstant(emptyValue, 0);
            EmitTailLocal(binaryEntry, 2, null);

            Mark(bitstring);
            owner.Op(IsBitString, LabelTag, failure);
            owner.EmitTypedRegister(0, NonNilTypeBits);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                1
            );
            EmitTailLocal(binaryEntry, 2, null);

            return true;

            static bool Forwards(
                Expr.Call call,
                Pattern.Variable list,
                Pattern.Variable options,
                Pattern.Variable tags
            ) => call is
            {
                Module: null, DynamicRemote: false,
                Arguments: [Expr.Variable actualList, Expr.Variable actualOptions, Expr.Variable actualTags]
            } && actualList.Name == list.Name && actualOptions.Name == options.Name && actualTags.Name == tags.Name;
        }
    }
}
