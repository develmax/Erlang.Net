from pathlib import Path
import ast,gzip,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-450/ddll-simultaneous-alive-and-fatal-peer';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper();read=lambda p:json.loads((r/p).read_bytes())
runtime='6B102A987BFCE92CB494E608A190F947E2D4DEB978F2851D289AEFE5B513AE0C';base='7e4a324d3d7af67eb565ee4959cc597bc8ff5c93'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==base
assert read('artifacts/library450-gates1/completion.json')['Passed'] and read('artifacts/library450-style-readiness1/completion.json')['Passed']
assert read('artifacts/library450-main449-green.json')['Passed'] and read('artifacts/library450-source-authority.json')['Passed'] and read('artifacts/library450-plan-supplement-final.json')['Passed']
audit=read('artifacts/library450-independent-analysis1/accepted-audit.json')
for key,value in {'Passed':True,'ObservationRows':400,'RawExact':344,'ProtocolOnly':56,'PairedRows':352,'AuxiliaryGuards':48,'AuthenticatedChannels':24,'AliveFalseRefusals':8,'AliveWrongCookieRefusals':8,'ExpectedFatalOldVmExits':8,'NormalOracleReplacementExits':16,'CapturedKilledFamilyHandles':16,'PrivateEpmdProcesses':2}.items():assert audit[key]==value,key
rows=[];channels=0;families=0
for run,depths in [(1,[8]),(2,[0,1,2])]:
 top=read(f'artifacts/library450-probe-run{run}/completion.json');assert top['Passed'] and top['InputsUnchanged'] and top['RuntimeSha256']==runtime and len(top['Groups'])==len(depths)*2 and all(x['Passed'] for x in top['Groups'])
 assert len(top['ProviderScans'])==2 and all(x['Equal'] and x['Files']==6427 for x in top['ProviderScans'])
 epmd=read(f'artifacts/library450-probe-run{run}/epmd.exit.json');assert epmd['Settled'] and epmd['ExitCode']==0 and not epmd['ForcedTermination']
 for depth in depths:
  for order in ['incoming','outgoing']:
   p=f'artifacts/library450-probe-run{run}/depth{depth}-{order}';rep=read(p+'/observations/comparison.json');assert rep['Passed'] and rep['PhysicalCleanup'] and len(rep['Results'])==50 and not rep['Failures'];rows+=rep['Results']
   selected=read(p+'/observations/simultaneous-selection.json');incoming=order=='incoming';assert selected['Passed'] and selected['IncomingSelected']==incoming and selected['OriginalStatus']==('nok' if incoming else 'ok_simultaneous') and selected['ActualOriginalConnect'] and not selected['LoserAdopted']
   for directory in [('simultaneous-incoming' if incoming else 'simultaneous-outgoing'),'alive-handoff','connection2']:
    close=read(p+'/observations/'+directory+'/cleanup.json');snap=close['FinalSnapshot'];assert close['Passed'] and close['Authenticated'] and close['Incoming']>0 and close['Outgoing']>0 and close['ControllerJoined'] and close['SocketDisposed'];assert snap['QueuedPacketBytes']==snap['QueuedPackets']==snap['QueuedSegments']==0;channels+=1
   handoff=read(p+'/observations/alive-handoff-proof.json');assert handoff['Passed'] and handoff['OldControllerJoined'] and handoff['RetiredBeforeHostAckRead'] and handoff['SamePeerVmCreation'] and handoff['Old']['RemoteCreation']==handoff['Current']['RemoteCreation'] and handoff['Old']['ConnectionId']!=handoff['Current']['ConnectionId']
   for directory,key in [('alive-false','OriginalClosed'),('alive-wrong-cookie','WrongCookieRejected')]:
    refusal=read(p+'/observations/'+directory+'/candidate-refusal.json');assert refusal['OriginalStatus']=='alive' and refusal[key] and not refusal['Adopted']
   fatal=read(p+'/observations/fatal-peer-stop.json');exit=read(p+'/peer.exit.json');assert fatal['Passed'] and fatal['KillRequested'] and fatal['EntireOwnedTree'] and fatal['OldPid']==exit['Pid'] and exit['ExpectedScenarioTermination'] and not exit['ForcedCleanup'] and exit['ExitCode']!=0 and exit['Settled'];assert len(fatal['Family'])>=1 and all(x['Settled'] and x['StartedUtc'] for x in fatal['Family']);families+=len(fatal['Family'])
   for name in ['primary.exit.json','restarted-peer/exit.json']:
    e=read(p+'/'+name);assert e['Settled'] and e['ExitCode']==0 and not e['ForcedTermination']
    if name.startswith('restarted'):assert e['PumpsJoined']
   restart=read(p+'/observations/actual-peer-restart.json');assert restart['Passed'] and restart['OldPid']!=restart['NewPid'] and restart['OldCreation']!=restart['NewCreation'] and restart['OldConnection']!=restart['NewConnection'] and restart['OldReferenceWireUnchanged']
   for name in ['alive-old-handler.json','old-handle-current-input-handler.json']:
    stale=read(p+'/observations/'+name);assert stale['Passed'] and stale['OneControllerTurn'] and stale['OldPayloadInert'] and stale['CurrentActualInputHandler']
