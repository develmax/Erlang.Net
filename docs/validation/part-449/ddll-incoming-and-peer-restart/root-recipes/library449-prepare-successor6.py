from pathlib import Path
import shutil,json
out=Path('artifacts/library449-probe6');assert not out.exists();shutil.copytree('artifacts/library449-probe5',out)
p=out/'DistributionTcp449.cs';s=p.read_text();start=s.index(' internal async Task<object> ProbeStale(');end=s.index(' private async Task<byte[]> Read(',start)
s=s[:start]+''' internal async Task<object> ProbeStale(Term oldHandle,byte[] genuineOldPacket)
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
''' +s[end:];p.write_text(s)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library449-probe5','Reason':'Observed genuine fresh global input may arrive concurrently after incoming auth; before/stale/after now execute within one actual controller turn, with raw attempt saved before assertion. Preserve prior race, no weakening of zero-stale-input/queue criterion.'},indent=2)+'\n')
print('Successor6 prepared')
