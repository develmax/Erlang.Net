namespace Erlang.Differential;

public static class ProcLibReportCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = ProcLibReportCaseData.Reports.Select(row => new CompiledModuleCase(
        ProcLibReportCaseNames.Prefix + row.Name,
        row.Source,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Decode(row.ExpectedEtf))
    )).ToArray();

    public static Term Decode(string expectedEtf)
        => ExternalTermFormat.Decode(Convert.FromBase64String(expectedEtf));
}
