using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitSiblingConstructors(Clause clause)
    {
        if (clause is not
            {
                Guard: null,
                Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } parameter],
                Body: Expr.Tuple tuple
            }
            || !tuple.Items.Any(ReadsInput))
        {
            return false;
        }

        var operands = new (int? Register, Term? Constant)[tuple.Items.Count];
        var constructors = new List<(bool Tuple, Term?[] Fields)>();
        int words = tuple.Items.Count + 1;
        for (int index = 0; index < tuple.Items.Count; index++)
        {
            Expr expression = tuple.Items[index];
            if (ReadsInput(expression))
            {
                operands[index] = (0, null);
                continue;
            }

            if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
            {
                operands[index] = (null, constant);
                continue;
            }

            bool isTuple = expression is Expr.Tuple;
            IReadOnlyList<Expr>? fields = expression switch
            {
                Expr.Tuple nested => nested.Items,
                Expr.List { Items.Count: > 0 } list => [.. list.Items, list.Tail ?? new Expr.Literal(Nil.Value)],
                _ => null
            };
            if (fields is null || !fields.Any(ReadsInput) || constructors.Count + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            var constructor = new Term?[fields.Count];
            for (int field = 0; field < fields.Count; field++)
            {
                if (!ReadsInput(fields[field]))
                {
                    if (!AttributeLiteral.TryNormalizeConstant(fields[field], out Term value))
                    {
                        return false;
                    }

                    constructor[field] = value;
                }
            }

            if (!isTuple)
            {
                // Fold the closed suffix before allocating the live part of the list.
                int last = constructor.Length - 1;
                Term? tail = constructor[last];
                while (last > 0 && tail is not null && constructor[last - 1] is { } head)
                {
                    tail = new Cons(head, tail);
                    last--;
                }

                constructor[last] = tail;
                Array.Resize(ref constructor, last + 1);
            }

            words = checked(words + (isTuple ? constructor.Length + 1 : (constructor.Length - 1) * ConsHeapWords));
            constructors.Add((isTuple, constructor));
            operands[index] = (constructors.Count, null);
        }

        if (constructors.Count < 2)
        {
            return false;
        }

        // The outer tuple keeps X0 and every sibling result live until its construction.
        Op(TestHeap, BeamWire.UnsignedTag, words);
        Operand(BeamWire.UnsignedTag, 1);
        for (int index = 0; index < constructors.Count; index++)
        {
            var (isTuple, fields) = constructors[index];
            int register = index + 1;
            if (isTuple)
            {
                EmitTupleHeader(XRegisterTag, fields.Length, register);
                foreach (Term? field in fields)
                {
                    WriteField(field);
                }

                continue;
            }

            for (int field = fields.Length - 2; field >= 0; field--)
            {
                instructions.WriteByte(PutList);
                WriteField(fields[field]);
                if (field == fields.Length - 2)
                {
                    WriteField(fields[^1]);
                }
                else
                {
                    Operand(XRegisterTag, register);
                }

                Operand(XRegisterTag, register);
            }
        }

        EmitTupleHeader(XRegisterTag, operands.Length);
        foreach (var (register, constant) in operands)
        {
            if (register is int value)
            {
                Operand(XRegisterTag, value);
            }
            else
            {
                LiteralOperand(constant!);
            }
        }

        instructions.WriteByte(Return);

        return true;

        bool ReadsInput(Expr expression) => expression is Expr.Variable variable && variable.Name == parameter.Name;

        void WriteField(Term? value)
        {
            if (value is null)
            {
                Operand(XRegisterTag, 0);
            }
            else
            {
                LiteralOperand(value);
            }
        }
    }
}
