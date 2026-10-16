from pathlib import Path,PurePosixPath
import argparse,json,hashlib,zipfile

root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--formatted-freeze',required=True)
parser.add_argument('--formatted-freeze-sha256',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args()
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def dump(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
formatted_path=Path(args.formatted_freeze).resolve()
assert sha(formatted_path.read_bytes())==args.formatted_freeze_sha256.upper()
formatted=json.loads(formatted_path.read_bytes())
assert formatted['AuthorFreezeSha256']=='4C01B791522C2EBA5EDF27707A41EB0D8D037CB9F7D7FFA5CE5FFDAACB334FFD'
assert (formatted['SourcePaths'],formatted['CSharpPaths'],formatted['SourceClosurePaths'])==(17,17,2379)
assert formatted['AllOwnedTokensByteExact'] and formatted['OutsideOwnedRawExact']==2362
token_before=formatted_path.parent/'tokens-before.json';token_after=formatted_path.parent/'tokens-after.json'
assert token_before.read_bytes()==token_after.read_bytes()
green=formatted_path.parent/'candidate'
author_path=root/'artifacts/process-error-handler327-candidate4/source-freeze.json'
assert sha(author_path.read_bytes())==formatted['AuthorFreezeSha256']
author=json.loads(author_path.read_bytes())
helpers=[row['Path'] for row in author['Files'] if not row['Path'].startswith('src/')]
assert len(helpers)==6 and all(p.endswith('.cs') for p in helpers)
formatted_rows={row['Path']:row for row in formatted['Files']}
green_data={p:(green/p).read_bytes() for p in helpers}
for p,b in green_data.items():assert sha(b)==formatted_rows[p]['Sha256'] and len(b)==formatted_rows[p]['Bytes']
baseline_path=root/'artifacts/process-error-handler327-candidate1/baseline-source-resource-closure.json'
baseline=json.loads(baseline_path.read_bytes())['Files'];assert len(baseline)==2370
archive_path=root/'artifacts/process-error-handler327-candidate1/baseline-source-resources.zip'
assert sha(archive_path.read_bytes())=='B3DB48FE446616624D2AD0CFBF9C917CB70D62A1D2241C828DE4F3D3E3A03AF1'
preflight_path=root/'artifacts/process-error-handler327-candidate1/published-parent-source-preflight.json'
preflight=json.loads(preflight_path.read_bytes());assert preflight['Head']=='b51f050c287f3b91346e1d3e30e86061708fb035'
assert sha(preflight_path.read_bytes())==author['PublishedParentPreflightSha256']
published={row['Path']:row for row in preflight['Files']};assert len(published)==2370
base={row['Path']:row for row in baseline}
with zipfile.ZipFile(archive_path) as archive:
 assert set(archive.namelist())==set(base)
 baseline_data={p:archive.read(p) for p in base}
 for p,b in baseline_data.items():
  assert not PurePosixPath(p).is_absolute() and '..' not in PurePosixPath(p).parts
  assert sha(b)==base[p]['Sha256'] and len(b)==base[p]['Bytes']
  assert sha(b)==published[p]['WorkingSha256']
  assert sha(b)==published[p]['PublishedBlobSha256'] or sha(b.replace(b'\r\n',b'\n'))==published[p]['PublishedBlobSha256']
out=Path(args.output).resolve();assert out.is_relative_to(root/'artifacts') and not out.exists()
out.mkdir();candidate=out/'candidate'
for p,b in baseline_data.items():
 target=candidate/p;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b)
for p,b in green_data.items():
 target=candidate/p;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b)
paths=set(base)|set(helpers);assert len(paths)==2374
rows=[]
for p in sorted(paths):
 b=(candidate/p).read_bytes();rows.append({'Path':p,'Bytes':len(b),'Sha256':sha(b)})
for p,b in baseline_data.items():
 if p not in helpers:assert (candidate/p).read_bytes()==b
production=[p for p in paths if p.startswith('src/')]
assert all((candidate/p).read_bytes()==baseline_data[p] for p in production)
added_production=[row['Path'] for row in author['Files'] if row['Path'].startswith('src/') and row['Added']]
assert len(added_production)==5 and all(not (candidate/p).exists() for p in added_production)
dump(out/'source-resource-closure.json',{'Files':rows})
dump(out/'historical-build-recipe.json',{
 'Status':'SOURCE_ONLY_NO_EXECUTION','Project':str(candidate/'tests/Erlang.Tests/Erlang.Tests.csproj'),
 'Selector':'Erlang.Tests.ProcessErrorHandlerHostChecks:RunState',
 'ExpectedOldRoute':'Real public process_flag(error_handler,Atom) badarg before generated module compilation or barriers; only genuine RED after actual clean target and settled nonzero selector process.',
 'CpuBudget':2,'BuildSingleNode':True,'SharedCompilation':False,'NodeReuse':False,'RestoreParallel':False,
 'RootOwnedBoundedProcessRequired':True,'ActualBuildResult':None,'ActualSelectorResult':None,
 'NeverTreatCompileTypeSelectorTimeoutAsSemanticRed':True})
freeze={'Status':'HISTORICAL326_SOURCE_ONLY_SAME_SIX_FORMATTED327_HELPERS_UNBUILT',
 'BaselinePublishedHead':'b51f050c287f3b91346e1d3e30e86061708fb035',
 'BaselineArchivePath':str(archive_path),'BaselineArchiveSha256':sha(archive_path.read_bytes()),
 'BaselineClosurePath':str(baseline_path),'BaselineClosureSha256':sha(baseline_path.read_bytes()),
 'PublishedParentCanonicalPreflightPath':str(preflight_path),'PublishedParentCanonicalPreflightSha256':sha(preflight_path.read_bytes()),
 'GreenFormattedFreezePath':str(formatted_path),'GreenFormattedFreezeSha256':sha(formatted_path.read_bytes()),
 'GreenTokensBeforeSha256':sha(token_before.read_bytes()),'GreenTokensAfterSha256':sha(token_after.read_bytes()),
 'AuthorCandidate4FreezeSha256':sha(author_path.read_bytes()),'CandidateDirectory':str(candidate),
 'AppliedSameFormattedPaths':helpers,'Files':[next(r for r in rows if r['Path']==p) for p in sorted(helpers)],
 'SourcePaths':6,'CSharpPaths':6,'SourceClosurePaths':len(rows),'SourceClosureSha256':sha((out/'source-resource-closure.json').read_bytes()),
 'AllBaselineProductionIncludingTermsRawExact':True,'BaselineOutsideSixRawExact':2368,
 'FiveNewProductionClassesOrEnumAbsent':added_production,'ActualBuildOrVmResult':None,
 'MainOrGitMutated':False,'SourceOnly':True}
dump(out/'source-freeze.json',freeze)
print(json.dumps({'FreezeSha256':sha((out/'source-freeze.json').read_bytes()),'ClosureSha256':freeze['SourceClosureSha256'],'Files':len(rows),'SameHelpers':len(helpers)}))
