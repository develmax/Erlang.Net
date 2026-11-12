using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPathOptionList(FunctionDefinition function)
        {
            if (function.Clauses is not [var first, var second, {
                Guard: null, Patterns: [Pattern.Variable role],
                Body: Expr.Sequence { Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable directory,
                        Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [Expr.List { Items: [Expr.Call
                            {
                                Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [var rootArgument]
                            } root, ..], Tail: null } rootList] } join
                    },
                    Expr.List { Tail: null } options
                ] }
            }] || !DistinctBindings(role, directory)
                || ReadOptionRedirect(first, function.Name) is not { } a
                || ReadOptionRedirect(second, function.Name) is not { } b || a.Key.Equals(b.Key)
                || !entries.TryGetValue((function.Name, 1), out int entry)
                || !AttributeLiteral.TryNormalizeConstant(rootArgument, out Term library)
                || !AttributeLiteral.TryNormalizeConstant(new Expr.List(rootList.Items.Skip(1).ToArray()), out Term rootTail)
                || options.Items.Count < 3)
            {
                return false;
            }

            var staticOptions = new List<Term>();
            foreach (Expr item in options.Items.SkipLast(3))
            {
                if (!AttributeLiteral.TryNormalizeConstant(item, out Term literal))
                {
                    return false;
                }
                staticOptions.Add(literal);
            }

            var paths = new List<(Atom Key, Term Suffix, Expr.Call Call)>();
            foreach (Expr item in options.Items.TakeLast(3))
            {
                if (item is not Expr.Tuple { Items: [Expr.Literal { Value: Atom key },
                    Expr.Call { DynamicRemote: false, Arguments: [Expr.List { Items: [
                        Expr.Variable dir, Expr.Variable name, var suffix
                    ], Tail: null }] } path] }
                    || path.Module != join.Module || path.Function != join.Function
                    || dir.Name != directory.Name || name.Name != role.Name
                    || !AttributeLiteral.TryNormalizeConstant(suffix, out Term basename))
                {
                    return false;
                }
                paths.Add((key, Cons.From([basename]), path));
            }

            int firstLabel = Label();
            int secondLabel = Label();
            int fallback = Label();
            owner.instructions.WriteByte(SelectValue);
            if (OnlyAtomOrConsCalls(function))
            {
                owner.EmitTypedRegister(0, AtomTypeBits | ConsTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }
            owner.Operand(LabelTag, fallback);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            foreach (var (key, label) in new[] { (a.Key, firstLabel), (b.Key, secondLabel) }
                .OrderBy(item => item.Key, Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))))
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, label);
            }
            EmitRedirect(firstLabel, a.Value);
            EmitRedirect(secondLabel, b.Value);
            Mark(fallback);

            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 2);
            owner.EmitConstant(library, 0);
            owner.EmitCallLine(root, location);
            Call(root.Module, root.Function, 1);
            EmitHeap(2);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(rootTail);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(join, CallLocation(root, location));
            Call(join.Module, join.Function, 1);

            var previous = CallLocation(join, location);
            for (int index = 0; index < paths.Count; index++)
            {
                var (_, suffix, path) = paths[index];
                EmitHeap(4);
                owner.Op(PutList, YRegisterTag, 2);
                owner.LiteralOperand(suffix);
                owner.Operand(XRegisterTag, 1);
                owner.Op(PutList, index == 0 ? XRegisterTag : YRegisterTag, index == 0 ? 0 : 1);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(XRegisterTag, 1);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, index switch { 0 => 1, 1 => 0, _ => 2 });
                if (index == 2)
                {
                    owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 1);
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                }

                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                owner.EmitCallLine(path, previous);
                Call(path.Module, path.Function, 1);
                previous = CallLocation(path, previous);
            }

            EmitHeap(paths.Count * 3 + options.Items.Count * ConsHeapWords);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(paths[2].Key.Name)), (XRegisterTag, 0));
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 0);
            for (int index = 1; index >= 0; index--)
            {
                owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(paths[index].Key.Name)), (YRegisterTag, index));
                owner.Op(PutList, XRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
            foreach (Term item in staticOptions.AsEnumerable().Reverse())
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(item);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
            owner.EmitFrameReturn(2);

            return true;

            void EmitRedirect(int label, Term value)
            {
                Mark(label);
                owner.EmitConstant(value, 0);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, entry);
            }

            void EmitHeap(int words)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }
        }

        private static (Atom Key, Term Value)? ReadOptionRedirect(Clause clause, string function) =>
            clause is { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }],
                Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [var argument] } call }
            && call.Function == function && AttributeLiteral.TryNormalizeConstant(argument, out Term value)
                ? (key, value)
                : null;

        private bool OnlyAtomOrConsCalls(FunctionDefinition function)
        {
            if (owner.exportedFunctions.Contains((function.Name, 1)))
            {
                return false;
            }

            bool called = false;
            foreach (Expr expression in localFunctions.Values.SelectMany(definition =>
                LocalFunctionEntries.ClauseExpressions(definition.Clauses)).SelectMany(LocalFunctionEntries.Walk))
            {
                if (expression is Expr.FunctionReference reference && reference.Name == function.Name && reference.Arity == 1)
                {
                    return false;
                }

                if (expression is Expr.Call { Module: null, DynamicRemote: false, Arguments: [var argument] } call
                    && call.Function == function.Name)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(argument, out Term value) || value is not (Atom or Cons))
                    {
                        return false;
                    }

                    called = true;
                }
            }

            return called;
        }
    }
}
