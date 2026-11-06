using System.Reflection;
using Erlang;

// Bind the actual existing runtime I/O device. No OTP/library/test return values are substituted.
internal static class GenServerValidationIoDevice
{
    private const string DeviceType = "Erlang.IoDefaultDevice";
    private const string ExecuteMethod = "Execute";

    internal static ProcessHandle Start(ProcessRuntime runtime)
    {
        MethodInfo execute = typeof(ProcessRuntime).Assembly.GetType(DeviceType, true)!.GetMethod(ExecuteMethod, BindingFlags.Static | BindingFlags.Public)!;

        return runtime.Spawn(async context =>
        {
            while (true)
            {
                Term request = (await context.ReceiveAsync(term => term))!;
                await (ValueTask)execute.Invoke(null, [context, request])!;
            }
        });
    }
}
