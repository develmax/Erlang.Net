from pathlib import Path
import ast,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-451/ddll-check-pending-and-already-pending';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper();read=lambda p:json.loads((r/p).read_bytes())
base='df3cccb10a722e4ceba7e2276d608852f9a53bbd';runtime='A261E9763EEC072D08F69C0E186BC353B417FB0464F1D1F3D03F3160107835D3'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==base
for name in ['library451-style-correction1/completion.json','library451-style-readiness1/completion.json','library451-main450-green.json','library451-source-authority.json','library451-independent-analysis1/accepted-audit.json','library451-independent-analysis1/independent-audit.json','library451-independent-analysis1/reference-audit.json']:assert read('artifacts/'+name)['Passed'],name
gates=read('artifacts/library451-gates1/completion.json');assert len(gates['Stages'])==5 and all(x['Passed'] for x in gates['Stages'][:4]) and not gates['Stages'][4]['Passed']
correction=read('artifacts/library451-style-correction1/completion.json');assert correction['WholeNormalizedBytesIdentical'] and correction['NoBehaviorChange'] and correction['AfterSha256']==sha(r/'tests/Erlang.Tests/Program.cs')
assert all(sha(f)==h for f,h in correction['BinariesUnchanged'].items())
accepted=read('artifacts/library451-independent-analysis1/accepted-audit.json');independent=read('artifacts/library451-independent-analysis1/independent-audit.json')
for key,value in {'ObservationRows':424,'RawExact':368,'ProtocolOnly':56,'PairedRows':376,'AuxiliaryGuards':48,'CheckPendingTakeovers':8,'AlreadyPendingSilentRefusals':16,'FirstPendingFalseSettled':8,'FirstPendingTrueAuthenticated':8,'ActualKernelReplyTracesVerified':24,'AuthenticatedChannels':24,'CapturedKilledFamilyHandles':8}.items():assert accepted[key]==value,key
assert independent['TraceMessages']==40 and independent['AlreadyPendingReplies']==16 and independent['CheckPendingReplies']==8
for run,depths in [(2,[8]),(3,[0,1,2])]:
 root=f'artifacts/library451-probe-run{run}';finish=read(root+'/completion.json');assert finish['Passed'] and finish['InputsUnchanged'] and finish['RuntimeSha256']==runtime and len(finish['Groups'])==len(depths)*2
 assert all(g['Passed'] for g in finish['Groups']) and len(finish['ProviderScans'])==2 and all(s['Equal'] and s['Files']==6427 for s in finish['ProviderScans'])
 inputs=read(root+'/inputs-before.json');assert len(inputs)==2698 and all(sha(p)==h for p,h in inputs.items())
 for depth in depths:
  for order in ['incoming','outgoing']:
   obs=root+f'/depth{depth}-{order}/observations';body=read(obs+'/comparison.json');assert body['Passed'] and body['PhysicalCleanup'] and len(body['Results'])==53 and all(x['Passed'] for x in body['Results'])
   for name in ['check-pending-proof.json','already-pending-false-proof.json','already-pending-true-proof.json','alive-handoff-proof.json','actual-peer-restart.json']:assert read(obs+'/'+name)['Passed']
for name in ['beam_ddll_distribution451.beam','ddll_tcp451.beam','ddll451_epmd.beam']:assert sha(r/'artifacts/library451-probe-run2/observer-ebin'/name)==sha(r/'artifacts/library451-probe-run3/observer-ebin'/name)
failed=read('artifacts/library451-probe-run1/completion.json');assert not failed['Passed'] and not failed['Groups'][0]['Passed'] and failed['Groups'][1]['Passed']
assert 'FormatException' in (r/'artifacts/library451-probe-run1/compare-depth8-incoming.stderr.log').read_text()
for role in ['primary','peer']:
 e=read(f'artifacts/library451-probe-run1/depth8-incoming/{role}.exit.json');assert e['ForcedCleanup'] and e['Settled']
