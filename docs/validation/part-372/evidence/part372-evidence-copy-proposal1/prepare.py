"""Artifact-only durable evidence plan. Does not copy or modify Main/docs."""
from pathlib import Path
import argparse,collections,datetime,hashlib,json,os,subprocess
O=Path(__file__).resolve().parent;R=O.parents[1]
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def save(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
def git(*args):return subprocess.check_output(['git','-C',str(R),*args],timeout=60)
def main():
 p=argparse.ArgumentParser();p.add_argument('--add-prefix',action='append',default=[]);a=p.parse_args()
 rules=J(O/'rules.json');destination=R/rules['Destination']
 producer=rules['Producer'];span=producer['BaseCommit']+'..'+producer['Head']
 changed=git('diff','--name-only',span).decode().splitlines()
 assert changed and all(any(name==prefix or name.startswith(prefix) for prefix in producer['AllowedChangedPrefixes']) for name in changed),changed
 patch=git('diff','--binary','--full-index',producer['BaseCommit'],producer['Head'],'--',*changed)
 (O/'producer-packet6.patch').write_bytes(patch)
 commit=git('cat-file','commit',producer['Head']);(O/'producer-commit.raw').write_bytes(commit)
 assert hashlib.sha1(b'commit '+str(len(commit)).encode()+b'\0'+commit).hexdigest()==producer['Head']
 parents=[line.split(b' ',1)[1].decode() for line in commit.split(b'\n\n',1)[0].splitlines() if line.startswith(b'parent ')]
 assert parents==[producer['BaseCommit']],'Retain additional producer parent Git objects before claiming standalone restoration'
 tree=next(line.split(b' ',1)[1].decode() for line in commit.splitlines() if line.startswith(b'tree '))
 save(O/'producer-source.json',dict(producer,ChangedPaths=changed,PatchSha256=H(O/'producer-packet6.patch'),PatchBytes=len(patch),CommitRawSha256=H(O/'producer-commit.raw'),ExactGitCommitObject=producer['Head'],ExactGitTree=tree,Parents=parents,NativeParentConclusion='failure',IndependentOrdsetsAndUsortAdmitted=True,NumericOutcome='Original VM aborted (-6); no normal numeric credit',GitReadOnly=True))
 prefixes=list(rules['ArtifactPrefixes'])+a.add_prefix
 for prefix in prefixes:
  resolved=(R/prefix).resolve();assert resolved.is_relative_to(R/'artifacts') and resolved!=R/'artifacts'
  assert not any(prefix==bad or prefix.startswith(bad+'/') for bad in rules['ForbiddenEntirePrefixes'])
 selected={};excluded=[];pruned=[];missing=[];unstable=[]
 forbiddenparts=set(rules['ExcludedDirectoryNames']);forbiddenext=set(rules['ExcludedExtensions']);licenses=set(rules['PrivateLicenseFileNames'])
 def file_allowed(q):
  name=q.name.lower()
  if name in licenses or 'privatelicense' in name or 'private-license' in name:return 'Private or single license file is intentionally unpublished'
  if q.suffix.lower() in forbiddenext:return 'Compiled binary/cache excluded'
  if any(name.endswith(suffix) for suffix in rules['PinnedAuthorityFileSuffixes']):return 'Pinned original authority is referenced by commit/hash, not recopied'
  return None
 def add(q):
  relative=q.relative_to(R).as_posix()
  if any(relative==bad or relative.startswith(bad+'/') for bad in rules['ForbiddenEntirePrefixes']):return
  reason=file_allowed(q)
  if reason:excluded.append({'Path':relative,'Reason':reason});return
  resolved=q.resolve();assert resolved.is_relative_to(R/'artifacts')
  before=q.stat();raw=q.read_bytes();after=q.stat()
  if (before.st_size,before.st_mtime_ns)!=(after.st_size,after.st_mtime_ns):unstable.append(relative);return
  digest=hashlib.sha256(raw).hexdigest().upper();dest=destination/Path(relative).relative_to('artifacts')
  assert dest.resolve().is_relative_to(destination.resolve())
  base=H(dest) if dest.exists() else None
  assert base is None or base==digest,'Existing durable evidence conflict: '+str(dest)
  selected[relative]={'Source':relative,'SourceAbsolutePath':str(q.resolve()),'Destination':dest.relative_to(R).as_posix(),'DestinationAbsolutePath':str(dest.resolve()),'Bytes':len(raw),'Sha256':digest,'ExistingDestinationSha256':base}
 for prefix in prefixes:
  base=R/prefix
  if not base.exists():missing.append(prefix);continue
  for directory,dirs,files in os.walk(base):
   current=Path(directory);keep=[]
   for name in dirs:
    relative=(current/name).relative_to(R).as_posix()
    if name.lower() in forbiddenparts or any(relative==bad or relative.startswith(bad+'/') for bad in rules['ForbiddenEntirePrefixes']):pruned.append(relative)
    else:keep.append(name)
   dirs[:]=keep
   for name in files:add(current/name)
 for name in ['rules.json','prepare.py','README.md','producer-packet6.patch','producer-commit.raw','producer-source.json']:
  q=O/name
  if q.exists():add(q)
 protected=[]
 for prefix in rules['ExistingCommittedEvidenceNotDuplicated']:
  base=R/prefix
  if not base.exists():continue
  for q in sorted(base.rglob('*')):
   if q.is_file():protected.append({'Path':q.relative_to(R).as_posix(),'Bytes':q.stat().st_size,'Sha256':H(q)})
 save(O/'protected-existing-evidence.json',{'MustRemainByteExact':True,'Files':protected})
 files=[selected[k] for k in sorted(selected)];summary=collections.defaultdict(lambda:{'Files':0,'Bytes':0})
 for row in files:
  prefix=row['Source'].split('/')[1];summary[prefix]['Files']+=1;summary[prefix]['Bytes']+=row['Bytes']
 for row in files:assert row['Source'].startswith('artifacts/') and not row['Destination'].startswith('docs/validation/part-371/')
 save(O/'copy-plan.json',{'Status':'CONTROLLED_EVIDENCE_COPY_PLAN_ONLY_ROOT_EXECUTES','GeneratedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'Destination':str(destination),'Files':files,'FileCount':len(files),'Bytes':sum(x['Bytes'] for x in files),'ScopeSummary':dict(summary),'SelectedPrefixes':prefixes,'MissingPrefixes':missing,'UnstableFilesNotAdmitted':unstable,'ExcludedFiles':excluded,'PrunedDirectories':pruned,'ProtectedExistingEvidence':'protected-existing-evidence.json','ProtectedExistingEvidenceSha256':H(O/'protected-existing-evidence.json'),'RootFillsLater':rules['RootFillsLater'],'ReadyForFinalCopy':False,'VerifyEverySourceHashImmediatelyBeforeCopy':True,'NoPrivateLicenseDllBinObjCachesOrOriginalSourceTree':True,'NoMainOrDocsWritesByPreparation':True,'PreserveFailureAndHistoricalLabels':True,'RestoreOriginalArtifactPathsForReplay':True,'PublishThisPlanSeparatelyAt':'docs/validation/part-372/evidence-copy-plan.json'})
 save(O/'summary.json',{'Files':len(files),'Bytes':sum(x['Bytes'] for x in files),'ProtectedFiles':len(protected),'ProtectedBytes':sum(x['Bytes'] for x in protected),'ExcludedFiles':len(excluded),'PrunedDirectories':len(pruned),'Unstable':unstable,'MissingPrefixes':missing,'ProducerPatchSha256':H(O/'producer-packet6.patch'),'CopyPlanSha256':H(O/'copy-plan.json'),'NoCopyPerformed':True,'RootMustRegenerateAfterFinalOutputs':True})
 print(json.dumps(J(O/'summary.json')))
if __name__=='__main__':main()
