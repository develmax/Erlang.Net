using System.Reflection;

try
{
    Assembly tests = Assembly.Load(FocusNames.Assembly);
    foreach (string name in FocusNames.Groups)
    {
        Type group = tests.GetType(name, true)!;
        string method = name == FocusNames.ResourceGroup ? FocusNames.Resources : FocusNames.Run;
        MethodInfo run = group.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ?? throw new MissingMethodException(name, method);
        await (Task)run.Invoke(null, null)!;
        Console.WriteLine(name);
    }
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}
