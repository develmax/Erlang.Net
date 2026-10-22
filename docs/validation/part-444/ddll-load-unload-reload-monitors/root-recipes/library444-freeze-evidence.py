from pathlib import Path
import gzip,hashlib,json,shutil,subprocess
r=Path.cwd();out=r/'docs/validation/part-444/ddll-load-unload-reload-monitors';assert not out.exists()
def read(p):return json.loads((r/p).read_bytes())
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,x):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
original=read('artifacts/library444-ddll-modes-original1/completion.json');assert original['Passed'] and original['WholeRows']==72 and original['CompleteProviderScans']==2 and not original['NativeRecompiled']
managed=read('artifacts/library444-ddll-modes-managed1/completion.json');assert managed['Passed']
comp=read('artifacts/library444-ddll-modes-managed1/observations/ddll-modes444/comparison.json')
assert comp['Passed'] and comp['ExpectedRows']==72 and comp['PhysicalExit'] and comp['PhysicalCleanup'] and comp['ChildCompletion']
retained=read('artifacts/library444-ddll-retained-managed1/observations/visibility-all.json');assert retained['Passed'] and len(retained['Results'])==8
assert read('artifacts/library444-ddll-retained-managed1/observations/ddll-init-prefix435/comparison.json')['Passed']
reg=read('artifacts/library444-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==195 and reg['ExactCoverage'] and reg['ImmutableInputs']
boot=read('artifacts/library444-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
assert read('artifacts/library444-style-readiness1/completion.json')['Passed']
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tests','tools'],cwd=r)
ledger=read('docs/mfa-compatibility.json');assert len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert managed['RuntimeSha256']==boot['RuntimeSha256']=='05674E2BCE40D0F78C443D56D76901D8431AAE1CBEB562292AD9FC0A0E6564B4'
out.mkdir(parents=True);copied=[]
def copy(src,dst):
    src=r/src;dest=out/dst;dest.parent.mkdir(parents=True,exist_ok=True);raw=src.read_bytes();packed=src.suffix=='.json' and len(raw)>65536
    if packed:dest=dest.with_name(dest.name+'.gz');dest.write_bytes(gzip.compress(raw,mtime=0));assert gzip.decompress(dest.read_bytes())==raw
    else:dest.write_bytes(raw)
    copied.append(dict(Path=dest.relative_to(out).as_posix(),OriginalPath=src.relative_to(r).as_posix(),Bytes=dest.stat().st_size,Sha256=sha(dest),RawBytes=len(raw),RawSha256=hashlib.sha256(raw).hexdigest().upper(),Gzip=packed))
def tree(base,dest,skip=()):
    for p in sorted((r/base).rglob('*')):
        rel=p.relative_to(r/base)
        if p.is_file() and not set(rel.parts).intersection(skip) and p.suffix in {'.json','.log','.etf','.erl','.cs','.py','.md','.beam'}:copy(p.relative_to(r),Path(dest)/rel)
tree('artifacts/library444-ddll-modes-original1','original72')
tree('artifacts/library444-ddll-modes-managed1','managed72',{'harness'})
tree('artifacts/library444-ddll-retained-managed1','retained36-native8',{'harness'})
tree('artifacts/library444-affected-run1','affected195')
tree('artifacts/library444-standard79-final1','boot79')
tree('artifacts/library444-style-readiness1','style-readiness')
tree('artifacts/library444-ddll-modes-proposal1','recipe',{'authority'})
copy('artifacts/library444-affected-selection1/selection.json','affected-selection.json')
for name in ['library444-prepare-package1.py','library443-prepare-package.py','library444-select-affected.py','library444-run-standard79.py','library444-style-readiness.py','library444-freeze-evidence.py']:copy('artifacts/'+name,'root-recipes/'+name)
for folder in ['library444-driver-package1','binary-integration369/library444-current-build1']:
    for p in sorted((r/'artifacts'/folder).glob('*')):
        if p.is_file() and p.suffix in {'.json','.log'}:copy(p.relative_to(r),'preparation/'+Path(folder).name+'/'+p.name)
copy('artifacts/library444-driver-package1/package/erldrv-host-package.json','preparation/final-package-manifest.json')
save(out/'archive-map.json',copied)
save(out/'completion.json',dict(Status='LOCAL_SCOPE_ACCEPTED_CI_PENDING',RuntimeSha256=managed['RuntimeSha256'],PackageSha256=managed['PackageSha256'],NewOriginalRows=72,OwnRawRows=72,RetainedOriginalRows=36,RetainedNativeGroups=8,AffectedPassed=195,StandardPassed=79,SourceGuards=1618,ApiEntries=708,VerifiedContracts=118,ApiDelta=0,VerifiedDelta=0,ProductionFilesChanged=0,ReportedDifferences=0,NewCanonicalTestCount=0,WholeModuleClaim=False,ProviderScansPerBatch=2,Merge443='1bb8ce6b1dbc6df04e52bdc1b03095ad39219445',Merge443GreenCi=39872120419))
names=read('artifacts/library444-ddll-modes-proposal1/profile.json')['Cases'];assert len(names)==12
table='\n'.join('| `'+name+'` | Matched in all six captures |' for name in names)
(out/'README.md').write_text('''# Part444 — DDLL load/unload/reload and monitor protocols

Pinned OTP29.1.1: ad05823719d77c8faee87348ea39513d4e2f99c5. All12 prepared scenarios execute with the same genuine existing echo_drv DLL and identical compiled Erlang source on original OTP and our actual .NET native child/parent backend. No native recompilation, synthetic driver, injected parent row, sleeps, source result/golden rewrite or comparison-side normalization. All72 main results match in both complete typed terms and entire ETF values/stacks files. No discrepancy was found; production code and API classifications are unchanged.

| Scenario | Actual result |
|---|---|
'''+table+'''

Twelve value rows plus default/0/1/2/8 profiles make72, not72 new canonical test registrations. Explicit profile metadata uses actual system_flag prior-value readback and restores the original value in after; default metadata is read from the actual VM. These successful mode rows do not independently prove the entire backtrace-depth BIF/error-truncation contract. The observer catches complete unexpected probe errors/stacks; all original positive probes returned normally.

Each main source-observer result contains real statuses, ordered role/count observations and exact identity/reference predicates. Original and managed own-values/own-stacks separately preserve full actual PID/reference-bearing info and notification terms. Their12 names/order, five profiles and actual initial observer PID/nonempty records are validated; opaque bytes are not equated across VMs. This is not a claim of bit-identical complete public info or global namespaces. Both main raw files are compared wholesale only after actual child PhysicalExit/PhysicalCleanup/Completion and ZeroLeases pass.

Death is fenced by real helper DOWN: pinned process teardown performs DDLL cleanup before monitor exit delivery. Cleanup closes genuine held ports, joins helper termination, takes one actual info(processes) snapshot, unloads exactly this actor's counted references, and awaits a real unloaded_only notification. Only genuine absent-driver badarg maps to zero and is retained; this expected cleanup absence is not a tested full error-stack contract. An initial source draft retry-to-row-absence loop was corrected before execution/sealing, rather than accepting a timing-dependent witness.

Final Runtime05674E2B passes new72 plus retained36 whole comparisons, all8 native/HOST regression groups (including Part443 physical-child-loss correction), affected195/195 in10 immutable blocks and standard79/79 with actual collector/persistent cleanup. Build has zero warnings/errors and source layout is clean. Provider6427 and all bound inputs remain unchanged; each batch has two full before/finally-after scans and independent values/stacks routes after successful real observer compilation. The existing228 native DLL/stock receipt is authenticated/reused, not rerun. Source1618/default708/verified118 stay unchanged, deltas0/0. Historical8813 readiness authority is explicitly separate from exact publication-head CI, which is linked in the attached PR.

Large JSON receipts are losslessly stored as .json.gz. archive-map.json binds both stored and original raw bytes/hashes; Python gzip.decompress or another standard gzip reader recovers every byte. No observations or failures are removed by compression. All regular fixture/ETF/source bytes remain unchanged. Recipe paths retain original workspace-relative authority/helper dependencies; licensed vendor authorities are hash-referenced rather than duplicated. This is provenance, not a turnkey fresh-clone claim. Root alone builds/executes/publishes; helper preparation/review was source-only. LICENSE.local.txt is excluded.

Not closed: full DDLL API contracts, arbitrary reference/generation/owner races, permanent transitions, deferred reload failures, kill_ports/force-mode cross-products and other OS/ABI/physical profiles. These12 scenarios close their stated coverage only.
''',encoding='utf-8')
checkpoint='Part444 scoped local acceptance: all12 prepared DDLL mode/monitor scenarios match original OTP in72 whole source-observer values/profiles; own72 PID/reference rows retained separately. No differences or production edits. Retained36/native8, affected195,boot79 and actual cleanup pass on Runtime05674E2B. Source1618/default708/full118 unchanged, deltas0/0; standalone72 are not72 added canonical tests. Exact444 CI/packages tracked in attached PR. Part443 merged main1bb8ce6b; merge CI39872120419 green8823/8823 and both packages. Root owns execution; LICENSE.local.txt excluded. Evidence:docs/validation/part-444/ddll-load-unload-reload-monitors.'
for name in ['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md']:
    p=r/name;p.write_text(checkpoint+'\n\n'+p.read_text(encoding='utf-8'),encoding='utf-8')
p=r/'docs/NATIVE_API_LIMITATIONS.md';p.write_text(p.read_text()+'\nPart444 verifies12 prepared reference/load/unload/reload/monitor scenarios:72 new whole source-observer results and separate own72 raw rows, retained36/native8, affected195 andboot79 pass locally without production changes. Remaining broader mode/generation/owner/permanent/force/platform profiles stay open. Exact CI is tracked in PR. [Evidence](validation/part-444/ddll-load-unload-reload-monitors/README.md).\n')
p=r/'docs/remaining-work/INDEX.md';s=p.read_text().replace('Published main b90a53e4 includes Part442; exact merge CI39864425689 passes8823/8823 and both packages. Part443 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.','Published main1bb8ce6b includes Parts442/443; exact merge CI39872120419 passes8823/8823 and both packages. Part444 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.')
s=s.replace('remaining broader load/unload/reload/monitor/reference-owner partitions;', 'Part444 verifies its12 prepared load/unload/reload/monitor/reference-owner scenarios (new72 complete source-observer results); remaining broader generation/owner races, permanent/deferred-failure/force-mode partitions;')
p.write_text(s+'\nPart444 evidence and exact scenario list: [DDLL modes](../validation/part-444/ddll-load-unload-reload-monitors/README.md). No full-module or new API promotion follows from these successful scenarios.\n')
p=r/'docs/changelog/0078.md';assert '## Part444 ' not in p.read_text()
entry='''

## Part444 — DDLL reference, reload and monitor protocols

- Verify PR4 head7a90b14d against its green exact-head CI, then merge443 into main1bb8ce6b. Merge CI39872120419 passes8823/8823, all integration steps and both package/consumer checks. This is Git publication/CI package validation, not a NuGet release.
- Execute all12 prepared cases with the unchanged authenticated echo_drv DLL and exact stock facade: duplicate references and pending_driver admission, nonowner refusal, pending versus pending_driver monitors, ordinary/only unload cancellation, immediate loaded monitor, automatic monitor during reload, repeated reload, last deferred-owner cancellation, foreign demonitor and real monitor-owner death. Compile only the new identical Erlang observer; no native rebuild or old228 rerun. Full real values and five profiles yield72 complete main results, both whole typed/raw files equal, with separate72 own raw observations preserving actual refs/PIDs/info/notifications. No comparison normalization or expected-value construction.
- Correct a pre-execution observer cleanup draft: close real ports/join helpers, take one info(processes) snapshot and unload the exact self-owned count before awaiting actual unloaded_only. Source-prove DDLL cleanup precedes process DOWN. Do not use a fixed retry-to-absence loop or sleep-based lifetime assumption. Explicit depth metadata uses actual readback/restoration; successful mode rows are not a full depth-BIF contract audit.
- Both original and managed batches pass with two complete6427 provider scans (before/finally-after), bound input hashes and independent values/stacks. Genuine native child physical exit/cleanup/completion and zero owner leases pass. Retained36 full original comparisons/all8 native-HOST groups, affected195/195 in10 immutable blocks and standard79/79 with actual collector/persistent cleanup pass on Runtime05674E2B. Build has zero warnings/errors; layout/source/readiness authorities pass. Historical8813 readiness evidence is not relabelled current full execution.
- No discrepancy was found in this bounded group; production source and classifications remain unchanged. Source1618/default708/verified118, deltas0/0. The standalone72 comparisons are not72 extra canonical registrations; current catalog remains8823. Exact publication-head full CI/package results are tracked in the attached PR.
- Publish [all12 scenario results and complete raw proof](../validation/part-444/ddll-load-unload-reload-monitors/README.md). Compress large JSON receipts losslessly with both stored/raw hashes, avoiding repeated large text diffs without dropping any content. Remaining permanent, deferred-failure, force-mode, broader owner/generation races and platform/ABI contracts remain open. Preserve LICENSE.local.txt uncommitted.
'''
assert len((p.read_text()+entry).encode('utf-8'))<32768 and len((p.read_text()+entry).splitlines())<400
p.write_text(p.read_text()+entry)
files=[dict(Path=f.relative_to(out).as_posix(),Bytes=f.stat().st_size,Sha256=sha(f)) for f in sorted(out.rglob('*')) if f.is_file()]
save(out/'manifest.json',files)
print(json.dumps(dict(Passed=True,Files=len(files)+1,NewOriginal=72,OwnRows=72,Affected=195,Standard=79,CompressedJson=sum(x['Gzip'] for x in copied),ArchivedRawBytes=sum(x['RawBytes'] for x in copied),StoredBytes=sum(x['Bytes'] for x in copied))))
