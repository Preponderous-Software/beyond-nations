// On-screen controls for touch screens: a joystick, buttons that press a key
// (by KeyboardEvent.code, so the game sees exactly what a keyboard sends),
// the safe-area insets, and keeping the browser's own gestures (scrolling,
// pinch zoom, pull to refresh, long-press menus) away from the game.
// Layout and labels come from the page; this only wires them.

/** A device whose main pointer is a finger: no hover, coarse pointer. */
export function isTouchOnly() {
  return window.matchMedia?.("(hover: none) and (pointer: coarse)").matches === true;
}

/**
 * The touch-controls setting: ?touch=on|off|auto in the URL wins, then what
 * the player last chose (localStorage, which may be unavailable), then auto.
 */
export function readTouchMode(storageKey) {
  const fromUrl = new URLSearchParams(window.location.search).get("touch");
  const valid = (value) => (value === "on" || value === "off" || value === "auto" ? value : null);
  if (valid(fromUrl)) return fromUrl;
  try {
    return valid(localStorage.getItem(storageKey)) || "auto";
  } catch (_) {
    return "auto";
  }
}

export function storeTouchMode(storageKey, mode) {
  try { localStorage.setItem(storageKey, mode); } catch (_) { /* private window: this visit only */ }
}

/**
 * container: an element holding <button data-key="KeyE">..</button> buttons.
 * input: the object startWebHost returned as `input` (key(code, down)).
 *
 * A button is a key press: down and up at once when the finger lands, which
 * the game's input latch turns into exactly one press, so a button that
 * opens a panel (and is then hidden under it) can never leave its key stuck
 * down. Buttons with data-toggle (Run) stay down until tapped again.
 * Returns { release() } to let go of every toggle.
 */
export function wireTouchControls(container, input) {
  const toggles = [];
  for (const button of container.querySelectorAll("[data-key]")) {
    const code = button.dataset.key;
    const toggle = button.hasAttribute("data-toggle");
    let held = false;
    const setHeld = (value) => {
      held = value;
      button.classList.toggle("held", held);
      button.setAttribute("aria-pressed", held ? "true" : "false");
      input.key(code, held);
    };
    if (toggle) {
      button.setAttribute("aria-pressed", "false");
      toggles.push(() => { if (held) setHeld(false); });
    }
    button.addEventListener("pointerdown", (event) => {
      event.preventDefault();
      event.stopPropagation();
      if (toggle) {
        setHeld(!held);
        return;
      }
      button.classList.add("held");
      input.key(code, true);
      input.key(code, false);
    });
    const unflash = () => { if (!toggle) button.classList.remove("held"); };
    button.addEventListener("pointerup", unflash);
    button.addEventListener("pointercancel", unflash);
    button.addEventListener("pointerleave", unflash);
    button.addEventListener("contextmenu", (event) => event.preventDefault());
    // Keyboard and screen-reader activation (Enter/Space on a focused
    // button) arrives as a click with no pointerdown before it.
    button.addEventListener("click", (event) => {
      if (event.detail !== 0) return;
      if (toggle) setHeld(!held);
      else { input.key(code, true); input.key(code, false); }
    });
  }
  return {
    release() { for (const letGo of toggles) letGo(); },
  };
}

/**
 * A virtual joystick. base: the ring the thumb moves in; its first element
 * child is the knob. The stick reports (x, y) with x to the right and y
 * forward (up the screen), each in [-1, 1], through input.move: once when it
 * changes, and (0, 0) when the finger lifts. A small dead zone stops a
 * resting thumb from creeping. onMove(x, y), if given, sees the same values.
 */
export function wireJoystick(base, input, onMove) {
  const knob = base.firstElementChild;
  const DEAD_ZONE = 0.12;
  const AXIS_SNAP = 0.15;
  let activeId = null;
  let centre = { x: 0, y: 0 };
  let radius = 1;
  let last = { x: 0, y: 0 };

  const send = (x, y) => {
    if (x === last.x && y === last.y) return;
    last = { x, y };
    input.move(x, y);
    onMove?.(x, y);
  };

  const place = (clientX, clientY) => {
    let dx = (clientX - centre.x) / radius;
    let dy = (clientY - centre.y) / radius;
    const length = Math.hypot(dx, dy);
    if (length > 1) { dx /= length; dy /= length; }
    knob.style.transform = `translate(${(dx * radius).toFixed(1)}px, ${(dy * radius).toFixed(1)}px)`;
    const reach = Math.min(1, length);
    if (reach < DEAD_ZONE) {
      send(0, 0);
      return;
    }
    // rescale past the dead zone, so the stick still reaches full speed
    const scale = (reach - DEAD_ZONE) / (1 - DEAD_ZONE) / reach;
    // and snap a nearly straight push to straight, so walking forward with
    // a thumb a little off-centre does not also turn the player
    const snap = (value) => (Math.abs(value) < AXIS_SNAP ? 0 : +value.toFixed(3));
    send(snap(dx * scale), snap(-dy * scale));
  };

  const reset = () => {
    activeId = null;
    base.classList.remove("held");
    knob.style.transform = "";
    send(0, 0);
  };

  base.addEventListener("pointerdown", (event) => {
    event.preventDefault();
    event.stopPropagation();
    if (activeId !== null) return;
    activeId = event.pointerId;
    base.setPointerCapture?.(event.pointerId);
    const rect = base.getBoundingClientRect();
    centre = { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
    radius = rect.width / 2;
    base.classList.add("held");
    place(event.clientX, event.clientY);
  });
  base.addEventListener("pointermove", (event) => {
    if (event.pointerId !== activeId) return;
    event.preventDefault();
    place(event.clientX, event.clientY);
  });
  const lift = (event) => { if (event.pointerId === activeId) reset(); };
  base.addEventListener("pointerup", lift);
  base.addEventListener("pointercancel", lift);
  base.addEventListener("lostpointercapture", lift);
  base.addEventListener("contextmenu", (event) => event.preventDefault());
  return { reset };
}

/**
 * The safe area's insets in CSS pixels, read from an element whose padding
 * is env(safe-area-inset-*) (style.css #safe-area-probe).
 */
export function readSafeArea(probe) {
  const style = getComputedStyle(probe);
  const px = (value) => Math.max(0, parseFloat(value) || 0);
  return {
    left: px(style.paddingLeft), top: px(style.paddingTop),
    right: px(style.paddingRight), bottom: px(style.paddingBottom),
  };
}

/**
 * While the game is up, the page itself never scrolls, zooms, refreshes on
 * a pull or opens a long-press menu: touch-action and overscroll-behavior
 * in style.css do most of it, and these listeners catch what iOS Safari
 * does regardless (pinch gestures, double-tap zoom, the callout). Elements
 * inside .panel (the loading, exited and error boxes) keep their defaults so
 * their links and scrolling work.
 */
export function preventPageGestures() {
  const inPanel = (event) => event.target instanceof Element && event.target.closest(".panel") !== null;
  const stop = (event) => { if (!inPanel(event)) event.preventDefault(); };
  document.addEventListener("touchmove", stop, { passive: false });
  document.addEventListener("gesturestart", stop, { passive: false });
  document.addEventListener("gesturechange", stop, { passive: false });
  document.addEventListener("dblclick", stop, { passive: false });
  document.addEventListener("contextmenu", stop);
  document.addEventListener("selectstart", stop);
}
