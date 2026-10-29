#include <atomic>
#include <cstdlib>
#include <pthread.h>

#define PROBE_EXPORT extern "C" __attribute__((visibility("default")))

struct ProbeState {
    pthread_key_t key;
    std::atomic<int> cpp_created{0};
    std::atomic<int> cpp_destroyed{0};
    std::atomic<int> key_destroyed{0};
    std::atomic<int> worker_returned{0};
    int setspecific_status = -1;
    bool explicit_exit = false;
};

struct ThreadWitness {
    ProbeState *owner = nullptr;
    ~ThreadWitness() noexcept
    {
        if (owner != nullptr) owner->cpp_destroyed.fetch_add(1, std::memory_order_relaxed);
    }
};

static thread_local ThreadWitness cpp_witness;

static void key_destructor(void *value)
{
    auto *owner = static_cast<ProbeState *>(value);
    owner->key_destroyed.fetch_add(1, std::memory_order_relaxed);
}

static void *worker(void *value)
{
    auto *owner = static_cast<ProbeState *>(value);
    cpp_witness.owner = owner;
    owner->cpp_created.fetch_add(1, std::memory_order_relaxed);
    owner->setspecific_status = pthread_setspecific(owner->key, owner);
    owner->worker_returned.fetch_add(1, std::memory_order_relaxed);
    if (owner->explicit_exit) pthread_exit(nullptr);
    return nullptr;
}

static int run_probe(int values[9], bool explicit_exit)
{
    for (unsigned index = 0; index < 9; ++index) values[index] = -1;
    ProbeState state;
    state.explicit_exit = explicit_exit;
    values[0] = pthread_key_create(&state.key, key_destructor);
    if (values[0] != 0) return -10;
    pthread_t thread;
    values[1] = pthread_create(&thread, nullptr, worker, &state);
    if (values[1] != 0) {
        values[4] = pthread_key_delete(state.key);
        return -20;
    }
    values[2] = pthread_join(thread, nullptr);
    // A failed join cannot establish that the worker stopped using stack state.
    // Terminate this owned test child rather than return with a dangling owner.
    if (values[2] != 0) std::abort();
    values[3] = state.setspecific_status;
    values[4] = pthread_key_delete(state.key);
    values[5] = state.cpp_created.load(std::memory_order_relaxed);
    values[6] = state.cpp_destroyed.load(std::memory_order_relaxed);
    values[7] = state.key_destroyed.load(std::memory_order_relaxed);
    values[8] = state.worker_returned.load(std::memory_order_relaxed);
    for (unsigned index = 0; index < 5; ++index) if (values[index] != 0) return -30;
    for (unsigned index = 5; index < 9; ++index) if (values[index] < 0 || values[index] > 1) return -40;
    return 1000 * values[5] + 100 * values[6] + 10 * values[7] + values[8];
}

PROBE_EXPORT int ordinary_dependency_value(void)
{
    int values[9];
    int returned = run_probe(values, false);
    if (returned != 1111) return returned;
    return run_probe(values, true);
}
