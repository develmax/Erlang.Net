using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

internal sealed class DistributionListener449 : IDisposable
{
    internal TcpListener Listener { get; } = new(IPAddress.Loopback, 0);
    internal uint Creation { get; }
    private readonly TcpClient registration = new();

    internal DistributionListener449(int epmdPort, string name, string directory)
    {
        Directory.CreateDirectory(directory);
        Listener.Start();
        registration.Connect(IPAddress.Loopback, epmdPort);
        var stream = registration.GetStream();
        stream.ReadTimeout = 10000;
        stream.WriteTimeout = 10000;
        byte[] shortName = Encoding.Latin1.GetBytes(name.Split('@')[0]);
        byte[] request = new byte[13 + shortName.Length];
        request[0] = 120;
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(1), checked((ushort)((IPEndPoint)Listener.LocalEndpoint).Port));
        request[3] = (byte)'M';
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(5), 6);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(7), 6);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(9), checked((ushort)shortName.Length));
        shortName.CopyTo(request, 11);
        byte[] size = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(size, checked((ushort)request.Length));
        stream.Write(size);
        stream.Write(request);
        byte[] response = new byte[6];
        stream.ReadExactly(response);
        if (response[0] != 118 || response[1] != 0)
            throw new IOException("Private EPMD ALIVE2_X registration refused");

        Creation = BinaryPrimitives.ReadUInt32BigEndian(response.AsSpan(2));
        File.WriteAllBytes(Path.Combine(directory, "alive2-request.bin"), request);
        File.WriteAllBytes(Path.Combine(directory, "alive2-response.bin"), response);
        File.WriteAllText(Path.Combine(directory, "registration.json"), JsonSerializer.Serialize(new
        {
            Node = name,
            Creation,
            Port = ((IPEndPoint)Listener.LocalEndpoint).Port,
            EpmdPort = epmdPort,
            RegistrationSocketKeptOpen = true
        }));
    }

    public void Dispose()
    {
        Listener.Stop();
        registration.Dispose();
    }
}
