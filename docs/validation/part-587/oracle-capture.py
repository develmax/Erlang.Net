from pathlib import Path
import json,hashlib,subprocess,os
r=Path.cwd();out=r/'artifacts/unicode-util/corpus';adpath=r/'../library376-official-compiler/official-admission.json';ad=json.loads(adpath.read_bytes());provider=Path(ad['InstalledOtpRoot']);manifest=Path(ad['ExecutionManifestPath']);h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert h(manifest)==ad['ExecutionManifestSha256'];inventory=json.loads(manifest.read_bytes());assert len(inventory)==6427
for item in inventory:assert h(provider/item['Path'])==item['Sha256'],item['Path']
expr='''Expected=os:getenv("UNICODE_UTIL_ORACLE_ROOT"),true=string:equal(filename:absname(Expected),filename:absname(code:root_dir()),true),{17,0}=unicode_util:spec_version(),{ok,unicode_util_port_cases}=compile:file("unicode_util_port_cases.erl",[report_errors]),{ok,unicode_util_table_capture}=compile:file("unicode_util_table_capture.erl",[report_errors]),true=code:add_patha("."),ok=unicode_util_port_cases:write("."),ok=unicode_util_port_cases:write_long("."),ok=unicode_util_table_capture:write("."),halt(0).'''
env={k:v for k,v in os.environ.items()if k not in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS']};env['ERLC_USE_SERVER']='false';env['UNICODE_UTIL_ORACLE_ROOT']=str(provider)
with (r/'artifacts/unicode-util/original-capture2.log').open('wb')as log:
 p=subprocess.run([str(provider/'bin/erl.exe'),'+S','2:2','-noshell','-noinput','-eval',expr],cwd=out,env=env,stdout=log,stderr=subprocess.STDOUT)
assert p.returncode==0,p.returncode
for item in inventory:assert h(provider/item['Path'])==item['Sha256'],item['Path']
names=['unicode_util_port_cases.erl','unicode_util_port_cases.beam','unicode_util_table_capture.erl','unicode_util_table_capture.beam','contracts.etf','long.etf','exports.etf','tables.etf','NormalizationTest.txt','GraphemeBreakTest.txt']
record=dict(Baseline='OTP-29.1.1',Commit=ad['SourceCommit'],SpecVersion=[17,0],ExecutionManifestSha256=h(manifest),EveryInstalledFileVerifiedBeforeAndAfter=True,ExecutionFiles=len(inventory),GeneratedSourceSha256=h(r/'artifacts/unicode-util/generate/src/unicode_util.erl'),Command=['bin/erl.exe','+S','2:2','-noshell','-noinput','-eval',expr],Files=[dict(Path=name,Sha256=h(out/name),Bytes=(out/name).stat().st_size)for name in names])
(out/'provenance.json').write_bytes((json.dumps(record,indent=2)+'\n').encode())
resource=r/'tests/Erlang.Tests/Resources/UnicodeUtilBackend';resource.mkdir(exist_ok=True)
for name in ['unicode_util_port_cases.erl','unicode_util_port_cases.beam','contracts.etf','exports.etf','provenance.json']:(resource/name).write_bytes((out/name).read_bytes())
print('Original capture and exhaustive table hashes complete; provider unchanged')
