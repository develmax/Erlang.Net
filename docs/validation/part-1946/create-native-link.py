from pathlib import Path
z,u=map(Path,(__import__('sys').argv[1:]))
body=(z/'verify-linked-runtime.py').read_text(encoding='utf-8')
body=body.replace(str(z).replace('\\','/'),str(u).replace('\\','/')).replace("p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/response-roots-and-sequential-comprehensions-1944')","p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945')")
start=body[:body.index('import re\nrelease_paths=')]
capture=body[body.index('def capture_exact('):body.index('\nif absent:')]
parent='''
def until_parent():
    deadline=time.monotonic()+2700
    report=p/'full-uncached/otp-corpus.json'
    while not report.is_file():
        if time.monotonic()>deadline: raise TimeoutError(report)
        time.sleep(3)
    return report
'''
(u/'verify-linked-runtime.py').write_text(start+'import re\n'+capture+'\n'+parent+(u/'native-witness-body.txt').read_text(encoding='utf-8'),encoding='utf-8')
assert (u/'verify-linked-runtime.py').read_text().count(str(u).replace('\\','/'))==1

