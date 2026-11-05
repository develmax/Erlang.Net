using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class StringSeparatorAllocationChecks
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
        byte[] bytes = new byte[StringSeparatorAllocationNames.InputBytes];
        for (int index = 0; index < bytes.Length; index++)
            bytes[index] = (index + 1) % StringSeparatorAllocationNames.SeparatorPeriod == 0
                ? StringSeparatorAllocationNames.Separator : StringSeparatorAllocationNames.Content;
        Term input = new BitString(bytes);
        Term separators = Term.List(Term.I(StringSeparatorAllocationNames.Separator));
        var reports = new List<object>();
        long allocated = 0;
        bool allocationsPass = true;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                foreach (string function in StringSeparatorAllocationNames.Functions)
                {
                    Term[] Arguments(Term value) => function switch
                    {
                        StringSeparatorAllocationNames.Trim => [value, Term.A(StringSeparatorAllocationNames.Trailing), separators],
                        StringSeparatorAllocationNames.Take => [value, separators, Term.A(StringSeparatorAllocationNames.False), Term.A(StringSeparatorAllocationNames.Trailing)],
                        _ => [value, separators]
                    };
                    await runtime.Modules.Call(
                        context,
                        StringSeparatorAllocationNames.Module,
                        function,
                        Arguments(new BitString(bytes.AsSpan(0, StringSeparatorAllocationNames.SeparatorPeriod)))
                    );
                    Term expected = function switch
                    {
                        StringSeparatorAllocationNames.Trim => new BitString(bytes.AsSpan(0, bytes.Length - 1)),
                        StringSeparatorAllocationNames.Take => Term.Tuple(new BitString(bytes.AsSpan(0, bytes.Length - 1)), new BitString([StringSeparatorAllocationNames.Separator])),
                        _ => Cons.From(Enumerable.Repeat<Term>(
                            new BitString(bytes.AsSpan(0, StringSeparatorAllocationNames.SeparatorPeriod - 1)),
                            bytes.Length / StringSeparatorAllocationNames.SeparatorPeriod
                        ))
                    };
                    long before = GC.GetTotalAllocatedBytes(true);
                    Term actual = await runtime.Modules.Call(
                        context,
                        StringSeparatorAllocationNames.Module,
                        function,
                        Arguments(input)
                    );
                    long current = GC.GetTotalAllocatedBytes(true) - before;
                    allocated += current;
                    bool equal = actual.Equals(expected);
                    long budget = function switch
                    {
                        StringSeparatorAllocationNames.Trim => StringSeparatorAllocationNames.TrimAllocationBudget,
                        StringSeparatorAllocationNames.Take => StringSeparatorAllocationNames.TakeAllocationBudget,
                        _ => StringSeparatorAllocationNames.LexemesAllocationBudget
                    };
                    bool withinBudget = current < budget;
                    allocationsPass &= withinBudget;
                    reports.Add(new
                    {
                        Function = function,
                        Allocated = current,
                        Budget = budget,
                        WithinBudget = withinBudget,
                        Equal = equal,
                        ResultSha256 = Convert.ToHexString(SHA256.HashData(ExternalTermFormat.Encode(actual)))
                    });
                    if (!equal)
                        throw new InvalidDataException(StringSeparatorAllocationNames.Failure);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(StringSeparatorAllocationNames.Output)!);
                File.WriteAllText(
                    StringSeparatorAllocationNames.Output,
                    JsonSerializer.Serialize(new
                    {
                        InputBytes = bytes.Length,
                        Allocated = allocated,
                        Reports = reports
                    }, new JsonSerializerOptions { WriteIndented = true })
                );
                if (!allocationsPass)
                    throw new InvalidDataException(StringSeparatorAllocationNames.Failure);
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
}
