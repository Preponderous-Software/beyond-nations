#!/usr/bin/env bash
# Builds Strata for the browser with the Emscripten the .NET wasm-tools
# workload links with, and leaves the merged archive at <out>/strata.a.
#
#   build-strata.sh <commit> <sha256-of-source-tarball> <out-dir>
#
# Environment (set by native/strata.targets from the workload's packs):
#   BN_EMSCRIPTEN_SDK   .../Microsoft.NET.Runtime.Emscripten.<v>.Sdk.<rid>/<ver>/tools/
#   BN_EMSCRIPTEN_NODE  the node binary of the matching Node pack
#   BN_EMSCRIPTEN_CACHE the matching Cache pack's emscripten/cache directory
#   STRATA_CMAKE        a CMake >= 3.24 (default: cmake on PATH)
#
# Why this Emscripten and these options (Strata docs/WEB.md, "Beyond Nations"):
# objects from a newer Emscripten than the one .NET links with may not link;
# SDL3 is off because the page owns the canvas and Strata draws into its
# context (STRATA_SURFACE_HOST_CONTEXT); exceptions are wasm exceptions, the
# mode .NET 8 links the whole module with (WasmEnableExceptionHandling).
set -euo pipefail

commit="$1"
expected="$2"
out="$3"
cmake_bin="${STRATA_CMAKE:-cmake}"

work="$out/work"
mkdir -p "$work"
tarball="$work/strata-$commit.tar.gz"
if [ ! -s "$tarball" ]; then
    curl -fsSL -o "$tarball.part" "https://github.com/Preponderous-Software/strata/archive/$commit.tar.gz"
    mv "$tarball.part" "$tarball"
fi
actual="$(sha256sum "$tarball" | cut -d' ' -f1)"
if [ "$actual" != "$expected" ]; then
    rm -f "$tarball"
    echo "error: Strata $commit has SHA-256 $actual, expected $expected; the download was deleted." >&2
    exit 1
fi
src="$work/strata-$commit"
[ -d "$src" ] || tar -xzf "$tarball" -C "$work"

export DOTNET_EMSCRIPTEN_LLVM_ROOT="${BN_EMSCRIPTEN_SDK}bin"
export DOTNET_EMSCRIPTEN_BINARYEN_ROOT="${BN_EMSCRIPTEN_SDK}"
export DOTNET_EMSCRIPTEN_NODE_JS="$BN_EMSCRIPTEN_NODE"
export EM_CACHE="$BN_EMSCRIPTEN_CACHE"
export EM_FROZEN_CACHE=True
export PATH="${BN_EMSCRIPTEN_SDK}emscripten:${BN_EMSCRIPTEN_SDK}bin:$PATH"

"${BN_EMSCRIPTEN_SDK}emscripten/emcmake" "$cmake_bin" -S "$src" -B "$work/build" \
    -DCMAKE_BUILD_TYPE=Release \
    -DSTRATA_WITH_SDL=OFF \
    -DSTRATA_WASM_EXCEPTIONS=wasm \
    -DSTRATA_BUILD_TESTS=OFF -DSTRATA_BUILD_TOOLS=OFF -DSTRATA_BUILD_EXAMPLES=OFF \
    -DSTRATA_WITH_SIM=OFF -Wno-dev
"$cmake_bin" --build "$work/build" -j "${STRATA_BUILD_JOBS:-4}"
# The file name is the P/Invoke module name: [DllImport("strata")].
cp "$work/build/libstrata.a" "$out/strata.a"
echo "Strata $commit for wasm: $out/strata.a"
