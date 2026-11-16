using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ElementRead(
            Expr.Call Call,
            int Index,
            int Register,
            int MinimumArity
        );

        private abstract record ElementBranch;

        private sealed record ElementTest(
            ElementRead? Read,
            ConstructionOperand Value,
            ConstructionOperand Compared,
            ElementBranch Match,
            ElementBranch Fallback
        ) : ElementBranch
        {
            internal int FallbackLabel { get; set; }
        }

        private sealed record ElementTuple(ElementRead[] Reads, ConstructionOperand[] Fields) : ElementBranch;

        private sealed record ElementValue(ConstructionOperand Value) : ElementBranch;

        private sealed record ElementTail(EntryCall Call) : ElementBranch;

        private (int Source, ElementTest Branch)? ReadListElementCase(Expr body, string[] parameters)
        {
            if (body is not Expr.Case { Value: Expr.Call call } || ReadElement(call) is not { } input
                || ReadBranch(body, new Dictionary<int, ConstructionOperand>(), 0) is not ElementTest plan)
            {
                return null;
            }

            return (input.Source, plan);

            ElementBranch? ReadBranch(Expr expression, Dictionary<int, ConstructionOperand> known, int minimumArity)
            {
                if (expression is Expr.Case decision)
                {
                    if (decision is not
                        {
                            Value: Expr.Call selector,
                            Clauses: [
                            { Guard: null, Patterns: [var pattern], Body: var matched },
                            { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: var fallback }
                            ]
                        }
                        || ReadElement(selector) is not { } member || member.Source != input.Source
                        || ReadCompared(pattern) is not { } compared)
                    {
                        return null;
                    }

                    ElementRead? read = null;
                    if (!known.TryGetValue(member.Index, out var value))
                    {
                        int destination = NextRegister(Occupied(decision, known, member.Index));
                        if (destination >= FirstReservedXRegister)
                        {
                            return null;
                        }

                        read = new ElementRead(
                            selector,
                            member.Index,
                            destination,
                            minimumArity
                        );
                        value = new ConstructionOperand(XRegisterTag, destination);
                    }

                    int checkedArity = Math.Max(minimumArity, member.Index);
                    var failed = new Dictionary<int, ConstructionOperand>(known) { [member.Index] = value };
                    var passed = new Dictionary<int, ConstructionOperand>(known)
                    {
                        [member.Index] = compared.Literal is null ? value : compared
                    };

                    return ReadBranch(matched, passed, checkedArity) is { } match
                        && ReadBranch(fallback, failed, checkedArity) is { } otherwise
                        ? new ElementTest(
                            read,
                            value,
                            compared,
                            match,
                            otherwise
                        ) : null;
                }

                if (ReadOperand(expression, known) is { } returned)
                {
                    return new ElementValue(returned);
                }

                if (expression is Expr.Tuple tuple)
                {
                    var fields = new ConstructionOperand[tuple.Items.Count];
                    var reads = new List<ElementRead>();
                    var available = new Dictionary<int, ConstructionOperand>(known);
                    var occupied = Occupied(tuple, known);
                    occupied.Add(input.Source);
                    for (int index = 0; index < fields.Length; index++)
                    {
                        Expr item = tuple.Items[index];
                        if (ReadOperand(item, available) is { } operand)
                        {
                            fields[index] = operand;
                            continue;
                        }

                        if (item is not Expr.Call field || ReadElement(field) is not { } member
                            || member.Source != input.Source)
                        {
                            return null;
                        }

                        int destination = NextRegister(occupied);
                        if (destination >= FirstReservedXRegister)
                        {
                            return null;
                        }

                        occupied.Add(destination);
                        reads.Add(new ElementRead(
                            field,
                            member.Index,
                            destination,
                            minimumArity
                        ));
                        minimumArity = Math.Max(minimumArity, member.Index);
                        fields[index] = new ConstructionOperand(XRegisterTag, destination);
                        available.Add(member.Index, fields[index]);
                    }

                    return new ElementTuple(reads.ToArray(), fields);
                }

                return expression is Expr.Call tail
                    && ReadEntryCall(owner.ResolveImportedCall(tail, entries), parameters, allowControlTransfer: true)
                        is { } target ? new ElementTail(target) : null;
            }

            HashSet<int> Occupied(
                Expr expression,
                IReadOnlyDictionary<int, ConstructionOperand> known,
                int? currentField = null)
            {
                var occupied = new HashSet<int>();
                Visit(expression);

                return occupied;

                void Visit(Expr item)
                {
                    if (item is Expr.Variable variable && Array.IndexOf(parameters, variable.Name) is >= 0 and var register)
                    {
                        occupied.Add(register);
                    }
                    else if (item is Expr.Call access && ReadElement(access) is { } field && field.Source == input.Source)
                    {
                        if (known.TryGetValue(field.Index, out var value))
                        {
                            if (value.Tag == XRegisterTag)
                            {
                                occupied.Add(value.Register);
                            }
                        }
                        else if (field.Index != currentField)
                        {
                            occupied.Add(input.Source);
                        }

                        return;
                    }
                    else if (item is Expr.Case decision)
                    {
                        foreach (Pattern pattern in decision.Clauses.SelectMany(branch => branch.Patterns))
                        {
                            if (ReadCompared(pattern) is { Tag: XRegisterTag } compared)
                            {
                                occupied.Add(compared.Register);
                            }
                        }
                    }

                    foreach (Expr child in LocalFunctionEntries.Children(item))
                    {
                        Visit(child);
                    }
                }
            }

            ConstructionOperand? ReadOperand(Expr expression, IReadOnlyDictionary<int, ConstructionOperand> known)
            {
                if (expression is Expr.Variable variable && Array.IndexOf(parameters, variable.Name) is >= 0 and var register
                    && variable.Name != VariableScopeNames.Wildcard)
                {
                    return new ConstructionOperand(XRegisterTag, register);
                }

                if (expression is Expr.Call projection && ReadElement(projection) is { } field
                    && field.Source == input.Source && known.TryGetValue(field.Index, out var value))
                {
                    return value;
                }

                return AttributeLiteral.TryNormalizeConstant(expression, out Term constant)
                    ? new ConstructionOperand(0, 0, constant) : null;
            }

            ConstructionOperand? ReadCompared(Pattern pattern) => pattern switch
            {
                Pattern.Literal literal => new ConstructionOperand(0, 0, literal.Value),
                Pattern.Variable { Name: not VariableScopeNames.Wildcard } name
                    when Array.IndexOf(parameters, name.Name) is >= 0 and var register =>
                    new ConstructionOperand(XRegisterTag, register),
                _ => null
            };

            (int Source, int Index)? ReadElement(Expr.Call access) =>
                access is { Function: AutoImportNames.Element, DynamicRemote: false, Arguments: [var position, Expr.Variable tuple] }
                && tuple.Name != VariableScopeNames.Wildcard && Array.IndexOf(parameters, tuple.Name) is >= 0 and var source
                && AttributeLiteral.TryNormalizeConstant(position, out Term value)
                && value is Integer integer && integer.Value > 0 && integer.Value <= int.MaxValue
                && owner.ResolvesErlangBif(access)
                    ? (source, (int)integer.Value) : null;

        }

        private void LabelListElementCase(ElementBranch branch)
        {
            if (branch is ElementTest test)
            {
                LabelListElementCase(test.Match);
                test.FallbackLabel = Label();
                LabelListElementCase(test.Fallback);
            }
        }

        private void EmitListElementCase(int source, ElementTest plan)
        {
            EmitBranch(plan, location);

            void EmitBranch(ElementBranch branch, BeamSourceLocation previous)
            {
                switch (branch)
                {
                    case ElementTest test:
                        if (test.Read is { } read)
                        {
                            previous = EmitRead(read, previous);
                        }

                        owner.Op(ExactEqualTest, LabelTag, test.FallbackLabel);
                        EmitOperand(test.Value);
                        EmitOperand(test.Compared);
                        EmitBranch(test.Match, previous);
                        Mark(test.FallbackLabel);
                        EmitBranch(test.Fallback, previous);
                        break;
                    case ElementTuple tuple:
                        foreach (ElementRead field in tuple.Reads)
                        {
                            previous = EmitRead(field, previous);
                        }

                        owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Fields.Length + 1);
                        owner.Operand(
                            BeamWire.UnsignedTag,
                            tuple.Fields.Where(field => field.Tag == XRegisterTag)
                            .Select(field => field.Register + 1).DefaultIfEmpty(0).Max()
                        );
                        owner.EmitTupleHeader(XRegisterTag, tuple.Fields.Length, 0);
                        foreach (var field in tuple.Fields)
                        {
                            EmitOperand(field);
                        }

                        owner.instructions.WriteByte(Return);
                        break;
                    case ElementValue value:
                        if (value.Value.Literal is { } literal)
                        {
                            owner.EmitConstant(literal, 0);
                        }
                        else
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                value.Value.Register,
                                XRegisterTag,
                                0
                            );
                        }

                        owner.instructions.WriteByte(Return);
                        break;
                    case ElementTail tail:
                        EmitRegisterTailCall(tail.Call, previous);
                        break;
                }
            }

            BeamSourceLocation EmitRead(ElementRead read, BeamSourceLocation previous)
            {
                if (read.Index <= read.MinimumArity)
                {
                    owner.EmitTuplePart(source, read.Index - 1, read.Register);

                    return previous;
                }

                owner.EmitCallLine(read.Call, previous);
                owner.Op(BifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
                owner.LiteralOperand(Term.I(read.Index));
                if (read.MinimumArity == 0)
                {
                    owner.Operand(XRegisterTag, source);
                }
                else
                {
                    owner.EmitTypedRegister(source, TupleTypeBits);
                }

                owner.Operand(XRegisterTag, read.Register);

                return CallLocation(read.Call, previous);
            }

            void EmitOperand(ConstructionOperand operand)
            {
                if (operand.Literal is { } literal)
                {
                    owner.LiteralOperand(literal);
                }
                else
                {
                    owner.Operand(operand.Tag, operand.Register);
                }
            }
        }

        private static int NextRegister(HashSet<int> occupied)
        {
            int register = 0;
            while (occupied.Contains(register))
            {
                register++;
            }

            return register;
        }
    }
}
