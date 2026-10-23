using System.Buffers.Binary;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;

internal sealed class DistributionTcp448:IAsyncDisposable
{
 internal const string FixtureCookie="ddll448_owned_fixture_cookie";
 internal const ulong AdvertisedFlags=DistributionPassThroughFlags.Mandatory|0x04000000|0x4000|1;
 private readonly ProcessRuntime runtime;
 private readonly string directory;
 private readonly TcpClient client=new();
 private readonly CancellationTokenSource stop=new();
 private readonly SemaphoreSlim writes=new(1);
 private readonly List<string> failures=[];
 private NetworkStream? stream;
 private DriverParentHarnessActor423? controller;
 private Task? reader,writer;
 private int incoming,outgoing;
 private bool disposed;
 internal TupleTerm Handle {get;private set;}=null!;
 internal uint RemoteCreation {get;private set;}
 internal ulong Flags {get;private set;}
 internal DistributionConnectedSnapshot? FinalSnapshot {get;private set;}
 internal bool Authenticated {get;private set;}
 internal DistributionTcp448(ProcessRuntime runtime,string directory)
 {
  this.runtime=runtime;this.directory=directory;Directory.CreateDirectory(directory);
 }
 internal async Task Connect(int port,string peer,string local,uint creation,bool wrongCookie=false)
 {
  using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
  await client.ConnectAsync("127.0.0.1",port,deadline.Token);stream=client.GetStream();
  byte[] name=Encoding.Latin1.GetBytes(local),hello=new byte[15+name.Length];
  hello[0]=(byte)'N';BinaryPrimitives.WriteUInt64BigEndian(hello.AsSpan(1),AdvertisedFlags);BinaryPrimitives.WriteUInt32BigEndian(hello.AsSpan(9),creation);BinaryPrimitives.WriteUInt16BigEndian(hello.AsSpan(13),checked((ushort)name.Length));name.CopyTo(hello,15);
  await Write(hello,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"hello.bin"),hello);
  byte[] status=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"status.bin"),status);
  DriverParentHarnessAccess423.Require(status.AsSpan().SequenceEqual("sok"u8));
  byte[] challenge=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"challenge.bin"),challenge);
  DriverParentHarnessAccess423.Require(challenge.Length>=19&&challenge[0]==(byte)'N');
  ulong remoteFlags=BinaryPrimitives.ReadUInt64BigEndian(challenge.AsSpan(1));uint remoteChallenge=BinaryPrimitives.ReadUInt32BigEndian(challenge.AsSpan(9));RemoteCreation=BinaryPrimitives.ReadUInt32BigEndian(challenge.AsSpan(13));int nameLength=BinaryPrimitives.ReadUInt16BigEndian(challenge.AsSpan(17));
  DriverParentHarnessAccess423.Require(challenge.Length==19+nameLength&&Encoding.Latin1.GetString(challenge.AsSpan(19))==peer);
  Flags=AdvertisedFlags&remoteFlags;DriverParentHarnessAccess423.Require(DistributionCapabilityProfile429.SupportsNegotiation(Flags));
  uint ownChallenge=BinaryPrimitives.ReadUInt32BigEndian(RandomNumberGenerator.GetBytes(4));
  byte[] reply=new byte[21];reply[0]=(byte)'r';BinaryPrimitives.WriteUInt32BigEndian(reply.AsSpan(1),ownChallenge);Digest(wrongCookie?FixtureCookie+"_wrong":FixtureCookie,remoteChallenge).CopyTo(reply,5);
  await Write(reply,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"reply.bin"),reply);
  byte[] ack;
  try{ack=await Read(2,deadline.Token);}
  catch(EndOfStreamException) when(wrongCookie)
  {
   File.WriteAllText(Path.Combine(directory,"authentication.json"),JsonSerializer.Serialize(new{ExpectedWrongCookieRejected=true,ChallengeReceived=true,Adopted=false,RemoteCreation,Flags}));return;
  }
  File.WriteAllBytes(Path.Combine(directory,"ack.bin"),ack);
  DriverParentHarnessAccess423.Require(!wrongCookie&&ack.Length==17&&ack[0]==(byte)'a'&&CryptographicOperations.FixedTimeEquals(ack.AsSpan(1),Digest(FixtureCookie,ownChallenge)));
  Authenticated=true;
  File.WriteAllText(Path.Combine(directory,"authentication.json"),JsonSerializer.Serialize(new{Authenticated,Local=local,Remote=peer,RemoteCreation,Flags,ChallengeAckVerified=true,Packet2Then4=true}));
  controller=new(runtime);Handle=runtime.DistributionPending.BeginPending(Term.A(peer));
  await controller.Invoke(context=>
  {
   runtime.DistributionPending.AdoptConnected(Handle,context,Flags,RemoteCreation);runtime.DistributionPending.SetConnectedInputHandler(Handle,context,context.Self);return Task.FromResult(true);
  });
  reader=ReadPackets();writer=WritePackets();
 }
 private static byte[] Digest(string cookie,uint challenge)=>MD5.HashData(Encoding.Latin1.GetBytes(cookie+challenge.ToString(CultureInfo.InvariantCulture)));
 internal async Task<object> ProbeStale(Term oldHandle,byte[] genuineOldPacket)
 {
  var before=runtime.DistributionPending.ConnectedSnapshot(Handle);Term? result=null;string? failure=null;
  try{result=await controller!.Invoke(context=>Task.FromResult(DistributionInputAdapter.PutData(context,oldHandle,new BitString(genuineOldPacket))));}
  catch(ErlangException error) when(error.Reason.Equals(Term.A("badarg"))){failure=error.ToString();}
  var after=runtime.DistributionPending.ConnectedSnapshot(Handle);
  bool inert=after.AcceptedInputPackets==before.AcceptedInputPackets&&after.QueuedPacketBytes==before.QueuedPacketBytes&&after.QueuedPackets==before.QueuedPackets&&after.QueuedSegments==before.QueuedSegments;
  DriverParentHarnessAccess423.Require(inert&&(result?.Equals(Term.A("ok"))==true||failure is not null));
  return new{Passed=true,Before=before,After=after,Result=result?.ToString(),Failure=failure,CurrentActualInputHandler=true,OldPayloadInert=true};
 }
 private async Task<byte[]> Read(int prefix,CancellationToken cancellation)
 {
  byte[] header=new byte[prefix];await stream!.ReadExactlyAsync(header,cancellation);int size=prefix==2?BinaryPrimitives.ReadUInt16BigEndian(header):checked((int)BinaryPrimitives.ReadUInt32BigEndian(header));
  if(size>1048576)throw new InvalidDataException("Distribution frame exceeds bound");byte[] body=new byte[size];await stream.ReadExactlyAsync(body,cancellation);return body;
 }
 private async Task Write(byte[] body,int prefix,CancellationToken cancellation)
 {
  byte[] header=new byte[prefix];if(prefix==2)BinaryPrimitives.WriteUInt16BigEndian(header,checked((ushort)body.Length));else BinaryPrimitives.WriteUInt32BigEndian(header,checked((uint)body.Length));
  await writes.WaitAsync(cancellation);try{await stream!.WriteAsync(header,cancellation);await stream.WriteAsync(body,cancellation);}finally{writes.Release();}
 }
 private async Task ReadPackets()
 {
  try
  {
   while(true)
   {
    byte[] packet=await Read(4,stop.Token);
    if(packet.Length==0){await Write([],4,stop.Token);continue;}
    File.WriteAllBytes(Path.Combine(directory,$"input-{++incoming:D4}.bin"),packet);
    await controller!.Invoke(context=>Task.FromResult(DistributionInputAdapter.PutData(context,Handle,new BitString(packet))));
   }
  }
  catch(Exception error) when(stop.IsCancellationRequested&&error is OperationCanceledException or IOException or ObjectDisposedException){ }
  catch(Exception error){lock(failures)failures.Add("input: "+error);}
 }
 private async Task WritePackets()
 {
  try
  {
   while(true)
   {
    stop.Token.ThrowIfCancellationRequested();
    HostDistributionBuffer? buffer=await controller!.Invoke(context=>Task.FromResult(runtime.DistributionPending.TakeConnectedOutput(Handle,context,out _)));
    if(buffer is null){await Task.Delay(1,stop.Token);continue;}
    byte[] packet=buffer.Segments.SelectMany(s=>s.ToArray()).ToArray();File.WriteAllBytes(Path.Combine(directory,$"output-{++outgoing:D4}.bin"),packet);await Write(packet,4,stop.Token);
   }
  }
  catch(Exception error) when(stop.IsCancellationRequested&&error is OperationCanceledException or IOException or ObjectDisposedException){ }
  catch(Exception error){lock(failures)failures.Add("output: "+error);}
 }
 public async ValueTask DisposeAsync()
 {
  if(disposed)return;disposed=true;
  if(controller is not null)try{FinalSnapshot=runtime.DistributionPending.ConnectedSnapshot(Handle);if(FinalSnapshot.QueuedPacketBytes!=0||FinalSnapshot.QueuedPackets!=0||FinalSnapshot.QueuedSegments!=0)failures.Add("Output queue is not drained");}catch(Exception error){lock(failures)failures.Add("snapshot: "+error);}
  stop.Cancel();client.Dispose();
  try{await Task.WhenAll(new[]{reader,writer}.Where(t=>t is not null).Cast<Task>()).WaitAsync(TimeSpan.FromSeconds(10));}catch(Exception error){lock(failures)failures.Add("join: "+error);}
  if(controller is not null)try{await controller.DisposeAsync();}catch(Exception error){lock(failures)failures.Add("controller: "+error);}
  File.WriteAllText(Path.Combine(directory,"cleanup.json"),JsonSerializer.Serialize(new{Passed=failures.Count==0,Authenticated,Incoming=incoming,Outgoing=outgoing,FinalSnapshot,Failures=failures,ControllerJoined=controller?.Process.Completion.IsCompletedSuccessfully,SocketDisposed=true}));
  if(failures.Count!=0)throw new AggregateException(failures.Select(s=>new IOException(s)));
 }
}
