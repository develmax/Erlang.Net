import pathlib, json, hashlib, struct, zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
D = ROOT / 'artifacts/spawn-protocol-nif-290-38688662194-terminal'
O = D / 'artifact-11952131310/nif-original-observations'
def sha(b): return hashlib.sha256(b).hexdigest().upper()
def load(p): return json.loads(p.read_text(encoding='utf-8-sig'))
class Atom(str): pass
def etf(p):
    b=p.read_bytes(); i=1
    assert b[0]==131
    def take(n):
        nonlocal i
        x=b[i:i+n]; assert len(x)==n; i+=n; return x
    def number(n): return int.from_bytes(take(n),'big')
    def term():
        tag=number(1)
        if tag==97: return number(1)
        if tag==98: return int.from_bytes(take(4),'big',signed=True)
        if tag in (100,118,115,119): return Atom(take(number(1 if tag in (115,119) else 2)).decode('utf-8' if tag in (118,119) else 'latin1'))
        if tag in (104,105): return tuple(term() for _ in range(number(1 if tag==104 else 4)))
        if tag==106: return []
        if tag==107: return list(take(number(2)))
        if tag==108:
            values=[term() for _ in range(number(4))]; tail=term(); assert tail==[]; return values
        if tag==109: return take(number(4))
        if tag in (110,111):
            n=number(1 if tag==110 else 4); sign=number(1); return (-1 if sign else 1)*int.from_bytes(take(n),'little')
        if tag==116: return [(term(),term()) for _ in range(number(4))]
        raise AssertionError(('unsupported ETF tag',tag,i))
    value=term(); assert i==len(b); return value
def a(s): return Atom(s)
def exception(t, reason):
    assert type(t)==tuple and len(t)==4 and t[:3]==(a('exception'),a('error'),a(reason))
    assert all(type(x) is Atom for x in t[:3]) and type(t[3]) is list and t[3]
    return t[3]
manifest=load(D/'download-manifest.json')
for r in manifest:
    p=D/r['Path']; b=p.read_bytes(); assert len(b)==r['Bytes'] and sha(b)==r['Sha256']
for ident in (11951535100,11952131310):
    with zipfile.ZipFile(D/f'artifact-{ident}.zip') as z:
        assert len(z.namelist())==len(set(z.namelist()))
        for info in z.infolist():
            if not info.is_dir(): assert z.read(info)==(D/f'artifact-{ident}'/info.filename).read_bytes()
