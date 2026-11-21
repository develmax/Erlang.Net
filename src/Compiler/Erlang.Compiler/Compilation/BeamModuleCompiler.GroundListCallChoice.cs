using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record GroundListCallArm(int[] Key, Expr.Call Call, int Entry);

        private bool TryEmitGroundListCallChoice(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable key],
                        Body: Expr.Sequence
                        {
                            Items: [
                                Expr.Match { Pattern: Pattern.Variable ownerObject, Value: Expr.Call create },
                                Expr.Call configure,
                                Expr.Match { Pattern: Pattern.Variable container, Value: Expr.Call child },
                                Expr.Match { Pattern: Pattern.Variable selected, Value: Expr.Case decision },
                                Expr.Call attach,
                                Expr.Call fit,
                                ..]
                        } body
                    }
                ] || body.Items.Count < 7 || !DistinctBindings(
                    parent,
                    key,
                    ownerObject,
                    container,
                    selected
                ) || decision.Value is not Expr.Variable discriminant || discriminant.Name != key.Name
                || decision.Clauses.Count < 2 || create is not { Module: not null, DynamicRemote: false }
                || !MatchesCallBindings(configure, [ownerObject.Name])
                || !MatchesCallBindings(attach, [container.Name, selected.Name])
                || !MatchesCallBindings(fit, [selected.Name, container.Name])
                || child is not { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable frame, var arguments] }
                || frame.Name != ownerObject.Name || !AttributeLiteral.TryNormalizeConstant(arguments, out Term childOptions)
                || ReadEntryCall(create, [parent.Name, key.Name]) is not { } first)
            {
                return false;
            }

            var branches = new List<GroundListCallArm>();
            foreach (var clause in decision.Clauses)
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [var pattern],
                        Body: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable field]
                        } call
                    } || field.Name != container.Name || ReadGroundIntegerList(pattern) is not { Length: > 0 } text
                    || !entries.TryGetValue((call.Function, 1), out int entry)
                    || branches.Any(branch => IsPrefix(branch.Key, text) || IsPrefix(text, branch.Key)))
                {
                    return false;
                }

                branches.Add(new(text, call, entry));
            }

            var continuation = new List<EntryCall>();
            foreach (Expr expression in body.Items.Skip(6))
            {
                if (expression is not Expr.Call { Module: not null, DynamicRemote: false } call
                    || ReadEntryCall(call, [], [ownerObject.Name]) is not { } prepared
                    || prepared.SavedInputs is not [{ Destination: 0, Source: 0 }]
                    || prepared.ConstructedArgument is not null)
                {
                    return false;
                }

                continuation.Add(prepared);
            }

            const int keySlot = 2;
            const int ownerSlot = 1;
            const int containerSlot = 0;
            EmitFrameAllocation(2, 3);
            EmitFrameInitialization(0, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                keySlot
            );
            EmitEntryCall(first);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                ownerSlot
            );
            var previous = CallLocation(create, location);
            EmitEffect(configure, ref previous);
            owner.EmitConstant(childOptions, 1);
            owner.MoveRegister(
                YRegisterTag,
                ownerSlot,
                XRegisterTag,
                0
            );
            EmitEffect(child, ref previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                containerSlot
            );

            int joined = owner.nextLabel + CountBranches(branches);
            int rejected = joined + 1;
            BeamSourceLocation branchLocation = previous;
            EmitChoice(
                branches,
                YRegisterTag,
                keySlot,
                last: true
            );
            Mark(Label());
            owner.MoveRegister(
                YRegisterTag,
                keySlot,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                containerSlot,
                XRegisterTag,
                0
            );
            EmitEffect(attach, ref previous);
            owner.MoveRegister(
                YRegisterTag,
                containerSlot,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                keySlot,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                ownerSlot,
                YRegisterTag,
                keySlot
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitEffect(fit, ref previous);
            const int retainedOwnerSlot = 0;
            foreach (var step in continuation.SkipLast(1))
            {
                EmitContinuationArguments(step);
                EmitEffect(step.Call, ref previous);
            }

            EmitContinuationArguments(continuation[^1]);
            EmitPreparedFrameTail(continuation[^1], previous, 1);
            Mark(Label());
            owner.EmitLine(new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine));
            owner.Op(CaseEnd, YRegisterTag, keySlot);

            return true;

            void EmitContinuationArguments(EntryCall call)
            {
                EmitEntryCallArguments(call);
                owner.MoveRegister(
                    YRegisterTag,
                    retainedOwnerSlot,
                    XRegisterTag,
                    0
                );
            }

            void EmitEffect(Expr.Call call, ref BeamSourceLocation site)
            {
                owner.EmitCallLine(call, site);
                owner.EmitStaticRemoteCall(call);
                site = CallLocation(call, site);
            }

            void EmitChoice(
                IReadOnlyList<GroundListCallArm> arms,
                int tag,
                int source,
                bool last
            )
            {
                if (arms is [var leaf])
                {
                    owner.Op(leaf.Key.Length == 0 ? IsNil : ExactEqualTest, LabelTag, rejected);
                    owner.Operand(tag, source);
                    if (leaf.Key.Length != 0)
                    {
                        owner.LiteralOperand(Term.List(leaf.Key.Select(value => (Term)Term.I(value)).ToArray()));
                    }

                    EmitFrameInitialization(keySlot, keySlot + 1);
                    owner.MoveRegister(
                        YRegisterTag,
                        containerSlot,
                        XRegisterTag,
                        0
                    );
                    owner.EmitCallLine(leaf.Call, branchLocation);
                    owner.Op(LocalCall, BeamWire.UnsignedTag, 1);
                    owner.Operand(LabelTag, leaf.Entry);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        keySlot
                    );
                    if (!last)
                    {
                        JumpTo(joined);
                    }

                    return;
                }

                owner.Op(IsNonemptyList, LabelTag, rejected);
                owner.Operand(tag, source);
                owner.Op(GetList, tag, source);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 1);
                var heads = SplitHeads(arms);
                if (heads is [var shared])
                {
                    owner.Op(ExactEqualTest, LabelTag, rejected);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(Term.I(shared.Head));
                    EmitChoice(
                        shared.Arms,
                        XRegisterTag,
                        1,
                        last
                    );

                    return;
                }

                int next = owner.nextLabel;
                var entries = new int[heads.Length];
                for (int index = 0; index < heads.Length; index++)
                {
                    entries[index] = next;
                    next += 1 + CountBranches(heads[index].Arms);
                }

                owner.Op(SelectValue, XRegisterTag, 0);
                owner.Operand(LabelTag, rejected);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2 * heads.Length);
                for (int index = heads.Length - 1; index >= 0; index--)
                {
                    owner.LiteralOperand(Term.I(heads[index].Head));
                    owner.Operand(LabelTag, entries[index]);
                }

                for (int index = 0; index < heads.Length; index++)
                {
                    Mark(Label());
                    EmitChoice(
                        heads[index].Arms,
                        XRegisterTag,
                        1,
                        last && index == heads.Length - 1
                    );
                }
            }

            static int CountBranches(IReadOnlyList<GroundListCallArm> arms)
            {
                if (arms.Count <= 1)
                {
                    return 0;
                }

                var split = SplitHeads(arms);

                return (split.Length == 1 ? 0 : split.Length) + split.Sum(group => CountBranches(group.Arms));
            }

            static (int Head, GroundListCallArm[] Arms)[] SplitHeads(IReadOnlyList<GroundListCallArm> arms) =>
                arms.GroupBy(arm => arm.Key[0]).OrderByDescending(group => group.Key).Select(group =>
                    (group.Key, group.Select(arm => arm with { Key = arm.Key[1..] }).ToArray())).ToArray();

            static bool IsPrefix(int[] prefix, int[] value) => value.AsSpan().StartsWith(prefix);

            static bool MatchesCallBindings(Expr.Call call, string[] bindings) =>
                call is { Module: not null, DynamicRemote: false }
                && call.Arguments.SequenceEqual(bindings.Select(name => new Expr.Variable(name)));
        }

        private static int[]? ReadGroundIntegerList(Pattern pattern)
        {
            var values = new List<int>();
            if (pattern is Pattern.List { Tail: null } list)
            {
                foreach (var item in list.Items)
                {
                    if (item is not Pattern.Literal { Value: Integer value } || value.Value < int.MinValue || value.Value > int.MaxValue)
                    {
                        return null;
                    }

                    values.Add((int)value.Value);
                }
            }
            else if (pattern is Pattern.Literal literal)
            {
                Term tail = literal.Value;
                while (tail is Cons { Head: Integer value, Tail: var next })
                {
                    if (value.Value < int.MinValue || value.Value > int.MaxValue)
                    {
                        return null;
                    }

                    values.Add((int)value.Value);
                    tail = next;
                }

                if (tail is not Nil)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            return values.ToArray();
        }
    }
}
