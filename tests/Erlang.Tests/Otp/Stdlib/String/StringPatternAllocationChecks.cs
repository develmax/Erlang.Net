using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Erlang.Tests;

internal static class StringPatternAllocationChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeNativePrimitiveProviders.RegisterCatalog(runtime, registry);
            StringModule.Register(registry);
        });
        StringPatternAllocationImages.Publish(runtime);
        byte[] bytes = new byte[StringPatternAllocationNames.InputBytes];
        Array.Fill(bytes, StringPatternAllocationNames.Content);
        for (int index = StringPatternAllocationNames.SeparatorPeriod - 1; index < bytes.Length;
            index += StringPatternAllocationNames.SeparatorPeriod)
            bytes[index] = StringPatternAllocationNames.Separator;
        Term input = new BitString(bytes);
        Term pattern = new BitString([StringPatternAllocationNames.Separator]);
        var reports = new List<object>();
        long allocated = 0;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                foreach (string mode in StringPatternAllocationNames.Modes)
                {
                    string function = mode == StringPatternAllocationNames.Find
                        ? StringPatternAllocationNames.Find : StringPatternAllocationNames.Split;
                    Term where = Term.A(mode == StringPatternAllocationNames.Find
                        ? StringPatternAllocationNames.Trailing : mode);
                    await runtime.Modules.Call(
                        context,
                        StringPatternAllocationNames.Module,
                        function,
                        new BitString(Encoding.UTF8.GetBytes(StringPatternAllocationNames.Warmup)),
                        pattern,
                        where
                    );
                    Term expected = Expected(mode, bytes, pattern);
                    long before = GC.GetTotalAllocatedBytes(true);
                    Term actual = await runtime.Modules.Call(
                        context,
                        StringPatternAllocationNames.Module,
                        function,
                        input,
                        pattern,
                        where
                    );
                    long current = GC.GetTotalAllocatedBytes(true) - before;
                    allocated += current;
                    bool equal = actual.Equals(expected);
                    reports.Add(new
                    {
                        Mode = mode,
                        Allocated = current,
                        Equal = equal,
                        ResultSha256 = Convert.ToHexString(SHA256.HashData(ExternalTermFormat.Encode(actual)))
                    });
                    if (!equal)
                        throw new InvalidDataException(StringPatternAllocationNames.Failure);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(StringPatternAllocationNames.Output)!);
                File.WriteAllText(
                    StringPatternAllocationNames.Output,
                    JsonSerializer.Serialize(
                        new
                        {
                            InputBytes = bytes.Length,
                            Allocated = allocated,
                            Budget = StringPatternAllocationNames.AllocationBudget,
                            Reports = reports
                        },
                        new JsonSerializerOptions { WriteIndented = true }
                    )
                );
                if (allocated >= StringPatternAllocationNames.AllocationBudget)
                    throw new InvalidDataException(StringPatternAllocationNames.Failure);
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task;
        await actor.Completion;
    }

    private static Term Expected(string mode, byte[] bytes, Term pattern)
    {
        if (mode == StringPatternAllocationNames.Find)
            return pattern;
        if (mode == StringPatternAllocationNames.Trailing)
            return Term.List(new BitString(bytes.AsSpan(0, bytes.Length - 1)), new BitString([]));

        int parts = bytes.Length / StringPatternAllocationNames.SeparatorPeriod;
        byte[] segment = Enumerable.Repeat(StringPatternAllocationNames.Content,
            StringPatternAllocationNames.SeparatorPeriod - 1).ToArray();

        return Cons.From(Enumerable.Range(0, parts).Select(_ => (Term)new BitString(segment)).Append(new BitString([])));
    }
}
