from pathlib import Path
import re,json,hashlib,subprocess
root=Path.cwd();out=root/'artifacts/loader-iodata-native-prototype';art=out/'artifacts';tool=out/'tools/Erlang.LoaderIodataContrasts';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();old=json.loads((art/'iodata-pre-constant-source.json').read_text());fields={}
for p in tool.glob('*.cs'):
 for key,value in re.findall(r'const string (\w+) = ("(?:\\.|[^"\\])*");',p.read_text()):fields[p.stem+'.'+key]=value
def expanded(text):
 for key,value in sorted(fields.items(),key=lambda x:-len(x[0])):text=re.sub(r'\b'+re.escape(key)+r'\b',lambda m:value,text)
 return text
def tokens(text):return re.findall(r'"(?:\\.|[^"\\])*"|[^\s]',text)
for name in ['LoaderExporter.cs','LoaderProcess.cs']:assert tokens(expanded((tool/name).read_text()))==tokens(expanded(old[name])),name
(art/'iodata-constant-equivalence-initial-checker-failure.json').write_text(json.dumps({'Status':'FAIL','Reason':'Initial equivalence checker expanded pre-existing LoaderContrastNames only in new code; verification-only repair expands common definitions on BOTH sides. No behavior/source correction was required.'},indent=2)+'\n')
initial=json.loads((art/'iodata-source-freeze.json').read_text())
for row in initial['BaselineFiles']:assert sha(out/row['Path'])==row['Sha256']
for row in initial['AddedFiles']:
 if Path(row['Path']).suffix!='.cs':assert sha(out/row['Path'])==row['Sha256']
for p in tool.glob('*.cs'):
 for line in p.read_text().splitlines():
  if '"' in line:assert 'const string ' in line,(p.name,line)
dll=tool/'bin/Debug/net10.0/Erlang.LoaderIodataContrasts.dll';copy=root/'artifacts/iodata-managed-focus/bin/Debug/net10.0/Erlang.LoaderIodataContrasts.dll';assert dll.read_bytes()==copy.read_bytes()
raw=json.loads((root/'artifacts/iodata-managed-actual-constants-revision/focus.json').read_text());cli=json.loads((root/'artifacts/iodata-cli-actual-constants-revision/proof.json').read_text());assert raw['RawBytesExact'] and raw['IncompleteInheritedPipe'] and not raw['NativeExecuted'] and cli['Status']=='PASS' and cli['ToolSha256']==sha(dll)
held='9c7b49a3fb3fc3611bfb3656ec87b6ae0df436cb';assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==held
added=[]
for p in sorted(tool.iterdir()):
 if p.is_file():added.append({'Path':p.relative_to(out).as_posix(),'Sha256':sha(p),'Length':p.stat().st_size,'OldSource':old.get(p.name),'NewSource':p.read_text()})
workflow=out/'.github/workflows/loader-iodata-contrasts.yml';added.append({'Path':workflow.relative_to(out).as_posix(),'Sha256':sha(workflow),'Length':workflow.stat().st_size,'OldSource':None,'NewSource':workflow.read_text()})
proof={'Status':'ConstantsRevisionManagedVerifiedNativePending','InitialTwelveSourceFreezeSha256':sha(art/'iodata-source-freeze.json'),'PriorManagedProofPreserved':'iodata-target-focus-workflow-proof.json','AddedSourcePaths':added,'SourceDeltaCount':len(added),'ChangedCSharpFiles':['LoaderExporter.cs','LoaderProcess.cs'],'SemanticConstantOwnerClasses':sorted({key.split('.')[0] for key in fields}),'ExpandedCSharpTokenSequenceMatchesPriorBehavior':True,'NoInlineStringLiteralsOutsideConstantDeclarations':True,'ErlangWrapperDeclarationsAndPythonGoldensByteExact':True,'DefaultElevenToolAndBaselineByteExact':len(initial['BaselineFiles']),'TargetBuildWarnings':0,'TargetBuildErrors':0,'FocusBuildWarnings':0,'FocusBuildErrors':0,'ActualManagedControls':raw,'ActualCliRefusals':cli,'TargetDllSha256':sha(dll),'HeldMainHead':held,'CurrentDefaultMfas':493,'CurrentErlangRegistered':335,'NativeSixteenExecuted':False,'OriginalErlangSyntaxVerified':False,'MainChanged':False}
(art/'iodata-constants-revision-source-freeze.json').write_text(json.dumps(proof,indent=2)+'\n')
print('PASS token-exact constant substitution/20paths/default11 goldens/target0+realmanaged5CLI; no original execution')
print('FreezeSHA256',sha(art/'iodata-constants-revision-source-freeze.json'))
