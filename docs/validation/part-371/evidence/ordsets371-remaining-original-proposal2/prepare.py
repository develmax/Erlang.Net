"""Immutable full21 successor with numeric representative/argument-role controls."""
from pathlib import Path
import hashlib,json,shutil
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def W(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def main():
    old=ROOT/'artifacts/ordsets371-remaining-original-proposal1';tool=HERE/'tool';assert not tool.exists();shutil.copytree(old/'tool',tool)
    profile=J(tool/'profile.json');g=next(x for x in profile['Fixtures'] if x['Id']=='ordsets_set_operations')
    controls=[('union_role_swap_numeric','union',['[2.0]','[1,2]']),('union_role_reverse_numeric','union',['[1,2]','[2.0]']),('intersection_role_swap_numeric','intersection',['[2.0]','[1,2]']),('intersection_role_reverse_numeric','intersection',['[1,2]','[2.0]']),('is_disjoint_role_swap_numeric','is_disjoint',['[2.0]','[1,2]']),('is_disjoint_role_reverse_numeric','is_disjoint',['[1,2]','[2.0]']),('union_role_swap_tail','union',['[3.0,4|tail]','[1,2,3,4,5]']),('intersection_role_swap_tail','intersection',['[3.0,4|tail]','[1,2,3,4,5]']),('is_disjoint_role_swap_tail','is_disjoint',['[3.0|tail]','[1,2]'])]
    path=tool/g['Path'];text=path.read_text();oldnames='cases() -> ['+','.join(g['Names'])+'].';assert oldnames in text
    clauses=[]
    for name,f,args in controls:
        assert name not in g['Names'];g['Names'].append(name);g['Cases'].append(dict(Name=name,Mfas=['ordsets:'+f+'/2'],Scope='Source-proven greater-head argument-role swap; numeric representative identity and full recursive error frames retained.'))
        clauses.append('probe('+name+') -> erlang:apply(ordsets,'+f+',['+','.join(args)+'])')
    text=text.replace(oldnames,'cases() -> ['+','.join(g['Names'])+'].');last=text.rfind('.');assert text[last:].strip()=='.';text=text[:last]+';\n'+';\n'.join(clauses)+'.\n';path.write_text(text,encoding='utf-8')
    profile['ExpectedValues']=sum(len(f['Names']) for f in profile['Fixtures']);profile['ExpectedProfiles']=profile['ExpectedValues']*5
    assert profile['ExpectedValues']==422 and profile['ExpectedProfiles']==2110
    profile['FixtureSources']=[dict(Location='tool',Path=p.relative_to(tool).as_posix(),Sha256=H(p)) for p in sorted((tool/'original').glob('*.erl'))];W(tool/'profile.json',profile)
    manifest=J(tool/'source-inputs.corrected.json');manifest['ToolInputs']=[dict(Path=p.relative_to(tool).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(tool.rglob('*')) if p.is_file() and p.name!='source-inputs.corrected.json'];W(tool/'source-inputs.corrected.json',manifest)
    shutil.copyfile(ROOT/'artifacts/erlang371-passive-proposal1/ordsets-preflight.py',HERE/'ci-preflight.py')
    worktree=ROOT/'artifacts/proplists-stack-ci-validation1';rows=[]
    for p in sorted(tool.rglob('*')):
        if p.is_file():
            destination=Path('tools/Erlang.OrdsetsCoreOriginalObservations')/p.relative_to(tool);previous=worktree/destination
            rows.append(dict(Source=p.relative_to(ROOT).as_posix(),Destination=destination.as_posix(),Bytes=p.stat().st_size,Sha256=H(p),BaseSha256=H(previous) if previous.is_file() else None))
    for name,dest in [('ci-preflight.py','tools/Erlang.OrdsetsCoreOriginalObservations/ci-preflight.py'),('ordsets21-original.yml','.github/workflows/proplist-get-value-original.yml')]:
        p=HERE/name;previous=worktree/dest;rows.append(dict(Source=p.relative_to(ROOT).as_posix(),Destination=dest,Bytes=p.stat().st_size,Sha256=H(p),BaseSha256=H(previous) if previous.is_file() else None))
    W(HERE/'copy-plan.json',dict(OnlyOriginalToolAndWorkflow=True,PreviouslyDispatchedCore8Preserved=True,Files=rows))
    (HERE/'README.md').write_text('Immutable successor to proposal1. Full21 ordsets422 values+2110 stacks=2532 strict candidate comparisons,3 independent cohorts/5 parsed sources. Nine new representative/argument-role controls reflect pinned union/intersection/is_disjoint greater-head role swaps. Original8 fixture/support/worker preserved exactly. ordsets21-original.yml replaces existing original workflow using inputs expected_head/source_inputs_sha256 (ordsets manifest), one owned preflight, one source-built OTP installation, one ordsets collector with three independent groups. No erlang18 re-execution. Upload protocol original-queue8 and artifact root remain unchanged. No cache/runtime archive refactor; prior reuse audit is documentation only. ci-preflight.py remains separately producer-Git-bound outside original tool manifest. Root must run five-source parse gate, full-tree Style and review copy plan before commit/dispatch. No test/build/git/network execution by author.\n',encoding='utf-8')
    W(HERE/'source-seal.json',dict(Status='PREPARED_ONLY_FULL21_ROLE_CONTROLS',Files=[dict(Path=p.relative_to(HERE).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(HERE.rglob('*')) if p.is_file() and p.name!='source-seal.json']))
    print(json.dumps(dict(Manifest=H(tool/'source-inputs.corrected.json'),Seal=H(HERE/'source-seal.json'),Values=422,Profiles=2110,Comparisons=2532)))
if __name__=='__main__':main()
