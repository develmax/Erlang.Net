"""Passive actual decoded-field comparison. No expected result is synthesized."""
from pathlib import Path
import argparse,hashlib,importlib.util,json
R=Path(__file__).resolve().parents[2]
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def main():
 p=argparse.ArgumentParser()
 for key in ['capture-root','approved-size-completion-sha256','approved-reference-completion-sha256','approved-sdk-completion-sha256','approved-admission-sha256','output']:p.add_argument('--'+key,required=True)
 a=p.parse_args();root=Path(a.capture_root).resolve();sdk=root/'sdk';admission=root/'bridge/admission.json'
 assert H(admission)==a.approved_admission_sha256.upper() and H(sdk/'completion.json')==a.approved_sdk_completion_sha256.upper()
 ad=J(admission);sc=J(sdk/'completion.json');assert ad['Status']=='ROOT_OWN_LINUX_CALLBACK17_ADMISSION' and sc['Status']=='ACQUIRED_EXPERIMENTAL_REVIEWED_CALLBACK17' and sc['SubjectHead']==ad['SubjectHead'] and sc['AdmissionSha256']==H(admission)
 assert H(sdk/'inputs.json')==sc['InputsSha256']
 assert sc['Routes']==['reviewed'] and sc['PrimaryRowsPerRoute']==17 and sc['RuntimeDomainsPerRoute']==2
 for stage in sc['OwnedStages']:
  name=stage['Name'];receipt=sdk/(name+'.process.json');assert H(receipt)==stage['ReceiptSha256']
  value=J(receipt);assert value['ExitCode']==0 and value['ParentReaped'] and value['StreamsClosed'] and not value['TerminationRequested'] and not value['RemainingGroupKillRequested'] and value['CleanupFailure'] is None
  assert H(sdk/(name+'.stdout'))==stage['StdoutSha256'] and H(sdk/(name+'.stderr'))==stage['StderrSha256']
 decoderpath=R/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py'
 spec=importlib.util.spec_from_file_location('linux17_passive',decoderpath);d=importlib.util.module_from_spec(spec);spec.loader.exec_module(d)
 originals={}
 for family,approved in [('size',a.approved_size_completion_sha256),('reference',a.approved_reference_completion_sha256)]:
  original=root/(family+'-original');assert H(original/'completion.json')==approved.upper();oc=J(original/'completion.json');image=next(i for i in ad['Images'] if ('map_size' in i['Module'])==(family=='size'))
  assert oc['Status'].startswith('PASS_ORIGINAL_') and oc['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
  assert H(original/'native-build-receipt.json')==oc['NativeBuildReceiptSha256']==image['BuildReceiptSha256']
  assert oc['SubjectHead']==ad['SubjectHead'] and oc['SourceManifestSha256']==image['ManifestSha256'] and oc['NativeSourceSha256']==image['NativeSourceSha256'] and oc['ErlangSourceSha256']==image['ErlangSourceSha256']
  assert H(original/'returned.etf')==oc['ReturnedSha256'] and H(original/'source-manifest.json')==oc['SourceManifestSha256']
  manifest=J(original/'source-manifest.json');dep=next(x for x in manifest['Dependencies'] if x['Path']=='tools/Erlang.GuestConnectedSendObservations/passive_etf.py');assert H(decoderpath)==dep['Sha256']
  assert H(original/'cases.json')==next(x['Sha256'] for x in manifest['Files'] if x['Path']=='cases.json')
  names=J(original/'cases.json')['ObservationNames'];assert names==image['ObservationNames'] and len(names)==(8 if family=='size' else 9)
  for stage in oc['OwnedStages']:
   name=stage['Name'];rp=original/(name+'.process.json');assert H(rp)==stage['ReceiptSha256'];r=J(rp);assert r['ExitCode']==0 and r['ParentReaped'] and r['StreamsClosed'] and not r['TerminationRequested'] and not r['RemainingGroupKillRequested'] and r['CleanupFailure'] is None
   assert H(original/(name+'.stdout'))==stage['StdoutSha256'] and H(original/(name+'.stderr'))==stage['StderrSha256']
  value=d.decode_exact((original/'returned.etf').read_bytes());assert value[0]=='tuple' and len(value[1])==4 and value[1][0]==('atom',image['Module']) and value[1][3]==('tuple',(('atom','cleanup'),('atom','deleted_and_purged')))
  originals[image['Module']]=value
 def diff(x,y,path,found):
  if type(x) is not type(y):found.append(dict(Path=path,Original=repr(x),Sdk=repr(y)));return
  if isinstance(x,(tuple,list)):
   if len(x)!=len(y):found.append(dict(Path=path,OriginalLength=len(x),SdkLength=len(y)))
   for i,(left,right) in enumerate(zip(x,y)):diff(left,right,path+'/'+str(i),found)
  elif x!=y:found.append(dict(Path=path,Original=repr(x),Sdk=repr(y)))
 reports=[]
 for route in ['reviewed']:
  completion=J(sdk/route/'completion.json');assert completion['Status']=='ACQUIRED_LINUX_CALLBACK17_TWO_RUNTIME_ROWS' and completion['Route']==route and completion['AdmissionSha256']==H(admission) and completion['RuntimeDomains']==2 and completion['FirstForeignAllocationOwnershipChecked'] and completion['AllImageRetirementsCompleted'] and completion['RuntimeDisposalsCompleted']
  bound={x['Path']:x['Sha256'] for x in completion['Files']}
  for name,sha in bound.items():assert H(sdk/route/name)==sha
  for domain in ['first','second']:
   for image in ad['Images']:
    leaf=domain+'/'+image['Module']+'.etf';assert leaf in bound
    actual=d.decode_exact((sdk/route/leaf).read_bytes());original=originals[image['Module']]
    assert actual[0]=='tuple' and len(actual[1])==3 and actual[1][0]==original[1][0]
    widths=('list',tuple(('integer',x) for x in [4,8,8,40]));assert original[1][2]==actual[1][2]==widths
    left=original[1][1];right=actual[1][1];assert left[0]==right[0]=='list' and len(left[1])==len(right[1])==len(image['ObservationNames'])
    for index,name in enumerate(image['ObservationNames']):
     x=left[1][index];y=right[1][index];assert x[1][0]==y[1][0]==('atom',name)
     changes=[];diff(x,y,'/'+route+'/'+domain+'/'+image['Module']+'/'+name,changes)
     reports.append(dict(Route=route,RuntimeDomain=domain,Name=name,OriginalDecoded=repr(x),SdkDecoded=repr(y),Equal=not changes,FieldDifferences=changes))
 assert len(reports)==34
 equal=all(x['Equal'] for x in reports);out=Path(a.output);assert not out.exists()
 out.write_text(json.dumps(dict(ProductionVariant='EXPERIMENT_BASE_LMID_DISTINCT_MEMFD_COPIES_DEEPBIND_REVIEWED_ONLY',Status='ACTUAL_EXPERIMENTAL_REVIEWED_34_ROWS_EQUAL' if equal else 'ACTUAL_LINUX_ROWS_DIFFER_RAW_RETAINED',Equal=equal,UniqueObservationGroups=17,Routes=1,RuntimeDomainsPerRoute=2,RowComparisons=len(reports),Widths=[4,8,8,40],WidthsComparedExactly=True,OriginalCleanupAndSdkRetirementIndependentlyRequired=True,WholeRawEtfByteEqualityClaimed=False,NoExpectedOutputSeededFromManaged=True,Rows=reports),indent=2)+'\n')
 assert equal
if __name__=='__main__':main()
