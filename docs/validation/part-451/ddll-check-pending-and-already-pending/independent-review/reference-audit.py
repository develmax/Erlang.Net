from pathlib import Path
import json
R=Path.cwd()
def term(obs,n,s):
 p=[p for p in obs.glob('*-'+s+'.json') if p.name.split('-',1)[1]==n+'-'+s+'.json'];assert len(p)==1;return json.loads(p[0].read_text(encoding='utf-8-sig'))
def refs(v):
 if isinstance(v,dict):
  if v.get('Kind')=='ReferenceTerm':yield v
  for x in v.values():yield from refs(x)
 elif isinstance(v,list):
  for x in v:yield from refs(x)
checks=[]
for run,depths in [(2,[8]),(3,[0,1,2])]:
 for depth in depths:
  for order in ['incoming','outgoing']:
   obs=R/f'artifacts/library451-probe-run{run}/depth{depth}-{order}/observations'
   for side in ['original','managed']:
    old=term(obs,'genuine-peer-reference-via-distribution',side);new=term(obs,'new-peer-reference',side)
    assert old['Node']==new['Node'] and old['Creation']!=new['Creation']
    for n in ['foreign-reject-before-disconnect','foreign-reject-after-reconnect','old-peer-ref-rejected-by-new-peer']:
     t=term(obs,n,side);assert [x['Name'] for x in t['Items'][:3]]==['exception','error','badarg'] and len(t['Items'][3]['Items'])==max(1,min(depth,3)) and old in list(refs(t))
    assert term(obs,'new-peer-queue-survives-old-ref',side)=={'Kind':'Integer','Decimal':'1'} and term(obs,'new-peer-queue-zero-after-new-ref',side)=={'Kind':'Integer','Decimal':'0'}
    assert term(obs,'peer-memory-lost-after-actual-restart',side)=={'Kind':'Atom','Name':'undefined'}
    checks.append({'Run':run,'Depth':depth,'Order':order,'Side':side,'Passed':True,'Old':old,'New':new})
(R/'artifacts/library451-independent-analysis1/reference-audit.json').write_text(json.dumps({'Passed':True,'CheckedSides':len(checks),'Checks':checks},indent=2)+'\n');print('Independent full reference/error sides:',len(checks))
