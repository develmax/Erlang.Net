import pathlib,subprocess,hashlib,json,re
root=pathlib.Path(__file__).resolve().parents[2]
out=pathlib.Path(__file__).resolve().parent
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
paths=['src/Erlang.Runtime/HostDistribution.cs','src/Erlang.Runtime/ProcessRuntime.cs','src/Erlang.Runtime/ProcessRuntime.ActorDispatch.cs','src/Erlang.Runtime/ProcessSlotTable.cs','src/Erlang.Runtime/RuntimeActorDispatcher.cs','src/Erlang.Runtime/RuntimeDistributionPending.cs','src/Erlang.Runtime/RuntimeDistributionPending.Connected.cs','src/Erlang.Runtime/RuntimeDistributionPending.LinkSignals.cs','src/Erlang.Runtime/ProcessRuntime.DistributionLinks.cs','src/Erlang.Runtime/ProcessRuntime.DistributionMonitors.cs']
rows=[]
pattern=re.compile(r'Terminate\(|ProcessTerminated\(|Done.Try|Unwound.Try|processes.Remove|processSlots.Remove|QueueConnectedControlSignal|QueueDistributionLinkReply|QueueDistributionUnlinkReply|SendConnectedMonitorControl|TryEnqueueFinalizedHostBuffer|EnqueueConnectedOutput|WaitForOutputCapacity|TryRunUnreserved')
for p in paths:
 b=(root/p).read_bytes()
 rows.append(dict(Path=p,Sha256=sha(b),Locations=[dict(Line=i,Text=line.strip()) for i,line in enumerate(b.decode().splitlines(),1) if pattern.search(line)]))
oldprefix='artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/'
for name in ['DistributionOutputAdmission.cs','RuntimeDistributionPending.AsyncOutput.cs','RuntimeDistributionPending.LinkSignals.cs','RuntimeDistributionPending.Connected.cs','ProcessRuntime.cs']:
 p=oldprefix+'src/Erlang.Runtime/'+name;b=(root/p).read_bytes()
 rows.append(dict(Path=p,Sha256=sha(b),Locations=[dict(Line=i,Text=line.strip()) for i,line in enumerate(b.decode().splitlines(),1) if pattern.search(line)]))
original=[]
for p,ranges in {'erts/emulator/beam/dist.c':[(3268,3298),(3595,3785),(4150,4190)],'erts/emulator/beam/erl_process.c':[(13600,13635),(13715,13755),(14234,14295),(14406,14482),(14635,14835)]}.items():
 b=subprocess.check_output(['git','-C',str(root.parent/'otp'),'show',pin+':'+p]);lines=b.decode().splitlines()
 target=out/(pathlib.Path(p).name+'.source.txt')
 target.write_text('\n\n'.join('\n'.join(f'{i}: {lines[i-1]}' for i in range(a,z+1)) for a,z in ranges)+'\n',encoding='utf8')
 original.append(dict(Path=p,Sha256=sha(b),Ranges=ranges,Extract=target.name))
target=out/'source-bindings.json'
target.write_text(json.dumps(dict(Status='Source-only audit; old full run incomplete, cause unknown',MainHead=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),PinnedCommit=pin,MainAndCandidate=rows,Original=original),indent=2)+'\n',encoding='utf8')
print(json.dumps(dict(Manifest=str(target),Sha256=sha(target.read_bytes()))))
