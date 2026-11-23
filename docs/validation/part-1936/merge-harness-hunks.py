from pathlib import Path
import difflib,hashlib,json,re,sys
stage,source=map(Path,sys.argv[1:])
manifest=json.loads((stage/'agent-intake.json').read_bytes())
target='tests/Erlang.Tests/Harness/Program.cs'
anchor='Test(Erlang.Tests.BeamBinaryConstructionStringChecks.TestName, Erlang.Tests.BeamBinaryConstructionStringChecks.Run);'
expected=(stage/'harness-registrations.txt').read_text().strip().splitlines()
old=(source/target).read_text()
assert old.count(anchor)==1 and not any(x in old for x in expected)
registrations=list(expected)
sync_name='Erlang.Tests.BeamTerminalCallProjectionChecks'
for index,registration in enumerate(registrations):
    listener=f'{sync_name}.Run'
    if registration.endswith(listener+');'):
        registrations[index]=registration.removesuffix(listener+');')+'() =>\n{\n'+f'    {listener}();\n'+            '    return Task.CompletedTask;\n});'
new=old.replace(anchor,anchor+'\n'+'\n'.join(registrations))
root=stage/'adapted-tests'
root.mkdir()
harness=root/'integrated-harness-registrations.patch'
patch=''.join(difflib.unified_diff(old.splitlines(keepends=True),new.splitlines(keepends=True),'a/'+target,'b/'+target))
harness.write_text(patch,encoding='utf-8',newline='\n')
queue=[]
receipts=[]
for entry in manifest:
    for path,expected in entry['PatchDigests'].items():
        fn=Path(path)
        assert hashlib.sha256(fn.read_bytes()).hexdigest()==expected.lower()
        if 'test' not in fn.name:
            queue.append(path)
            continue
        source_patch=fn.read_text(encoding='utf-8-sig')
        sections=re.split(r'(?=^diff --git )',source_patch,flags=re.MULTILINE)
        keep=[block for block in sections if block and not block.startswith('diff --git a/'+target+' ')]
        assert keep and all("+++ b/tests/Erlang.Tests/" in block for block in keep),len(keep)
        stripped=''.join(keep)
        assert target not in stripped
        out=root/(entry['Agent']+'.patch')
        out.write_text(stripped,encoding='utf-8',newline='\n')
        queue.append(out.as_posix())
        receipts.append(dict(Agent=entry['Agent'],Original=path,OriginalHash=expected,
                             Adapted=out.as_posix(),AdaptedHash=hashlib.sha256(out.read_bytes()).hexdigest()))
queue.append(harness.as_posix())
(stage/'intake-replay-order.txt').write_text('\n'.join(queue)+'\n',encoding='utf-8')
(stage/'adapted-test-receipt.json').write_text(json.dumps(receipts,indent=2)+'\n',encoding='utf-8')
print(len(queue),len(receipts))




