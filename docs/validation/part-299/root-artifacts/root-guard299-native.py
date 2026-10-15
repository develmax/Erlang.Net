from pathlib import Path
import json,hashlib,re
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper()
fpath=a/'root-part299-final-source-freeze.json';f=j(fpath)
assert j(a/'root-part299-full-exit.json')['ExitCode']==0 and j(a/'root-part299-main-native-exit.json')['ExitCode']==0
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
lines=(a/'root-part299-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert len(lines)==4 and lines[:2]==['Managed backend checks passed; native ABI and original OTP NOT executed.','Internal trusted native fixture checks passed; general NIF ABI NOT implemented.']
prefix='Native generated source SHA256: ';assert lines[2].startswith(prefix);generated=lines[2][len(prefix):];assert re.fullmatch('[A-F0-9]{64}',generated)
assert lines[3]=='Trusted native generated-source entry routes passed; BEAM/old-image/general ABI NOT established.'
assert lines==(a/'root-nif-native299-isolated/windows-native-compiled-nontail.log').read_text(encoding='utf-8-sig').splitlines()
runtime=r/'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.Runtime.dll'
assert h(runtime.read_bytes())==h((r/'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes())
fixture=r/'artifacts/spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
out={'Status':'PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE','ExitCode':0,'SourcePaths':55,'CSharpPaths':31,'SourceHashesExact':True,'SourceFreezeSha256':h(fpath.read_bytes()),'ReviewedSourcePlanSha256':f['ReviewedSourcePlanSha256'],'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedCSharpRoutePassed':True,'ManagedToolDllSha256':h((r/'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll').read_bytes()),'RuntimeDllSha256':h(runtime.read_bytes()),'FixtureSha256':h(fixture.read_bytes()),'FixtureSourceSha256':h((r/'tools/Erlang.NifFixtureHost/native/fixture.c').read_bytes()),'GeneratedCSharpSha256':generated,'PreformatIsolatedGeneratedSourceAndOutputExactlyMatch':True,'GeneralNifAbiComplete':False,'PublicLoadNifRegistered':False}
(a/'root-part299-main-native-guard.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out))
