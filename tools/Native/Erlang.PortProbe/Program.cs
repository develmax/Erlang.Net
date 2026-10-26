using System.Text;
using Erlang.PortProbe;

using Stream input = args.FirstOrDefault() == PortProbeParameters.Auxiliary && OperatingSystem.IsLinux()
    ? new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(PortProbeParameters.AuxiliaryInputDescriptor, false), FileAccess.Read)
    : Console.OpenStandardInput();
using Stream output = args.FirstOrDefault() == PortProbeParameters.Auxiliary && OperatingSystem.IsLinux()
    ? new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(PortProbeParameters.AuxiliaryOutputDescriptor, false), FileAccess.Write)
    : Console.OpenStandardOutput();
if (args.FirstOrDefault() == PortProbeParameters.Streams)
{
    using Stream error = Console.OpenStandardError();
    await output.WriteAsync(PortProbeParameters.OutputFirst);
    await output.FlushAsync();
    await error.WriteAsync(PortProbeParameters.ErrorFirst);
    await error.FlushAsync();
    await output.WriteAsync(PortProbeParameters.OutputSecond);
    await output.FlushAsync();
    await error.WriteAsync(PortProbeParameters.ErrorSecond);
    await error.FlushAsync();

    return 0;
}
if (args.FirstOrDefault() == PortProbeParameters.ArgumentZero)
{
    byte[] first = PortProbeNativeArguments.First();
    await output.WriteAsync(new byte[] { checked((byte)first.Length) });
    await output.WriteAsync(first);
    await output.FlushAsync();

    return 0;
}
if (args.FirstOrDefault() == PortProbeParameters.InputEof)
{
    int count = await input.ReadAsync(new byte[1]);
    if (count != 0)
        return PortProbeParameters.ExitCode;
    await output.WriteAsync(PortProbeParameters.InputClosedPacket);
    await output.FlushAsync();

    return 0;
}
if (args.FirstOrDefault() is PortProbeParameters.SlowEcho or PortProbeParameters.DrainFile or PortProbeParameters.HeldDrainFile)
{
    await output.WriteAsync(PortProbeParameters.ReadyPacket);
    await output.FlushAsync();
    if (args.FirstOrDefault() == PortProbeParameters.HeldDrainFile)
    {
        while (!File.Exists(args[2]))
            await Task.Delay(PortProbeParameters.ReleasePollMilliseconds);
    }
    else
        await Task.Delay(PortProbeParameters.StartDelayMilliseconds);
}
if (args.FirstOrDefault() == PortProbeParameters.Lines)
{
    await output.WriteAsync(Encoding.ASCII.GetBytes(PortProbeParameters.LinePayload));
    await output.FlushAsync();

    return 0;
}
if (args.FirstOrDefault() == PortProbeParameters.Emit)
{
    await output.WriteAsync(Encoding.ASCII.GetBytes(PortProbeParameters.Payload));
    await output.FlushAsync();

    return PortProbeParameters.ExitCode;
}
var buffer = new byte[PortProbeParameters.BufferBytes];
using Stream? capture = args.FirstOrDefault() is PortProbeParameters.DrainFile or PortProbeParameters.HeldDrainFile ? File.Create(args[1]) : null;
while (true)
{
    int count = await input.ReadAsync(buffer);
    if (count == 0)
        break;
    Stream destination = capture ?? output;
    await destination.WriteAsync(buffer.AsMemory(0, count));
    await destination.FlushAsync();
}

return 0;
