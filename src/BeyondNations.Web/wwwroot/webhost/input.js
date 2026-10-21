// Browser input for the web host, forwarded to the game's exports as it
// arrives (host/WebHost.cs, WebHostExports). Game-agnostic: keys are
// KeyboardEvent.code strings and the game maps them.
//
// Looking around is a right-button drag, as on the desktop since BN #291:
// pressing the right button asks for pointer lock, so the cursor neither
// hits the screen edge nor wanders over the UI, and the relative movement is
// sent as Look(dx, dy) until the button is released. Where pointer lock is
// refused (some embeds) the same drag still works, the cursor just moves.

// A finger on the canvas (pointerType "touch") is handled apart from the
// mouse. What a drag does depends on what is on screen, which the page
// answers through hooks.touchDrag():
//   "look"    the drag steers, as a right-drag does (hooks.touchLookScale
//             times the pixels); a tap clicks the UI and is reported as
//             Tap(x, y), so the game can act on a tap on the world;
//   "scroll"  the drag scrolls the panel under the finger (as wheel
//             movement); a tap clicks;
//   "pointer" the finger is a mouse with its left button held: ImGui sees
//             the press at once and every move (sliders).
// In "look" and "scroll" the press is only sent when the finger lifts
// without having travelled, so a drag that starts on a button never clicks it.

// Keys whose browser default (scrolling, find, reload, help) would fight the
// game. Ctrl/Cmd combinations are always left to the browser.
const GAME_KEYS = /^(Key[A-Z]|Digit\d|Arrow\w+|Space|Tab|Enter|Escape|Backspace|Insert|Delete|Home|End|PageUp|PageDown|ShiftLeft|ShiftRight|F[1-9]|F10|F12|Quote|Slash|Backquote)$/;

// One event's movement, bounded: a wild value from a lock transition or a
// driver hiccup should nudge the view, not spin it.
const clampMove = (value) => Math.max(-250, Math.min(250, value || 0));

// How far a finger may wander and still count as a tap, in CSS pixels.
const TAP_SLOP = 10;
// Dear ImGui scrolls 5 lines of its 13 px font per wheel step and the C#
// side divides DOM wheel pixels by 100, so this many DOM pixels of wheel
// per pixel of finger keep the panel under the finger.
const WHEEL_PER_PIXEL = 100 / 65;

