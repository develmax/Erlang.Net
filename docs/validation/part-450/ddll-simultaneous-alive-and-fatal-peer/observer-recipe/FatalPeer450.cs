using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

internal static class FatalPeer450
{
 internal static void Terminate(Process root,string receipt)
 {
  var snapshot=Snapshot();uint rootId=checked((uint)root.Id);if(!snapshot.ContainsKey(rootId)||root.HasExited)throw new InvalidOperationException("Owned peer already exited before scenario kill");
  var ids=new HashSet<uint>{rootId};bool changed=true;
  while(changed){changed=false;foreach(var item in snapshot.Values)if(ids.Contains(item.ParentProcessId)&&ids.Add(item.ProcessId))changed=true;}
  var owned=new List<(Process Process,string Executable,DateTime Started)>();
  try
  {
   foreach(uint id in ids){var p=Process.GetProcessById(checked((int)id));owned.Add((p,snapshot[id].Executable,p.StartTime.ToUniversalTime()));}
   root.Kill(entireProcessTree:true);var exits=new List<object>();bool passed=true;
   foreach(var item in owned)
   {
    bool settled=item.Process.WaitForExit(10000)&&item.Process.HasExited;passed&=settled;exits.Add(new{Pid=item.Process.Id,Executable=item.Executable,StartedUtc=item.Started,Settled=settled});
   }
   File.WriteAllText(receipt,JsonSerializer.Serialize(new{Passed=passed,OldPid=root.Id,KillRequested=true,EntireOwnedTree=true,Family=exits,Operation="deliberate scenario kill, not cleanup fallback"}));
   if(!passed)throw new IOException("Owned peer family did not settle after scenario kill");
  }
  finally{foreach(var item in owned)item.Process.Dispose();}
 }

 private static Dictionary<uint,FatalProcessEntry450> Snapshot()
 {
  using SafeFileHandle handle=CreateToolhelp32Snapshot(2,0);if(handle.IsInvalid)throw new Win32Exception(Marshal.GetLastPInvokeError());
  var entry=new FatalProcessEntry450{Size=checked((uint)Marshal.SizeOf<FatalProcessEntry450>()),Executable=string.Empty};
  if(!Process32First(handle,ref entry))throw new Win32Exception(Marshal.GetLastPInvokeError());
  var result=new Dictionary<uint,FatalProcessEntry450>();do{result.Add(entry.ProcessId,entry);}while(Process32Next(handle,ref entry));
  if(Marshal.GetLastPInvokeError()!=18)throw new Win32Exception(Marshal.GetLastPInvokeError());return result;
 }

 [DllImport("kernel32.dll",SetLastError=true)]private static extern SafeFileHandle CreateToolhelp32Snapshot(uint flags,uint processId);
 [DllImport("kernel32.dll",EntryPoint="Process32FirstW",ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
 [return:MarshalAs(UnmanagedType.Bool)]private static extern bool Process32First(SafeFileHandle snapshot,ref FatalProcessEntry450 entry);
 [DllImport("kernel32.dll",EntryPoint="Process32NextW",ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
 [return:MarshalAs(UnmanagedType.Bool)]private static extern bool Process32Next(SafeFileHandle snapshot,ref FatalProcessEntry450 entry);
}
