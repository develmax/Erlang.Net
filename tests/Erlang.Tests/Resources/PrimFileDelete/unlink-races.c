/* Independent host leaf oracle: OTP-29.1.1 efile_del_file calls unlink.
 * No managed bindings or ERTS dependency participate in these races. */
#define _POSIX_C_SOURCE 200809L
#include <pthread.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

enum { rounds = 2048 };
struct race {
    pthread_mutex_t mutex;
    pthread_cond_t condition;
    const char *path;
    int ready;
    int go;
    int results[2];
};
struct argument { struct race *state; int index; };

static void *remove_file(void *pointer) {
    struct argument *argument = pointer;
    struct race *state = argument->state;
    pthread_mutex_lock(&state->mutex);
    state->ready++;
    pthread_cond_broadcast(&state->condition);
    while (!state->go) pthread_cond_wait(&state->condition, &state->mutex);
    pthread_mutex_unlock(&state->mutex);
    int result = unlink(state->path);
    state->results[argument->index] = result == 0 ? 0 : errno;
    return NULL;
}

int main(int argc, char **argv) {
    if (argc != 2) return 2;
    size_t size = strlen(argv[1]) + sizeof("/native-race-2147483647");
    char *path = malloc(size);
    if (!path) return 2;
    snprintf(path, size, "%s/native-sequential", argv[1]);
    int initial = open(path, O_WRONLY | O_CREAT | O_EXCL, 0600);
    if (initial < 0) return 3;
    close(initial);
    if (unlink(path) != 0 || unlink(path) != -1 || errno != ENOENT) return 7;
    int single = 0;
    int both = 0;
    for (int round = 0; round < rounds; round++) {
        snprintf(path, size, "%s/native-race-%d", argv[1], round);
        int fd = open(path, O_WRONLY | O_CREAT | O_EXCL, 0600);
        if (fd < 0) return 3;
        if (write(fd, "\1", 1) != 1) return 3;
        close(fd);
        struct race state = {
            .mutex = PTHREAD_MUTEX_INITIALIZER,
            .condition = PTHREAD_COND_INITIALIZER,
            .path = path
        };
        struct argument arguments[2] = { { &state, 0 }, { &state, 1 } };
        pthread_t threads[2];
        if (pthread_create(&threads[0], NULL, remove_file, &arguments[0]) != 0) return 4;
        if (pthread_create(&threads[1], NULL, remove_file, &arguments[1]) != 0) return 4;
        pthread_mutex_lock(&state.mutex);
        while (state.ready != 2) pthread_cond_wait(&state.condition, &state.mutex);
        state.go = 1;
        pthread_cond_broadcast(&state.condition);
        pthread_mutex_unlock(&state.mutex);
        pthread_join(threads[0], NULL);
        pthread_join(threads[1], NULL);
        pthread_cond_destroy(&state.condition);
        pthread_mutex_destroy(&state.mutex);
        int left = state.results[0];
        int right = state.results[1];
        if (left == 0 && right == 0) both++;
        else if ((left == 0 && right == ENOENT) || (right == 0 && left == ENOENT)) single++;
        else return 5;
        if (access(path, F_OK) == 0 || errno != ENOENT) return 6;
    }
    printf("{\"Rounds\":%d,\"OneSuccess\":%d,\"BothSuccess\":%d,\"SequentialVerified\":true}\n", rounds, single, both);
    free(path);
    return 0;
}
