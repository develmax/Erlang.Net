from pathlib import Path
import shutil
r=Path.cwd();old=r/'artifacts/library448-probe3';out=r/'artifacts/library449-probe1';assert not out.exists();out.mkdir()
for f in old.glob('*'):
 if f.is_file():
  text=f.read_text().replace('448','449');(out/f.name.replace('448','449')).write_text(text)
# Keep authenticated final runtime/package, do not invent a fresh build or change previous evidence.
p=out/'run-root.py';s=p.read_text().replace('library449-driver-package2','library448-driver-package2')
s=s.replace("'ddll449managed'+str(depth)+'@127.0.0.1'],", "'ddll449managed'+str(depth)+'@127.0.0.1',str(epmd_port)],")
p.write_text(s)
p=out/'beam_ddll_distribution449.erl';s=p.read_text().replace('start_node/1,','start_node/1,start_node_creation/2,ping_node/1,set_peer_cookie/2,')
s=s.replace('start_node(Name) ->\n    Kernel=', 'start_node(Name) -> start_node_creation(Name,4).\nstart_node_creation(Name,Creation) ->\n    Kernel=').replace('erlang:setnode(Name,4)','erlang:setnode(Name,Creation)')
s+='\nping_node(Node) -> net_adm:ping(Node).\nset_peer_cookie(Node,Cookie) -> erlang:set_cookie(Node,Cookie).\n'
p.write_text(s)
p=out/'DistributionTcp449.cs';s=p.read_text().replace('private readonly TcpClient client=new();','private TcpClient client=new();')
start=s.index(' internal async Task Connect(');end=s.index(' private static byte[] Digest',start)
s=s[:start]+''' internal async Task Accept(DistributionListener449 listener,string peer,string local,uint creation,bool wrongCookie=false)
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
''' +s[end:]
# Actual remote halt is an expected EOF, not evidence that an open unrelated channel fault can be ignored.
s=s.replace('private bool disposed;','private bool disposed;\n private readonly TaskCompletionSource<bool> remoteClosed=new(TaskCreationOptions.RunContinuationsAsynchronously);\n internal Task RemoteClosed=>remoteClosed.Task;')
s=s.replace('  catch(Exception error){lock(failures)failures.Add("input: "+error);}', '  catch(EndOfStreamException){remoteClosed.TrySetResult(true);}\n  catch(Exception error){lock(failures)failures.Add("input: "+error);remoteClosed.TrySetException(error);}')
p.write_text(s)
