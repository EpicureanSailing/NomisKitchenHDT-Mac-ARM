#define _DARWIN_C_SOURCE
#include <mach-o/dyld.h>
#include <limits.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

static void parent(char *path) {
    char *slash = strrchr(path, '/');
    if (!slash) exit(1);
    *slash = '\0';
}

int main(int argc, char **argv) {
    (void)argc;
    char raw[PATH_MAX], executable[PATH_MAX], root[PATH_MAX];
    char original[PATH_MAX], runtime[PATH_MAX], target[PATH_MAX];
    char corlibs[2 * PATH_MAX], library[PATH_MAX], disabled[PATH_MAX];
    uint32_t size = sizeof(raw);
    if (_NSGetExecutablePath(raw, &size) || !realpath(raw, executable)) return 1;
    if (snprintf(original, sizeof(original), "%s.nomi-original", executable) >= (int)sizeof(original)) return 1;
    strlcpy(root, executable, sizeof(root));
    /* MacOS -> Contents -> Hearthstone.app -> game installation directory. */
    for (int i = 0; i < 4; i++) parent(root);
    if (snprintf(runtime, sizeof(runtime), "%s/.nomi-dance-arm64", root) >= (int)sizeof(runtime)) return 1;
    if (snprintf(target, sizeof(target), "%s/BepInEx/core/BepInEx.Preloader.dll", runtime) >= (int)sizeof(target) ||
        snprintf(library, sizeof(library), "%s/libdoorstop.dylib", runtime) >= (int)sizeof(library) ||
        snprintf(disabled, sizeof(disabled), "%s/disabled", runtime) >= (int)sizeof(disabled) ||
        snprintf(corlibs, sizeof(corlibs), "%s/BepInEx/unity6000_corlibs:%s/BepInEx/unstripped_corlib", runtime, runtime) >= (int)sizeof(corlibs)) return 1;
    if (access(disabled, F_OK) && !access(target, R_OK) && !access(library, R_OK)) {
        const char *previous = getenv("DYLD_INSERT_LIBRARIES");
        char *libraries = NULL;
        if (asprintf(&libraries, "%s%s%s", library,
                     previous && *previous ? ":" : "", previous ? previous : "") < 0) return 1;
        int failed = setenv("DOORSTOP_ENABLED", "1", 1) ||
            setenv("DOORSTOP_TARGET_ASSEMBLY", target, 1) ||
            setenv("DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE", corlibs, 1) ||
            setenv("DOORSTOP_MONO_DEBUG_ENABLED", "0", 1) ||
            setenv("DYLD_INSERT_LIBRARIES", libraries, 1);
        free(libraries);
        if (failed) return 1;
    }
    execv(original, argv);
    perror("Nomi Dance ARM64: original executable");
    return 1;
}
