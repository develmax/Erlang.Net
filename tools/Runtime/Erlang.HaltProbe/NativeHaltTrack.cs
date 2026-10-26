using System.Text.Json;

namespace Erlang.HaltProbe;

internal static class NativeHaltTrack
{
    public static async Task<int> Run(string[] args)
    {
        if (args.Length is < 3 or > 4 || args[0] != NativeHaltTrackNames.Command)
        {
            Console.Error.WriteLine(NativeHaltTrackNames.Usage);

            return NativeHaltTrackNames.InfrastructureExit;
        }
        string executable = args[1];
        string reportPath = Path.GetFullPath(args[2]);
        string caseRoot = args.Length == 4 ? Path.GetFullPath(args[3]) : Path.Combine(Path.GetDirectoryName(reportPath)!, NativeHaltTrackNames.CasesDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        Directory.CreateDirectory(caseRoot);
        var options = new JsonSerializerOptions { WriteIndented = true };
        var rows = new List<NativeHaltTrackRow>();
        NativeHaltVersionResult? version = null;
        bool versionVerified = false;
        HaltSubprocessContract? active = null;
        HaltSubprocessContract[] contracts = HaltSubprocessCases.NumericAndErrors.Concat(HaltSubprocessCases.Adapters).Append(HaltSubprocessCases.Abort).Append(HaltSubprocessCases.CrashDump).ToArray();

        async Task Save(bool complete, string? infrastructure = null)
        {
            var comparable = rows.Where(row => row.Contract.ComparableToOriginal).ToArray();
            var physical = rows.Where(row => !row.Contract.ComparableToOriginal).ToArray();
            var report = new NativeHaltTrackReport(
                NativeHaltTrackNames.Baseline,
                NativeHaltTrackNames.DeclaredSourceCommit,
                versionVerified,
                version,
                complete,
                NativeHaltTrackNames.Planned,
                rows.Count,
                NativeHaltTrackNames.ComparablePlanned,
                comparable.Count(row => row.Errors is { Count: 0 }),
                comparable.Count(row => row.Errors is { Count: > 0 }),
                NativeHaltTrackNames.PhysicalPlanned,
                physical.Count(row => row.Errors is { Count: 0 }),
                physical.Count(row => row.Errors is { Count: > 0 }),
                false,
                infrastructure,
                infrastructure is null ? null : active,
                rows.AsReadOnly()
            );
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, options));
        }

        try
        {
            if (contracts.Length != NativeHaltTrackNames.Planned || contracts.Count(contract => contract.ComparableToOriginal) != NativeHaltTrackNames.ComparablePlanned || contracts.Select(contract => contract.Name).Distinct().Count() != contracts.Length)
                throw new InvalidDataException("Declared halt contract count or identities changed");
            await File.WriteAllTextAsync(reportPath + NativeHaltTrackNames.LedgerSuffix, JsonSerializer.Serialize(contracts, options));
            await Save(false);
            version = await NativeHaltVersionProbe.Run(executable);
            versionVerified = version.RawExitCode == 0 && !version.TimedOut && version.InfrastructureError is null && version.Version == NativeHaltTrackNames.Version;
            await Save(false);
            if (!versionVerified)
            {
                await Save(false, version.InfrastructureError ?? "Pinned OTP version mismatch: " + version.Version);

                return NativeHaltTrackNames.InfrastructureExit;
            }
            foreach (HaltSubprocessContract contract in contracts)
            {
                active = contract;
                HaltSubprocessResult result = await HaltSubprocessRunner.Run(
                    contract,
                    HaltProcessBackend.Original,
                    executable,
                    null,
                    caseRoot
                );
                rows.Add(new(contract, result, null));
                await Save(false);
                NativeHaltDumpEvidence? dump = contract.Name == HaltSubprocessCases.CrashDump.Name ? await NativeHaltDumpCapture.Read(result.WorkingDirectory) : null;
                rows[^1] = new(
                    contract,
                    result,
                    null,
                    dump
                );
                await Save(false);
                var errors = HaltSubprocessVerifier.Check(contract, result).ToList();
                if (dump is not null && (!dump.Exists || dump.Length is not > 0 || dump.Sha256 is null || !dump.SignatureMatches || dump.Error is not null))
                    errors.Add("Native dump missing, unreadable or lacking pinned =erl_crash_dump:0.5 header: " + dump.Error);
                rows[^1] = new(
                    contract,
                    result,
                    errors.AsReadOnly(),
                    dump
                );
                await Save(false);
                Console.WriteLine((errors.Count == 0 ? "PASS " : "FAIL ") + contract.Name + (contract.ComparableToOriginal ? " comparable" : " physical-format-gap"));
            }
            active = null;
            await Save(true);

            return rows.Any(row => row.Errors is null || row.Errors.Count != 0) ? NativeHaltTrackNames.ComparisonFailureExit : 0;
        }
        catch (Exception exception)
        {
            await Save(false, exception.ToString());
            Console.Error.WriteLine(exception);

            return NativeHaltTrackNames.InfrastructureExit;
        }
    }
}
