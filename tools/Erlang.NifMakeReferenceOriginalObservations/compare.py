"""Passive acquired-row comparison; producer completion hashes come from reviewed acquisitions."""
from pathlib import Path
import argparse,hashlib,json,importlib.util
p=argparse.ArgumentParser()
for k in ('original','sdk','output','approved-original-completion-sha256','approved-sdk-completion-sha256'):p.add_argument('--'+k,required=True)
a=p.parse_args();root=Path(__file__).resolve().parents[2];o=Path(a.original);s=Path(a.sdk);out=Path(a.output);assert not out.exists()
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();J=lambda p:json.loads(p.read_bytes())
assert H(o/'completion.json')==a.approved_original_completion_sha256.upper();assert H(s/'completion.json')==a.approved_sdk_completion_sha256.upper()
oc=J(o/'completion.json');sc=J(s/'completion.json');assert oc['Status']=='PASS_ORIGINAL_MAKE_REFERENCE_CALLBACK_SELECTED_CONTRACTS' and sc['Status']=='ACQUIRED_OWNED_MAKE_REFERENCE_SDK_OBSERVATIONS'
assert H(o/'returned.etf')==oc['ReturnedSha256'];assert H(s/'observation/returned.etf')==sc['ReturnedSha256'];assert H(s/'observation/completion.json')==sc['DriverCompletionSha256'];assert H(s/'inputs.json')==sc['InputsSha256']
dc=J(s/'observation/completion.json');assert dc['Status']=='native_image_retired' and dc['ObservationCount']==9 and dc['ObservationShapeOnly'] and dc['NoExpectedResultSeededFromManaged'];assert dc['ReturnedSha256']==sc['ReturnedSha256']
assert H(s/'admission.json')==sc['AdmissionSha256']==dc['AdmissionSha256']==J(s/'inputs.json')['AdmissionSha256'];admission=J(s/'admission.json')
assert admission['Status']=='ROOT_OWN_MAKE_REFERENCE_CALLBACK_FIXTURE_ADMISSION'
for key in ('FixtureManifestSha256','NativeSourceSha256','ErlangSourceSha256','LibrarySha256'):
 assert admission[key]==sc[key]==dc[key]
assert H(s/'native-build-receipt.json')==sc['NativeBuildReceiptSha256']==admission['BuildReceiptSha256']==dc['BuildReceiptSha256'];sb=J(s/'native-build-receipt.json');assert sb['Status']=='PASS_OWNED_WINDOWS_NATIVE_ENTRY_FIXTURE_BUILD' and sb['Platform']=='windows-x64-llp64' and len(sb['Images'])==1;assert sb['SourceManifestSha256']==sc['FixtureManifestSha256'];assert sb['Images'][0]['SourceSha256']==sc['NativeSourceSha256'] and sb['Images'][0]['Sha256']==sc['LibrarySha256']
assert H(o/'source-manifest.json')==oc['SourceManifestSha256']==sc['FixtureManifestSha256'];manifest=J(o/'source-manifest.json');assert manifest['PinnedOtpCommit']==oc['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
files={x['Path']:x for x in manifest['Files']};assert files['native/make_reference_callback_contract.c']['Sha256']==oc['NativeSourceSha256']==sc['NativeSourceSha256'];assert files['original/make_reference_callback_contract.erl']['Sha256']==oc['ErlangSourceSha256']==sc['ErlangSourceSha256']==H(o/'make_reference_callback_contract.erl')
assert H(o/'cases.json')==files['cases.json']['Sha256'];names=J(o/'cases.json')['ObservationNames'];assert len(names)==len(set(names))==9 and oc['ObservationNames']==names
assert H(o/'native-build-receipt.json')==oc['NativeBuildReceiptSha256'];ob=J(o/'native-build-receipt.json');assert ob['Status']=='PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD' and ob['PinnedOtpCommit']==oc['PinnedOtpCommit'] and ob['SourceManifestSha256']==oc['SourceManifestSha256'] and len(ob['Images'])==1 and ob['Images'][0]['SourceSha256']==oc['NativeSourceSha256']
assert H(s/'owned/process.exit.json')==sc['ProcessExitSha256'];sr=J(s/'owned/process.exit.json');assert sr['ExitCode']==0 and sr['Settled'] and not sr['TimedOut'] and not sr['Failures'] and sr['KillOnCloseJobAssigned'] and not sr['JobTerminationRequested'] and sr['RemainingJobProcesses']==0
for stage in oc['OwnedStages']:
 name=stage['Name'];rp=o/(name+'.process.json');assert H(rp)==stage['ReceiptSha256'];r=J(rp);assert r['ExitCode']==0 and r['ParentReaped'] and r['StreamsClosed'] and not r['TerminationRequested'] and not r['RemainingGroupKillRequested'] and r['CleanupFailure'] is None
 assert H(o/(name+'.stdout'))==stage['StdoutSha256'] and H(o/(name+'.stderr'))==stage['StderrSha256']
path=root/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py';dependency=next(x for x in manifest['Dependencies'] if x['Path']=='tools/Erlang.GuestConnectedSendObservations/passive_etf.py');assert H(path)==dependency['Sha256'];spec=importlib.util.spec_from_file_location('tb_decode',path);d=importlib.util.module_from_spec(spec);spec.loader.exec_module(d)
original=d.decode_exact((o/'returned.etf').read_bytes());sdk=d.decode_exact((s/'observation/returned.etf').read_bytes());assert original[0]==sdk[0]=='tuple' and len(original[1])==4 and len(sdk[1])==3 and original[1][0]==sdk[1][0]==('atom','make_reference_callback_contract')
for value in (original,sdk):
 rows=value[1][1];assert rows[0]=='list' and len(rows)==2 and len(rows[1])==9
 assert [row[1][0] for row in rows[1]]==[('atom',name) for name in names] and all(row[0]=='tuple' and len(row[1])==2 for row in rows[1])
assert original[1][2]==('list',tuple(('integer',n) for n in [4,8,8,40]));assert sdk[1][2]==('list',tuple(('integer',n) for n in [4,4,8,40]));assert original[1][3]==('tuple',(('atom','cleanup'),('atom','deleted_and_purged')))
equal=original[1][1]==sdk[1][1];report=dict(Status='ACTUAL_ROWS_EQUAL' if equal else 'ACTUAL_ROWS_DIFFER_RAW_RETAINED',Equal=equal,ObservationNames=names,OriginalGroups=9,SdkGroups=9,OriginalLp64Widths=[4,8,8,40],WindowsLlp64Widths=[4,4,8,40],WidthEqualityClaimed=False,OriginalCodeDeleteAndSdkImageRetirementNotEquated=True,SourceManifestSha256=oc['SourceManifestSha256'],NativeSourceSha256=oc['NativeSourceSha256'],ErlangSourceSha256=oc['ErlangSourceSha256'],OriginalCompletionSha256=H(o/'completion.json'),SdkCompletionSha256=H(s/'completion.json'),OriginalSha256=H(o/'returned.etf'),SdkSha256=H(s/'observation/returned.etf'),NoExpectedOutputSeededFromManaged=True)
out.write_bytes((json.dumps(report,indent=2)+'\n').encode());assert equal
