// WebGL2 device for the web host: three passes, each a port of a desktop
// renderer, driven from C# (host/WebHost.cs, WebGpu) with pointers into wasm
// memory. Nothing here knows which game it is drawing.
//
//   primitives  instanced coloured meshes with one directional light
//               (BeyondNations.Desktop/render/InstancedPrimitiveShader.cs)
//   labels      textured, tinted quads over the world, no depth test
//               (BeyondNations.Desktop/text/LabelShader.cs)
//   imgui       Dear ImGui draw lists (as imgui_impl_opengl3)
//
// The shaders are the desktop's with "#version 330 core" replaced by
// "#version 300 es" and a precision line added; nothing else changed.

let gl = null;
let canvas = null;
let heap = () => { throw new Error("webhost: heap view not set"); };

const meshes = [null];
const textures = [null];

let primitive = null;
let label = null;
let imgui = null;
let sampleRequest = null;

function u8() { return heap(); }
function f32(ptr, count) { return new Float32Array(u8().buffer, ptr, count); }

function compile(type, source) {
  const shader = gl.createShader(type);
  gl.shaderSource(shader, source);
  gl.compileShader(shader);
  if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
    const log = gl.getShaderInfoLog(shader);
    gl.deleteShader(shader);
    throw new Error("shader failed to compile: " + log);
  }
  return shader;
}

function program(vertexSource, fragmentSource, uniforms) {
  const p = gl.createProgram();
  const vs = compile(gl.VERTEX_SHADER, vertexSource);
  const fs = compile(gl.FRAGMENT_SHADER, fragmentSource);
  gl.attachShader(p, vs);
  gl.attachShader(p, fs);
  gl.linkProgram(p);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
    throw new Error("program failed to link: " + gl.getProgramInfoLog(p));
  }
  gl.detachShader(p, vs);
  gl.detachShader(p, fs);
  gl.deleteShader(vs);
  gl.deleteShader(fs);
  const locations = {};
  for (const name of uniforms) locations[name] = gl.getUniformLocation(p, name);
  return { program: p, u: locations };
}

const PRIMITIVE_VS = `#version 300 es
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec4 aModel0;
layout (location = 3) in vec4 aModel1;
layout (location = 4) in vec4 aModel2;
layout (location = 5) in vec4 aModel3;
layout (location = 6) in vec4 aColor;

uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vNormal;
out vec4 vColor;

void main() {
    mat4 model = mat4(aModel0, aModel1, aModel2, aModel3);
    vec4 worldPosition = model * vec4(aPosition, 1.0);
    mat3 rotationScale = mat3(model);
    vec3 squaredScale = vec3(
        dot(rotationScale[0], rotationScale[0]),
        dot(rotationScale[1], rotationScale[1]),
        dot(rotationScale[2], rotationScale[2]));
    vNormal = normalize(rotationScale * (aNormal / max(squaredScale, vec3(1e-8))));
    vColor = aColor;
    gl_Position = uProjection * uView * worldPosition;
}
`;

const PRIMITIVE_FS = `#version 300 es
precision highp float;
in vec3 vNormal;
in vec4 vColor;

uniform vec3 uLightDirection;
uniform float uAmbient;

out vec4 fragColor;

void main() {
    vec3 normal = normalize(vNormal);
    float diffuse = max(dot(normal, -uLightDirection), 0.0);
    float light = uAmbient + (1.0 - uAmbient) * diffuse;
    fragColor = vec4(vColor.rgb * light, vColor.a);
}
`;

const LABEL_VS = `#version 300 es
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec4 aColor;

uniform mat4 uView;
uniform mat4 uProjection;

out vec2 vTexCoord;
out vec4 vColor;

void main() {
    vTexCoord = aTexCoord;
    vColor = aColor;
    gl_Position = uProjection * uView * vec4(aPosition, 1.0);
}
`;

const LABEL_FS = `#version 300 es
precision highp float;
in vec2 vTexCoord;
in vec4 vColor;

uniform sampler2D uAtlas;

out vec4 fragColor;

void main() {
    vec4 glyph = texture(uAtlas, vTexCoord);
    if (glyph.a * vColor.a < 0.01) {
        discard;
    }
    fragColor = vec4(vColor.rgb, glyph.a * vColor.a);
}
`;

const IMGUI_VS = `#version 300 es
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aUV;
layout (location = 2) in vec4 aColor;
uniform mat4 uProjection;
out vec2 vUV;
out vec4 vColor;
void main() {
    vUV = aUV;
    vColor = aColor;
    gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
}
`;

