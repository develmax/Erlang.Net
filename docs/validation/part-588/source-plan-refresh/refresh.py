"""Refresh declared byte identities only; do not change membership or acceptance flags."""
import hashlib,json,pathlib,re
root=pathlib.Path.cwd()
out=root/'docs/validation/part-588/source-plan-refresh'
out.mkdir(parents=True,exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
names=[
 'Native/Erlang.NifIntegratedObservations/inputs.json',
 'Native/Erlang.NifVersion651Observations/inputs.json',
 'Native/Erlang.NifVersion651Observations/build-inputs.json',
 'Native/Erlang.NifUpgradeObservations/build-inputs.json',
 'Native/Erlang.NifUpgradeObservations/original-inputs.json',
 'Native/Erlang.NifUpgradeObservations/inputs.json',
 'Native/Erlang.NifUpgradeObservations/environments/build-inputs.json',
 'Native/Erlang.NifUpgradeObservations/environments/original-inputs.json',
 'Native/Erlang.NifUpgradeObservations/environments/managed-inputs.json',
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
# Publish a new monitor receipt; historical source freezes are immutable.
monitor_parent=root/'docs/validation/part-587/source-plan-refresh/monitor-current-source.json'
monitor_current=out/'monitor-current-source.json'
monitor=json.loads(monitor_parent.read_bytes())
monitor['Scope']='Part587 current declared source identities only; no native execution or compatibility promotion'
monitor['ParentPath']=monitor_parent.relative_to(root).as_posix()
monitor['ParentSha256']=sha(monitor_parent.read_bytes())
for row in monitor['Rows']:
 row['Sha256']=sha((root/row['Path']).read_bytes())
monitor_current.write_bytes((json.dumps(monitor,indent=2)+'\n').encode())
monitor_plan=root/'tools/Runtime/Erlang.MonitorOriginal645Observations/inputs.json'
raw=monitor_plan.read_text()
raw=re.sub(r'("SourceFreezePath"\s*:\s*")[^"]+',lambda m:m[1]+monitor_current.relative_to(root).as_posix(),raw)
raw=re.sub(r'("SourceFreezeSha256"\s*:\s*")[^"]+',lambda m:m[1]+sha(monitor_current.read_bytes()),raw)
monitor_plan.write_bytes(raw.encode())

audit_path=out/'refresh-audit.json'
audit=json.loads(audit_path.read_bytes())['Updates'] if audit_path.exists() else []
def refresh(p):
 raw=p.read_bytes();text=raw.decode('utf-8-sig');doc=json.loads(text);changes=[]
 def walk(x,group=None):
  nonlocal text
  if isinstance(x,dict):
   if isinstance(x.get('Path'),str) and isinstance(x.get('Sha256'),str):
    local_files=p.name=='manifest.json' and p.parent.name in ['Erlang.NifEntryOriginalObservations','Erlang.NifTermCallbackOriginalObservations','Erlang.NifTextBinaryOriginalObservations','Erlang.NifMakeReferenceOriginalObservations','Erlang.NifMapSizeOriginalObservations','Erlang.SchedulerAffinityOriginalObservations']
    base=p.parent if x.get('Scope')=='Tool' or x.get('Root') in ('Packet','Tool') or (group=='Files' and local_files) else root
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
def update_metadata():
 targets={
 'Native/Erlang.NifVersion651Observations/linux-inputs.json':{'BuildPlanSha256':'Native/Erlang.NifVersion651Observations/linux-build-inputs.json'},
 'Native/Erlang.NifVersion651Observations/inputs.json':{'BuildPlanSha256':'Native/Erlang.NifVersion651Observations/build-inputs.json'},
 'Native/Erlang.NifLinuxCallbackObservations/acceptance-inputs.json':{'N1BuildPlanSha256':'Native/Erlang.NifVersion651Observations/linux-build-inputs.json','OriginalPlanSha256':'Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json'}}
 for base,original,managed in [('Native/Erlang.NifUpgradeObservations/','original-inputs.json','inputs.json'),('Native/Erlang.NifUpgradeObservations/environments/','original-inputs.json','managed-inputs.json')]:
  targets[base+original]={'NativeBuildPlanSha256':base+'build-inputs.json'}
  targets[base+managed]={'NativeBuildPlanSha256':base+'build-inputs.json','OriginalPlanSha256':base+original}
 for relative,fields in targets.items():
  p=root/'tools'/relative;raw=p.read_bytes();text=raw.decode();d=json.loads(raw)
  for key,target in fields.items():
   text=re.sub(r'("'+key+r'"\s*:\s*")[^"]+',lambda m:m[1]+sha((root/'tools'/target).read_bytes()),text)
  if 'WrongEntryNativeBuildPlans'in d:
   old=d['WrongEntryNativeBuildPlans'];new={'Windows':sha((root/'tools/Native/Erlang.NifVersion651Observations/build-inputs.json').read_bytes()),'Linux':sha((root/'tools/Native/Erlang.NifVersion651Observations/linux-build-inputs.json').read_bytes())}
   text=re.sub(r'("WrongEntryNativeBuildPlans"\s*:\s*)\{[^{}]*\}',lambda m:m[1]+json.dumps(new,indent=4),text)
  if 'FixtureManifests'in d:
   for name in d['FixtureManifests']:
    text=re.sub(r'("'+name+r'"\s*:\s*")[^"]+',lambda m:m[1]+sha((root/'tools/Native'/name/'manifest.json').read_bytes()),text)
  if text.encode()!=raw:p.write_bytes(text.encode());audit.append(dict(Plan=p.relative_to(root).as_posix(),ProducerPinsUpdated=True))
for iteration in range(15):
 before=len(audit);replacements=[]
 update_metadata()
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
 p=root/'.github/workflows/nif-version651-linux.yml';data=p.read_bytes();text=data.decode()
 for key,relative in [('ORIGINAL_PLAN','Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json'),('NATIVE_PLAN','Native/Erlang.NifVersion651Observations/linux-build-inputs.json'),('CAPTURE_PLAN','Native/Erlang.NifVersion651Observations/linux-inputs.json'),('ACCEPTANCE_PLAN','Native/Erlang.NifLinuxCallbackObservations/acceptance-inputs.json')]:
  text=re.sub(r'(?m)^(      '+key+r': )[0-9A-F]{64}',lambda m:m[1]+sha((root/'tools'/relative).read_bytes()),text)
 if text.encode()!=data:p.write_bytes(text.encode());audit.append(dict(Plan=p.relative_to(root).as_posix(),ExplicitN1Pins=True))
 for workflow,key,relative in [('nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256','Native/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'),('nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256','Native/Erlang.NifReallocationObservations/SOURCE-PLAN.json')]:
  p=root/'.github/workflows'/workflow;raw=p.read_bytes()
  data=re.sub(rb'('+key.encode()+rb': )[0-9A-F]{64}',lambda m:m[1]+sha((root/'tools'/relative).read_bytes()).encode(),raw)
  if data!=raw:p.write_bytes(data);audit.append(dict(Plan=p.relative_to(root).as_posix(),ExplicitReviewedPin=True))
 # All existing integrated workflow roles bind to their declared producer paths.
 p=root/'.github/workflows/nif-upgrade-integrated.yml';raw=p.read_bytes();text=raw.decode()
 integrated=root/'tools/Native/Erlang.NifIntegratedObservations/inputs.json';d=json.loads(integrated.read_bytes())
 pins={'INTEGRATED_PLAN':sha(integrated.read_bytes()),**{k:sha((root/v['Path']).read_bytes()) for k,v in d['PlanPins'].items()}}
 for key,digest in pins.items():text=re.sub(r'(?m)^(      '+key+r': )[0-9A-F]{64}',lambda m:m[1]+digest,text)
 if text.encode()!=raw:p.write_bytes(text.encode());audit.append(dict(Plan=p.relative_to(root).as_posix(),ExplicitIntegratedPins=True))
 p=root/'.github/workflows/validate.yml';raw=p.read_bytes();text=raw.decode()
 for name in ['Erlang.NifMapFamily504Observations','Erlang.NifMapMaterialized505Observations','Erlang.NifMapReentry505Observations']:
  digest=sha((root/'tools/Native'/name/'source-plan.json').read_bytes())
  text=re.sub(r'('+re.escape(name)+r'/collect.py[^\r\n]*?--plan-sha256 )[0-9A-F]{64}',lambda m:m[1]+digest,text)
 if text.encode()!=raw:p.write_bytes(text.encode());audit.append(dict(Plan=p.relative_to(root).as_posix(),ExplicitMapPins=True))
 if len(audit)==before:break
else:raise ValueError('Dependency pins did not stabilize')
(out/'refresh-audit.json').write_bytes((json.dumps(dict(Scope='Declared source identities and existing pins only',NativeExecuted=False,AcceptanceStatusesChanged=False,FullMembershipClaimed=False,MonitorSourceFreeze=dict(Path=monitor_current.relative_to(root).as_posix(),ParentPath=monitor['ParentPath'],ParentSha256=monitor['ParentSha256'],Rows=len(monitor['Rows'])),Iterations=iteration+1,Updates=audit),indent=2)+'\n').encode())
print('Stabilized',iteration+1,'iterations;',len(audit),'updates')
