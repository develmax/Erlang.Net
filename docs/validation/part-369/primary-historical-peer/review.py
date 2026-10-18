"""Independent passive comparison of admitted native and retained actual2 SDK bytes."""
from pathlib import Path
import collections, hashlib, importlib.util, json, zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
TOOL = ROOT / 'artifacts/binary-original369-original1/tool'
NATIVE = ROOT / 'artifacts/binary369-original-remote1/run-39083021286/artifact-12119578493/capture'
CAPTURE = ROOT / 'artifacts/binary-integration369-actual2/capture1'
SDK = CAPTURE / 'sdk'
COMPARATOR = ROOT / 'artifacts/binary369-comparison-successor1'

def read(p): return json.loads(p.read_bytes())
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def require(value, message):
    if not value: raise AssertionError(message)

def named(node):
    require(node[0] == 'list', 'named list')
    rows = node[1]
    require(all(x[0] == 'tuple' and len(x[1]) == 2 for x in rows), 'named pairs')
    require(len({x[1][0] for x in rows}) == len(rows), 'duplicate named rows')
    return {x[1][0]:x[1][1] for x in rows}

def pairwise(left, right, mapping=None):
    pending = [(left, right)]
    count = 0
    while pending:
        a, b = pending.pop()
        if isinstance(a, tuple) and a and a[0] == 'reference':
            if mapping is None:
                if a != b: return False, count
            elif a != mapping[0] or b != mapping[1]:
                return False, count
            count += 1
        elif isinstance(b, tuple) and b and b[0] == 'reference':
            return False, count
        elif isinstance(a, tuple) and isinstance(b, tuple):
            if len(a) != len(b): return False, count
            pending.extend(zip(a, b))
        elif a != b:
            return False, count
    return True, count

def witness(outcome):
    require(outcome[0] == 'tuple' and outcome[1][:3] == (('atom','error'), ('atom','error'), ('atom','badarg')), 'resource error class/reason')
    effects = outcome[1][-1]
    require(effects[0] == 'tuple' and len(effects[1]) == 2 and effects[1][0] == ('list',()), 'resource callback effects')
    records = effects[1][1]
    require(records[0] == 'list' and len(records[1]) == 1, 'one reference provenance')
    record = records[1][0]
    require(record[0] == 'tuple', 'source provenance tuple')
    role = record[1][0]
    if role == ('atom','ordinary'):
        require(len(record[1]) == 2 and record[1][1][0] == 'reference', 'ordinary provenance')
        return record, record[1][1], 'ordinary'
    require(role == ('atom','compiled') and len(record[1]) == 6, 'compiled provenance')
    require(record[1][1] == ('atom','bm') and record[1][2][0] == 'reference', 'genuine tag/reference')
    require(record[1][3][0] == record[1][4][0] == 'binary', 'pattern/subject bytes')
    return record, record[1][2], 'compiled'

def normal(folder, stem):
    path = folder / (stem + '.exit.json')
    data = read(path)
    require(data['ExitCode'] == 0 and data['Settled'] is True and data['TimedOut'] is False and not data['Failures'], 'normal owned process')
    for stream in ['Stdout', 'Stderr']:
        require(sha(folder / (stem + '.' + stream.lower() + '.log')) == data[stream + 'Sha256'], 'process stream hash')
    return dict(Path=str(path), Sha256=sha(path), ElapsedSeconds=data['ElapsedSeconds'], Pid=data['Pid'])

