from pathlib import Path
import gzip,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-445/ddll-permanent-reload-force';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
read=lambda p:json.loads((r/p).read_bytes())
def save(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
runtime='90EAF3A29E9DEDE3302FF8A5DC669386DEDA443646A381F60494197C07EA987A'
original=read('artifacts/library445-ddll-final-original1/completion.json');assert original['Passed'] and original['WholeRows']==60 and len(original['ProviderBatchChecks'])==2
before=read('artifacts/library445-ddll-final-managed1/observations/ddll-final-modes445/comparison.json');assert not before['Passed'] and len(before['Groups'])==6
diagnosis=read('artifacts/library445-prefixed-row-diagnosis.json');assert diagnosis['Equal']==54 and diagnosis['Different']==6
after=read('artifacts/library445-ddll-final-managed2/observations/ddll-final-modes445/comparison.json');assert after['Passed'] and after['WholeMainRows']==60 and len(after['Groups'])==6
for row in after['Groups']:assert all(row[k] for k in ['Passed','WholeTypedEqual','WholeRawEqual','OwnProvenanceValid','PhysicalExit','PhysicalCleanup','ChildCompletion']) and not row['Failures']
assert read('artifacts/library445-ddll-final-managed2/completion.json')['RuntimeSha256']==runtime
assert read('artifacts/library445-retained444-managed1/observations/ddll-modes444/comparison.json')['Passed']
assert read('artifacts/library445-ddll-retained-managed1/observations/ddll-init-prefix435/comparison.json')['Passed']
native=read('artifacts/library445-ddll-retained-managed1/observations/visibility-all.json');assert native['Passed'] and len(native['Results'])==8
reg=read('artifacts/library445-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==195 and reg['ExactCoverage'] and reg['ImmutableInputs']
boot=read('artifacts/library445-standard79-final2/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
assert read('artifacts/library445-style-readiness2/completion.json')['Passed']
ledger=read('docs/mfa-compatibility.json');assert len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tests','tools'],cwd=r).decode().splitlines()==['src/Erlang.Runtime/RuntimeDdllParent422.cs']
out.mkdir(parents=True);archive=[]
def copy(src,dest):
    source=r/src;destination=out/dest;destination.parent.mkdir(parents=True,exist_ok=True);raw=source.read_bytes();packed=source.suffix=='.json' and len(raw)>65536
    if packed:destination=destination.with_name(destination.name+'.gz');destination.write_bytes(gzip.compress(raw,mtime=0));assert gzip.decompress(destination.read_bytes())==raw
    else:destination.write_bytes(raw)
    archive.append(dict(Path=destination.relative_to(out).as_posix(),OriginalPath=source.relative_to(r).as_posix(),Bytes=destination.stat().st_size,Sha256=sha(destination),RawBytes=len(raw),RawSha256=hashlib.sha256(raw).hexdigest().upper(),Gzip=packed))
def tree(base,dest,skip=()):
    for p in sorted((r/base).rglob('*')):
        rel=p.relative_to(r/base)
        if p.is_file() and not set(rel.parts).intersection(skip) and p.suffix in {'.json','.log','.etf','.erl','.cs','.c','.h','.py','.md','.txt','.beam'}:copy(p.relative_to(r),Path(dest)/rel)
for source,dest,skip in [
    ('artifacts/library445-ddll-final-original1','original60',{'drivers'}),
    ('artifacts/library445-ddll-final-managed1','pre-fix54-of60',{'harness'}),
    ('artifacts/library445-ddll-final-managed2','final60',{'harness'}),
    ('artifacts/library445-retained444-managed1','retained72',{'harness'}),
    ('artifacts/library445-ddll-retained-managed1','retained36-native8',{'harness'}),
    ('artifacts/library445-affected-run1','affected195',set()),
    ('artifacts/library445-standard79-final1','interrupted-boot79',set()),
    ('artifacts/library445-standard79-final2','boot79',set()),
    ('artifacts/library445-style-readiness1','first-style-oom',set()),
    ('artifacts/library445-style-readiness2','style-readiness',set()),
    ('artifacts/library445-ddll-final-modes-proposal1','capture-recipe',{'authority','harness'}),
    ('artifacts/library445-ddll-final-modes-proposal2','pre-fix-consumer-recipe',{'authority'}),
    ('artifacts/library445-ddll-final-modes-proposal3','final-consumer-recipe',{'authority'}),
    ('artifacts/library445-retained444-proposal1','retained72-recipe',{'authority'}),
    ('artifacts/library445-independent-review1','independent-pre-fix-review',set()),
    ('artifacts/library445-independent-final-review1','independent-final-review',set())]:tree(source,dest,skip)
copy('artifacts/library445-ddll-final-original1/drivers/permanent_drv445.dll','original60/native/permanent_drv445.dll')
for name in ['library445-main444-green-receipt.json','library445-prefixed-row-diagnosis.json','library445-diagnose.py','library445-final-preparation.py','library445-prepare-package1.py','library445-prepare-package2.py','library443-prepare-package.py','library445-prepare-retained444.py','library445-select-affected.py','library445-run-standard79.py','library445-run-standard79-2.py','library445-style-readiness.py','library445-style-readiness2.py','library445-freeze-evidence.py','library445-stage-audit.py']:copy('artifacts/'+name,'root-recipes/'+name)
copy('artifacts/library445-affected-selection1/selection.json','affected-selection.json')
for folder in ['library445-driver-package1','library445-driver-package2','binary-integration369/library445-current-build1','binary-integration369/library445-current-build2','binary-integration369/library445-current-build3']:
    for p in sorted((r/'artifacts'/folder).glob('*')):
        if p.is_file() and p.suffix in {'.json','.log'}:copy(p.relative_to(r),'preparation/'+Path(folder).name+'/'+p.name)
copy('artifacts/library445-driver-package2/package/erldrv-host-package.json','preparation/final-package-manifest.json')
save(out/'archive-map.json',archive)
save(out/'completion.json',dict(Status='LOCAL_SCOPE_ACCEPTED_EXACT_HEAD_CI_TRACKED_IN_PR',RuntimeSha256=runtime,PackageSha256=read('artifacts/library445-driver-package2/completion.json')['PackageSha256'],NewOriginalRows=60,OwnRawRows=60,PreFixMatches=54,PreFixDifferences=6,FinalMatches=60,RetainedOriginalRows=108,RetainedNativeGroups=8,AffectedPassed=195,StandardPassed=79,SourceGuards=1618,ApiEntries=708,VerifiedContracts=118,ApiDelta=0,VerifiedDelta=0,ProductionFilesChanged=1,NewCanonicalTestCount=0,WholeModuleClaim=False,Merge444='ef3840c5fe8db7c792232e71cdc896e4d03a6644',Merge444GreenCi=39914974411))
cases=read('artifacts/library445-ddll-final-modes-proposal3/profile.json')['Cases'];assert len(cases)==10
table='\n'.join('| `'+name+'` | Matches in all six profiles |' for name in cases)
(out/'README.md').write_text('''# Part445 — DDLL permanent, deferred reload failures and force modes

Pinned OTP29.1.1: ad05823719d77c8faee87348ea39513d4e2f99c5. Root merged Part444 as main ef3840c5; exact merge CI39914974411 passes8823/8823, all mandatory steps and both package/consumer checks. This is Git publication and CI package validation, not a NuGet release.

## Actual defect and correction

The genuine new native driver invokes driver_lock_driver on its real opened port from control, outside init/finish. While a real port holds a pending reload, the same observer owns loaded, unloaded and unloaded_only waiters. Before correction, all six complete source-profile comparisons fail only the permanent notification-order field:54/60 per-case values agree. Original order is loaded_monitor, reload_request, unloaded_monitor, only_monitor. Our combined traversal sent only_monitor, unloaded_monitor, loaded_monitor, reload_request. Independent review binds complete actual/expected raw/typed/own files and proves no other field differs.

RuntimeDdllParent422.LockDriver now performs three per-kind passes in pinned source order (erl_bif_ddll.c1022-1043): AwaitingLoad, AwaitingUnload, AwaitingUnloadOnly. Traversal within a kind, monitor reference/name identity, shared gate, process-reference clearing and permanent ownership remain intact. The source SHA changes exactly E290340C to CD1BAAF0; no other product file changes. Independent final review reverses only that block in memory and reproduces the entire pre-fix source hash.

## Genuine acceptance

'''+table+'''

Identical compiled Erlang source and exact same DLL bytes run on original OTP and our actual native child/parent backend. One actual native fixture is newly compiled; authenticated echo/wrongname/initfail DLLs and the stock facade from the earlier228 receipt are reused. The latter is not a228 rerun. Missing DLL, wrong-name and failed-init deferred reloads produce actual load_failure/open_error or source error atoms, remove failed state, then permit a fresh healthy load and real echo exchange. Force scenarios cover unload option, initial kill_ports flag, reload, multiple owners and actual last-owner death.

Ten rows in each of six independently isolated VM/Runtime-child groups (values/default/0/1/2/8) yield60 whole main observations. All six entire final main ETF files and complete typed terms match; no source golden construction, data rewriting or comparison-side normalization. Explicit profile metadata is actual system_flag readback/restoration. These normal-return mode results are not proof of every error stack or the entire depth-BIF contract. An explicit binary-name subprobe retains either its actual full exception/stack or actual accepted reference+notification; pinned OTP accepted it in this cohort.

Each group retains complete own ETF/typed logs with real refs/PIDs/messages/info. Opaque bits are not compared across independent VMs. Same-actor permanent notification order is compared; force port EXIT and DDLL notification are separate producers, so source main waits for both and reports each role. Actual arrival stream remains in own raw logs, without inventing a cross-producer ordering guarantee. Genuine ports, helper handshakes and process DOWN provide barriers; no sleeps or retry-to-absence loop. Permanent is retained until VM/runtime disposal, with no false wait for unloaded.

All six managed children prove actual PhysicalExit, PhysicalCleanup, Completion and zero owner leases. Final Runtime90EAF3A2 additionally passes previous Part44472 and retained36 whole original comparisons, all8 native/HOST regressions, affected195/195 in10 immutable blocks and standard79/79 with actual collector/persistent cleanup. Thus168 deterministic source rows were replayed on final assemblies;60 are new,108 retained. Provider6427 is scanned completely before/finally-after each new batch, all bound inputs stable; native/compiler/VM processes are owned and bounded. Independent routes continue after sibling failure when preparation succeeded.

First compiler and style initialization OOMs are preserved. The serial build retry and one-processor low-memory style retry pass without weakening checks or changing product behavior. An earlier boot execution session disappeared without an exit/comparison receipt; its child was absent on read-only inspection, but that attempt receives no success/cleanup credit. Fresh boot79 output supplies the actual passing lifecycle evidence. Actual CLR builds have zero warnings/errors; original Erlang observer retains three unused-helper warnings. Final source layout needs zero changes. Source1618/default708/full118 and classifications stay unchanged, deltas0/0. The standalone60 rows are not60 new canonical registrations; the catalog remains8823. Historical8813 readiness authority is distinct from final publication-head full CI, whose exact SHA/results are linked in the attached PR.

## Evidence and limits

Large JSON receipts use lossless gzip with deterministic metadata; archive-map.json binds both stored and original raw SHA/length. Git-index audit decompresses every stored receipt and verifies all source/evidence bytes. No earlier failure is overwritten. Source-only helper preparation/review is separate from Root actual native/VM execution. Recipes retain original workspace-relative authority/helper dependencies; vendor authority files are hash-referenced, not redundantly copied. This is provenance, not turnkey fresh-clone replay. LICENSE.local.txt remains excluded.

This closes these ten stated scenarios, not all DDLL contracts or all force combinations. Wider reference-origin/generation/owner concurrency, repeated permanent transitions, additional force cross-products and other OS/ABI/native-thread/physical profiles remain separate. Adjacent erlang port APIs and actual ERTS layout are not promoted by these results.
''',encoding='utf-8')
checkpoint='Part445 scoped local acceptance: genuine permanent callback exposes6 ordered-message mismatches (54/60 before); three source-ordered NotifyAll passes fix all60/60 whole source-profile results. Three deferred failures recover and six force scenarios match. Retained72+36/native8, affected195,boot79 and real cleanup pass on Runtime90EAF3A2. One product/source-guard delta; source1618/default708/full118 unchanged, API/full deltas0/0; standalone60 are not60 canonical tests. Exact publication-head CI/packages tracked in attached PR. Part444 merged main ef3840c5; merge CI39914974411 green8823/8823 and both packages. Root owns execution; first compiler/style OOMs retained; LICENSE.local.txt excluded. Evidence:docs/validation/part-445/ddll-permanent-reload-force.'
for name in ['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md']:
    p=r/name;p.write_text(checkpoint+'\n\n'+p.read_text(encoding='utf-8'),encoding='utf-8')
p=r/'docs/NATIVE_API_LIMITATIONS.md';text=p.read_text().replace('checkpoints through Part444','checkpoints through Part445')
oldrow=next(line for line in text.splitlines() if line.startswith('| `erl_ddll` |'))
text=text.replace(oldrow,'| `erl_ddll` | Part445 permanent notification ordering, three deferred reload failures/recovery and six force scenarios | Original60 whole source-profile results:54/60 before,60/60 after one source-ordered notification fix. Retained72+36/native8, affected195,boot79 and real physical cleanup pass locally. Part444 merge ef3840c5 CI39914974411 green8823/8823 and both packages; exact445 CI tracked in PR. | Wider owner/generation/reference races, repeated permanent transitions, force cross-products beyond the six cases and additional OS/ABI/native-thread/physical profiles. No whole-DDLL contract promotion. |')
p.write_text(text+'\nPart445 scope and exact defect/evidence: [permanent, deferred reload and force](validation/part-445/ddll-permanent-reload-force/README.md). API/full-contract deltas0/0.\n',encoding='utf-8')
p=r/'docs/remaining-work/INDEX.md';text=p.read_text();text=text.replace('Published main1bb8ce6b includes Parts442/443; exact merge CI39872120419 passes8823/8823 and both packages. Part444 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.','Published main ef3840c5 includes Parts442-444; exact merge CI39914974411 passes8823/8823 and both packages. Part445 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.')
text=text.replace('remaining broader generation/owner races, permanent/deferred-failure/force-mode partitions;', 'Part445 corrects permanent notification order and verifies its three deferred reload failure/recovery and six force scenarios (new60 whole source-profile results); remaining broader generation/reference/owner races, repeated permanent transitions and additional force cross-products;')
p.write_text(text+'\nPart445 exact ten-scenario scope: [DDLL permanent/reload/force](../validation/part-445/ddll-permanent-reload-force/README.md). No API or full-module promotion follows from this increment.\n',encoding='utf-8')
p=r/'docs/changelog/0078.md';assert '## Part445 ' not in p.read_text()
entry='''

## Part445 — DDLL permanent ordering, deferred reload errors and force modes

- Verify PR5 head fe76360a green CI39874125918, merge444 into main ef3840c5, and verify exact merge CI39914974411:8823/8823, all mandatory steps and both package/consumer checks. Git publication/CI package validation is not a NuGet release.
- Compile one genuine native driver whose actual control calls driver_lock_driver, then execute the identical source observer and exact DLL bytes in six original OTP VM/profile groups. All60 positive main rows return normally; own raw logs retain actual identities/info/messages. Existing authenticated echo/wrongname/initfail fixtures and stock facade are reused, without re-running the older228 corpus.
- Preserve actual pre-fix54/60: all six mismatches are the same-actor permanent notification order, with every other field and nine other scenarios equal. Correct LockDriver to notify AwaitingLoad, AwaitingUnload and AwaitingUnloadOnly in three pinned-source passes. Shared gate, actual reference delivery, process clearing, permanent image ownership and physical fences are unchanged. Independently verify the exact single-file source delta; never refresh arbitrary admission hashes or rewrite expected values.
- Final Runtime90EAF3A2 matches all60 whole typed/raw source-profile outcomes, including permanent followups; missing/wrong-name/failed-init deferred reload notifications and healthy recovery; force unload option, initial kill_ports flag, reload, multiple owners and owner death. All six actual child physical exits/cleanups/completions and zero leases pass. Complete actual own logs remain separate; no opaque cross-VM bit parity or cross-producer signal-order guarantee is invented.
- Re-run prior72 and retained36 original comparisons, all8 native/HOST groups, affected195/195 in10 immutable blocks and standard79/79 with real collector/persistent cleanup on final sources/assemblies. Preserve initial compiler/style OOM receipts and an interrupted first boot session without credit; fresh serial/low-memory retries pass, source layout is clean and only the reviewed source guard changes. Original observer unused-helper warnings remain visible. Historical8813 readiness evidence is not relabelled as a current full-suite run.
- Source1618/default708/verified118 remain unchanged; API/full deltas0/0. Standalone60 comparisons do not add60 canonical test registrations. Exact publication-head full CI/package outcomes are tracked in the attached PR. Publish [complete proof and bounded remaining work](../validation/part-445/ddll-permanent-reload-force/README.md), with lossless large-JSON compression and stored/raw hash audit. Broader reference/generation/owner races, repeated permanent transitions, other force combinations and OS/ABI/physical profiles remain separate. Preserve LICENSE.local.txt uncommitted.
'''
assert len((p.read_text()+entry).encode('utf-8'))<32768 and len((p.read_text()+entry).splitlines())<400
p.write_text(p.read_text()+entry,encoding='utf-8')
files=[dict(Path=p.relative_to(out).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(out.rglob('*')) if p.is_file()]
save(out/'manifest.json',files)
print(json.dumps(dict(Passed=True,Files=len(files)+1,RawArchiveFiles=len(archive),OriginalNew=60,OriginalRetained=108,NativeGroups=8,Affected=195,Standard=79,CompressedJson=sum(x['Gzip'] for x in archive),RawBytes=sum(x['RawBytes'] for x in archive),StoredBytes=sum(x['Bytes'] for x in archive))))
