// Keeps the canvas's drawing buffer matched to its displayed size in device
// pixels, so the picture is sharp at any devicePixelRatio and the UI is laid
// out in CSS pixels (the size a person sees), not shrunk on high-DPI screens.

const MAX_DRAWING_BUFFER = 4096;

export function trackCanvasSize(canvas) {
  const state = { cssWidth: 1, cssHeight: 1, ratio: 1 };

  function measure() {
    const rect = canvas.getBoundingClientRect();
    const cssWidth = Math.max(1, rect.width);
    const cssHeight = Math.max(1, rect.height);
    let ratio = window.devicePixelRatio || 1;
    // Cap the buffer, not the layout: a 4K screen at 2x still gets a
    // reasonable framebuffer, drawn a little softer.
    const largest = Math.max(cssWidth, cssHeight) * ratio;
    if (largest > MAX_DRAWING_BUFFER) ratio *= MAX_DRAWING_BUFFER / largest;
    const width = Math.max(1, Math.round(cssWidth * ratio));
    const height = Math.max(1, Math.round(cssHeight * ratio));
    if (canvas.width !== width || canvas.height !== height) {
      canvas.width = width;
      canvas.height = height;
    }
    state.cssWidth = cssWidth;
    state.cssHeight = cssHeight;
    // The ratio actually in effect after rounding.
    state.ratio = width / cssWidth;
    return state;
  }

  return { measure, state };
}
