using System.Runtime.InteropServices;

namespace Erlang;

// Exact Win64 typedef widths from pinned erl_win_dyn_driver.h.
// These are ABI types only; no callback operation is fabricated here.
internal static class ErlDriverCallbackTypes420
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int null_func();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_failure_atom(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_failure_posix(nint argument0, int argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_failure(nint argument0, int argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_exit(nint argument0, int argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_failure_eof(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_busy_msgq_limits(nint argument0, nint argument1, nint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_select(
        nint argument0,
        nint argument1,
        int argument2,
        int argument3
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_output(nint argument0, nint argument1, nuint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_output2(
        nint argument0,
        nint argument1,
        nuint argument2,
        nint argument3,
        nuint argument4
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_output_binary(
        nint argument0,
        nint argument1,
        nuint argument2,
        nint argument3,
        nuint argument4,
        nuint argument5
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_outputv(
        nint argument0,
        nint argument1,
        nuint argument2,
        nint argument3,
        nuint argument4
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_vec_to_buf(nint argument0, nint argument1, nuint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_set_timer(nint argument0, uint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_cancel_timer(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_read_timer(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_consume_timeslice(nint argument0, int argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint erl_errno_id(int argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void set_busy_port(nint argument0, int argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void set_port_control_flags(nint argument0, int argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int get_port_flags(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_alloc_binary(nuint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_realloc_binary(nint argument0, nuint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void driver_free_binary(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_alloc(nuint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_realloc(nint argument0, nuint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void driver_free(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_enq(nint argument0, nint argument1, nuint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_pushq(nint argument0, nint argument1, nuint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_deq(nint argument0, nuint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_sizeq(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_enq_bin(
        nint argument0,
        nint argument1,
        nuint argument2,
        nuint argument3
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_pushq_bin(
        nint argument0,
        nint argument1,
        nuint argument2,
        nuint argument3
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_peekqv(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_peekq(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_enqv(nint argument0, nint argument1, nuint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_pushqv(nint argument0, nint argument1, nuint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void add_driver_entry(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int remove_driver_entry(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_mk_atom(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_mk_port(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_connected(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_caller(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_mk_term_nil();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_output_term(nuint argument0, nint argument1, int argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_output_term(nint argument0, nint argument1, int argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_send_term(
        nuint argument0,
        nuint argument1,
        nint argument2,
        int argument3
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_send_term(
        nint argument0,
        nuint argument1,
        nint argument2,
        int argument3
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint driver_async_port_key(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_async(
        nint argument0,
        nint argument1,
        nint argument2,
        nint argument3,
        nint argument4
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_lock_driver(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_dl_open(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_dl_sym(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_dl_close(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_dl_error();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_binary_get_refc(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_binary_inc_refc(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_binary_dec_refc(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_pdl_create(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void driver_pdl_lock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void driver_pdl_unlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_pdl_get_refc(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_pdl_inc_refc(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint driver_pdl_dec_refc(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void driver_system_info(nint argument0, nuint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_get_now(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate long erl_drv_monotonic_time(int argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate long erl_drv_time_offset(int argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate long erl_drv_convert_time_unit(long argument0, int argument1, int argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_monitor_process(nint argument0, nuint argument1, nint argument2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_demonitor_process(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint driver_get_monitored_process(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int driver_compare_monitors(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint erl_drv_mutex_create(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_mutex_destroy(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_mutex_trylock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_mutex_lock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_mutex_unlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint erl_drv_cond_create(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_cond_destroy(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_cond_signal(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_cond_broadcast(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_cond_wait(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint erl_drv_rwlock_create(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_rwlock_destroy(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_rwlock_tryrlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_rwlock_rlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_rwlock_runlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_rwlock_tryrwlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_rwlock_rwlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_rwlock_rwunlock(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_tsd_key_create(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_tsd_key_destroy(int argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_tsd_set(int argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint erl_drv_tsd_get(int argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint erl_drv_thread_opts_create(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_thread_opts_destroy(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_thread_create(
        nint argument0,
        nint argument1,
        nint argument2,
        nint argument3,
        nint argument4
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint erl_drv_thread_self();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_equal_tids(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void erl_drv_thread_exit(nint argument0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_thread_join(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_putenv(nint argument0, nint argument1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int erl_drv_getenv(nint argument0, nint argument1, nint argument2);

}
