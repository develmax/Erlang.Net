from pathlib import Path
import argparse,hashlib,json,shutil
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
a=argparse.ArgumentParser();a.add_argument('--approved-plan-sha256',required=True);v=a.parse_args()
p=P/'composition-plan.json';assert H(p)==v.approved_plan_sha256.upper();plan=J(p)
assert plan['ComposerSha256']==H(Path(__file__))
d=Path(plan['Destination']);assert d==R/'artifacts/size-bif354-full-candidate4' and not d.exists()
for row in plan['InputPins']+plan['Files']:
 q=R/row.get('SourcePath',row['Path']);assert q.is_relative_to(R) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'],str(q)
for row in plan['Files']:
 p=Path(row['Path']);assert not p.is_absolute() and '..' not in p.parts and not any(x.lower() in ('.git','bin','obj') for x in p.parts) and 'license.local' not in str(p).lower()
d.mkdir()
for row in plan['Files']:
 q=d/row['Path'];q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/row['SourcePath'],q)
for row in plan['Files']:
 q=d/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
(P/'application-receipt.json').write_text(json.dumps(dict(Status='ACTUAL_SOURCE_COMPOSITION_ONLY_NO_SDK',PlanSha256=H(P/'composition-plan.json'),Rows=len(plan['Files']),Destination=str(d),MainChanged=False),indent=2)+'\n')
print(H(P/'application-receipt.json'))
