/* strata-loader.js: the loading shell for a Strata page.

   Part of the Strata web kit (docs/WEB.md, "The web kit"). A classic script
   with no dependencies. It defines window.StrataLoader.

     const mod = await StrataLoader.load({
       script: "/3d/scene.js",          // the Emscripten glue (MODULARIZE)
       factory: "createScene",          // its EXPORT_NAME
       wasm: "/3d/scene.wasm",
       bytes: 1002049,                  // optional: the size, stated before the fetch answers
       title: "the 3D garden",          // "Loading the 3D garden"
       poster: "/3d/poster.jpg",        // optional backdrop, blurred while loading
       trailer: {webm: "...", mp4: "..."},  // optional: played when the page can't run live
       fallback: {href: "/", text: "Open the 2D dashboard"},  // optional link on every fallback
       gate: false,                     // true: wait for a tap when the device asks for less
     });
     // ... create the scene with StrataKit.create({module: mod, ...}), then:
     StrataLoader.done();

   A host whose own loader already knows how far it has got (a .NET or
   Unity-style boot that fetches its own files) shows the same overlay and
   drives its bar itself:

     if (StrataLoader.show({title: "the game", poster: "/poster.jpg"})) {
       StrataLoader.progress(0.4, "Loading assemblies");  // 0..1, label optional
       // ... boot; then:
       StrataLoader.done();
     }

   show() takes load()'s display options (title, poster, trailer, fallback,
   mount) and returns false, with the fallback showing, when WebGL 2 is
   missing. progress(fraction, label) clamps the fraction to 0..1 and never
   moves the bar back; the label (or the percentage) replaces the size line.

   What it shows, in an overlay it adds to the page (class sk-loader):

   - the poster (if any) as a blurred backdrop, and the title;
   - real progress: the .wasm is fetched as a stream and its bytes counted
     against Content-Length (or `bytes`), shown as "1.2 of 3.4 MB", with
     role=progressbar. The counted bytes are handed to the module as
     wasmBinary, so nothing is downloaded twice. The glue script loads at the
     same time;
   - the fallback, instead of a blank page: when WebGL 2 is missing, when the
     engine fails, when a lost context does not come back, or when the device
     is too slow even at the lowest tier (StrataLoader.slow()): the trailer
     (muted, looping, with controls; a still poster under reduced motion), or
     the poster, with the reason in words and the fallback link.

   With gate: true, a device that asks for less (prefers-reduced-motion,
   Save-Data, or under 4 GB of device memory where the browser says) gets the
   poster and a "Start" button instead of an automatic download.

   load() resolves with the module, or rejects with an Error whose `code` is
   "no-webgl2", "network" or "engine" after the fallback is already showing.

   Colours follow the page through CSS custom properties on :root (or any
   ancestor): --sk-loader-bg, --sk-loader-fg, --sk-loader-panel and
   --sk-loader-accent.

   MIT licensed, as Strata. */
