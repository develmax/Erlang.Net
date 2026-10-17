#include <pthread.h>
#include <stdint.h>
#include <stdatomic.h>
#include <stdlib.h>
#define CONTROL_EXPORT __attribute__((visibility("default")))
#define CONTROL_COUNT 6
static _Atomic int state;
static uintptr_t providers[CONTROL_COUNT];
static _Atomic unsigned long calls[CONTROL_COUNT];
CONTROL_EXPORT int nif_bridge_pthread_register(const uintptr_t *values, unsigned count)
{
    int expected = 0;
    if (values == NULL || count != CONTROL_COUNT) return -1;
    for (unsigned i=0;i<CONTROL_COUNT;i++) if (values[i] == 0) return -1;
    if (!atomic_compare_exchange_strong(&state,&expected,1)) return -1;
    for (unsigned i=0;i<CONTROL_COUNT;i++) providers[i]=values[i];
    atomic_store_explicit(&state,2,memory_order_release);
    return 0;
}
static uintptr_t provider(unsigned index)
{
    if (atomic_load_explicit(&state,memory_order_acquire) != 2 || index>=CONTROL_COUNT) abort();
    return providers[index];
}
CONTROL_EXPORT uintptr_t nif_bridge_pthread_provider(unsigned index) { return provider(index); }
CONTROL_EXPORT unsigned long nif_bridge_pthread_calls(unsigned index)
{
    if (index>=CONTROL_COUNT) abort();
    return atomic_load(&calls[index]);
}
CONTROL_EXPORT int pthread_create(pthread_t *thread,const pthread_attr_t *attr,void *(*entry)(void *),void *argument)
{
    typedef int (*function)(pthread_t *,const pthread_attr_t *,void *(*)(void *),void *);
    atomic_fetch_add(&calls[0],1);return ((function)provider(0))(thread,attr,entry,argument);
}
CONTROL_EXPORT int pthread_join(pthread_t thread,void **value)
{
    typedef int (*function)(pthread_t,void **);
    atomic_fetch_add(&calls[1],1);return ((function)provider(1))(thread,value);
}
CONTROL_EXPORT int pthread_key_create(pthread_key_t *key,void (*destructor)(void *))
{
    typedef int (*function)(pthread_key_t *,void (*)(void *));
    atomic_fetch_add(&calls[2],1);return ((function)provider(2))(key,destructor);
}
CONTROL_EXPORT int pthread_key_delete(pthread_key_t key)
{
    typedef int (*function)(pthread_key_t);
    atomic_fetch_add(&calls[3],1);return ((function)provider(3))(key);
}
CONTROL_EXPORT void *pthread_getspecific(pthread_key_t key)
{
    typedef void *(*function)(pthread_key_t);
    atomic_fetch_add(&calls[4],1);return ((function)provider(4))(key);
}
CONTROL_EXPORT int pthread_setspecific(pthread_key_t key,const void *value)
{
    typedef int (*function)(pthread_key_t,const void *);
    atomic_fetch_add(&calls[5],1);return ((function)provider(5))(key,value);
}
