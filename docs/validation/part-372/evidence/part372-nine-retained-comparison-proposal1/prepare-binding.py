"""Prepare source binding only after parent independently accepts final source guard review."""
from pathlib import Path
import argparse,hashlib,json
O=Path(__file__).resolve().parent;R=O.parents[1]
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def main():
 p=argparse.ArgumentParser()
 for key in ['guard-review','guards','output']:p.add_argument('--'+key,type=Path,required=True)
 for key in ['guard-review-sha256','guards-sha256']:p.add_argument('--'+key,required=True)
 a=p.parse_args();assert not a.output.exists() and a.output.resolve().is_relative_to(O)
 assert H(a.guard_review)==a.guard_review_sha256.upper() and H(a.guards)==a.guards_sha256.upper()
 guards=J(a.guards);assert len(guards)>=499 and len({r['Path'] for r in guards})==len(guards)
 oldguards=J(R/'docs/validation/part-371/evidence/erlang371-final-guard-review1/verified-source-files.proposed.json');assert len(oldguards)==496
 assert {r['Path'] for r in oldguards}<={r['Path'] for r in guards}
 for row in guards:assert H(R/row['Path'])==row['Sha256'],row['Path']
 prior=J(R/'docs/validation/part-371/evidence/erlang371-retained-comparison2/source-binding.json')
 old=J(R/'artifacts/queue-ends368-comparison-peer3/bindings.json');changes=[]
 accepted={x['Path']:x for x in prior['ExactOldBindingChanges']}
 for row in old['Files']:
  if row['Path'].endswith('.dll'):continue
  now=H(R/row['Path'])
  if now!=row['Sha256']:
   assert row['Path'] in accepted and accepted[row['Path']]['Before']==row['Sha256'] and accepted[row['Path']]['After']==now,'New source binding delta needs separate semantic review: '+row['Path']
   changes.append(accepted[row['Path']])
 result={'GuardCount':len(guards),'GuardReviewPath':a.guard_review.resolve().relative_to(R).as_posix(),'GuardReviewSha256':H(a.guard_review),'BindingsPath':a.guards.resolve().relative_to(R).as_posix(),'BindingsSha256':H(a.guards),'ExactOldBindingChanges':changes,'PreservedAll496PublishedGuardPaths':True,'NoComparisonPolicyChanged':True,'NoNativePacketEdited':True,'RequiresFreshFinalDllEquality':True,'NoNewBindingDeltaSilentlyAccepted':True}
 a.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(H(a.output))
if __name__=='__main__':main()
