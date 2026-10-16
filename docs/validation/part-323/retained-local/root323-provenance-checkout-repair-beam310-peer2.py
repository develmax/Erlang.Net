from pathlib import Path
import json,hashlib,subprocess,re
r=Path.cwd();a=r/'artifacts';old=a/'root-part323-composed1';new=a/'root-part323-composed2';h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();sha=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());resource='tests/Erlang.Tests/Resources/Beam/guest_connected_bang322.provenance.json';probe=a/'root323-provenance-checkout-probe1/report.json';p=j(probe);assert h(probe)=='87C5EB2424ECB634C32EA1812DE59DCC5F1A9F0713B1BB872583BA5AB07CF112';assert p['Published'] is False;oid=p['GitRawBlob'];raw=subprocess.check_output(['git','cat-file','blob',oid]);assert sha(raw)==p['WorkingSha256']=='3718A2F53E9BF8B19110BD33AD00FC5D487AB285C9DAEE87EE8494FC21FAB631'
profiles=[]
for label,candidate in [('before',old/'candidate'),('after',new/'candidate')]:
 for autocrlf in ['false','true']:
  cmd=['git','--git-dir='+str(r/'.git'),'--work-tree='+str(candidate),'-c','core.autocrlf='+autocrlf];result=subprocess.check_output(cmd+['cat-file','--filters','--path='+resource,oid],cwd=candidate);attrs=subprocess.check_output(cmd+['check-attr','-a','--',resource],cwd=candidate).decode();profiles.append({'Phase':label,'CoreAutocrlf':autocrlf,'Bytes':len(result),'Sha256':sha(result),'EffectiveAttributes':attrs,'RawExact':result==raw})
  if label=='before' and autocrlf=='true':assert sha(result)=='4675CEE94EC78D926DD678D831A0AA42A398E97C8C1DC5E659CC552B42388404' and len(result)==2872 and result!=raw
  else:assert result==raw
  if label=='after':assert 'text: unset' in attrs and 'binary: set' in attrs
f=new/'source-freeze.json';cl=new/'source-resource-closure.json';v=j(f);assert h(f)=='F4A6F6E64D5D587BA48F70CA2A849981A1D697A93954C275A8C8825C7CD18EAD';assert h(cl)==v['FinalSourceClosureSha256'];rows=j(cl)['Files'];oldmap={x['Path']:x for x in j(old/'source-resource-closure.json')['Files']};assert len(rows)==2349;changed=set()
for x in rows:
 path=new/'candidate'/x['Path'];assert path.stat().st_size==x['Bytes'] and h(path)==x['Sha256']
 if x!=oldmap[x['Path']]:changed.add(x['Path'])
assert changed==set(v['MetadataOnlyChangedPaths'])=={'.gitattributes','.github/workflows/nif-reallocation-native.yml','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
attrs=(new/'candidate/.gitattributes').read_bytes();before=(old/'candidate/.gitattributes').read_bytes();assert attrs==before.rstrip(b'\n')+b'\n'+resource.encode()+b' binary\n'
for plan in v['Plans']:
 path=new/'candidate'/plan['Path'];value=j(path);prior=j(old/'candidate'/plan['Path']);assert len(value['Files'])==plan['Inputs'];assert {k:z for k,z in value.items() if k!='Files'}=={k:z for k,z in prior.items() if k!='Files'}
 diff=[(x,y) for x,y in zip(prior['Files'],value['Files']) if x!=y]
 if plan['Inputs']==1108:assert diff==[] and path.read_bytes()==(old/'candidate'/plan['Path']).read_bytes()
 else:assert len(diff)==1 and diff[0][0]['Path']==diff[0][1]['Path']=='.gitattributes' and plan['Sha256']=='2A31DD136547D9F9395930CF6EC5C02CD28CB426C94BE385757F6696E02D89D5'
 assert h(path)==plan['Sha256'] and b'\r' not in path.read_bytes();assert re.findall(r'^\s*'+plan['Pin']+r': ([A-F0-9]{64})$',(new/'candidate'/plan['Workflow']).read_text(),re.M)==[plan['Sha256']]
 for x in value['Files']:
  z=(new/'candidate'/x['Path']).read_bytes();z=z.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else z;assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
out=a/'root323-provenance-checkout-repair-beam310-peer.json';assert not out.exists();out.write_text(json.dumps({'Status':'PASS_SOURCE_REPAIR_AND_ACTUAL_READONLY_GIT_SMUDGE_RESOURCE_BYTE_PROFILES','SourceFreezeSha256':h(f),'SourceClosureSha256':h(cl),'SourceRowsRehashed':2349,'OwnedPaths':30,'CSharpPaths':13,'ChangedPaths':sorted(changed),'ActualProfiles':profiles,'AllThirteenCompiledCSharpAndBothResourcesRawUnchanged':True,'Legacy1108PlanRawUnchanged':True,'Own1133PlanOnlyAttributeInputRowChangedAndWorkflowResealed':True,'NativeNonFilesPoliciesAndSelectorsExact':True,'HazardCaughtBeforePublicationNotRuntimeFailure':True,'PriorMain1FullAndNativePassStillValidAsPreattributeEvidence':True,'FreshMain2FullRequiredForReboundAuthority':True,'ReviewerGitObjectWritesOrMainSdkNativeBuildVmMutation':False},indent=2)+'\n');print(json.dumps({'PeerSha256':h(out),'ClosureSha256':h(cl)}))
