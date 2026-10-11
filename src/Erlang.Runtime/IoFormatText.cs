namespace Erlang;

internal static class IoFormatText
{
    public static string Render(Term formatValue, Term argumentList)
    {
        string format = Term.Text(formatValue);
        var args = Cons.Items(argumentList).ToArray();
        int n = 0;
        var output = new System.Text.StringBuilder();
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != IoFormatControls.Prefix)
            {
                output.Append(format[i]);
                continue;
            }
            if (++i == format.Length)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            switch (format[i])
            {
                case IoFormatControls.Newline:
                    output.Append(IoFormatControls.LineFeed);
                    break;
                case IoFormatControls.LiteralPrefixDirective:
                    output.Append(IoFormatControls.Prefix);
                    break;
                case IoFormatControls.Text:
                    if (n >= args.Length)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    output.Append(args[n] is Atom atom ? atom.Name : Term.Text(args[n]));
                    n++;
                    break;
                case IoFormatControls.Pretty:
                case IoFormatControls.Write:
                    if (n >= args.Length)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    output.Append(args[n++]);
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        if (n != args.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return output.ToString();
    }
}
