using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Otp;

var tests = new List<(string Name, Func<Task> Body)>();
Test(Erlang.Tests.HaltLifecycleHostNames.Test, Erlang.Tests.HaltLifecycleHostChecks.Run);
Test(Erlang.Tests.HaltPlatformHostNames.Test, Erlang.Tests.HaltPlatformHostChecks.Run);
Test(Erlang.Tests.HaltPortHostNames.Test, Erlang.Tests.HaltPortHostChecks.Run);
Test(Erlang.Tests.HaltSubprocessHostNames.Test, Erlang.Tests.HaltSubprocessHostChecks.Run);
Test(Erlang.Tests.CpuTopologyStateHostNames.Test, Erlang.Tests.CpuTopologyStateHostChecks.Run);
Test(Erlang.Tests.RuntimeStatisticsHostParameters.Test, Erlang.Tests.RuntimeStatisticsHostChecks.Run);
Test(Erlang.Tests.MatchSpecHostNames.Test, Erlang.Tests.MatchSpecHostChecks.Run);
Test(Erlang.Tests.ProcessTraceHostParameters.Test, Erlang.Tests.ProcessTraceHostChecks.Run);
Test(Erlang.Tests.SequenceTraceHostParameters.Test, Erlang.Tests.SequenceTraceHostChecks.Run);
Test(Erlang.Tests.SystemMonitorHostNames.Test, Erlang.Tests.SystemMonitorHostChecks.Run);
Test(Erlang.Tests.SystemProfileHostParameters.Test, Erlang.Tests.SystemProfileHostChecks.Run);
Test(Erlang.Tests.ProcessDisplayHostNames.Test, Erlang.Tests.ProcessDisplayHostChecks.Run);
Test(Erlang.Tests.SequenceExitHostParameters.Test, Erlang.Tests.SequenceExitHostChecks.Run);
Test(Erlang.Tests.JointPlatformHostNames.Test, Erlang.Tests.JointPlatformHostChecks.Run);
Test(Erlang.Tests.SpawnRequestHostParameters.Test, Erlang.Tests.SpawnRequestHostChecks.Run);
Test(Erlang.Tests.NativeRecordHostNames.Test, Erlang.Tests.NativeRecordHostChecks.Run);
Test(Erlang.Tests.NativeRecordDefinitionHostNames.Test, Erlang.Tests.NativeRecordDefinitionHostChecks.Run);
Test(Erlang.Tests.NativeRecordCompletionHostNames.Test, Erlang.Tests.NativeRecordCompletionHostChecks.Run);
Test(Erlang.Tests.ModuleLoadingHostNames.TestName, Erlang.Tests.ModuleLoadingHostChecks.Run);
Test(Erlang.Tests.ProcessTableHostNames.TestName, Erlang.Tests.ProcessTableHostChecks.Run);
Test(Erlang.Tests.ReferenceTextHostNames.TestName, Erlang.Tests.ReferenceTextHostChecks.Run);
Test(Erlang.Tests.FunctionResolutionHostNames.TestName, Erlang.Tests.FunctionResolutionHostChecks.Run);
Test(Erlang.Tests.PreloadedModuleHostNames.TestName, Erlang.Tests.PreloadedModuleHostChecks.Run);
Test(Erlang.Tests.ModuleInformationHostNames.Test, Erlang.Tests.ModuleInformationHostChecks.Run);
Test(Erlang.Tests.ModuleAttributeHostNames.Test, Erlang.Tests.ModuleAttributeHostChecks.Run);
Test(Erlang.Tests.ModuleDeletionHostNames.Test, Erlang.Tests.ModuleDeletionHostChecks.Run);
Test(Erlang.Tests.SourceTailHostNames.Test, Erlang.Tests.SourceTailHostChecks.Run);
Test(Erlang.Tests.RuntimeTailHostNames.Test, Erlang.Tests.RuntimeTailHostChecks.Run);
Test(Erlang.Tests.CodeImageHostNames.Test, Erlang.Tests.CodeImageHostChecks.Run);
Test(Erlang.Tests.ProcessCodeHostNames.Test, Erlang.Tests.ProcessCodeHostChecks.Run);
Test(Erlang.Tests.ProcessGarbageCollectionHostNames.Test, Erlang.Tests.ProcessGarbageCollectionHostChecks.Run);
Test(Erlang.Tests.ProcessInfoObservableHostNames.Test, Erlang.Tests.ProcessInfoObservableHostChecks.Run);
Test(Erlang.Tests.FunctionReloadHostNames.Test, Erlang.Tests.FunctionReloadHostChecks.Run);
Test(Erlang.Tests.CodePurgeHostNames.Test, Erlang.Tests.CodePurgeHostChecks.Run);
Test(Erlang.Tests.StandardStreamHostNames.Test, Erlang.Tests.StandardStreamHostChecks.Run);
Test(Erlang.Tests.ProcessSuspensionHostNames.Test, Erlang.Tests.ProcessSuspensionHostChecks.Run);
Test(Erlang.Tests.TermDisplayHostNames.Test, Erlang.Tests.TermDisplayHostChecks.Run);
Test(Erlang.Tests.RuntimeNodeQueryHostNames.Test, Erlang.Tests.RuntimeNodeQueryHostChecks.Run);
Test(Erlang.Tests.NodeSpawnHostNames.Test, Erlang.Tests.NodeSpawnHostChecks.Run);
Test(Erlang.Tests.RuntimeNodeControlHostNames.Test, Erlang.Tests.RuntimeNodeControlHostChecks.Run);
Test(Erlang.Tests.ProcessSavedCallHostNames.Test, Erlang.Tests.ProcessSavedCallHostChecks.Run);
Test(Erlang.Tests.PortHostParameters.Test, Erlang.Tests.PortHostChecks.Run);
Test(Erlang.Tests.PortDrainHostParameters.Test, Erlang.Tests.PortDrainHostChecks.Run);
Test(Erlang.Differential.DescriptorIoNames.CancellationTest, Erlang.Differential.DescriptorIoContracts.RunCancellation);
foreach (var fixture in Erlang.Differential.DescriptorIoCases.All)
    Test("runtime/port-fd-io/" + fixture.Name, async () => await Erlang.Differential.DescriptorIoContracts.RunSdk(fixture));
