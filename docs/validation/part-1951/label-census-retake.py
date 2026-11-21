from pathlib import Path
import hashlib,json,os,re,subprocess,sys,time

stage,donor,oracle=map(Path,sys.argv[1:])
out=stage/'label-census-retake'
out.mkdir()
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def rows(stage_dir):
    return dict(line.split('\t') for line in (stage_dir/'all-final-label-paths.tsv').read_text(encoding='utf-8-sig').splitlines())
expected=rows(stage)
reference=rows(donor)
assert list(expected)==list(reference) and len(expected)==1366
raw=(donor/'final-label-census.terms').read_bytes().splitlines(keepends=True)
assert len(raw)==1367 and b'total => 1366,canonical => 1366,noncanonical => []' in raw[-1]
pattern=re.compile(rb'^\{<<"([\w./-]+)">>,')
def index(items):
    return {pattern.match(line).group(1).decode():line for line in items}
indexed=index(raw[:-1])
assert list(indexed)==list(expected)
hashes={key:sha(Path(file)) for key,file in expected.items()}
same=[key for key,digest in hashes.items() if digest==sha(Path(reference[key]))]
changed=[key for key in expected if key not in set(same)]
proof={k:indexed[k] for k in same}
script=(stage/'final-label-census.eval').read_text(encoding='utf-8-sig')
batches=[]
began=time.monotonic()
for first in range(0,len(changed),12):
    batch=changed[first:first+12]
    ctl=out/f'{first:03d}.tsv'
    term=out/f'{first:03d}.terms'
    ctl.write_text(''.join(f'{key}\t{expected[key]}\n' for key in batch),encoding='utf-8',newline='\n')
    with (out/f'{first:03d}.stderr.log').open('wb') as err:
        run=subprocess.run([oracle,'+S','1:1','+SDcpu','1','+SDio','1','+A','1','-noshell','-eval',script,'-extra',ctl,term],
                           stderr=err,env=os.environ|{'ERL_CRASH_DUMP':str(out/f'{first:03d}.crash.dump')})
    assert run.returncode==0,first
    items=term.read_bytes().splitlines(keepends=True)
    assert len(items)==len(batch)+1 and all(item.rstrip().endswith(b',true}.') for item in items[:-1]),first
    found=index(items[:-1])
    assert list(found)==batch,first
    proof.update(found)
    batches.append({'First':first,'Count':len(batch),'Sha256':sha(term)})
assert proof.keys()==expected.keys()
(stage/'final-label-census.terms').write_bytes(b''.join(proof[k] for k in expected)+raw[-1])
receipt={'Count':len(proof),'Fresh':len(changed),'ReusedByExactSha256':len(same),'Seconds':time.monotonic()-began,
         'DonorReportSha256':sha(donor/'final-label-census.terms'),'EvaluatorSha256':sha(stage/'final-label-census.eval'),
         'ExaminedImageHashes':hashes,'Batches':batches}
(out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(f'{len(proof)} canonical with {len(changed)} freshly disassembled, {len(same)} byte-identical reused')




