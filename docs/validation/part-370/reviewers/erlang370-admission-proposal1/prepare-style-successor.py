"""Source-only style successor preparation; reads Root's two styled C# files, writes a new artifact only."""
from pathlib import Path
import argparse,json,hashlib,shutil,subprocess,ast
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
HEAD='e62e0f383929a2f5acf83adc012ee26f11342bce'
FILES=['parse-gate/ParseGateNames.cs','parse-gate/Program.cs']
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def main():
    p=argparse.ArgumentParser();p.add_argument('--worktree',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();out=a.output.resolve();w=a.worktree.resolve()
    assert out.is_relative_to(ROOT/'artifacts') and not out.exists()
    assert subprocess.check_output(['git','-C',str(w),'rev-parse','HEAD']).decode().strip()==HEAD
    changed=subprocess.check_output(['git','-C',str(w),'diff','--name-only']).decode().splitlines()
    expected={'tools/Erlang.ProplistFamilyObservations/'+x for x in FILES};assert set(changed)==expected
    base=ROOT/'artifacts/erlang370-original-proposal1';seal=json.loads((base/'source-seal.json').read_bytes())
    for row in seal['Files']:
        q=base/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    out.mkdir();shutil.copytree(base/'tool',out/'tool')
    for name in FILES:shutil.copyfile(w/'tools/Erlang.ProplistFamilyObservations'/name,out/'tool'/name)
    manifest=out/'tool/source-inputs.corrected.json';plan=json.loads(manifest.read_bytes())
    for row in plan['ToolInputs']:
        q=out/'tool'/row['Path']
        if row['Path'] in FILES:row.update(Bytes=q.stat().st_size,Sha256=H(q))
        else:assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    manifest.write_text(json.dumps(plan,indent=2)+'\n')
    payload=[]
    for name in FILES+['source-inputs.corrected.json']:
        dest='tools/Erlang.ProplistFamilyObservations/'+name;raw=subprocess.check_output(['git','-C',str(w),'show',HEAD+':'+dest]);q=out/'tool'/name
        payload.append(dict(Source=q.relative_to(ROOT).as_posix(),Destination=dest,BaseSha256=hashlib.sha256(raw).hexdigest().upper(),Bytes=q.stat().st_size,Sha256=H(q)))
    (out/'copy-plan.json').write_text(json.dumps(dict(BaseHead=HEAD,Files=payload),indent=2)+'\n')
    (out/'README.md').write_text('Style-only successor: exactly two C# parser utility files and their source-manifest hashes change. All ten original Erlang fixture/support/worker files, collector and metadata policy remain byte-identical. Existing original run39106192411/e62e evidence remains bound to its original manifest and must NOT be relabelled as this successor. Root must run full-tree Style --check after overlay and before commit. No original rerun is required solely to repair unrelated automatic CLR validation.\n')
    (out/'source-seal.json').write_text(json.dumps({'Files':[dict(Path=q.relative_to(out).as_posix(),Bytes=q.stat().st_size,Sha256=H(q)) for q in sorted(out.rglob('*')) if q.is_file() and q.name!='source-seal.json'],'PredecessorSealSha256':H(base/'source-seal.json'),'OriginalOutputRelabelled':False},indent=2)+'\n')
    print(json.dumps(dict(SourceInputsSha256=H(manifest),CopyPlanSha256=H(out/'copy-plan.json'),SealSha256=H(out/'source-seal.json'))))
if __name__=='__main__':main()
