from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd();d=r/'artifacts/connected-remote-rpc-permanent-proposal'
h=lambda b:hashlib.sha256(b).hexdigest().upper()
freeze=d/'source-freeze.json'
assert h(freeze.read_bytes())=='7ACEC24BB2CC57005F78CD79A44B53A24ED6081B7547756F33735E84B5419C92'
for row in json.loads(freeze.read_bytes())['Files']:
 p=d/row['Path'];b=p.read_bytes();assert len(b)==row['Bytes'] and h(b)==row['Sha256']
source=d/'source/tools/Erlang.Differential'
constants=lambda p:{k:json.loads(v) for k,v in re.findall(r'const string (\w+) = ("(?:[^"\\]|\\.)*");',p.read_text())}
names=constants(source/'ConnectedDependencyCaseNames.cs');sources=constants(source/'ConnectedDependencyCaseSources.cs')
cases=json.loads((d/'cases.json').read_bytes());assert len(cases)==len(sources)==44
mapping={names[k]:v for k,v in sources.items()}
for row in cases:
 assert row['Expected']=='ok' and row['ExpectationOrigin'] in ['Pinned OTP source contract; not observed SDK output','Pinned rpc RPCIFY/erpc call source, not SDK output','Reviewed shared apply339 source proposal, no SDK output','Pinned erpc monitor result/timeout call flow, not SDK output','Reviewed runtime-nodes339 source prerequisite, no SDK output']
 assert (d/row['Path']).read_text()==mapping[row['Name']]
 assert '?MODULE' not in mapping[row['Name']]
catalog=(source/'ConnectedDependencyCases.cs').read_text()
pairs=re.findall(r'new\(ConnectedDependencyCaseNames\.(\w+), ConnectedDependencyCaseSources\.(\w+),',catalog)
assert len(pairs)==44 and all(a==b for a,b in pairs)
assert [names[a] for a,b in pairs]==[row['Name'] for row in cases]
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
native=subprocess.check_output(['git','-C','../otp','show',pin+':erts/emulator/beam/dist.c'])
assert b'case am_known:     visible = hidden = not_connected = this = 1;' in native
assert b'if (no_opts != erts_map_size(options))' in native
assert b'cid = am_undefined;' in native
result=dict(Status='PASS_ROOT44_CATALOG_SOURCE_REVIEW_ONLY',PinnedOtp=pin,SourceFreezeSha256=h(freeze.read_bytes()),Fixtures=44,GeneratedSourcesLogicallyEqual=True,FixtureExpectedDerivedFromPinnedContracts=True,MainEdited=False,SdkExecuted=False,OriginalExecuted=False,Reviewed=['Eight nodes selector and info cases against dist.c6262-6475','Plain disconnected spawn separate-monitor noconnection/noproc race; atomic linked variants require noconnection','Zero RPC timeout worker is released and joined with real correlated return DOWN','No MODULE preprocessing assumption','Eleven direct contracts dispatch to real compiled fixtures'])
out=r/'artifacts/root-340-catalog-source-review.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(**result,GuardSha256=h(out.read_bytes()))))
