using System;
using Silk.NET.OpenGL;

namespace beyondnations.desktop.render {

    /**
    * The one shader the renderer uses, written inline because the project has no
    * asset pipeline to load it from and does not need one for two dozen lines of
    * GLSL.
    *
    * A vertex carries a position and a normal; an instance carries a model
    * matrix and a colour. Lighting is a single directional term plus ambient,
    * which is enough to tell one face of a cube from another and is all the old
    * Unity build ever showed either.
    *
    * The model matrix arrives as four vec4 attributes because a mat4 attribute
    * occupies four slots. System.Numerics stores its matrices in the row order
    * that makes each of those attributes a column here, so the matrix uploaded
    * is used as-is and multiplies on the left, with no transposing anywhere.
    */
    public class InstancedPrimitiveShader : IDisposable {
        public const uint AttributePosition = 0;
        public const uint AttributeNormal = 1;
        public const uint AttributeModelColumn0 = 2;
        public const uint AttributeColor = 6;

        private const string VertexSource = @"#version 330 core

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

    // Instances are scaled and translated, never sheared, so the correct
    // normal matrix is the rotation part divided by the scale -- which is
    // what dividing by the squared column lengths comes to. Cheaper than
    // inverting a matrix per vertex, and still right if rotation is added.
    mat3 rotationScale = mat3(model);
    vec3 squaredScale = vec3(
        dot(rotationScale[0], rotationScale[0]),
        dot(rotationScale[1], rotationScale[1]),
        dot(rotationScale[2], rotationScale[2]));
    vNormal = normalize(rotationScale * (aNormal / max(squaredScale, vec3(1e-8))));

    vColor = aColor;
    gl_Position = uProjection * uView * worldPosition;
}
";

        private const string FragmentSource = @"#version 330 core

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
";

        private readonly GL gl;
        private readonly uint program;
        private readonly int viewLocation;
        private readonly int projectionLocation;
        private readonly int lightDirectionLocation;
        private readonly int ambientLocation;

        public InstancedPrimitiveShader(GL gl) {
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
                throw new InvalidOperationException("the primitive shader failed to link: " + log);
            }

            gl.DetachShader(program, vertexShader);
            gl.DetachShader(program, fragmentShader);
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);

            // Looked up once. Uniform names are strings, and looking them up per
            // frame would allocate in the render loop for no reason.
            viewLocation = gl.GetUniformLocation(program, "uView");
            projectionLocation = gl.GetUniformLocation(program, "uProjection");
            lightDirectionLocation = gl.GetUniformLocation(program, "uLightDirection");
            ambientLocation = gl.GetUniformLocation(program, "uAmbient");
        }

        public uint getProgram() { return program; }
        public int getViewLocation() { return viewLocation; }
        public int getProjectionLocation() { return projectionLocation; }
        public int getLightDirectionLocation() { return lightDirectionLocation; }
        public int getAmbientLocation() { return ambientLocation; }

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
                throw new InvalidOperationException("the " + type + " failed to compile: " + log);
            }
            return shader;
        }

        public void Dispose() {
            gl.DeleteProgram(program);
        }
    }
}
