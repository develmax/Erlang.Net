from pathlib import Path
import gzip,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-449/ddll-incoming-and-peer-restart';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
read=lambda p:json.loads((r/p).read_bytes())
runtime='8309B8972A37F0967D6BF7835C7F5D6B2DB00FC697B0C05BE447D137DE678756'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='029f740cfba3b27dd9890ff312250ad2284686e8'
assert read('artifacts/library449-gates1/completion.json')['Passed'] and read('artifacts/library449-style-readiness1/completion.json')['Passed']
assert read('artifacts/library449-main448-green.json')['Passed'] and read('artifacts/library449-source-authority.json')['Passed']
top=read('artifacts/library449-probe-run8/completion.json');assert top['Passed'] and top['InputsUnchanged'] and top['RuntimeSha256']==runtime and len(top['Groups'])==4 and all(x['Passed'] for x in top['Groups'])
assert len(top['ProviderScans'])==2 and all(x['Equal'] and x['Files']==6427 for x in top['ProviderScans'])
epmd=read('artifacts/library449-probe-run8/epmd.exit.json');assert epmd['Settled'] and epmd['ExitCode']==0 and not epmd['ForcedTermination']
audit=read('artifacts/library449-independent-analysis1/accepted-final-audit.json');assert audit['Passed'] and audit['ObservationRows']==164 and audit['RawExact']==144 and audit['ProtocolOnly']==20 and audit['PairedRows']==140 and audit['AuxiliaryGuards']==24
assert audit['AuthenticatedIncomingChannels']==8 and audit['WrongCookieRefusals']==4 and audit['OriginalOsNodes']==12 and audit['PrivateEpmdProcesses']==1
rows=[]
for depth in [0,1,2,8]:
 p=f'artifacts/library449-probe-run8/depth{depth}';rep=read(p+'/observations/comparison.json');assert rep['Passed'] and rep['PhysicalCleanup'] and len(rep['Results'])==41 and not rep['Failures'];rows+=rep['Results']
 restart=read(p+'/observations/actual-peer-restart.json');assert restart['Passed'] and restart['OldPid']!=restart['NewPid'] and restart['OldCreation']!=restart['NewCreation'] and restart['OldConnection']!=restart['NewConnection'] and restart['OldReferenceWireUnchanged']
 for f in ['primary.exit.json','peer.exit.json','restarted-peer/exit.json']:
  x=read(p+'/'+f);assert x['ExitCode']==0 and x['Settled'] and not x['ForcedTermination']
  if f.startswith('restarted'):assert x['PumpsJoined']
 reg=read(p+'/observations/listener/registration.json');assert reg['RegistrationSocketKeptOpen']
 for n in [1,2]:
  c=p+f'/observations/connection{n}';a=read(c+'/authentication.json');z=read(c+'/cleanup.json');assert a['Authenticated'] and a['Incoming'] and a['LocalCreation']==reg['Creation'] and a['ChallengeReplyVerified'] and a['AckSent'] and a['Packet2Then4']
  assert z['Passed'] and z['ControllerJoined'] and z['SocketDisposed'] and z['Incoming']>0 and z['Outgoing']>0
  assert z['FinalSnapshot']['QueuedPacketBytes']==z['FinalSnapshot']['QueuedPackets']==z['FinalSnapshot']['QueuedSegments']==0
 wrong=read(p+'/observations/wrong-cookie/authentication.json');assert wrong['ExpectedWrongCookieRejected'] and not wrong['Adopted']
 stale=read(p+'/observations/old-handle-current-input-handler.json');assert stale['Passed'] and stale['OneControllerTurn'] and stale['OldPayloadInert'] and stale['CurrentActualInputHandler']
