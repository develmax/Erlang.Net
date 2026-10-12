using System.Security.Cryptography;
using System.Text;

namespace Erlang.Differential;

public static class NativeCompilationInvestigation
{
    private static readonly string[] fixtureNames =
    [
        "otp-supervisor-bad-init-return",
        "otp-server-init-stop",
        "otp-supervisor-start-failure",
        "otp-supervisor-invalid-intensity",
        "erlang-spawn-group-callback-tuple",
        "erlang-spawn-group-callback-wrong-arity-child-error"
    ];

    public static string Command(string source, bool disableOptimizations, bool? trapExits)
        => Command(source, disableOptimizations ? OracleInvestigationModes.DisabledOptimizations : string.Empty, trapExits);

    public static string Command(string source, string disabledPasses, bool? trapExits)
    {
        string compileOptions = disabledPasses.Length == 0 ? string.Empty : "," + disabledPasses;
        string load = CompiledModuleProtocol.Forms(source) +
            "{ok,M,Bin,_}=compile:forms(Forms,[binary,return_errors,return_warnings" + compileOptions + "])," +
            "{module,M}=code:load_binary(M,\"differential.erl\",Bin)," +
            "Disassembled=beam_disasm:file(Bin)," +
            "RunCode=[F||F={function,run,0,_,_}<-element(6,Disassembled)]," +
            "Disassembly=unicode:characters_to_binary(io_lib:format(\"~tp\",[RunCode])),";
        const string invoke = "try apply(M,run,[]) of V->{ok,V} catch C:R->{error,C,R} end";
        string execute = trapExits is null ? "Outcome=" + invoke + "," :
            "OracleOwner=self(),{OracleRunner,OracleMonitor}=spawn_monitor(fun()->" +
            "process_flag(trap_exit," + (trapExits.Value ? "true" : "false") + ")," +
            "OracleOutcome=" + invoke + "," +
            "OracleOwner!{" + NativeModuleResultNames.Tag + ",self(),OracleOutcome} end)," +
            "Outcome=receive {" + NativeModuleResultNames.Tag + ",OracleRunner,OracleValue}->" +
            "demonitor(OracleMonitor,[flush]),OracleValue;" +
            "{'DOWN',OracleMonitor,process,OracleRunner,OracleReason}->{error,exit,OracleReason} end,";

        return load + execute + "Result={Outcome,Disassembly}," + OracleResultProtocol.Command("Result");
    }

    public static async Task<IReadOnlyList<object>> Run(Func<string, Task<OracleProcessOutput>> execute, Func<IReadOnlyList<object>, string, Task> checkpoint)
    {
        var rows = new List<object>();
        foreach (string name in fixtureNames)
        {
            CompiledModuleCase fixture = CompiledModuleCases.All.Single(fixture => fixture.Name == name);
            foreach (string disabledPasses in new[]
            {
                string.Empty,
                OracleInvestigationModes.DisabledOptimizations,
                OracleInvestigationModes.DisabledCoreOptimization,
                OracleInvestigationModes.DisabledSsaOptimization,
                OracleInvestigationModes.DisabledPostOptimization
            })
            {
                foreach (bool? trapExits in new bool?[] { null, false, true })
                {
                    await checkpoint(rows, fixture.Source);
                    string command = Command(fixture.Source, disabledPasses, trapExits);
                    OracleProcessOutput output = await execute(command);
                    if (OracleResultProtocol.Decode(output.StandardOutput) is not TupleTerm { Items.Count: 2 } result || result.Items[1] is not BitString disassembly)
                        throw new InvalidOperationException(OracleInvestigationDiagnostics.InvalidNativeResult);
                    rows.Add(new
                    {
                        fixture.Name,
                        fixture.Source,
                        SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source))),
                        FixtureExpected = fixture.Expected.ToString(),
                        CompilerMode = disabledPasses.Length == 0 ? OracleInvestigationModes.DefaultCompiler : disabledPasses,
                        ProcessMode = trapExits is null ? OracleInvestigationModes.InitialProcess : trapExits.Value ? OracleInvestigationModes.TrappedFreshProcess : OracleInvestigationModes.FreshProcess,
                        NativeOutcome = result.Items[0].ToString(),
                        MatchesIndependentFixture = result.Items[0].Equals(fixture.Expected),
                        RunZeroDisassembly = Encoding.UTF8.GetString(disassembly.ToArray()),
                        Command = command,
                        RawStandardOutput = output.StandardOutput,
                        RawStandardError = output.StandardError,
                        output.ExitCode,
                        Scope = OracleInvestigationDiagnostics.Scope
                    });
                    await checkpoint(rows, fixture.Source);
                }
            }
        }

        return rows;
    }
}
