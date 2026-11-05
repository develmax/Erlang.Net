using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class DetsPlatformNativeExpectation
{
    private const string FileError = "file_error";
    private const string FilePath = "file_path";
    private const string Error = "error";
    private const string AccessDenied = "eacces";
    private const string Modes = "modes";
    private const string InvalidEvidence = "DETS native readonly truncate evidence differs";

    internal static Term ForHost(Term expected, string repository, string output)
    {
        if (OperatingSystem.IsWindows()) return expected;
        string packet = Path.Combine(output, Erlang.Tests.PrimFileDeleteNativeOracleNames.Receipt);
        using JsonDocument evidence = JsonDocument.Parse(File.ReadAllBytes(packet));
        string source = Path.Combine(repository, Erlang.Tests.PrimFileDeleteContractNames.Capture, Erlang.Tests.PrimFileDeleteNativeOracleNames.Source);
        if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))) != evidence.RootElement.GetProperty("SourceSha256").GetString())
            throw new InvalidDataException(InvalidEvidence);
        int error = evidence.RootElement.GetProperty("Observed").GetProperty("ReadonlyTruncateErrno").GetInt32();
        string name = error switch
        {
            9 => "ebadf",
            13 => AccessDenied,
            22 => "einval",
            _ => throw new InvalidDataException(InvalidEvidence)
        };
        int replacements = 0;
        Term windows = Term.Tuple(Term.A(FileError), Term.A(FilePath), Term.Tuple(Term.A(Error), Term.A(AccessDenied)));

        Term Rewrite(Term value)
        {
            if (value.Equals(windows))
            {
                replacements++;

                return Term.Tuple(Term.A(FileError), Term.A(FilePath), Term.Tuple(Term.A(Error), Term.A(name)));
            }

            return value switch
            {
                TupleTerm tuple => Term.Tuple(tuple.Items.Select(Rewrite).ToArray()),
                Cons cell => new Cons(Rewrite(cell.Head), Rewrite(cell.Tail)),
                _ => value
            };
        }
        var rows = Cons.Items(expected).ToArray();
        int index = Array.FindIndex(rows, row => row is TupleTerm pair && pair.Items[0].Equals(Term.A(Modes)));
        if (index < 0) throw new InvalidDataException(InvalidEvidence);
        var modes = (TupleTerm)rows[index];
        var ramVariants = Cons.Items(modes.Items[1]).ToArray();
        ramVariants[0] = Rewrite(ramVariants[0]);
        if (replacements != 3) throw new InvalidDataException(InvalidEvidence);
        rows[index] = Term.Tuple(modes.Items[0], Cons.From(ramVariants));
        File.WriteAllText(
            Path.Combine(output, "native-readonly-expectation.json"),
            JsonSerializer.Serialize(
                new
                {
                    ExpectedErrno = name,
                    ExactFields = replacements,
                    Scope = "Only the three persistent disk-readonly delete_all_objects failure fields in modes; RAM and all other fields/stacks unchanged",
                    IndependentNativeReceiptSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(packet)))
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

        return Cons.From(rows);
    }
}