(function () {
  "use strict";

  const CSS = `
.sk-loader{position:fixed;inset:0;z-index:20;display:grid;place-items:center;padding:16px;
  background:var(--sk-loader-bg,#101712);color:var(--sk-loader-fg,#e3ece4);
  font:14px/1.4 system-ui,-apple-system,"Segoe UI",sans-serif;transition:opacity .3s}
.sk-loader[hidden]{display:none}
.sk-loader.sk-done{opacity:0;pointer-events:none}
.sk-loader .sk-backdrop{position:absolute;inset:0;background-size:cover;background-position:center;
  filter:blur(14px) brightness(.55);transform:scale(1.06)}
.sk-loader .sk-media{position:absolute;inset:0;width:100%;height:100%;object-fit:cover}
.sk-loader .sk-box{position:relative;box-sizing:border-box;max-width:min(420px,100%);width:100%;text-align:center;
  background:var(--sk-loader-panel,rgba(0,0,0,.45));border-radius:12px;padding:16px 18px;
  -webkit-backdrop-filter:blur(4px);backdrop-filter:blur(4px)}
.sk-loader.sk-fallback .sk-box{position:absolute;left:16px;right:16px;bottom:max(16px,env(safe-area-inset-bottom));
  width:auto;max-width:420px;margin:0 auto}
.sk-loader .sk-title{font-weight:600;margin:0 0 8px}
.sk-loader .sk-bar{height:8px;border-radius:4px;background:rgba(255,255,255,.18);overflow:hidden}
.sk-loader .sk-fill{height:100%;width:0;background:var(--sk-loader-accent,#7fd082);transition:width .15s}
.sk-loader .sk-bar.sk-indeterminate .sk-fill{width:30%;animation:sk-slide 1.2s ease-in-out infinite}
@keyframes sk-slide{from{margin-left:-30%}to{margin-left:100%}}
.sk-loader .sk-size{margin-top:6px;font-size:12px;opacity:.85;font-variant-numeric:tabular-nums}
.sk-loader .sk-msg{margin:0 0 8px}
.sk-loader a{color:var(--sk-loader-accent,#7fd082)}
.sk-loader button{font:inherit;min-height:44px;min-width:44px;padding:8px 18px;border-radius:8px;border:0;
  background:var(--sk-loader-accent,#7fd082);color:#0b130c;font-weight:600;cursor:pointer}
@media (prefers-reduced-motion: reduce){.sk-loader,.sk-loader .sk-fill{transition:none}
  .sk-loader .sk-bar.sk-indeterminate .sk-fill{animation:none;width:100%;opacity:.5}}`;

  let root = null, opts = {}, state = "idle", shown = 0;

  function el(tag, cls, text) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = text;
    return e;
  }
  function mb(n) { return (n / 1048576).toFixed(n < 10485760 ? 1 : 0); }
  function sizeText(got, total) {
    if (!total) return got ? `${mb(got)} MB` : "";
    return `${mb(Math.min(got, total))} of ${mb(total)} MB`;
  }
  const reducedMotion = () => matchMedia("(prefers-reduced-motion: reduce)").matches;

  function mount() {
    if (root) return root;
    const style = el("style");
    style.textContent = CSS;
    document.head.appendChild(style);
    root = el("div", "sk-loader");
    root.setAttribute("role", "status");
    if (opts.poster) {
      const b = el("div", "sk-backdrop");
      b.style.backgroundImage = `url("${String(opts.poster).replace(/"/g, "%22")}")`;
      root.appendChild(b);
    }
    const box = el("div", "sk-box");
    box.appendChild(el("p", "sk-title", opts.title ? `Loading ${opts.title}` : "Loading"));
    const bar = el("div", "sk-bar");
    bar.setAttribute("role", "progressbar");
    bar.setAttribute("aria-valuemin", "0");
    bar.setAttribute("aria-valuemax", "100");
    bar.setAttribute("aria-label", opts.title ? `Loading ${opts.title}` : "Loading");
    bar.appendChild(el("div", "sk-fill"));
    box.appendChild(bar);
    box.appendChild(el("div", "sk-size", opts.bytes ? `${mb(opts.bytes)} MB` : ""));
    root.appendChild(box);
    (opts.mount || document.body).appendChild(root);
    return root;
  }

  // Sets the bar to `fraction` (0..1), or to indeterminate when it is null,
  // and the line under it to `text`. Both progress paths draw through here.
  function setBar(fraction, text) {
    if (!root) return;
    const bar = root.querySelector(".sk-bar"), fill = root.querySelector(".sk-fill");
    if (!bar) return;
    if (fraction != null) {
      const pct = Math.min(100, Math.round(fraction * 100));
      bar.classList.remove("sk-indeterminate");
      fill.style.width = pct + "%";
      bar.setAttribute("aria-valuenow", String(pct));
    } else {
      bar.classList.add("sk-indeterminate");
      bar.removeAttribute("aria-valuenow");
    }
    root.querySelector(".sk-size").textContent = text;
  }

  // load()'s byte progress: `got` of `total` bytes (total 0 when unknown).
  function byteProgress(got, total) {
    setBar(total ? got / total : null, sizeText(got, total));
  }

  // A host's own progress: `fraction` of the way (clamped to 0..1, and never
  // lower than the last call's), with an optional label for the line under
  // the bar (else the percentage). Mounts the overlay if show() or load() has
  // not; does nothing once the shell is done or showing the fallback.
  function progress(fraction, label) {
    if (state === "done" || state === "fallback") return;
    const f = Number(fraction);
    if (Number.isNaN(f)) return;
    shown = Math.max(shown, Math.min(1, Math.max(0, f)));
    if (state === "idle") state = "loading";
    mount();
    setBar(shown, label != null && label !== "" ? String(label) : `${Math.round(shown * 100)}%`);
    const bar = root.querySelector(".sk-bar");
    if (bar) {
      if (label) bar.setAttribute("aria-valuetext", String(label));
      else bar.removeAttribute("aria-valuetext");
    }
  }

  // The overlay without load()'s fetch, for a host that boots its own way.
  // Returns false (the fallback showing) when WebGL 2 is missing.
  function show(o) {
    opts = Object.assign({}, o);
    state = "loading";
    shown = 0;
    mount();
    if (!hasWebGL2()) { fallback("no-webgl2"); return false; }
    return true;
  }

  function hasWebGL2() {
    try {
      const c = document.createElement("canvas");
      const gl = c.getContext("webgl2");
      if (!gl) return false;
      const lose = gl.getExtension("WEBGL_lose_context");
      if (lose) lose.loseContext();  // give the probe's context back at once
      return true;
    } catch (e) { return false; }
  }

  function asksForLess() {
    const conn = navigator.connection;
    if (reducedMotion()) return "reduced-motion";
    if (conn && conn.saveData) return "save-data";
    if (navigator.deviceMemory && navigator.deviceMemory < 4) return "low-memory";
    return null;
  }

  const REASONS = {
    "no-webgl2": "This browser can't draw WebGL 2, which the live view needs.",
    "engine": "The live view's renderer failed to start.",
    "network": "The live view didn't download.",
    "context-lost": "The live view lost its graphics context.",
    "slow": "This device draws the live view too slowly.",
  };

  // Shows the fallback: the trailer, else the poster, with the reason and the
  // link. Also usable by a page directly, e.g. StrataLoader.fallback("slow").
  function fallback(code, message) {
    state = "fallback";
    mount();
    root.hidden = false;
    root.classList.remove("sk-done");
    root.classList.add("sk-fallback");
    root.setAttribute("role", "alert");
    root.querySelectorAll(".sk-media").forEach((m) => m.remove());
    const t = opts.trailer;
    if (t && (t.webm || t.mp4) && !reducedMotion()) {
      const v = el("video", "sk-media");
      v.muted = true; v.loop = true; v.autoplay = true; v.playsInline = true; v.controls = true;
      v.setAttribute("aria-label", opts.trailerLabel || "A recorded clip of the live view");
      if (opts.poster) v.poster = opts.poster;
      if (t.webm) v.appendChild(Object.assign(el("source"), {src: t.webm, type: "video/webm"}));
      if (t.mp4) v.appendChild(Object.assign(el("source"), {src: t.mp4, type: "video/mp4"}));
      root.insertBefore(v, root.querySelector(".sk-box"));
    } else if (opts.poster) {
      const img = el("img", "sk-media");
      img.src = opts.poster;
      img.alt = opts.posterAlt || "A still of the live view";
      root.insertBefore(img, root.querySelector(".sk-box"));
    }
    const box = root.querySelector(".sk-box");
    box.textContent = "";
    box.appendChild(el("p", "sk-msg", message || REASONS[code] || REASONS.engine));
    if (t && (t.webm || t.mp4)) box.appendChild(el("p", "sk-size", "Showing a recorded clip instead."));
    if (opts.fallback && opts.fallback.href) {
      const p = el("p", "");
      p.style.margin = "0";
      const a = el("a", "", opts.fallback.text || "Open the simpler view");
      a.href = opts.fallback.href;
      p.appendChild(a);
      box.appendChild(p);
    }
    if (opts.onFallback) opts.onFallback(code);
  }

  function loadScript(src) {
    return new Promise((resolve, reject) => {
      const s = document.createElement("script");
      s.src = src;
      s.async = true;
      s.onload = resolve;
      s.onerror = () => reject(new Error("script " + src));
      document.head.appendChild(s);
    });
  }

  async function fetchWasm(url, statedBytes) {
    const res = await fetch(url, {credentials: "same-origin"});
    if (!res.ok) throw new Error(`${url}: HTTP ${res.status}`);
    // Content-Length is the size on the wire; when the body is compressed it
    // is smaller than what the reader yields, so the stated size wins then.
    const enc = res.headers.get("Content-Encoding");
    const header = parseInt(res.headers.get("Content-Length") || "", 10);
    let total = (!enc || enc === "identity") && header > 0 ? header : (statedBytes || 0);
    if (!res.body || !res.body.getReader) {
      const buf = await res.arrayBuffer();
      byteProgress(buf.byteLength, buf.byteLength);
      return buf;
    }
    const reader = res.body.getReader();
    const chunks = [];
    let got = 0;
    byteProgress(0, total);
    for (;;) {
      const {done, value} = await reader.read();
      if (done) break;
      chunks.push(value);
      got += value.byteLength;
      if (total && got > total) total = 0;  // the stated size was wrong: say bytes only
      byteProgress(got, total);
    }
    const out = new Uint8Array(got);
    let o = 0;
    for (const c of chunks) { out.set(c, o); o += c.byteLength; }
    byteProgress(got, got);
    return out.buffer;
  }

  function waitForStart(reason) {
    return new Promise((resolve) => {
      const box = root.querySelector(".sk-box");
      const keep = [...box.childNodes];
      box.textContent = "";
      const msg = reason === "save-data" ? "Data saver is on."
        : reason === "low-memory" ? "This device has little memory." : "This device asks for reduced motion.";
      const size = opts.bytes ? `a ${mb(opts.bytes)} MB download` : "a download";
      box.appendChild(el("p", "sk-msg", `${msg} The live view is ${size} and is animated.`));
      const go = el("button", "", "Start the live view");
      go.type = "button";
      go.addEventListener("click", () => { box.textContent = ""; keep.forEach((n) => box.appendChild(n)); resolve(); });
      box.appendChild(go);
      if (opts.fallback && opts.fallback.href) {
        const a = el("a", "", opts.fallback.text || "Open the simpler view");
        a.href = opts.fallback.href;
        a.style.display = "block";
        a.style.marginTop = "8px";
        box.appendChild(a);
      }
      go.focus();
    });
  }

  async function load(o) {
    opts = Object.assign({}, o);
    state = "loading";
    shown = 0;
    mount();
    if (!hasWebGL2()) {
      fallback("no-webgl2");
      throw Object.assign(new Error("no-webgl2"), {code: "no-webgl2"});
    }
    if (opts.gate) {
      const why = asksForLess();
      if (why) await waitForStart(why);
    }
    let binary;
    try {
      const glue = window[opts.factory] ? Promise.resolve() : loadScript(opts.script);
      [binary] = await Promise.all([fetchWasm(opts.wasm, opts.bytes), glue]);
    } catch (e) {
      fallback("network", `${REASONS.network} (${e.message || e})`);
      throw Object.assign(new Error("network"), {code: "network", cause: e});
    }
    const factory = window[opts.factory];
    if (typeof factory !== "function") {
      fallback("network", `${REASONS.network} (${opts.factory} is missing)`);
      throw Object.assign(new Error("network"), {code: "network"});
    }
    try {
      return await factory(Object.assign({
        wasmBinary: binary,
        locateFile: (p) => (opts.base || "") + p,
        print: (t) => console.log(t),
        printErr: (t) => console.warn(t),
      }, opts.moduleArgs || {}));
    } catch (e) {
      fallback("engine", `${REASONS.engine} (${e.message || e})`);
      throw Object.assign(new Error("engine"), {code: "engine", cause: e});
    }
  }

  // The live view is up: fade the shell out.
  function done() {
    if (!root || state === "fallback") return;
    state = "done";
    root.classList.add("sk-done");
    setTimeout(() => { if (state === "done") root.hidden = true; }, reducedMotion() ? 0 : 320);
  }

  // The kit could not go any lower and frames are still slow: offer the clip
  // (or the fallback link) without taking the live view away. Once: a second
  // call while the bar is up does nothing.
  function slow() {
    if (state === "fallback" || (!opts.trailer && !(opts.fallback && opts.fallback.href))) return;
    if (document.querySelector(".sk-slow")) return;
    const bar = el("div", "sk-slow");
    bar.setAttribute("role", "status");
    bar.style.cssText = "position:fixed;left:50%;transform:translateX(-50%);bottom:max(12px,env(safe-area-inset-bottom));" +
      "z-index:21;background:rgba(0,0,0,.7);color:#fff;padding:8px 12px;border-radius:8px;" +
      "font:13px system-ui,sans-serif;display:flex;gap:10px;align-items:center;max-width:calc(100vw - 32px)";
    bar.appendChild(el("span", "", "This device is drawing slowly."));
    const b = el("button", "", opts.trailer ? "Show the clip" : (opts.fallback.text || "Open the simpler view"));
    b.type = "button";
    b.style.cssText = "font:inherit;min-height:36px;padding:4px 10px;border-radius:6px;border:0;cursor:pointer";
    b.addEventListener("click", () => {
      bar.remove();
      if (opts.trailer) fallback("slow"); else location.href = opts.fallback.href;
    });
    const x = el("button", "", "×");
    x.type = "button";
    x.setAttribute("aria-label", "Dismiss");
    x.style.cssText = b.style.cssText;
    x.addEventListener("click", () => bar.remove());
    bar.appendChild(b);
    bar.appendChild(x);
    document.body.appendChild(bar);
  }

  window.StrataLoader = {load, show, progress, done, fallback, slow, hasWebGL2, get state() { return state; }};
})();
