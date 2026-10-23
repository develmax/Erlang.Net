from pathlib import Path
source=Path('artifacts/library447-probe4/run-root.py').read_text()
source=source.replace('import argparse,hashlib,importlib.util,json,os,shutil,subprocess,time,xml.etree.ElementTree as ET','import argparse,hashlib,importlib.util,json,os,shutil,socket,struct,subprocess,time,xml.etree.ElementTree as ET')
source=source.replace('library447-driver-package1','library448-driver-package1').replace('ddll_tcp447','ddll_tcp448')
source=source.replace('scans=[];failures=[];groups=[]','scans=[];failures=[];groups=[];epmd=None;epmd_logs=[];epmd_port=None')
source=source.replace("save(out/'inputs-before.json',inputs)",'''save(out/'inputs-before.json',inputs)
 with socket.socket() as reservation:reservation.bind(('127.0.0.1',0));epmd_port=reservation.getsockname()[1]
 env['ERL_EPMD_PORT']=str(epmd_port)
 epmd_logs=[(out/'epmd.stdout.log').open('xb'),(out/'epmd.stderr.log').open('xb')]
 epmd_command=[str(installed/'bin/epmd.exe'),'-port',str(epmd_port),'-address','127.0.0.1']
 epmd=subprocess.Popen(epmd_command,cwd=r,env=env,stdout=epmd_logs[0],stderr=epmd_logs[1]);save(out/'epmd.live.json',{'Pid':epmd.pid,'Command':epmd_command})
 deadline=time.monotonic()+10;epmd_ready=False
 while epmd.poll() is None and time.monotonic()<deadline:
  try:
   with socket.create_connection(('127.0.0.1',epmd_port),timeout=.2):epmd_ready=True;break
  except OSError:time.sleep(.05)
 assert epmd_ready,'Private foreground EPMD failed to start'
 def read_exact(stream,n):
  data=b''
  while len(data)<n:
   part=stream.recv(n-len(data));assert part,'EPMD truncated response';data+=part
  return data
 def port_please(name,destination):
  request=b'z'+name.split('@')[0].encode('latin1')
  with socket.create_connection(('127.0.0.1',epmd_port),timeout=5) as stream:
   stream.sendall(struct.pack('>H',len(request))+request);head=read_exact(stream,12)
   assert head[0:2]==bytes([119,0]) and head[5]==0 and struct.unpack('>H',head[6:8])[0]>=6 and struct.unpack('>H',head[8:10])[0]<=6
   tail=read_exact(stream,struct.unpack('>H',head[10:12])[0]+2);extra=read_exact(stream,struct.unpack('>H',tail[-2:])[0]);destination.write_bytes(head+tail+extra)
   return struct.unpack('>H',head[2:4])[0]
''')
source=source.replace("ports=[]\n   for role", "ports=[];nodes={}\n   for role")
old="command=[str(installed/'bin/erl.exe'),'-noshell','-noinput','-boot','no_dot_erlang','-pa',str(ebin),'-eval',expr]"
new="nodes[role]='ddll448'+('oracle' if role=='primary' else 'peer')+str(depth)+'@127.0.0.1'\n    command=[str(installed/'bin/erl.exe'),'-noshell','-noinput','-boot','no_dot_erlang','-start_epmd','false','-name',nodes[role],'-setcookie','ddll448_owned_fixture_cookie','-kernel','inet_dist_use_interface','{127,0,0,1}','-pa',str(ebin),'-eval',expr]"
assert source.count(old)==1;source=source.replace(old,new)
source=source.replace("ports.append(ready.read_text())","ports.append(ready.read_text())\n   dist_port=port_please(nodes['peer'],group/'peer-epmd-port.bin')")
source=source.replace("*ports,str(depth)]","*ports,str(depth),str(dist_port),nodes['peer'],nodes['primary'],'ddll448managed'+str(depth)+'@127.0.0.1']")
old="finally:\n try:scan('after')"
new="""finally:
 if epmd is not None:
  forced=False
  try:
   owned.owned([str(installed/'bin/epmd.exe'),'-port',str(epmd_port),'-kill'],out,'epmd-stop',15,env,str(r));epmd.wait(timeout=10)
  except Exception as error:
   failures.append('Private EPMD cleanup: '+repr(error))
   if epmd.poll() is None:
    forced=True;epmd.terminate();epmd.wait(timeout=10)
  finally:
   for log in epmd_logs:log.close()
   save(out/'epmd.exit.json',{'Pid':epmd.pid,'ExitCode':epmd.returncode,'ForcedTermination':forced,'Settled':epmd.poll() is not None})
 try:scan('after')"""
assert source.count(old)==1;source=source.replace(old,new)
source=source.replace("'two real OTP OS processes and actual managed runtime over packet4 TCP ETF; no distribution handshake claim'","'two real distributed OTP OS nodes, authenticated Erlang distribution v6, physical TCP disconnect/reconnect and actual runtime data queues'")
out=Path('artifacts/library448-probe1/run-root.py');assert not out.exists();out.write_text(source)
