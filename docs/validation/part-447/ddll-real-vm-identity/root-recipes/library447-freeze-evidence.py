from pathlib import Path
import ast,gzip,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-447/ddll-real-vm-identity';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
read=lambda p:json.loads((r/p).read_bytes())
runtime='723E93D73FE05F5182FD7BAB3CC0DE7FEDC013AA98E37DF799145BE474F5E096'
assert read('artifacts/library447-gates1/completion.json')['Passed']
accepted=[];original_exits=[]
for run,depths in [('library447-probe-run3',[1,2]),('library447-probe-run4',[0,8])]:
 top=read('artifacts/'+run+'/completion.json');assert top['RuntimeSha256']==runtime and top['InputsUnchanged'] and all(s['Equal'] for s in top['ProviderScans'])
 for d in depths:
  base='artifacts/'+run+'/depth'+str(d);body=read(base+'/observations/comparison.json');assert body['Passed'] and body['PhysicalCleanup'] and not body['Failures'] and len(body['Results'])==48
  assert all(x['Passed'] for x in body['Results']);accepted.extend(body['Results'])
  for role in ['primary','peer']:
   exit=read(base+'/'+role+'.exit.json');assert exit['Settled'] and not exit['ForcedTermination'] and exit['ExitCode']==0;original_exits.append(exit)
assert len(accepted)==192 and len(original_exits)==8
assert not read('artifacts/library447-probe-run3/completion.json')['Passed'];assert read('artifacts/library447-probe-run4/completion.json')['Passed']
for n in ['beam_ddll_identity447.erl','ddll_tcp447.erl']:
 assert sha(r/'artifacts/library447-probe3'/n)==sha(r/'artifacts/library447-probe4'/n)
 assert sha(r/'artifacts/library447-probe-run3/source'/n)==sha(r/'artifacts/library447-probe-run4/source'/n)
for n in ['beam_ddll_identity447.beam','ddll_tcp447.beam']:assert sha(r/'artifacts/library447-probe-run3/observer-ebin'/n)==sha(r/'artifacts/library447-probe-run4/observer-ebin'/n)
s3=(r/'artifacts/library447-probe3/Program.cs').read_text();s4=(r/'artifacts/library447-probe4/Program.cs').read_text();assert s3.replace('frames.Length==Math.Min(depth,2)','frames.Length==Math.Clamp(depth,1,3)')==s4
reg=read('artifacts/library447-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==205 and reg['ExactCoverage'] and reg['ImmutableInputs']
boot=read('artifacts/library447-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
native=read('artifacts/library447-ddll-retained-managed1/observations/visibility-all.json');assert native['Passed'] and len(native['Results'])==8
assert read('artifacts/library447-ddll-retained-managed1/observations/ddll-init-prefix435/comparison.json')['Passed']
assert read('artifacts/library447-retained446-managed1/observations/ddll-frontier446/comparison.json')['Passed']
assert read('artifacts/library447-style-readiness1/completion.json')['Passed']
ledger=read('docs/mfa-compatibility.json');assert ledger==json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'])) and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tests','tools'])
assert (r/'artifacts/library447-independent-analysis1/accepted-review.md').is_file()
helpers=r/'artifacts/library445-freeze-evidence.py';assert sha(helpers)==sha(r/'docs/validation/part-445/ddll-permanent-reload-force/root-recipes/library445-freeze-evidence.py')
functions=[n for n in ast.parse(helpers.read_text()).body if isinstance(n,ast.FunctionDef) and n.name in {'save','copy','tree'}];assert len(functions)==3
exec(compile(ast.Module(body=functions,type_ignores=[]),str(helpers),'exec'));out.mkdir(parents=True);archive=[]
for src,dest,skip in [
 ('artifacts/library447-probe-run1','initial-depth8',{'harness','drivers'}),
 ('artifacts/library447-probe-run3','first-four-profiles',{'harness','drivers'}),
 ('artifacts/library447-probe-run4','corrected-depth0-and8',{'harness','drivers'}),
 ('artifacts/library447-ddll-retained-managed1','retained36-native8',{'harness'}),
 ('artifacts/library447-retained446-managed1','retained446-36',{'harness'}),
 ('artifacts/library447-affected-run1','affected205',set()),
 ('artifacts/library447-standard79-final1','boot79',set()),
 ('artifacts/library447-style-readiness1','style-readiness',set()),
 ('artifacts/library447-gates1','gates',set()),
 ('artifacts/library447-independent-analysis1','independent-review',set())]:tree(src,dest,skip)