assert len(rows)==164 and all(x['Passed'] for x in rows) and sum(x['WholeRawEqual'] and x['WholeTypedEqual'] for x in rows)==144
reg=read('artifacts/library449-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==588 and reg['ExactCoverage'] and reg['ImmutableInputs']
native=read('artifacts/library449-native-retained-managed1/observations/visibility-all.json');assert native['Passed'] and len(native['Results'])==8
retained=read('artifacts/library449-native-retained-managed1/observations/ddll-init-prefix435/comparison.json');assert retained['Passed'] and retained['ExpectedRows']==36 and retained['ActualNativeDrivers']
boot=read('artifacts/library449-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
focus=read('artifacts/library449-build-focus4/focus.json');assert focus['Passed']==focus['SelectedCount']==1 and focus['Failed']==0 and focus['TotalCount']==8825
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')==runtime==sha(r/'artifacts/library449-driver-package1/package/Erlang.Runtime.dll')
ledger=read('docs/mfa-compatibility.json');old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert ledger==old and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1621
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tools']).decode().strip()
out.mkdir(parents=True);archive=[]
def save(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
def copy(source,dest):
 source=r/source;assert 'LICENSE.local' not in source.name;destination=out/dest;destination.parent.mkdir(parents=True,exist_ok=True);raw=source.read_bytes();packed=source.suffix=='.json' and len(raw)>65536
 if packed:destination=destination.with_name(destination.name+'.gz');destination.write_bytes(gzip.compress(raw,mtime=0));assert gzip.decompress(destination.read_bytes())==raw
 else:destination.write_bytes(raw)
 archive.append({'Path':destination.relative_to(out).as_posix(),'OriginalPath':source.relative_to(r).as_posix(),'Bytes':destination.stat().st_size,'Sha256':sha(destination),'RawBytes':len(raw),'RawSha256':hashlib.sha256(raw).hexdigest().upper(),'Gzip':packed})
def tree(base,dest,skip=()):
 for p in sorted((r/base).rglob('*')):
  rel=p.relative_to(r/base)
  if p.is_file() and not set(rel.parts).intersection(skip) and p.suffix in {'.json','.log','.etf','.erl','.cs','.py','.md','.txt','.beam','.bin','.csproj'}:copy(p.relative_to(r),Path(dest)/rel)
for n in [1,2,3,4,5]:tree(f'artifacts/library449-probe-run{n}',f'preparation-history/run{n}',{'harness','drivers'})
tree('artifacts/library449-probe-run8','final-incoming-restart164',{'harness','drivers'})
for n in [6,7]:
 for name in ['completion.json','provider.json','inputs-before.json','groups.json','epmd.exit.json']:copy(f'artifacts/library449-probe-run{n}/'+name,f'preliminary-old-binary/run{n}/'+name)
for n in range(1,8):tree(f'artifacts/library449-probe{n}',f'observer-recipes/probe{n}')
for base,dest,skip in [
 ('artifacts/library449-affected-run1','affected588',set()),
 ('artifacts/library449-native-retained-managed1','native8-retained36',{'harness'}),
 ('artifacts/library449-standard79-final1','boot79',set()),
 ('artifacts/library449-style-readiness1','style-readiness',set()),
 ('artifacts/library449-independent-analysis1','independent-review',set()),
 ('artifacts/library449-format-owned-tests1','format-owned-tests',set()),
 ('artifacts/library449-gates1','gates',set()),
 ('artifacts/library449-driver-package1','fresh-native-package',{'build','package','observations'})]:tree(base,dest,skip)
for n in range(1,5):tree(f'artifacts/library449-build-focus{n}',f'build-focus-history/{n}')
copy('artifacts/library449-driver-package1/package/erldrv-host-package.json','fresh-native-package/package-manifest.json')
copy('artifacts/library449-affected-selection1/selection.json','affected-selection.json')
for p in sorted((r/'artifacts').glob('library449-*.py')):copy(p.relative_to(r),'root-recipes/'+p.name)
for name in ['library449-main448-green.json','library449-source-authority.json']:copy('artifacts/'+name,name.removeprefix('library449-'))
tests=['tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/ConnectedRemoteEpochHostChecks449.cs','tests/Erlang.Tests/ConnectedRemoteEpochHostNames449.cs']
for p in tests:copy(p,'final-test-sources/'+Path(p).name)
save(out/'archive-map.json',archive)
save(out/'completion.json',{'Status':'SCOPED_LOCAL_ACCEPTED_EXACT_PUBLICATION_HEAD_CI_TRACKED_IN_PR','RuntimeSha256':runtime,'PackageSha256':read('artifacts/library449-driver-package1/completion.json')['PackageSha256'],'OriginalObservationRows':164,'PairedSourceProtocolRows':140,'AuxiliaryGuards':24,'WholeTypedRawRows':144,'OwnReferenceRows':20,'IncomingAuthenticatedChannels':8,'WrongCookieRefusals':4,'ActualRemoteVmRestarts':4,'OriginalOsNodes':12,'AffectedPassed':588,'RetainedRows':36,'NativeGroups':8,'Standard':79,'NewCanonicalGroups':1,'CanonicalCatalog':8825,'SourceGuards':1621,'ApiEntries':708,'FullContracts':118,'ApiDelta':0,'FullDelta':0,'ProductionFilesChanged':0,'WholeModuleClaim':False,'ForcedPeerDeathClaim':False,'StandardManagedTransportBootClaim':False})
(out/'README.md').write_text('''# Part449 — incoming distribution handshake and actual peer VM restart

Baseline: OTP29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`. Part448 is merged as main `029f740c`; exact merge CI39945517467 passes8824/8824, all mandatory steps and both package builds. Publication is Git/CI package validation, not a NuGet release. Source authority reads exact pinned Git blobs: checkout HEAD is newer, and the actual oracle is the unchanged admitted original release provider.

## Actual incoming transport

The test-only BCL listener registers its real port in private foreground EPMD, retains the ALIVE2 socket and reads the actual ALIVE2_X creation. Public managed setnode and server challenge use exactly that creation. The original VM initiates its real net_kernel:connect_node, performs EPMD lookup and connects to this accepting socket. It is not a renamed outbound448 exchange or a synthetic net_adm pong: the minimal managed kernel does not implement the latter is_auth service.

The accepting host reads modern static-name NAME, checks exact node/length/mandatory capabilities, sends fresh-status sok and CHALLENGE, verifies the genuine original client's cookie digest, sends ACK, then adopts a genuine runtime controller. Only then do packet4 pumps feed actual runtime input/output queues. Four wrong synthetic fixture-cookie attempts are refused without adoption; subsequent successful connection/data exchange proves the same listener remains usable. The explicit fixture cookie is nonsecret test data. Actual raw NAME/status/challenge/reply/ACK/data packets are retained; the independent audit recomputes all eight successful reply/ACK digests and four negative digests.

Original-compiled common worker calls/replies cross real distribution; direct runtime.Send is used only for final local worker stop, never data-plane delivery. Auxiliary raw control configures original fixtures and connection setup, observes identity and requests orderly shutdown. This is explicit evidence infrastructure for runtime queues, not a new production standard net_kernel/TCP/EPMD service.

## Actual remote OS VM restart and DDLL

Every profile starts an oracle and peer original OS VM. The surviving local worker creates a genuine queued DDLL monitor, sends its reference to peer and captures a genuine peer-owned reference. Source monitor_node observes peer death. Stop joins the peer's actual worker, exits the OS VM normally, produces nodedown/absence, closes the channel and leaves local DDLL queue1. A pre-stop OS process handle proves death before replacement; the actual Python parent independently requires exit0/unforced. Windows non-parent .NET ExitCode is deliberately not used as authority.

The harness launches the same original command/provider as a new owned process with the same node name. Both actual OS PID and observed NAME creation change; the new incoming connection receives a new runtime connection generation. No guessed creation increment or snapshot rewrite is used. Old peer dictionary state is actually undefined after restart. The surviving local reference is explicitly resent through the new VM, re-encoded, returned to its surviving owner, cancels its real DDLL monitor and changes queue1 to0; no in-memory peer retention across OS death is claimed.

The old peer-created reference preserves complete node/creation/word bytes in surviving source actors after active connection metadata moves to new creation. The replacement rejects this old reference with full error/badarg frames while its genuine new monitor remains queued1. A new peer reference routed out and back cancels that row to0; a second cancellation on the shared original owner is an idempotent true, not another independent removal. Raw complete errors match in profiles0/1/2/8 with actual frame counts1/1/2/3.

Old snapshots fail before/after replacement. The actual new input-handler actor feeds a genuine old packet through the old handle. Before/stale/after execute within one controller turn, so new global traffic cannot race the observer. Accepted-input/queue counts stay unchanged; subsequent current real input succeeds and increments accepted input. Each accepted channel drains queued bytes/packets/segments to0 and joins its real controller/socket. Twelve actual original OS process receipts (four oracle, four old peer, four replacement), one private EPMD and all native children/leases clean up; replacement stdout/stderr pumps join.

## Accepted evidence and regression

Final acceptance is one fresh four-profile batch on Runtime8309B897 with fresh actual native package353C421D: **164/164 observations =140 paired common-source/protocol +24 auxiliary guards**,144 whole typed/raw exact and20 complete own-reference protocol comparisons. Own-reference bindings come from actual corresponding wire outputs; all other fields and complete frames remain unchanged. These rows are not164 full API contracts or164 canonical tests.

The independent final audit rehashes all2689 bound inputs, original6427-file inventory before/after, actual handshake directions/digests, ALIVE2/local identity, old/new peer identity/ref values and physical cleanup. Earlier composed164 old-binary observations are preliminary only. The final rebuild changes assembly provenance/hash without product source changes; unchanged observer sources and BEAM are rerun against final binaries. No runtime algorithm discrepancy was confirmed; no pure Erlang library or runtime production file is changed by449.

One permanent managed HOST group guards real queued old/new reference delivery, stale-generation rejection/inertness and immutable old reference bytes. It models remote epoch replacement using independent managed endpoints; it is explicitly distinct from real OS/original handshake evidence. Catalog8824 becomes8825. Final build/focus,588/588 affected in30 immutable blocks, retained DDLL36,8 native/HOST groups,standard79 and collector/persistent cleanup pass. Source layout is clean; only two exact owned test copies were eligible for isolated formatting and neither changed. Source guards1621,default708/full118 and classifications stay unchanged, deltas0/0. Readiness is regenerated with a new scoped Runtime milestone and an explicitly historical8813 input authority; separately verified main448 full8824 is not relabelled a current8825 pass. Exact publication-head full CI and both packages are tracked in the PR.

## Preserved preparation and limits

Five failed initial harness preparations/observer runs remain: unapplied Program rewrite/nonexistent Connect, extra parenthesis, unsupported common-worker net_adm import, non-parent ExitCode observation and concurrent stale measurement. Original-only bootstrap is moved to its auxiliary server, independent .NET compile precedes expensive original preparation, old exit0 remains a parent fence, and the stale check keeps its zero-input criterion in one actual controller turn. The first full provider scan took82seconds cold while warm scans take roughly1–2seconds; this is cache timing, not a proven algorithm speedup. New canonical test history retains duplicate Core registration and reserved creation3 expectation failures; use existing registration guard and valid creation7. No production bug is inferred from these test mistakes, and no failed result is overwritten.

This closes scoped fresh incoming v6 and graceful actual peer OS restart on admitted Windows/x64. Simultaneous/alive handoff, forced remote death, malformed/legacy/dynamic-name negotiation, TLS, full standard managed transport boot, autoconnect, priority ALTACT, wider DDLL modes/interleavings and additional OS/ABI/native-thread profiles remain separate. No whole-module compatibility or performance claim follows. Historical raw packet/source receipts are losslessly archived with stored/raw hashes; some recipes refer to existing authenticated workspace preparations and are not fresh-clone turnkey native CI. The owner license draft remains uncommitted and excluded.
''',encoding='utf-8')
manifest=[{'Path':p.relative_to(out).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(out.rglob('*')) if p.is_file()];save(out/'manifest.json',manifest)
docs=['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/DECISIONS.md','docs/NATIVE_API_LIMITATIONS.md','docs/OTP-PORTS.md','docs/dependency-decisions.md','docs/changelog/0079.md','docs/remaining-work/INDEX.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
save(r/'artifacts/library449-publication-paths.json',{'EvidenceDirectory':out.relative_to(r).as_posix(),'DocumentPaths':docs,'ProductPaths':tests})
print(json.dumps({'Passed':True,'ProofFiles':len(manifest)+1,'ArchiveFiles':len(archive),'Runtime':runtime,'ProductionChanges':0}))
