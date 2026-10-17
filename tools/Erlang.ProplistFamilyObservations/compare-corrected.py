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
    for key in ['original','sdk-family-results','sdk-family-seal','sdk-family-seal-sha256','sdk-family-stacks','sdk-family-stacks-sha256','sdk-old59-returned','sdk-old59-returned-sha256','sdk-old59-stacks','sdk-old59-stacks-sha256','output']:p.add_argument('--'+key,required=True)
    a=p.parse_args();t=Path(__file__).resolve().parent;o=Path(a.original);out=Path(a.output);assert not out.exists()
    profile=json.loads((t/'profile.json').read_bytes());completion=json.loads((o/'completion.json').read_bytes())
    assert completion['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and completion['FailedGroups']==0 and completion['AllGroupsAttempted']
    assert completion['ProfileSha256']==digest(t/'profile.json') and completion['SourceInputsSha256']==digest(t/'source-inputs.corrected.json')
    decoder=load(t/'passive_etf_family.py','family_compare_decoder');decode=lambda path:decoder.decode_exact(Path(path).read_bytes())
    assert digest(a.sdk_family_seal)==a.sdk_family_seal_sha256.upper()
    seal=json.loads(Path(a.sdk_family_seal).read_bytes());base=Path(a.sdk_family_results).resolve()
    admitted={}
    for row in seal['Files']:
        f=(base/row['Path']).resolve();assert f.is_relative_to(base) and f.stat().st_size==row['Bytes'] and digest(f)==row['Sha256'];admitted[row['Path']]=row['Sha256']
    comparisons=[];problems=[]
    for fixture in profile['Fixtures']:
        try:
            group=next(x for x in completion['Groups'] if x['Id']==fixture['Id']);directory=o/fixture['Id']
            for filename,key in [('returned.etf','ReturnedSha256'),('stack-observations.etf','RawStacksSha256'),('metadata.etf','MetadataSha256')]:assert digest(directory/filename)==group[key]
            original=named_rows(decode(directory/'returned.etf'),fixture['Names'])
            if fixture['Id']=='family148':
                for case in fixture['Cases']:
                    lane_values=[]
                    for lane in ['direct','compiled']:
                        try:
                            name=case['Name']+'.'+lane+'.etf';assert name in admitted
                            actual=decode(base/name);passed=equal(original[case['Name']],actual,case['Policy'])
                            comparisons.append(dict(Group=fixture['Id'],Name=case['Name'],Mfas=case['Mfas'],Lane=lane,Policy=case['Policy'],Passed=passed,OriginalDecoded=repr(original[case['Name']]),SdkDecoded=repr(actual)))
                        except BaseException as e:
                            problems.append(dict(Group=fixture['Id'],Name=case['Name'],Lane=lane,Error=repr(e),DependentComparisonIncomplete=True))
                stack_path=Path(a.sdk_family_stacks);assert digest(stack_path)==a.sdk_family_stacks_sha256.upper()
            else:
                sdk_path=Path(a.sdk_old59_returned);assert digest(sdk_path)==a.sdk_old59_returned_sha256.upper();sdk=named_rows(decode(sdk_path),fixture['Names'])
                for case in fixture['Cases']:
                    observed=original[case['Name']];passed=observed==sdk[case['Name']]==('tuple',(('atom','ok'),('atom','done')))
                    comparisons.append(dict(Group=fixture['Id'],Name=case['Name'],Mfas=case['Mfas'],Lane='compiled_existing59',Policy='exact_assertion',Passed=passed,OriginalDecoded=repr(observed),SdkDecoded=repr(sdk[case['Name']])))
                stack_path=Path(a.sdk_old59_stacks);assert digest(stack_path)==a.sdk_old59_stacks_sha256.upper()
            # Whole raw term comparison: no dropping helpers, file names, lines, args, or depth rows.
            left=decode(directory/'stack-observations.etf');right=decode(stack_path)
            comparisons.append(dict(Group=fixture['Id'],Name='entire_unprojected_stack_capture',Mfas=sorted({m for x in fixture['Cases'] for m in x['Mfas']}),Lane='shared_fixture_stacks',Policy='whole_decoded_term',Passed=left==right,OriginalSha256=digest(directory/'stack-observations.etf'),SdkSha256=digest(stack_path)))
        except BaseException as e:problems.append(dict(Group=fixture['Id'],Error=repr(e),DependentComparisonsIncomplete=True))
    mfas=[]
    for mfa in profile['Mfas']:
        rows=[x for x in comparisons if mfa in x['Mfas']]
        mfas.append(dict(Mfa=mfa,Compared=len(rows),Failed=sum(not x['Passed'] for x in rows),CompleteClassification='PENDING_REVIEW_EVEN_IF_FINITE_CASES_PASS',OriginalWholeStacksCompared=all(any(x['Group']==f['Id'] and x['Name']=='entire_unprojected_stack_capture' for x in rows) for f in profile['Fixtures'] if any(mfa in c['Mfas'] for c in f['Cases']))))
    payload=dict(Status='PASS_FINITE_FAMILY_COMPARISONS_PENDING_PER_MFA_REVIEW' if not problems and all(x['Passed'] for x in comparisons) else 'FAILED_FAMILY_COMPARISONS_RAW_RETAINED',Comparisons=comparisons,Problems=problems,Mfas=mfas,Published=False,CanonicalMfaDelta=0,CompatibilityPromoted=False)
    out.write_text(json.dumps(payload,indent=2)+'\n')
    if payload['Status'].startswith('FAILED'):raise SystemExit(1)
if __name__=='__main__':main()
