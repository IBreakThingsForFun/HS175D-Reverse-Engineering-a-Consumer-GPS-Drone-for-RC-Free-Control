/*
 * ttyinject.c — ttyhook + on-demand frame INJECTOR for the HS175D / Syma X30 Pro
 * flight UART (/dev/ttyS1), as an LD_PRELOAD shim inside img_trans_app.
 *
 * It does everything ttyhook.c does (logs both directions of ttyS1 with
 * direction+timestamp to /tmp/ttyinject.log), PLUS it can inject our own frames
 * onto ttyS1 — piggybacking on img_trans_app's already-open, already-configured
 * fd, and writing only on a clean frame boundary (right after one of the app's
 * own writes), so the MCU never sees a half-frame.
 *
 *   W = bytes img_trans_app sends to MCU (OF 0x68/05, poll 0x68/0a, sticks 0x68/01)
 *   R = bytes MCU sends back (telemetry 0x58 8a/8b/8c ; 0x8b pos17 = mode 0e GPS/02 optical)
 *   I = a frame WE injected
 *
 * --- control file: /tmp/ttyinject.cmd ---
 *   Write "<count> <hex bytes...>" to it; the shim injects that frame `count`
 *   times (one per ttyS1 write-cycle), logs each as "I", then clears the file.
 *   count <= 0  OR  the word "inf"  => inject on every cycle until you blank the file.
 *
 *   # inject the captured stick frame once:
 *   echo "1 68 01 0d 80 80 00 80 20 18 00 00 00 00 00 00 00 b4" > /tmp/ttyinject.cmd
 *   # hold a candidate mode-command frame continuously, then watch 0x8b pos17:
 *   echo "inf 68 02 01 02 ab" > /tmp/ttyinject.cmd
 *   : > /tmp/ttyinject.cmd        # stop the continuous injection
 *
 * --- build / deploy ---  (same as ttyhook; see tools/build.ps1 -Src ttyinject.c -Out ttyinject.so)
 *   adb push ttyinject.so /tmp/ ; kill img_trans_app ; cd /img_trans ;
 *   export LD_LIBRARY_PATH=/oem/usr/lib:/img_trans/libs ;
 *   setsid env LD_PRELOAD=/tmp/ttyinject.so ./bin/img_trans_app >/tmp/app.out 2>&1 </dev/null &
 *
 * SAFETY: injecting a valid command frame while the drone is armed can spin the
 * motors / trigger takeoff. ALWAYS bench-test with PROPS OFF.
 */
#define _GNU_SOURCE
#include <dlfcn.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>
#include <fcntl.h>
#include <unistd.h>
#include <time.h>
#include <pthread.h>

#define CMD_PATH "/tmp/ttyinject.cmd"
#define LOG_PATH "/tmp/ttyinject.log"
#define MAXFRAME 64

static ssize_t (*real_read)(int, void *, size_t);
static ssize_t (*real_write)(int, const void *, size_t);
static int     (*real_open)(const char *, int, ...);

static int  fc_fd = -1;                 /* /dev/ttyS1 descriptor, learned at open() */
static FILE *lg;
static pthread_mutex_t mx = PTHREAD_MUTEX_INITIALIZER;

/* pending injection, filled by the watcher thread, drained by write() */
static unsigned char inj_buf[MAXFRAME];
static int           inj_len   = 0;
static long          inj_count = 0;     /* >0 = that many; <0 = forever */
static pthread_mutex_t injmx = PTHREAD_MUTEX_INITIALIZER;

static void init(void)
{
    if (!real_read)  real_read  = dlsym(RTLD_NEXT, "read");
    if (!real_write) real_write = dlsym(RTLD_NEXT, "write");
    if (!real_open)  real_open  = dlsym(RTLD_NEXT, "open");
    if (!lg)         lg         = fopen(LOG_PATH, "a");
}

static void dump(const char *dir, const unsigned char *b, ssize_t n)
{
    if (n <= 0 || !lg) return;
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    pthread_mutex_lock(&mx);
    fprintf(lg, "%ld.%03ld %s %zd:", (long)ts.tv_sec, ts.tv_nsec / 1000000, dir, n);
    for (ssize_t i = 0; i < n; i++) fprintf(lg, " %02x", b[i]);
    fputc('\n', lg);
    fflush(lg);
    pthread_mutex_unlock(&mx);
}

/* watcher thread: poll CMD_PATH; on non-empty, parse "<count> <hex...>" into the
 * pending buffer, then truncate the file so it fires once per write. */
static void *watcher(void *arg)
{
    (void)arg;
    for (;;) {
        usleep(50 * 1000);
        FILE *f = fopen(CMD_PATH, "r");
        if (!f) continue;
        char line[512];
        if (!fgets(line, sizeof line, f)) { fclose(f); continue; }
        fclose(f);
        if (line[0] == '\0' || line[0] == '\n') continue;

        /* parse first token = count (number or "inf"), rest = hex bytes */
        char *p = line;
        while (*p == ' ' || *p == '\t') p++;
        long count;
        if (!strncmp(p, "inf", 3)) { count = -1; p += 3; }
        else { count = strtol(p, &p, 10); if (count == 0) count = -1; }

        unsigned char buf[MAXFRAME];
        int n = 0;
        while (n < MAXFRAME) {
            char *end;
            long v = strtol(p, &end, 16);
            if (end == p) break;
            buf[n++] = (unsigned char)v;
            p = end;
        }
        if (n > 0) {
            pthread_mutex_lock(&injmx);
            memcpy(inj_buf, buf, n);
            inj_len = n;
            inj_count = count;
            pthread_mutex_unlock(&injmx);
            if (lg) {
                pthread_mutex_lock(&mx);
                fprintf(lg, "# armed inject count=%ld len=%d\n", count, n);
                fflush(lg);
                pthread_mutex_unlock(&mx);
            }
        }
        /* clear the command file so a one-shot does not re-trigger */
        FILE *t = fopen(CMD_PATH, "w");
        if (t) fclose(t);
    }
    return NULL;
}

int open(const char *path, int flags, ...)
{
    init();
    va_list ap; va_start(ap, flags);
    mode_t m = va_arg(ap, int); va_end(ap);
    int fd = real_open(path, flags, m);
    if (fd >= 0 && path && strstr(path, "ttyS1")) {
        fc_fd = fd;
        if (lg) { fprintf(lg, "# open %s fd=%d\n", path, fd); fflush(lg); }
        static int started = 0;
        if (!started) { started = 1; pthread_t th; pthread_create(&th, NULL, watcher, NULL); }
    }
    return fd;
}

ssize_t read(int fd, void *buf, size_t cnt)
{
    init();
    ssize_t n = real_read(fd, buf, cnt);
    if (fd == fc_fd) dump("R", buf, n);     /* MCU -> SoC (telemetry) */
    return n;
}

ssize_t write(int fd, const void *buf, size_t cnt)
{
    init();
    if (fd == fc_fd) dump("W", buf, cnt);   /* SoC -> MCU */
    ssize_t r = real_write(fd, buf, cnt);

    /* after the app's own frame on ttyS1, inject ours if one is pending */
    if (fd == fc_fd) {
        pthread_mutex_lock(&injmx);
        if (inj_len > 0 && inj_count != 0) {
            unsigned char local[MAXFRAME]; int n = inj_len;
            memcpy(local, inj_buf, n);
            if (inj_count > 0) inj_count--;
            pthread_mutex_unlock(&injmx);
            real_write(fc_fd, local, n);
            dump("I", local, n);
        } else {
            pthread_mutex_unlock(&injmx);
        }
    }
    return r;
}
