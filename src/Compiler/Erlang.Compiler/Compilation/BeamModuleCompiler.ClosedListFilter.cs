using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ListMapperFilter(Expr Expression, IReadOnlyList<ListMapperCall> Calls, bool Negate);

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecursiveListBooleanFilter(FunctionDefinition function)
        {
            if (!function.IsRecursiveListMapper || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.Case
                    {
                        Value: Expr.Call predicate,
                        Clauses: [{ Body: Expr.List { Items: [var value] } }, _, _]
                    }
                })
            {
                return false;
            }

            var calls = new List<ListMapperCall>();
            if (value is Expr.Variable returned && returned.Name == item.Name && function.Arity == 1
                && !function.IsStrictRecursiveListMapper && IsExhaustiveLocalBooleanSelector(predicate)
                && AppendListMapperCalls(predicate, item.Name, calls))
            {
                EmitClosedListFilter([], new(predicate, calls, Negate: false), (function, null));

                return true;
            }

            if (value is not Expr.Tuple tuple)
            {
                return false;
            }

            var fields = new Term?[tuple.Items.Count];
            for (int index = 0; index < tuple.Items.Count; index++)
            {
                Expr field = tuple.Items[index];
                if (AttributeLiteral.TryNormalizeConstant(field, out Term constant))
                {
                    fields[index] = constant;
                }
                else if (field is not Expr.Variable variable || variable.Name != item.Name)
                {
                    return false;
                }
            }

            if (fields.All(field => field is not null) || !AppendListMapperCalls(predicate, item.Name, calls))
            {
                return false;
            }

            EmitClosedListFilter([], new(predicate, calls, Negate: false), (function, fields));

            return true;
        }

        private void EmitClosedListFilter(
            IReadOnlyList<ListMapperCall> mapperCalls,
            ListMapperFilter filter,
            (FunctionDefinition Function, Term?[]? Fields)? projection = null
        )
        {
            var predicate = filter.Calls[^1].Call;
            int arity = predicate.Arguments.Count;
            var signature = (predicate.Function, arity);
            bool local = predicate.Module is null && localFunctions.ContainsKey(signature);
            string? module = local ? null : predicate.Module ?? owner.ResolveExternalModule(predicate.Function, arity);
            bool listMembership = module == ListModuleNames.Module && signature is
                (ListModuleNames.Member, 2) or (ListModuleNames.KeyMember, 3);
            bool boolean = listMembership || local && !owner.IsDeclaredNative(signature)
                && localFunctions[signature].Clauses.All(clause => IsBooleanResult(clause.Body));

            int entry = owner.nextLabel - 1;
            int first = Label();
            int second = boolean ? 0 : Label();
            int invalid = boolean ? 0 : Label();
            int empty = Label();
            int badGenerator = projection is null ? Label() : 0;
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(1, 2);
            owner.EmitListParts(
                0,
                YRegisterTag,
                1,
                YRegisterTag,
                0
            );
            bool loadAfterConstants = listMembership && filter.Calls is [{ Arguments: [null, ..] } stage]
                && stage.Arguments.Skip(1).All(argument => argument is not null);
            if (!loadAfterConstants)
            {
                Load(1);
            }

            var current = EmitListMapperCalls(filter.Calls, location, loadAfterConstants);
            if (boolean)
            {
                owner.EmitAtomTest(0, "true", first);
            }
            else
            {
                owner.Op(SelectValue, XRegisterTag, 0);
                owner.Operand(LabelTag, invalid);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 4);
                owner.LiteralOperand(Term.A("false"));
                owner.Operand(LabelTag, second);
                owner.LiteralOperand(Term.A("true"));
                owner.Operand(LabelTag, first);
                Mark(first);
            }

            if (filter.Negate)
            {
                EmitSkip();
                Mark(boolean ? first : second);
                EmitKeep();
            }
            else
            {
                EmitKeep();
                Mark(boolean ? first : second);
                EmitSkip();
            }

            if (!boolean)
            {
                Mark(invalid);
                if (filter.Negate)
                {
                    owner.EmitConstant(Term.A(ErlangErrorReasons.BadArgument), 0);
                }
                else
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                    owner.EmitProtectedPair(AtomTag, owner.Intern(ComprehensionErrorReasons.BadFilter));
                }

                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Error, 1));
                owner.Operand(BeamWire.UnsignedTag, 2);
            }

            if (projection is { } projected)
            {
                EmitRecursiveListMappingEnd(projected.Function, empty);
            }
            else
            {
                owner.EmitMappingEnd(empty, badGenerator);
            }

            void EmitKeep()
            {
                var active = current;
                if (mapperCalls.Count != 0)
                {
                    Load(1);
                    EmitFrameInitialization(1, 2);
                    active = EmitListMapperCalls(mapperCalls, active);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        1
                    );
                }

                Load(0);
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
                if (active != location)
                {
                    owner.EmitLine(location);
                }

                owner.EmitFieldCall(1, entry);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + (projection?.Fields is { } fields ? fields.Length + 1 : 0));
                owner.Operand(BeamWire.UnsignedTag, 1);
                if (projection?.Fields is { } tupleFields)
                {
                    owner.Op(PutTuple, XRegisterTag, 1);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, tupleFields.Length);
                    foreach (Term? field in tupleFields)
                    {
                        if (field is null)
                        {
                            owner.Operand(YRegisterTag, 0);
                        }
                        else
                        {
                            owner.LiteralOperand(field);
                        }
                    }
                }

                owner.Op(PutList, projection?.Fields is null ? YRegisterTag : XRegisterTag, projection?.Fields is null ? 0 : 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(1);
            }

            void EmitSkip()
            {
                Load(0);
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, entry);
                owner.Operand(BeamWire.UnsignedTag, 2);
            }
        }

        private bool IsExhaustiveLocalBooleanSelector(Expr expression)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.OnLoadFunction is not null || owner.SourceModule?.DeclaredNifs is { Count: > 0 }
                || expression is not Expr.Call { Module: null, DynamicRemote: false } call
                || owner.ResolvesErlangBif(call))
            {
                return false;
            }

            var target = (call.Function, call.Arguments.Count);

            return localFunctions.TryGetValue(target, out var function)
                && !owner.IsDeclaredNative(target)
                && function.Clauses.All(clause => IsBooleanResult(clause.Body));
        }

        private static bool IsBooleanResult(Expr expression) => expression switch
        {
            Expr.Literal { Value: Atom { Name: "true" or "false" } } => true,
            Expr.Block block => IsBooleanResult(block.Body),
            Expr.Sequence { Items: [.., var result] } => IsBooleanResult(result),
            Expr.Case decision => decision.Clauses.All(branch => IsBooleanResult(branch.Body)),
            Expr.If decision => decision.Clauses.All(branch => IsBooleanResult(branch.Body)),
            _ => false
        };
    }
}
