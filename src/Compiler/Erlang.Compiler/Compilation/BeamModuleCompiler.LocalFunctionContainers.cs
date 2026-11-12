using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitLocalFunctionContainer(Clause clause, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (clause.Guard is not null || !HasUnconditionalParameters(clause))
        {
            return false;
        }
        if (clause.Body is Expr.FunctionReference direct && entries.TryGetValue((direct.Name, direct.Arity), out int directEntry))
        {
            EmitClosureAllocation(0, 0, 1);
            EmitDeclaredFunctionReference(direct, directEntry, 0);
            instructions.WriteByte(Return);
            return true;
        }
        if (clause.Patterns.Count != 0)
        {
            return TryEmitLocalFunctionTuple(clause, entries);
        }
        if (clause.Body is not (Expr.Tuple or Expr.List))
        {
            return false;
        }
        if (clause.Body is Expr.List { Tail: null } list && TryEmitLocalFunctionPairs(list, entries))
        {
            return true;
        }
        if (ReadFunctionContainer(clause.Body) is not { } container)
        {
            return false;
        }

        var references = LocalFunctionEntries.Walk(clause.Body).OfType<Expr.FunctionReference>().ToArray();
        if (references is not [var reference] || !entries.TryGetValue((reference.Name, reference.Arity), out int entry))
        {
            return false;
        }

        EmitClosureAllocation(container.Words, 0, 1);
        EmitDeclaredFunctionReference(reference, entry, 0);
        foreach (var (tuple, fields) in container.Steps)
        {
            if (tuple)
            {
                EmitTupleHeader(XRegisterTag, fields.Length);
            }
            else
            {
                instructions.WriteByte(PutList);
            }
            foreach (Term? field in fields)
            {
                if (field is null)
                {
                    Operand(XRegisterTag, 0);
                }
                else
                {
                    LiteralOperand(field);
                }
            }
            if (!tuple)
            {
                Operand(XRegisterTag, 0);
            }
        }
        instructions.WriteByte(Return);

        return true;
    }

    private bool TryEmitLocalFunctionTuple(Clause clause, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (clause.Body is not Expr.Tuple tuple
            || tuple.Items.OfType<Expr.FunctionReference>().ToArray() is not [var reference]
            || !entries.TryGetValue((reference.Name, reference.Arity), out int entry)
            || tuple.Items.Any(item => item is not Expr.FunctionReference && !IsCallOperand(item)))
        {
            return false;
        }

        var retained = tuple.Items.OfType<Expr.Variable>()
            .Select(variable => ArgumentRegister(clause, variable.Name)).ToHashSet();
        int destination = 0;
        while (retained.Contains(destination))
        {
            destination++;
        }
        if (destination >= FirstReservedXRegister)
        {
            return false;
        }

        EmitClosureAllocation(tuple.Items.Count + 1, retained.DefaultIfEmpty(-1).Max() + 1, 1);
        EmitDeclaredFunctionReference(reference, entry, destination);
        EmitTupleHeader(XRegisterTag, tuple.Items.Count);
        foreach (Expr item in tuple.Items)
        {
            if (item is Expr.FunctionReference)
            {
                Operand(XRegisterTag, destination);
            }
            else if (item is Expr.Variable variable)
            {
                Operand(XRegisterTag, ArgumentRegister(clause, variable.Name));
            }
            else
            {
                AttributeLiteral.TryNormalizeConstant(item, out Term constant);
                LiteralOperand(constant);
            }
        }
        instructions.WriteByte(Return);
        return true;
    }

    private bool TryEmitLocalFunctionPairs(Expr.List list, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (list.Items.Count < 2 || list.Items.Count > FirstReservedXRegister)
        {
            return false;
        }

        var pairs = new List<(Term Key, Expr.FunctionReference Reference, int Entry)>();
        foreach (Expr item in list.Items)
        {
            if (item is not Expr.Tuple { Items: [var key, Expr.FunctionReference reference] }
                || !AttributeLiteral.TryNormalizeConstant(key, out Term literal)
                || !entries.TryGetValue((reference.Name, reference.Arity), out int entry))
            {
                return false;
            }
            pairs.Add((literal, reference, entry));
        }

        EmitClosureAllocation(pairs.Count * (3 + ConsHeapWords), 0, pairs.Count);
        for (int register = 0; register < pairs.Count; register++)
        {
            EmitDeclaredFunctionReference(pairs[register].Reference, pairs[register].Entry, register);
        }
        for (int register = pairs.Count - 1; register >= 0; register--)
        {
            EmitTupleHeader(XRegisterTag, 2, register);
            LiteralOperand(pairs[register].Key);
            Operand(XRegisterTag, register);
            Op(PutList, XRegisterTag, register);
            if (register == pairs.Count - 1)
            {
                LiteralOperand(Nil.Value);
            }
            else
            {
                Operand(XRegisterTag, register + 1);
            }
            Operand(XRegisterTag, register);
        }
        instructions.WriteByte(Return);

        return true;
    }

    private void EmitDeclaredFunctionReference(Expr.FunctionReference reference, int entry, int register)
    {
        int index = closures.FindIndex(closure => closure is { Body: null, General: null } && closure.Entry == entry);
        if (index < 0)
        {
            index = closures.Count;
            closures.Add(new(
                reference.Name,
                null,
                default,
                0,
                null,
                reference.Arity
            )
            { Entry = entry });
        }
        Op(MakeFun, BeamWire.UnsignedTag, index);
        Operand(XRegisterTag, register);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 0);
    }

    private static (int Words, List<(bool Tuple, Term?[] Fields)> Steps)? ReadFunctionContainer(Expr expression)
    {
        var steps = new List<(bool Tuple, Term?[] Fields)>();
        int words = 0;

        bool Read(Expr node)
        {
            if (node is Expr.FunctionReference)
            {
                return true;
            }

            IReadOnlyList<Expr>? items = node switch
            {
                Expr.Tuple tuple => tuple.Items,
                Expr.List { Tail: null } list => list.Items,
                _ => null
            };
            if (items is null)
            {
                return false;
            }

            var fields = new Term?[items.Count];
            int dynamicIndex = -1;
            for (int index = 0; index < items.Count; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(items[index], out fields[index]))
                {
                    if (dynamicIndex >= 0 || !Read(items[index]))
                    {
                        return false;
                    }
                    dynamicIndex = index;
                    fields[index] = null;
                }
            }
            if (dynamicIndex < 0)
            {
                return false;
            }

            if (node is Expr.Tuple)
            {
                steps.Add((true, fields));
                words += fields.Length + 1;
            }
            else
            {
                Term tail = Cons.From(fields.Skip(dynamicIndex + 1).Select(field => field!));
                steps.Add((false, [null, tail]));
                for (int index = dynamicIndex - 1; index >= 0; index--)
                {
                    steps.Add((false, [fields[index], null]));
                }
                words += (dynamicIndex + 1) * ConsHeapWords;
            }

            return true;
        }

        return Read(expression) ? (words, steps) : null;
    }
}