for n in [1,2,3,4]:tree('artifacts/library447-probe'+str(n),'recipe'+str(n),set())
for n in ['prepare-package1','prepare-successor2','prepare-successor3','prepare-successor4','select-affected','run-standard79','style-readiness','gates1','freeze-evidence']:
 copy('artifacts/library447-'+n+'.py','root-recipes/library447-'+n+'.py')
copy('artifacts/library447-main446-green.json','main446-green.json')
copy('artifacts/library447-affected-selection1/selection.json','affected-selection.json')
for folder in ['library447-driver-package1','binary-integration369/library447-current-build1']:
 for p in (r/'artifacts'/folder).glob('*'):
  if p.is_file() and p.suffix in {'.json','.log'}:copy(p.relative_to(r),'preparation/'+Path(folder).name+'/'+p.name)
copy('artifacts/library447-driver-package1/package/erldrv-host-package.json','preparation/final-package-manifest.json')
exact=sum(x['WholeRawEqual'] and x['WholeTypedEqual'] for x in accepted);own=192-exact
save(out/'archive-map.json',archive)
save(out/'completion.json',dict(Status='SCOPED_LOCAL_ACCEPTANCE_EXACT_HEAD_CI_TRACKED_IN_PR',RuntimeSha256=runtime,AcceptedComparisons=192,ExactWholeComparisons=exact,OwnReferenceProtocolComparisons=own,RetainedOriginalComparisons=72,RetainedNativeGroups=8,AffectedPassed=205,StandardPassed=79,SourceGuards=1618,ApiEntries=708,VerifiedContracts=118,ApiDelta=0,VerifiedDelta=0,ProductionChanges=0,CanonicalTestsAdded=0,DistributionHandshakeClaim=False,WholeModuleClaim=False))
(out/'README.md').write_text(f'''# Part447 — real VM DDLL reference exchange and node identity

Oracle: OTP29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`. Part446 is merged as `a2d58038`; exact merge CI39930254918 passes8823/8823, all mandatory steps and both package checks. Git publication and CI package validation are not a NuGet release.

## Accepted scope

The original compiler compiles one common Erlang worker/actor. Its exact same BEAM executes on real OTP and our actual runtime with the native DriverHost and original `erl_ddll` facade. Each depth0/1/2/8 cohort owns two separate original OTP OS processes and one actual managed runtime/native child. A private loopback packet4 TCP channel exchanges genuine references with the second VM. The peer actually decodes, calls DDLL and re-encodes the reference; the returned wire must equal the original entire input bytes. This is physical VM/codec/DDLL interoperability over framed TCP ETF, **not an Erlang distribution handshake, epmd, net_kernel transport, disconnect/reconnect or full distributed-node conformance test**.

- A genuine queued reference produced by the peer is rejected with complete `badarg` values and full source stacks. Both primary runtimes receive the same actual peer reference, permitting exact raw equality of that entire error.
- The primary's actual queued reference is decoded on the peer. Foreign DDLL cancellation fails and the creator's real queue remains1. Re-encoding on the peer and decoding on the creator recovers its internal reference, cancellation succeeds and the queue becomes0. Repeated cancellation succeeds without recreating a monitor.
- Public `setnode/2` with a real registered kernel process changes inactive node identity. A previously captured wire remains external and is rejected; the retained internal monitor still cancels correctly. A wire captured in the current epoch localizes correctly.
- Actual kernel termination is joined and public `is_alive` is bounded until inactive. The prior active wire is external after reset; the internal reference remains valid. A different node name and then the original name are admitted through the same public operation.
- The prior external epoch is retained by a genuine runtime `binary_to_term` value in the worker dictionary. Re-admitting the original name with requested creation4 selects5 on both VMs. Old wire is rejected, old internal reference remains valid, and a new-epoch wire roundtrip cancels a real new monitor with queue0.
- Actual unload notification precedes native child shutdown; PhysicalExit, PhysicalCleanup, Completion and zero leases pass for all accepted cohorts. Original servers ACK only after joining their worker and exit normally; cleanup attempts for both processes are independent.

All **192/192** accepted actions match across four profiles: **{exact} complete typed/raw equalities and {own} full own-reference protocol comparisons**. The latter preserve all raw terms and every stack frame/field; only specifically captured source reference pairs are compared by their actual ownership protocol. Expected values/stacks are not rewritten and opaque reference bits are not called equal. Binary wire observations remain separate from these192 action results. Standalone192 is not192 added canonical test registrations or API contract promotions.

## Corrected observer and retained failed attempt

Initial depth8 probe1 passes31 bounded actions but lacks the later queue/join/epoch fences. Unexecuted probe2 adds source-review fences. Probe3 runs all four profiles:1/2 each pass48;0/8 stop at an incorrect harness length assertion. The actual original and managed raw foreign-reference stacks already agree: cardinalities1/1/2/3 for0/1/2/8. Immutable successor4 corrects **only** `Min(depth,2)` to `Clamp(depth,1,3)`, retaining all frame comparisons. Erlang sources, BEAM bytes, product sources, assemblies and fixtures are unchanged. Only failed0/8 are repeated, each48/48. Final192 is explicitly a composition of96 unchanged successful rows and96 corrected-repeat rows, not one uninterrupted new four-profile run. No runtime discrepancy was found or product fix invented. All failed raw files and receipts remain archived.

Each execution batch has complete uncached6427-file provider inventories before/finally-after and immutable input checks. The cold initial inventory took65.734s; warm final0/8 scans took1.375s/1.453s, not an algorithm-speedup claim. The correction avoids rerunning96 already accepted action comparisons and two extra VM pairs. Existing echo DLL and original facade are authenticated against Passed228 preparation; no native rebuild or old228 rerun is claimed.

## Regression, readiness and remaining work

Final Runtime723E93D7 passes retained72 source-profile DDLL results (36 previous visibility/init and36 Part446), all8 native/HOST groups, **205/205** affected tests in11 immutable shards (the original195 plus10 identity/node targets), standard79 and actual collector/persistent cleanup. Build has zero warnings/errors; source layout passes. Readiness uses explicitly historical8813 evidence and is not a new full-suite claim. Exact publication-head8823/full CI and both package outcomes are tracked in the attached PR.

Source guards1618, default APIs708 and verified contracts118 remain unchanged; API/full deltas0/0. Native bodies/loading remain ETS51/51, DDLL7/7, debug20/20, internal71/71, separate from full compatibility. No whole-module promotion. Remaining DDLL work includes actual Erlang distribution handshake/connection-generation/reconnect paths, held native teardown interleavings, wider load/reload/force partitions and additional OS/ABI/thread profiles. Public setnode identity probes here do not prove standard net_kernel/application transport boot.

This lossless source/provenance archive retains local workspace-relative preparation paths; it is not a fresh-clone turnkey native CI recipe. `.json.gz` decompresses to the original bytes and every stored/raw hash appears in archive-map; original ETF and BEAM remain unchanged. Vendor provider/package binaries are hash-referenced rather than copied. The uncommitted local license file is excluded.
''',encoding='utf-8')
note='Part447 scoped local acceptance: real TCP/ETF exchange with two original OTP OS processes plus genuine managed/native runtime; public setnode/kernel death/different-name and retained same-name creation4->5 transitions pass192/192 composed actions ('+str(exact)+' exact whole,'+str(own)+' full own-reference protocols). Preserve failed observer cardinality assumption and repeat only0/8; no product discrepancy/change or distribution handshake claim. Retained72/native8,affected205,boot79 and cleanup pass on Runtime723E93D7; source1618/default708/full118 unchanged, deltas0/0. Part446 merged a2d58038; merge CI39930254918 green8823 and both packages. Exact447 CI/packages tracked in PR; LICENSE.local.txt excluded. Evidence:docs/validation/part-447/ddll-real-vm-identity.'
docs=['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md','docs/NATIVE_API_LIMITATIONS.md','docs/remaining-work/INDEX.md']
for name in docs[:3]:
 p=r/name;p.write_text(note+'\n\n'+p.read_text(),encoding='utf-8')
