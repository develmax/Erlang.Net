using System.Reflection;
namespace Erlang.Tests;
internal static class HashPrefixDiagnosticMetadata
{
    public static readonly List<object> Rows = new();
    static HashPrefixDiagnosticMetadata()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => File.WriteAllText(Environment.GetEnvironmentVariable("ERLANG_HASH_DIAGNOSTIC_REPORT") ?? throw new InvalidOperationException("Missing diagnostic report path"), System.Text.Json.JsonSerializer.Serialize(new { Measurements = Rows, FullValidateExecuted = false, NativeExecuted = false }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
    public static object Capture()
    {
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        object? context = typeof(Thread).GetField("_executionContext", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Thread.CurrentThread);
        FieldInfo? field = context?.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault(item => item.FieldType.Name.Contains("AsyncLocalValueMap", StringComparison.Ordinal));
        if (context is not null && field is null) throw new InvalidOperationException("ExecutionContext local-values map field unresolved");
        object? map = field?.GetValue(context);
        var types = new List<object>();
        void Pair(object? pair)
        {
            if (pair is null) return;
            object? key = pair.GetType().GetProperty("Key")?.GetValue(pair);
            object? value = pair.GetType().GetProperty("Value")?.GetValue(pair);
            if (key is not null) types.Add(new { KeyType = key.GetType().FullName, ValueType = value?.GetType().FullName });
        }
        if (map is System.Collections.IDictionary dictionary)
            foreach (System.Collections.DictionaryEntry item in dictionary) types.Add(new { KeyType = item.Key.GetType().FullName, ValueType = item.Value?.GetType().FullName });
        else if (map is not null)
            foreach (var item in map.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object? value = item.GetValue(map);
                if (value is Array array) foreach (object? pair in array) Pair(pair); else Pair(value);
            }
        return new { Gen0 = gen0, Gen1 = gen1, Gen2 = gen2, Thread = Environment.CurrentManagedThreadId, MapType = map?.GetType().FullName, LocalValuesField = field?.Name, Count = types.Count, Entries = types, AmbientValuesPrinted = false };
    }
}