export function attachInput(canvas, exports, hooks = {}) {
  const pointer = { look: false, lookId: -1, wasLocked: false, touchStart: null };

  const position = (event) => {
    const rect = canvas.getBoundingClientRect();
    return [event.clientX - rect.left, event.clientY - rect.top];
  };

  // DOM button order (left, middle, right) -> ImGui's (left, right, middle).
  const imguiButton = (domButton) => (domButton === 0 ? 0 : domButton === 2 ? 1 : domButton === 1 ? 2 : domButton);

  window.addEventListener("keydown", (event) => {
    if (event.ctrlKey || event.metaKey || event.altKey) return;
    if (GAME_KEYS.test(event.code)) event.preventDefault();
    if (!event.repeat) exports.Key(event.code, true);
    if (event.key && event.key.length === 1) exports.Character(event.key.codePointAt(0));
    hooks.onKey?.(event.code);
  });
  window.addEventListener("keyup", (event) => {
    if (GAME_KEYS.test(event.code)) event.preventDefault();
    exports.Key(event.code, false);
  });

  canvas.addEventListener("contextmenu", (event) => event.preventDefault());

  canvas.addEventListener("pointerdown", (event) => {
    canvas.focus({ preventScroll: true });
    const [x, y] = position(event);
    exports.PointerMove(x, y);
    if (event.pointerType === "touch") {
      // One finger on the canvas at a time; a second (the other thumb is
      // usually on the joystick, which is not the canvas) is ignored.
      if (pointer.touchStart) return;
      const mode = hooks.touchDrag?.() || "look";
      pointer.touchStart = {
        id: event.pointerId, x: event.clientX, y: event.clientY,
        lastX: event.clientX, lastY: event.clientY, dragging: false, mode,
      };
      if (mode === "pointer") exports.PointerButton(0, true);
      else exports.PointerMove(-1e6, -1e6);
      hooks.onTouch?.();
      hooks.onTap?.();
      canvas.setPointerCapture?.(event.pointerId);
      return;
    }
    exports.PointerButton(imguiButton(event.button), true);
    if (event.button === 2) {
      pointer.look = true;
      pointer.wasLocked = document.pointerLockElement === canvas;
      pointer.lookId = event.pointerId;
      canvas.setPointerCapture?.(event.pointerId);
      // Pointer lock is a nicety here; the drag works without it, so a
      // refusal (an embed without allow-pointer-lock, an old browser that
      // lacks unadjustedMovement) is retried plainly once and then ignored.
      const quiet = (attempt) => {
        try { attempt()?.catch?.(() => {}); } catch (_) { /* refused */ }
      };
      try {
        const request = canvas.requestPointerLock?.({ unadjustedMovement: true });
        request?.catch?.(() => quiet(() => canvas.requestPointerLock?.()));
      } catch (_) {
        quiet(() => canvas.requestPointerLock?.());
      }
    }
  });

  canvas.addEventListener("pointermove", (event) => {
    if (pointer.touchStart && event.pointerId === pointer.touchStart.id) {
      const t = pointer.touchStart;
      // Deltas from the previous event, not movementX: not every browser
      // fills that in for touch.
      // The first step of a drag counts from where the finger landed, so
      // the slop a tap is allowed is not lost from the drag.
      const stepX = event.clientX - (t.dragging ? t.lastX : t.x);
      const stepY = event.clientY - (t.dragging ? t.lastY : t.y);
      t.lastX = event.clientX;
      t.lastY = event.clientY;
      if (t.mode === "pointer") {
        const [x, y] = position(event);
        exports.PointerMove(x, y);
        return;
      }
      if (!t.dragging && Math.hypot(event.clientX - t.x, event.clientY - t.y) > TAP_SLOP) {
        t.dragging = true;
        if (t.mode === "scroll") {
          // the wheel scrolls whatever window is under the pointer
          const [sx, sy] = position({ clientX: t.x, clientY: t.y });
          exports.PointerMove(sx, sy);
        }
      }
      if (t.dragging) {
        if (t.mode === "scroll") {
          exports.Wheel(0, -stepY * WHEEL_PER_PIXEL);
        } else {
          const scale = hooks.touchLookScale ?? 1;
          exports.Look(clampMove(stepX * scale), clampMove(stepY * scale));
        }
      }
      return;
    }
    if (pointer.look && event.pointerId === pointer.lookId) {
      const locked = document.pointerLockElement === canvas;
      if (locked !== pointer.wasLocked) {
        // The first movement after the lock starts or ends is measured
        // against wherever the cursor was before (a known Chromium jump), so
        // it is not a real movement of the mouse.
        pointer.wasLocked = locked;
      } else {
        exports.Look(clampMove(event.movementX), clampMove(event.movementY));
      }
      if (locked) return;
    }
    const [x, y] = position(event);
    exports.PointerMove(x, y);
  });

  const release = (event) => {
    if (pointer.touchStart && event.pointerId === pointer.touchStart.id) {
      const t = pointer.touchStart;
      pointer.touchStart = null;
      const [x, y] = position(event);
      if (t.mode === "pointer") {
        exports.PointerMove(x, y);
        exports.PointerButton(0, false);
        return;
      }
      if (t.dragging || event.type === "pointercancel") {
        exports.PointerMove(-1e6, -1e6);
        return;
      }
      // A tap: ImGui's input queue plays the press and the release over two
      // frames, so a button under the finger is clicked; then the pointer
      // leaves, so nothing stays highlighted under a finger that has gone.
      exports.PointerMove(x, y);
      exports.PointerButton(0, true);
      exports.PointerButton(0, false);
      if (t.mode === "look") exports.Tap?.(x, y);
      exports.PointerMove(-1e6, -1e6);
      return;
    }
    exports.PointerButton(imguiButton(event.button), false);
    if (event.button === 2 || (event.type === "pointercancel" && event.pointerId === pointer.lookId)) {
      pointer.look = false;
      pointer.lookId = -1;
      if (document.pointerLockElement === canvas) document.exitPointerLock?.();
    }
  };
  canvas.addEventListener("pointerup", release);
  canvas.addEventListener("pointercancel", release);

  canvas.addEventListener("wheel", (event) => {
    event.preventDefault();
    const scale = event.deltaMode === 1 ? 33 : event.deltaMode === 2 ? 400 : 1;
    exports.Wheel(event.deltaX * scale, event.deltaY * scale);
  }, { passive: false });

  const lost = () => {
    pointer.look = false;
    pointer.touchStart = null;
    exports.FocusLost();
  };
  window.addEventListener("blur", lost);
  document.addEventListener("visibilitychange", () => { if (document.hidden) lost(); });
  document.addEventListener("pointerlockchange", () => {
    // Escape releases pointer lock without a pointerup reaching us.
    if (document.pointerLockElement !== canvas && pointer.look) {
      pointer.look = false;
      exports.PointerButton(1, false);
    }
  });

  return {
    /** For on-screen controls: press or release a key by its code. */
    key(code, down) { exports.Key(code, down); },
    look(dx, dy) { exports.Look(dx, dy); },
    /** The on-screen stick: x right, y forward, each in [-1, 1]. */
    move(x, y) { exports.Move?.(x, y); },
  };
}
