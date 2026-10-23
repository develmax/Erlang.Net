from pathlib import Path
import ast,gzip,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-448/ddll-distribution-handshake-reconnect';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
read=lambda p:json.loads((r/p).read_bytes())
runtime='1A0592162FA6080CA2E14464074B28986C3C23C256C9D106462662EA5A3427E8';base='614d81437a2cf4896059de558942f70cf78a96cd'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==base
assert read('artifacts/library448-gates1/completion.json')['Passed']
rows=[];channels=[]
aux={'peer-setup','peer-handshake-identity','peer-sees-managed','oracle-disconnect','physical-peer-node-down','peer-observes-both-absent','remote-driver-monitor-survives','physical-peer-unload'}
for run,depths in [('library448-probe-run3',[8]),('library448-probe-run4',[0,1,2])]:
 top=read('artifacts/'+run+'/completion.json');assert top['Passed'] and top['RuntimeSha256']==runtime and top['InputsUnchanged'] and len(top['ProviderScans'])==2 and all(x['Equal'] for x in top['ProviderScans'])
 epmd=read('artifacts/'+run+'/epmd.exit.json');assert epmd['Settled'] and epmd['ExitCode']==0 and not epmd['ForcedTermination']
 for depth in depths:
  p='artifacts/'+run+'/depth'+str(depth);report=read(p+'/observations/comparison.json');assert report['Passed'] and report['PhysicalCleanup'] and len(report['Results'])==34 and not report['Failures'];rows+=report['Results']
  assert sum(x['Name'] in aux for x in report['Results'])==8
  for role in ['primary','peer']:
   exit=read(p+'/'+role+'.exit.json');assert exit['Settled'] and exit['ExitCode']==0 and not exit['ForcedTermination']
  assert 'Invalid challenge reply' in (r/p/'peer.stdout.log').read_text()
  wrong=read(p+'/observations/wrong-cookie/authentication.json');assert wrong['ExpectedWrongCookieRejected'] and wrong['ChallengeReceived'] and not wrong['Adopted']
  for n in [1,2]:
   auth=read(p+'/observations/connection'+str(n)+'/authentication.json');assert auth['Authenticated'] and auth['ChallengeAckVerified'] and auth['Packet2Then4']
   close=read(p+'/observations/connection'+str(n)+'/cleanup.json');snap=close['FinalSnapshot'];assert close['Passed'] and close['ControllerJoined'] and close['SocketDisposed'] and close['Incoming']>0 and close['Outgoing']>0
   assert snap['QueuedPacketBytes']==snap['QueuedPackets']==snap['QueuedSegments']==0;channels.append((auth,close))
  assert channels[-2][0]['RemoteCreation']==channels[-1][0]['RemoteCreation'] and channels[-2][1]['FinalSnapshot']['ConnectionId']!=channels[-1][1]['FinalSnapshot']['ConnectionId']
  stale=read(p+'/observations/old-handle-current-input-handler.json');assert stale['Passed'] and stale['OldPayloadInert'] and stale['CurrentActualInputHandler'] and stale['Before']['AcceptedInputPackets']==stale['After']['AcceptedInputPackets']
