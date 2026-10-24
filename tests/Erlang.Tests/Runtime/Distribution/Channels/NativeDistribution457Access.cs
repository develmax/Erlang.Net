using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal static class NativeDistribution457Access
{
    private static readonly Assembly Assembly = typeof(ProcessRuntime).Assembly;
    private static readonly Type WakeType = Assembly.GetType(NativeDistribution457Names.WakeType)!;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static object NewWake() => Activator.CreateInstance(WakeType, nonPublic: true)!;

    internal static Task Capture(object wake)
        => (Task)WakeType.GetMethod(NativeDistribution457Names.Capture, Instance)!.Invoke(wake, null)!;

    internal static void Wake(object wake)
        => WakeType.GetMethod(NativeDistribution457Names.Wake, Instance)!.Invoke(wake, null);

    internal static byte[] Copy(
        string kind,
        byte[] header,
        byte[] body,
        byte[][]? segments = null
    )
    {
        object value = Enum.Parse(Assembly.GetType(NativeDistribution457Names.KindType)!, kind);
        object output = Activator.CreateInstance(
            Assembly.GetType(NativeDistribution457Names.OutputType)!,
            Instance,
            null,
            [value, header, body, segments, true],
            null
        )!;
        try
        {
            return (byte[])Assembly.GetType(NativeDistribution457Names.InputType)!
                .GetMethod(NativeDistribution457Names.Copy, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [output])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } cause)
        {
            ExceptionDispatchInfo.Capture(cause).Throw();
            throw;
        }
    }
}
