"""Materialize fresh capture4 proof, only after Root signals. Does not run comparator."""
from pathlib import Path
import hashlib,json
O=Path(__file__).resolve().parent;R=O.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def main():
 target=O/'bindings.json';assert not target.exists()
 sdk=R/'artifacts/queue-integration368/capture4/sdk';review=R/'artifacts/queue-higher368-source-review4'
 completion=J(sdk/'completion.json');assert completion['FailedGroups']==0 and len(completion['Groups'])==8
 guards=J(review/'guarded-files-proposal.json')['VerifiedSourceFiles'];assert len(guards)==382
 for x in guards:assert H(R/x['Path'])==x['Sha256']
 closure=J(sdk.parent/'main-dll-closure.json');assert len(closure['Files'])==5
 for x in closure['Files']:assert H(R/x['Path'])==H(R/x['CopiedTo'])==x['Sha256']
 b=J(R/'artifacts/queue-ends368-comparison-peer2/bindings.json')
 for x in b['Files']:
  path=x['Path'].replace('/capture3/','/capture4/').replace('queue-higher368-source-review3','queue-higher368-source-review4')
  x.update(Path=path,Bytes=(R/path).stat().st_size,Sha256=H(R/path))
 for path in ['src/Erlang.Compiler/StaticApplyParameters.cs','artifacts/queue-ends368-comparison-peer3/cap-addendum.json']:
  b['Files'].append({'Path':path,'Bytes':(R/path).stat().st_size,'Sha256':H(R/path)})
 b['CapApplicability']='StaticApplySyntax inverse-byte-equivalent except args.Count>255 refusal. Every selected queue and observer literal destination call <=3 args; no active source/entry/protocol changed.'
 target.write_text(json.dumps(b,indent=2)+'\n')
 print('Fresh382 guards and5DLLs bound. Comparison NOT executed. Root command: python -B artifacts/queue-ends368-comparison-peer3/compare.py --native artifacts/queue-original368-original1/download/run-39078144460/artifact-12117323576/capture --output artifacts/queue-ends368-comparison-peer3/actual1')
if __name__=='__main__':main()
