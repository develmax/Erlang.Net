from pathlib import Path
import hashlib,json
r=Path.cwd();d=r/'artifacts/root-part319-historical-red1';c=d/'candidate';assert not d.exists();c.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
base=r/'artifacts/root-part318-main1/source-resource-closure.json';assert h(base.read_bytes())=='7B8C0913A6E46324BF8A40DBD8EB86105990AA6DA76B35EF9BA294E7A44D0C03'
rows=read(base)['Files']
for e in rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path'];p=c/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
new=[]
for name in ['DistributionGuestSendPublicRouteHostChecks.cs','DistributionGuestSendHostNames.cs']:
 path='tests/Erlang.Tests/'+name;raw=(r/'artifacts/root-part319-composed2/candidate'/path).read_bytes();p=c/path;assert not p.exists();p.write_bytes(raw);new.append({'Path':path,'Bytes':len(raw),'Sha256':h(raw)})
write(d/'source-resource-closure.json',{'Files':sorted(rows+new,key=lambda e:e['Path'])});write(d/'source-freeze.json',{'Status':'EXACT318_PRODUCTION_WITH_SAME319_PUBLIC_ROUTE_HOST_PENDING_ACTUAL_RED','ParentHead':'f7ef36d1ddba1af2e52f77b7536f34e5db71a04f','SourceClosurePaths':2274,'ParentSourceClosureSha256':h(base.read_bytes()),'SourceClosureSha256':h((d/'source-resource-closure.json').read_bytes()),'SameGreenHostFiles':new,'ProductionSourceChanges':0})
print(h((d/'source-freeze.json').read_bytes()))
