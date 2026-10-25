"""Manual Linux own-build original/SDK observation. Source preparation only until reviewed/executed."""
from pathlib import Path
import argparse,hashlib,json,os,signal,subprocess,shutil,re
ap=argparse.ArgumentParser()
for n in ('otp-root','otp-source','sdk-driver','output','expected-head','source-manifest-sha256','build-receipt','approved-build-receipt-sha256'):ap.add_argument('--'+n,required=True)
a=ap.parse_args();root=Path(__file__).resolve().parents[3];otp=Path(a.otp_root).resolve();driver=Path(a.sdk_driver).resolve();out=Path(a.output).resolve()
assert os.name=='posix' and not out.exists() and driver.is_file()
a.runtime_root=str(otp/'lib/erlang')
from inputs import verify
identity,common,owned,runtime,runtime_build=verify(a,root,Path(__file__).resolve().parent)
out.mkdir();H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper();B=lambda v:(json.dumps(v,indent=2)+'\n').encode()
environment=identity.sanitized_environment(os.environ);environment.update(DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_TELEMETRY_OPTOUT='1',ERLC_USE_SERVER='false')
def execute(name,args,cwd=None,timeout=120,extra=None):
 env=dict(environment);env.update(extra or {})
 common.stage(owned,identity,args,cwd or out,env,out/name,timeout)
 return (out/(name+'.stdout')).read_bytes()
native=root/'tools/Native/Erlang.NifUnixFilenameObservations/native/echo.c';source=root/'tools/Native/Erlang.NifUnixFilenameObservations/raw_unix_filename_contract.erl';bridge=root/'tools/Native/Erlang.NifNativeBackend/native';include=runtime/'usr/include'
headers=[]
for name in ('erl_nif.h','erl_nif_api_funcs.h','erl_drv_nif.h'):
 p=include/name;q=Path(a.otp_source)/'erts/emulator/beam'/name;assert H(p)==H(q);headers.append(dict(Path=str(p),Sha256=H(p),PinnedSourcePath=str(q)))
generated=include/'erl_int_sizes_config.h';assert any(H(p)==H(generated) for p in Path(a.otp_source).rglob(generated.name) if p.is_file());headers.append(dict(Path=str(generated),Sha256=H(generated),Origin='Actual configured source header'))
compiler=Path(shutil.which('gcc')).resolve();dotnet=Path(shutil.which('dotnet')).resolve()
(out/'provenance.json').write_bytes(B(dict(PinnedCommit=runtime_build['Commit'],SubjectHead=a.expected_head,RuntimeBuildReceiptSha256=H(a.build_receipt),SourceManifestSha256=H(Path(__file__).parent/'manifest.json'),InstalledRuntimeFiles=runtime_build['InstalledFiles'],Headers=headers,CompilerPath=str(compiler),CompilerSha256=H(compiler),DotnetPath=str(dotnet),DotnetSha256=H(dotnet),SdkAssemblies=[dict(Path=str(p),Sha256=H(p)) for p in sorted(driver.parent.glob('*.dll'))])))
execute('compiler-version',[str(compiler),'--version'])
execute('otp-version',[str(otp/'bin/erl'),'+S','2:2','-noshell','-eval','io:format("~s",[erlang:system_info(otp_release)]),halt().'])
assert (otp/'lib/erlang/releases/29/OTP_VERSION').read_text().strip()=='29.1.1'
flags=['-shared','-fPIC','-O2','-std=c11','-Wall','-Wextra','-Werror','-I',str(include)]
execute('build-image',[str(compiler),*flags,str(native),'-o',str(out/'echo.so')])
execute('build-bridge',[str(compiler),*flags,str(bridge/'nif_bridge_exports.c'),str(bridge/'nif_bridge_varargs.c'),'-o',str(out/'bridge.so')])
receipt=out/'build.json';receipt.write_bytes(B(dict(Status='OWN_BUILD_NOT_INDEPENDENT_SOURCE_REVIEW',NativeSourceSha256=H(native),ImageSha256=H(out/'echo.so'),BridgeSha256=H(out/'bridge.so'),OtpVersion='29.1.1')))
execute('compile-original',[str(otp/'bin/erlc'),'-o',str(out),str(source)])
rawroot=os.fsencode(out);rawdir=rawroot+b'/cwd-\xfe';os.mkdir(rawdir);rawfile=rawroot+b'/raw-\xff.so';shutil.copyfile(os.fsencode(out/'echo.so'),rawfile);shutil.copyfile(rawfile,rawdir+b'/raw-\xff.so')
os.mkdir(rawroot+b'/target');os.mkdir(rawroot+b'/target/inner');os.symlink(b'target/inner',rawroot+b'/symlink');shutil.copyfile(rawfile,rawroot+b'/target/raw-\xff.so')
cases=[('absolute',rawroot+b'/raw-\xff',rawroot,rawfile),('first-nul',rawroot+b'/raw-\xff\0ignored',rawroot,rawfile),('raw-cwd',b'./raw-\xff',rawdir,rawdir+b'/./raw-\xff.so'),('symlink-dotdot',b'symlink/../raw-\xff',rawroot,rawroot+b'/symlink/../raw-\xff.so')]
search=out/'loader-search';search.mkdir();shutil.copyfile(rawfile,os.fsencode(search)+b'/search-\xff.so')
results=[]
for name,stem,cwd,image in cases:
 expr='V=raw_unix_filename_contract:run(binary:decode_hex(<<"'+stem.hex().upper()+'">>)),io:format("~s~n",[binary:encode_hex(term_to_binary(V))]),halt().'
 stdout=execute(name+'-original',[str(otp/'bin/erl'),'+S','2:2','-noshell','-pa',str(out),'-eval',expr],cwd)
 assert re.fullmatch(b'[0-9A-F]+\n',stdout);expected=bytes.fromhex(stdout.decode().strip());(out/(name+'-original.etf')).write_bytes(expected)
 for route in ('ordinary','reviewed'):
  # Exact lexical raw absolute path; no normalization of symlink/.. is permitted.
  plan=dict(Status='OWN_BUILT_RAW_UNIX_FILENAME_FIXTURE',SourcePath=str(source),SourceSha256=H(source),BuildReceiptPath=str(receipt),BuildReceiptSha256=H(receipt),NativeSourcePath=str(native),NativeSourceSha256=H(native),BridgePath=str(out/'bridge.so'),BridgeSha256=H(out/'bridge.so'),ImageRawPathHex=image.hex().upper(),ImageSha256=H(out/'echo.so'),RequestedStemHex=stem.hex().upper(),Route=route)
  admission=out/(name+'-'+route+'-admission.json');admission.write_bytes(B(plan));target=out/(name+'-'+route)
  execute(name+'-'+route,[str(dotnet),str(driver),'--unix-raw-path',str(admission),H(admission),str(target)],cwd)
  assert (target/'returned.etf').read_bytes()==expected
  results.append(dict(Name=name,Route=route,Passed=True,OriginalEtfSha256=H(out/(name+'-original.etf')),SdkEtfSha256=H(target/'returned.etf')))
