from pathlib import Path
import hashlib,json,zipfile

a=Path(__file__).parent
root=a/'part-264-loader-iodata-contrasts'
native=root/'loader-iodata-contrasts'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
head='d45b59da199dfc682b8652a2d0e516662777d570'
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
metadata=json.loads((a/'part-264-loader-iodata-contrasts-metadata.json').read_text())
run=metadata['Run'];job=metadata['Jobs'][0];artifact=metadata['Artifact']
assert run['id']==38558205507 and run['head_sha']==head and run['conclusion']=='success'
assert job['id']==115832506435 and job['head_sha']==head and all(s['conclusion']=='success' for s in job['steps'])
assert artifact['id']==11891616849 and artifact['workflow_run']['head_sha']==head and not artifact['expired']
archive=a/'part-264-loader-iodata-contrasts.zip'
assert sha(archive)==metadata['ZipSHA256']==artifact['digest'][7:].upper()
with zipfile.ZipFile(archive) as z:
    members=[i for i in z.infolist() if not i.is_dir()]
    assert {i.filename for i in members}=={p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}
    for i in members:assert z.read(i)==(root/i.filename).read_bytes()
build=json.loads((root/'beam-source-build-evidence.json').read_text())
assert build['SourceCommit']==pin and build['SourceTag']=='OTP-29.1.1' and build['HarnessCommit']==head
assert build['SourceBuildCompleted'] and build['RunId']==str(run['id']) and build['BuildArguments']==['make','-j2']
assert sha(root/'beam-otp-build.log')==build['SourceBuildLogSha256']==sha(native/'source-build.log')
report=json.loads((native/'loader-iodata-contrasts.json').read_text())
assert report['BuildEvidence']==build
for stage in ['compile','disassemble','runtime-version']:
    assert (native/(stage+'.exit')).read_text().strip()=='0'
    assert (native/(stage+'.raw-complete')).read_text().strip().lower()=='true'
runtime=(native/'invoked-runtime.txt').read_text()
assert '"29","17.1","x86_64-pc-linux-gnu",opt,jit' in runtime and build['InstalledOtpRoot'] in runtime
digest=json.loads((a/'part-264-iodata-job.log.digest.json').read_text())
assert sha(a/'part-264-iodata-job.log')==digest['SHA256']
log=(a/'part-264-iodata-job.log').read_text()
assert pin in log and 'SixteenIndependentIodataNativeSummariesVerified' in log
independent=json.loads((a/'part264-iodata-independent-native-guard.json').read_text())
assert independent['NativeExecutions']==16 and independent['EqualWrongActualExpectedRejected']
result={'Status':'ExactHeadNative16ProvenanceAndAllRawBytesVerified','HarnessHead':head,'RunId':run['id'],'JobId':job['id'],'ArtifactId':artifact['id'],'ZipSha256':sha(archive),'RawFileCount':len(members),'PinnedSourceCommit':pin,'NativeFreshVmExecutions':16,'IndependentGuardSha256':sha(a/'part264-iodata-independent-native-guard.json'),'NativeRuntimeRaw':runtime,'WorkingSourceBindingFailurePreserved':True,'ActualSdkExecuted':False,'AnyFixtureOrOutcomeNormalization':False}
(a/'part264-iodata-provenance-guard.json').write_text(json.dumps(result,indent=2)+'\n')
print(result['Status'],len(members))
