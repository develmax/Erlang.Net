"""Read-only bulk Git bytes/attributes; root may integrate after independent review."""
import subprocess
MAX_BATCH_BYTES=512*1024*1024
def capture(paths,cwd,revision=None):
 paths=list(paths);assert len(paths)==len(set(paths)) and all('\n' not in x and '\r' not in x and '\0' not in x for x in paths)
 def run(args,data):
  p=subprocess.run(['git',*args],cwd=cwd,input=data,capture_output=True);assert p.returncode==0,p.stderr.decode(errors='replace');assert len(p.stdout)<=MAX_BATCH_BYTES;return p.stdout
 specs=''.join((revision or '')+':'+p+'\n' for p in paths).encode();raw=run(['cat-file','--batch'],specs);offset=0;files={}
 for path in paths:
  end=raw.index(b'\n',offset);header=raw[offset:end].split();assert len(header)==3 and header[1]==b'blob',(path,header);size=int(header[2]);start=end+1;data=raw[start:start+size];assert len(data)==size and raw[start+size:start+size+1]==b'\n';offset=start+size+1;files[path]=data
 assert offset==len(raw)
 args=['check-attr',*(['--source='+revision] if revision else ['--cached']),'-z','--stdin','text','binary'];rawattrs=run(args,b'\0'.join(p.encode() for p in paths)+b'\0').decode().split('\0');assert rawattrs.pop()=='' and len(rawattrs)==len(paths)*6;attrs={p:{} for p in paths}
 for offset in range(0,len(rawattrs),3):
  path,key,value=rawattrs[offset:offset+3];assert path in attrs and key in ['text','binary'] and key not in attrs[path];attrs[path][key]=value
 assert all(set(x)=={'text','binary'} for x in attrs.values());return files,attrs
