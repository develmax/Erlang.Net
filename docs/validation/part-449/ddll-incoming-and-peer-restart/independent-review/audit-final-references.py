import json
from pathlib import Path
R=Path.cwd()
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def one(obs,name,side):
 p=[p for p in obs.glob('*-'+side+'.json') if p.name.split('-',1)[1]==name+'-'+side+'.json'];assert len(p)==1;return read(p[0])
def refs(t):
 if isinstance(t,dict):
  if t.get('Kind')=='ReferenceTerm':yield t
  for v in t.values():yield from refs(v)
 elif isinstance(t,list):
  for v in t:yield from refs(v)
checked=[]
for run,depths in [(8,[0,1,2,8])]:
 for depth in depths:
  obs=R/f'artifacts/library449-probe-run{run}/depth{depth}/observations'
  for side in ['original','managed']:
   old=one(obs,'genuine-peer-reference-via-distribution',side)
   fresh=one(obs,'new-peer-reference',side)
   assert old['Kind']==fresh['Kind']=='ReferenceTerm' and old['Node']==fresh['Node'] and old['Creation']!=fresh['Creation']
   for name in ['foreign-reject-before-disconnect','foreign-reject-after-reconnect','old-peer-ref-rejected-by-new-peer']:
    e=one(obs,name,side);items=e['Items'];assert [i['Name'] for i in items[:3]]==['exception','error','badarg']
    frames=items[3]['Items'];assert len(frames)==max(1,min(depth,3))
    assert old in list(refs(e))
   assert one(obs,'new-peer-queue-survives-old-ref',side)=={'Kind':'Integer','Decimal':'1'}
   assert one(obs,'new-peer-queue-zero-after-new-ref',side)=={'Kind':'Integer','Decimal':'0'}
   assert one(obs,'peer-memory-lost-after-actual-restart',side)=={'Kind':'Atom','Name':'undefined'}
   assert one(obs,'actual-peer-absent-after-halt',side)=={'Kind':'Atom','Name':'false'}
   assert one(obs,'real-node-visible-after-reconnect',side)=={'Kind':'Atom','Name':'true'}
   checked.append({'Run':run,'Depth':depth,'Side':side,'OldReference':old,'FreshReference':fresh,'OldRefPreservedAcrossActualFullErrors':True,'OldRefRejectedNewQueueOne':True,'NewRefQueueZero':True,'PeerMemoryLost':True})
(R/'artifacts/library449-independent-analysis1/reference-final-audit.json').write_text(json.dumps({'Passed':True,'CheckedSides':len(checked),'Checks':checked},indent=2)+'\n')
print('Full stale-reference/stack/queue audits:',len(checked))