const IMGUI_FS = `#version 300 es
precision mediump float;
in vec2 vUV;
in vec4 vColor;
uniform sampler2D uTexture;
out vec4 fragColor;
void main() {
    fragColor = vColor * texture(uTexture, vUV);
}
`;

export function init(targetCanvas, heapView) {
  canvas = targetCanvas;
  heap = heapView;
  gl = canvas.getContext("webgl2", {
    alpha: false,
    antialias: true,
    depth: true,
    stencil: false,
    premultipliedAlpha: false,
    preserveDrawingBuffer: false,
    powerPreference: "high-performance",
  });
  if (!gl) return null;

  primitive = program(PRIMITIVE_VS, PRIMITIVE_FS, ["uView", "uProjection", "uLightDirection", "uAmbient"]);
  label = program(LABEL_VS, LABEL_FS, ["uView", "uProjection", "uAtlas"]);
  label.vao = gl.createVertexArray();
  label.vbo = gl.createBuffer();
  label.ibo = gl.createBuffer();
  gl.bindVertexArray(label.vao);
  gl.bindBuffer(gl.ARRAY_BUFFER, label.vbo);
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, label.ibo);
  const labelStride = 9 * 4;
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 3, gl.FLOAT, false, labelStride, 0);
  gl.enableVertexAttribArray(1);
  gl.vertexAttribPointer(1, 2, gl.FLOAT, false, labelStride, 12);
  gl.enableVertexAttribArray(2);
  gl.vertexAttribPointer(2, 4, gl.FLOAT, false, labelStride, 20);
  gl.bindVertexArray(null);

  imgui = program(IMGUI_VS, IMGUI_FS, ["uProjection", "uTexture"]);
  imgui.vao = gl.createVertexArray();
  imgui.vbo = gl.createBuffer();
  imgui.ibo = gl.createBuffer();
  gl.bindVertexArray(imgui.vao);
  gl.bindBuffer(gl.ARRAY_BUFFER, imgui.vbo);
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, imgui.ibo);
  imguiAttributes(0);
  gl.bindVertexArray(null);

  return gl;
}

function imguiAttributes(vertexOffset) {
  const stride = 20;
  const base = vertexOffset * stride;
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 2, gl.FLOAT, false, stride, base);
  gl.enableVertexAttribArray(1);
  gl.vertexAttribPointer(1, 2, gl.FLOAT, false, stride, base + 8);
  gl.enableVertexAttribArray(2);
  gl.vertexAttribPointer(2, 4, gl.UNSIGNED_BYTE, true, stride, base + 16);
}

export function info() {
  if (!gl) return {};
  const debug = gl.getExtension("WEBGL_debug_renderer_info");
  return {
    version: gl.getParameter(gl.VERSION),
    renderer: debug ? gl.getParameter(debug.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER),
  };
}

// --- frame ---

export function beginFrame(r, g, b) {
  gl.viewport(0, 0, canvas.width, canvas.height);
  gl.disable(gl.SCISSOR_TEST);
  gl.depthMask(true);
  gl.clearColor(r, g, b, 1);
  gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
}

export function endFrame() {
  gl.disable(gl.SCISSOR_TEST);
  gl.bindVertexArray(null);
  if (sampleRequest) {
    const resolve = sampleRequest;
    sampleRequest = null;
    resolve(sampleNow());
  }
}

// --- primitives ---

export function createMesh(verticesPtr, vertexFloats, indicesPtr, indexCount) {
  const mesh = {
    vao: gl.createVertexArray(),
    vbo: gl.createBuffer(),
    ibo: gl.createBuffer(),
    instances: gl.createBuffer(),
    capacity: 0,
    indexCount,
  };
  gl.bindVertexArray(mesh.vao);
  gl.bindBuffer(gl.ARRAY_BUFFER, mesh.vbo);
  gl.bufferData(gl.ARRAY_BUFFER, f32(verticesPtr, vertexFloats), gl.STATIC_DRAW);
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, mesh.ibo);
  gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint32Array(u8().buffer, indicesPtr, indexCount), gl.STATIC_DRAW);
  const vertexStride = 6 * 4;
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 3, gl.FLOAT, false, vertexStride, 0);
  gl.enableVertexAttribArray(1);
  gl.vertexAttribPointer(1, 3, gl.FLOAT, false, vertexStride, 12);

  gl.bindBuffer(gl.ARRAY_BUFFER, mesh.instances);
  const instanceStride = 20 * 4;
  for (let column = 0; column < 4; column++) {
    gl.enableVertexAttribArray(2 + column);
    gl.vertexAttribPointer(2 + column, 4, gl.FLOAT, false, instanceStride, column * 16);
    gl.vertexAttribDivisor(2 + column, 1);
  }
  gl.enableVertexAttribArray(6);
  gl.vertexAttribPointer(6, 4, gl.FLOAT, false, instanceStride, 64);
  gl.vertexAttribDivisor(6, 1);
  gl.bindVertexArray(null);
  gl.bindBuffer(gl.ARRAY_BUFFER, null);
  meshes.push(mesh);
  return meshes.length - 1;
}

