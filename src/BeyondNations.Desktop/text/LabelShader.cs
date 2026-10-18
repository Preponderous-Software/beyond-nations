using System;
using Silk.NET.OpenGL;

namespace beyondnations.desktop.text {

    /**
    * The shader that draws glyph quads.
    *
    * Nothing is billboarded here. The CPU has already placed every corner in
    * world space along the camera's right and up axes, which is what lets every
    * glyph of every label share one vertex buffer and one draw call; a shader
    * that billboarded per instance would need the label origin as an instance
    * attribute and would gain nothing.
    *
    * So all that is left is a transform, a texture lookup and a tint. The
    * atlas holds white glyphs with coverage in the alpha channel, so the colour
    * comes entirely from the vertex and the alpha is the product of the two.
    */
    public class LabelShader : IDisposable {
        public const uint AttributePosition = 0;
        public const uint AttributeTexCoord = 1;
        public const uint AttributeColor = 2;

        private const string VertexSource = @"#version 330 core

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
";

        private const string FragmentSource = @"#version 330 core

in vec2 vTexCoord;
in vec4 vColor;

uniform sampler2D uAtlas;

out vec4 fragColor;

void main() {
    vec4 glyph = texture(uAtlas, vTexCoord);

    // Fully transparent texels are the space between glyphs in the atlas.
    // Discarding them rather than blending them keeps a label from laying a
    // faint rectangle over whatever is behind it when the blend mode is not
    // exactly what this shader expects.
    if (glyph.a * vColor.a < 0.01) {
        discard;
    }

    fragColor = vec4(vColor.rgb, glyph.a * vColor.a);
}
";

        private readonly GL gl;
        private readonly uint program;
        private readonly int viewLocation;
        private readonly int projectionLocation;
        private readonly int atlasLocation;

        public LabelShader(GL gl) {
            this.gl = gl;

            uint vertexShader = compile(ShaderType.VertexShader, VertexSource);
            uint fragmentShader = compile(ShaderType.FragmentShader, FragmentSource);

            program = gl.CreateProgram();
            gl.AttachShader(program, vertexShader);
            gl.AttachShader(program, fragmentShader);
            gl.LinkProgram(program);

            int linked;
            gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out linked);
            if (linked == 0) {
                string log = gl.GetProgramInfoLog(program);
                throw new InvalidOperationException("the label shader failed to link: " + log);
            }

            gl.DetachShader(program, vertexShader);
            gl.DetachShader(program, fragmentShader);
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);

            viewLocation = gl.GetUniformLocation(program, "uView");
            projectionLocation = gl.GetUniformLocation(program, "uProjection");
            atlasLocation = gl.GetUniformLocation(program, "uAtlas");
        }

        public uint getProgram() { return program; }
        public int getViewLocation() { return viewLocation; }
        public int getProjectionLocation() { return projectionLocation; }
        public int getAtlasLocation() { return atlasLocation; }

        public void use() {
            gl.UseProgram(program);
        }

        private uint compile(ShaderType type, string source) {
            uint shader = gl.CreateShader(type);
            gl.ShaderSource(shader, source);
            gl.CompileShader(shader);

            int compiled;
            gl.GetShader(shader, ShaderParameterName.CompileStatus, out compiled);
            if (compiled == 0) {
                string log = gl.GetShaderInfoLog(shader);
                gl.DeleteShader(shader);
                throw new InvalidOperationException("the label " + type + " failed to compile: " + log);
            }
            return shader;
        }

        public void Dispose() {
            gl.DeleteProgram(program);
        }
    }
}
