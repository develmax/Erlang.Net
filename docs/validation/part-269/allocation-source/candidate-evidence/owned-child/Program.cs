using System.Diagnostics;
using System.Reflection;
if(args[0]=="raw") {await Console.OpenStandardOutput().WriteAsync(new byte[]{0,255,13,10});await Console.OpenStandardError().WriteAsync(new byte[]{128,0,10});return 3;}
if(args[0]=="fast") return 0;
if(args[0]=="descendant") {await Task.Delay(30000);return 0;}
var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);start.ArgumentList.Add("descendant");using var child=Process.Start(start)!;await File.WriteAllTextAsync(args[1],child.Id.ToString());await Task.Delay(30000);return 0;
