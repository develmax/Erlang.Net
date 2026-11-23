from pathlib import Path
import json,hashlib,shutil,sys
n,m=map(Path,sys.argv[1:])
spec={}
data=json.loads('''[["ClosedCompoundConstructorGroups","D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931/closed-compound-patterns-1957","closed-compound-patterns-receipt.json","e3afb919ac5cd457ef8f3c20b5fa9cc743bfaa796ca1e45f8db6671b005bad08","candidate-final-corpus.json",["closed-compound-patterns.patch","closed-compound-patterns-test.patch"],["replayFiles"],"sha256"],["RecursiveIntegerEntryFacts","D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/recursive-integer-entry-types-1953","receipt.json","b05d6098fcf59622216d42dd92e0bfd635ad2f26d6f455dc5f84fa9ff348f1d0","scope/otp-corpus.json",["production.patch","test.patch"],["PatchDigests"],null],["X400SetRankDispatch","D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/x400-set-closures-1951","final-receipt.json","4c551fd3dd97476e3f1ff7b26f385b152532c5ea65bd5768330118c7e55e6dff","candidate.json",["x400-set-rank-dispatch.patch","x400-set-rank-dispatch-tests.patch"],["StrictReplay","Patches"],null],["X400StaticMapTarget","D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/x400-static-map-1952","final-receipt.json","53de38ba40ba67b4856554a2dec81087237683d69ca00e3997b58ffb590bfd90","candidate.json",["x400-static-map.patch","x400-static-map-tests.patch"],["StrictReplay","Patches"],null],["X400PairProjection","D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/x400-pair-projection-1952","final-receipt.json","9c4625691bc63bf212195fc3ff1eeced7f2e520e83e1a0f578fa73185c20afd9","candidate.json",["x400-pair-projection.patch","x400-pair-projection-tests.patch"],["StrictReplay","Patches"],null]]''')
for label,root,name,expected,report,patches,keys,field in data:
    root=Path(root)
    assert hashlib.sha256((root/name).read_bytes()).hexdigest()==expected,name
    spec[label]=dict(Root=root.as_posix(),Receipt=name,Report=report,Patches=patches,Hashes=keys)
    if field:
        spec[label]['DigestField']=field
(n/'component-specs.json').write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8')
for name in ('normalize-frozen-partitions.py','cached-normalized-delta.eval','normalized-cache-benchmark.json'):
    shutil.copyfile(m/name,n/name)

