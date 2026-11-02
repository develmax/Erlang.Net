using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class VmConstantEntry357 : IDisposable
{
    private readonly ModuleLiteralDomain800 domain;
    internal ModuleLiteralRoots801 Roots { get; }
    internal Term Value { get; }
    private int disposed;

    private VmConstantEntry357(ModuleLiteralDomain800 domain, ModuleLiteralRoots801 roots, Term value)
    {
        this.domain = domain;
        Roots = roots;
        Value = value;
    }

    internal static VmConstantEntry357 Create(VmConstantPool357 pool, Term template)
    {
        ProcessRuntime runtime = pool.Runtime357;
        ModuleLiteralDomain800 domain = ModuleLiteralDomain800.Prepare800(runtime.Modules, runtime, [template]);
        ModuleLiteralLease800? lease = null;
        ModuleLiteralRoots801? roots = null;
        try
        {
            domain.BindVmConstant357(pool);
            lease = domain.AcquireForVmConstant357(pool);
            roots = ModuleLiteralRoots801.TakeRegisteredLease357(runtime, lease);
            lease = null;
            runtime.MarkPreparedLiterals425(domain.Literals800);

            return new(domain, roots, domain.Literals800[0]);
        }
        catch (Exception primary)
        {
            Exception? rootsFailure = null, leaseFailure = null, ownerFailure = null;
            try
            {
                roots?.Dispose();
            }
            catch (Exception error)
            {
                rootsFailure = error;
            }
            try
            {
                lease?.Dispose();
            }
            catch (Exception error)
            {
                leaseFailure = error;
            }
            try
            {
                _ = domain.CloseCodeOwner800();
            }
            catch (Exception error)
            {
                ownerFailure = error;
            }
            if (rootsFailure is not null || leaseFailure is not null || ownerFailure is not null)
                throw new AggregateException(new[] { primary, rootsFailure, leaseFailure, ownerFailure }.OfType<Exception>());
            ExceptionDispatchInfo.Capture(primary).Throw();
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Exception? rootsFailure = null, ownerFailure = null;
        try
        {
            Roots.Dispose();
        }
        catch (Exception error)
        {
            rootsFailure = error;
        }
        try
        {
            _ = domain.CloseCodeOwner800();
        }
        catch (Exception error)
        {
            ownerFailure = error;
        }
        if (rootsFailure is not null && ownerFailure is not null)
            throw new AggregateException(rootsFailure, ownerFailure);
        if (rootsFailure is not null) ExceptionDispatchInfo.Capture(rootsFailure).Throw();
        if (ownerFailure is not null) ExceptionDispatchInfo.Capture(ownerFailure).Throw();
    }
}
