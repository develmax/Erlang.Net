from pathlib import Path
import ast,gzip,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-452/ddll-late-discovery-and-owner-races';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper();read=lambda p:json.loads((r/p).read_bytes())
runtime='18AE9106B6A4396D402BFCD9058ACE77CC2863A57C627EBFEBA496029625D307'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='d4430b636685a4e7015cdddb4b1c512bd4885510'
for name in ['parent451-green.json','source-authority.json','final-gates1/completion.json','style-readiness1/completion.json','independent-analysis1/accepted-audit.json','independent-analysis1/independent-audit.json','gates1/completion.json']:assert read('artifacts/library452-'+name)['Passed'],name
audit=read('artifacts/library452-independent-analysis1/accepted-audit.json')
for key,value in {'ObservationRows':1272,'RawExact':1104,'ProtocolOnly':168,'PairedRows':1128,'AuxiliaryGuards':144,'LateSuccessResults':8,'LateErrorResults':8,'NaturalOldTimersAfterHandoff':8,'OrderedMailboxes':16,'AuthenticatedChannels':72,'AlreadyPendingSilentRefusals':48,'CapturedKilledFamilyHandles':32}.items():assert audit[key]==value,key
assert len(audit['Groups'])==24
for n in range(1,6):
 root=f'artifacts/library452-probe-run{n}';top=read(root+'/completion.json');assert top['InputsUnchanged'] and top['RuntimeSha256']==runtime
 assert top['Passed']==(n!=3)
 assert all(x['Equal'] and x['Files']==6427 for x in top['ProviderScans']) and len(top['ProviderScans'])==2
 bindings=read(root+'/inputs-before.json');assert len(bindings)==2698 and all(sha(p)==h for p,h in bindings.items())
