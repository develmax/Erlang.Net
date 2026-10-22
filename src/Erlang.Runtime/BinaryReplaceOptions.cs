namespace Erlang;

internal sealed record BinaryReplaceOptions(Term Scope, bool Global, Term Insert)
{
    internal static async ValueTask<BinaryReplaceOptions> Read(ProcessContext context, Term value)
    {
        Term scope = Nil.Value, insert = Nil.Value;
        bool global = false;
        while (value is Cons cell)
        {
            await context.ReduceAsync();
            switch (cell.Head)
            {
                case Atom { Name: BinaryWrapperNames.Global }:
                    global = true;
                    break;
                case TupleTerm { Items.Count: 2 } option when option.Items[0] is Atom { Name: BinaryWrapperNames.Scope }
                    && option.Items[1] is TupleTerm { Items.Count: 2 }:
                    scope = Term.List(option);
                    break;
                case TupleTerm { Items.Count: 2 } option when option.Items[0] is Atom { Name: BinaryWrapperNames.InsertReplaced }:
                    insert = option.Items[1];
                    break;
                default:
                    throw new ErlangException(Term.A(BinaryWrapperNames.BadOption), ErlangExceptionClasses.Throw);
            }
            value = cell.Tail;
        }
        if (value is not Nil)
            throw new ErlangException(Term.A(BinaryWrapperNames.BadOption), ErlangExceptionClasses.Throw);

        return new(scope, global, insert);
    }
}