# Different invalid raw bytes share DisplayPath but must not share reviewed permission.
plan=dict(plan,Route='collision',ImageRawPathHex=rawfile.hex().upper(),RequestedStemHex=(rawroot+b'/raw-\xfe').hex().upper())
admission=out/'collision-admission.json';admission.write_bytes(B(plan));execute('collision',[str(dotnet),str(driver),'--unix-raw-path',str(admission),H(admission),str(out/'collision')],rawroot)
assert json.loads((out/'collision/completion.json').read_bytes())['Status']=='RAW_UNIX_FILENAME_POLICY_COLLISION_REFUSED'
basename=b'search-\xff';extra={'LD_LIBRARY_PATH':str(search)}
expr='V=raw_unix_filename_contract:run(binary:decode_hex(<<"'+basename.hex().upper()+'">>)),io:format("~s~n",[binary:encode_hex(term_to_binary(V))]),halt().'
stdout=execute('basename-original',[str(otp/'bin/erl'),'+S','2:2','-noshell','-pa',str(out),'-eval',expr],rawroot,extra=extra);assert re.fullmatch(b'[0-9A-F]+\n',stdout);expected=bytes.fromhex(stdout.decode().strip());(out/'basename-original.etf').write_bytes(expected)
plan=dict(plan,Route='ordinary',ImageRawPathHex=(os.fsencode(search)+b'/search-\xff.so').hex().upper(),RequestedStemHex=basename.hex().upper());admission=out/'basename-admission.json';admission.write_bytes(B(plan));execute('basename-ordinary',[str(dotnet),str(driver),'--unix-raw-path',str(admission),H(admission),str(out/'basename-ordinary')],rawroot,extra=extra);assert (out/'basename-ordinary/returned.etf').read_bytes()==expected
results.append(dict(Name='bare-loader-search',Route='ordinary',Passed=True,OriginalEtfSha256=H(out/'basename-original.etf'),SdkEtfSha256=H(out/'basename-ordinary/returned.etf')))
(out/'comparison.json').write_bytes(B(dict(Status='SELECTED_RAW_UNIX_ORIGINAL_SDK_EQUAL',Passed=len(results),Failed=0,Results=results,SeparatePolicyCollisionRefusal=True,Scope='Four path scenarios in ordinary/reviewed routes plus basename loader search ordinary-only; reviewed basename policy intentionally refuses an unverified search target; one locally owned echo image; no broader native ABI or full corpus acceptance.')))
