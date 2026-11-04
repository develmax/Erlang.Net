#pragma once
#include <stdint.h>
enum { ENFS_READ=1, ENFS_WRITE=2, ENFS_APPEND=4, ENFS_EXCLUSIVE=8, ENFS_SYNC=16 };
enum { ENFS_RENAME=1, ENFS_MKDIR=2, ENFS_RMDIR=3 };
enum { ENFS_DIRECTORY=1, ENFS_REGULAR=2, ENFS_SYMLINK=3, ENFS_DEVICE=4, ENFS_OTHER=5 };
typedef struct {
    int64_t type, access, size, atime, mtime, ctime, mode, links, dev, rdev, inode, uid, gid;
} enfs_info;
