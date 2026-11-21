from pathlib import Path
import hashlib,json,re,subprocess,sys

stage,inputs,refs=map(Path,sys.argv[1:])
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

full=stage/'full-uncached'
raw=read(full/'otp-corpus.json')
delta=read(full/'compare-summary.json')
assert raw['Total']==1366 and raw['Counts']['Unsupported']==raw['Counts']['SourceRejected']==0
assert all(row['Before']=='Different' and row['After']=='Equal' for row in delta['StatusChanges'])
assert read(stage/'profile-equivalence.json')['EffectiveProfilesEqual']
ready=stage/'native-early-records'
ready.mkdir()
loader=(full/'loader.terms').read_text(encoding='utf-8-sig')
prepared=len(re.findall(r',accepted[},]',loader))
assert prepared%3==0
on_load=re.findall(r'\{(\w+),on_load_not_allowed\}',loader)
assert prepared+len(on_load)==delta['Changed']*3 and all(on_load.count(name)==3 and name in ('crypto','pubkey_os_cacerts','asn1rt_nif','dyntrace','zstd') for name in set(on_load)), (prepared,delta['Changed'],on_load)
paths={}
for line in (full/'changed-paths.tsv').read_text(encoding='utf-8-sig').splitlines():
    ref,old,new=map(Path,line.split('\t'))
    assert ref.is_relative_to(refs)
    paths[ref.stem]=new
proof={}
for name in sorted(set(on_load)):
    if name=='crypto':
        corpus=inputs/'wx-dispatch-and-nested-configuration-1916-bound-env-retake/native-on-load-crypto'
        recipe=corpus/'execute-retake.eval'
    elif name=='pubkey_os_cacerts':
        corpus=inputs/'compact-physical-label-order-1919'
        recipe=corpus/'pubkey-on-load.eval'
    else:
        recipe=inputs/'label-numbering-and-codec-dispatch-1920/pinned-onload-sources/observe-load-and-native-results.eval'
    eval_text=recipe.read_text(encoding='utf-8-sig')
    path_line=next(line for line in (full/'changed-paths.tsv').read_text(encoding='utf-8-sig').splitlines()
                     if Path(line.split('\t',1)[0]).stem==name)
    proof[name]={}
    for role,path in zip(('Reference','Before','After'),map(Path,path_line.split('\t'))):
        output=ready/f'{name}-{role}.terms'
        command=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',eval_text,'-extra',str(path),str(output)]
        with (ready/f'{name}-{role}.stdout.log').open('wb') as stdout, (ready/f'{name}-{role}.stderr.log').open('wb') as stderr:
            subprocess.run(command,stdout=stdout,stderr=stderr,check=True,cwd=ready)
        proof[name][role]={'Image':sha(path),'Output':sha(output)}
    assert len({entry['Output'] for entry in proof[name].values()})==1, name
freeze=read(stage/'source-freeze.json')
execution=read(stage/'linked-runtime-witnesses/receipt.json')
assert execution['Compiler'].lower()==freeze['Compiler'].lower()
assert execution['Runtime'].lower()==freeze['Runtime'].lower()
for name,witness in execution['Witnesses'].items():
    assert name in paths and sha(paths[name])==witness['Beam'], name
exact=[]
for status in delta['StatusChanges']:
    path=Path(status['Beam'].replace(chr(92),'/'))
    expected=sha(refs/path)
    assert sha(paths[path.stem])==expected
    exact.append(dict(Beam=path.as_posix(),Path=str(paths[path.stem]),Sha256=expected))
for source,destination in [('loader.terms','loader-verified.log'),('loader.stderr.log','loader-verified.stderr.log')]:
    (ready/destination).write_bytes((full/source).read_bytes())
receipt=dict(ChangedImagesPrepared=delta['Changed'],InputProfiles=raw['Total'],BatchPrepared=prepared//3,
             IndividuallyLoaded=len(on_load)//3,OnLoad=proof,VerifiedNativeImages=exact,
             LinkedExecution=execution)
(ready/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt,indent=2))





