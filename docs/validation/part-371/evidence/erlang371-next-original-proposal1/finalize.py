"""Artifact-only finalize, merge independently authored bounded-contract fixtures."""
from pathlib import Path
import hashlib,json,shutil
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];TOOL=HERE/'tool'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def W(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def main():
    profile=J(TOOL/'profile.json');assert len(profile['Fixtures'])==4
    supplement=ROOT/'artifacts/erlang371-contract-audit1'
    fragment=J(supplement/'profile.fragment.json')
    source=supplement/'original/erlang371_bounded_contracts.erl'
    assert H(source)=='06638B80B7BF97F545AEB26DB5695A75EFA50384A47B41FFDFD170B24B2C419D'
    shutil.copyfile(source,TOOL/'original'/source.name)
    profile['Fixtures']+=fragment['Fixtures']
    profile['Mfas']=sorted({m for g in profile['Fixtures'] for c in g['Cases'] for m in c['Mfas']})
    profile['ExpectedValues']=sum(len(g['Names']) for g in profile['Fixtures']);profile['ExpectedProfiles']=profile['ExpectedValues']*5
    profile['ExpectedUniqueSourceCount']=7
    profile['FixtureSources']=[dict(Location='tool',Path=p.relative_to(TOOL).as_posix(),Sha256=H(p)) for p in sorted((TOOL/'original').glob('*.erl'))]
    profile['Scope']='14 new numeric/text conversion MFAs plus four prior bounded-contract MFAs. Whole raw values and stacks; finite corpus does not itself establish complete contracts.'
    assert len(profile['Mfas'])==18 and profile['ExpectedValues']==751 and profile['ExpectedProfiles']==3755
    W(TOOL/'profile.json',profile)
    plan=J(TOOL/'source-inputs.corrected.json');plan['ToolInputs']=[dict(Path=p.relative_to(TOOL).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(TOOL.rglob('*')) if p.is_file() and p.name!='source-inputs.corrected.json'];W(TOOL/'source-inputs.corrected.json',plan)
    base=ROOT/'artifacts/proplists-stack-ci-validation1';rows=[]
    for p in sorted(TOOL.rglob('*')):
        if p.is_file():
            rel=p.relative_to(TOOL);dest=Path('tools/Erlang.ProplistFamilyObservations')/rel;old=base/dest
            rows.append(dict(Source=p.relative_to(ROOT).as_posix(),Destination=dest.as_posix(),BaseSha256=H(old) if old.is_file() else None,Bytes=p.stat().st_size,Sha256=H(p)))
    W(HERE/'copy-plan.json',dict(BaseHead='20001454356011e318f2dea6b3d7ed8b8d569f90',Files=rows))
    script=(ROOT/'artifacts/erlang370-closure-execution-proposal1/compose-original.py').read_text()
    script=script.replace('erlang370-closure-original-proposal2','erlang371-next-original-proposal1').replace('4e51fba990a0e113d31cae50353d4d45903f1fc8','20001454356011e318f2dea6b3d7ed8b8d569f90').replace('validation/erlang-370-closure','validation/erlang-371-conversions').replace('708A3322EF66A5A8824BC9E06652BCF836B41DA4C1D044D1AE32EA8FF269F47B','__SEAL__').replace("==6","==7").replace("'Values':44,'Profiles':220","'Values':751,'Profiles':3755").replace('0258DDF97CE3940C7C07AD129747726CFB8263F6F196D9369A1EDCD4419A1182',H(TOOL/'source-inputs.corrected.json')).replace('test: observe new-only erlang closure without repeating prior inputs','test: observe erlang conversion and bounded contracts against pinned OTP')
    (HERE/'compose-original.template.py').write_text(script,encoding='utf-8')
    # Frozen reusable SDK harness remains unchanged; only source-manifest and shape admissions change.
    sdk=HERE/'sdk-recipe';shutil.copytree(ROOT/'artifacts/erlang370-sdk-recipe3/harness',sdk/'harness',ignore=shutil.ignore_patterns('bin','obj'))
    capture=(ROOT/'artifacts/erlang370-sdk-recipe3/run-main-capture.py').read_text().replace('0258DDF97CE3940C7C07AD129747726CFB8263F6F196D9369A1EDCD4419A1182',H(TOOL/'source-inputs.corrected.json')).replace("len(profile['Fixtures'])==4","len(profile['Fixtures'])==5").replace('==44','==751').replace("'ExpectedValues':44,'ExpectedProfiles':220","'ExpectedValues':751,'ExpectedProfiles':3755")
    (sdk/'run-main-capture.py').write_text(capture,encoding='utf-8')
    (HERE/'README.md').write_text('Prepared only; no Main writes, SDK build, git mutation, network or CI dispatch by author.\n\n18 selected MFAs:14 numeric conversions outside previous57 plus4 bounded contracts. 751 values +3755 full-stack profiles =4506 comparisons,5 independent groups. Exact metadata policy is unchanged erlang-preloaded-v1, same compiled monitored-worker context, same default/0/1/2/8 depths. Collector retains all independent group failures and skips dependent VM execution after failed compilation.\n\nRoot may use the guarded copy plan in the existing validation worktree at20001454356011e318f2dea6b3d7ed8b8d569f90. Mandatory parse gate must accept all7 sources, then full-tree Style before staging. Existing original workflow and source-build receipt can execute this adapter and the independent ordsets adapter after one OTP build; add each independent collector with an explicit successful-build condition, preserve failures, aggregate failure after both. Never mark failed preparation as passed. No expected outcomes are seeded.\n\ncompose-original.template.py is a prepared recipe requiring replacement of __SEAL__ by source-seal.json hash after Root review (or use copy plan directly). It intentionally does not push/dispatch. sdk-recipe/run-main-capture.py uses Main source and no private runtime candidate. SDK receipt stage name erlang57-capture is retained as the established capture protocol, not the actual selected count.\n\nAuthority: pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 snapshots under authority/. Inspect bif.c integer_to_list/list_to_integer/do_float_to_list/list_to_float and erl_bif_binary.c integer/binary and float formatting/parser implementations before promoting any full contract. This proposal does not promote compatibility.\n',encoding='utf-8')
    W(HERE/'source-seal.json',dict(Status='PREPARED_SOURCE_ONLY_NO_EXECUTION',Files=[dict(Path=p.relative_to(HERE).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(HERE.rglob('*')) if p.is_file() and p.name!='source-seal.json']))
    print(json.dumps(dict(Manifest=H(TOOL/'source-inputs.corrected.json'),Seal=H(HERE/'source-seal.json'),CopyPlan=H(HERE/'copy-plan.json'),Values=751,Profiles=3755)))
if __name__=='__main__':main()
