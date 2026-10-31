"""Fail early on current MFA and native inventory raw bindings; full .NET Readiness stays authoritative."""
import argparse,hashlib,json,ntpath,os,re
from pathlib import Path
HEX=frozenset('0123456789abcdefABCDEF')
KINDS={'OriginalOtpComparison','CompatibilityAudit','SourceReview','LocalValidation'}
class InvalidBinding(ValueError): pass
def require(ok,why):
 if not ok:raise InvalidBinding(why)
def parse(data):
 return json.loads(data.decode('utf-8-sig'),parse_constant=lambda x:(_ for _ in ()).throw(InvalidBinding('Invalid JSON numeric token')))
def shape(value,required,optional=()):
 require(isinstance(value,dict),'Ledger object required')
 result={};allowed={key.lower():key for key in [*required,*optional]}
 for key,item in value.items():
  require(isinstance(key,str) and key.isascii() and key.lower() in allowed,'Unknown ledger member: '+str(key))
  result[allowed[key.lower()]]=item
 require(all(key in result for key in required),'Required ledger member absent')
 return result
def nonempty(value):return isinstance(value,str) and bool(value.strip())
def rooted(path):return bool(ntpath.splitdrive(path)[0]) or path.startswith(('/', '\\')) if os.name=='nt' else path.startswith('/')
def verify(repository,ledger_bytes,baseline_bytes,read_bytes):
 repository=os.path.abspath(str(repository));docs=os.path.join(repository,'docs')
 baseline=parse(baseline_bytes);ledger=shape(parse(ledger_bytes),['SchemaVersion','Tag','Commit','Entries','VerifiedSourceFiles'])
 require(type(ledger['SchemaVersion']) is int and ledger['SchemaVersion']==1 and ledger['Tag']==baseline['tag'] and ledger['Commit']==baseline['commit'],'Ledger baseline differs')
 require(isinstance(ledger['Entries'],list) and isinstance(ledger['VerifiedSourceFiles'],list),'Ledger arrays required')
 rows=[];source_paths=set();hashes={};failures=[]
 def evidence(base,item,label):
  e=shape(item,['Kind','Path','Sha256','Scope'])
  require(e['Kind'] in KINDS and nonempty(e['Scope']) and nonempty(e['Path']),'Invalid evidence kind/scope/path: '+label)
  path=e['Path'];require(not rooted(path) and all(part not in ('','.','..') for part in re.split(r'[/\\]',path)),'Rooted/escaping evidence path: '+label)
  digest=e['Sha256'];require(isinstance(digest,str) and len(digest)==64 and all(c in HEX for c in digest),'Invalid SHA256: '+label)
  # Exact current ledger uses ASCII relative paths. Reject unproven Unicode casing
  # instead of approximating .NET OrdinalIgnoreCase or weakening later Readiness.
  require(path.isascii(),'Non-ASCII path needs canonical .NET Readiness path validation: '+label)
  full=os.path.abspath(os.path.join(base,path));prefix=os.path.abspath(base).rstrip('/\\')+os.sep
  require(full.upper().startswith(prefix.upper()),'Evidence outside lexical root: '+label)
  key=full.upper()
  if key not in hashes:
   data=read_bytes(Path(full));hashes[key]=hashlib.sha256(data).hexdigest().upper()
  actual=hashes[key]
  rows.append(dict(Label=label,Path=path,Expected=digest.upper(),Actual=actual))
  if actual!=digest.upper():failures.append(rows[-1])
 for i,raw in enumerate(ledger['Entries']):
  entry=shape(raw,['Module','Function','Arity','Status','Scope','Evidence','Limits'],['Key'])
  require(isinstance(entry['Evidence'],list),'Evidence array required')
  for j,item in enumerate(entry['Evidence']):evidence(docs,item,'Entries['+str(i)+'].Evidence['+str(j)+']')
 for i,raw in enumerate(ledger['VerifiedSourceFiles']):
  source=shape(raw,['Path','Sha256']);path=source['Path'];require(isinstance(path,str) and path.startswith('src/') and path.upper() not in source_paths,'Invalid/duplicate verified source path')
  source_paths.add(path.upper());evidence(repository,dict(Kind='SourceReview',Path=path,Sha256=source['Sha256'],Scope='Audited current implementation source binding'),'VerifiedSourceFiles['+str(i)+']')
 return dict(Passed=not failures,Scope='EARLY_RAW_SOURCE_AND_EVIDENCE_BINDINGS_ONLY',VerifiedSourceFiles=len(ledger['VerifiedSourceFiles']),AllSourceEvidenceRows=len(rows),UniqueRawFiles=len(hashes),Failures=failures,RawBytesOnly=True,TextNormalizationPerformed=False,ClassificationOrRegistryValidationPerformed=False,FullReadinessReplaced=False,BuildSdkOrProductVmExecuted=False)
