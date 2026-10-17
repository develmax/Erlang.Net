#include <stdatomic.h>
#include <stdint.h>
#include <stddef.h>
typedef uintptr_t (*allocator_fn)(void);
static _Atomic(allocator_fn) published_allocator;
int probe_register_allocator(allocator_fn allocator)
{
    allocator_fn expected = NULL;
    if (!allocator) return 1;
    return atomic_compare_exchange_strong_explicit(&published_allocator,&expected,allocator,memory_order_release,memory_order_acquire) ? 0 : 2;
}
uintptr_t probe_allocate(void)
{
    allocator_fn allocator=atomic_load_explicit(&published_allocator,memory_order_acquire);
    return allocator ? allocator() : 0;
}
