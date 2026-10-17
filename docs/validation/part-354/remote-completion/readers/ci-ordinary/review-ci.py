"""Unexecuted passive same-head CI354; trusted data only, no SDK/network."""
import json,re,collections
import common as m
FULL='docs/validation/part-354/'
RAW=FULL+'full/raw/'
GUARD=FULL+'root-delivery/size-bif354-full4-independent/actual-report.json'
PEER_SHA='5DE0409B787679B78A7780DC30F941991B1A292E859C7D5F74AAA65EA9191C78'
def main(args):
 extracted,run,job,log=m.transport(args,'ci');pub,values,count=m.publication(args)
 tf=list(extracted.rglob('tests.json'));it=list(extracted.rglob('integration-results.json'));m.need(len(tf)==len(it)==1,'one CI tests/integrations')
 remote=m.read(tf[0]);names=m.test_names(remote,7343)
 local=json.loads(values[RAW+'artifacts/tests.json']);old=json.loads(m.blobs(args.repository,'a9ec41acf2eb5727f60c613ba8146c470f82fbb0',['docs/validation/part-353/full/raw/artifacts/tests.json'])['docs/validation/part-353/full/raw/artifacts/tests.json'])
 m.need(old['Passed']==7313 and old['Failed']==0 and names[:7313]==[x['Name'] for x in old['Tests']] and names==[x['Name'] for x in local['Tests']],'all7313 old ordered names and actual full7343')
 peer=json.loads(values[GUARD]);m.need(m.digest(values[GUARD])==PEER_SHA and peer['FullAccepted'] and peer['Passed']==7343 and peer['Failed']==0 and peer['LedgerRequiredEvidenceSchemaVerified'],'accepted actual full4')
 m.need(m.read(it[0])==peer['IntegrationResults']==m.EXPECTED_INTEGRATIONS==json.loads(values[RAW+'artifacts/integration-results.json']),'exact11 integrations')
 composition=json.loads(values[RAW+'composition-plan.json']);m.need(m.digest(values[RAW+'composition-plan.json'])==peer['CompositionPlanSha256']=='C209F4B6A4B97655D70E5AB794334554955502C998B67DB428BA5C6827D72653' and len(composition['StableClosure'])==3210 and len(composition['OwnedPaths'])==134,'actual composition')
 inventory=m.read(m.Path(__file__).parent/'source-representations.json');m.need(m.sha(m.Path(__file__).parent/'source-representations.json')=='F7962B86491F67F31BFE63108FA1145186BAFCA01C1F5291A94BF7550CF64615','closed source inventory')
 declared={x['Path']:x for x in inventory['Differences']};source=m.blobs(args.repository,args.expected_head,[x['Path'] for x in composition['StableClosure']]);changed=[]
 for row in composition['StableClosure']:
  data=source[row['Path']]
  if m.digest(data)!=row['Sha256'] or len(data)!=row['Bytes']:
   d=declared[row['Path']];m.need(d['FrozenSha256']==row['Sha256'] and d['PublishedSha256']==m.digest(data),'closed exact source change')
   if d['Reason']=='ClosedTextLFRepresentation':
    frozen=(args.repository/'artifacts/size-bif354-full-candidate4'/row['Path']).read_bytes();m.need(m.digest(frozen)==row['Sha256'] and frozen.replace(b'\r\n',b'\n')==data and not row['Path'].endswith('.cs'),'exact LF representation only')
   changed.append(d)
 m.need({x['Path'] for x in changed}==set(declared),'no hidden or missing source delta')
 # Every owned C# source has its archived actual-tested bytes, with exactly one publication correction.
 cs=[p for p in composition['OwnedPaths'] if p.endswith('.cs') and p.startswith(('src/','tests/','tools/'))];snap=m.blobs(args.repository,args.expected_head,[FULL+'source-attempt3/'+p for p in cs]);m.need(len(cs)==55,'actual55 owned CSharp')
 mapping={x['Path']:x for x in composition['StableClosure']}
 for path in cs:
  before=snap[FULL+'source-attempt3/'+path];m.need(m.digest(before)==mapping[path]['Sha256'],'actual tested source snapshot')
  if path!='tools/Erlang.Differential/SizeErrorCaseSources.cs':m.need(before==source[path],'other CSharp raw exact')
  else:
   after=source[path];a,b=before.splitlines(keepends=True),after.splitlines(keepends=True);m.need(len(a)==len(b),'literal line count');delta=[(x,y) for x,y in zip(a,b) if x!=y]
   m.need(len(delta)==11 and all(x.strip()==y.strip()==b'' and y in (b'\n',b'\r\n') for x,y in delta),'exact11 blank lines')
   proof=json.loads(values[FULL+'publication-whitespace/actual-proof.json']);m.need(proof['BeforeSha256']==m.digest(before) and proof['AfterSha256']==m.digest(after) and len(proof['Fixtures'])==23 and proof['Tokens']==173 and proof['ChangedRawLiteralTokenCount']==5,'actual Roslyn/compiled23 identity')
   m.need(m.constants(before.decode())==m.constants(after.decode()) and all(m.digest(m.constants(after.decode())[x['Name']].encode())==x['Utf8Sha256'] for x in proof['Fixtures']),'all23 rawliteral values exact')
 registry=m.blobs(args.repository,args.expected_head,['docs/supported-mfas.json','docs/mfa-compatibility.json','docs/module-readiness.json']);before=json.loads(values[RAW+'registry-before.json']);raw=json.loads(values[RAW+'raw-supported-mfas.json']);restored=json.loads(registry['docs/supported-mfas.json'])
 m.need(registry['docs/supported-mfas.json']==values[RAW+'registry-before.json'] and len(restored)==len(raw)==587 and [{k:v for k,v in x.items() if k!='Limits'} for x in raw]==[{k:v for k,v in x.items() if k!='Limits'} for x in before],'all587 order/nonLimits and exact customLimits restoration')
 ledger=json.loads(registry['docs/mfa-compatibility.json']);counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));m.need(counts=={'Verified compatible':16,'Known differences':30,'Needs verification':541} and len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==115,'exact statuses115guards')
 readiness=json.loads(registry['docs/module-readiness.json']);m.need((readiness['RegisteredMfas'],readiness['LocalPassedTests'],readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'])==(587,7343,16,30,541),'readiness actual7343')
 guards=m.blobs(args.repository,args.expected_head,[x['Path'] for x in ledger['VerifiedSourceFiles']]);m.need(len(guards)==115,'unique115 guards')
 for row in ledger['VerifiedSourceFiles']:m.need(m.digest(guards[row['Path']])==row['Sha256'],'actual source guard')
 proofs=m.blobs(args.repository,args.expected_head,{'docs/'+x['Path'] for e in ledger['Entries'] for x in e['Evidence']})
 for e in ledger['Entries']:
  for row in e['Evidence']:m.need(set(row)=={'Kind','Path','Sha256','Scope'} and isinstance(row['Scope'],str) and row['Scope'].strip() and m.digest(proofs['docs/'+row['Path']])==row['Sha256'],'all required evidence fields and hashes')
 package_map=pub['PackageSourceRepresentationMapping']['Resources'];m.need(len(package_map)==6,'six explicit package representations');package_values=m.blobs(args.repository,args.expected_head,[x['Path'] for x in package_map]);pr={x['Path']:x for x in composition['PackageResources']};lf=[]
 for row in package_map:
  p=row['Path'];actual=package_values[p];tested=(args.repository/'artifacts/size-bif354-full-candidate4'/p).read_bytes();m.need(m.digest(tested)==row['TestedWindowsSourceSha256']==pr[p]['Sha256'] and m.digest(actual)==row['StagedGitSourceSha256'],'source/tested package bindings')
  m.need((tested==actual)==row['RawEqual'] and (tested.replace(b'\r\n',b'\n')==actual.replace(b'\r\n',b'\n'))==row['TextLfEqual'],'explicit raw vs text package equality')
  if tested!=actual:lf.append(p)
 m.need(set(lf)=={'docs/DOTNET-LICENSE.txt','docs/DOTNET-ThirdPartyNotices.txt','docs/OTP-LICENSE.txt','docs/OTP-PORTS.md'},'closed four package LF paths')
 m.need(len(peer['Packages'])==2 and all(x['CrcAndSafeUniquePathsVerified'] for x in peer['Packages']),'accepted full4 two packages; payload absent in public archive')
 # Raw bounded children are rechecked against the retained actual loaded Terms hash.
 captured=json.loads(values[RAW+'captured-outputs.json']);terms=[x['Sha256'] for x in captured['TestDlls'] if x['Path'].endswith('/Erlang.Terms.dll')];m.need(len(terms)==1,'one actual full4 Terms')
 children=[p for p in values if p.startswith(FULL+'full/children/') and p.endswith('/receipt.json')];m.need(len(children)==12,'12 children');matrix=set()
 for p in children:
  v=json.loads(values[p]);m.need(v['Started'] and v['Settled'] and not v['TimedOut'] and not v['KillRequested'] and v['ExitCode']==0 and not v['LifecycleFailures'] and v['StandardOutputComplete'] and v['StandardErrorComplete'] and v['ProcessId']>0,'normal owned child');d=p.rsplit('/',1)[0];stdout,stderr=values[d+'/stdout.txt'],values[d+'/stderr.txt'];m.need(m.digest(stdout)==v['StandardOutputSha256'] and m.digest(stderr)==v['StandardErrorSha256'] and stderr==b'','child rawstreams');lines=stdout.decode('utf-8-sig').splitlines();m.need(json.loads(lines[0])['TermsAssemblySha256']==v['TermsDllSha256']==terms[0] and lines[-1]=='PASS','actual loaded Terms identity');matrix.add((v['Family'],v['Operation'],v['Depth']))
 expected={(f,op,32768) for f in ('tuple','list-head','map-value','fun-environment','native-record') for op in ('compare','hash')}|{('tuple','shallow',6),('tuple','snapshot',0)};m.need(matrix==expected,'exact child matrix')
 native=[]
 for p,w,var,n in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1282),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1307)]:
  pair=m.blobs(args.repository,args.expected_head,[p,w]);body=json.loads(pair[p]);m.need(b'\r' not in pair[p] and not pair[p].startswith(b'\xef\xbb\xbf') and len(body['Files'])==n,'strictLF nativeplan');nv=m.blobs(args.repository,args.expected_head,[x['Path'] for x in body['Files']])
  for row in body['Files']:
   b=nv[row['Path']];m.need(row['HashMode'] in ('TextLF','BinaryExact'),'row policy');b=b.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else b;m.need(m.digest(b)==row['Sha256'] and len(b)==row['Bytes'],'every native row')
  m.need(re.findall(var.encode()+rb': ([A-F0-9]{64})',pair[w])==[m.digest(pair[p]).encode()],'native seal');native.append(dict(Path=p,Rows=n))
 m.need('origin +'+args.expected_head+':refs/remotes/origin/main' in log,'checkout subject')
 m.fresh(args.report,dict(Status='PASS_PASSIVE_PUBLISHED354_CI7343_ELEVEN_INTEGRATIONS',Head=args.expected_head,Run=args.run,Job=job['id'],Artifact=args.artifact,Passed=7343,Failed=0,Integrations=11,Old7313OrderedPrefixPreserved=True,New30Names=names[7313:],SourceGuards=115,CompatibilityCounts=counts,PublishedStageBlobs=count,OwnedCSharpSnapshots=55,StableSourceRows=3210,ExplicitSourceRepresentations=changed,WholeClosureRawEqual=False,PackageResources=6,PackageLFPaths=lf,PackagePayloadRehashedByThisReader=False,AcceptedActualPackageReceipts=2,BoundedChildren=12,NativePlans=native,ActualReportSha256=m.sha(tf[0]),IndependentFull4Sha256=PEER_SHA,SdkOrNativeExecutedByReviewer=False,NetworkUsed=False))
if __name__=='__main__':main(m.parser().parse_args())
