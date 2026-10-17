"""Build exact source-defined native images; no NIF initialization or VM observation."""
from pathlib import Path
import shutil
from common import *

def main():
 args=arguments('build');root,tool,plan,identity,owned,_=verify(args.repository,args.expected_head,args.manifest_sha256)
 runtime,build=approve_runtime(args,plan,identity);out=Path(args.output).resolve();assert not out.exists();out.mkdir(parents=True)
 phase='inputs';stages=[]
 try:
  write(out/'inputs.json',{'SubjectHead':args.expected_head,'PinnedOtpCommit':PIN,'SourceManifestSha256':digest(tool/'manifest.json'),'RuntimeBuildReceiptSha256':digest(args.build_receipt),'OriginalObservationExecuted':False})
  headers=out/'headers';headers.mkdir();header_rows=[]
  for row in plan['Headers']:
   src=runtime/'usr/include'/row['Name'];assert digest(src)==row['Sha256'];shutil.copyfile(src,headers/row['Name'])
   header_rows.append({'Name':row['Name'],'Path':str(headers/row['Name']),'Bytes':src.stat().st_size,'Sha256':digest(src)})
  sizes=runtime/'usr/include/erl_int_sizes_config.h';assert sizes.is_file();shutil.copyfile(sizes,headers/sizes.name)
  header_rows.append({'Name':sizes.name,'Path':str(headers/sizes.name),'Bytes':sizes.stat().st_size,'Sha256':digest(sizes)})
  gcc,nm,readelf=[shutil.which(x) for x in ['gcc','nm','readelf']];assert gcc and nm and readelf
  env=identity.sanitized_environment(os.environ);phase='compiler-version';stages.append(stage(owned,identity,[gcc,'--version'],out,env,out/phase,30))
  images={}
  for case in read(tool/'cases.json')['Cases']:
   key=case['NativeLeaf'];record=(case['NativeSource'],case['CompilerDefinitions'])
   if key in images:assert images[key]==record
   images[key]=record
  result=[]
  for leaf,(source,definitions) in images.items():
   assert leaf.replace('_','').isalnum();directory=out/leaf;directory.mkdir();src=tool/source.replace('source/','',1)
   assert src.resolve().is_relative_to(tool);shutil.copyfile(src,directory/src.name);shutil.copyfile(tool/'native/entry_fixture_names.h',directory/'entry_fixture_names.h')
   library=directory/(leaf+'.so');argv=[gcc,'-shared','-fPIC','-O2','-std=c11','-Wall','-Wextra','-Werror','-I',str(headers)]+['-D'+x for x in definitions]+[src.name,'-o',library.name]
   phase='compile-'+leaf;receipt=stage(owned,identity,argv,directory,env,directory/'compile',90);stages.append(receipt)
   phase='symbols-'+leaf;stages.append(stage(owned,identity,[nm,'-D','--defined-only','--format=posix',library],directory,env,directory/'defined',30))
   stages.append(stage(owned,identity,[nm,'-D','--undefined-only','--format=posix',library],directory,env,directory/'undefined',30))
   stages.append(stage(owned,identity,[readelf,'-h','-Ws','-d',library],directory,env,directory/'readelf',30))
   defined={line.split()[0].split('@')[0] for line in (directory/'defined.stdout').read_text().splitlines() if line.split()};assert defined=={'nif_init'}
   undefined={line.split()[0].split('@')[0] for line in (directory/'undefined.stdout').read_text().splitlines() if line.split()};assert undefined<=set(plan['PermittedUndefinedSymbols'])
   raw=library.read_bytes();assert raw[:6]==b'\x7fELF\x02\x01' and int.from_bytes(raw[18:20],'little')==62
   result.append({'Leaf':leaf,'SourcePath':source,'SourceSha256':digest(src),'CompilerDefinitions':definitions,'Path':str(library),'Bytes':library.stat().st_size,'Sha256':digest(library),'DefinedSymbols':sorted(defined),'UndefinedSymbols':sorted(undefined),'BuildProcessReceiptSha256':receipt['ReceiptSha256']})
  assert len(result)==plan['NativeImageCount'];approve_runtime(args,plan,identity)
  write(out/'native-build-receipt.json',{'Status':'PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD','SubjectHead':args.expected_head,'PinnedOtpCommit':PIN,'SourceManifestSha256':digest(tool/'manifest.json'),'RuntimeBuildReceiptSha256':digest(args.build_receipt),'HeaderInputs':header_rows,'Compiler':{'Path':gcc,'VersionStdoutSha256':digest(out/'compiler-version.stdout')},'Images':result,'OwnedStages':stages,'NifInitializationExecuted':False,'OriginalObservationsExecuted':False})
 except BaseException as error:
  write(out/'failure.json',{'Status':'FAILED_NATIVE_BUILD_RAW_RETAINED','Phase':phase,'Error':repr(error),'OwnedStages':stages,'OriginalObservationsExecuted':False});raise
if __name__=='__main__':main()
