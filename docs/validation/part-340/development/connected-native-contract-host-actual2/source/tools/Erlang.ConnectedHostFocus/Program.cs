using Erlang.Tests;
using System.Text.Json;

var outcomes = new List<object>();
bool failed = false;
string output = args.Single();
try
{
    await ConnectedSpawnHostChecks.CodecFrames();
    outcomes.Add(new { Name = "codec-frames", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS codec-frames");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "codec-frames", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnHostChecks.RealRequestReplyAndDeath();
    outcomes.Add(new { Name = "real-request-reply-death", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS real-request-reply-death");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "real-request-reply-death", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnRetirementHostChecks.StaleReplyCannotRebind();
    outcomes.Add(new { Name = "stale-reply-generation", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS stale-reply-generation");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "stale-reply-generation", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnCleanupHostChecks.ParentDeathBeforeReply();
    outcomes.Add(new { Name = "owner-death-before-reply", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS owner-death-before-reply");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "owner-death-before-reply", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnCleanupHostChecks.AbandonBeforeReply();
    outcomes.Add(new { Name = "abandon-before-reply", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS abandon-before-reply");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "abandon-before-reply", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnCleanupHostChecks.ParentDeathAfterReplyBeforeSelection();
    outcomes.Add(new { Name = "owner-death-after-reply-before-selection", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS owner-death-after-reply-before-selection");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "owner-death-after-reply-before-selection", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnCapacityHostChecks.ProducerDeathDuringCapacityWait();
    outcomes.Add(new { Name = "producer-death-during-capacity-wait", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS producer-death-during-capacity-wait");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "producer-death-during-capacity-wait", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await NodeSpawnHostChecks.Run();
    outcomes.Add(new { Name = "unchanged-node-spawn-regressions", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS unchanged-node-spawn-regressions");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "unchanged-node-spawn-regressions", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    var regressionType = typeof(ConnectedSpawnHostChecks).Assembly.GetType("Erlang.Tests.SpawnRequestHostChecks", throwOnError: true)!;
    var regression = regressionType.GetMethod("Run", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
    await (Task)regression.Invoke(null, null)!;
    outcomes.Add(new { Name = "unchanged-local-spawn-request-regressions", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS unchanged-local-spawn-request-regressions");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "unchanged-local-spawn-request-regressions", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnTraceHostChecks.TracedSuccess();
    outcomes.Add(new { Name = "traced-success", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS traced-success");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "traced-success", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnTraceHostChecks.TracedCreatorError();
    outcomes.Add(new { Name = "traced-creator-error", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS traced-creator-error");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "traced-creator-error", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnMonitorOptionHostChecks.ExplicitAlias();
    outcomes.Add(new { Name = "priority-customtag-explicit-alias", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS priority-customtag-explicit-alias");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "priority-customtag-explicit-alias", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnMonitorOptionHostChecks.DemonitorAlias();
    outcomes.Add(new { Name = "priority-customtag-demonitor-alias", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS priority-customtag-demonitor-alias");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "priority-customtag-demonitor-alias", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnMonitorOptionHostChecks.ReplyDemonitorAlias();
    outcomes.Add(new { Name = "priority-customtag-reply-demonitor-alias", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS priority-customtag-reply-demonitor-alias");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "priority-customtag-reply-demonitor-alias", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnMonitorOptionHostChecks.DemonitorAfterSuccess();
    outcomes.Add(new { Name = "real-demonitor-after-connected-success", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS real-demonitor-after-connected-success");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "real-demonitor-after-connected-success", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnPriorityLinkHostChecks.PriorityLinkedExit();
    outcomes.Add(new { Name = "priority-linked-exit", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS priority-linked-exit");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "priority-linked-exit", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnWireOptionHostChecks.WireOrderAndLastTuplePrecedence();
    outcomes.Add(new { Name = "wire-order-last-tuple-precedence", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS wire-order-last-tuple-precedence");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "wire-order-last-tuple-precedence", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnAliasWireHostChecks.PlainSend2();
    outcomes.Add(new { Name = "remote-alias-send2", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS remote-alias-send2");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "remote-alias-send2", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnAliasWireHostChecks.TracedSend2();
    outcomes.Add(new { Name = "remote-alias-trace-send2", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS remote-alias-trace-send2");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "remote-alias-trace-send2", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnAliasWireHostChecks.PlainSend3();
    outcomes.Add(new { Name = "remote-alias-send3-noconnect", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS remote-alias-send3-noconnect");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "remote-alias-send3-noconnect", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnAliasWireHostChecks.AlternativeTracedSend3();
    outcomes.Add(new { Name = "remote-alias-alternative-trace-send3", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS remote-alias-alternative-trace-send3");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "remote-alias-alternative-trace-send3", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnNativeAliasHostChecks.ThreeMonitorAliasModes();
    outcomes.Add(new { Name = "native-three-monitor-alias-sequences", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS native-three-monitor-alias-sequences");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "native-three-monitor-alias-sequences", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedSpawnAliasWireHostChecks.LegacyTracedSend3();
    outcomes.Add(new { Name = "native-legacy-traced-send3-noconnect", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS native-legacy-traced-send3-noconnect");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "native-legacy-traced-send3-noconnect", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
Environment.ExitCode = failed ? 1 : 0;