assert len(rows)==136 and len(channels)==8 and all(x['Passed'] for x in rows)
assert sum(x['WholeRawEqual'] and x['WholeTypedEqual'] for x in rows)==116
assert sum(x['Name'] not in aux for x in rows)==104
assert not read('artifacts/library448-probe-run2/completion.json')['Passed'];red=read('artifacts/library448-probe-run2/depth8/observations/connection1/cleanup.json');assert red['Authenticated'] and red['Outgoing']==0 and red['Incoming']==2
for name in ['Program.cs','DistributionTcp448.cs','Peer448.cs','beam_ddll_distribution448.erl','ddll_tcp448.erl']:assert sha(r/'artifacts/library448-probe2'/name)==sha(r/'artifacts/library448-probe3'/name)
for name in ['beam_ddll_distribution448.beam','ddll_tcp448.beam']:assert sha(r/'artifacts/library448-probe-run2/observer-ebin'/name)==sha(r/'artifacts/library448-probe-run3/observer-ebin'/name)==sha(r/'artifacts/library448-probe-run4/observer-ebin'/name)
reg=read('artifacts/library448-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==587 and reg['ExactCoverage'] and reg['ImmutableInputs']
boot=read('artifacts/library448-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
native=read('artifacts/library448-ddll-retained-managed1/observations/visibility-all.json');assert native['Passed'] and len(native['Results'])==8
assert read('artifacts/library448-ddll-retained-managed1/observations/ddll-init-prefix435/comparison.json')['Passed']
assert read('artifacts/library448-retained446-managed1/observations/ddll-frontier446/comparison.json')['Passed']
retained=read('artifacts/library448-retained447-run1/completion.json');assert retained['Passed'] and retained['RuntimeSha256']==runtime and len(retained['Groups'])==4
for d in [0,1,2,8]:
 x=read('artifacts/library448-retained447-run1/depth'+str(d)+'/observations/comparison.json');assert x['Passed'] and x['PhysicalCleanup'] and len(x['Results'])==48
assert read('artifacts/library448-style-readiness1/completion.json')['Passed']
focus=read('artifacts/library448-focus1.json');assert focus['Partial'] and focus['SelectedCount']==focus['Passed']==1 and focus['TotalCount']==8824 and focus['Failed']==0
ledger=read('docs/mfa-compatibility.json');old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert ledger['Entries']==old['Entries'] and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1621
for x in ledger['VerifiedSourceFiles']:assert sha(r/x['Path'])==x['Sha256'],x['Path']
src=[x['Path'] for x in read('artifacts/library448-source-guard-delta.json')['Changed']];assert len(src)==4 and set(subprocess.check_output(['git','diff','--name-only','HEAD','--','src']).decode().splitlines())==set(src)
tests=['tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/ConnectedRegisteredSendHostChecks448.cs','tests/Erlang.Tests/ConnectedRegisteredSendHostNames448.cs'];assert subprocess.check_output(['git','diff','--name-only','HEAD','--','tests']).decode().splitlines()==[tests[0]]
assert (r/'artifacts/library448-independent-analysis1/accepted-review.md').is_file()
helpers=r/'artifacts/library445-freeze-evidence.py';assert sha(helpers)==sha(r/'docs/validation/part-445/ddll-permanent-reload-force/root-recipes/library445-freeze-evidence.py')
functions=[n for n in ast.parse(helpers.read_text()).body if isinstance(n,ast.FunctionDef) and n.name in {'save','copy','tree'}];exec(compile(ast.Module(body=functions,type_ignores=[]),str(helpers),'exec'));out.mkdir(parents=True);archive=[]
trees=[
 ('artifacts/library448-probe-run1','initial-epmd-refusal',{'harness','drivers'}),
 ('artifacts/library448-probe-run2','before-fix-authenticated-send-timeout',{'harness','drivers'}),
 ('artifacts/library448-probe-run3','fixed-depth8',{'harness','drivers'}),
 ('artifacts/library448-probe-run4','fixed-depth0-1-2',{'harness','drivers'}),
 ('artifacts/library448-ddll-retained-managed1','retained36-native8',{'harness'}),
 ('artifacts/library448-retained446-managed1','retained446-36',{'harness'}),
 ('artifacts/library448-retained447-run1','retained447-192',{'harness','drivers'}),
 ('artifacts/library448-affected-run1','affected587',set()),
 ('artifacts/library448-standard79-final1','boot79',set()),
 ('artifacts/library448-style-readiness1','style-readiness',set()),
 ('artifacts/library448-gates1','gates',set()),
 ('artifacts/library448-independent-analysis1','independent-review',set())]
for source,dest,skip in trees:
 tree(source,dest,skip)
 for p in sorted((r/source).rglob('*.bin')):
  rel=p.relative_to(r/source)
  if not set(rel.parts).intersection(skip):copy(p.relative_to(r),Path(dest)/rel)
for n in [1,2,3]:tree('artifacts/library448-probe'+str(n),'recipe'+str(n),set())
for name in ['prepare-packet1','prepare-runner1','prepare-package1','prepare-package2','prepare-successor2','prepare-successor3','update-source-guards1','update-source-guards2','select-affected','run-standard79','replay447','style-readiness','gates1','freeze-evidence','freeze-evidence2','stage-audit','audit-pinned-source','prepare-publisher2']:
 copy('artifacts/library448-'+name+'.py','root-recipes/library448-'+name+'.py')
for name in ['source-guard-delta','main447-green','preparation-history']:copy('artifacts/library448-'+name+'.json',name+'.json')
copy('artifacts/library448-focus1.json','canonical-focus/report.json');copy('artifacts/library448-affected-selection1/selection.json','affected-selection.json')
for folder in ['library448-driver-package1','library448-driver-package2','binary-integration369/library448-current-build1','binary-integration369/library448-current-build2','binary-integration369/library448-current-build3','binary-integration369/library448-style-check1','binary-integration369/library448-style-check2','binary-integration369/library448-focus1']:
 for p in (r/'artifacts'/folder).glob('*'):
  if p.is_file() and p.suffix in {'.json','.log'}:copy(p.relative_to(r),'preparation/'+Path(folder).name+'/'+p.name)
copy('artifacts/library448-driver-package2/package/erldrv-host-package.json','preparation/final-package-manifest.json')
for name in src+tests:copy(name,'final-source/'+name)
for name in src:
 p=out/'before-source'/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(subprocess.check_output(['git','show','HEAD:'+name]))
authority=read('artifacts/library448-pinned-source-audit.json');assert authority['Passed'] and authority['AuthorityReadFromExactGitBlobs']
save(out/'source-authority.json',authority);copy('artifacts/library448-pinned-source-audit.json','pinned-source-audit.json')
save(out/'archive-map.json',archive)
save(out/'completion.json',dict(Status='SCOPED_LOCAL_ACCEPTANCE_EXACT_HEAD_CI_TRACKED_IN_PR',RuntimeSha256=runtime,ObservationRows=136,PairedCommonSourceObservations=104,AuxiliaryProtocolGuards=32,ExactRawRows=116,OwnReferenceProtocolRows=20,AuthenticatedConnections=8,WrongCookieRefusals=4,SourceGuards=1621,ApiEntries=708,FullContracts=118,ApiDelta=0,FullDelta=0,NewCanonicalGroups=1,ControlsInNewGroup=10,CanonicalCatalog=8824,Affected=587,RetainedSourceRows=264,NativeGroups=8,Standard=79,ProductionFilesChanged=4,WholeModuleClaim=False,PhysicalPeerRestartClaim=False))
(out/'README.md').write_text('''# Part448 — authenticated Erlang distribution and DDLL reconnect

Pinned OTP29.1.1: `ad05823719d77c8faee87348ea39513d4e2f99c5`. Part447 is merged as `614d8143`; exact merge CI39935740532 passes8823/8823, all mandatory steps and both packages. This is Git publication/CI package validation, not a NuGet release.

## Real transport and confirmed product correction

Publication audit refused to infer the source pin from checkout HEAD516126e9. The actual oracle remains the exact admitted OTP29.1.1 release provider. Authoritative source hashes are now read directly from pinned Git blobs: the only normalized dist_util checkout delta is an unrelated compile-warning attribute; the reviewed sender slice is byte-identical; flags/DDLL/reference-code source files match after LF normalization. Receiver trace-token changes outside that slice are not used as pinned authority. The partial first publication is retained locally as publication-refused1; no runtime, observer, original provider, golden value or test criterion changes.

Two actual original OS VMs boot with `-name`, a dedicated public fixture cookie and a private foreground EPMD; `-start_epmd false` prevents global daemon fallback. The admitted ERTS epmd executable and original provider are hash-bound. Genuine EPMD port discovery precedes physical loopback TCP. The host transport performs packet2 N/status/N/r/a handshake v6, verifies both peer name/creation and MD5 cookie+unsigned-decimal challenge ACK, then switches to packet4. No atom-cache/fragment flags are advertised. Wrong fixture cookie is refused after a real challenge; original peer logs confirm Invalid challenge reply. Only authenticated sockets are adopted by the actual runtime controller.

The data plane drains actual owned runtime distribution output and feeds actual input-handler calls. A common original-compiled Erlang actor uses public registered sends/replies; data does not bypass the runtime queues through host `runtime.Send` or raw control RPC. Auxiliary raw TCP controls configure fixtures, observe actual node state and join shutdown only; they are distinguished from distribution-carried DDLL data.

First actual run2 authenticates, receives2 global packets, emits0 outgoing packets and times out on the first registered remote request. The original branch returned before the managed await failed; that failed pair has no completed paired raw row. Full timeout/native/channel receipts remain retained. Inspection confirms `ErlangOperatorValues.Send` silently discards a remote `{Name,Node}` fallback. The correction routes real guest bang/send2/send3/send_nosuspend2/3 named targets through the existing ordinary/async/backpressure owners. REG_SEND6 / REG_SEND_TT16 field order matches pinned dist.c1668–1679. PID/alias paths keep their ownership guards. Four product files change; no pure OTP library is rewritten in C#.

On fresh Runtime1A059216, the **unchanged observer/handshake source and BEAM** passes:7 outgoing/9 incoming packets before disconnect and4 outgoing/6 incoming after reconnect. The same profile criteria are not relaxed. Source/BEAM hashes across red and green are identical; only the actual package/runtime changes. A permanent canonical HOST group verifies five guest routes in both ordinary/async modes,10 meaningful delivery controls including noconnect. Catalog grows8823→8824, not by the count of standalone observations.

## Accepted comparisons and physical lifecycle

- The genuine peer DDLL reference travels through the real distribution connection. Both source branches decode it and reject foreign demonitor with complete native errors/full source frames.
- The primary's queued reference travels to the peer, whose foreign demonitor leaves creator queue1. The peer re-encodes it, the creator restores its internal identity, cancellation succeeds and queue becomes0. Full actual binary return equals each complete own original wire.
- Another actual reference is stored in the peer process dictionary **before** disconnect. Physical socket/controller teardown and actual original disconnect_node generate peer nodedown; both nodes are then observed absent. The local DDLL queue and peer driver monitor survive.
- A second genuine authenticated TCP connection keeps remote VM creation and receives a new runtime connection generation/controller. Old ConnectedSnapshot is rejected. The current real input handler feeds the old handle a genuine old packet: it remains inert (ok or badarg according to live resource state), accepted input/queue counters do not change. Subsequent fresh real distribution input increases the current generation counter.
- The exact reference retained at the peer is retrieved after reconnect without resending its old wire, rejected there as foreign, encoded and returned through the new connection. Creator cancellation removes its real queue entry. Repeated cancellation of a completed internal ref remains true.
- Both accepted channels per profile have zero queued bytes/buffers/segments at close and joined actual controllers. Original workers are joined before stop ACK; both original OS VMs and private EPMD exit normally. Actual native PhysicalExit/PhysicalCleanup/Completion, zero leases, collector and persistent cleanup are enforced.

Profiles0/1/2/8 pass **136/136 observation rows**:104 paired common-source/protocol observations and32 auxiliary setup/identity/node/cleanup guards. There are116 entire typed/raw equalities and20 complete own-reference protocol comparisons. Own-reference pairs are captured from actual corresponding wire outputs; all remaining values, full frames and metadata are compared unchanged. Opaque reference bits are not relabelled equal. These are not136 full API contracts or136 canonical tests. Accepted composition is depth8 run3 plus remaining0/1/2 run4 on identical final sources/binaries; no successful depth8 repeat. Independent review recomputes all8 challenge reply/ACK digests from raw packets, all136 ETF pairs, both2685-input freezes, node/generation/drain/cleanup fences.

## Regression, documentation and limits

Final source passes **587/587 affected tests in30 immutable shards**, retaining all205 previous affected names and adding send/distribution/alias/trace/async targets. The new canonical group passes separately. Fresh replay of Part447 passes192 actions; previous DDLL72 source-profile results and all8 native/HOST groups pass, as do standard79 and actual collector/persistent cleanup. Source layout passes after only one new test file was manually formatted; broad formatter write was refused before execution and was not bypassed. The first guessed EPMD path, initial guard-updater refusal and actual failed network run remain explicit preparation history.

Default708 APIs and118 full contracts/classifications remain unchanged, deltas0/0. One existing source guard is updated and three reached helper guards are added:1618→1621. Readiness is regenerated on the final Tool/runtime, with explicitly historical8813 authority; it is not a current full-run claim. Exact publication-head8824/full CI and both packages are tracked in its attached PR.

This verifies outbound client handshake and controlled same-VM disconnect/reconnect over the admitted Windows/x64 driver backend. It is not physical peer restart, incoming connection acceptance, EPMD implementation, TLS, full standard net_kernel/application boot, all native platforms or complete DDLL/erlang contracts. Named priority+ALTACT sends remain unsupported by this legacy REG_SEND route; the public synchronous C# Send/Binary helper and auto-connect are outside the validated guest async entrypoints. Held native teardown interleavings, broader modes/transport/profile partitions remain open.

The archive is lossless provenance with stored/raw hashes; individual raw handshake/data .bin, ETF and BEAM files are unchanged. Some recipes refer to existing workspace preparations and are not a fresh-clone turnkey native CI suite. Original/provider/native package bytes are hash-referenced where already authenticated. LICENSE.local.txt remains uncommitted.
''',encoding='utf-8')
note='Part448 scoped local acceptance: genuine Erlang distribution v6 handshake/private EPMD and same-VM reconnect expose actual registered-name guest send loss (authenticated input2/output0 timeout). Four source files route named targets through existing ordinary/async output owners with source REG_SEND/REG_SEND_TT; unchanged observer passes136 rows (104paired+32aux;116exact+20ownref),8 authenticated channels/4 wrong-cookie refusals. Canonical8824 (+1group/10controls),affected587/587,retained192+72/native8,boot79 and real cleanup pass on Runtime1A059216. Sourceguards1621 (oneupdated/threeadded),default708/full118 unchanged,API/full deltas0/0. Part447 merged614d8143,merge CI39935740532 green8823/packages. Exact448 full CI/packages tracked in PR. Legacy priority+ALTACT,host sync/autoconnect,peer restart/inbound/TLS/platform/fullmodule remain separate. Evidence:docs/validation/part-448/ddll-distribution-handshake-reconnect; LICENSE.local.txt excluded.'
docs=['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md','docs/NATIVE_API_LIMITATIONS.md','docs/remaining-work/INDEX.md','docs/changelog/0079.md','docs/mfa-compatibility.json']
for name in docs[:3]:
 p=r/name;p.write_text(note+'\n\n'+p.read_text(),encoding='utf-8')
p=r/docs[3];s=p.read_text().replace('checkpoints through Part447','checkpoints through Part448');oldrow=next(x for x in s.splitlines() if x.startswith('| `erl_ddll` |'))
s=s.replace(oldrow,'| `erl_ddll` | Part448 authentic v6 distribution and actual controlled reconnect; shared guest registered-name send correction |136 scoped rows (104paired/32aux),8 verified channels/4 wrong-cookie refusals;587 affected,192+72 retained/native8/boot79,physical cleanup pass.447 merge614d8143 CI39935740532 green8823/packages;448 exact CI tracked in PR.|Physical peer restart/inbound/TLS/standard transport boot,held native teardown,wider modes and OS/ABI/thread profiles; no whole-module promotion.|')
p.write_text(s+'\nPart448 scope: [authenticated distribution and DDLL reconnect](validation/part-448/ddll-distribution-handshake-reconnect/README.md). Shared guest send correction covers legacy REG_SEND/REG_SEND_TT ordinary/async; named priority+ALTACT, public synchronous host helper transport and auto-connect remain outside this acceptance.\n',encoding='utf-8')
p=r/docs[4];s=p.read_text().replace('Published main a2d58038 includes Parts442-446; exact merge CI39930254918 passes8823/8823 and both packages. Part447 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.','Published main614d8143 includes Parts442-447; exact merge CI39935740532 passes8823/8823 and both packages. Part448 scoped local acceptance and source correction are recorded separately; exact publication-head CI is linked from its pull request.')
s=s.replace('remaining actual Erlang distribution handshake/connection-generation/reconnect paths, held native teardown interleavings','Part448 verifies authentic outbound v6 handshake,controlled same-VM reconnect,peer-retained references/current-handler stale generation fencing and corrects registered guest sends; remaining physical peer restart,inbound/TLS/full transport profiles,held native teardown interleavings')
p.write_text(s+'\nPart448 proof: [distribution handshake and reconnect](../validation/part-448/ddll-distribution-handshake-reconnect/README.md). Default708/full118 unchanged; one new canonical group, sourceguards1621.\n',encoding='utf-8')
entry='''

## Part448 — real Erlang distribution handshake, registered sends and DDLL reconnect

- Merge Part447 PR8 at exact green13c039cd into main614d8143; verify merge CI39935740532:8823/8823,all mandatory steps and both packages. Preserve separate Git publication and package-validation versus release claims.
- Boot two actual distributed OTP OS VMs with dedicated fixture cookie/private foreground EPMD and no auto-daemon fallback. Discover the actual EPMD port, perform N/status/N/r/a packet2 handshake,verify peer name/creation and challenge ACK,then use packet4. Advertise the supported mandatory/digest/small-atom/published profile without atomcache/fragments. Four actual wrong-cookie attempts are refused and confirmed by original logs.
- Preserve initial pre-VM EPMD path refusal. Independent review strengthens real peer reference retention before disconnect,source data delivery through distribution,current-handler stale generation checks,node absence,zero queues and cleanup aggregation. Actual run2 then authenticates but records input2/output0 and source worker timeout: guest remote named sends are silently dropped. Do not rewrite that failed result or claim a complete red paired raw row that was not written before its managed await failed.
- Correct four existing shared runtime files: named targets use the same ordinary/async/backpressure owners as PID/alias paths,with source REG_SEND6/REG_SEND_TT16 shape. Keep original Erlang libraries unchanged. The exact same observer/handshake source and BEAM now passes:7 outgoing/9 incoming before disconnect,4 outgoing/6 incoming after reconnect. One permanent canonical group covers five guest routes×two async modes,10 actual delivery controls; testcatalog8823→8824.
- Peer-created refs travel through actual distribution. Foreign DDLL cancellation preserves creator queue1; returned wire restores local identity and cancellation produces0. A separate ref is actually stored at peer before physical disconnect,then retrieved and returned after authentic reconnect without resending its old wire. Observe real nodedown/both absent,same remote creation/new connection generation,old snapshot refusal and current-handler stale input inertness followed by positive current input. Join actual controller/socket/native/VM/EPMD cleanup and require zero output queues.
- Accept136 rows across0/1/2/8:104 paired common-source/protocol observations plus32 auxiliary guards,116 whole exact/20 own-reference protocol rows. Keep all raw packets/ETF/full stacks and explicit reference bindings,never rewrite expected values or claim opaque bits equal. Compose passing depth8 run3 with remaining0/1/2 run4 on identical final binaries,avoiding repeat depth8. Independent review recomputes all8 challenge/ACK digests and source/input freezes.
- Publication source audit refuses assumption that current checkout HEAD516126e9 equals the pin; read exact ad058237 Git blobs and independently verify the inspected protocol/sender slices. The checkout changes are outside the reviewed handshake/sender code; flags/DDLL/reference code normalize equal. Preserve incomplete publication locally, no provider/runtime/observer/golden change or redundant test rerun.
- Final Runtime1A059216 passes587/587 affected in30 immutable shards,one new canonical group,192 fresh Part447 replay plus72 retained DDLL comparisons,8 native/HOST groups,standard79 and actual collector/persistent cleanup. Build/style/readiness pass. Broad --write . was auto-review rejected before process start; read-only check identified one file,manually formatted only that file. Initial guard updater stopped before write on unguarded helpers; exact successor updates one guard/adds three,1618→1621. Default708/full118/classifications unchanged,API/full deltas0/0; no whole-module promotion. Historical8813 readiness authority is not relabelled a new full run.
- Publish [complete proof,source red/green and boundaries](../validation/part-448/ddll-distribution-handshake-reconnect/README.md),then require exact-head8824/full CI and both package checks tracked in PR. Named priority+ALTACT,synchronous host helper,auto-connect,physical peer restart/inbound/TLS/full boot/native platform and remaining DDLL modes stay separate. Root owns shared code/build/native/VM/git/CI; keep LICENSE.local.txt uncommitted.
'''
p=r/'docs/changelog/0079.md';assert '## Part448 ' not in p.read_text();text=p.read_text()+entry;assert len(text.encode())<32768 and len(text.splitlines())<=400;p.write_text(text,encoding='utf-8')
for name in ['docs/module-readiness.json','docs/MODULE_READINESS.md','docs/supported-mfas.json','docs/supported-mfas.md','docs/supported-mfas.tsv']:
 if subprocess.run(['git','diff','--quiet','HEAD','--',name]).returncode!=0:docs.append(name)
save(r/'artifacts/library448-publication-paths.json',dict(DocumentPaths=docs,ProductPaths=src+tests,EvidenceDirectory=out.relative_to(r).as_posix(),LicenseLocalExcluded=True))
files=[dict(Path=p.relative_to(out).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(out.rglob('*')) if p.is_file()];save(out/'manifest.json',files)
print(json.dumps(dict(Passed=True,Files=len(files)+1,RawArchiveFiles=len(archive),Rows=136,Paired=104,Auxiliary=32,Affected=587,SourceGuards=1621,CanonicalCatalog=8824,ProductPaths=src+tests,DocumentPaths=docs)))
