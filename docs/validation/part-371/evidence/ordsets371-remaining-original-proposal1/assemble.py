"""Assemble full21 source-only tool; preserve the previously dispatched8 producer files."""
from pathlib import Path
import hashlib,json,shutil
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def W(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def main():
    old=ROOT/'artifacts/next-api371-proposal1/tool';tool=HERE/'tool';assert not tool.exists()
    assert H(old/'source-inputs.corrected.json')=='699A359762EA9EBBA64B6369BDCA92814E437C5F0A3F56246FA2E9F1A04F0829'
    shutil.copytree(old,tool)
    for source in (HERE/'original').glob('*.erl'):shutil.copyfile(source,tool/'original'/source.name)
    profile=J(tool/'profile.json');extra=J(HERE/'profile.fragment.json');profile['Fixtures']+=extra['Fixtures'];profile['Family']='ordsets-full21'
    profile['Mfas']=sorted({m for f in profile['Fixtures'] for c in f['Cases'] for m in c['Mfas']});assert len(profile['Mfas'])==21
    profile['FixtureSources']=[dict(Location='tool',Path=p.relative_to(tool).as_posix(),Sha256=H(p)) for p in sorted((tool/'original').glob('*.erl'))]
    profile.update(ExpectedUniqueSourceCount=5,ExpectedValues=413,ExpectedProfiles=2065,Scope='All21 exports, full source contract and strict raw value/stack comparison required before complete status.')
    W(tool/'profile.json',profile)
    manifest=J(tool/'source-inputs.corrected.json');manifest['Status']='SOURCE_ONLY_FULL21_NOT_EXECUTED';manifest['ToolInputs']=[dict(Path=p.relative_to(tool).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(tool.rglob('*')) if p.is_file() and p.name!='source-inputs.corrected.json'];W(tool/'source-inputs.corrected.json',manifest)
    worktree=ROOT/'artifacts/proplists-stack-ci-validation1';rows=[]
    for p in sorted(tool.rglob('*')):
        if p.is_file():
            destination=Path('tools/Erlang.OrdsetsCoreOriginalObservations')/p.relative_to(tool);previous=worktree/destination
            rows.append(dict(Source=p.relative_to(ROOT).as_posix(),Destination=destination.as_posix(),Bytes=p.stat().st_size,Sha256=H(p),BaseSha256=H(previous) if previous.is_file() else None))
    W(HERE/'copy-plan.json',dict(OnlyToolOverlay=True,PreviouslyDispatchedCore8Preserved=True,Files=rows))
    W(HERE/'full-source-seal.json',dict(Status='PREPARED_ONLY_FULL21_SOURCE',Files=[dict(Path=p.relative_to(HERE).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(HERE.rglob('*')) if p.is_file() and p.name!='full-source-seal.json']))
    print(json.dumps(dict(Manifest=H(tool/'source-inputs.corrected.json'),Seal=H(HERE/'full-source-seal.json'),Full21Values=413,Full21Profiles=2065,TotalComparisons=2478)))
if __name__=='__main__':main()
