"""Refresh declared byte identities only; do not change membership or acceptance flags."""
import hashlib,json,pathlib,re
root=pathlib.Path.cwd()
out=root/'docs/validation/part-576/source-plan-refresh'
out.mkdir(parents=True,exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
monitor=root/'tools/Runtime/Erlang.MonitorOriginal645Observations/inputs.json'
md=json.loads(monitor.read_bytes())
oldpath='docs/validation/part-574/source-plan-refresh/monitor-current-source.json'
newpath='docs/validation/part-576/source-plan-refresh/monitor-current-source.json'
old=json.loads((root/oldpath).read_bytes())
new=dict(Scope='PR108 current declared source identities only; no native execution or compatibility promotion',
 PinnedOtpCommit=old['PinnedOtpCommit'],ParentPath=oldpath,ParentSha256=sha((root/oldpath).read_bytes()),
 Rows=[dict(Path=x['Path'],Sha256=sha((root/x['Path']).read_bytes())) for x in old['Rows']])
(root/newpath).write_bytes((json.dumps(new,indent=2)+'\n').encode())
text=monitor.read_text();text=text.replace(md['SourceFreezePath'],newpath).replace(md['SourceFreezeSha256'],sha((root/newpath).read_bytes()))
monitor.write_bytes(text.encode())
names=[
 'Native/Erlang.NifEntryOriginalObservations/manifest.json',
 'Native/Erlang.NifTermCallbackOriginalObservations/manifest.json',
 'Native/Erlang.NifTextBinaryOriginalObservations/manifest.json',
 'Native/Erlang.NifMakeReferenceOriginalObservations/manifest.json',
 'Native/Erlang.NifMapSizeOriginalObservations/manifest.json',
 'Native/Erlang.NifMapFamily504Observations/source-plan.json',
 'Native/Erlang.NifMapMaterialized505Observations/source-plan.json',
 'Native/Erlang.NifMapReentry505Observations/source-plan.json',
 'Runtime/Erlang.OwnedGuest506Observations/source-plan.json',
 'Native/Erlang.NifVariadic526Observations/source-plan.json',
 'Native/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json',
 'Native/Erlang.NifReallocationObservations/SOURCE-PLAN.json',
 'Native/Erlang.NifPrivateData557Observations/source-plan.json',
 'Native/Erlang.NifResource529Observations/source-plan.json',
 'Runtime/Erlang.MatchSaved634Observations/inputs.json',
 'Runtime/Erlang.EtsDispatch540Observations/inputs.json',
 'Runtime/Erlang.MonitorOriginal645Observations/inputs.json',
 'Runtime/Erlang.BeamFunction542Observations/inputs.json',
 'Native/Erlang.NifUnixFilenameObservations/manifest.json',
 'Native/Erlang.NifVersion651Observations/linux-build-inputs.json',
 'Native/Erlang.NifVersion651Observations/linux-inputs.json',
 'Native/Erlang.NifLinuxCallbackObservations/acceptance-inputs.json',
 'Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json']
paths=[root/'tools'/n for n in names]+[root/'docs/mfa-compatibility.json',root/'docs/native-infrastructure.json']
audit_path=out/'refresh-audit.json'
audit=json.loads(audit_path.read_bytes())['Updates'] if audit_path.exists() else []
def refresh(p):
 raw=p.read_bytes();text=raw.decode('utf-8-sig');doc=json.loads(text);changes=[]
 def walk(x,group=None):
  nonlocal text
  if isinstance(x,dict):
   if isinstance(x.get('Path'),str) and isinstance(x.get('Sha256'),str):
    local_files=p.name=='manifest.json' and p.parent.name in ['Erlang.NifEntryOriginalObservations','Erlang.NifTermCallbackOriginalObservations','Erlang.NifTextBinaryOriginalObservations','Erlang.NifMakeReferenceOriginalObservations','Erlang.NifMapSizeOriginalObservations','Erlang.SchedulerAffinityOriginalObservations']
    base=p.parent if x.get('Scope')=='Tool' or (group=='Files' and local_files) else root
    target=base/x['Path']
    if not target.is_file():return
    if target.resolve()==p.resolve():raise ValueError('Self reference: '+str(p))
    data=target.read_bytes()
    if x.get('HashMode')=='TextLF':data=data.replace(b'\r\n',b'\n')
    digest=sha(data)
    # Replace the exact object span, preserving surrounding JSON layout.
    pattern=r'\{[^{}]*"Path"\s*:\s*'+re.escape(json.dumps(x['Path']))+r'[^{}]*\}'
    def update(match):
     block=match.group(0)
     if not re.search(r'"Sha256"\s*:\s*"'+x['Sha256']+'"',block):return block
     block=re.sub(r'("Sha256"\s*:\s*")[^"]+',lambda m:m[1]+digest,block)
     if 'Bytes'in x:block=re.sub(r'("Bytes"\s*:\s*)\d+',lambda m:m[1]+str(len(data)),block)
     return block
    if digest!=x['Sha256'].upper() or ('Bytes'in x and x['Bytes']!=len(data)):
     text,count=re.subn(pattern,update,text)
     if not count:raise ValueError('No object span: '+x['Path'])
     changes.append(dict(Path=x['Path'],Before=x['Sha256'],After=digest,HashMode=x.get('HashMode','Raw')))
    return
   for k,v in x.items():walk(v,k)
  elif isinstance(x,list):
   for v in x:walk(v,group)
 walk(doc)
 if text.encode()!=raw:
  p.write_bytes(text.encode());audit.append(dict(Plan=p.relative_to(root).as_posix(),Rows=changes))
 return sha(raw),sha(p.read_bytes())
for iteration in range(15):
 before=len(audit);replacements=[]
 for p in paths:
  original_hash=sha(p.read_bytes())
  if p.name=='manifest.json':
   raw=p.read_bytes();doc=json.loads(raw)
   if 'RuntimeBuildManifestSha256'in doc:
    newhash=sha((root/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json').read_bytes())
    if doc['RuntimeBuildManifestSha256']!=newhash:
     p.write_bytes(raw.replace(doc['RuntimeBuildManifestSha256'].encode(),newhash.encode()))
     audit.append(dict(Plan=p.relative_to(root).as_posix(),RuntimeBuildManifestPinUpdated=True))
  a,b=refresh(p);a=original_hash
  if a!=b:replacements.append((a,b))
 # Existing workflow pins and the acceptance plan's explicit N1 plan pin.
 for p in list((root/'.github/workflows').glob('*.yml'))+paths:
  raw=p.read_bytes();data=raw
  for a,b in replacements:data=data.replace(a.encode(),b.encode())
  if data!=raw:p.write_bytes(data);audit.append(dict(Plan=p.relative_to(root).as_posix(),PinUpdates=True))
 p=root/'.github/workflows/nif-version651-linux.yml';data=p.read_bytes();text=data.decode()
 for key,relative in [('ORIGINAL_PLAN','Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json'),('NATIVE_PLAN','Native/Erlang.NifVersion651Observations/linux-build-inputs.json'),('CAPTURE_PLAN','Native/Erlang.NifVersion651Observations/linux-inputs.json'),('ACCEPTANCE_PLAN','Native/Erlang.NifLinuxCallbackObservations/acceptance-inputs.json')]:
  text=re.sub(r'(?m)^(      '+key+r': )[0-9A-F]{64}',lambda m:m[1]+sha((root/'tools'/relative).read_bytes()),text)
 if text.encode()!=data:p.write_bytes(text.encode());audit.append(dict(Plan=p.relative_to(root).as_posix(),ExplicitN1Pins=True))
 for workflow,key,relative in [('nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256','Native/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'),('nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256','Native/Erlang.NifReallocationObservations/SOURCE-PLAN.json')]:
  p=root/'.github/workflows'/workflow;raw=p.read_bytes()
  data=re.sub(rb'('+key.encode()+rb': )[0-9A-F]{64}',lambda m:m[1]+sha((root/'tools'/relative).read_bytes()).encode(),raw)
  if data!=raw:p.write_bytes(data);audit.append(dict(Plan=p.relative_to(root).as_posix(),ExplicitReviewedPin=True))
 if len(audit)==before:break
else:raise ValueError('Dependency pins did not stabilize')
(out/'refresh-audit.json').write_bytes((json.dumps(dict(Scope='Declared source identities and existing pins only',NativeExecuted=False,AcceptanceStatusesChanged=False,FullMembershipClaimed=False,Iterations=iteration+1,Updates=audit),indent=2)+'\n').encode())
print('Stabilized',iteration+1,'iterations;',len(audit),'updates')
