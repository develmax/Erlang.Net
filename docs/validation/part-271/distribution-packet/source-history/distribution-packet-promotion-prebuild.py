import pathlib,json,hashlib,shutil
r=pathlib.Path.cwd();p=r/'artifacts/distribution-packet-promotion-prototype';e=r/'artifacts/distribution-packet-promotion-evidence';e.mkdir(exist_ok=True);tool=p/'tools/Erlang.DistributionPacketContrasts'
owned=[f['Path'] for f in json.loads((r/'artifacts/distribution-packet-source-freeze-revision3.json').read_text())['Files']]
owned += ['tests/Erlang.Tests/DistributionPacketChecks.cs','tests/Erlang.Tests/DistributionPacketTestNames.cs','tests/Erlang.Tests/DistributionPacketTestParameters.cs','tests/Erlang.Tests/Program.cs']
owned += [x.relative_to(p).as_posix() for x in sorted(tool.glob('*')) if x.is_file()]
owned += ['.github/workflows/distribution-packet-contrasts.yml']
files=[]
for name in owned:
 f=p/name;raw=f.read_bytes();original=r/name;files.append({'Path':name,'BeforeSha256':hashlib.sha256(original.read_bytes()).hexdigest().upper() if original.exists() else None,'Sha256':hashlib.sha256(raw).hexdigest().upper(),'BeforeSource':original.read_text() if original.exists() else None,'SourceContent':raw.decode('utf8')})
initial={'Stage':'PreBuild','Candidate':str(p),'NativeExecuted':False,'ProductionCodecSourceFreezeSha256':'74C270F384C84D20DE1720D8182179453C3E5413A484ABCD95951A42FC1B3FCB','NativeProbeSha256':hashlib.sha256((tool/'distribution_packet_probe.erl').read_bytes()).hexdigest().upper(),'Files':files}
f=r/'artifacts/distribution-packet-promotion-source-freeze-initial.json';f.write_text(json.dumps(initial,indent=2));print('Owned',len(owned),'prebuildSHA',hashlib.sha256(f.read_bytes()).hexdigest().upper())
scope=e/'owned-style-root'
for directory in ['src','tools','tests','benchmarks','examples']:(scope/directory).mkdir(parents=True,exist_ok=True)
for name in owned:
 if not name.endswith('.cs'):continue
 dest=scope/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p/name,dest)