run=load(D/'run.json'); jobs=load(D/'jobs.json')['jobs']
assert run['id']==38688662194 and run['head_sha']=='f2e861afc3f1ff2bdfade705ecb058e7cef433d6' and run['conclusion']=='success'
assert {j['id'] for j in jobs}=={116269261154,116269261420} and all(j['conclusion']=='success' for j in jobs)
r=load(O/'receipt.json'); assert r['Complete'] and not r['SemanticAssertionsExecuted']
assert r['PinnedCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
assert sha((O/'fixture.c').read_bytes())==r['FixtureSourceSha256']=='7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074'
for f in r['Files']:
    b=(O/f['Path']).read_bytes(); assert len(b)==f['Bytes'] and sha(b)==f['Sha256']
assert sha((O/'fixture.so').read_bytes())==r['FixtureSharedLibrarySha256']
assert sha((O/'source-plan.json').read_bytes())==r['PlanSha256']=='49EB3B6C0F841122C04AE3A30BE19C7E606B92549F9B3DE03BC7BD708AC7639C'
plan=load(O/'source-plan.json')
for f in plan['Sources']: assert sha((O/f['Name']).read_bytes())==f['Sha256']
for f in plan['Headers']: assert sha((O/'headers'/f['Name']).read_bytes())==f['Sha256']
results={}
for case in r['OrderedFreshVmCases']:
    p=load(O/case/'observation.process.json'); assert p['ExitCode']==0 and not p['TimedOut'] and p['PipesClosed'] and p['ParentReaped']
    t=etf(O/case/'result.etf'); assert len(t)==6 and t[:2]==(a('nif_original_observation'),a(case))
    assert all(type(x) is Atom for x in t[:2])
    assert t[2]==(a('load_info'),17 if case=='load_veto' else 42)
    assert t[3][0]==a('before_load'); before=exception(t[3][1],'not_loaded'); assert before[0][:2]==(a('erlang'),a('nif_error'))
    assert t[4][0]==a('load_result') and t[5][0]==a('call_result')
    if case=='load_veto':
        assert t[4][1]==(a('return'),(a('error'),(a('load'),list(b'Library load-call unsuccessful (1).'))))
        exception(t[5][1],'not_loaded')
    else:
        assert t[4][1]==(a('return'),a('ok')) and type(t[4][1][1]) is Atom
        if case.endswith('badarg'):
            stack=exception(t[5][1],'badarg'); assert stack[0][:2]==(a('memory_allocator_nif_fixture'),a('increment'))
        else:
            expected={'increment_zero':1,'increment_negative':0,'increment_maximum_success':2147483647,'greeting':list(b'native hello')}[case]
            assert t[5][1]==(a('return'),expected)
    results[case]={'Passed':True,'RawEtfSha256':sha((O/case/'result.etf').read_bytes())}
C=D/'artifact-11952131310/nif-process-capture-checks'
controls=load(C/'receipt.json'); assert controls['Passed']==['raw-exit-3-preserved','owned-group-inherited-pipe-bounded'] and controls['InheritedPipeElapsedSeconds']<15
raw=load(C/'raw-exit.process.json'); assert raw['ExitCode']==3 and not raw['TimedOut'] and raw['PipesClosed'] and raw['ParentReaped']
assert (C/'raw-exit.stdout').read_bytes()==b'raw-out\n' and (C/'raw-exit.stderr').read_bytes()==b'raw-err\n'
raw=load(C/'inherited-pipe.process.json'); assert raw['TimedOut'] and raw['PipesClosed'] and raw['ParentReaped']
assert (C/'inherited-pipe.stdout').read_bytes()==b'parent-exited\n'
metadata=etf(O/'metadata.etf'); assert metadata[0]==a('nif_original_metadata')
fields=dict(metadata[1:]); assert fields[a('otp_release')]==list(b'29') and fields[a('erts_version')]==list(b'17.1') and fields[a('wordsize')]==8
assert fields[a('system_architecture')]==list(b'x86_64-pc-linux-gnu') and fields[a('emulator_type')]==a('jit')
provenance=load(D/'artifact-11952131310/nif-otp-build-provenance.json')
assert provenance['SourceHead']==r['PinnedCommit'] and len(provenance['Files'])==4
for tool in r['Tools'][:2]:
    assert any(tool['Path'].endswith(record['Path']) and tool['Sha256']==record['Sha256'] for record in provenance['Files'])
buildlog=(D/'artifact-11952131310/nif-otp-build.log').read_text()
joblog=(D/'job-116269261420.log').read_text()
assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in buildlog and 'make install' in buildlog and '--prefix=' in buildlog
assert '--approved-plan-sha256 49EB3B6C0F841122C04AE3A30BE19C7E606B92549F9B3DE03BC7BD708AC7639C' in joblog
out={'Status':'EXACT_HEAD_ORIGINAL_NIF_SEVEN_TYPED_ETF_AND_PROCESS_CONTROLS_PASS','Head':run['head_sha'],'Run':run['id'],'LinuxJob':116269261420,'LinuxArtifact':11952131310,'DownloadMembersVerified':len(manifest),'ReceiptFilesVerified':len(r['Files']),'OriginalCases':results,'ProcessControls':controls,'WindowsGuard':'spawn-protocol-nif290-windows-independent-guard.json','NativeUnloadCounterCompared':False,'GenericNifSupport':False,'SdkLoadNifImplemented':False,'SemanticScope':'Independent typed original observation review; fixture host managed/native assertions are separate, not a general SDK NIF differential run.'}
(ROOT/'artifacts/nif-proof290-independent-guard.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({'PassedCases':len(results),'ReceiptFiles':len(r['Files']),'DownloadMembers':len(manifest)}))
