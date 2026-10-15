"""Independent narrow ETF decoder for immutable pressure observations; no evaluation.
Keeps atoms, PIDs, refs, binary data and improper lists as distinct typed values.
"""
import struct
class Atom(str): pass
class Reader:
 def __init__(self,data):self.data=data;self.pos=0
 def take(self,n):
  assert n>=0 and self.pos+n<=len(self.data),'Truncated ETF'
  value=self.data[self.pos:self.pos+n];self.pos+=n;return value
 def uint(self,n):return int.from_bytes(self.take(n),'big')
 def term(self,depth=0):
  assert depth<256,'ETF depth exceeded'
  tag=self.uint(1);sub=lambda:self.term(depth+1)
  if tag==97:return self.uint(1)
  if tag==98:return int.from_bytes(self.take(4),'big',signed=True)
  if tag==70:return struct.unpack('>d',self.take(8))[0]
  if tag in [100,115,118,119]:
   n=self.uint(1 if tag in [115,119] else 2)
   return Atom(self.take(n).decode('utf-8' if tag in [118,119] else 'latin1'))
  if tag in [104,105]:return tuple(sub() for _ in range(self.uint(1 if tag==104 else 4)))
  if tag==106:return []
  if tag==107:return list(self.take(self.uint(2)))
  if tag==108:
   values=[sub() for _ in range(self.uint(4))];tail=sub()
   return values if tail==[] else ('IMPROPER_LIST',tuple(values),tail)
  if tag==109:return self.take(self.uint(4))
  if tag==77:
   n=self.uint(4);bits=self.uint(1);raw=self.take(n);return ('BIT_BINARY',bits,raw)
  if tag in [110,111]:
   n=self.uint(1 if tag==110 else 4);sign=self.uint(1);assert sign in [0,1]
   return int.from_bytes(self.take(n),'little')*(-1 if sign else 1)
  if tag in [103,88]:
   node=sub();number=self.uint(4);serial=self.uint(4);creation=self.uint(1 if tag==103 else 4)
   return ('PID',node,number,serial,creation)
  if tag in [114,90]:
   count=self.uint(2);node=sub();creation=self.uint(1 if tag==114 else 4)
   return ('REF',node,creation,tuple(self.uint(4) for _ in range(count)))
  if tag==101:
   node=sub();number=self.uint(4);creation=self.uint(1);return ('REF',node,creation,(number,))
  if tag in [102,89,120]:
   node=sub();number=self.uint(8 if tag==120 else 4);creation=self.uint(1 if tag==102 else 4)
   return ('PORT',node,number,creation)
  if tag==116:return ('MAP',tuple((sub(),sub()) for _ in range(self.uint(4))))
  raise AssertionError('Unsupported ETF tag '+str(tag)+' at '+str(self.pos-1))
def decode(data):
 reader=Reader(data);assert reader.uint(1)==131;value=reader.term();assert reader.pos==len(data),'Trailing ETF bytes';return value
def decode_prefix(data):
 reader=Reader(data);assert reader.uint(1)==131;value=reader.term();return value,reader.pos
def flat_iolist(value):
 if isinstance(value,bytes):return value
 if isinstance(value,int):assert 0<=value<=255;return bytes([value])
 assert isinstance(value,list),'Expected proper iolist';return b''.join(flat_iolist(x) for x in value)
