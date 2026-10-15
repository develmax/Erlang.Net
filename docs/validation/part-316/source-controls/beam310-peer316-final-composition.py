from pathlib import Path
import json,hashlib,re,ast
R=Path.cwd();A=R/'artifacts';D=A/'allocator-public-sys-profile316-composed2';C=D/'candidate';B=A/'native-payload-provider315-composed2/candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();load=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
f=D/'formatted-source-freeze.json';assert sha(f.read_bytes())=='2798591EE8BC8FE77E719B96C2D9535672255C5E772E0FFC30E6E294BFA4372A';v=load(f);cl=D/'formatted-source-resource-closure.json';assert sha(cl.read_bytes())==v['SourceClosureSha256']=='87CC2B5E1C8DDEF229E95DA38875431E73BA715D6EE71275F7DAEA5755E00299'
rows=load(cl)['Files'];owned={r['Path']:r for r in v['Files']};assert len(rows)==2241 and len(owned)==25 and sum(n.endswith('.cs') for n in owned)==18
base={r['Path']:r for r in load(A/'root-part315-main1/source-resource-closure.json')['Files']};outside=0
for r in rows:
 b=(C/r['Path']).read_bytes();assert len(b)==r['Bytes'] and sha(b)==r['Sha256']
 if r['Path'] not in owned:assert r==base[r['Path']];outside+=1
for n,r in owned.items():assert r['Added']==(n not in base) and r['ParentSha256']==(base[n]['Sha256'] if n in base else None)
core=A/'allocator-public-sys-profile316-composed1';cf=core/'formatted-core-source-freeze.json';assert sha(cf.read_bytes())=='EDF2CD42E162933F73EA2AA1588494F3754F2DB1AF529FD707C792E48149D6B5';assert len(load(cf)['Files'])==15
for r in load(cf)['Files']:assert sha((C/r['Path']).read_bytes())==r['Sha256']
assert (core/'tokens-before.json').read_bytes()==(core/'tokens-after.json').read_bytes();assert (D/'tokens-before.json').read_bytes()==(D/'tokens-after.json').read_bytes();assert len(load(D/'tokens-before.json'))==3
obs=A/'allocator-public-sys-profile316-oracle2';of=obs/'source-freeze.json';assert sha(of.read_bytes())=='B427BA5D2D6BF853F40AE11225BD03D1D7557A9008780C52D79AD8F11FC89B9A'
for r in load(of)['Files']+load(of)['Artifacts']:
 p=obs/'overlay'/r['Path'] if r in load(of)['Files'] else R/r['Path'];b=p.read_bytes();assert len(b)==r['Bytes'] and sha(b)==r['Sha256']
