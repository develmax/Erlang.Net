using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

var assembly = typeof(Erlang.ProcessRuntime).Assembly;
var pipe = assembly.GetType("Erlang.WindowsPortChildProcess", true)!.GetMethod("Pipe", BindingFlags.NonPublic | BindingFlags.Static)!;
var results = new List<object>();
foreach (bool overlapped in new[] { false, true })
{
    try
    {
        var handles = (ITuple)pipe.Invoke(null, new object[] { true, overlapped })!;
        ((IDisposable)handles[0]!).Dispose();
        ((IDisposable)handles[1]!).Dispose();
        results.Add(new { Overlapped = overlapped, Passed = true });
    }
    catch (TargetInvocationException failure) when (failure.InnerException is Win32Exception native)
    {
        results.Add(new { Overlapped = overlapped, Passed = false, native.NativeErrorCode, Exception = native.ToString() });
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { Runtime = assembly.Location, RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))), Results = results }, new JsonSerializerOptions { WriteIndented = true }));
