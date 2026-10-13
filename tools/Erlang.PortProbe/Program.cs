using System.Text;
using Erlang.PortProbe;

using Stream input = Console.OpenStandardInput();
using Stream output = Console.OpenStandardOutput();
if (args.FirstOrDefault() is PortProbeParameters.SlowEcho or PortProbeParameters.DrainFile)
{
    await output.WriteAsync(PortProbeParameters.ReadyPacket);
    await output.FlushAsync();
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
using Stream? capture = args.FirstOrDefault() == PortProbeParameters.DrainFile ? File.Create(args[1]) : null;
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
