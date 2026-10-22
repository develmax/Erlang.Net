"""Passive exact whole-term comparison of new binding cohort; never executes fixture code."""
from pathlib import Path
import argparse,hashlib,json,importlib.util,sys
R=Path(__file__).resolve().parents[2];H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
s=importlib.util.spec_from_file_location('trusted_review',R/'artifacts/proplists-stack-relational-peer1/validate.py');v=importlib.util.module_from_spec(s);sys.modules[s.name]=v;s.loader.exec_module(v)
p=argparse.ArgumentParser()
for k in ['original','sdk','output']:p.add_argument('--'+k,type=Path,required=True)
p.add_argument('--before',type=Path,default=R/'artifacts/proplists-stack-binding-original1/sdk-before-guard/capture');a=p.parse_args();assert not a.output.exists()
profile=R/'artifacts/proplists-stack-binding-original1/tool/profile.json';fixture=json.loads(profile.read_bytes())['Fixtures'][0];names=[v.atom(n) for n in fixture['Names']];g=fixture['Id'];admission=v.admit_original(a.original,profile)
reports=[];seals={}
for label,sdk in [('before_guard',a.before),('after_guard',a.sdk)]:
 seal,files=v.admit_sdk(sdk);seals[label]=seal
 for filename in ['returned.etf','stack-observations.etf']:
  op=a.original/g/filename;sp=sdk/g/filename;left=v.decode(op);right=v.decode(sp);assert g+'/'+filename in files
  if filename=='returned.etf':
   ll=v.named(left);rr=v.named(right);assert list(ll)==list(rr)==names;pairs=[('values',name,ll[name],rr[name]) for name in names]
  else:
   ll=v.named(left);rr=v.named(right);depths=[v.atom('default'),('integer',0),('integer',1),('integer',2),('integer',8)];assert list(ll)==list(rr)==depths;pairs=[]
   for depth in depths:
    l=v.named(ll[depth]);r=v.named(rr[depth]);assert list(l)==list(r)==names;pairs.extend((repr(depth),n,l[n],r[n]) for n in names)
  for depth,name,l,r in pairs:reports.append(dict(Cohort=label,File=filename,Depth=depth,Name=repr(name),FullDecodedEqual=l==r,Original=repr(l),Sdk=repr(r),OriginalSha256=H(op),SdkSha256=H(sp)))
summary={}
for cohort in seals:
 summary[cohort]={}
 for filename in ['returned.etf','stack-observations.etf']:
  rows=[x for x in reports if x['Cohort']==cohort and x['File']==filename];summary[cohort][filename]=dict(Total=len(rows),WholeExact=sum(x['FullDecodedEqual'] for x in rows))
a.output.parent.mkdir(exist_ok=True,parents=True)
a.output.write_text(json.dumps(dict(Status='BOUNDED_EXACT_BINDING_COHORT_REVIEW',Summary=summary,Rows=reports,Seals=seals,OriginalAdmission=admission,Projection=False,ValueReplacement=False,CompatibilityPromoted=False),indent=2)+'\n');print(json.dumps(dict(Summary=summary,ReportSha256=H(a.output)),indent=2))
