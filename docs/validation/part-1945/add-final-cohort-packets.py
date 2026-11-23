from pathlib import Path
import hashlib,json,re,sys
stage,binary,asn=map(Path,sys.argv[1:])
p=stage/'component-specs.json'
specs=json.loads(p.read_bytes())
for label,root,receipt,report,target,keys,hashes in [
    ('ComputedDiscardedComprehensionSources',binary/'discarded-list-helper-effects-1945','receipt.json',
     'candidate-corpus.json','7a4f2643f4bd75e517124b0327c5a5b4a5822b1127ab63c6b093e53f5d5899eb',
     ['discarded-computed-generator.patch','discarded-computed-generator-test.patch'],['ArtifactHashes']),
    ('RemoteOptionalFieldLifetimes',asn/'remote-optional-field-lifetime','final-receipt.json',
     'candidate.json','e0b367766a64193788519436f8d585ed51a229cfea514c64d5f61fa3c0fd76bc',
     ['remote-optional-lifetime.patch','remote-optional-lifetime-tests.patch'],['StrictReplay','Patches']),
]:
    assert hashlib.sha256((root/receipt).read_bytes()).hexdigest()==target
    specs[label]=dict(Root=root.as_posix(),Receipt=receipt,Report=report,Hashes=hashes,Patches=keys)
p.write_text(json.dumps(specs,indent=2)+'\n',encoding='utf-8')
scope=binary/'discarded-list-helper-effects-1945/discarded-cohort.terms'
lines=[line for line in scope.read_text().splitlines()
       if re.search(r',(?:statement|discarded_match),generate,(?:call|cons|record_field),var,call,\[\],',line)]
assert len(lines)==32,len(lines)
modules={re.match(r'^\{"([^"]+\.beam)"',line).group(1) for line in lines}
print('Discarded source cohort:',len(lines),'keys',len(modules),'modules')
cases=(stage/'case-valued-cohort.txt').read_text(encoding='utf-8-sig').splitlines()
assert len(cases)==32
controls={'lib/inets-9.8/ebin/mod_head.beam','lib/inets-9.8/ebin/httpd_instance_sup.beam','lib/inets-9.8/ebin/mod_trace.beam',
    'lib/ssh-6.0.6/ebin/ssh_tcpip_forward_client.beam','lib/ssh-6.0.6/ebin/ssh_tcpip_forward_srv.beam','lib/kernel-11.0.4/ebin/local_udp.beam',
    'lib/public_key-1.21.7/ebin/PKIX1Implicit-2009.beam'}
(stage/'all-affected-scope.txt').write_text('\n'.join(sorted(modules|set(cases)|controls,key=str.casefold))+'\n',encoding='utf-8')
sync=set()
for part in specs.values():
    text=(Path(part['Root'])/part['Patches'][1]).read_text(encoding='utf-8-sig')
    if re.search(r'^\+\s*internal static void Run\(\)',text,re.MULTILINE):
        classes=re.findall(r'^\+internal static class (\w+)',text,re.MULTILINE)
        assert classes,part
        sync.update('Erlang.Tests.'+klass+'.Run' for klass in classes)
(stage/'synchronous-callbacks.json').write_text(json.dumps(sorted(sync),indent=2)+'\n',encoding='utf-8')