export function beginPrimitives(matricesPtr, lightX, lightY, lightZ, ambient) {
  gl.enable(gl.DEPTH_TEST);
  gl.depthFunc(gl.LESS);
  gl.enable(gl.CULL_FACE);
  gl.cullFace(gl.BACK);
  gl.frontFace(gl.CCW);
  gl.disable(gl.BLEND);
  gl.useProgram(primitive.program);
  // System.Numerics rows are GLSL columns: uploaded as they are.
  gl.uniformMatrix4fv(primitive.u.uView, false, f32(matricesPtr, 16));
  gl.uniformMatrix4fv(primitive.u.uProjection, false, f32(matricesPtr + 64, 16));
  gl.uniform3f(primitive.u.uLightDirection, lightX, lightY, lightZ);
  gl.uniform1f(primitive.u.uAmbient, ambient);
}

export function drawInstances(meshId, instancesPtr, instanceCount) {
  const mesh = meshes[meshId];
  const bytes = instanceCount * 80;
  gl.bindVertexArray(mesh.vao);
  gl.bindBuffer(gl.ARRAY_BUFFER, mesh.instances);
  if (bytes > mesh.capacity) {
    // Grown to the next power of two, so a world that gains one instance a
    // frame does not reallocate every frame.
    let capacity = Math.max(mesh.capacity, 80 * 1024);
    while (capacity < bytes) capacity *= 2;
    gl.bufferData(gl.ARRAY_BUFFER, capacity, gl.STREAM_DRAW);
    mesh.capacity = capacity;
  }
  gl.bufferSubData(gl.ARRAY_BUFFER, 0, u8(), instancesPtr, bytes);
  gl.drawElementsInstanced(gl.TRIANGLES, mesh.indexCount, gl.UNSIGNED_INT, 0, instanceCount);
}

// --- textures ---

export function createTexture(width, height, linear) {
  const texture = gl.createTexture();
  gl.bindTexture(gl.TEXTURE_2D, texture);
  gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, width, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
  const filter = linear ? gl.LINEAR : gl.NEAREST;
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, filter);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, filter);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  gl.bindTexture(gl.TEXTURE_2D, null);
  textures.push(texture);
  return textures.length - 1;
}

export function updateTexture(id, x, y, width, height, pixelsPtr, byteLength) {
  gl.bindTexture(gl.TEXTURE_2D, textures[id]);
  gl.pixelStorei(gl.UNPACK_ALIGNMENT, 1);
  gl.texSubImage2D(gl.TEXTURE_2D, 0, x, y, width, height, gl.RGBA, gl.UNSIGNED_BYTE,
    new Uint8Array(u8().buffer, pixelsPtr, byteLength));
  gl.pixelStorei(gl.UNPACK_ALIGNMENT, 4);
  gl.bindTexture(gl.TEXTURE_2D, null);
}

// --- labels ---

export function drawLabels(matricesPtr, textureId, verticesPtr, vertexCount, indicesPtr, indexCount) {
  gl.useProgram(label.program);
  gl.uniformMatrix4fv(label.u.uView, false, f32(matricesPtr, 16));
  gl.uniformMatrix4fv(label.u.uProjection, false, f32(matricesPtr + 64, 16));
  gl.uniform1i(label.u.uAtlas, 0);
  gl.disable(gl.DEPTH_TEST);
  gl.disable(gl.CULL_FACE);
  gl.enable(gl.BLEND);
  gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
  gl.activeTexture(gl.TEXTURE0);
  gl.bindTexture(gl.TEXTURE_2D, textures[textureId]);
  gl.bindVertexArray(label.vao);
  gl.bindBuffer(gl.ARRAY_BUFFER, label.vbo);
  gl.bufferData(gl.ARRAY_BUFFER, f32(verticesPtr, vertexCount * 9), gl.STREAM_DRAW);
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, label.ibo);
  gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint32Array(u8().buffer, indicesPtr, indexCount), gl.STREAM_DRAW);
  gl.drawElements(gl.TRIANGLES, indexCount, gl.UNSIGNED_INT, 0);
  gl.disable(gl.BLEND);
  gl.enable(gl.CULL_FACE);
  gl.enable(gl.DEPTH_TEST);
  gl.bindVertexArray(null);
}

