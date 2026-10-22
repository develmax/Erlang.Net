"""All typed values and full stacks; no PID, frame, float or error_info normalization."""
from pathlib import Path
import argparse,hashlib,importlib.util,json
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def main():
    p=argparse.ArgumentParser()
    for key in ['native','sdk','native-admission','tool','output']:p.add_argument('--'+key,type=Path,required=True)
    for key in ['expected-head','manifest-sha256','native-admission-sha256']:p.add_argument('--'+key,required=True)
    a=p.parse_args();out=a.output.resolve();assert not out.exists() and out.is_relative_to(ROOT/'artifacts')
    for row in J(HERE/'source-freeze.json')['Files']:assert H(HERE/row['Path'])==row['Sha256']
    tool=a.tool.resolve();manifest=tool/'source-inputs.corrected.json';assert H(manifest)==a.manifest_sha256.upper()
    for row in J(manifest)['ToolInputs']:
        q=tool/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    assert H(a.native_admission)==a.native_admission_sha256.upper()
    admission=J(a.native_admission)
    assert admission['Status']=='PASS_PASSIVE_ORDSETS371_ORIGINAL_ADMISSION' and admission['Head']==a.expected_head and admission['ManifestSha256']==H(manifest)
    assert admission['NativeCapture']==str(a.native.resolve()) and admission['Values']==112 and admission['FullStackProfiles']==560 and admission['MetadataCorruptionControls']==3
    for row in admission['NativeFiles']:
        q=(a.native/row['Path']).resolve();assert q.is_relative_to(a.native.resolve()) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    dlls=J(a.sdk.parent/'main-dll-closure.json');assert len(dlls['Files'])==5 and dlls['MainSource'] and not dlls['PrivateCandidateDllsUsed']
    dll_checks=[]
    for row in dlls['Files']:
        captured=(ROOT/row['CopiedTo']).resolve();assert captured.is_relative_to(ROOT/'artifacts') and H(captured)==row['Sha256']
        live=(ROOT/row['Path']).resolve();assert live.is_relative_to(ROOT)
        dll_checks.append(dict(Path=row['Path'],CapturePath=row['CopiedTo'],CapturedSha256=row['Sha256'],LiveSha256=H(live) if live.exists() else None,MatchesLive=live.exists() and H(live)==row['Sha256']))
    current=all(x['MatchesLive'] for x in dll_checks)
    receipt=J(a.sdk.parent/'erlang57-capture.exit.json');assert receipt['Settled'] and not receipt['TimedOut'] and receipt['ExitCode']==0 and not receipt['Failures']
    profile=J(tool/'profile.json');sdk=J(a.sdk/'completion.json');assert sdk['FailedGroups']==0 and len(sdk['Groups'])==len(profile['Fixtures'])==1
    assert H(tool/'profile.json')==admission['ProfileSha256']
    for row in J(a.sdk/'capture-seal.json')['Files']:
        q=(a.sdk/row['Path']).resolve();assert q.is_relative_to(a.sdk.resolve()) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    spec=importlib.util.spec_from_file_location('sealed_erlang371_decoder',tool/'passive_etf_family.py');decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
    rows=[];differences=[]
    for fixture,group in zip(profile['Fixtures'],sdk['Groups']):
        assert group['Id']==fixture['Id'] and not group['ExecutionSkipped'] and group['Status']=='CAPTURED_PENDING_PASSIVE_CHECKS'
        names=[fixture['Path']]+fixture['Companions'];assert len(names)==len(group['Compile'])
        for name,compiled in zip(names,group['Compile']):assert compiled['Passed'] and compiled['DeclaredSourceFile']==Path(name).name and compiled['SourceSha256']==H(tool/name)
        directory=a.sdk/fixture['Id'];metadata=decoder.decode_exact((directory/'runtime-metadata.etf').read_bytes())
        assert metadata[0]=='tuple' and len(metadata[1])==3 and metadata[1][0]==('atom','sdk_runtime_provenance') and metadata[1][2][0]=='pid'
        for filename,key in [('returned.etf','ReturnedSha256'),('stack-observations.etf','StacksSha256')]:
            assert H(directory/filename)==group[key]
            native=decoder.decode_exact((a.native/fixture['Id']/filename).read_bytes());managed=decoder.decode_exact((directory/filename).read_bytes())
            if filename=='returned.etf':batches=[('value',native,managed)]
            else:
                assert native[0]==managed[0]=='list' and len(native[1])==len(managed[1])==5;batches=[]
                for depth,left,right in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],native[1],managed[1]):
                    assert left[0]==right[0]=='tuple' and len(left[1])==len(right[1])==2 and left[1][0]==right[1][0]==depth
                    batches.append((repr(depth),left[1][1],right[1][1]))
            for depth,left,right in batches:
                assert left[0]==right[0]=='list' and len(left[1])==len(right[1])==len(fixture['Names'])
                for name,native_row,managed_row in zip(fixture['Names'],left[1],right[1]):
                    assert native_row[0]==managed_row[0]=='tuple' and len(native_row[1])==len(managed_row[1])==2 and native_row[1][0]==managed_row[1][0]==('atom',name)
                    equal=native_row[1][1]==managed_row[1][1];rows.append(dict(Group=fixture['Id'],Name=name,Depth=depth,Strict=equal))
                    if not equal:differences.append(dict(Group=fixture['Id'],Name=name,Depth=depth,Original=repr(native_row[1][1]),Sdk=repr(managed_row[1][1])))
    assert len(rows)==672 and sum(r['Depth']=='value' for r in rows)==112
    out.mkdir();(out/'rows.json').write_text(json.dumps(rows,indent=2)+'\n');(out/'differences.json').write_text(json.dumps(differences,indent=2)+'\n')
    (out/'acceptance.json').write_text(json.dumps(dict(Status='PASS_ALL672_FIELDS_STRICT' if not differences else 'DIFFERENCES_RETAINED',Rows=len(rows),Values=112,FullStackProfiles=560,Strict=len(rows)-len(differences),Differences=len(differences),NormalizedFields=0,Relations=0,OriginalAdmissionSha256=H(a.native_admission),SdkCaptureSealSha256=H(a.sdk/'capture-seal.json'),SdkDlls=dll_checks,FinalLiveDllClosure=current,HistoricalCapture=not current,FinalRecaptureRequired=not current,NoPromotion=True),indent=2)+'\n')
    if differences:raise SystemExit(1)
if __name__=='__main__':main()
