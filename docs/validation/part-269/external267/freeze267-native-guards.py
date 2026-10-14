from pathlib import Path
from functools import reduce

a=Path(__file__).parent
pairs={b'part-266':b'part-267',b'part266':b'part267',b'5b3bd2e0b7f12bfcf4fe717e72579df454f8aa4d':b'85a01e41d4f9fee085bee7a3fa656ea168645dc6',b'38564019036':b'38568438645',b'115852013139':b'115867008746',b'6483':b'6484'}
old=b"ci=j(a/'part267-ci-final-guard.json');assert ci['Status']=='PASS' and ci['CiPassed']==6484 and not ci['RawJobLogAuditPending']"
new=b"ci=j(a/'part267-ci-failure-guard.json');assert ci['Status']=='ExactFailed267CiProofPreserved' and ci['CiPassed']==6483 and ci['CiFailed']==1 and ci['Head']==m['Run']['head_sha']"
for name in ['original-strict-guard','original-observation-guard']:
    data=reduce(lambda s,p:s.replace(*p),pairs.items(),(a/('part266-'+name+'.py')).read_bytes())
    assert data.count(old)==1
    data=data.replace(old,new).replace(b"'CiPassed':6484",b"'CiPassed':6483,'CiFailed':1,'CiStatus':'FirstRunFailedPortableHashAllocationBudget','CombinedValidationStatus':'NativeVerifiedCiFailed'")
    data=data.replace(b'same-headCI6484',b'separateCI6483of6484FAILED').replace(b'+CI6484',b'; separateCI6483of6484FAILED')
    (a/('part267-'+name+'.py')).write_bytes(data)
print('Prepared native267 guards with independently retained failedCI; original outcomes unobserved')
