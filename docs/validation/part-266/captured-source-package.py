from pathlib import Path
import hashlib,json,subprocess,re,shutil
repo=Path(r'D:\Distr\Work\Codex\Erlang.Net\Erlang.Net')
candidate=Path(r'D:\Distr\Work\Codex\Erlang.Net\artifacts\loader-gzip-iodata-revision2-prototype')
draft=Path(r'D:\Distr\Work\Codex\Erlang.Net\artifacts\loader-gzip-iodata-prototype')
stage=repo/'artifacts/part266-agent-staging'
stage.mkdir(exist_ok=True)
f=candidate/'artifacts/gzip-iodata-source-freeze-revision6.json'
freeze=json.loads(f.read_text());rows=[]
for item in freeze['Files']:
    path=item['Path'];after=(candidate/path).read_bytes()
    assert hashlib.sha256(after).hexdigest()==item['AfterSha256']
    assert (repo/path).read_bytes()==after,path
    original=subprocess.run(['git','-C',str(repo),'show','7ae41e30f377749a67f12eeb47cdd829479434ea:'+path],capture_output=True)
    before=original.stdout if original.returncode==0 else None
    assert (hashlib.sha256(before).hexdigest() if before is not None else None)==item['BeforeGitBlobSha256']
    for folder,data in [('after',after),('before',before)]:
        if data is not None:
            target=stage/folder/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
    rows.append(item)
# Compare the unchanged raw DEFLATE algorithm as C# lexical tokens; whitespace is immaterial.
def tokens(s):
    return re.findall(r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_]\w*|\d+|==|!=|>=|<=|\+\+|--|&&|\|\||=>|[^\s]',s)
old=subprocess.run(['git','-C',str(repo),'show','7ae41e30f377749a67f12eeb47cdd829479434ea:src/Erlang.Runtime/BeamZlibMemberBoundary.cs'],capture_output=True,check=True).stdout.decode()
new=(candidate/'src/Erlang.Runtime/BeamZlibMemberBoundary.cs').read_text()
old_body=old[old.index('        long produced = 0;'):old.index('        reader.Skip(BeamDeflateFormat.AdlerBytes')]
new_body=new[new.index('        long produced = 0;'):new.index('        return reader.PositionBytes;')]
assert tokens(old_body)==tokens(new_body)
# Standalone source declaration expansion: each semantic char/string must evaluate to its prior value.
checks=(candidate/'tests/Erlang.Tests/BeamGzipNativeChecks.cs').read_text()
names=(candidate/'tests/Erlang.Tests/BeamGzipNativeNames.cs').read_text()
values={name:value for name,value in re.findall(r'internal const (?:char|string)\s+(\w+)\s*=\s*("(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\')\s*;',names)}
expected={'ResourceSeparator':'"."','ResourceNamespaceSeparator':"'.'",'ResourcePathSeparator':"'/'",'ResourceFolderHyphen':"'-'",'ResourceNamespaceUnderscore':"'_'"}
assert all(values[k]==v for k,v in expected.items())
previous=(candidate/'artifacts/BeamGzipNativeChecks.cs.revision5.passed.cs.txt').read_text()
expanded=checks
for key,value in expected.items():expanded=expanded.replace('BeamGzipNativeNames.'+key,value)
assert tokens(expanded)==tokens(previous)
# Every new string literal appears in its semantic owner declaration, not executable checks.
inline=[]
for path in ['src/Erlang.Runtime/BeamGzipMembers.cs','src/Erlang.Runtime/BeamGzipFormat.cs','tests/Erlang.Tests/BeamGzipHostChecks.cs','tests/Erlang.Tests/BeamGzipNativeChecks.cs']:
    text=(candidate/path).read_text()
    if any(t.startswith('"') for t in tokens(text)):inline.append(path)
assert not inline
for source in [draft/'artifacts/gzip-iodata-source-freeze.json',draft/'artifacts/gzip-iodata-reuse-audit.md']+list((candidate/'artifacts').glob('*')):
    if source.is_file():
        target=stage/'evidence'/source.name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
# Include exact six-file original draft as independently recoverable source.
for row in json.loads((draft/'artifacts/gzip-iodata-source-freeze.json').read_text())['Files']:
    source=draft/row['Path'];assert hashlib.sha256(source.read_bytes()).hexdigest()==row['AfterSha256']
    target=stage/'original-draft'/row['Path'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
receipt={'Status':'Exact68ImportedSourcesAndEvidenceStaged','PublishedBaseline':'7ae41e30f377749a67f12eeb47cdd829479434ea','FreezeSha256':hashlib.sha256(f.read_bytes()).hexdigest(),'ExactFiles':len(rows),'BeforeAfterRows':rows,'ExtractedDeflateTokenBodyExact':True,'NamedConstantsStandaloneExpansionExact':True,'ExecutableNewInlineStrings':inline,'OriginalDraftFreezeSha256':hashlib.sha256((draft/'artifacts/gzip-iodata-source-freeze.json').read_bytes()).hexdigest(),'InitialFocusedFailures':0,'BroadControlsNativeOriginalObserved':False,'MainFull6483Confirmed':False,'NoWholeSeedCopied':True,'MainGitMutations':False}
(stage/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Files':len(rows),'Stage':str(stage),'ReceiptSha256':hashlib.sha256((stage/'receipt.json').read_bytes()).hexdigest()}))
