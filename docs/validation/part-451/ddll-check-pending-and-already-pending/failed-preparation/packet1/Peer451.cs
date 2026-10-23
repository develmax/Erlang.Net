using System.Buffers.Binary;
using System.Net.Sockets;
using Erlang;
sealed class Peer451:IDisposable
{
 readonly TcpClient client=new();readonly NetworkStream stream;
 public Peer451(int port){client.Connect("127.0.0.1",port);stream=client.GetStream();stream.ReadTimeout=15000;stream.WriteTimeout=15000;}
 Term Exchange(Term value)
 {
  byte[] payload=ExternalTermFormat.Encode(value),size=new byte[4];BinaryPrimitives.WriteInt32BigEndian(size,payload.Length);stream.Write(size);stream.Write(payload);stream.ReadExactly(size);int n=BinaryPrimitives.ReadInt32BigEndian(size);if(n<1||n>1048576)throw new Exception("Invalid packet size");byte[] body=new byte[n];stream.ReadExactly(body);return ExternalTermFormat.DecodeOne(body,new()).Term;
 }
 public Term Call(string f,params Term[] a)
 {
  Term value=Exchange(Term.Tuple(Term.A(f),Term.List(a)));
  if(value is TupleTerm{Items.Count:2} t&&t.Items[0].Equals(Term.A("ok")))return t.Items[1];
  throw new Exception("Original worker failed: "+value);
 }
 public void Stop(){if(!Exchange(Term.A("stop")).Equals(Term.A("stopped")))throw new Exception("No shutdown acknowledgement");}
 public void Dispose(){stream.Dispose();client.Dispose();}
}
