from pathlib import Path
p=Path('artifacts/library451-probe1/DistributionTcp451.Initiator.cs');s=p.read_text();start=s.index('  byte[] status;');end=s.index('  DriverParentHarnessAccess423.Require(status.Length',start)
s=s[:start]+'''  if(expectSilentRefusal)
  {
   int received=-1;bool reset=false;
   try{received=await stream!.ReadAsync(new byte[1],deadline.Token);}
   catch(IOException error) when(error.InnerException is SocketException socket&&socket.SocketErrorCode is SocketError.ConnectionReset or SocketError.ConnectionAborted){reset=true;}
   DriverParentHarnessAccess423.Require(received==0||reset);client.Dispose();
   File.WriteAllText(Path.Combine(directory,"candidate-refusal.json"),JsonSerializer.Serialize(new{OriginalClosedWithoutStatus=true,ReceivedStatusBytes=0,PhysicalEof=received==0,PhysicalReset=reset,Adopted=false,ChallengeReceived=false}));return false;
  }
  byte[] status=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"status.bin"),status);
'''+s[end:];p.write_text(s)