reg=read('artifacts/library451-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==590 and reg['ExactCoverage'] and reg['ImmutableInputs']
assert read('artifacts/library451-native-retained-managed1/observations/visibility-all.json')['Passed'] and len(read('artifacts/library451-native-retained-managed1/observations/visibility-all.json')['Results'])==8
retained=read('artifacts/library451-native-retained-managed1/observations/ddll-init-prefix435/comparison.json');assert retained['Passed'] and retained['ExpectedRows']==36 and retained['ActualNativeDrivers']
boot=read('artifacts/library451-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
focus=read('artifacts/library451-build-focus1/focus.json');assert focus['Passed']==focus['SelectedCount']==1 and focus['Failed']==0 and focus['TotalCount']==8827
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')==runtime==sha(r/'artifacts/library451-driver-package1/package/Erlang.Runtime.dll')
ledger=read('docs/mfa-compatibility.json');assert ledger==json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'])) and len(ledger['VerifiedSourceFiles'])==1621 and len(ledger['Entries'])==708
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tools','build','original']).decode().strip()
helpers=r/'artifacts/library450-freeze-evidence.py'
# These helpers themselves came from the already authenticated449 archive.
prior=r/'artifacts/library449-freeze-evidence.py';assert sha(prior)==sha(r/'docs/validation/part-449/ddll-incoming-and-peer-restart/root-recipes/library449-freeze-evidence.py')
funcs=[n for n in ast.parse(prior.read_text()).body if isinstance(n,ast.FunctionDef) and n.name in {'save','copy','tree'}];import gzip;exec(compile(ast.Module(body=funcs,type_ignores=[]),str(prior),'exec'));out.mkdir(parents=True);archive=[]
for run in [2,3]:tree(f'artifacts/library451-probe-run{run}',f'accepted-network/run{run}',{'harness','drivers'})
tree('artifacts/library451-probe-run1','failed-preparation/run1',{'harness','drivers'})
tree('artifacts/library451-probe1','failed-preparation/packet1',set())
for source,dest,skip in [
 ('artifacts/library451-probe2','observer-recipe',set()),
 ('artifacts/library451-affected-run1','affected590',set()),
 ('artifacts/library451-native-retained-managed1','native8-retained36',{'harness'}),
 ('artifacts/library451-standard79-final1','boot79',set()),
 ('artifacts/library451-style-readiness1','style-readiness',set()),
 ('artifacts/library451-style-correction1','style-correction',set()),
 ('artifacts/library451-independent-analysis1','independent-review',set()),
 ('artifacts/library451-gates1','initial-gates-style-failure',set()),
 ('artifacts/library451-build-focus1','build-focus',set()),
 ('artifacts/library451-driver-package1','fresh-native-package',{'build','package','observations'})]:tree(source,dest,skip)
copy('artifacts/library451-driver-package1/package/erldrv-host-package.json','fresh-native-package/package-manifest.json');copy('artifacts/library451-affected-selection1/selection.json','affected-selection.json')
for p in sorted((r/'artifacts').glob('library451-*.py')):copy(p.relative_to(r),'root-recipes/'+p.name)
for name in ['library451-main450-green.json','library451-source-authority.json']:copy('artifacts/'+name,name.removeprefix('library451-'))
tests=['tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/PendingCandidateHostChecks451.cs','tests/Erlang.Tests/PendingCandidateHostNames451.cs']
for p in tests:copy(p,'final-test-sources/'+Path(p).name)
save(out/'archive-map.json',archive)
save(out/'completion.json',{'Status':'SCOPED_LOCAL_ACCEPTED_EXACT_PUBLICATION_HEAD_CI_TRACKED_IN_PR','RuntimeSha256':runtime,'PackageSha256':read('artifacts/library451-driver-package1/completion.json')['PackageSha256'],'ObservationRows':424,'PairedSourceProtocolRows':376,'AuxiliaryGuards':48,'WholeTypedRawRows':368,'OwnReferenceRows':56,'CheckPendingTakeovers':8,'AlreadyPendingRefusals':16,'KernelTraceGroups':24,'KernelTraceMessages':40,'AuthenticatedChannels':24,'AffectedPassed':590,'RetainedRows':36,'NativeGroups':8,'Standard':79,'SourceGuards':1621,'ApiEntries':708,'FullContracts':118,'ApiDelta':0,'FullDelta':0,'NewCanonicalGroups':1,'CanonicalCatalog':8827,'ProductionFilesChanged':0,'WholeModuleClaim':False,'ProductionManagedNetKernelBootClaim':False,'FinalTestSourceLfOnlySupplement':True,'OriginalFailurePreserved':True})
(out/'README.md').write_text('''# Part451 — check_pending and already_pending

Pinned OTP29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`. Part450 merged maindf3cccb1, exact merge CI39960005038 green8826/8826 and both packages. The source receipt reads exact pinned blobs; newer checkout HEAD is not the oracle. Actual original execution uses the authenticated release provider, verified6427 files before/after. Exact451 publication-head CI and both packages are recorded in PR; no NuGet release implied.

## Actual original branches, explicit adapter boundary

Temporarily configure a delegating original-only epmd_module callback after normal boot. Its address_please blocks real transport selection, before outgoing NAME. The real original net_kernel inserts check_pending, owns the actual checker PID/queued caller and handles incoming. Both lexical orders emit actual sok_simultaneous, stop checker with remarked and reply true to the original connect waiter. Connection identity is preserved and a genuine authenticated stream adopts the runtime's pre-created pending handle. No direct sys_dist insertion, hot patch or fictional CheckPending enum. This is controlled discovery, not default DNS, physical scheduling equivalence or complete managed standard net_kernel boot. The original kernel/state machine and the managed runtime queue/controller primitives are distinct evidence.

On the actual established channel, hold first duplicate after salive before true/false. Capture whole up_pending source row/owner/controller/connid. A second actual NAME yields zero status bytes and physical EOF/reset, no challenge/auth/adoption; complete original row and whole managed snapshot stay unchanged. Capture actual kernel send replies to distinct first/second owners: internal up_pending then already_pending. Do not invent salready_pending or snok. First false closes and restores old connection; first valid true survives refusal and authenticates, retires/joins old controller before HOST ACK read, preserves same peer creation and genuine peer-held DDLL references. Old physical common-worker traffic continues while candidates are held and after refusal.

Independent audits parse24 trace groups containing40 actual messages:8ok_pending,16up_pending,16already_pending, with real recipient roles. Original row/checker/waiter/source raw captures are own-VM observations, not cross-VM byte equality. The code restores discovery configuration on success; no alternate-config failure cleanup or default resolver claim is added.

## Results and lifecycle

Immutable packet2 passes both depth8 orders in run2 and all six0/1/2 orders in run3:424/424 observations=376 paired common-source/protocol+48 auxiliary controls;368 complete typed/raw matches+56 explicit own-reference protocols. Same final RuntimeA261E976/native package1FE86D4E, all three BEAMs identical,2698 bindings per batch unchanged. These are not424 complete API contracts or canonical tests.

Eight actual check_pending takeovers and16 zero-status extras pass, with8 first-false/8 first-true completions and real data/DDLL evidence. Retain actual before/after original rows, status/challenge/reply/ACK/data bytes, full error/frame profiles0/1/2/8 and reference-role/raw identity.24 authenticated runtime channels drain bytes/packets/segments to0 and join. Eight deliberate original peer-tree exits match8 actually captured OS handles, each joined; no unseen child count is fabricated.16 normal oracle/replacement VM exits and2 normal EPMDs pass. Native PhysicalExit/Cleanup/Completion and zero leases pass. This is a captured process-family snapshot, not Job containment or an unload after death.

One permanent HOST regression exercises two actual controller actors concurrently adopting one pending generation, one winner, losing actor disposal, connected abort inertness and real queued delivery/drain. Final590/590 affected retains589 prior cases; native8/DDLL36,boot79 and actual collector/persistent cleanup pass. Default708/full118/source1621 remain unchanged, deltas0/0; catalog8826→8827.

## Preserved failures and final source

Initial run1 incoming fails before guest/runtime startup: existing readiness file was still empty, int.Parse receives empty port. Two owned originals require forced failure cleanup, which is NOT counted as normal accepted cleanup. Outgoing run1 passes independently but is not substituted for final packet2 acceptance. Packet2 waits for actual numeric port in the same existing deadline in initial/restart readers; semantic observer/common BEAM/handshake expectations are unchanged. Raw failed packet/run are retained. Passive aggregator's old450 BEAM filename was corrected without rerunning VMs.

Initial five-stage gate has four functional passes and style-only failure: registration script wrote Program.cs CRLF while existing source is LF. Final correction restores LF, verifies whole normalized source bytes identical and test/runtime DLLs unchanged, then style passes separately. Functional snapshots retain their original CRLF source binding; final-test-sources and explicit style-correction receipt bind the LF supplement. No semantic functional rerun is needed for endings; exact-head CI rebuilds and tests final sources. Nothing rewrites the failed gate as an uninterrupted pass.

## Remaining work

Late checker results/timers, additional setup/timeout/replacement races, malformed/legacy/dynamic negotiation,TLS,full managed original net_kernel standard transport/autoconnect/ALTACT, wider DDLL/native/platform/ABI/thread contracts. No whole-module or performance promotion; P0/P1/future architecture queue is unchanged. Recipes retain workspace authenticated helpers and are not a fresh-clone turnkey native CI guarantee. Complete raw ETF/protocol/source retained; large JSON may be losslessly gzip-compressed with stored/raw SHA manifests. LICENSE.local.txt is excluded.
''',encoding='utf-8')
manifest=[{'Path':p.relative_to(out).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(out.rglob('*')) if p.is_file()];save(out/'manifest.json',manifest)
docs=['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/DECISIONS.md','docs/NATIVE_API_LIMITATIONS.md','docs/OTP-PORTS.md','docs/dependency-decisions.md','docs/changelog/0079.md','docs/remaining-work/INDEX.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
save(r/'artifacts/library451-publication-paths.json',{'EvidenceDirectory':out.relative_to(r).as_posix(),'DocumentPaths':docs,'ProductPaths':tests})
print(json.dumps({'Passed':True,'ProofFiles':len(manifest)+1,'ArchiveFiles':len(archive),'Runtime':runtime,'ProductionChanges':0}))
