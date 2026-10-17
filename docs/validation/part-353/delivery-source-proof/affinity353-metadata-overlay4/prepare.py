import pathlib, json, hashlib, copy, re, collections

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = pathlib.Path(__file__).resolve().parent
OVERLAY = OUT / 'overlay'
BASE = ROOT / 'artifacts/affinity353-compatibility-rebase-plan'
PERM = ROOT / 'artifacts/scheduler-affinity353-permanent-proposal2'
CANDIDATE = ROOT / 'artifacts/scheduler-affinity353-candidate'
PARENT = '4c9cb16e7c52bc10e54ceaf652797d100def488b'
def sha(b): return hashlib.sha256(b).hexdigest().upper()
def load(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def serial(x): return (json.dumps(x, ensure_ascii=False, indent=2)+'\n').encode()
def write(path, b):
    p = OVERLAY / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(b)
    return p
def key(r): return (r['Module'], r['Function'], r['Arity'])

freeze = load(PERM/'source-freeze.json')
assert sha((PERM/'source-freeze.json').read_bytes()) == '6D719CDE2635B96E3D95C6BC4A2F7039D80A8B6DE422667D91212E40B30EF0C3'
runtime = {r['Path']: r for r in freeze['Files'] if r['Path'].startswith('src/Erlang.Runtime/')}
assert len(runtime) == 20
for path, row in runtime.items():
    b = (PERM/'source'/path).read_bytes()
    assert len(b) == row['Bytes'] and sha(b) == row['Sha256']
    assert b == (CANDIDATE/path).read_bytes()
token_path = ROOT/'artifacts/scheduler-affinity353-permanent-style2/token-review.json'
assert sha(token_path.read_bytes()) == freeze['RoslynTokenReviewSha256']
guards = load(BASE/'proposed-source-guards.json')['Files']
for row in guards:
    p = row['Path']
    b = (PERM/'source'/p).read_bytes() if p in runtime else (ROOT/p).read_bytes()
    row['Sha256'] = sha(b)
assert len(guards) == len({r['Path'] for r in guards}) == 89

ledger_before = load(ROOT/'docs/mfa-compatibility.json')
ledger = copy.deepcopy(ledger_before)
ledger['VerifiedSourceFiles'] = guards
review_public = 'docs/validation/part-353/compatibility-source-review/report.json'
for row in ledger['Entries']:
    if row['Module'] == 'erlang' and row['Function'] == 'spawn_opt' and row['Arity'] in [2,3,4,5]:
        assert row['Status'] == 'Known differences'
        row['Scope'] = ('Existing local and connected spawn paths implement low/normal/high/max process scheduling priority and logical scheduler binding. Heap/GC tuning and message_queue_data remain source-proven valid OTP refusals, so the full option contract remains Known differences. Logical lanes do not certify native scheduler timing, throughput or physical CPU pinning.')
        row['Limits'] = [
          'SPAWN_VALID_TUNING_REJECTED: valid message_queue_data on_heap/off_heap, min_heap_size, min_bin_vheap_size, fullsweep_after and max_heap_size remain unsupported. Process scheduling priority and logical {scheduler,N} binding are implemented source slices. Historical evidence retains its original historical scope.',
          'Current styled-source preservation review and prior managed focus do not establish complete spawn_opt compatibility. Final part 353 full validation and explicit two-lane original comparison remain pending; original part 351 covers its three priority witnesses only.'
        ]
        row['Evidence'].append({'Kind':'SourceReview','Path':review_public.removeprefix('docs/'),'Sha256':'PENDING','Scope':'Current logical scheduler binding replaces the prior current refusal description; remaining concrete tuning refusals keep Known differences. No new Verified promotion.'})
for old,new in zip(ledger_before['Entries'],ledger['Entries']):
    assert key(old)==key(new) and old['Status']==new['Status']
    if old['Status']=='Verified compatible': assert old==new
    elif old!=new: assert old['Function']=='spawn_opt' and old['Evidence']==new['Evidence'][:-1]
assert collections.Counter(r['Status'] for r in ledger['Entries']) == {'Verified compatible':16,'Known differences':24,'Needs verification':547}

registry_before = load(ROOT/'docs/supported-mfas.json')
registry = copy.deepcopy(registry_before)
proposals = load(BASE/'registry-limits-proposals.json')['Rows']
for proposal in proposals:
    row = next(r for r in registry if key(r)==key(proposal))
    assert row['Limits']==proposal['Before']
    row['Limits']=proposal['Proposed']
for row in registry:
    if row['Module']=='erlang' and row['Function']=='spawn_request' and row['Arity'] in [2,3,4,5]:
        assert 'unsupported priority/allocator options' in row['Limits']
        row['Limits']=row['Limits'].replace('unsupported priority/allocator options','unsupported heap/GC tuning and message_queue_data options')
        row['Limits']=row['Limits'].replace('This does not implement +pad defaults, connected remote spawning, unsupported heap/GC tuning and message_queue_data options, pending/autoconnect or full native protocol/scheduler parity; current original326 execution remains pending.', 'Existing connected remote spawning is implemented. Remaining limitations include +pad defaults, unsupported heap/GC tuning and message_queue_data options, pending/autoconnect and complete native remote protocol/scheduler compatibility. The original326 pending gate is historical; completed original351 preserves the accumulated corpus and covers its bounded priority witnesses, without proving the full remote/protocol option domain.')
        assert 'connected remote spawning, unsupported' not in row['Limits'] and 'current original326 execution remains pending' not in row['Limits']
        row['Limits']+=' Current source implements process scheduling priority and logical {scheduler,N} binding on existing creator/request and inbound connected paths before child publication. This corrects the historical priority-refusal wording; whole spawn_request option/protocol parity and native scheduler performance remain unverified.'
changed_registry=[]
for a,b in zip(registry_before,registry):
    assert {k:v for k,v in a.items() if k!='Limits'}=={k:v for k,v in b.items() if k!='Limits'}
    if a['Limits']!=b['Limits']: changed_registry.append({'Module':b['Module'],'Function':b['Function'],'Arity':b['Arity'],'Before':a['Limits'],'After':b['Limits']})
assert len(changed_registry)==13 and len(registry)==587

otp_before=(ROOT/'docs/OTP-PORTS.md').read_bytes()
otp_header,otp_tail=otp_before.split(b'\n\n',1) if b'\r\n' not in otp_before else otp_before.split(b'\r\n\r\n',1)
otp_sep=b'\r\n' if b'\r\n' in otp_before else b'\n'
otp_entry=(OUT/'adaptation-entry.txt').read_bytes().replace(b'\r\n',b'\n').replace(b'\n',otp_sep)
otp_after=otp_header+otp_sep*2+otp_entry+otp_tail
assert otp_after.endswith(otp_tail) and otp_header==otp_before.split(otp_sep*2,1)[0]
write('docs/OTP-PORTS.md',otp_after)
old_report=load(BASE/'report.json')
report={'Status':'SOURCE_ONLY_STYLED353_RETAINED16_REVIEW_AND_PREFULL_METADATA_REQUIRES_ROOT_PEER_AND_ACTUAL_FULL',
 'ParentHead':PARENT,'PermanentSourceFreezeSha256':sha((PERM/'source-freeze.json').read_bytes()),
 'RoslynTokenReviewSha256':sha(token_path.read_bytes()),'Runtime20':list(runtime.values()),
 'AdaptationRegister':{'Path':'docs/OTP-PORTS.md','ParentSha256':sha(otp_before),'ProposedSha256':sha(otp_after),'PriorBodyBytesExact':True,'PackageResourceMustBindNewHashBeforeFull':True,'Date':'2026-10-17','Scope':'Source-guided CLR adaptation register; no future full/native/performance claim'},
 'PriorSourceReviewSha256':sha((BASE/'report.json').read_bytes()),
 'RetainedContractArgument':old_report['RetainedContractArgument'],
 'StyledDeltaScope':'Four runtime rows changed layout only according to the bound Roslyn token review; all twenty actual styled byte hashes are re-read. Primitive delegates and guard/import/matchspec paths retain the prior source review argument; scheduler lanes preserve process ownership and captured execution context.',
 'Verified16EntriesScopesLimitsPriorEvidenceExact':True,'Statuses':{'Verified':16,'Known':24,'Needs':547},
 'VerifiedSourceFiles':guards,'RegistryLimitsChanges13':changed_registry,
 'UnchangedRegistryNonLimits587':True,'OtherRegistryLimits574Exact':True,
 'Full353':'Pending. 7313 is planned arithmetic 7297+11 HOST+5 compiled, not an observed result.',
 'NativeAffinityProfile':'Pending explicit two-lane original profile; no performance/physical CPU pinning or whole option compatibility claim.',
 'PriorActualEvidence':old_report['ActualManagedFocus'],'Archive66Plan':old_report['OriginalArchive66Plan'],
 'MainCandidateSdkNetworkGitMutated':False}
report_bytes=serial(report)
write(review_public,report_bytes)
for row in ledger['Entries']:
    if row['Module']=='erlang' and row['Function']=='spawn_opt' and row['Arity'] in [2,3,4,5]: row['Evidence'][-1]['Sha256']=sha(report_bytes)
write('docs/mfa-compatibility.json',serial(ledger))
write('docs/supported-mfas.json',serial(registry))
for source,target in [(BASE/'report.json','prior-source-review.json'),(PERM/'source-freeze.json','styled-source-freeze.json'),(token_path,'styled-token-review.json')]:
    write('docs/validation/part-353/compatibility-source-review/'+target,source.read_bytes())
for path in runtime: write('docs/validation/part-353/compatibility-source-review/source/'+path,(PERM/'source'/path).read_bytes())

attrs=(ROOT/'.gitattributes').read_bytes()
newline=b'\r\n' if b'\r\n' in attrs else b'\n'
for pattern in ['docs/validation/part-353/** binary','docs/validation/original351-completion/** binary','docs/validation/corrected352-ci-completion/** binary','tools/Erlang.SchedulerAffinityOriginalObservations/** -text whitespace=trailing-space,space-before-tab,cr-at-eol','.github/workflows/scheduler-affinity-original.yml text eol=lf']:
    assert pattern.encode() not in attrs
    if not attrs.endswith(b'\n'): attrs+=newline
    attrs+=pattern.encode()+newline
write('.gitattributes',attrs)

matrix=(ROOT/'docs/ERLANG_COMPATIBILITY.md').read_bytes()
text=matrix.decode(); sep='\r\n' if '\r\n' in text else '\n'
head,table=text.split('| Export | Current state |',1)
lookup={f"{r['Function']}/{r['Arity']}":r for r in ledger['Entries'] if r['Module']=='erlang'}
counts=collections.Counter(r['Status'] for r in lookup.values())
assert counts=={'Verified compatible':16,'Needs verification':328,'Known differences':7}
labels={'Verified compatible':'100% compatible','Needs verification':'Needs verification','Known differences':'Known differences'}
seen=[]; lines=table.splitlines(keepends=True)
for i,line in enumerate(lines):
    match=re.fullmatch(r'\| `(.+)` \| .* \|(?:\r?\n)?',line)
    if match:
        name=match.group(1); assert name in lookup
        lines[i]=f'| `{name}` | {labels[lookup[name]["Status"]]}; see [per-MFA evidence](API_COMPATIBILITY.md) |'+sep
        seen.append(name)
assert len(seen)==len(set(seen))==351
heading=('Current per-command compatibility: **16 commands 100% compatible, 328 need verification, 7 have known differences**. See [API compatibility and evidence](API_COMPATIBILITY.md). This current matrix follows the ledger; API registration alone is not a compatibility claim. Historical paragraphs above retain their original scope and counts.'+sep+sep)
write('docs/ERLANG_COMPATIBILITY.md',(head+heading+'| Export | Current state |'+''.join(lines)).encode())

def resolved(path):
    p=OVERLAY/path
    if p.exists(): return p.read_bytes()
    if path in runtime: return (PERM/'source'/path).read_bytes()
    return (CANDIDATE/path).read_bytes() if (CANDIDATE/path).exists() else (ROOT/path).read_bytes()
def policy(b,mode):
    assert mode in ['TextLF','BinaryExact']
    return b.replace(b'\r\n',b'\n') if mode=='TextLF' else b
plans=[('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1262,1268),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1287,1293)]
native=[]
for path,workflow,seal,before,after in plans:
    plan=load(ROOT/path); assert len(plan['Files'])==before
    existing={r['Path'] for r in plan['Files']}
    new=sorted(set(runtime)-existing); assert len(new)==6
    for p in new: plan['Files'].append({'Path':p,'Bytes':0,'Sha256':'','HashMode':'TextLF'})
    plan['Files'].sort(key=lambda r:r['Path'])
    for row in plan['Files']:
        b=policy(resolved(row['Path']),row['HashMode']);row['Bytes']=len(b);row['Sha256']=sha(b)
    assert len(plan['Files'])==after
    b=serial(plan);write(path,b)
    workflow_bytes=(ROOT/workflow).read_bytes().replace(b'\r\n',b'\n')
    pattern=rb'('+seal.encode()+rb': )[A-F0-9]{64}'
    workflow_bytes,n=re.subn(pattern,lambda m:m.group(1)+sha(b).encode(),workflow_bytes);assert n==1
    write(workflow,workflow_bytes)
    native.append({'Path':path,'Rows':after,'Sha256':sha(b),'Workflow':workflow,'WorkflowSha256':sha(workflow_bytes)})
for path,*_ in plans:
    for row in load(OVERLAY/path)['Files']:
        b=policy(resolved(row['Path']),row['HashMode']);assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']

archive=load(ROOT/'artifacts/original351-and-ci352-archive-proposal/copy-plan.json')['Files']
rows=[]
for p in sorted(OVERLAY.rglob('*')):
    if p.is_file():
        target=p.relative_to(OVERLAY).as_posix();b=p.read_bytes();parent=ROOT/target
        rows.append({'SourcePath':p.relative_to(ROOT).as_posix(),'TargetPath':target,'Bytes':len(b),'Sha256':sha(b),'ParentSha256':sha(parent.read_bytes()) if parent.exists() else None})
for row in archive:
    b=(ROOT/row['SourcePath']).read_bytes();assert sha(b)==row['Sha256'] and len(b)==row['Bytes']
    rows.append({**row,'ParentSha256':sha((ROOT/row['TargetPath']).read_bytes()) if (ROOT/row['TargetPath']).exists() else None})
assert len(rows)==len({r['TargetPath'] for r in rows})
plan={'Status':'ARTIFACT_ONLY_REVIEWABLE353_PREFULL_OVERLAY_NO_APPLICATION','ParentHead':PARENT,'Files':rows,'FilesCount':len(rows),'NativePlans':native,'GuardCount':89,'Counts':{'Verified':16,'Known':24,'Needs':547},'NoMainCandidateSdkNetworkGitMutation':True}
(OUT/'copy-plan.json').write_bytes(serial(plan))
(OUT/'source-freeze.json').write_bytes(serial({'Status':'SOURCE_ONLY_REQUIRES_INDEPENDENT_PEER','PrepareSha256':sha(pathlib.Path(__file__).read_bytes()),'CopyPlanSha256':sha((OUT/'copy-plan.json').read_bytes()),'ReportSha256':sha(report_bytes),'PermanentFreezeSha256':sha((PERM/'source-freeze.json').read_bytes())}))
print(json.dumps({'Files':len(rows),'PlanSha256':sha((OUT/'copy-plan.json').read_bytes()),'FreezeSha256':sha((OUT/'source-freeze.json').read_bytes()),'NativePlans':native}))
