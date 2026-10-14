using System.Security.Cryptography;
using System.Text.Json;

namespace LoaderContrasts;

internal static class LoaderExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != LoaderContrastNames.ArgumentCount)
            throw new ArgumentException("Usage: Erlang.LoaderContrasts installedOtpRoot repositoryRoot newOutput buildEvidence");
        string root = Path.GetFullPath(arguments[0]);
        string repository = Path.GetFullPath(arguments[1]);
        string output = Path.GetFullPath(arguments[2]);
        string evidencePath = Path.GetFullPath(arguments[3]);
        using var evidence = JsonDocument.Parse(await File.ReadAllBytesAsync(evidencePath));
        JsonElement proof = evidence.RootElement;
        if (proof.GetProperty("SourceCommit").GetString() != LoaderContrastNames.Commit ||
            proof.GetProperty("SourceTag").GetString() != LoaderContrastNames.Tag ||
            !proof.GetProperty("SourceBuildCompleted").GetBoolean() ||
            Path.GetFullPath(proof.GetProperty("InstalledOtpRoot").GetString()!) != root)
            throw new InvalidDataException("Verified pinned source-build evidence is required");
        string buildLog = proof.GetProperty("SourceBuildLog").GetString()!;
        if (Hash(await File.ReadAllBytesAsync(buildLog)) != proof.GetProperty("SourceBuildLogSha256").GetString())
            throw new InvalidDataException("Pinned source build log changed");
        if ((await File.ReadAllTextAsync(Path.Combine(
            root,
            "releases",
            LoaderContrastNames.Release,
            "OTP_VERSION"
        ))).Trim() != LoaderContrastNames.Version)
            throw new InvalidDataException("Installed OTP version differs");
        string suffix = OperatingSystem.IsWindows() ? ".exe" : "";
        string erl = Path.Combine(root, "bin", "erl" + suffix);
        string erlc = Path.Combine(root, "bin", "erlc" + suffix);
        if (!File.Exists(erl) || !File.Exists(erlc))
            throw new FileNotFoundException("Installed erl and erlc are required");
        if (Directory.Exists(output))
            throw new IOException("Output directory must be new");
        Directory.CreateDirectory(output);
        byte[] declarations = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, LoaderContrastNames.Declarations));
        await File.WriteAllBytesAsync(Path.Combine(output, LoaderContrastNames.Declarations), declarations);
        await File.WriteAllBytesAsync(Path.Combine(output, "source-build-evidence.json"), await File.ReadAllBytesAsync(evidencePath));
        await File.WriteAllBytesAsync(Path.Combine(output, "source-build.log"), await File.ReadAllBytesAsync(buildLog));
        await File.WriteAllBytesAsync(
            Path.Combine(output, "pinned-source-provenance.json"),
            await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "pinned-source-provenance.json"))
        );
        using var declared = JsonDocument.Parse(declarations);
        JsonElement[] scenarios = declared.RootElement.GetProperty("Scenarios").EnumerateArray().ToArray();
        if (scenarios.Length != LoaderContrastNames.CaseCount)
            throw new InvalidDataException("Ten independent scenarios required");
        foreach (JsonElement input in declared.RootElement.GetProperty("InputBytes").EnumerateArray())
        {
            string path = input.GetProperty("Path").GetString()!;
            byte[] bytes = await File.ReadAllBytesAsync(Path.Combine(repository, path));
            if (Hash(bytes) != input.GetProperty("Sha256").GetString())
                throw new InvalidDataException("Immutable native input changed: " + path);
            string captured = Path.Combine(output, "inputs", path);
            Directory.CreateDirectory(Path.GetDirectoryName(captured)!);
            await File.WriteAllBytesAsync(captured, bytes);
        }
        string versionText = Path.Combine(output, "invoked-runtime.txt");
        string versionEtf = Path.Combine(output, "invoked-runtime.etf");
        string versionExpression = "V={erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(system_architecture),erlang:system_info(build_type),erlang:system_info(emu_flavor),code:root_dir()}," +
            "ok=file:write_file(" + Quote(versionEtf) + ",term_to_binary(V)),ok=file:write_file(" + Quote(versionText) + ",iolist_to_binary(io_lib:format(\"~tp~n\",[V])))," +
            "{" + Quote(LoaderContrastNames.Release) + "," + Quote(LoaderContrastNames.ErtsVersion) + ",_,_,jit," + Quote(root) + "}=V,halt(0).";
        await File.WriteAllTextAsync(Path.Combine(output, "invoked-runtime-evaluation.erl"), versionExpression + "\n");
        LoaderProcessResult version = await LoaderProcess.Run(erl, ["-noshell", "-eval", versionExpression], Path.Combine(output, "runtime-version"));
        if (version.ExitCode != 0 || version.TimedOut || !version.RawComplete || !File.Exists(versionEtf) || !File.Exists(versionText))
            throw new InvalidDataException("Actual invoked runtime differs from the pinned JIT build; raw version evidence retained");
        string sourcePath = Path.Combine(output, LoaderContrastNames.Source);
        await File.WriteAllBytesAsync(sourcePath, await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, LoaderContrastNames.Source)));
        LoaderProcessResult compile = await LoaderProcess.Run(erlc, ["-Werror", "+debug_info", "-o", output, sourcePath], Path.Combine(output, "compile"));
        if (compile.ExitCode != 0 || compile.TimedOut || !compile.RawComplete)
            throw new InvalidDataException("Compiled direct wrapper is unavailable; raw compile evidence retained");
        string wrapperBeam = Path.Combine(output, LoaderContrastNames.Wrapper + ".beam");
        string disassembly = Path.Combine(output, "wrapper-disassembly.txt");
        string disassemble = "D=beam_disasm:file(" + Quote(wrapperBeam) + "),ok=file:write_file(" + Quote(disassembly) + ",iolist_to_binary(io_lib:format(\"~tp~n\",[D])))," +
            "{beam_file,loader_contrast_wrapper,_,_,_,Fs}=D,{function,direct,1,_,Is}=lists:keyfind(direct,2,Fs)," +
            "true=lists:any(fun({call_ext,1,{extfunc,erlang,call_on_load_function,1}})->true;" +
            "({call_ext_only,1,{extfunc,erlang,call_on_load_function,1}})->true;" +
            "({call_ext_last,1,{extfunc,erlang,call_on_load_function,1},_})->true;(_)->false end,Is),halt(0).";
        await File.WriteAllTextAsync(Path.Combine(output, "disassembly-evaluation.erl"), disassemble + "\n");
        LoaderProcessResult disasm = await LoaderProcess.Run(erl, ["-noshell", "-eval", disassemble], Path.Combine(output, "disassemble"));
        if (disasm.ExitCode != 0 || disasm.TimedOut || !disasm.RawComplete || !File.Exists(disassembly))
            throw new InvalidDataException("Actual compiled direct call_on_load instruction evidence missing");
        var results = new List<LoaderCaseResult>();
        foreach (JsonElement scenario in scenarios)
        {
            string name = scenario.GetProperty("Name").GetString()!;
            string[] variants = name == "wrapper-gzip-and-real-load" ? [name + "-raw", name + "-gzip"] : [name];
            foreach (string variant in variants)
            {
                string directory = Path.Combine(output, variant);
                Directory.CreateDirectory(directory);
                string evaluation = LoaderContrastNames.Wrapper + ":run(" + Quote(variant) + "," + Quote(Path.Combine(output, "inputs")) + "," + Quote(directory) + ").";
                await File.WriteAllTextAsync(Path.Combine(directory, "evaluation.erl"), evaluation + "\n");
                await File.WriteAllTextAsync(Path.Combine(directory, "stage.txt"), "created");
                LoaderProcessResult observed = await LoaderProcess.Run(erl, ["-noshell", "-pa", output, "-eval", evaluation], Path.Combine(directory, "process"));
                string? stage = File.Exists(Path.Combine(directory, "stage.txt")) ? await File.ReadAllTextAsync(Path.Combine(directory, "stage.txt")) : null;
                byte[] actual = File.Exists(Path.Combine(directory, "actual.etf")) ? await File.ReadAllBytesAsync(Path.Combine(directory, "actual.etf")) : [];
                byte[] expected = File.Exists(Path.Combine(directory, "expected.etf")) ? await File.ReadAllBytesAsync(Path.Combine(directory, "expected.etf")) : [];
                bool passed = observed.ExitCode == 0 && !observed.TimedOut && observed.RawComplete && stage == "completed" && File.Exists(Path.Combine(directory, "actual.etf")) && File.Exists(Path.Combine(directory, "expected.etf")) &&
                    actual.SequenceEqual(expected);
                results.Add(new(
                    variant,
                    name,
                    observed.ExitCode,
                    observed.TimedOut,
                    observed.RawComplete,
                    stage,
                    passed,
                    await Files(directory)
                ));
                await Report(
                    output,
                    proof,
                    declarations,
                    sourcePath,
                    wrapperBeam,
                    results,
                    false
                );
            }
        }
        await Report(
            output,
            proof,
            declarations,
            sourcePath,
            wrapperBeam,
            results,
            true
        );
        if (results.Any(result => !result.Passed))
            throw new InvalidDataException("Native loader contrasts failed; every raw VM outcome retained");
    }

    private static async Task Report(
        string output,
        JsonElement proof,
        byte[] declarations,
        string source,
        string beam,
        List<LoaderCaseResult> rows,
        bool complete
    )
    {
        string target = Path.Combine(output, LoaderContrastNames.Report);
        string temporary = target + ".tmp";
        var report = new
        {
            SourceCommit = LoaderContrastNames.Commit,
            SourceTag = LoaderContrastNames.Tag,
            BuildEvidence = proof,
            PredeclaredSha256 = Hash(declarations),
            WrapperSourceSha256 = Hash(await File.ReadAllBytesAsync(source)),
            WrapperBeamSha256 = Hash(await File.ReadAllBytesAsync(beam)),
            CaseCount = LoaderContrastNames.CaseCount,
            FreshVmExecutions = rows.Count,
            ExpectedFreshVmExecutions = LoaderContrastNames.CaseCount + 1,
            Complete = complete,
            Passed = rows.Count(row => row.Passed),
            Results = rows,
            MfaRegistrations = 0,
            SdkExecuted = false,
            NativePhysicalCompatibilityClaim = false
        };
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        File.Move(temporary, target, overwrite: true);
    }

    private static async Task<IReadOnlyList<object>> Files(string directory)
    {
        var files = new List<object>();
        foreach (string path in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.Ordinal))
        {
            byte[] bytes = await File.ReadAllBytesAsync(path);
            files.Add(new { Name = Path.GetFileName(path), Length = bytes.Length, Sha256 = Hash(bytes) });
        }

        return files;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
