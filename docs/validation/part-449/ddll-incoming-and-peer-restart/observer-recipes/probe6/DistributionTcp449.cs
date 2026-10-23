using System.Buffers.Binary;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;

internal sealed class DistributionTcp449:IAsyncDisposable
{
 internal const string FixtureCookie="ddll449_owned_fixture_cookie";
 internal const ulong AdvertisedFlags=DistributionPassThroughFlags.Mandatory|0x04000000|0x4000|1;
 private readonly ProcessRuntime runtime;
 private readonly string directory;
 private TcpClient client=new();
 private readonly CancellationTokenSource stop=new();
 private readonly SemaphoreSlim writes=new(1);
 private readonly List<string> failures=[];
 private NetworkStream? stream;
 private DriverParentHarnessActor423? controller;
 private Task? reader,writer;
 private int incoming,outgoing;
 private bool disposed;
 private readonly TaskCompletionSource<bool> remoteClosed=new(TaskCreationOptions.RunContinuationsAsynchronously);
 internal Task RemoteClosed=>remoteClosed.Task;
 internal bool ExpectRemoteExit {get;set;}
 internal TupleTerm Handle {get;private set;}=null!;
 internal uint RemoteCreation {get;private set;}
 internal ulong Flags {get;private set;}
 internal DistributionConnectedSnapshot? FinalSnapshot {get;private set;}
 internal bool Authenticated {get;private set;}
 internal DistributionTcp449(ProcessRuntime runtime,string directory)
 {
  this.runtime=runtime;this.directory=directory;Directory.CreateDirectory(directory);
 }
 internal async Task Accept(DistributionListener449 listener,string peer,string local,uint creation,bool wrongCookie=false)
 {
  using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
  client=await listener.Listener.AcceptTcpClientAsync(deadline.Token);stream=client.GetStream();
  byte[] hello=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"hello.bin"),hello);
  DriverParentHarnessAccess423.Require(hello.Length>=15&&hello[0]==(byte)'N');
  ulong offered=BinaryPrimitives.ReadUInt64BigEndian(hello.AsSpan(1));RemoteCreation=BinaryPrimitives.ReadUInt32BigEndian(hello.AsSpan(9));int nameLength=BinaryPrimitives.ReadUInt16BigEndian(hello.AsSpan(13));
  DriverParentHarnessAccess423.Require(hello.Length==15+nameLength&&Encoding.Latin1.GetString(hello.AsSpan(15))==peer);
  Flags=AdvertisedFlags&offered;DriverParentHarnessAccess423.Require(DistributionCapabilityProfile429.SupportsNegotiation(Flags));
  byte[] status="sok"u8.ToArray();await Write(status,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"status.bin"),status);
  uint ownChallenge=BinaryPrimitives.ReadUInt32BigEndian(RandomNumberGenerator.GetBytes(4));
  byte[] name=Encoding.Latin1.GetBytes(local),challenge=new byte[19+name.Length];challenge[0]=(byte)'N';BinaryPrimitives.WriteUInt64BigEndian(challenge.AsSpan(1),AdvertisedFlags);BinaryPrimitives.WriteUInt32BigEndian(challenge.AsSpan(9),ownChallenge);BinaryPrimitives.WriteUInt32BigEndian(challenge.AsSpan(13),creation);BinaryPrimitives.WriteUInt16BigEndian(challenge.AsSpan(17),checked((ushort)name.Length));name.CopyTo(challenge,19);
  await Write(challenge,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"challenge.bin"),challenge);
  byte[] reply=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"reply.bin"),reply);
  DriverParentHarnessAccess423.Require(reply.Length==21&&reply[0]==(byte)'r');
  bool valid=CryptographicOperations.FixedTimeEquals(reply.AsSpan(5),Digest(FixtureCookie,ownChallenge));
  if(wrongCookie)
  {
   DriverParentHarnessAccess423.Require(!valid);client.Dispose();
   File.WriteAllText(Path.Combine(directory,"authentication.json"),JsonSerializer.Serialize(new{ExpectedWrongCookieRejected=true,ChallengeReceived=true,Adopted=false,RemoteCreation,Flags,Incoming=true}));return;
  }
  DriverParentHarnessAccess423.Require(valid);uint remoteChallenge=BinaryPrimitives.ReadUInt32BigEndian(reply.AsSpan(1));
  byte[] ack=new byte[17];ack[0]=(byte)'a';Digest(FixtureCookie,remoteChallenge).CopyTo(ack,1);await Write(ack,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"ack.bin"),ack);
  Authenticated=true;
  File.WriteAllText(Path.Combine(directory,"authentication.json"),JsonSerializer.Serialize(new{Authenticated,Local=local,Remote=peer,LocalCreation=creation,RemoteCreation,Flags,ChallengeReplyVerified=true,AckSent=true,Packet2Then4=true,Incoming=true}));
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
  return await controller!.Invoke(context=>
  {
   var before=runtime.DistributionPending.ConnectedSnapshot(Handle);Term? result=null;string? failure=null;
   try{result=DistributionInputAdapter.PutData(context,oldHandle,new BitString(genuineOldPacket));}
   catch(ErlangException error) when(error.Reason.Equals(Term.A("badarg"))){failure=error.ToString();}
   var after=runtime.DistributionPending.ConnectedSnapshot(Handle);
   bool inert=after.AcceptedInputPackets==before.AcceptedInputPackets&&after.QueuedPacketBytes==before.QueuedPacketBytes&&after.QueuedPackets==before.QueuedPackets&&after.QueuedSegments==before.QueuedSegments;
   var proof=new{Passed=inert&&(result?.Equals(Term.A("ok"))==true||failure is not null),Before=before,After=after,Result=result?.ToString(),Failure=failure,CurrentActualInputHandler=true,OldPayloadInert=inert,OneControllerTurn=true};
   File.WriteAllText(Path.Combine(directory,"stale-input-attempt.json"),JsonSerializer.Serialize(proof));
   DriverParentHarnessAccess423.Require(proof.Passed);return Task.FromResult<object>(proof);
  });
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
  catch(Exception error) when(ExpectRemoteExit&&error is EndOfStreamException or IOException){remoteClosed.TrySetResult(true);}
  catch(Exception error){lock(failures)failures.Add("input: "+error);remoteClosed.TrySetException(error);}
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
