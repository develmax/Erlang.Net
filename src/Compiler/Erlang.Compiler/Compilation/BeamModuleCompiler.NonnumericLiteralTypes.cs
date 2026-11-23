namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool HasNonnumericLiteralType(Term literal)
    {
        // OTP beam_types only keeps the first twelve concrete tuple element types.
        const int maximumTrackedTupleElements = 12;
        if (literal is Atom or Nil or BitString)
        {
            return true;
        }

        if (literal is not (TupleTerm or Cons or MapTerm))
        {
            return false;
        }

        var pending = new Stack<Term>();
        pending.Push(literal);
        while (pending.TryPop(out Term? term))
        {
            switch (term)
            {
                case Atom or Nil or BitString or MapTerm { Entries.Count: 0 }:
                    break;
                case TupleTerm tuple when tuple.Items.Count <= maximumTrackedTupleElements:
                    foreach (Term element in tuple.Items)
                    {
                        pending.Push(element);
                    }
                    break;
                case Cons list:
                    // Mixed compound list heads may lose complete field facts when
                    // OTP joins their types. A single head keeps its literal type.
                    Term tail = list.Tail;
                    if (tail is not Cons)
                    {
                        pending.Push(list.Head);
                        pending.Push(tail);
                        break;
                    }

                    for (Term cursor = list; cursor is Cons cell; cursor = cell.Tail)
                    {
                        if (cell.Head is not (Atom or Nil or BitString))
                        {
                            return false;
                        }

                        if (cell.Tail is not Cons)
                        {
                            pending.Push(cell.Tail);
                        }
                    }
                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}