def verify_native(repository,inventory_bytes,baseline_bytes,read_bytes):
 repository=Path(repository).resolve();baseline=parse(baseline_bytes);inventory=parse(inventory_bytes)
 require(inventory['SchemaVersion']==1 and inventory['PinnedOtp']['Tag']==baseline['tag'] and inventory['PinnedOtp']['Commit']==baseline['commit'],'Native inventory baseline differs')
 require(isinstance(inventory['Evidence'],dict) and isinstance(inventory['Entries'],list),'Native binding arrays required')
 rows=[];hashes={};failures=[];sources={}
 def binding(item,label):
  require(isinstance(item,dict),'Native binding object required')
  path=item['Path'];digest=item['Sha256']
  require(nonempty(path) and path.isascii() and isinstance(digest,str) and len(digest)==64 and all(c in HEX for c in digest),'Invalid native path/hash: '+label)
  full=Path(os.path.abspath(os.path.join(repository,path)));relative=os.path.relpath(full,repository)
  # Matches NativeInfrastructureReport.ValidateHash lexical confinement, not historical SourceEquivalence.
  require(not relative.startswith('..') and not os.path.isabs(relative),'Native binding outside repository: '+label)
  key=str(full)
  if key not in hashes:hashes[key]=hashlib.sha256(read_bytes(full)).hexdigest().upper()
  row=dict(Label=label,Path=path,Expected=digest,Actual=hashes[key]);rows.append(row)
  if digest!=hashes[key]:failures.append(row)
 for name,item in inventory['Evidence'].items():binding(item,'Evidence.'+name)
 for i,entry in enumerate(inventory['Entries']):
  require(isinstance(entry['SourceBindings'],list),'Native SourceBindings array required')
  for j,item in enumerate(entry['SourceBindings']):
   path=item['Path'];digest=item['Sha256']
   require(path not in sources or sources[path]==digest,'Conflicting native source binding: '+path)
   sources[path]=digest;binding(item,'Entries['+str(i)+'].SourceBindings['+str(j)+']')
 return dict(Passed=not failures,AllSourceEvidenceRows=len(rows),UniqueRawFiles=len(hashes),CurrentBoundSourceFiles=len(sources),Failures=failures,HistoricalSourceEquivalenceValidatedAsCurrent=False)

def verify_all(repository,ledger_bytes,inventory_bytes,baseline_bytes,read_bytes):
 mfa=verify(repository,ledger_bytes,baseline_bytes,read_bytes)
 native=verify_native(repository,inventory_bytes,baseline_bytes,read_bytes)
 return dict(Passed=mfa['Passed'] and native['Passed'],Scope='EARLY_CURRENT_MFA_AND_NATIVE_RAW_BINDINGS_ONLY',Mfa=mfa,Native=native,AllSourceEvidenceRows=mfa['AllSourceEvidenceRows']+native['AllSourceEvidenceRows'],Failures=[*mfa['Failures'],*native['Failures']],RawBytesOnly=True,TextNormalizationPerformed=False,ClassificationOrRegistryValidationPerformed=False,FullReadinessReplaced=False,BuildSdkOrProductVmExecuted=False)

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--repository',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();repo=a.repository.resolve();out=a.output.resolve()
 require(out.is_relative_to(repo/'artifacts') and not out.exists(),'Fresh confined artifact output required')
 try:report=verify_all(repo,(repo/'docs/mfa-compatibility.json').read_bytes(),(repo/'docs/native-infrastructure.json').read_bytes(),(repo/'reference-baseline.json').read_bytes(),lambda path:path.read_bytes())
 except Exception as error:report=dict(Passed=False,Scope='EARLY_RAW_SOURCE_AND_EVIDENCE_BINDINGS_ONLY',Error=repr(error),FullReadinessReplaced=False,BuildSdkOrProductVmExecuted=False)
 out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
 print(json.dumps({key:value for key,value in report.items() if key not in ('Failures','Mfa','Native')}));raise SystemExit(0 if report['Passed'] else 1)
if __name__=='__main__':main()
