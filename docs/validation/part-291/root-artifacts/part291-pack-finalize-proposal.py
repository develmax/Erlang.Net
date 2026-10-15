"""PROPOSAL ONLY: root reviews and executes after terminal serial full validation.
No build, runtime, git write or network is initiated by this finalizer.
"""
from pathlib import Path
import argparse, hashlib, json, re, subprocess

p = argparse.ArgumentParser()
p.add_argument('--full-exit', required=True, help='Root-authored terminal receipt with ExitCode:0')
p.add_argument('--fixture-compile-guard', required=True, help='Root guard with BeforeRejected:true, AfterCompiled:true, SourceUnchanged:true')
p.add_argument('--codegen-guard', required=True)
args = p.parse_args()
r = Path.cwd().resolve(); a = r/'artifacts'; o = r/'docs/validation/part-291'
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda q: json.loads(q.read_bytes())
parent = 'f2e861afc3f1ff2bdfade705ecb058e7cef433d6'
closed43 = '1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1'
assert subprocess.check_output(['git','rev-parse','HEAD'], cwd=r, text=True).strip() == parent
assert sha((r/'docs/changelog/0043.md').read_bytes()) == closed43
if o.exists():
    assert [q.relative_to(o).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]==['tests.json'], 'Only the readiness comparison preseed is allowed'
    assert (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
freeze = read(a/'root-part291-final-source-freeze.json')
assert freeze['ParentHead'] == parent and freeze['TokensExact'] and len(freeze['Files']) == 21
assert {x['Path'] for x in freeze['Files']} == set(read(a/'root-part291-source-paths.json'))
for row in freeze['Files']:
    raw = (r/row['Path']).read_bytes()
    assert sha(raw) == row['Sha256'] and len(raw) == row['Bytes'], row['Path']

t = read(a/'tests.json'); old = read(r/'docs/validation/part-289/tests.json')
names = [x['Name'] for x in t['Tests']]; old_names = [x['Name'] for x in old['Tests']]
assert (t['Passed'], t['Failed']) == (6599, 0)
assert len(names) == len(set(names)) == 6599 and all(x['Status'] == 'Passed' for x in t['Tests'])
assert len(old_names) == len(set(old_names)) == 6588
assert [n for n in names if n in set(old_names)] == old_names
positive = ['absent-fallback','empty-fallback','private-fallback','tuple-form-fallback','duplicate-function-fallback','repeated-attribute-fallback','empty-then-nonempty-fallback','nonempty-then-empty-fallback']
added = {'compiler/nif-declaration-control-metadata-and-ownership', *('oracle/compiled-module/erlang-nif-declaration-'+x for x in positive), 'oracle/compiler-diagnostic/nif-declaration-undefined-local', 'oracle/compiler-diagnostic/nif-declaration-invalid-integer-entry'}
assert {n for n in names if n not in set(old_names)} == added and len(added) == 11
assert read(Path(args.full_exit))['ExitCode'] == 0
assert b'Validation passed' in (a/'root-part291-full-validation.log').read_bytes()
expected_integration = {'CleanRebuild':'Passed','Readiness':'Passed','LocalTool':'Passed','Tests':'Passed','PackageConsumer':'Passed','Build':'Passed','Incremental':'Passed','HybridOutput':'Hello World','OracleUnavailableReport':'Expected failure recorded','DisabledPreprocessing':'Expected failure','PackagedReadiness':'Passed'}
assert read(a/'integration-results.json') == expected_integration
registry = read(r/'docs/supported-mfas.json')
assert registry == read(r/'docs/validation/part-289/supported-mfas.json') and len(registry) == 502
assert sum(x['Module'] == 'erlang' for x in registry) == 344
ready=read(r/'docs/module-readiness.json')
assert ready['LocalPassedTests']==6599 and ready['RegisteredMfas']==502 and ready['PreviousRegisteredMfas']==502 and ready['RegisteredMfaDelta']==0
assert ready['ComparisonTestReport']=='validation/part-291/tests.json'
cg = read(Path(args.codegen_guard)); assert cg['Status'] == 'HISTORICAL4895_SOURCE_AND_GENERATED_CSHARP_EXACT' and cg['Passed'] == 4895 and cg['Failed'] == 0
fc = read(Path(args.fixture_compile_guard)); assert fc['BeforeRejected'] and fc['AfterCompiled'] and fc['SourceUnchanged']
ci = read(a/'spawn-protocol-ci290-38688620435-independent-guard.json')
assert ci['Head'] == parent and ci['Passed'] == 6588 and ci['Failed'] == 0 and ci['SameHeadCIPassed'] and ci['OrderedNamesExact'] and ci['RegisteredMfas'] == 502 and ci['Integration'] == expected_integration
ng = read(a/'nif-proof290-independent-guard.json')
assert ng['Status'] == 'EXACT_HEAD_ORIGINAL_NIF_SEVEN_TYPED_ETF_AND_PROCESS_CONTROLS_PASS' and ng['Head'] == parent
assert len(ng['OriginalCases']) == 7 and all(v['Passed'] for v in ng['OriginalCases'].values())
assert set(ng['ProcessControls']['Passed']) == {'raw-exit-3-preserved','owned-group-inherited-pipe-bounded'}

# Validate all selected bytes and destinations before copying or metadata edits.
manifest_paths = [a/'spawn-protocol-ci290-detached-staging-manifest.json', a/'nif-proof290-detached-staging-manifest.json']
copies = {}
def select(src, dest, expected=None, size=None):
    src = src.resolve(); dest = dest.resolve(); assert dest.is_relative_to(o)
    assert src.is_file() and 'LICENSE.local.txt' not in src.parts and '__pycache__' not in src.parts
    raw = src.read_bytes()
    if expected: assert sha(raw) == expected
    if size is not None: assert len(raw) == size
    if dest in copies: assert copies[dest][1] == sha(raw)
    copies[dest] = (src, sha(raw))
for mp in manifest_paths:
    m = read(mp); rows = m if isinstance(m, list) else m['Files']
    for row in rows:
        select(r/row['SourcePath'], r/row['Destination'], row['Sha256'], row['Bytes'])
    select(mp, o/'staging-manifests'/mp.name)
for source in a.glob('root-part291-*'):
    if source.is_file(): select(source, o/'root-artifacts'/source.name)
for source in [Path(args.full_exit),Path(args.fixture_compile_guard),Path(args.codegen_guard),Path(__file__).resolve(),a/'part291-pack-plan.md']:
    select(source, o/'root-artifacts'/source.name)
for name in ['part291-readiness-metadata-proposal.py','part291-stage-index-guard-proposal.py']:
    select(a/name, o/'root-artifacts'/name)
for name in ['module-readiness.json','MODULE_READINESS.md']:
    select(r/'docs'/name, o/name)
select(a/'nif-declaration291-independent-source-review.json',o/'source-reviews/nif-declaration291-independent-source-review.json')
for name in ['contracts.json','pinned-bindings.json','source-freeze.json','README.md']:
    select(a/'nif-declaration-candidate1'/name, o/'source-contracts'/name)
for name in ['Focus.csproj','Program.cs']:
    select(a/'root-part291-focus'/name, o/'focus-driver'/name)
for name in ['Compare.csproj','Program.cs']:
    select(a/'part291-codegen-compare'/name, o/'codegen-comparison-driver'/name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]: select(src,dst)
entry = (a/'part291-pack-changelog-draft.md').read_text(encoding='utf-8-sig')
assert '## Part 291' in entry and '@@' not in entry
volume = r/'docs/changelog/0044.md'; old_volume = volume.read_bytes(); assert b'## Part 291' not in old_volume
combined = old_volume + b'\r\n' + entry.replace('\r\n','\n').replace('\n','\r\n').encode()
assert len(combined) <= 32768 and len(combined.splitlines()) <= 400, 'Rotate volume explicitly; never edit closed volume bytes'
index = r/'CHANGELOG.md'; index_raw = index.read_bytes(); label = 'parts289–290 (active)'.encode(); assert index_raw.count(label) == 1
note = 'Part291: Main6599/6599/full integration passes with all6588 historical ordered names and eleven additions. Source -nifs declarations preserve absent/empty immutable metadata, local-function validation and constructor ABI; unchanged fixture source now compiles. Historical4895 generated C# hashes are exact. Default502 and erlang344/351 (98.0% API presence) remain unchanged. Preceding-head290 CI6588/full and original trusted NIF7 plus Linux collector controls2 are independently archived; native unload and general load_nif parity are not claimed. New ten original contracts and same-head291 CI remain pending. General NIF loading, all-writer pressure/EXITING, physical resources and whole erlang remain unfinished.'
metadata = ['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
    data=(r/name).read_bytes(); assert b'Part291:' not in data and re.search(br'\r?\n\r?\n', data)
attr=r/'.gitattributes'; assert b'docs/validation/part-291/** binary' not in attr.read_bytes()

for dest,(src,digest) in copies.items():
    raw=src.read_bytes(); assert sha(raw)==digest; dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes(raw)
volume.write_bytes(combined); index.write_bytes(index_raw.replace(label,'parts289–291 (active)'.encode()))
for name in metadata:
    q=r/name; data=q.read_bytes(); cut=re.search(br'\r?\n\r?\n',data).end(); nl=b'\r\n' if b'\r\n' in data else b'\n'; q.write_bytes(data[:cut]+note.encode()+nl+nl+data[cut:])
attr.write_bytes(attr.read_bytes()+b'docs/validation/part-291/** binary\n')
assert sha((r/'docs/changelog/0043.md').read_bytes()) == closed43
receipt={'Status':'ACTUAL_MAIN6599_FULL_SOURCE_DECLARATION_PASS_PRIOR_HEAD290_CI_AND_NIF_VERIFIED_NEW_ORIGINAL10_AND_CI291_PENDING','ParentHead':parent,'SourcePaths':21,'Passed':6599,'Failed':0,'Historical6588OrderedNamesExact':True,'Added':[n for n in names if n in added],'RegisteredMfas':502,'ErlangMfas':344,'ErlangReference':351,'HistoricalGeneratedCSharpExact':4895,'PriorCI290Passed':6588,'PriorOriginalNifCasesPassed':7,'PriorLinuxProcessControlsPassed':2,'NativeUnloadCounterCompared':False,'Original291NewCasesExecuted':False,'CI291Passed':False,'WholeCompatibility':False,'Closed0043Sha256':closed43}
receipt['IntegrationAndPackagesPassed']=True
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':sha(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'ProofFiles':len(receipt['Files']),'SourcePaths':21,'Passed':6599,'Registry':502,'ChangelogBytes':len(combined)}))
