using System.Text;

namespace Erlang;

internal static class ErlangDataRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, Func<IReadOnlyList<Term>, Term> body) => registry.Register(
            ErlangModuleNames.Module,
            name,
            arity,
            (_, arguments) => ValueTask.FromResult(body(arguments))
        );
        foreach (int arity in new[] { 1, 2 })
        {
            Add(
                ErlangDataBifNames.IntegerToList,
                arity,
                arguments => Term.String(ErlangIntegerText.Format(arguments[0], ErlangIntegerText.Base(arguments)))
            );
            Add(
                ErlangDataBifNames.IntegerToBinary,
                arity,
                arguments => new BitString(Encoding.ASCII.GetBytes(ErlangIntegerText.Format(arguments[0], ErlangIntegerText.Base(arguments))))
            );
            Add(
                ErlangDataBifNames.ListToInteger,
                arity,
                arguments => ErlangIntegerText.Parse(ErlangIntegerText.ListCharacters(arguments[0]), ErlangIntegerText.Base(arguments))
            );
            Add(
                ErlangDataBifNames.BinaryToInteger,
                arity,
                arguments => ErlangIntegerText.Parse(ErlangIoData.Binary(arguments[0]).Select(number => (int)number), ErlangIntegerText.Base(arguments))
            );
        }
        Add(ErlangDataBifNames.ListToBinary, 1, arguments => ErlangIoData.Flatten(arguments[0], false, true));
        Add(ErlangDataBifNames.IolistToBinary, 1, arguments => ErlangIoData.Flatten(arguments[0], false, false));
        Add(ErlangDataBifNames.ListToBitstring, 1, arguments => ErlangIoData.Flatten(arguments[0], true, true));
        Add(ErlangDataBifNames.IolistToIovec, 1, arguments => ErlangIoData.Iovec(arguments[0]));
        Add(ErlangDataBifNames.IolistSize, 1, arguments => ErlangIoData.Size(arguments[0]));
        Add(ErlangDataBifNames.BinaryToList, 1, arguments => ErlangIoData.ToList(arguments[0], false));
        Add(ErlangDataBifNames.BinaryToList, 3, arguments => ErlangIoData.SliceList(arguments));
        Add(ErlangDataBifNames.BitstringToList, 1, arguments => ErlangIoData.ToList(arguments[0], true));
        Add(ErlangDataBifNames.BinaryPart, 3, arguments => ErlangIoData.Part(arguments[0], arguments[1], arguments[2]));
        Add(
            ErlangDataBifNames.BinaryPart,
            2,
            arguments => arguments[1] is TupleTerm { Items.Count: 2 } range ? ErlangIoData.Part(arguments[0], range.Items[0], range.Items[1]) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(ErlangDataBifNames.SplitBinary, 2, arguments => ErlangIoData.Split(arguments[0], arguments[1]));
        Add(ErlangDataBifNames.ListToFloat, 1, arguments => ErlangFloatText.Parse(ErlangIntegerText.ListCharacters(arguments[0])));
        Add(
            ErlangDataBifNames.BinaryToFloat,
            1,
            arguments => ErlangFloatText.Parse(ErlangIoData.Binary(arguments[0]).Select(number => (int)number))
        );
        foreach (string operation in new[] { ErlangChecksumNames.Adler, ErlangChecksumNames.Crc })
        {
            Add(operation, 1, arguments => ErlangChecksums.Compute(operation, operation == ErlangChecksumNames.Adler ? 1u : 0u, arguments[0]));
            Add(operation, 2, arguments => ErlangChecksums.Compute(operation, ErlangChecksums.Word(arguments[0]), arguments[1]));
        }
        foreach (string operation in new[] { ErlangChecksumNames.AdlerCombine, ErlangChecksumNames.CrcCombine })
            Add(operation, 3, arguments => ErlangChecksums.Combine(operation, arguments));
        Add(ErlangChecksumNames.Md5, 1, arguments => ErlangChecksums.Md5(arguments[0]));
    }
}
