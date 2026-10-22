"""Passive full-field comparison against a separately admitted original capture."""
from pathlib import Path
import argparse,json,hashlib,importlib.util
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];TOOL=ROOT/'artifacts/erlang370-original-proposal1/tool'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def load(p,name):
    spec=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def span_validate(node,raw,decoder):
    if not isinstance(node,tuple):return
    if node and node[0]=='new_fun':
        marker,start,size=node[10];assert marker=='raw_span' and 0<=start<len(raw) and start+size<=len(raw)
        again=decoder.decode_exact(b'\x83'+raw[start:start+size]);assert again[:10]==node[:10]
        return
    for child in node:
        if isinstance(child,(tuple,frozenset)):
            for item in child:span_validate(item,raw,decoder)

def main():
    p=argparse.ArgumentParser();p.add_argument('--native',type=Path,required=True);p.add_argument('--sdk',type=Path,required=True);p.add_argument('--native-admission',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();out=a.output.resolve();assert not out.exists() and out.is_relative_to(ROOT/'artifacts')
    frozen=J(HERE/'source-freeze.json')
    for row in frozen['Files']:assert H(HERE/row['Path'])==row['Sha256']
    plan=J(TOOL/'source-inputs.corrected.json')
    for row in plan['ToolInputs']:assert H(TOOL/row['Path'])==row['Sha256']
    admission=J(a.native_admission);assert admission['Status']=='PASS_PASSIVE_ERLANG370_FIRST57_ORIGINAL_ADMISSION' and admission['Head']=='e62e0f383929a2f5acf83adc012ee26f11342bce'
    dlls=J(a.sdk.parent/'main-dll-closure.json');assert len(dlls['Files'])==5 and dlls['MainSource'] and not dlls['PrivateCandidateDllsUsed']
    for row in dlls['Files']:
        assert H(ROOT/row['Path'])==H(ROOT/row['CopiedTo'])==row['Sha256']
    receipt=J(a.sdk.parent/'erlang57-capture.exit.json');assert receipt['Settled'] and not receipt['TimedOut'] and receipt['ExitCode']==0 and not receipt['Failures']
    decoder=load(TOOL/'passive_etf_family.py','trusted_decode');relation=load(HERE/'fun_relation.py','trusted_relation');profile=J(TOOL/'profile.json');sdk=J(a.sdk/'completion.json')
    assert sdk['FailedGroups']==0 and len(sdk['Groups'])==8
    seal=J(a.sdk/'capture-seal.json')
    for row in seal['Files']:
        q=(a.sdk/row['Path']).resolve();assert q.is_relative_to(a.sdk.resolve()) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    results=[];failures=[]
    for fixture,group in zip(profile['Fixtures'],sdk['Groups']):
        assert group['Id']==fixture['Id'] and not group['ExecutionSkipped'] and group['Status']=='CAPTURED_PENDING_PASSIVE_CHECKS'
        source_names=[fixture['Path']]+fixture['Companions'];assert len(group['Compile'])==len(source_names)
        for name,compile in zip(source_names,group['Compile']):
            assert compile['Passed'] and compile['DeclaredSourceFile']==Path(name).name and compile['SourceSha256']==H(TOOL/name)
        directory=a.sdk/fixture['Id'];metadata=decoder.decode_exact((directory/'runtime-metadata.etf').read_bytes())
        assert metadata[0]=='tuple' and len(metadata[1])==3 and metadata[1][0]==('atom','sdk_runtime_provenance') and metadata[1][2][0]=='pid'
        init=metadata[1][2]
        for filename,hashkey in [('returned.etf','ReturnedSha256'),('stack-observations.etf','StacksSha256')]:
            assert H(directory/filename)==group[hashkey]
            nr=(a.native/fixture['Id']/filename).read_bytes();sr=(directory/filename).read_bytes();nv=decoder.decode_exact(nr);sv=decoder.decode_exact(sr)
            span_validate(nv,nr,decoder);span_validate(sv,sr,decoder)
            if filename=='returned.etf':batches=[('value',nv,sv)]
            else:
                assert nv[0]==sv[0]=='list' and len(nv[1])==len(sv[1])==5
                batches=[]
                for depth,n,s in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],nv[1],sv[1]):
                    assert n[0]==s[0]=='tuple' and n[1][0]==s[1][0]==depth and len(n[1])==len(s[1])==2
                    batches.append((repr(depth),n[1][1],s[1][1]))
            for depth,n,s in batches:
                assert n[0]==s[0]=='list' and len(n[1])==len(s[1])==len(fixture['Names'])
                for name,l,r in zip(fixture['Names'],n[1],s[1]):
                    assert l[0]==r[0]=='tuple' and len(l[1])==len(r[1])==2 and l[1][0]==r[1][0]==('atom',name)
                    allowed=fixture['Id']=='local_fun_order' and name.endswith(('_minimum','_maximum'))
                    try:kind,proof=relation.compare_outcome(l[1][1],r[1][1],allowed,init);error=None
                    except AssertionError as e:kind='different';proof=None;error=repr(e);failures.append(dict(Group=fixture['Id'],Name=name,Depth=depth,Original=repr(l),Sdk=repr(r),Error=error))
                    results.append(dict(Group=fixture['Id'],Name=name,Depth=depth,Status=kind,Proof=proof))
    assert len(results)==9582
    out.mkdir();(out/'rows.json').write_text(json.dumps(results,indent=2)+'\n');(out/'differences.json').write_text(json.dumps(failures,indent=2)+'\n')
    (out/'acceptance.json').write_text(json.dumps(dict(Status='PASS_ALL_FIELDS_WITH_DECLARED_FUN_CREATOR_RELATION' if not failures else 'DIFFERENCES_RETAINED',Rows=len(results),Strict=sum(r['Status']=='strict' for r in results),Relations=sum(r['Status']=='relation' for r in results),Differences=len(failures),OriginalAdmissionSha256=H(a.native_admission),SdkCaptureSealSha256=H(a.sdk/'capture-seal.json'),NoPromotion=True),indent=2)+'\n')
    if failures:raise SystemExit(1)
if __name__=='__main__':main()
