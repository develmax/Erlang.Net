"""Shared fail-closed gates. Configuration and evidence are supplied by root after full."""
from pathlib import Path
import hashlib, json, subprocess

ROOT = Path.cwd().resolve()
OUT = ROOT / 'docs/validation/part-306'
CLOSED45 = '9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6'
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(Path(p).read_bytes())
git = lambda *args: subprocess.check_output(['git', *args], cwd=ROOT)

def safe(relative):
    p = (ROOT / relative).resolve()
    assert p.is_relative_to(ROOT) and not {'.git','bin','obj','__pycache__'}.intersection(p.parts)
    assert not any(part.lower().startswith('license') or part.lower() in {'cache','.cache','__pycache__'} for part in p.relative_to(ROOT).parts)
    return p

def pinned(pin):
    p = safe(pin['Path']); data = p.read_bytes()
    assert len(data) == pin['Bytes'] and sha(data) == pin['Sha256'], str(p)
    return p

def gates(config):
    c = read(config)
    assert c['Part'] == 306 and c['ParentHead'] == '0494375635061b4952d40a3c7667234db321a931'
    assert git('rev-parse','HEAD').decode().strip() == c['ParentHead']
    assert c['Expected'] == {'Passed':6627,'Failed':0,'Registry':504,'OptionalIo':506,'Erlang':346,'ErlangReference':351,'NativeInputs':1012,'MfaDelta':1}
    f = read(pinned(c['SourceFreeze']))
    assert f['ParentHead'] == c['ParentHead'] and len(f['Files']) == f['SourcePaths'] == c['SourcePaths']
    assert sum(x['Path'].endswith('.cs') for x in f['Files']) == f['CSharpPaths'] == c['CSharpPaths']
    assert f['TokensExactForCSharp'] and f['ReviewedSourceInputs']==1012
    assert f['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
    assert f['PublicMfaAdded']=='erlang:dist_get_stat/1' and not f['PublicLoadNifActivated']
    assert len({x['Path'] for x in f['Files']}) == len(f['Files'])
    for row in f['Files']: pinned(row)
    assert read(pinned(c['FullExit']))['ExitCode'] == 0
    assert b'Validation passed' in pinned(c['FullLog']).read_bytes()
    tests = read(pinned(c['Tests'])); old = read(pinned(c['PreviousTests']))
    names = [x['Name'] for x in tests['Tests']]; previous = [x['Name'] for x in old['Tests']]
    assert (tests['Passed'],tests['Failed']) == (6627,0)
    assert len(names) == len(set(names)) == 6627 and len(previous) == 6624
    assert [n for n in names if n in set(previous)] == previous
    assert set(names)-set(previous) == set(c['AddedNames']) and len(c['AddedNames']) == 3
    assert set(f['AddedHostNames']) <= set(c['AddedNames']) and len(f['AddedHostNames'])==2
    assert all(x['Status'] == 'Passed' for x in tests['Tests'])
    assert read(pinned(c['Integration'])) == read(pinned(c['PreviousIntegration']))
    assert len(read(pinned(c['Integration']))) == 11
    for label in ['FullGuard','NativeGuard','RegistryGuard','OptionalIoGuard','RuntimePeer','AllocatorPeer','AllocatorControls','InstalledAllocatorControls']:
        guard = read(pinned(c[label]))
        for key,value in c['GuardAssertions'][label].items(): assert guard[key] == value, (label,key)
    for label in ['FullGuard','NativeGuard']:
        guard=read(pinned(c[label]))
        assert guard['SourceHashesExact'] and guard['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
    assert read(pinned(c['NativeGuard']))['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
    registry_guard=read(pinned(c['RegistryGuard']))
    assert registry_guard['RawSha256']==c['RawRegistry']['Sha256'] and registry_guard['RestoredSha256']==c['DerivedRegistry']['Sha256']
    optional=read(pinned(c['OptionalIoGuard']));assert optional['FullTestsSha256']==c['Tests']['Sha256']
    assert any(x['Name']==optional['PassedHost'] and x['Status']=='Passed' for x in tests['Tests'])
    for row in optional['Sources']:assert sha(safe(row['Path']).read_bytes())==row['Sha256']
    plan = read(pinned(c['NativeSourcePlan'])); assert len(plan['Files']) == 1012
    for row in plan['Files']:
        data=safe(row['Path']).read_bytes()
        if row.get('HashMode')=='TextLF': data=data.replace(b'\r\n',b'\n')
        assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
    raw = read(pinned(c['RawRegistry'])); derived = read(pinned(c['DerivedRegistry']))
    key = lambda x:(x['Module'],x['Function'],x['Arity'])
    assert len(raw) == len(derived) == 504 and len({key(x) for x in derived}) == 504
    assert sum(x['Module']=='erlang' for x in derived) == 346
    oldreg = json.loads(git('show',c['ParentHead']+':docs/supported-mfas.json'))
    assert len(oldreg) == 503 and {key(x) for x in derived}-{key(x) for x in oldreg} == {('erlang','dist_get_stat',1)}
    assert [key(x) for x in raw] == [key(x) for x in derived]
    for before,after in zip(raw,derived):
        allowed = key(before) == ('erlang','process_info',1)
        assert before == after or (allowed and {k:v for k,v in before.items() if k!='Limits'} == {k:v for k,v in after.items() if k!='Limits'})
    for row in oldreg: assert row == next(x for x in derived if key(x)==key(row))
    assert sha((ROOT/'docs/changelog/0045.md').read_bytes()) == CLOSED45
    active = pinned(c['ActiveChangelogBefore']).read_bytes()
    assert len(active) <= 32768 and len(active.splitlines()) <= 400
    index = read(pinned(c['EvidenceIndex'])); destinations = set()
    for row in index:
        pinned({'Path':row['SourcePath'],'Bytes':row['Bytes'],'Sha256':row['Sha256']})
        dest = safe(row['Destination']); assert dest.is_relative_to(OUT) and dest not in destinations
        destinations.add(dest)
    reference=next(x for x in index if x['Destination'].endswith('/reused-original279-references.json'))
    reused=read(safe(reference['SourcePath']))
    assert reused['FreshScenarioVMs']==7 and not reused['DuplicateZipCopied']
    for row in reused['Files']:pinned(row)
    for directory in ['distribution-owner306-combined-validated1','nif-zero-export-owner308-validated2']:
        freeze=ROOT/'artifacts'/directory/'validation-freeze.json'
        for row in read(freeze)['Files']:
            source='artifacts/'+directory+'/'+row['Path']
            assert any(x['SourcePath']==source and x['Bytes']==row['Bytes'] and x['Sha256']==row['Sha256'] for x in index),source
    assert c['RequiredEvidenceCategories'] == ['previous305-ci','previous305-native','previous305-original','runtime-candidate','runtime-failures','runtime-peer','allocator-source','allocator-controls','allocator-peer','actual-main']
    assert set(x['Category'] for x in index) >= set(c['RequiredEvidenceCategories'])
    for kind,count in [('ci',12),('native',227),('original',20)]:
        manifest = read(ROOT/f'artifacts/pack305-{kind}-detached-staging-manifest.json')
        assert len(manifest) == count
        for row in manifest:
            assert any(all(item[k]==row[k] for k in ['SourcePath','Destination','Bytes','Sha256']) for item in index)
    assert not c['PublicLoadNifActivated'] and not c['WholeCompatibilityComplete'] and not c['FreshOriginal306Verified']
    return c,f,index