Test(Erlang.Tests.StandardStreamHostNames.ProtocolTest, Erlang.Tests.StandardStreamProtocolChecks.Run);
Test("runtime/exit-pending-kill-cleans-concurrent-alias-signals", Erlang.Tests.ProcessExitHostChecks.PendingKillCleanup);
Test("runtime/io-monitored-group-leader-concurrent-output", Erlang.Tests.IoProtocolHostChecks.ConcurrentOutput);
Test("runtime/monitor-concurrent-termination-and-cancellation", Erlang.Tests.ProcessMonitorHostChecks.ConcurrentTermination);
Test(
    "runtime/alias-pending-signal-selective-wakeup-and-reply-consumption",
    Erlang.Tests.ProcessAliasHostChecks.PendingWakeups
);
Test("runtime/alias-priority-mailbox-removal-and-wakeup", Erlang.Tests.ProcessAliasHostChecks.Mailbox);
Test("runtime/alias-concurrent-reply-wire-identity-owner-cleanup", Erlang.Tests.ProcessAliasHostChecks.Lifecycle);
Test("erlang/hash-iterative-deep-terms-and-no-binary-payload-copy", Erlang.Tests.ErlangHashHostChecks.Resources);
var outcomes = new List<object>();
foreach (var test in tests)
{
    string? error = null;
    try { await test.Body(); } catch (Exception exception) { error = exception.ToString(); }
    outcomes.Add(new { test.Name, Error = error });
    Console.WriteLine(JsonSerializer.Serialize(new { test.Name, Error = error }));
}
File.WriteAllText(args[0], JsonSerializer.Serialize(new { PrefixOutcomes = outcomes, Measurements = Erlang.Tests.HashPrefixDiagnosticMetadata.Rows, FullValidation = false, NativeExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
void Test(string name, Func<Task> body) => tests.Add((name, body));
