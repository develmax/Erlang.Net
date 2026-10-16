from pathlib import Path
import hashlib
import json

r = Path.cwd()
out = r/'docs/validation/part-333'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
assert not out.exists()
prior_path = r/'artifacts/part332-five-terminal-lean-copy-rows.json'
assert h(prior_path.read_bytes()) == '5D0FDAF9BCAFD5BFC01977C9F3096DC78E4F45E2E522256A5DA3E5BCE0748BE7'
prior = json.loads(prior_path.read_bytes())
assert len(prior) == 714
trees = {
    'proc-lib333-source-proposal':'source-proposal1',
    'proc-lib333-source-proposal2':'source-proposal2',
    'proc-lib333-source-proposal3':'source-proposal3',
    'proc-lib333-source-proposal4':'source-proposal4',
    'proc-lib333-source-independent':'source-review',
    'proc-lib333-full-source':'final-candidate',
    'proc-lib333-full-source-independent':'composition-review',
    'proc-lib333-full-actual-independent':'full-review',
    'proc-lib333-promotion-source-independent':'promotion-review',
    'proc-lib333-native-proposal':'native-driver',
    'proc-lib333-native-proposal-peer':'native-driver-review',
    'proc-lib333-native-actual':'final-native',
    'proc-lib333-native-actual-passive-review':'native-review',
}
for name in trees:
    assert (r/'artifacts'/name).is_dir(), name
rows = []

def copy(source, target, expected=None):
    source, target = r/source, r/target
    assert source.resolve().is_relative_to(r) and target.resolve().is_relative_to(out)
    assert source.is_file() and not target.exists()
    data = source.read_bytes()
    if expected:
        assert len(data) == expected['Bytes'] and h(data) == expected['Sha256'], str(source)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
    rows.append({'SourcePath':source.relative_to(r).as_posix(),'TargetPath':target.relative_to(r).as_posix(),'Bytes':len(data),'Sha256':h(data)})

for row in prior:
    assert row['TargetPath'].startswith('docs/validation/part-333/prior332/')
    copy(row['SourcePath'],row['TargetPath'],row)
allowed = {'.json','.cs','.py','.ps1','.yml','.erl','.txt','.md','.log','.stdout','.stderr','.diff'}
for name, category in trees.items():
    base = r/'artifacts'/name
    for p in sorted(base.rglob('*')):
        if p.is_file() and p.suffix.lower() in allowed and not any(x in {'bin','obj','__pycache__'} for x in p.relative_to(base).parts):
            copy(p.relative_to(r), out.relative_to(r)/category/p.relative_to(base))
for number in [1,2,3]:
    base = r/'artifacts'/f'proc-lib333-actual{number}'
    assert base.is_dir()
    for p in sorted(base.iterdir()):
        if p.is_file() and p.suffix.lower() in allowed:
            copy(p.relative_to(r),out.relative_to(r)/f'focused{number}'/p.name)
for name in ['root-promote333.py','root-document333.py','root-finalize333.py','root-part333-promotion.json','proc-lib333-full.py']:
    copy(Path('artifacts')/name,out.relative_to(r)/'root-controls'/name)
for name in ['MODULE_READINESS.md','module-readiness.json']:
    (r/'docs'/name).write_bytes((r/'artifacts/root-part333-readiness'/name).read_bytes())
    copy(Path('artifacts/root-part333-readiness')/name,out.relative_to(r)/'readiness'/name)
for name in ['module-readiness-scope.json','supported-mfas.json']:
    copy(Path('docs')/name,out.relative_to(r)/'readiness'/name)
(out/'.gitattributes').write_bytes(b'* binary\n')
index = out/'evidence-index.json'
index.write_bytes((json.dumps(rows,indent=2)+'\n').encode())
receipt = {'Status':'LOCAL333_VALIDATED_AND_ORIGINAL332_COMPLETE','Passed':6871,'Failed':0,'IntegrationOutcomes':11,'UninterruptedLocalFullPass':True,'RegisteredMfas':528,'ProcLibMfas':19,'ProcLibDeclarations':39,'ErlangMfas':351,'SourceClosurePaths':2439,'PackageResourceRows':6,'OwnedPaths':18,'PreviousOrderedTests':6822,'PreviousRegistryRows':521,'CurrentOriginalExecuted':False,'CurrentCiVerified':False,'Prior332Original':{'Passed':6001,'Failed':0,'DescriptorCases':8,'NewSourceCases':39,'StrictOrdinaryVariance':0},'CopiedFiles':len(rows),'CopiedBytes':sum(x['Bytes'] for x in rows),'EvidenceIndexSha256':h(index.read_bytes()),'NoRuntimeOrSdkBinariesCopied':True}
(out/'evidence-receipt.json').write_bytes((json.dumps(receipt,indent=2)+'\n').encode())
print(json.dumps(receipt))
