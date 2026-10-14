using Erlang.Tests;
using System.Text.Json;
await DistributionPacketChecks.Run();
Console.WriteLine(JsonSerializer.Serialize(new { Status = "PASS", Test = DistributionPacketTestNames.Test, IndependentGolden = true, AllPrefixTruncations = true, ProductionFull = false }));
