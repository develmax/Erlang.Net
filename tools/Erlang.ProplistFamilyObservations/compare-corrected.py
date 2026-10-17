"""Passive exact comparison of admitted family captures; no VM, SDK or normalization."""
from pathlib import Path
import argparse,hashlib,importlib.util,json

def digest(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(p,name):
    s=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def named_rows(node,names):
    assert node[0]=='list' and len(node[1])==len(names)
    result={}
    for expected,row in zip(names,node[1]):
        assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',expected) and expected not in result
        result[expected]=row[1][1]
    return result

def equal(left,right,policy):
    if policy!='unordered_list':return left==right
    if left==right:return True
    if not(left[0]==right[0]=='tuple' and len(left[1])==len(right[1])==2 and left[1][0]==right[1][0]==('atom','ok')):return False
    a,b=left[1][1],right[1][1]
    return a[0]==b[0]=='list' and len(a[1])==len(b[1]) and len(set(a[1]))==len(a[1]) and set(a[1])==set(b[1])

def main():
    p=argparse.ArgumentParser()
    for key in ['original','sdk-capture','sdk-capture-seal','sdk-capture-seal-sha256','output']:p.add_argument('--'+key,required=True)
    a=p.parse_args();t=Path(__file__).resolve().parent;o=Path(a.original).resolve();sdk=Path(a.sdk_capture).resolve();out=Path(a.output);assert not out.exists()
    profile=json.loads((t/'profile.json').read_bytes());completion=json.loads((o/'completion.json').read_bytes())
    assert completion['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and completion['FailedGroups']==0 and completion['AllGroupsAttempted']
    assert completion['ProfileSha256']==digest(t/'profile.json') and completion['SourceInputsSha256']==digest(t/'source-inputs.corrected.json')
    decoder=load(t/'passive_etf_family.py','worker_compare_decoder');decode=lambda path:decoder.decode_exact(Path(path).read_bytes())
    seal=Path(a.sdk_capture_seal);assert digest(seal)==a.sdk_capture_seal_sha256.upper()
    admitted={}
    for row in json.loads(seal.read_bytes())['Files']:
        path=(sdk/row['Path']).resolve();assert path.is_relative_to(sdk) and path.stat().st_size==row['Bytes'] and digest(path)==row['Sha256'];admitted[row['Path']]=row['Sha256']
    comparisons=[];problems=[]
    for fixture in profile['Fixtures']:
        try:
            group=next(x for x in completion['Groups'] if x['Id']==fixture['Id']);directory=o/fixture['Id'];right=sdk/fixture['Id']
            for filename,key in [('returned.etf','ReturnedSha256'),('stack-observations.etf','RawStacksSha256'),('metadata.etf','MetadataSha256')]:assert digest(directory/filename)==group[key]
            for name in ['returned.etf','stack-observations.etf']:assert fixture['Id']+'/'+name in admitted
            left_values=decode(directory/'returned.etf');right_values=decode(right/'returned.etf')
            left=named_rows(left_values,fixture['Names']);right_rows=named_rows(right_values,fixture['Names'])
            case_rows=[dict(Name=case['Name'],Exact=left[case['Name']]==right_rows[case['Name']],Policy=case['Policy'],PolicyDiagnosticEqual=equal(left[case['Name']],right_rows[case['Name']],case['Policy'])) for case in fixture['Cases']]
            original_stacks=decode(directory/'stack-observations.etf');sdk_stacks=decode(right/'stack-observations.etf')
            full_values_equal=left_values==right_values;full_stacks_equal=original_stacks==sdk_stacks
            comparisons.append(dict(Group=fixture['Id'],Cases=case_rows,FullValuesEqual=full_values_equal,FullStacksEqual=full_stacks_equal,Passed=full_values_equal and full_stacks_equal,OriginalReturnedSha256=digest(directory/'returned.etf'),SdkReturnedSha256=digest(right/'returned.etf'),OriginalStacksSha256=digest(directory/'stack-observations.etf'),SdkStacksSha256=digest(right/'stack-observations.etf')))
        except BaseException as e:problems.append(dict(Group=fixture['Id'],Error=repr(e)))
    passed=not problems and len(comparisons)==len(profile['Fixtures']) and all(x['Passed'] for x in comparisons)
    payload=dict(Status='PASS_ALL_WHOLE_WORKER_OBSERVATIONS' if passed else 'FAILED_WHOLE_WORKER_OBSERVATIONS_RAW_RETAINED',Groups=comparisons,AcquisitionProblems=problems,AllGroupsAttempted=True,Normalization=False,Projection=False,PolicyComparisonsDiagnosticOnly=True,OpaqueIdentityDifferencesRemainFailures=True,CompatibilityPromoted=False)
    out.write_text(json.dumps(payload,indent=2)+'\n')
    if not passed:raise SystemExit(1)
if __name__=='__main__':main()
