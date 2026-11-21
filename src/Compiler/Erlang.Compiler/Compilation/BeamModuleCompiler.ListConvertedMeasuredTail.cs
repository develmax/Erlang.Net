using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListConvertedMeasuredTail(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable input, Pattern.Variable tag],
                    Guard: Expr.Call
                    {
                        Function: AutoImportNames.IsList, Arguments: [Expr.Variable tested]
                    } guard,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Call
                            {
                                Function: AutoImportNames.ListToBinary, Arguments: [Expr.Variable raw]
                            } convert,
                                Expr.Variable recursiveTag
                            ]
                    } recur
                },
                {
                    Guard: null, Patterns: [Pattern.Variable normalized, Pattern.Variable carriedTag],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Variable tailTag,
                                Expr.Variable output,
                                Expr.Call
                            {
                                Function: AutoImportNames.ByteSize, Arguments: [Expr.Variable measured]
                            } measure
                            ]
                    } tail
                }
                ]
                || !DistinctBindings(input, tag) || !DistinctBindings(normalized, carriedTag)
                || input.Name != tested.Name || input.Name != raw.Name || tag.Name != recursiveTag.Name
                || normalized.Name != output.Name || normalized.Name != measured.Name || carriedTag.Name != tailTag.Name
                || recur.Function != function.Name
                || !owner.ResolvesErlangBif(guard) || !owner.ResolvesErlangBif(convert) || !owner.ResolvesErlangBif(measure)
                || !entries.TryGetValue((function.Name, 2), out int recurEntry)
                || !entries.TryGetValue((tail.Function, 3), out int tailEntry))
            {
                return false;
            }

            bool nilTag = HasNilRecursiveTag(function, recur);
            int converted = Label();
            owner.Op(IsList, LabelTag, converted);
            owner.Operand(XRegisterTag, 0);
            int frameSize = nilTag ? 0 : 1;
            owner.Op(Allocate, BeamWire.UnsignedTag, frameSize);
            owner.Operand(BeamWire.UnsignedTag, nilTag ? 1 : 2);
            if (!nilTag)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
            }

            owner.EmitCallLine(convert, location);
            Call(ErlangModuleNames.Module, convert.Function, 1);
            if (nilTag)
            {
                owner.EmitConstant(Nil.Value, 1);
            }
            else
            {
                Load(0, 1);
            }

            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, recurEntry);
            owner.Operand(BeamWire.UnsignedTag, frameSize);
            Mark(converted);
            EmitMeasuredTail(
                measure,
                tailEntry,
                test: null,
                typeBits: 0,
                failure: 0,
                nilTag
            );

            return true;
        }

        private bool HasNilRecursiveTag(FunctionDefinition function, Expr.Call recursive)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return false;
            }

            bool hasSeed = false;
            bool bounded = HasOnlyLocalCalls(
                function,
                2,
                (_, invocation) =>
                {
                    if (ReferenceEquals(invocation, recursive))
                    {
                        return true;
                    }

                    if (ReadLocalSingletonArgument(invocation.Arguments[1]) is not Nil)
                    {
                        return false;
                    }

                    hasSeed = true;

                    return true;
                }
            );

            return bounded && hasSeed;
        }
    }
}
