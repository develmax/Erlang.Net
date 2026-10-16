from pathlib import Path
import hashlib,json
R=Path.cwd();A=R/'artifacts';j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
bind=lambda p:{'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p)}
head='b51f050c287f3b91346e1d3e30e86061708fb035';guard=A/'pack326-reallocation-independent-guard2.json';v=j(guard)
assert v['Head']==head and v['WholeInstalledPrefixFilesPassivelyRehashed']==2646 and v['PublishedPlanInputsRehashed']==1134
names=[]
for row in j(A/'pack325-native-reallocation-lean-copy-rows.json')['Rows']:
    if '/reallocation/' in row['TargetPath']:
        name=row['SourcePath'].replace('pack325-','pack326-').replace('38848192290','38850745048').replace('12024298549','12025517303').replace('116803360455','116811177237')
        assert (R/name).is_file(),name;names.append(name)
names+=['artifacts/pack326-reallocation-passive-source-adaptation.json','artifacts/pack326-build-reallocation-supplement.py']
summary=A/'pack326-reallocation-terminal-independent-summary.json';assert not summary.exists()
summary.write_text(json.dumps({'Status':'PASS_EXACT_PUBLISHED326_PINNED_FULL_PREFIX_AND_ORIGINAL3_SDK3_REALLOCATION','Head':head,'Run':38850745048,'Job':116811177237,'Artifact':12025517303,'Attempt':1,'IndependentGuard':bind(guard),'Own1134Sha256':'53500EFC145705B70364DE7BC19E60FBCE55C62C7C4C00CB66712B1B8F01F9E4','Legacy1109Sha256':'A673C88561EC5E31E1F69BE5795519A3AA658E013B6000910B03E192A520E6A5','ConfiguredPrefixFiles':2646,'WholeNamespaceAndSymlinksVerified':True,'FourRuntimeRoleIdentitiesVerified':True,'ThreePinnedHeadersAndActualConfiguredHeaderVerified':True,'FourElfSymbolsAndCapturedNmReadelfVerified':True,'PublishedPassiveComparison875FReplayed':True,'OriginalMasks':[1539,1601,2047],'SdkMasks':[1539,1601,2047],'NoOomPhysicalGeneralAbiClaim':True,'ReviewerNativeVmSdkNetworkMainGitMutation':False},indent=2)+'\n',encoding='utf-8');names.append(summary.relative_to(R).as_posix())
four=A/'pack326-four-terminal-independent-summary.json';assert not four.exists();guards=[]
for role,name in [('ci','pack326-ci-independent-guard.json'),('native-distribution','pack326-native-distribution-independent-summary.json'),('reallocation',summary.name)]:
    p=A/name;value=j(p);assert value['Head']==head and value['Status'].startswith('PASS');guards.append({'Role':role,**bind(p)})
four.write_text(json.dumps({'Status':'PASS_PUBLISHED326_CI_NATIVE_DISTRIBUTION_REALLOCATION_FOUR_TYPED_WORKFLOW_FAMILIES','Head':head,'CiTests':6708,'Integrations':11,'NativeStrictEightStdoutLinesDomainThreeSeparateOriginalThirteen':True,'DistributionGl4Send7Link7Exit8ControlWait2Retention2':True,'ReallocationOriginal3Sdk3':True,'StrictExitDisplayReprRefusalRetainedWithoutNormalization':True,'AllStrictRawTextComparisonsPassed':False,'Ordinary326NotYetIndependentlyAuditedByThisSummary':True,'New326FourteenSpawnOriginalCasesNotInferred':True,'Bindings':guards},indent=2)+'\n',encoding='utf-8');names.append(four.relative_to(R).as_posix())
rows=[]
for name in dict.fromkeys(names):
    p=R/name;assert p.is_file() and p.suffix.lower() not in {'.dll','.so','.zip','.tar','.gz'}
    rows.append({'SourcePath':name,'TargetPath':'docs/validation/part-327/previous-head326/reallocation/'+name.removeprefix('artifacts/'),'Bytes':p.stat().st_size,'Sha256':h(p)})
out=A/'pack326-reallocation-terminal-lean-copy-rows.json';assert not out.exists();out.write_text(json.dumps({'Status':'EXPLICIT_NEW326_REALLOCATION_ONLY_SELECTED_RAW_FOR_FUTURE327','Head':head,'Rows':rows,'Count':len(rows),'Bytes':sum(x['Bytes'] for x in rows),'NoApiArchivesOrRuntimeNativeManagedBinariesCopied':True,'SelectedRawNotWholeRuntimePortableClaim':True},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Guard':bind(guard),'Manifest':bind(out),'Summary':bind(summary),'FourFamiliesSummary':bind(four),'Rows':len(rows),'Bytes':sum(x['Bytes'] for x in rows)}))
