from pathlib import Path
import hashlib,json,shutil

root=Path.cwd();a=root/'artifacts'
dest=a/'part266-native-staging/docs/validation/part-266/native264'
dest.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
guard=json.loads((a/'part264-iodata-provenance-guard.json').read_text())
assert guard['Status']=='ExactHeadNative16ProvenanceAndAllRawBytesVerified' and guard['RawFileCount']==438
paths=['part264-working-source-binding-failure-recheck.stdout','part264-working-source-binding-failure-recheck.stderr','part264-working-source-binding-failure-recheck.json','part-264-loader-iodata-contrasts.zip','part-264-loader-iodata-contrasts-metadata.json','part-264-iodata-job.log','part-264-iodata-job.log.digest.json','part-264-iodata-latest-provenance.json','part264-iodata-provenance-guard.py','part264-iodata-provenance-guard.json','part264-iodata-independent-native-guard.json','part264-initial-local-source-binding-failure.json','part-264-frozen-head.txt','reference-provenance264-iodata.ps1']
rows=[]
def copy(src,target):
    target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,target)
    assert src.read_bytes()==target.read_bytes()
    rows.append({'Path':target.relative_to(dest).as_posix(),'Length':src.stat().st_size,'Sha256':sha(src)})
for name in paths:copy(a/name,dest/name)
for directory in ['part-264-loader-iodata-contrasts','part264-committed-iodata-source']:
    for source in (a/directory).rglob('*'):
        if source.is_file():copy(source,dest/directory/source.relative_to(a/directory))
for name in ['part-264-validation.zip','part-264-validation-metadata.json','part-264-ci-job.log','part-264-ci-job.log.digest.json','part-264-ci-latest-provenance.json','part264-ci-final-guard.json','reference-provenance264-ci.ps1']:
    copy(a/name,dest/'ci264'/name)
for source in (a/'part-264-validation').rglob('*'):
    if source.is_file():copy(source,dest/'ci264/part-264-validation'/source.relative_to(a/'part-264-validation'))
receipt={'Status':'ArtifactOnlyExactNative264AndCi264Staged','Head':guard['HarnessHead'],'NativeOriginalExecutions':16,'RawNativeFiles':438,'Ci264Passed':6481,'SdkNative16Execution':False,'InitialLocalSourceHashFailureRetained':True,'FailureCause':'Windows working CRLF source hash binding; committed Git blob source verified against raw Linux source with no data normalization','AllSourceAndExpectedBytesUnchanged':True,'Rows':rows,'ProposedGitAttribute':'docs/validation/part-266/native264/** binary','MainOrGitEdits':False}
path=dest/'staging-receipt.json';path.write_text(json.dumps(receipt,indent=2)+'\n')
print('Staged',len(rows),'raw evidence files; receiptSHA',sha(path))
