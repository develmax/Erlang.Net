using System.Text;

namespace Erlang;

internal static class HaltRequestParser
{
    public static RuntimeHaltRequest Read(Term status, Term optionList)
    {
        bool flush = true;
        uint? timeout = null;
        Term tail = optionList;
        while (tail is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: 2 } option || option.Items[0] is not Atom name)
                throw new HaltParseException(true);
            if (name.Name == HaltNames.Flush && option.Items[1] is Atom flag && flag.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False)
                flush = flag.Name == ErlangBooleanAtoms.True;
            else if (name.Name == HaltNames.FlushTimeout && option.Items[1] is Integer milliseconds && milliseconds.Value >= 0 && milliseconds.Value <= HaltNativeSmallParameters.MaximumPositive)
                timeout = (uint)(milliseconds.Value & HaltNativeSmallParameters.Unsigned32Mask);
            else
                throw new HaltParseException(true);
            tail = cell.Tail;
        }
        if (tail is not Nil)
            throw new HaltParseException(true);
        var options = new RuntimeHaltOptions(flush, timeout);
        if (status is Integer number && number.Value >= 0)
            return new(
                RuntimeHaltKind.Numeric,
                (int)(number.Value & HaltStatusParameters.StatusMask),
                null,
                options
            );
        if (status is Atom { Name: HaltNames.Abort })
            return new(
                RuntimeHaltKind.Abort,
                0,
                null,
                options
            );
        if (status is Cons or Nil)
            return new(
                RuntimeHaltKind.CrashDump,
                HaltStatusParameters.DumpExitStatus,
                Slogan(status),
                options
            );

        throw new HaltParseException(false);
    }

    private static string Slogan(Term list)
    {
        var text = new StringBuilder();
        int characters = 0;
        while (true)
        {
            if (list is Nil)
                return text.ToString();
            if (list is not Cons cell)
                throw new HaltParseException(false);
            if (characters == HaltStatusParameters.SloganCharacters)
                return text.ToString();
            if (cell.Head is not Integer code || code.Value < 0 || code.Value > HaltStatusParameters.MaximumUnicodeScalar || !Rune.IsValid((int)code.Value))
                throw new HaltParseException(false);
            text.Append(char.ConvertFromUtf32((int)code.Value));
            characters++;
            list = cell.Tail;
        }
    }
}
