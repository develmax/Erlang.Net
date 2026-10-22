"""Source-only successor: quote fix, actual MD5 contract, complete raw map/FUN controls."""
from pathlib import Path
import ast,hashlib,json,shutil
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def W(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def main():
    import argparse
    parser=argparse.ArgumentParser();parser.add_argument('--decoder',type=Path,required=True);parser.add_argument('--decoder-sha256',required=True);args=parser.parse_args()
    assert H(args.decoder)==args.decoder_sha256.upper()
    old=ROOT/'artifacts/ordsets371-remaining-original-proposal2';tool=HERE/'tool';assert not tool.exists();shutil.copytree(old/'tool',tool)
    path=tool/'original/ordsets371_callbacks.erl';text=path.read_text();assert text.count('fun erlang:/2')==16;text=text.replace('fun erlang:/2',"fun erlang:'/'/2");assert 'fun erlang:/2' not in text;path.write_text(text,encoding='utf-8')
    path=tool/'collect-original.py';text=path.read_text();assert text.count('{ok,[{ordsets,LoadedMd5}]}')==1;text=text.replace('{ok,[{ordsets,LoadedMd5}]}','{ok,{ordsets,LoadedMd5}}');path.write_text(text,encoding='utf-8')
    additional=ROOT/'artifacts/ordsets371-map-fun-control-proposal1';source=additional/'original/ordsets371_map_fun_keys.erl'
    assert H(source)=='C10F564C80E7065517EAF915B158BD9BB3EB2A55B5B561B0C79F026E2F37880E';shutil.copyfile(source,tool/'original'/source.name)
    shutil.copyfile(args.decoder,tool/'passive_etf_family.py')
    profile=J(tool/'profile.json');profile['Fixtures']+=J(additional/'profile.fragment.json')['Fixtures'];profile['Family']='ordsets-full21-map-fun-controls'
    profile['Mfas']=sorted({m for f in profile['Fixtures'] for c in f['Cases'] for m in c['Mfas']})
    assert len(profile['Mfas'])==21 and len(profile['Fixtures'])==4
    profile['ExpectedValues']=sum(len(f['Names']) for f in profile['Fixtures']);profile['ExpectedProfiles']=profile['ExpectedValues']*5
    assert profile['ExpectedValues']==436 and profile['ExpectedProfiles']==2180
    profile['FixtureSources']=[dict(Location='tool',Path=p.relative_to(tool).as_posix(),Sha256=H(p)) for p in sorted((tool/'original').glob('*.erl'))];profile['ExpectedUniqueSourceCount']=6;W(tool/'profile.json',profile)
    manifest=J(tool/'source-inputs.corrected.json');manifest['ToolInputs']=[dict(Path=p.relative_to(tool).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(tool.rglob('*')) if p.is_file() and p.name!='source-inputs.corrected.json'];W(tool/'source-inputs.corrected.json',manifest)
    for name in ['ordsets21-original.yml','ci-preflight.py']:shutil.copyfile(old/name,HERE/name)
    worktree=ROOT/'artifacts/proplists-stack-ci-validation1';rows=[]
    for p in sorted(tool.rglob('*')):
        if p.is_file():
            destination=Path('tools/Erlang.OrdsetsCoreOriginalObservations')/p.relative_to(tool);previous=worktree/destination
            rows.append(dict(Source=p.relative_to(ROOT).as_posix(),Destination=destination.as_posix(),Bytes=p.stat().st_size,Sha256=H(p),BaseSha256=H(previous) if previous.is_file() else None))
    for name,dest in [('ci-preflight.py','tools/Erlang.OrdsetsCoreOriginalObservations/ci-preflight.py'),('ordsets21-original.yml','.github/workflows/proplist-get-value-original.yml')]:
        p=HERE/name;previous=worktree/dest;rows.append(dict(Source=p.relative_to(ROOT).as_posix(),Destination=dest,Bytes=p.stat().st_size,Sha256=H(p),BaseSha256=H(previous) if previous.is_file() else None))
    W(HERE/'copy-plan.json',dict(OnlyOriginalToolAndWorkflow=True,PreviouslyDispatchedCore8Preserved=True,Files=rows))
    historical=[ROOT/'artifacts/ordsets371-original1/parse-receipt.json',ROOT/'artifacts/erlang371-original1/download/run-39112151003/artifact-12130754382/ordsets-capture/ordsets_core/observe.stderr']
    W(HERE/'failure-history.json',dict(Proposal2Parse='FAILED_CALLBACK_OPERATOR_ATOM_UNQUOTED',Original371Run='FAILED_ORDSETS_COLLECTOR_BEAM_LIB_MD5_BINARY_RESULT_PATTERN_BEFORE_OUTCOMES',Prior8ComparisonsClaimed=0,RetainedFiles=[dict(Path=p.relative_to(ROOT).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in historical]))
    (HERE/'README.md').write_text('Immutable successor3. Full21 ordsets436 values+2180 stacks=2616 strict candidate comparisons,4 independent cohorts/6parsed sources. Fixes actual16 unquoted operator fun fixture errors and native beam_lib:md5(Binary) result pattern {ok,{Module,Md5}}. Prior proposal2 parse failure and original371 ordsets collection failure remain failure-history.json; no original8 comparison success is claimed. Adds14 full raw map/FUN outcomes with fixed external creator and reviewed structural decoder; no source-level projection or blanket identity normalization. Runtime creator/map ordering discrepancies may remain and must be fixed or classified honestly after actual comparison. Original8 fixture/support/worker exact. Workflow remains ordsets-only, one OTP build, no duplicate erlang acquisition. Root must run all6 parse gate, full-tree Style and decoder corruption controls before sourcecommit/dispatch. No Main/git/SDK/native/network execution by author.\n',encoding='utf-8')
    for p in HERE.rglob('*.py'):ast.parse(p.read_text(encoding='utf-8'),filename=str(p))
    W(HERE/'source-seal.json',dict(Status='PREPARED_ONLY_FULL21_RAW_CONTROLS',Files=[dict(Path=p.relative_to(HERE).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(HERE.rglob('*')) if p.is_file() and p.name!='source-seal.json']))
    print(json.dumps(dict(Manifest=H(tool/'source-inputs.corrected.json'),Seal=H(HERE/'source-seal.json'),Values=436,Profiles=2180,Comparisons=2616,DecoderSha256=H(tool/'passive_etf_family.py'))))
if __name__=='__main__':main()