p=r/docs[3];s=p.read_text().replace('checkpoints through Part446','checkpoints through Part447');old=next(l for l in s.splitlines() if l.startswith('| `erl_ddll` |'))
s=s.replace(old,'| `erl_ddll` | Part447 genuine cross-VM TCP/ETF references and public node-identity/retained-epoch transitions | 192/192 composed actions ('+str(exact)+' exact whole / '+str(own)+' full own-reference protocols), retained72/native8, affected205 andboot79 with real cleanup; no product/API promotion. Part446 merge a2d58038 CI39930254918 green8823/packages; exact447 CI tracked in PR. | Erlang distribution handshake/connection-generation/reconnect paths, held native teardown interleavings, broader mode cross-products and additional OS/ABI/thread profiles. |')
p.write_text(s+'\nPart447 bounded acceptance: [real VM reference exchange and identity](validation/part-447/ddll-real-vm-identity/README.md).\n',encoding='utf-8')
p=r/docs[4];s=p.read_text();s=s.replace('Published main a72f6903 includes Parts442-445; exact merge CI39922245233 passes8823/8823 and both packages. Part446 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.','Published main a2d58038 includes Parts442-446; exact merge CI39930254918 passes8823/8823 and both packages. Part447 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.')
s=s.replace('remaining actual VM node-creation/live-peer references, held native teardown interleavings and broader reload/demonitor/force combinations;', 'Part447 verifies real two-OS-VM TCP/ETF reference exchange, public node identity reset/admission and retained same-name epoch selection in192 composed actions; remaining actual Erlang distribution handshake/connection-generation/reconnect paths, held native teardown interleavings and broader reload/demonitor/force combinations;')
p.write_text(s+'\nPart447 proof: [real VM references and node identity](../validation/part-447/ddll-real-vm-identity/README.md). No whole-module promotion.\n',encoding='utf-8')
entry='''# Detailed work changelog — volume0079

## Part447 — real VM DDLL references and node identity

- Merge Part446 PR7 only after verifying its exact head f7988a8d green; main a2d58038 merge CI39930254918 also passes8823/8823, every mandatory step and both package checks. Git publication is separate from a NuGet release.
- Execute a single original-compiled Erlang worker/actor unchanged on pinned OTP and genuine managed/native runtime. Each cohort owns two actual OTP OS processes connected through private packet4 TCP ETF. A real peer creates monitor refs, decodes foreign refs, invokes DDLL and re-encodes them; entire returned wire bytes are preserved. Foreign cancellation preserves queue1; creator cancellation produces queue0. Do not relabel this transport as Erlang distribution handshake/reconnect coverage.
- Exercise public setnode/2 and actual registered kernel termination, bounded inactive wait, different-name admission, retained old internal refs versus old external wire, and a genuine retained external epoch. Re-admitting the original name requests creation4 but correctly selects5 in both runtimes; fresh reference roundtrip and actual unload/cleanup pass.
- Preserve initial probe1 and failed probe3. Independent review strengthens actual queue/error/join/cleanup fences. Probe3 profiles1/2 pass48 each;0/8 expose an observer-only wrong stack-length assumption. Actual raw OTP and managed stacks already match1/1/2/3 frames. Immutable successor4 changes only Min(depth,2) to Clamp(depth,1,3); rerun only0/8 at48 each. Final192/192 is an explicit96 retained +96 repeat composition, with unchanged worker/BEAM/runtime/native bytes. No product discrepancy or forced fix.
- Preserve complete raw answers and full frames before assertions; distinguish exact whole equality from full own-reference protocol comparisons with captured reference bindings. No expected rewriting, opaque-bit equality, whole-module promotion or192 new canonical tests. Two complete6427 provider scans bound each batch. Avoid two unnecessary VM pairs/96 repeated accepted actions; cold versus warm inventory timing is not an algorithm speed claim.
- Final Runtime723E93D7 passes72 retained source-profile DDLL comparisons,8 actual native/HOST groups,205/205 affected in11 immutable shards, standard79 and real collector/persistent cleanup. Build/style/readiness pass; historical8813 evidence is not a new full run. Exact publication-head8823/full CI and both packages are tracked in its PR.
- Source1618/default708/full118 and classifications stay unchanged, API/full deltas0/0. Publish [full raw/source proof and remaining boundaries](../validation/part-447/ddll-real-vm-identity/README.md). Remaining real distribution handshake/generation/reconnect, held native teardown, wider mode and platform/ABI profiles stay explicit. Root alone owns shared code/build/native/VM/git/CI. Preserve LICENSE.local.txt uncommitted.
'''
p=r/'docs/changelog/0079.md';assert not p.exists();p.write_text(entry,encoding='utf-8');docs.append('docs/changelog/0079.md')
p=r/'CHANGELOG.md';s=p.read_text();old='- [0078.md](docs/changelog/0078.md) — part440 onward (active; readiness publication and remaining native contracts)';assert old in s;s=s.replace(old,'- [0078.md](docs/changelog/0078.md) — parts440–446 (closed; readiness publication and native contracts)');p.write_text(s+'\n- [0079.md](docs/changelog/0079.md) — part447 onward (active; native interoperability and identity contracts)\n',encoding='utf-8');docs.append('CHANGELOG.md')
save(r/'artifacts/library447-publication-paths.json',dict(DocumentPaths=docs,EvidenceDirectory=out.relative_to(r).as_posix(),LicenseLocalExcluded=True))
files=[dict(Path=p.relative_to(out).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(out.rglob('*')) if p.is_file()];save(out/'manifest.json',files)
print(json.dumps(dict(Passed=True,Files=len(files)+1,ExactWhole=exact,OwnProtocol=own,Accepted=192,Affected=205,Native=8,Standard=79,RawArchiveFiles=len(archive))))