program=(obs/'overlay/tools/Erlang.Differential/Program.cs').read_bytes();insert=b'if (args.Length == 3 && args[2] == AllocatorProfileObservationNames.Command)\n    return await AllocatorProfileObservation.Run(args[0], args[1]);\n\n';assert program.count(insert)==1 and program.replace(insert,b'')==(B/'tools/Erlang.Differential/Program.cs').read_bytes()
observer=(obs/'overlay/tools/Erlang.Differential/AllocatorProfileObservation.cs').read_text();assert 'RawMatchClaimed = false' in observer and 'DeclaredClrAdaptation = true' in observer and 'CommitVerification' in observer and 'child.Kill(true)' in observer and 'await child.WaitForExitAsync();' in observer and 'await Task.WhenAll(stdout, stderr);' in observer
plans=[]
for n,w,key,count,pin in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1073,'89E2FF550301826DFE26C02318578E1D21252D85478B9261BB3673B80AE6236C'),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1098,'04E3A8CF4739C70F79921F2BC76710BD7496FDB0122CDAE5B18A4858A7B44637')]:
 raw=(C/n).read_bytes();assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf') and raw.decode().encode()==raw and sha(raw)==pin
 p=load(C/n);old=load(B/n);assert len(p['Files'])==count
 if 'NativeImports' in old:
  old_non={k:x for k,x in old.items() if k!='Files'};new_non={k:x for k,x in p.items() if k!='Files'}
  for lib in ['legacy_fixture.so','legacy_bridge.so']:
   assert set(p['NativeImports'][lib])-set(old['NativeImports'][lib])=={'__stack_chk_fail'} and set(old['NativeImports'][lib])<=set(p['NativeImports'][lib]);new_non['NativeImports']=dict(new_non['NativeImports']);new_non['NativeImports'][lib]=old['NativeImports'][lib]
  assert new_non==old_non
 else:assert {k:x for k,x in old.items() if k!='Files'}=={k:x for k,x in p.items() if k!='Files'}
 for r in p['Files']:
  b=(C/r['Path']).read_bytes();s=b.replace(b'\r\n',b'\n') if r['HashMode']=='TextLF' else b;assert len(s)==r['Bytes'] and sha(s)==r['Sha256'],r['Path']
 assert re.findall(rb'(?m)^\s*'+key.encode()+rb': ([A-F0-9]{64})\s*$',(C/w).read_bytes())==[pin.encode()]
 assert (n+' text eol=lf').encode() in (C/'.gitattributes').read_bytes().splitlines();plans.append({'Path':n,'Inputs':count,'RawCanonicalModeledStagedWorkflowPin':pin,'AllInputRowsVerified':True})
seal='tools/Erlang.NifPendingVarargsObservations/seal-source-plan.py';new=(C/seal).read_text();old=(B/seal).read_text();assert new.replace("args.output.write_bytes((json.dumps(receipt, indent=2) + '\\n').encode('utf-8'))", "args.output.write_text(json.dumps(receipt, indent=2) + '\\n', encoding='utf-8')")==old
attrs=(C/'.gitattributes').read_bytes();line=b'docs/validation/part-316/** binary\n';assert attrs.count(line)==1 and attrs.replace(line,b'')==(B/'.gitattributes').read_bytes()
report={'Status':'PASS_INDEPENDENT_SOURCE_ONLY_OBSERVER316_AND_FINAL_CANONICAL_COMPOSITION','FreezeSha256':sha(f.read_bytes()),'SourceClosureSha256':sha(cl.read_bytes()),'SourcePaths':25,'CSharpPaths':18,'VerifiedSourceRows':2241,'Parent315OutsideOwnedRowsExact':outside,'AllParentBindingsVerified':True,'FifteenActuallyCompiledCoreFilesRawExactEdf':True,'Core15AndObserver3TokenStreamsRawExact':True,'ObserverAuthorFreezeSha256':sha(of.read_bytes()),'ObserverSeparateCliAndWorkflowStepNoOrdinaryEqualityChange':True,'ExplicitNativeLibcAndSdkDotnetContractsAndRawValuesRetained':True,'TimeoutKillsAndJoinsChildAndStreams':True,'SourceCommitAuthorityExplicitlyExternalToVersionCheck':True,'ObserverRunNotClaimedBySourcePeer':True,'Plans':plans,'OnlyNativePolicyDeltaTwoLegacyStackCheckImports':True,'ExactActualImportBasisGuardSha256':sha((A/'pack315-reallocation-independent-failure-guard.json').read_bytes()),'AllOtherNativeProfilesAbiCHeadersParserPoliciesExact':True,'OfficialSealWriterOnlyUtf8LfWriteBytesChange':True,'ExpectedFullPassedNotObservedHere':6654,'ExpectedRegistryNotObservedHere':507,'SdkBuildNativeVmGitOrMainExecutedByReviewer':False,'BlockingFindings':[]}
p=A/'allocator-public-sys-profile316-final-beam310-source-peer.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Path':p.relative_to(R).as_posix(),'Sha256':sha(p.read_bytes()),'Status':report['Status']}))
