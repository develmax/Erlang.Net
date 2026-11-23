using AstPattern = Erlang.Pattern;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedTwoConsPattern(
            Clause clause,
            int failure,
            int nonCons,
            bool checkCons
        )
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [
                        AstPattern.List
                    {
                        Items: [
                                AstPattern.Tuple
                        {
                            Items: [AstPattern.Literal { Value: Atom firstTag }, AstPattern.Variable shared]
                        },
                                AstPattern.Tuple
                        {
                            Items: [
                                        AstPattern.Literal { Value: Atom secondTag }, AstPattern.Variable repeated,
                                        AstPattern.Literal { Value: Atom or Integer } marker
                                    ]
                        }
                            ],
                        Tail: AstPattern.Variable tail
                    }
                    ],
                    Body: Expr.List
                    {
                        Items: [Expr.Tuple],
                        Tail: Expr.Call { Arguments: [Expr.Variable rest] }
                    }
                }
                || shared.Name != repeated.Name || tail.Name != rest.Name || !DistinctBindings(shared, tail))
            {
                return false;
            }

            const int firstHead = 1;
            const int secondHead = 2;
            const int suffix = 3;
            const int check = 4;
            string[] bindings =
            [
                VariableScopeNames.Wildcard, VariableScopeNames.Wildcard, VariableScopeNames.Wildcard,
                tail.Name, shared.Name
            ];
            if (ReadConsCallResult(clause.Body, bindings) is not { Saved: [check] } result)
            {
                return false;
            }

            if (checkCons)
            {
                owner.EmitNonemptyList(0, nonCons);
            }

            owner.EmitListParts(
                0,
                XRegisterTag,
                firstHead,
                XRegisterTag,
                0
            );
            EmitDetailRecordTest(
                firstHead,
                2,
                firstTag,
                failure
            );
            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(
                0,
                XRegisterTag,
                secondHead,
                XRegisterTag,
                suffix
            );
            EmitDetailRecordTest(
                secondHead,
                3,
                secondTag,
                failure
            );

            // The rejected prefix still owns the head and tail in X1 and X0.
            // Once the literal matches, its register holds the shared field.
            owner.EmitTuplePart(secondHead, 2, check);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, check);
            owner.LiteralOperand(marker.Value);
            owner.EmitTuplePart(secondHead, 1, secondHead);
            owner.EmitTuplePart(firstHead, 1, check);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, secondHead);
            owner.Operand(XRegisterTag, check);

            EmitConsCallResult(result, live: check + 1);

            return true;
        }
    }
}