def main():
    for freeze in [COMPARATOR / 'source-seal.json', ROOT / 'artifacts/binary369-original-admission-proposal2/source-freeze.json']:
        for row in read(freeze)['Files']:
            path = freeze.parent / row['Path']
            require(sha(path) == row['Sha256'] and path.stat().st_size == row['Bytes'], 'trusted reviewer source identity')
    admission_path = ROOT / 'artifacts/binary369-original-admission-proposal2/actual1/acceptance.json'
    require(sha(admission_path) == 'DAFEA0BFAD08C1BE2975C81D342C729B816717D5BA213FD6710BDE4EE6301FF6', 'native exact admission')
    process = [normal(CAPTURE, 'build'), normal(CAPTURE, 'binary31-capture'), normal(ROOT / 'artifacts/binary-integration369/compare2', 'execution')]
    closure = read(CAPTURE / 'main-dll-closure.json')
    require(closure['MainSource'] is True and closure['PrivateCandidateDllsUsed'] is False and len(closure['Files']) == 5, 'actual Main five DLLs')
    for row in closure['Files']:
        require(sha(ROOT / row['CopiedTo']) == row['Sha256'], 'retained actual DLL bytes')
    runtime = next(x['Sha256'] for x in closure['Files'] if x['Path'].endswith('/Erlang.Runtime.dll'))
    terms = next(x['Sha256'] for x in closure['Files'] if x['Path'].endswith('/Erlang.Terms.dll'))
    source = read(CAPTURE / 'harness-source-closure.json')['Files']
    for row in source:
        require(sha(COMPARATOR / 'harness' / row['Path']) == row['Sha256'], 'executed observer source')
        require(sha(CAPTURE.parent / 'main-capture' / row['Path']) == row['Sha256'], 'retained observer source')
    for row in read(SDK / 'capture-seal.json')['Files']:
        require(sha(SDK / row['Path']) == row['Sha256'] and (SDK / row['Path']).stat().st_size == row['Bytes'], 'SDK file seal')
    require(sha(TOOL / 'source-inputs.corrected.json') == 'A54B3C119311C183D635FD8112507E979FE574031584240D78639BF637A4CBC0', 'tool manifest')
    for row in read(TOOL / 'source-inputs.corrected.json')['ToolInputs']:
        require(sha(TOOL / row['Path']) == row['Sha256'], 'trusted fixture/tool source')
    decoder_path = TOOL / 'passive_etf_family.py'
    spec = importlib.util.spec_from_file_location('binary_peer_trusted_decoder', decoder_path)
    decoder = importlib.util.module_from_spec(spec); spec.loader.exec_module(decoder)
    profile = read(TOOL / 'profile.json')
    completion = read(SDK / 'completion.json')
    require(completion['FailedGroups'] == 0 and len(completion['Groups']) == 8, 'all SDK groups captured')
    eligible = {(r['Group'],r['Name']) for r in profile['OpaquePolicy']['EligibleCases']}
    require(len(eligible) == 6, 'six predeclared cases')
    rows, pairs, inputs = [], [], []
    archive_path = NATIVE.parent.parent / 'artifact-12119578493.zip'
    require(sha(archive_path) == '623A8B96C5DEB37D8C1A24521CF7EE327B9C229331BD8923AFD9F1C5446BAD74', 'native original archive')
    with zipfile.ZipFile(archive_path) as archive:
        for fixture, status in zip(profile['Fixtures'], completion['Groups']):
            gid = fixture['Id']
            require(status['Id'] == gid and status['ExecutionSkipped'] is False and status['Status'] == 'CAPTURED_PENDING_PASSIVE_CHECKS', 'SDK group complete')
            expected_sources = [fixture['Path'],*fixture['Companions']]
            require(len(status['Compile']) == len(expected_sources), 'compile inventory')
            for source_path, compiled in zip(expected_sources,status['Compile']):
                require(compiled.get('Passed') is True and 'Error' not in compiled and compiled['SourceSha256'] == sha(TOOL / source_path) and compiled['DeclaredSourceFile'] == Path(source_path).name, 'same source/basename compiled')
            binding_path = SDK / gid / 'binary-reference-observations.json'
            binding = read(binding_path)
            require(binding['Group'] == gid and binding['RuntimeAssemblySha256'] == runtime and binding['TermsAssemblySha256'] == terms and binding['RegistryReadWhileRuntimeAlive'] is True, 'live own DLL/registry binding')
            require(binding['ReturnedSha256'] == sha(SDK / gid / 'returned.etf') and binding['StacksSha256'] == sha(SDK / gid / 'stack-observations.etf'), 'proof raw bytes')
            bound = {(r['Case'],r['Depth']):r for r in binding['Observations']}
            expected = {(name,depth) for group,name in eligible if group == gid for depth in ['values','default','0','1','2','8']}
            require(len(bound) == len(binding['Observations']) and set(bound) == expected, 'exact live binding inventory')
            names = [('atom',n) for n in fixture['Names']]
            for filename in ['returned.etf','stack-observations.etf']:
                native_path, sdk_path = NATIVE / gid / filename, SDK / gid / filename
                require(archive.read(native_path.relative_to(NATIVE.parent).as_posix()) == native_path.read_bytes(), 'native raw same ZIP bytes')
                inputs += [dict(Path=str(p),Sha256=sha(p)) for p in [native_path,sdk_path,binding_path]]
                left,right = named(decoder.decode_exact(native_path.read_bytes())),named(decoder.decode_exact(sdk_path.read_bytes()))
                if filename == 'returned.etf':
                    require(list(left) == list(right) == names, 'value names/order')
                    observations = [('values',name,left[name],right[name]) for name in names]
                else:
                    depths = [('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
                    require(list(left) == list(right) == depths, 'five stack depth labels')
                    observations = []
                    for depth in depths:
                        a,b = named(left[depth]),named(right[depth]); require(list(a) == list(b) == names, 'profile names/order')
                        observations += [(str(depth[1]),name,a[name],b[name]) for name in names]
                for depth,name,a,b in observations:
                    mapping = None
                    if (gid,name[1]) in eligible:
                        native_record,native_ref,native_role = witness(a)
                        own_record,own_ref,own_role = witness(b)
                        proof = bound[(name[1],depth)]
                        require(proof['Group'] == gid and proof['Role'] == native_role == own_role and proof['LocalIdentityBound'] is True, 'same genuine source role/runtime')
                        require(decoder.decode_exact(bytes.fromhex(proof['ReferenceEtfHex'])) == own_ref and decoder.decode_exact(bytes.fromhex(proof['ProvenanceEtfHex'])) == own_record, 'actual own reference/provenance ETF')
                        if own_role == 'compiled':
                            require(proof['RegistryAcquired'] is True and proof['ResourceKind'] == 'BinarySearchPattern' and proof['PayloadTag'] == 'bm', 'actual lease kind and payload tag')
                            require(decoder.decode_exact(bytes.fromhex(proof['ReplayedMatchEtfHex'])) == own_record[1][5], 'actual same-resource match replay')
                        else:
                            require(proof['RegistryAcquired'] is False and proof['ResourceKind'] is None and proof['PayloadTag'] is None and proof['ReplayedMatchEtfHex'] is None, 'ordinary reference not resource')
                        mapping = native_ref,own_ref
                        require(pairwise(native_record,own_record,mapping)[0], 'all nonidentity provenance fields')
                    equal,occurrences = pairwise(a,b,mapping)
                    require(equal, 'whole-field observation differs')
                    rows.append(dict(Group=gid,Case=name[1],Depth=depth,StrictEqual=a == b,Passed=True))
                    if mapping is not None:
                        pairs.append(dict(Group=gid,Case=name[1],Depth=depth,Role=own_role,OriginalReference=repr(native_ref),OwnReference=repr(own_ref),PairedOccurrences=occurrences,ActualRegistryEvidence=True))
    require(len(rows) == 3510 and sum(x['StrictEqual'] for x in rows) == 3474 and len(pairs) == 36, 'complete actual2 equality counts')
    claimed = read(ROOT / 'artifacts/binary-integration369-actual2/comparison1/comparison.json')
    require(claimed['Status'] == 'PASS_WHOLE_FIELDS_WITH_SOURCE_BOUND_OWN_REFERENCES' and not claimed['Failures'] and claimed['StrictEqual'] == 3474 and claimed['SourceBoundReferenceRows'] == 36, 'Root report agrees')
    controls = read(COMPARATOR / 'controls.json'); require(controls['Count'] == 18 and all(x['Passed'] is True for x in controls['Controls']), '18 reviewed passive corruption controls')
    result = dict(Status='PASS_INDEPENDENT_PASSIVE_PRIMARY_ACTUAL2_WHOLE_FIELDS', Observations=len(rows),StrictEqual=3474,ActualSourceBoundReferencePairs=len(pairs),OrdinaryPairs=sum(x['Role']=='ordinary' for x in pairs),CompiledResourcePairs=sum(x['Role']=='compiled' for x in pairs),Pairs=pairs,OwnedProcesses=process,RetainedActualDlls=closure['Files'],ReviewedComparatorControls=18,Inputs=inputs,NativeAdmissionSha256=sha(admission_path),RootComparisonSha256=sha(ROOT / 'artifacts/binary-integration369-actual2/comparison1/comparison.json'),Projection=False,Normalization=False,MainClassificationChanged=False,Scope='Primary585 only; supplemental36/backing optimizer closure/full canonical regression remain separate gates.')
    (OUT / 'acceptance.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:result[k] for k in ['Status','Observations','StrictEqual','ActualSourceBoundReferencePairs','OrdinaryPairs','CompiledResourcePairs']}))

if __name__ == '__main__': main()
