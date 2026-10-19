// On-screen controls for touch-only devices. Each button holds a key (by
// KeyboardEvent.code) down for as long as a finger is on it, so the game
// sees exactly what a keyboard would send. Layout and labels come from the
// page; this only wires them.

export function isTouchOnly() {
  return window.matchMedia?.("(hover: none) and (pointer: coarse)").matches === true;
}

/**
 * container: an element holding <button data-key="KeyW">..</button> buttons.
 * input: the object startWebHost returned as `input` (key(code, down)).
 * Buttons with data-toggle stay down until tapped again (e.g. sprint).
 */
export function wireTouchControls(container, input) {
  for (const button of container.querySelectorAll("[data-key]")) {
    const code = button.dataset.key;
    const toggle = button.hasAttribute("data-toggle");
    let held = false;
    const press = (event) => {
      event.preventDefault();
      button.setPointerCapture?.(event.pointerId);
      if (toggle) {
        held = !held;
        button.classList.toggle("held", held);
        input.key(code, held);
        return;
      }
      if (held) return;
      held = true;
      button.classList.add("held");
      input.key(code, true);
    };
    const lift = (event) => {
      event.preventDefault();
      if (toggle || !held) return;
      held = false;
      button.classList.remove("held");
      input.key(code, false);
    };
    button.addEventListener("pointerdown", press);
    button.addEventListener("pointerup", lift);
    button.addEventListener("pointercancel", lift);
    button.addEventListener("lostpointercapture", lift);
    button.addEventListener("contextmenu", (event) => event.preventDefault());
  }
}
