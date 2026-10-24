using System.Text;
using Erlang;
namespace Erlang.Tests;

internal static class InternalTextAux430HostAccess
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalTextAux430HostNames.Failure);
    }

    internal static ErlangFunction Body(ProcessRuntime runtime, string function, int arity)
    {
        var type = typeof(ProcessRuntime).Assembly.GetType(InternalTextAux430HostNames.NativeType, true)!;
        object?[] arguments = [runtime, InternalTextAux430HostNames.Module, function, arity, null];
        Require((bool)type.GetMethod(InternalTextAux430HostNames.Capture, InternalTextAux430HostNames.Flags)!.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    internal static Term Bytes(string value) => Cons.From(Encoding.UTF8.GetBytes(value).Select(item => Term.I(item)));

    internal static byte[] Display(ProcessRuntime runtime, Term value)
    {
        var type = typeof(ProcessRuntime).Assembly.GetType(InternalTextAux430HostNames.WriterType, true)!;
        object writer = Activator.CreateInstance(type, [runtime])!;

        return (byte[])type.GetMethod(InternalTextAux430HostNames.Write, InternalTextAux430HostNames.Flags)!.Invoke(writer, [value])!;
    }

    internal static async Task<IReadOnlyList<int>> RunAuxiliary(ProcessRuntime runtime)
    {
        object dispatcher = typeof(ProcessRuntime).GetField(InternalTextAux430HostNames.Dispatcher, InternalTextAux430HostNames.Flags)!.GetValue(runtime)!;
        var observed = new List<int>();
        Action<int> body = identity =>
{
    lock (observed) observed.Add(identity);
};
        object batch = dispatcher.GetType().GetMethod(InternalTextAux430HostNames.Prepare, InternalTextAux430HostNames.Flags)!.Invoke(dispatcher, [body])!;
        batch.GetType().GetMethod(InternalTextAux430HostNames.Publish, InternalTextAux430HostNames.Flags)!.Invoke(batch, null);
        Task completion = (Task)batch.GetType().GetProperty(InternalTextAux430HostNames.Completion, InternalTextAux430HostNames.Flags)!.GetValue(batch)!;
        await completion.WaitAsync(InternalTextAux430HostNames.Deadline);
        lock (observed) return observed.Order().ToArray();
    }
}
