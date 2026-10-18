"""Passive all-field comparison with hash-bound actual own registry observations."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, zipfile
import reference_relation

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
HEAD = '672e4424ec9f0238be80ae14e6e077018adcb933'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def read(path):
    return json.loads(path.read_bytes())

def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def admit_own(sdk, tool):
    closure = read(sdk.parent / 'main-dll-closure.json')
    assert closure['MainSource'] is True and closure['PrivateCandidateDllsUsed'] is False
    assert len(closure['Files']) == 5
    for row in closure['Files']:
        copied = ROOT / row['CopiedTo']
        assert sha(copied) == row['Sha256']
    runtime = next(x['Sha256'] for x in closure['Files'] if x['Path'].endswith('/Erlang.Runtime.dll'))
    terms = next(x['Sha256'] for x in closure['Files'] if x['Path'].endswith('/Erlang.Terms.dll'))
    source = read(sdk.parent / 'harness-source-closure.json')['Files']
    assert {x['Path'] for x in source} == {p.name for p in (HERE / 'harness').iterdir() if p.is_file()}
    for row in source:
        assert sha(HERE / 'harness' / row['Path']) == row['Sha256']
    completion = read(sdk / 'completion.json')
    assert completion['FailedGroups'] == 0 and completion['Status'] == 'CAPTURED_PENDING_PASSIVE_CHECKS'
    profile = read(tool / 'profile.json')
    assert [x['Id'] for x in completion['Groups']] == [x['Id'] for x in profile['Fixtures']]
    for fixture, group in zip(profile['Fixtures'], completion['Groups']):
        assert group['Status'] == 'CAPTURED_PENDING_PASSIVE_CHECKS' and group['ExecutionSkipped'] is False
        expected = [fixture['Path'], *fixture['Companions']]
        assert len(group['Compile']) == len(expected)
        for path, compiled in zip(expected, group['Compile']):
            assert compiled['Passed'] is True and 'Error' not in compiled
            assert compiled['DeclaredSourceFile'] == Path(path).name
            assert compiled['SourceSha256'] == sha(tool / path)
    seal = read(sdk / 'capture-seal.json')
    for row in seal['Files']:
        path = sdk / row['Path']
        assert path.resolve().is_relative_to(sdk.resolve())
        assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256']
    return runtime, terms

def main():
    p = argparse.ArgumentParser()
    for key in ['native', 'sdk', 'tool', 'output', 'native-admission']:
        p.add_argument('--' + key, type=Path, required=True)
    args = p.parse_args()
    assert not args.output.exists()
    args.output.mkdir(parents=True)
    admission = read(args.native_admission)
    assert admission['Status'] == 'PASS_PASSIVE_BINARY369_ORIGINAL_ADMISSION'
    assert admission['Head'] == HEAD and admission['Run'] == 39083021286
    provenance_path = args.native_admission.parent / 'provenance/acceptance.json'
    assert sha(provenance_path) == admission['ProvenanceSha256']
    admitted = read(provenance_path)
    for group in read(args.tool / 'profile.json')['Fixtures']:
        assert any(Path(row['Directory']).resolve() == (args.native / group['Id']).resolve() for row in admitted['OriginalNormalProcesses'])
    remote = next((parent for parent in args.native.resolve().parents if (parent / 'download-manifest.json').is_file()), None)
    assert remote is not None
    archives = [path for path in remote.glob('artifact-*.zip') if sha(path) == admitted['ArtifactZipSha256']]
    assert len(archives) == 1
    artifact = remote / archives[0].stem
    strict = load(HERE / 'strict-base.py', 'binary_strict')
    manifest = read(args.tool / 'source-inputs.corrected.json')
    assert sha(args.tool / 'source-inputs.corrected.json') == 'A54B3C119311C183D635FD8112507E979FE574031584240D78639BF637A4CBC0'
    for row in manifest['ToolInputs']:
        path = args.tool / row['Path']
        assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256']
    decoder = load(args.tool / 'passive_etf_family.py', 'binary_trusted_decoder')
    profile = read(args.tool / 'profile.json')
    runtime, terms = admit_own(args.sdk, args.tool)
    eligible = {(r['Group'], r['Name']) for r in profile['OpaquePolicy']['EligibleCases']}
    assert len(eligible) == 6
    rows, failures, inputs = [], [], []
    with zipfile.ZipFile(archives[0]) as archive:
        for group in profile['Fixtures']:
            try:
                proof_path = args.sdk / group['Id'] / 'binary-reference-observations.json'
                proof = read(proof_path)
                assert proof['Group'] == group['Id'] and proof['RegistryReadWhileRuntimeAlive'] is True
                assert proof['RuntimeAssemblySha256'] == runtime and proof['TermsAssemblySha256'] == terms
                assert proof['ReturnedSha256'] == sha(args.sdk / group['Id'] / 'returned.etf')
                assert proof['StacksSha256'] == sha(args.sdk / group['Id'] / 'stack-observations.etf')
                observations = {(r['Case'], r['Depth']):r for r in proof['Observations']}
                assert len(observations) == len(proof['Observations'])
                expected_proofs = {(name, depth) for gid, name in eligible if gid == group['Id'] for depth in ['values', 'default', '0', '1', '2', '8']}
                assert set(observations) == expected_proofs
                for filename, stacks in [('returned.etf', False), ('stack-observations.etf', True)]:
                    native = args.native / group['Id'] / filename
                    sdk = args.sdk / group['Id'] / filename
                    assert archive.read(native.resolve().relative_to(artifact.resolve()).as_posix()) == native.read_bytes()
                    inputs += [dict(Path=str(path), Sha256=sha(path)) for path in [native, sdk, proof_path]]
                    left = strict.named(decoder.decode_exact(native.read_bytes()))
                    right = strict.named(decoder.decode_exact(sdk.read_bytes()))
                    names = [('atom', name) for name in group['Names']]
                    if not stacks:
                        assert list(left) == list(right) == names
                        pairs = [('values', name, left[name], right[name]) for name in names]
                    else:
                        depths = [('atom', 'default'), ('integer', 0), ('integer', 1), ('integer', 2), ('integer', 8)]
                        assert list(left) == list(right) == depths
                        pairs = []
                        for depth in depths:
                            a, b = strict.named(left[depth]), strict.named(right[depth])
                            assert list(a) == list(b) == names
                            pairs += [(str(depth[1]), name, a[name], b[name]) for name in names]
                    for depth, name, a, b in pairs:
                        strict.outcome(a, stacks)
                        strict.outcome(b, stacks)
                        relation = False
                        passed = a == b
                        if (group['Id'], name[1]) in eligible:
                            binding = observations[(name[1], depth)]
                            assert binding['Group'] == group['Id']
                            native_ref, own_ref = reference_relation.check_binding(a, b, binding, decoder)
                            passed = reference_relation.equal(a, b, native_ref, own_ref)
                            relation = passed and a != b
                        rows.append(dict(Group=group['Id'], Case=name[1], Depth=depth, StrictEqual=a == b, SourceBoundReferenceRelation=relation, Passed=passed, Differences=[] if passed else strict.difference(a, b)))
            except Exception as error:
                failures.append(dict(Group=group['Id'], Error=repr(error)))
    result = dict(Status='PASS_WHOLE_FIELDS_WITH_SOURCE_BOUND_OWN_REFERENCES' if not failures and len(rows) == 3510 and all(r['Passed'] for r in rows) else 'FAIL_OR_INSUFFICIENT_PROVENANCE', ExpectedRows=3510, ActualRows=len(rows), StrictEqual=sum(r['StrictEqual'] for r in rows), SourceBoundReferenceRows=sum(r['SourceBoundReferenceRelation'] for r in rows), Failures=failures, Rows=rows, Inputs=inputs, NativeAdmissionSha256=sha(args.native_admission), Projection=False, Normalization=False, CompatibilityPromoted=False)
    (args.output / 'comparison.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key:result[key] for key in ['Status', 'ExpectedRows', 'ActualRows', 'StrictEqual', 'SourceBoundReferenceRows']}))
    return 0 if result['Status'].startswith('PASS_') else 1

if __name__ == '__main__':
    raise SystemExit(main())
