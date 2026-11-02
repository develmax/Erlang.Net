using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal static class CountersInitializer356Access
{
    internal static async Task KillAndAssert(ProcessRuntime runtime, Action inspectUnderGate)
    {
        object gate = typeof(ProcessRuntime).GetField(CountersHost421Names.RuntimeGate, CountersHost421Names.Members)!.GetValue(runtime)!;
        CountersHost421Access.Require(!System.Threading.Monitor.IsEntered(gate));
        var processes = (Dictionary<Pid, ProcessContext>)typeof(ProcessRuntime)
            .GetField(CountersInitializer356Names.Processes, CountersHost421Names.Members)!.GetValue(runtime)!;
        MethodInfo prepare = typeof(ProcessRuntime).GetMethods(CountersHost421Names.Members).Single(method =>
            method.Name == CountersInitializer356Names.Prepare && method.GetParameters().Length == CountersInitializer356Names.PrepareArguments);
        MethodInfo terminate = typeof(ProcessRuntime).GetMethods(CountersHost421Names.Members).Single(method =>
            method.Name == CountersInitializer356Names.Terminate && method.GetParameters().Length == CountersInitializer356Names.TerminateArguments);
        while (true)
        {
            ProcessContext victim;
            lock (gate)
            {
                CountersHost421Access.Require(processes.TryGetValue(runtime.InitializationPid, out victim!));
            }
            object pending = Invoke(prepare, runtime, [victim, Term.A(CountersInitializer356Names.Killed), null, null])!;
            Task task = (Task)pending.GetType().GetMethod(CountersInitializer356Names.AsTask)!.Invoke(pending, null)!;
            await task;
            object? plan = task.GetType().GetProperty(CountersInitializer356Names.Result)!.GetValue(task);
            Exception? primary = null;
            try
            {
                CountersHost421Access.Require(plan is not null);
                lock (gate)
                {
                    CountersHost421Access.Require(processes.TryGetValue(runtime.InitializationPid, out ProcessContext? current)
                        && ReferenceEquals(victim, current));
                    if (!(bool)Invoke(plan!.GetType().GetMethod(CountersInitializer356Names.Current, CountersHost421Names.Members)!, plan, [])!)
                        continue;
                    Invoke(terminate, runtime, [victim, Term.A(CountersInitializer356Names.Killed), null, null, plan]);
                    inspectUnderGate();

                    return;
                }
            }
            catch (Exception error)
            {
                primary = error;
                throw;
            }
            finally
            {
                try
                {
                    if (plan is IAsyncDisposable owner) await owner.DisposeAsync();
                }
                catch (Exception cleanup) when (primary is not null)
                {
                    throw new AggregateException(primary, cleanup);
                }
            }
        }
    }

    private static object? Invoke(MethodInfo method, object owner, object?[] arguments)
    {
        try
        {
            return method.Invoke(owner, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
