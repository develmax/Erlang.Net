using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;

internal sealed partial class DistributionTcp450
{
 internal async Task<bool> Connect(int port,string peer,string local,uint creation,string? aliveReply=null,bool wrongCookie=false,Func<Task>? retireOld=null,Action<string>? statusObserved=null)
 {
  using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
  await client.ConnectAsync("127.0.0.1",port,deadline.Token);stream=client.GetStream();
  byte[] name=Encoding.Latin1.GetBytes(local),hello=new byte[15+name.Length];hello[0]=(byte)'N';BinaryPrimitives.WriteUInt64BigEndian(hello.AsSpan(1),AdvertisedFlags);BinaryPrimitives.WriteUInt32BigEndian(hello.AsSpan(9),creation);BinaryPrimitives.WriteUInt16BigEndian(hello.AsSpan(13),checked((ushort)name.Length));name.CopyTo(hello,15);
  await Write(hello,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"hello.bin"),hello);
  byte[] status=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"status.bin"),status);
  DriverParentHarnessAccess423.Require(status.Length>1&&status[0]==(byte)'s');string code=Encoding.Latin1.GetString(status.AsSpan(1));statusObserved?.Invoke(code);
  if(code=="nok")
  {
   client.Dispose();File.WriteAllText(Path.Combine(directory,"candidate-refusal.json"),JsonSerializer.Serialize(new{OriginalStatus=code,Adopted=false}));return false;
  }
  if(code=="alive")
  {
   DriverParentHarnessAccess423.Require(aliveReply is "true" or "false");byte[] response=Encoding.Latin1.GetBytes("s"+aliveReply);await Write(response,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"alive-response.bin"),response);
   if(aliveReply=="false")
   {
    bool closed=false;try{await Read(2,deadline.Token);}catch(EndOfStreamException){closed=true;}
    DriverParentHarnessAccess423.Require(closed);client.Dispose();File.WriteAllText(Path.Combine(directory,"candidate-refusal.json"),JsonSerializer.Serialize(new{OriginalStatus=code,AliveReply=false,OriginalClosed=true,Adopted=false}));return false;
   }
  }
  else DriverParentHarnessAccess423.Require(code is "ok" or "ok_simultaneous");
  byte[] challenge=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"challenge.bin"),challenge);
  DriverParentHarnessAccess423.Require(challenge.Length>=19&&challenge[0]==(byte)'N');ulong offered=BinaryPrimitives.ReadUInt64BigEndian(challenge.AsSpan(1));uint remoteChallenge=BinaryPrimitives.ReadUInt32BigEndian(challenge.AsSpan(9));RemoteCreation=BinaryPrimitives.ReadUInt32BigEndian(challenge.AsSpan(13));int count=BinaryPrimitives.ReadUInt16BigEndian(challenge.AsSpan(17));
  DriverParentHarnessAccess423.Require(challenge.Length==19+count&&Encoding.Latin1.GetString(challenge.AsSpan(19))==peer);Flags=AdvertisedFlags&offered;DriverParentHarnessAccess423.Require(DistributionCapabilityProfile429.SupportsNegotiation(Flags));
  uint ownChallenge=BinaryPrimitives.ReadUInt32BigEndian(RandomNumberGenerator.GetBytes(4));byte[] reply=new byte[21];reply[0]=(byte)'r';BinaryPrimitives.WriteUInt32BigEndian(reply.AsSpan(1),ownChallenge);Digest(wrongCookie?FixtureCookie+"_wrong":FixtureCookie,remoteChallenge).CopyTo(reply,5);await Write(reply,2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"reply.bin"),reply);
  if(retireOld is not null)await retireOld().WaitAsync(deadline.Token);
  byte[] ack;
  try{ack=await Read(2,deadline.Token);}
  catch(EndOfStreamException) when(wrongCookie)
  {
   client.Dispose();File.WriteAllText(Path.Combine(directory,"candidate-refusal.json"),JsonSerializer.Serialize(new{OriginalStatus=code,AliveReply=true,WrongCookieRejected=true,Adopted=false}));return false;
  }
  File.WriteAllBytes(Path.Combine(directory,"ack.bin"),ack);DriverParentHarnessAccess423.Require(!wrongCookie&&ack.Length==17&&ack[0]==(byte)'a'&&CryptographicOperations.FixedTimeEquals(ack.AsSpan(1),Digest(FixtureCookie,ownChallenge)));
  Authenticated=true;File.WriteAllText(Path.Combine(directory,"authentication.json"),JsonSerializer.Serialize(new{Authenticated,Local=local,Remote=peer,LocalCreation=creation,RemoteCreation,Flags,ChallengeAckVerified=true,OriginalStatus=code,Incoming=false,Packet2Then4=true,OldControllerRetiredBeforeHostAckRead=retireOld is not null}));
  controller=new(runtime);Handle=runtime.DistributionPending.BeginPending(Term.A(peer));
  await controller.Invoke(context=>
  {
   runtime.DistributionPending.AdoptConnected(Handle,context,Flags,RemoteCreation);runtime.DistributionPending.SetConnectedInputHandler(Handle,context,context.Self);return Task.FromResult(true);
  });
  reader=ReadPackets();writer=WritePackets();return true;
 }
}
