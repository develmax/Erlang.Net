from pathlib import Path
import subprocess,hashlib,json
repo=Path.cwd();otp=repo.parent/'otp';out=repo/'artifacts/library404-io-printable-command-contract-review2';pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
def git(*a):return subprocess.check_output(['git',*a],cwd=otp)
assert git('cat-file','-t',pin).strip()==b'commit'
resolved=git('rev-parse','--verify',pin).decode().strip();assert resolved==pin
paths=['erts/emulator/beam/erl_unicode.c','erts/emulator/sys/common/erl_sys_common_misc.c','erts/emulator/beam/erl_init.c','lib/stdlib/src/io.erl']
rows=[]
for p in paths:
 data=git('show',pin+':'+p);blob=git('rev-parse',pin+':'+p).decode().strip();actual=subprocess.check_output(['git','hash-object','--stdin'],cwd=otp,input=data).decode().strip();assert actual==blob
 f=out/'authority'/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(data)
 live=(otp/p).read_bytes();rows.append(dict(Pin=pin,GitObject=pin+':'+p,BlobSha1=blob,Sha256=hashlib.sha256(data).hexdigest().upper(),Bytes=len(data),Stored=f.relative_to(out).as_posix(),LiveCheckoutSha256=hashlib.sha256(live).hexdigest().upper(),LiveCheckoutByteEqual=live==data,Authority='git show explicit pinned commit; live checkout is comparison only'))
(out/'pinned-authority.json').write_text(json.dumps(dict(Commit=pin,VerifiedObjectType='commit',Files=rows),indent=2)+'\n',encoding='utf-8')
search={}
for pattern in ['erts_set_printable_characters','erts_get_printable_characters','printable_range']:
 scopes=['erts'] if pattern!='printable_range' else ['erts/emulator/beam/bif.tab','lib/stdlib/src/io.erl']
 search[pattern]=git('grep','-n',pattern,pin,'--',*scopes).decode()
(out/'pinned-references.json').write_text(json.dumps(search,indent=2)+'\n',encoding='utf-8')
# Full selected function/default/startup option/documentation sections, numbered against pinned bytes.
ex=[]
def section(p,start,end,title):
 lines=(out/'authority'/p).read_text(encoding='utf-8').splitlines();ex.append('\n## '+title+'\n'+pin+':'+p+'\n'+''.join(str(i+1)+': '+lines[i]+'\n' for i in range(start-1,end)))
p=paths[0];lines=(out/'authority'/p).read_text().splitlines();i=next(i for i,s in enumerate(lines) if 'BIF_RETTYPE io_printable_range_0' in s);j=next(j for j in range(i+1,len(lines)) if lines[j]=='}');section(p,i+1,j+1,'Complete native0arity getter')
p=paths[1];lines=(out/'authority'/p).read_text().splitlines();i=next(i for i,s in enumerate(lines) if 'static int printable_character_set' in s);section(p,i+1,i+1,'Actual default');i=next(i for i,s in enumerate(lines) if 'void erts_set_printable_characters' in s);j=next(j for j in range(i+1,len(lines)) if lines[j]=='}');k=next(k for k in range(j+1,len(lines)) if 'int erts_get_printable_characters' in lines[k]);z=next(z for z in range(k+1,len(lines)) if lines[z]=='}');section(p,i+1,z+1,'Complete internal setter/getter')
p=paths[2];lines=(out/'authority'/p).read_text().splitlines();i=next(i for i,s in enumerate(lines) if 'if (!sys_strncmp(argv[i],"-pc",3))' in s);j=next(j for j in range(i+1,len(lines)) if 'erts_set_printable_characters(printable_chars)' in lines[j]);section(p,i,j+2,'Complete startup valid-range mapping and rejection')
p=paths[3];lines=(out/'authority'/p).read_text().splitlines();i=next(i for i,s in enumerate(lines) if 'Returns the user-requested range of printable Unicode characters.' in s);j=next(j for j in range(i,len(lines)) if lines[j].startswith('printable_range() ->'));section(p,i+1,j+2,'Exposed source contract/options and actual stub')
(out/'pinned-numbered-excerpts.md').write_text('# Exact pinned printable command authority\n'+''.join(ex),encoding='utf-8')
print(json.dumps(rows,indent=2))

