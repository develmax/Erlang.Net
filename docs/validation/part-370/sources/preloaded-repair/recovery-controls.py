"""Passive recovery controls; never rewrites failed producer receipts or raw ETF."""
from pathlib import Path
import importlib.util,json,hashlib,ast
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent;TOOL=OUT/'tool';RAW=ROOT/'artifacts/erlang370-original-failure1/raw/capture'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def load(p,n):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
m=load(TOOL/'preloaded_provenance.py','repaired_metadata');d=load(TOOL/'passive_etf_family.py','trusted_decoder')
p=json.loads((TOOL/'profile.json').read_bytes());inputs=json.loads((RAW/'inputs.json').read_bytes());authority=OUT/'authority/erlang.beam'
predicted=m.preloaded_module_md5(authority.read_bytes()).hex().upper();assert predicted=='65B2F7089C6EFAF3E3A118E65970A18D' and predicted!=inputs['InstalledCompanionModuleMd5']
new=dict(inputs,PredictedPreloadModuleMd5=predicted);groups=[];count=profiles=0
for f in p['Fixtures']:
 q=RAW/f['Id'];node=d.decode_exact((q/'metadata.etf').read_bytes());fields=m.fields(node);c=json.loads((q/'compile.stage.json').read_bytes())['Arguments'];ebin=Path(c[c.index('-o')+1]).resolve();pid=json.loads((q/'observe.process.json').read_bytes())['OwnedPid']
 proof=m.validate(node,new,Path(m.text(fields['root'])).resolve(),f,ebin,pid,p['Mfas'])
 values=d.decode_exact((q/'returned.etf').read_bytes());assert values[0]=='list' and len(values[1])==len(f['Names'])
 for name,row in zip(f['Names'],values[1]):
  assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name);m.validate_outcome(row[1][1],False);count+=1
 stacks=d.decode_exact((q/'stack-observations.etf').read_bytes());assert stacks[0]=='list' and len(stacks[1])==5
 for depth,row in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],stacks[1]):
  assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==depth
  nested=row[1][1];assert nested[0]=='list' and len(nested[1])==len(f['Names'])
  for name,pair in zip(f['Names'],nested[1]):
   assert pair[0]=='tuple' and len(pair[1])==2 and pair[1][0]==('atom',name);m.validate_outcome(pair[1][1],True);profiles+=1
 groups.append(dict(Id=f['Id'],MetadataSha256=H(q/'metadata.etf'),ReturnedSha256=H(q/'returned.etf'),StacksSha256=H(q/'stack-observations.etf'),Admission=proof))
 # Corrupting predicted identity must continue to reject actual metadata.
 bad=dict(new,PredictedPreloadModuleMd5='00'*16)
 try:m.validate(node,bad,Path(m.text(fields['root'])).resolve(),f,ebin,pid,p['Mfas'])
 except AssertionError:pass
 else:raise AssertionError('corrupt predicted identity admitted')
assert count==1597 and profiles==7985
report=dict(Status='PASS_PASSIVE_CORRECTED_METADATA_AND_RAW_SHAPES_ONLY',NativeExecuted=False,OriginalProducerStatusPreserved='FAIL_ORIGINAL_CAPTURE_GROUPS_RETAINED',PredictedPinnedPreloadMd5=predicted,ActualInstalledCompanionMd5=inputs['InstalledCompanionModuleMd5'],MetadataMatchesPinnedStrippedPreload=True,InstalledCompanionIsNotLoadedImage=True,Values=count,Profiles=profiles,Groups=groups,Controls=dict(ActualMetadataAccepted=8,CorruptPredictedMd5Rejected=8),CompatibilityAccepted=False,CompleteSourceAndProcessAdmissionStillRequired=True)
(OUT/'passive-recovery-controls.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'Groups':len(groups),'Values':count,'Profiles':profiles}))
