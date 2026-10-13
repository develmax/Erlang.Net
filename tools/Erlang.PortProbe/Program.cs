using System.Text;
using Erlang.PortProbe;

using Stream input = Console.OpenStandardInput();
using Stream output = Console.OpenStandardOutput();
if (args.FirstOrDefault() == PortProbeParameters.Emit)
{
    await output.WriteAsync(Encoding.ASCII.GetBytes(PortProbeParameters.Payload));
    await output.FlushAsync();

    return PortProbeParameters.ExitCode;
}
var buffer = new byte[PortProbeParameters.BufferBytes];
while (true)
{
    int count = await input.ReadAsync(buffer);
    if (count == 0)
        break;
    await output.WriteAsync(buffer.AsMemory(0, count));
    await output.FlushAsync();
}

return 0;