assert read('artifacts/library452-build-focus4/focus.json')['Passed']==1
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')==runtime==sha(r/'artifacts/library452-driver-package1/package/Erlang.Runtime.dll')
assert read('artifacts/library452-native-retained-managed1/observations/visibility-all.json')['Passed']
ret=read('artifacts/library452-native-retained-managed1/observations/ddll-init-prefix435/comparison.json');assert ret['Passed'] and ret['ExpectedRows']==36 and ret['ActualNativeDrivers']
boot=read('artifacts/library452-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
ledger=read('docs/mfa-compatibility.json');assert ledger==json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'])) and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1621
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tools','build','original']).decode().strip()
prior=r/'artifacts/library449-freeze-evidence.py';assert sha(prior)==sha(r/'docs/validation/part-449/ddll-incoming-and-peer-restart/root-recipes/library449-freeze-evidence.py')
funcs=[n for n in ast.parse(prior.read_text()).body if isinstance(n,ast.FunctionDef) and n.name in {'save','copy','tree'}];exec(compile(ast.Module(body=funcs,type_ignores=[]),str(prior),'exec'));out.mkdir(parents=True);archive=[]
for n in range(1,6):tree(f'artifacts/library452-probe-run{n}',f'network-composite/run{n}',{'harness','drivers'})
for n in [1,2]:tree(f'artifacts/library452-probe{n}',f'observer-recipes/packet{n}',set())
for source,dest,skip in [
 ('affected-run1','pre-final-guards-affected591',set()),
 ('native-retained-managed1','native8-retained36',{'harness'}),
 ('standard79-final1','boot79',set()),
 ('style-readiness1','style-readiness',set()),
 ('independent-analysis1','independent-review',set()),
 ('gates1','pre-final-guards-gates',set()),
 ('final-gates1','final-guards-and-style',set()),
 ('driver-package1','fresh-native-package',{'build','package','observations'})]:tree('artifacts/library452-'+source,dest,skip)
for n in range(1,5):tree(f'artifacts/library452-build-focus{n}',f'build-focus-history/{n}',set())
copy('artifacts/library452-driver-package1/package/erldrv-host-package.json','fresh-native-package/package-manifest.json')
copy('artifacts/library452-affected-selection1/selection.json','pre-final-guards-selection.json')
for p in sorted((r/'artifacts').glob('library452-*.py')):copy(p.relative_to(r),'root-recipes/'+p.name)
for name in ['parent451-green.json','source-authority.json','final-test-guards.json']:copy('artifacts/library452-'+name,name)
copy('artifacts/library451-ci-final-receipt.json','parent451-ci-receipt.json')
tests=['tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/RetiredCandidateHostChecks452.cs','tests/Erlang.Tests/RetiredCandidateHostNames452.cs']
for p in tests:copy(p,'final-test-sources/'+Path(p).name)
save(out/'archive-map.json',archive)
save(out/'completion.json',{'Status':'SCOPED_LOCAL_ACCEPTED_EXACT_PUBLICATION_HEAD_CI_TRACKED_IN_PR','RuntimeSha256':runtime,'PackageSha256':read('artifacts/library452-driver-package1/completion.json')['PackageSha256'],'ObservationRows':1272,'PairedSourceProtocolRows':1128,'AuxiliaryGuards':144,'WholeTypedRawRows':1104,'OwnReferenceRows':168,'AcceptedScenarios':24,'LateDiscoveryResults':16,'NaturalOldTimersAfterHandoff':8,'AuthenticatedChannels':72,'AlreadyPendingRefusals':48,'ExpectedFatalOldVmExits':24,'NormalOracleReplacementExits':48,'CapturedKilledFamilyHandles':32,'PrivateEpmdProcesses':5,'AffectedComposite':591,'UnchangedRetainedCases':590,'FinalStrengthenedCase':1,'RetainedRows':36,'NativeGroups':8,'Standard':79,'SourceGuards':1621,'ApiEntries':708,'FullContracts':118,'ApiDelta':0,'FullDelta':0,'NewCanonicalGroups':1,'CanonicalCatalog':8828,'ProductionFilesChanged':0,'WholeModuleClaim':False,'ProductionManagedNetKernelBootClaim':False,'PreGuard591NotRelabelledFinal':True,'FailedRun3PreservedAndReplaced':True,'Parent451Merged':False})
(out/'README.md').write_text('''# Part452 — late discovery, old timers and owner races

Pinned OTP29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`; exact pinned blobs are retained. Actual original execution uses the admitted official release provider (6427 files verified before/after), not the newer source checkout HEAD. Parent451 PR12 remains open at d4430b63, exact CI39963573865 green8827/8827 and both packages. Exact452 publication-head full CI and package evidence are recorded in PR; no NuGet release is implied.

## Original kernel evidence and explicit adapter boundary

Reuse existing real TCP/EPMD/native/ETF observers and the temporarily configured original discovery callback after normal boot. All unheld discovery delegates to erl_epmd. Reach actual check_pending with genuine checker PID/queued caller; no direct sys_dist insertion or fake kernel messages.

Late success/error: establish the actual incoming acceptor before kernel suspension using the real controller send trace. Release NAME, observe real accept_pending in process_info(messages), then release the actual checker callback to return genuine erl_epmd discovery or nxdomain. Require actual setup_check second with its own PID/TimerRef/Res and no earlier timeout. Resume kernel; exact receive traces match both real queued messages. Original accept_pending kills/joins the checker with remarked, and its already queued result cannot steal the winning owner or identity.16 real mailbox schedules pass.

Old timer: leave discovery blocked; incoming takeover kills the checker while its real send_after to kernel remains armed. Complete valid alive handoff, actually replacing original owner/controller/connid, and exchange data. Require actual setup_check_timeout receive with the dead old checker PID AFTER handoff; sys:get_state fences processing. Whole raw original row is byte-identical before/after the old timer; whole managed snapshot assertions and actual data pass. Eight natural timers pass. Configured net_setuptime is5s; this is not default timeout or DNS proof.

Original net_kernel scheduling/configuration is original-only instrumentation. Managed runtime Pending/Connected queues, controllers and the explicit network adapter are separate evidence; no literal managed CheckPending/UpPending state or complete original standard managed kernel boot is claimed. Failure-only cleanup limits in independent packet review remain explicit (pre-connect link ownership and exception during resume/config restoration); successful accepted scenarios settle actual resources.

## Accepted composite and preserved failure

24 unique scenarios: depths0/1/2/8, both lexical orders, late_success/late_error/old_timer. Five-batch composite: run1 one pass,run2 two,run3 two independent passes,run4 one full replacement for the failed run3 old_timer,run5 remaining18. Run3 is NOT a successful batch. Its old_timer/handoff had passed before a Windows sharing IOException constructing the restarted peer; preserve complete failed logs/forced constructor cleanup. Exclude that case and use the full run4 replacement.

Packet2 changes readiness only: original writes/closes a private publishing file then atomically renames it; C#/Python readers retry only sharing errors32/33 within existing deadlines. Main observer/common worker/discovery/handshake and source scenario suffix stay unchanged. Control BEAM startup bytes intentionally differ; each is bound independently. No product semantics or comparison expectations are relaxed.

1272 observations=1128 paired source/protocol+144 auxiliary guards,1104 full typed/raw+168 explicit own-reference protocols. This is not1272 API contracts or canonical tests. Independent passive audits recompute every raw term and reference-role binding,16 ordered actual mailboxes,8 natural timers,72 accept_pending trace groups and48 silent already_pending refusals. All2698 bindings in every batch stay unchanged.72 actual authenticated channels drain queues and join.24 designated fatal old-peer exits and48 normal oracle/replacement exits settle;32 actually captured killed-family handles join,5 private EPMDs exit normally. Captured process family is not OS Job containment. Native exit/cleanup/completion and zero leases pass.

## Final managed test and regression provenance

The permanent452 test blocks the real old controller, queues a genuinely deferred adoption request, retires its pending generation, proves old native abort is inert for fresh pending/connected generations, releases the old request after the new winner exists, requires badarg without changing the whole winning snapshot, then delivers/drains actual queued data. Two final non-vacuity assertions prove the request had not completed early.

Preserve build1 Roslyn OutOfMemory and build2 new-test ulong conversion error. Correct the test using Integer; build3/final4 pass with0 warnings/errors.591/591 affected pass before the two final assertions. A source-delta receipt proves exactly those assertions were added; all other bound source inputs and product runtime are unchanged. Final composite retains590 unaffected cases plus strengthened final case1/1, not an uninterrupted final591 assembly run. Final style passes. Exact-head CI rebuilds final sources and runs all8828 tests and both packages; catalog presence alone is not full pass.

Runtime18AE9106B6A4396D402BFCD9058ACE77CC2863A57C627EBFEBA496029625D307 and package6B4CE9232A4AA8B6018D11484EF599F6599D072CEC1FBE5CA16594593EA2E56D bind all accepted network/native/boot evidence. Native8/retainedDDLL36 and standard79/actual collector+persistent cleanup pass. Default708 APIs/118 full contracts/1621 source guards stay unchanged, API/full deltas0/0. Historical8813 readiness authority stays separate.

## Remaining scope

Broader discovery/setup/timeout/owner schedules, malformed/legacy/dynamic negotiation,TLS,full managed standard original net_kernel transport/autoconnect/ALTACT and wider DDLL/platform/native ABI/thread contracts. No whole-module/performance promotion. P0/P1 and future architecture queue unchanged. Recipes retain authenticated workspace helpers and are not a fresh-clone turnkey native CI guarantee. Large JSON is losslessly compressed with stored/raw SHA manifests; raw ETF/protocol/source retained. LICENSE.local.txt excluded.
''',encoding='utf-8')
manifest=[{'Path':p.relative_to(out).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(out.rglob('*')) if p.is_file()];save(out/'manifest.json',manifest)
docs=['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/DECISIONS.md','docs/NATIVE_API_LIMITATIONS.md','docs/OTP-PORTS.md','docs/dependency-decisions.md','docs/changelog/0079.md','docs/remaining-work/INDEX.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
save(r/'artifacts/library452-publication-paths.json',{'EvidenceDirectory':out.relative_to(r).as_posix(),'DocumentPaths':docs,'ProductPaths':tests})
print(json.dumps({'Passed':True,'ProofFiles':len(manifest)+1,'ArchiveFiles':len(archive),'Runtime':runtime,'ProductionChanges':0}))
