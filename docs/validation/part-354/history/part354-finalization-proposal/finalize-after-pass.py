"""Artifact-only conditional Part354 finalization. Never changes Main, Git or runs code."""
from pathlib import Path, PurePosixPath
import argparse, collections, hashlib, json, re, shutil, subprocess

R=Path(__file__).resolve().parents[2]
P=Path(__file__).parent
H=lambda q:hashlib.sha256(q.read_bytes()).hexdigest().upper()
HB=lambda b:hashlib.sha256(b).hexdigest().upper()
J=lambda q:json.loads(q.read_bytes())
B=lambda x:(json.dumps(x,indent=2)+'\n').encode()
PLAN=R/'artifacts/size-bif354-full-preparation/composition-plan.json'
PLAN_SHA='7129614325BD320F9205DBFDEBC74E3C7F09065C318431360B64FA6A078890C6'
PARENTS_SHA='63D52C7E666B2AEFB1A0C66B9E95CBBCE00D1EB7997D2B71039623F71961A657'
RECIPE=R/'artifacts/part354-delivery-recipe/archive-copy-plan2.json'
RECIPE_SHA='FF1F5357770DC722886A3CB68E4161D8EF1D5E97AD8F29B1100816A88C475D9E'
STATUS='PASS_INDEPENDENT_ACTUAL_CANONICAL354_FULL_RAW_REGISTRY_RESTORATION_PENDING'
READER_SHA='60ABE504307A2ECA0BC686D5307B307C9B9AFFCA52E447F6C0FF022F99EDF82A'
FULL=R/'artifacts/size-bif354-full-actual1'
PREFIX='docs/validation/part-354/'


def safe(s):
    q=PurePosixPath(s)
    assert '\\' not in s and not q.is_absolute() and '..' not in q.parts and ':' not in s
    assert not any(x.lower() in ['.git','bin','obj'] for x in q.parts) and 'license.local' not in s.lower()
    return s


def artifact(q):
    q=q.resolve(strict=True)
    assert q.is_relative_to(R/'artifacts') and q.is_file()
    return q


def pinned(q,digest):
    q=artifact(q)
    assert H(q)==digest.upper(),str(q)
    return q


a=argparse.ArgumentParser(description=__doc__)
a.add_argument('--root-completion-notice',choices=['FULL354_TERMINAL_INDEPENDENT_PASS_AND_READINESS_ACCEPTED'],required=True)
a.add_argument('--approved-source-freeze-sha256',required=True)
a.add_argument('--independent-guard',type=Path,required=True)
a.add_argument('--independent-guard-sha256',required=True)
a.add_argument('--readiness-copy-plan',type=Path,required=True)
a.add_argument('--readiness-copy-plan-sha256',required=True)
a.add_argument('--output',type=Path,required=True)
a.add_argument('--extra-proof-plan',nargs=2,action='append',default=[],metavar=('PATH','SHA256'))
v=a.parse_args()
freeze=pinned(P/'source-freeze.json',v.approved_source_freeze_sha256)
for row in J(freeze)['Files']:
    q=P/safe(row['Path'])
    assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
assert H(PLAN)==PLAN_SHA and H(RECIPE)==RECIPE_SHA and H(P/'parent-guards.json')==PARENTS_SHA
plan=J(PLAN);recipe=J(RECIPE);parents=J(P/'parent-guards.json')
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,timeout=10).decode().strip()==parents['ParentHead']
for row in parents['ParentFiles']:
    q=R/safe(row['Path'])
    assert (H(q) if q.exists() else None)==row['ParentSha256'],row['Path']
candidate=Path(plan['Destination']).resolve()
assert candidate==(R/'artifacts/size-bif354-full-candidate').resolve()
gpath=pinned(v.independent_guard,v.independent_guard_sha256);g=J(gpath)
assert g['Status']==STATUS and g['ReaderSha256']==READER_SHA
assert (g['Passed'],g['Failed'],g['StableClosureRows'],g['OwnedPaths'],g['TermOrderChildren'],g['PackageInputs'],g['RuntimeSourceGuards'])==(7342,0,3207,128,12,6,114)
assert g['CompositionPlanSha256']==PLAN_SHA and g['SourceFreezeSha256']=='0AD2F1D40EC998581A6FC78A9B32343FE7D64DBF94D131AD4AB280C5B43EB83C'
assert g['Old7313ExactPrefix'] and g['ChildMatrixVerified'] and g['RegistryNonLimitsAndOrderPreserved']
assert g['CompatibilityCounts']=={'Verified compatible':16,'Known differences':30,'Needs verification':541}
assert g['NativeOriginalExecuted'] is False and g['MainModified'] is False
assert H(FULL/'full.exit.json')==g['FullExitSha256'] and H(FULL/'artifacts/tests.json')==g['TestReportSha256']
exit=J(FULL/'full.exit.json');assert exit['Settled'] and exit['ExitCode']==0 and not exit['TimedOut'] and not exit['Failures']
assert H(FULL/'raw-supported-mfas.json')==g['RawRegistrySha256']
assert len(g['Packages'])==2 and len(g['IntegrationResults'])==11
for x in g['Packages']:assert H(candidate/safe(x['Path']))==x['Sha256']
for row in plan['StableClosure']:
    q=candidate/safe(row['Path']);assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'],row['Path']
