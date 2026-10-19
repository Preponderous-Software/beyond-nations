// Fetches the runtime's large files gzip-compressed and inflates them in the
// browser. `dotnet publish` writes a .gz beside every _framework file; static
// hosts that serve files as they are (the arcade does) would otherwise send
// the wasm and assemblies at full size. DecompressionStream is built into
// every current browser; without it, or if a .gz is missing, the plain file
// is fetched instead.

const COMPRESSED_TYPES = new Set(["dotnetwasm", "assembly", "pdb", "icu", "globalization"]);

export function gzipResourceLoader(onBytes) {
  let loaded = 0;
  const canInflate = typeof DecompressionStream === "function";

  function counted(body) {
    const reader = body.getReader();
    return new ReadableStream({
      async pull(controller) {
        const { done, value } = await reader.read();
        if (done) { controller.close(); return; }
        loaded += value.byteLength;
        onBytes?.(loaded);
        controller.enqueue(value);
      },
      cancel(reason) { reader.cancel(reason); },
    });
  }

  return function load(type, name, defaultUri) {
    if (!COMPRESSED_TYPES.has(type)) return undefined;
    const contentType = type === "dotnetwasm" ? "application/wasm" : "application/octet-stream";
    const plain = () => fetch(defaultUri, { credentials: "omit" }).then((response) =>
      new Response(counted(response.body), { status: response.status, headers: { "content-type": contentType } }));
    if (!canInflate) return plain();
    return (async () => {
      try {
        const response = await fetch(defaultUri + ".gz", { credentials: "omit" });
        if (!response.ok || !response.body) return plain();
        const inflated = counted(response.body).pipeThrough(new DecompressionStream("gzip"));
        return new Response(inflated, { status: 200, headers: { "content-type": contentType } });
      } catch (_) {
        return plain();
      }
    })();
  };
}
