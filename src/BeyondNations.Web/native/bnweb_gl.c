// The page's WebGL 2 context, made known to Emscripten.
//
// Strata draws into the context the host has made current through
// Emscripten (STRATA_SURFACE_HOST_CONTEXT). The page's own passes
// (wwwroot/webhost/gfx.js: labels, Dear ImGui) use the same context from
// JavaScript. Asking Emscripten to create a WebGL 2 context on a canvas that
// already has one returns that same context and registers it, so both
// sides draw into one context and one framebuffer.
#include <emscripten/html5.h>
#include <stdint.h>

// Returns the Emscripten context handle (> 0), or a negative
// EMSCRIPTEN_RESULT_* when no WebGL 2 context could be had.
int bnweb_gl_attach(const char *canvas_selector) {
    EmscriptenWebGLContextAttributes attributes;
    emscripten_webgl_init_context_attributes(&attributes);
    attributes.majorVersion = 2;
    attributes.minorVersion = 0;
    attributes.alpha = 0;
    attributes.depth = 1;
    attributes.stencil = 0;
    attributes.antialias = 1;
    attributes.premultipliedAlpha = 0;
    attributes.preserveDrawingBuffer = 0;
    attributes.powerPreference = EM_WEBGL_POWER_PREFERENCE_HIGH_PERFORMANCE;
    EMSCRIPTEN_WEBGL_CONTEXT_HANDLE context = emscripten_webgl_create_context(canvas_selector, &attributes);
    if (context <= 0) {
        return (int) context;
    }
    EMSCRIPTEN_RESULT made = emscripten_webgl_make_context_current(context);
    if (made != EMSCRIPTEN_RESULT_SUCCESS) {
        return (int) made;
    }
    return (int) context;
}

// strata_create requires a non-NULL get_proc_address even though, with
// Emscripten's bindings, it never calls it. This one would answer properly.
static void *bnweb_get_proc_address(const char *name, void *user) {
    (void) user;
    return emscripten_webgl_get_proc_address(name);
}

intptr_t bnweb_gl_get_proc_address_function(void) {
    return (intptr_t) &bnweb_get_proc_address;
}
