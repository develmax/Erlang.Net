"""Passive full-field comparator. Opaque candidates remain unverified, never waived."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def named(node):
    assert node[0]=='list'
    result={}
    for row in node[1]:
        assert row[0]=='tuple' and len(row[1])==2 and row[1][0] not in result
        result[row[1][0]]=row[1][1]
    return result

def outcome(node,stacks):
    assert node[0]=='tuple'
    fields=node[1]
    if fields[0]==('atom','ok'):assert len(fields)==3
    else:
        assert fields[0]==('atom','error') and len(fields)==(5 if stacks else 4)
        if stacks:
            assert fields[3][0]=='list'
            for frame in fields[3][1]:assert frame[0]=='tuple' and len(frame[1])==4
    effects=fields[-1]
    assert effects[0]=='tuple' and len(effects[1])==2
    assert all(part[0]=='list' for part in effects[1])

def difference(left,right,path='$',eligible=False):
    if left==right:return []
    if isinstance(left,tuple) and isinstance(right,tuple):
        if left and right and left[0]==right[0]=='reference':
            return [{'Path':path,'Kind':'OPAQUE_REFERENCE_PENDING_ACTUAL_PROVENANCE' if eligible else 'UNDECLARED_REFERENCE_DIFFERENCE','Original':repr(left),'Sdk':repr(right)}]
        if len(left)==len(right):
            errors=[]
            for index,(a,b) in enumerate(zip(left,right)):errors.extend(difference(a,b,f'{path}[{index}]',eligible))
            return errors
    return [{'Path':path,'Kind':'STRICT_FULL_FIELD_DIFFERENCE','Original':repr(left)[:4000],'Sdk':repr(right)[:4000]}]

def main():
    parser=argparse.ArgumentParser()
    for key in ['native','sdk','tool','output']:parser.add_argument('--'+key,type=Path,required=True)
    args=parser.parse_args();args.output.mkdir(parents=True,exist_ok=False)
    profile=json.loads((args.tool/'profile.json').read_bytes())
    decoder_path=args.tool/'passive_etf_family.py'
    plan=json.loads((args.tool/'source-inputs.corrected.json').read_bytes())
    for row in plan['ToolInputs']:
        path=args.tool/row['Path']
        assert path.is_file() and path.stat().st_size==row['Bytes'] and sha(path)==row['Sha256']
    assert profile['SubjectModule']=='binary' and len(profile['Mfas'])==2 and len(profile['Fixtures'])==1
    assert sum(len(group['Names']) for group in profile['Fixtures'])==7
    assert profile['ValueAcquisition']=='compiled-monitored-worker-values-and-stacks-v1' and all(group['ValueAcquisition']=='compiled-monitored-worker-values-and-stacks-v1' for group in profile['Fixtures'])
    assert all(group['Names'] and len(group['Names'])==len(set(group['Names'])) for group in profile['Fixtures'])
    assert sha(decoder_path)==next(row['Sha256'] for row in plan['ToolInputs'] if row['Path']=='passive_etf_family.py')
    spec=importlib.util.spec_from_file_location('binary369_trusted_passive',decoder_path);decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
    eligible={(row['Group'],row['Name']) for row in profile['OpaquePolicy']['EligibleCases']}
    rows=[];inputs=[];failures=[]
    for group in profile['Fixtures']:
        for filename in ['returned.etf','stack-observations.etf']:
            try:
                native=args.native/group['Id']/filename;sdk=args.sdk/group['Id']/filename
                inputs.extend([{'Path':str(path),'Sha256':sha(path)} for path in [native,sdk]])
                left=named(decoder.decode_exact(native.read_bytes()));right=named(decoder.decode_exact(sdk.read_bytes()))
                names=[('atom',name) for name in group['Names']]
                if filename=='returned.etf':
                    assert list(left)==list(right)==names
                    pairs=[('values',name,left[name],right[name]) for name in names]
                else:
                    depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
                    assert list(left)==list(right)==depths
                    pairs=[]
                    for depth in depths:
                        a=named(left[depth]);b=named(right[depth]);assert list(a)==list(b)==names
                        pairs.extend((repr(depth),name,a[name],b[name]) for name in names)
                for depth,name,a,b in pairs:
                    outcome(a,filename!='returned.etf');outcome(b,filename!='returned.etf')
                    errors=difference(a,b,eligible=(group['Id'],name[1]) in eligible)
                    rows.append({'Group':group['Id'],'Case':name[1],'Depth':depth,'StrictEqual':not errors,
                        'OpaqueProvenancePending':bool(errors) and all(error['Kind']=='OPAQUE_REFERENCE_PENDING_ACTUAL_PROVENANCE' for error in errors),'Differences':errors})
            except Exception as error:failures.append({'Group':group['Id'],'File':filename,'Error':repr(error)})
    result={'Status':'PASS_STRICT_PENDING_SOURCE_ADMISSION' if not failures and all(row['StrictEqual'] for row in rows) else 'DIFFERENCES_OR_OPAQUE_PROVENANCE_PENDING',
        'ExpectedRows':sum(len(group['Names'])*6 for group in profile['Fixtures']),'Rows':rows,'ActualRows':len(rows),'StrictEqual':sum(row['StrictEqual'] for row in rows),
        'OpaqueRowsNotAccepted':sum(row['OpaqueProvenancePending'] for row in rows),'Failures':failures,'Inputs':inputs,'ProfileSha256':sha(args.tool/'profile.json'),
        'DecoderSha256':sha(decoder_path),'Projection':False,'Normalization':False,'ReferenceRelationsAccepted':0,'CompatibilityPromoted':False}
    (args.output/'comparison.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({key:result[key] for key in ['Status','ExpectedRows','ActualRows','StrictEqual','OpaqueRowsNotAccepted']}))
    return 0 if result['Status']=='PASS_STRICT_PENDING_SOURCE_ADMISSION' and result['ActualRows']==result['ExpectedRows'] else 1

if __name__=='__main__':raise SystemExit(main())
