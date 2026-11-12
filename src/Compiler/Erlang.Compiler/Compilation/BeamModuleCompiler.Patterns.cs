using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly record struct Deconstruction(byte Extract, Atom? Tag = null, int Arity = 0, int Element = 0);

    private static bool TryDeconstruction(Clause clause, out Deconstruction result)
    {
        result = default;
        if (clause is not { Guard: null, Patterns: [var pattern], Body: Expr.Variable returned })
        {
            return false;
        }
        if (pattern is Pattern.Tuple tuple && tuple.Items.Count >= 2
            && tuple.Items[0] is Pattern.Literal { Value: Atom tag })
        {
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            int selected = -1;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is not Pattern.Variable variable ||
                    (variable.Name != VariableScopeNames.Wildcard && !bindings.Add(variable.Name)))
                {
                    return false;
                }
                if (variable.Name == returned.Name)
                {
                    selected = index;
                }
            }
            if (selected >= 0)
            {
                result = new(GetTupleElement, tag, tuple.Items.Count, selected);

                return true;
            }
        }
        if (pattern is Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail } && head.Name != tail.Name)
        {
            if (head.Name == returned.Name || tail.Name == returned.Name)
            {
                result = new(head.Name == returned.Name ? GetHead : GetTail);

                return true;
            }
        }

        return false;
    }

    private void EmitDeconstruction(Deconstruction shape, Clause fallback)
    {
        int failure = nextLabel++;
        Op(shape.Tag is null ? IsNonemptyList : IsTaggedTuple, LabelTag, failure);
        Operand(XRegisterTag, 0);
        if (shape.Tag is not null)
        {
            Operand(BeamWire.UnsignedTag, shape.Arity);
            Operand(AtomTag, Intern(shape.Tag.Name));
        }

        Op(shape.Extract, XRegisterTag, 0);
        if (shape.Tag is not null)
        {
            Operand(BeamWire.UnsignedTag, shape.Element);
        }
        Operand(XRegisterTag, 0);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, failure);
        EmitBody(fallback);
        instructions.WriteByte(Return);
    }
}