assert len(rows)==400 and all(x['Passed'] for x in rows) and sum(x['WholeRawEqual'] and x['WholeTypedEqual'] for x in rows)==344 and channels==24 and families==16
for name in ['beam_ddll_distribution450.beam','ddll_tcp450.beam']:assert sha(r/'artifacts/library450-probe-run1/observer-ebin'/name)==sha(r/'artifacts/library450-probe-run2/observer-ebin'/name)
reg=read('artifacts/library450-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==589 and reg['ExactCoverage'] and reg['ImmutableInputs']
assert read('artifacts/library450-native-retained-managed1/observations/visibility-all.json')['Passed'] and len(read('artifacts/library450-native-retained-managed1/observations/visibility-all.json')['Results'])==8
retained=read('artifacts/library450-native-retained-managed1/observations/ddll-init-prefix435/comparison.json');assert retained['Passed'] and retained['ExpectedRows']==36 and retained['ActualNativeDrivers']
boot=read('artifacts/library450-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
focus=read('artifacts/library450-build-focus1/focus.json');assert focus['Passed']==focus['SelectedCount']==1 and focus['Failed']==0 and focus['TotalCount']==8826
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')==runtime==sha(r/'artifacts/library450-driver-package1/package/Erlang.Runtime.dll')
ledger=read('docs/mfa-compatibility.json');old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert ledger==old and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1621
for x in ledger['VerifiedSourceFiles']:assert sha(r/x['Path'])==x['Sha256'],x['Path']
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tools','build','original']).decode().strip()
helpers=r/'artifacts/library449-freeze-evidence.py';assert sha(helpers)==sha(r/'docs/validation/part-449/ddll-incoming-and-peer-restart/root-recipes/library449-freeze-evidence.py')
funcs=[n for n in ast.parse(helpers.read_text()).body if isinstance(n,ast.FunctionDef) and n.name in {'save','copy','tree'}];exec(compile(ast.Module(body=funcs,type_ignores=[]),str(helpers),'exec'));out.mkdir(parents=True);archive=[]
for run in [1,2]:tree(f'artifacts/library450-probe-run{run}',f'accepted-network/run{run}',{'harness','drivers'})
for source,dest,skip in [
 ('artifacts/library450-probe1','observer-recipe',set()),
 ('artifacts/library450-affected-run1','affected589',set()),
 ('artifacts/library450-native-retained-managed1','native8-retained36',{'harness'}),
 ('artifacts/library450-standard79-final1','boot79',set()),
 ('artifacts/library450-style-readiness1','style-readiness',set()),
 ('artifacts/library450-independent-analysis1','independent-review',set()),
 ('artifacts/library450-gates1','gates',set()),
 ('artifacts/library450-build-focus1','build-focus',set()),
 ('artifacts/library450-driver-package1','fresh-native-package',{'build','package','observations'})]:tree(source,dest,skip)
copy('artifacts/library450-driver-package1/package/erldrv-host-package.json','fresh-native-package/package-manifest.json');copy('artifacts/library450-affected-selection1/selection.json','affected-selection.json')
for p in sorted((r/'artifacts').glob('library450-*.py')):copy(p.relative_to(r),'root-recipes/'+p.name)
for name in ['library450-main449-green.json','library450-source-authority.json','library450-plan-supplement.json','library450-plan-supplement-final.json']:copy('artifacts/'+name,name.removeprefix('library450-'))
tests=['tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/ConnectedCandidateHostChecks450.cs','tests/Erlang.Tests/ConnectedCandidateHostNames450.cs']
for p in tests:copy(p,'final-test-sources/'+Path(p).name)
save(out/'archive-map.json',archive)
save(out/'completion.json',{'Status':'SCOPED_LOCAL_ACCEPTED_EXACT_PUBLICATION_HEAD_CI_TRACKED_IN_PR','RuntimeSha256':runtime,'PackageSha256':read('artifacts/library450-driver-package1/completion.json')['PackageSha256'],'ObservationRows':400,'PairedSourceProtocolRows':352,'AuxiliaryGuards':48,'WholeTypedRawRows':344,'OwnReferenceRows':56,'AuthenticatedChannels':24,'AliveFalseRefusals':8,'AliveWrongCookieRefusals':8,'ExpectedFatalPeerExits':8,'KilledCapturedFamilyHandles':16,'NormalOracleReplacementExits':16,'PrivateEpmdProcesses':2,'AffectedPassed':589,'RetainedRows':36,'NativeGroups':8,'Standard':79,'NewCanonicalGroups':1,'CanonicalCatalog':8826,'SourceGuards':1621,'ApiEntries':708,'FullContracts':118,'ApiDelta':0,'FullDelta':0,'ProductionFilesChanged':0,'WholeModuleClaim':False,'ProductionManagedNetKernelBootClaim':False,'JobContainmentClaim':False,'PlanSupplementOnly':True,'KnownDifferencesNewlyResolved':0})
(out/'README.md').write_text('''# Part450 — simultaneous/alive handoff and deliberate fatal peer

Pinned OTP29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`. Part449 is merged as main7e4a324d; exact merge CI39951441544 passes8825/8825, all mandatory steps and both package builds. Git publication/CI package builds do not imply a NuGet release. Source authority reads exact pinned Git blobs; newer checkout HEAD is not the oracle. The unchanged admitted original release provider is verified before/after each batch.

## Genuine pending collisions

A real original VM initiates net_kernel:connect_node to the managed listener registered in private EPMD. Hold its actual NAME before returning status. Only then initiate the competing socket to the original VM, so its genuine net_kernel is already pending. Both ASCII lexical orders are measured: higher original name returns nok and keeps its outgoing setup, lower original name kills its outgoing setup and returns ok_simultaneous for incoming. The host validates these actual statuses against the expected lexical choice, retires the losing socket without adoption and authenticates the selected direction. An actual runtime pending handle is created before the race and must be adopted unchanged by the winner.

This is an explicit source-guided evidence adapter plus genuine original net_kernel arbitration and production runtime queue/controller primitives. It is not a new production C# net_kernel implementation or full managed standard network boot. Shared original Erlang libraries are unchanged.

## Alive false, invalid cookie and successful handoff

On an actual connected original peer, a second same-name candidate receives alive. Reply false: the original closes the candidate without challenge/adoption. Reply true with wrong nonsecret fixture-cookie digest: the original rejects it after challenge without adopting it or destroying the old channel. After both negatives, a fresh common-worker request/reply travels through the actual old physical socket and runtime generation remains unchanged. Post-negative original sys_dist rows were not retained, so no separate exact original controller-row equality is claimed.

Reply true with valid digest: actual original wait_pending retires its old controller. The host observes the old physical socket close, disposes/joins the actual old runtime controller and then reads/verifies ACK and adopts the replacement. This proves retirement before HOST ACK READ/adoption, not wire-packet arrival ordering. Whole original sys_dist rows before/after successful replacement retain actual changed controller and connection-id values. Same peer OS VM creation stays unchanged while runtime connection generation changes. A genuine peer-held local DDLL reference survives same-VM handoff, returns byte-exact to its creator and changes queue1 to0. Source peer memory is genuinely retained here.

Old handle input is exercised by the actual new input-handler actor in one controller turn; accepted/queue counts stay unchanged and later real current data succeeds. All positive handshakes preserve complete raw NAME/status/challenge/reply/ACK/data bytes. Independent review recomputes24 positive reply/ACK digest pairs,8 wrong-cookie digests and8 false-alive refusals.

## Deliberate owned peer-tree kill and restart

The original peer is an actual live VM with loaded echo driver/port and monitor state. Reuse the repository-owned Halt437 Toolhelp layout to snapshot only its owned descendant family, retain actual OS handles/start times, call Process.Kill(entireProcessTree:true), and join every captured process. Sixteen captured handles across eight kills settle. This is a process-family snapshot, not a Windows Job or a guarantee of future-child containment. Actual original VM process is erl.exe; captured inet_gethost descendants are also joined.

The actual Python creator requires the designated old VM nonzero exit and matching fatal operation receipt. `ExpectedScenarioTermination=true`, `ForcedCleanup=false` distinguish this requested kill from accidental cleanup forcing. Legacy `ForcedTermination` in the parent receipt denotes cleanup fallback only; it remains false for the already-completed deliberate scenario operation. Do not call those eight exits normal, waive other failures or fabricate an unload after VM death.

Original oracle and runtime source actors observe nodedown/absence; local DDLL queue1 and immutable old external reference bytes survive. A real new owned original VM starts with the same name and different PID/creation; it authenticates incoming and gets a new connection generation. Peer memory is correctly lost after OS death. Surviving local refs are explicitly resent, returned and cancel their own queue to0. The replacement refuses an old-peer ref while its genuine fresh monitor remains queued1, then fresh reference return/cancellation removes that row to0. The second cancellation on the shared original owner is idempotent true, not an independent row removal. Whole errors/frames and explicit own-reference bindings remain unchanged across0/1/2/8.

## Acceptance and regression

One immutable observer packet passes both depth8 orders in run1, then the six remaining profiles in run2 without repeating depth8. Both batches use final Runtime6B102A98 and actual package863E7814, identical observer/BEAM and final source: **400/400 observations =352 paired common-source/protocol +48 auxiliary guards**,344 whole typed/raw and56 own-reference protocols. These are not400 complete API contracts or400 new canonical tests.

All2695 input bindings per batch and6427 original-provider files before/after are verified. Independent audit confirms actual statuses, positive/negative cookie digests, valid original controller/connid replacement, same-VM memory versus fatal memory loss, frozen old/new refs, sixteen normal oracle/replacement OS exits, eight designated fatal old exits and two normal private EPMDs. All24 accepted runtime channels drain queued bytes/packets/segments to0 and join. All eight native fixtures enforce real PhysicalExit/PhysicalCleanup/Completion and zero leases.

One new permanent managed HOST group validates losing-candidate refusal, connected AbortPending inertness, full live snapshot/output preservation, actual queued message delivery and old lease/controller retirement leaving the new generation intact. This managed test is distinct from real original protocol evidence. It passes first build; catalog8825→8826. Final589/589 affected in30 immutable blocks,retained DDLL36/native8,standard79 and actual collector/persistent cleanup pass. Style/source-guard/readiness pass. Default708/full118/sourceguards1621 are unchanged, API/full deltas0/0. Historical8813 readiness authority is explicit; separately verified main449 full8825 is not relabelled current full8826. Exact publication-head CI/packages are tracked in PR.

Preparation used one packet and two nonoverlapping passing batches, compared with449's five failed observer/preparation runs and later final-binary rebind. Final binaries/package are built first, avoiding that rebind cycle. Cold85.672s versus warm~1–2s inventory scans are cache timing, not algorithm acceleration. No runtime algorithm discrepancy was confirmed, no failed scenario is hidden and no forced product fix or expected-value weakening is introduced.

## Planned owner supplement and remaining scope

At the completed450 safe boundary, documentation only records P0 current revalidation/fixes for the31 named Known differences, then P1 full agreed library API coverage including559 pending and relevant exports outside default708. A1 tracked pinned source/image closure, A2 C# libraries in this repository and A3 incremental subsystem layout are planned prerequisites/workstreams, not implementation credit. Existing selected snapshots/ErlangLibrary/backend selection are acknowledged; four original/ fullsweep fixtures are not a canonical library snapshot. No new suites, project moves or compatibility reclassification follow from the plan supplement. See the linked current WORK_PLAN and remaining-work queue.

Remaining protocol scope includes check_pending/already_pending and broader setup/replacement races, malformed/legacy/dynamic negotiation, TLS/full managed standard net_kernel boot/autoconnect/priority ALTACT and wider DDLL/native/platform profiles. No whole-module, physical ERTS layout or performance claim follows. Archives retain complete raw ETF/protocol/source with stored/raw hashes; some recipes reference existing authenticated workspace helpers and are not fresh-clone turnkey native CI. LICENSE.local.txt is excluded.
''',encoding='utf-8')
manifest=[{'Path':p.relative_to(out).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(out.rglob('*')) if p.is_file()];save(out/'manifest.json',manifest)
docs=['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/DECISIONS.md','docs/NATIVE_API_LIMITATIONS.md','docs/OTP-PORTS.md','docs/dependency-decisions.md','docs/changelog/0079.md','docs/remaining-work/INDEX.md','docs/remaining-work/architecture-and-compatibility-queue.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
save(r/'artifacts/library450-publication-paths.json',{'EvidenceDirectory':out.relative_to(r).as_posix(),'DocumentPaths':docs,'ProductPaths':tests})
print(json.dumps({'Passed':True,'ProofFiles':len(manifest)+1,'ArchiveFiles':len(archive),'Runtime':runtime,'ProductionChanges':0}))
