using Erlang.Tests;
using System.Text.Json;

var outcomes = new List<object>();
bool failed = false;
string output = args.Single();
try
{
    await ConnectedRpcLocalHostChecks.LocalCallerAndExceptions();
    outcomes.Add(new { Name = "rpc-local-caller-and-exceptions", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS rpc-local-caller-and-exceptions");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "rpc-local-caller-and-exceptions", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedRpcLabelHostChecks.VisibleLabelDeathAndNodeRetirement();
    outcomes.Add(new { Name = "rpc-connected-label-lifecycle", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS rpc-connected-label-lifecycle");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "rpc-connected-label-lifecycle", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedRpcTimeoutHostChecks.QueuedDownBeforeZeroWait();
    outcomes.Add(new { Name = "rpc-queued-down-before-zero-wait", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS rpc-queued-down-before-zero-wait");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "rpc-queued-down-before-zero-wait", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedRpcTimeoutHostChecks.TimeoutLosesToAdmittedDown();
    outcomes.Add(new { Name = "rpc-timeout-loses-to-admitted-down", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS rpc-timeout-loses-to-admitted-down");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "rpc-timeout-loses-to-admitted-down", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedProcLibNodeHostChecks.SpawnFun();
    outcomes.Add(new { Name = "proc-lib-node-spawnfun", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS proc-lib-node-spawnfun");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "proc-lib-node-spawnfun", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedProcLibNodeHostChecks.SpawnMfa();
    outcomes.Add(new { Name = "proc-lib-node-spawnmfa", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS proc-lib-node-spawnmfa");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "proc-lib-node-spawnmfa", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedProcLibNodeHostChecks.SpawnLinkFun();
    outcomes.Add(new { Name = "proc-lib-node-spawnlinkfun", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS proc-lib-node-spawnlinkfun");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "proc-lib-node-spawnlinkfun", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedProcLibNodeHostChecks.SpawnLinkMfa();
    outcomes.Add(new { Name = "proc-lib-node-spawnlinkmfa", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS proc-lib-node-spawnlinkmfa");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "proc-lib-node-spawnlinkmfa", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedProcLibNodeHostChecks.SpawnOptFun();
    outcomes.Add(new { Name = "proc-lib-node-spawnoptfun", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS proc-lib-node-spawnoptfun");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "proc-lib-node-spawnoptfun", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await ConnectedProcLibNodeHostChecks.SpawnOptMfa();
    outcomes.Add(new { Name = "proc-lib-node-spawnoptmfa", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS proc-lib-node-spawnoptmfa");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "proc-lib-node-spawnoptmfa", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
try
{
    await RuntimeConnectedNodeQueryHostChecks.Run();
    outcomes.Add(new { Name = "nodes-real-connected-query", Passed = true, Error = (string?)null });
    Console.WriteLine("PASS nodes-real-connected-query");
}
catch (Exception exception)
{
    outcomes.Add(new { Name = "nodes-real-connected-query", Passed = false, Error = exception.ToString() });
    failed = true;
    Console.Error.WriteLine(exception);
}
File.WriteAllText(output, JsonSerializer.Serialize(outcomes, new JsonSerializerOptions { WriteIndented = true }));
Environment.ExitCode = failed ? 1 : 0;