# Full raw registry remains immutable; candidate customLimits restoration requires actual accepted readiness.
assert H(candidate/'docs/supported-mfas.json')==g['RegistryBeforeSha256']==H(FULL/'registry-before.json')
rpath=pinned(v.readiness_copy_plan,v.readiness_copy_plan_sha256);rp=J(rpath)
assert rp['Status']=='ACTUAL_ROOT_READINESS2_GENERATED_OUTPUT_PROPOSAL_NOT_MAIN_APPLIED'
assert rp['IndependentFullGuardSha256']==H(gpath) and rp['Counts']==[16,30,541] and rp['LocalPassedTests']==7342 and rp['RegistryRows']==587 and rp['RuntimeSourceGuards']==114
assert not rp['SdkBuildOrGuestRerun'] and not rp['MainChanged']
rc=rpath.parent;re=J(rc/'readiness.exit.json');assert re['Settled'] and re['ExitCode']==0 and not re['TimedOut'] and not re['Failures']
assert J(rc/'completion.json')['CopyPlanSha256']==H(rpath)
assert J(rc/'registry-restored.json')['RestoredSha256']==g['RegistryBeforeSha256']
assert J(rc/'restoration-intent.json')['IndependentGuardSha256']==H(gpath)
assert len(rp['Files'])==3 and {x['TargetPath'] for x in rp['Files']}=={'docs/module-readiness.json','docs/MODULE_READINESS.md','docs/API_COMPATIBILITY.md'}
ledger=J(candidate/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==114
for row in ledger['VerifiedSourceFiles']:assert H(candidate/safe(row['Path']))==row['Sha256']
for e in ledger['Entries']:
    for row in e['Evidence']:assert H(candidate/'docs'/safe(row['Path']))==row['Sha256']
# Establish all payload bytes before creating a fresh artifact overlay.
payload={};modes={}
def put(target,data,mode='RawExact'):
    safe(target)
    assert target not in payload or payload[target]==data,target
    payload[target]=data;modes[target]=mode

def copyrow(row):
    q=artifact(Path(row['SourcePath']));assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    put(row['TargetPath'],q.read_bytes())

mapping={x['Path']:x for x in plan['Files']}
for path in plan['OwnedPaths']:
    row=mapping[path];q=candidate/safe(path)
    assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'];put(path,q.read_bytes())
for row in recipe['Files']:copyrow(row)
assert len(recipe['Files'])==1080
for x in plan['PackageResources']:assert H(candidate/safe(x['Path']))==x['Sha256']
# Package/runtime binaries are independently hashed, not published as proof copies.
package_binding={'PackageResources':plan['PackageResources'],'Packages':g['Packages'],'RawRegistrySha256':g['RawRegistrySha256'],'ReviewedRegistrySha256':g['RegistryBeforeSha256'],'NoBinariesCopied':True}
put(PREFIX+'full/package-bindings.json',B(package_binding))
for row in rp['Files']:
    q=R/safe(row['SourcePath']);assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    put(PREFIX+'readiness/raw-generated/'+Path(row['TargetPath']).name,q.read_bytes())
    # Public Markdown gets only a documented final-newline trim; raw CLI bytes remain above.
    data=q.read_bytes()
    if row['TargetPath'].endswith('.md'):data=data.rstrip(b'\r\n')+b'\n'
    put(row['TargetPath'],data,'GeneratedRawWithSingleFinalNewline' if row['TargetPath'].endswith('.md') else 'RawExact')
for root,label in [(FULL,'full/raw'),(rc,'readiness/raw')]:
    for q in sorted(root.rglob('*')):
        if q.is_file() and q.suffix.lower() in ['.json','.log','.md','.txt'] and not any(x in ['bin','obj'] for x in q.relative_to(root).parts):
            put(PREFIX+label+'/'+q.relative_to(root).as_posix(),q.read_bytes())
childroot=candidate/'artifacts/term-order-children';receipts=list(childroot.glob('*/receipt.json'));assert len(receipts)==12
for receipt in receipts:
    for q in receipt.parent.iterdir():
        assert q.is_file() and q.name in ['receipt.json','stdout.txt','stderr.txt']
        put(PREFIX+'full/children/'+q.relative_to(childroot).as_posix(),q.read_bytes())
for q in [gpath,P/'parent-guards.json',freeze,Path(__file__),RECIPE,PLAN,rpath]:
    put(PREFIX+'root-delivery/'+q.name,q.read_bytes())
# Mandatory independent reader source, source-freeze and future restoration-helper source.
for folder in ['size-bif354-full-independent','size-bif354-after-full-proposal']:
    for q in (R/'artifacts'/folder).iterdir():
        if q.is_file() and q.suffix.lower() in ['.py','.json']:put(PREFIX+'root-delivery/'+folder+'/'+q.name,q.read_bytes())
for name,digest in v.extra_proof_plan:
    ep=pinned(Path(name),digest)
    for row in J(ep)['Files']:
        assert row['TargetPath'].startswith(PREFIX)
        copyrow(row)
# Keep the original readable registration-vs-compatibility split and rebuild only its category lists.
index=(R/'docs/API_COMPATIBILITY_INDEX.md').read_bytes().decode('utf-8');intro=index.split('## 100% compatible (16)',1)[0]
assert 'Registration and passing selected tests do not establish full compatibility.' in intro
old='**16 commands 100% compatible; 24 with known differences; 547 needing verification for 100% compatibility.**'
assert intro.count(old)==1;intro=intro.replace(old,'**16 commands 100% compatible; 30 with known differences; 541 needing verification for 100% compatibility.**')
sections=[]
for status,title,anchor,count in [('Verified compatible','100% compatible','100-compatible',16),('Known differences','Known differences','known-differences',30),('Needs verification','Needs verification for 100% compatibility','needs-verification-for-100-compatibility',541)]:
    entries=sorted((e for e in ledger['Entries'] if e['Status']==status),key=lambda e:(e['Module'],e['Function'],e['Arity']))
    assert len(entries)==count
    lines=[f'## {title} ({count})','','| Command | Details |','| --- | --- |']
    lines += [f"| `{e['Module']}:{e['Function']}/{e['Arity']}` | [Scope, evidence and remaining checks](API_COMPATIBILITY.md#{anchor}) |" for e in entries]
    sections.append('\n'.join(lines))
put('docs/API_COMPATIBILITY_INDEX.md',(intro+'\n\n'.join(sections)+'\n').encode(),'RebuiltCategoryListsPreservingIntroduction')
summary='Part 354 implements common failure-origin capture and the direct load_nif UTF8 filename-domain check. Actual canonical managed validation passes **7,342 tests, 11 integration outcomes and two packages**, preserving all 7,313 previous ordered names plus 29 permanent checks and twelve bounded child controls. The final focused ParseAll 28/36-target result remains separate from full evidence. Registry stays 587 APIs across 14 modules; compatibility is **16 commands 100% compatible, 30 Known differences and 541 Needs verification**, with 114 reviewed runtime source guards. The six new Known classifications record remaining valid heap/GC/MQD option refusals and absent default native loader behavior; they do not describe repaired generic capture or invalid filename42 as current faults. Earlier Parse/build/semantic failures and before/after raw ETF are retained. Original part 354 comparisons and same-head CI remain pending publication; no whole-module/native ABI/performance completion is claimed. '
for path,link in [('AGENTS.md','docs/validation/part-354/'),('docs/PROGRESS.md','validation/part-354/')]:
    raw=(R/path).read_bytes();separator=b'\r\n\r\n' if b'\r\n\r\n' in raw else b'\n\n';at=raw.index(separator)+len(separator)
    put(path,raw[:at]+(summary+f'[Evidence]({link}).\n\n').encode()+raw[at:],'PrependCheckpointPreserveHistoricalTail')
# Generated readiness remains archived verbatim; append a history paragraph in its public Markdown.
path='docs/MODULE_READINESS.md';raw=payload[path];separator=b'\r\n\r\n' if b'\r\n\r\n' in raw else b'\n\n';at=raw.index(separator)+len(separator)
payload[path]=raw[:at]+(summary+'[Evidence](validation/part-354/).\n\n').encode()+raw[at:];modes[path]='GeneratedMarkdownWithCheckpointAndSingleFinalNewline'
# Only current matrix/status summary changes; historical paragraphs remain untouched.
path='docs/ERLANG_COMPATIBILITY.md';text=(R/path).read_bytes().decode('utf-8');old='**16 APIs 100% compatible, 7 with known differences and 328 needing verification for 100% compatibility**';assert text.count(old)==1;text=text.replace(old,'**16 APIs 100% compatible, 13 with known differences and 322 needing verification for 100% compatibility**')
lookup={f"{e['Function']}/{e['Arity']}":e['Status'] for e in ledger['Entries'] if e['Module']=='erlang'};changed=0
for name in ['system_flag/2','spawn_request/2','spawn_request/3','spawn_request/4','spawn_request/5','load_nif/2']:
    oldline=f'| `{name}` | Needs verification for 100% compatibility; see [per-command evidence](API_COMPATIBILITY.md) |'
    assert text.count(oldline)==1,name
    text=text.replace(oldline,f'| `{name}` | Known differences; see [per-command evidence](API_COMPATIBILITY.md) |');changed+=1
assert changed==6;put(path,text.encode(),'CurrentSixMatrixRowsAndCountsOnly')
entry=(R/'artifacts/common-error354-documentation-proposal4/changelog-entry.md').read_text()
entry=entry.replace(' (conditional draft)','')
entry=entry.replace('Canonical full validation remains pending in this draft.','The separately accepted canonical full passes 7,342/7,342 tests, eleven integration outcomes, two packages and twelve bounded child controls; all 7,313 previous ordered names and 29 exact appended names are retained.')
entry=entry.replace('Final classification, full execution, package/readiness generation and publication require separate accepted evidence.','The full input freezes these classifications and 114 current source guards. After independent full acceptance, restore only the private registry customLimits and run the already-built Tool readiness against the actual report; actual readiness confirms 587 registrations and 16/30/541. Original comparisons, native loading/ABI compatibility, performance and same-head CI remain pending publication.')
entry=entry.replace('The proposed classification keeps','The source-reviewed classification keeps').replace('Proposed totals are','Totals are')
assert 'conditional draft' not in entry and 'remain pending in this draft' not in entry
entry += '\nRaw evidence, source/parent bindings and all failed attempts are archived in [part 354](../validation/part-354/). Managed NIF backend checks do not execute the native ABI or original OTP.\n'
raw=(R/'docs/changelog/0055.md').read_bytes();combined=raw+b'\n'+entry.encode();assert len(combined)<=32768 and len(combined.splitlines())<=400
put('docs/changelog/0055.md',combined,'AppendOnlyCurrentVolume')
idx=(R/'CHANGELOG.md').read_bytes();old=b'parts351\xe2\x80\x93353 (active)';assert idx.count(old)==1;put('CHANGELOG.md',idx.replace(old,b'parts351\xe2\x80\x93354 (active)'),'CurrentVolumeLabelOnly')
# No post-full mutation of README/OTP-PORTS or any other package source.
for row in plan['PackageResources']:
    if row['Path'] in payload:assert HB(payload[row['Path']])==row['Sha256']
assert payload['docs/OTP-PORTS.md']==(candidate/'docs/OTP-PORTS.md').read_bytes()
assert (R/'README.md').read_bytes()==(candidate/'README.md').read_bytes()
out=v.output.resolve();assert out.is_relative_to(R/'artifacts') and not out.exists();out.mkdir()
files=[]
for target,data in sorted(payload.items()):
    q=out/'overlay'/safe(target);q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data)
    parent=R/target;files.append({'SourcePath':str(q),'TargetPath':target,'Bytes':len(data),'Sha256':HB(data),'ParentSha256':H(parent) if parent.exists() else None,'PublicationMode':modes[target]})
receipt={'Status':'ACTUAL_ARTIFACT_ONLY_FINAL354_DELIVERY_OVERLAY_NOT_MAIN_APPLIED','ParentHead':parents['ParentHead'],'IndependentFullGuardSha256':H(gpath),'ReadinessCopyPlanSha256':H(rpath),'CompositionPlanSha256':PLAN_SHA,'SourceHelperSha256':H(Path(__file__)),'Files':files,'ChangelogBytes':len(combined),'ChangelogLines':len(combined.splitlines()),'Counts':[16,30,541],'RuntimeSourceGuards':114,'FullPassed':7342,'FullFailed':0,'IntegrationOutcomes':11,'PackageOutputs':2,'TermOrderChildren':12,'SourceClosureRows':3207,'OwnedInputPaths':128,'NoMainSdkGitNetworkMutation':True,'RawGeneratedReadinessPreserved':True,'Package6UnchangedFromFull':True,'PublicationMetadataChangesRequireNoNativeReseal':True}
(out/'copy-plan.json').write_bytes(B(receipt));print(H(out/'copy-plan.json'))