// --- Dear ImGui ---

export function drawImGuiList(verticesPtr, vertexCount, indicesPtr, indexCount, commandsPtr, commandCount,
                              displayW, displayH, scale) {
  const fbH = canvas.height;
  gl.useProgram(imgui.program);
  const L = 0, R = displayW, T = 0, B = displayH;
  gl.uniformMatrix4fv(imgui.u.uProjection, false, new Float32Array([
    2 / (R - L), 0, 0, 0,
    0, 2 / (T - B), 0, 0,
    0, 0, -1, 0,
    (R + L) / (L - R), (T + B) / (B - T), 0, 1,
  ]));
  gl.uniform1i(imgui.u.uTexture, 0);
  gl.enable(gl.BLEND);
  gl.blendEquation(gl.FUNC_ADD);
  gl.blendFuncSeparate(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA, gl.ONE, gl.ONE_MINUS_SRC_ALPHA);
  gl.disable(gl.CULL_FACE);
  gl.disable(gl.DEPTH_TEST);
  gl.enable(gl.SCISSOR_TEST);
  gl.activeTexture(gl.TEXTURE0);

  gl.bindVertexArray(imgui.vao);
  gl.bindBuffer(gl.ARRAY_BUFFER, imgui.vbo);
  gl.bufferData(gl.ARRAY_BUFFER, new Uint8Array(u8().buffer, verticesPtr, vertexCount * 20), gl.STREAM_DRAW);
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, imgui.ibo);
  gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(u8().buffer, indicesPtr, indexCount), gl.STREAM_DRAW);

  const commands = f32(commandsPtr, commandCount * 8);
  let boundOffset = 0;
  for (let i = 0; i < commandCount; i++) {
    const c = i * 8;
    const x1 = commands[c] * scale, y1 = commands[c + 1] * scale;
    const x2 = commands[c + 2] * scale, y2 = commands[c + 3] * scale;
    if (x2 <= x1 || y2 <= y1) continue;
    const vertexOffset = commands[c + 5];
    if (vertexOffset !== boundOffset) {
      imguiAttributes(vertexOffset);
      boundOffset = vertexOffset;
    }
    gl.scissor(Math.floor(x1), Math.floor(fbH - y2), Math.ceil(x2 - x1), Math.ceil(y2 - y1));
    gl.bindTexture(gl.TEXTURE_2D, textures[commands[c + 4]] || null);
    gl.drawElements(gl.TRIANGLES, commands[c + 7], gl.UNSIGNED_SHORT, commands[c + 6] * 2);
  }
  if (boundOffset !== 0) imguiAttributes(0);
  gl.disable(gl.SCISSOR_TEST);
  gl.disable(gl.BLEND);
  gl.bindVertexArray(null);
}

// --- test hook: what is on screen, read right after a frame is drawn ---

function sampleNow() {
  const w = canvas.width, h = canvas.height;
  const colours = new Set();
  const pixel = new Uint8Array(4);
  const grid = 24;
  let nonSky = 0;
  for (let gy = 1; gy < grid; gy++) {
    for (let gx = 1; gx < grid; gx++) {
      gl.readPixels(Math.floor(w * gx / grid), Math.floor(h * gy / grid), 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, pixel);
      const key = (pixel[0] >> 3) + "," + (pixel[1] >> 3) + "," + (pixel[2] >> 3);
      colours.add(key);
      // The clear colour is (0.45, 0.65, 0.90): anything else is something drawn.
      if (Math.abs(pixel[0] - 115) > 6 || Math.abs(pixel[1] - 166) > 6 || Math.abs(pixel[2] - 230) > 6) nonSky++;
    }
  }
  return { width: w, height: h, samples: (grid - 1) * (grid - 1), distinctColours: colours.size, notClearColour: nonSky };
}

/** Resolves with pixel statistics of the next frame, read before it is presented. */
export function sampleNextFrame() {
  return new Promise((resolve) => { sampleRequest = resolve; });
}
