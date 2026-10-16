import struct
class Reader:
 def __init__(self,b):self.b=b;self.i=0
 def take(self,n):
  if n<0 or self.i+n>len(self.b):raise ValueError('truncated')
  v=self.b[self.i:self.i+n];self.i+=n;return v
 def n(self,k):return int.from_bytes(self.take(k),'big')
 def term(self):
  t=self.n(1)
  if t==97:return self.n(1)
  if t==98:return int.from_bytes(self.take(4),'big',signed=True)
  if t in (100,118,115,119):return ('atom',self.take(self.n(2 if t in (100,118) else 1)).decode('utf-8' if t in (118,119) else 'latin1'))
  if t in (104,105):return tuple(self.term() for _ in range(self.n(1 if t==104 else 4)))
  if t==106:return ('nil',)
  if t==107:return ('list',tuple(self.take(self.n(2))),('nil',))
  if t==108:
   items=tuple(self.term() for _ in range(self.n(4)));return ('list',items,self.term())
  if t==109:return ('binary',self.take(self.n(4)).hex())
  if t==77:
   n=self.n(4);bits=self.n(1);return ('bits',self.take(n).hex(),bits)
  if t in (88,103):return ('pid',self.term(),self.n(4),self.n(4),self.n(4 if t==88 else 1))
  if t in (90,114):
   n=self.n(2);node=self.term();creation=self.n(4 if t==90 else 1);return ('ref',node,creation,tuple(self.n(4) for _ in range(n)))
  if t==101:return ('ref',self.term(),self.n(4),self.n(1))
  if t in (110,111):
   n=self.n(1 if t==110 else 4);sign=self.n(1);v=int.from_bytes(self.take(n),'little');return -v if sign else v
  if t==70:return ('float',struct.unpack('>d',self.take(8))[0])
  if t==116:return ('map',tuple((self.term(),self.term()) for _ in range(self.n(4))))
  raise ValueError(('unsupported ETF',t,self.i-1))
 def external(self):
  if self.n(1)!=131:raise ValueError('version')
  return self.term()
def decode(b):
 r=Reader(b);v=r.external()
 if r.i!=len(b):raise ValueError('trailing')
 return v
def packet(b):
 if b[0]!=112:raise ValueError('pass through marker')
 r=Reader(b[1:]);v=r.external();payload=r.external() if r.i<len(r.b) else ('atom','no_payload')
 if r.i!=len(r.b):raise ValueError('packet trailing')
 return (v,payload)
