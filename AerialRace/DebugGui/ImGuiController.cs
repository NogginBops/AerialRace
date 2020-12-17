using AerialRace.RenderData;
using ImGuiNET;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AerialRace.DebugGui
{
    struct TextureRef
    {
        public Texture Texture;
        public float Level;

        public TextureRef(Texture tex, float level = -1)
        {
            Texture = tex;
            Level = level;
        }
    }

    /// <summary>
    /// A modified version of Veldrid.ImGui's ImGuiRenderer.
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
    /// </summary>
    class ImGuiController
    {
        static List<TextureRef> PermanentRefs = new List<TextureRef>();
        static List<TextureRef> TextureRefs = new List<TextureRef>();
        public static int ReferenceTexture(Texture texture, float level = -1)
        {
            TextureRefs.Add(new TextureRef(texture, level));
            return PermanentRefs.Count + TextureRefs.Count;
        }

        private bool _frameBegun;

        //private int _vertexArray;
        private RenderData.Buffer VertexBuffer;
        private RenderData.IndexBuffer IndexBuffer;

        private Texture _fontTexture;
        private Sampler _textureSampler;

        private ShaderPipeline _shader;

        private AttributeSpecification PositionAttribSpec;
        private AttributeSpecification UVAttribSpec;
        private AttributeSpecification ColorAttribSpec;

        private int _windowWidth;
        private int _windowHeight;

        private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            var io = ImGui.GetIO();
            io.Fonts.AddFontDefault();

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            CreateDeviceResources();
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();
            _frameBegun = true;
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        public void CreateDeviceResources()
        {
            VertexBuffer = RenderDataUtil.CreateDataBuffer<ImDrawVert>("ImGui", 10000, BufferFlags.Dynamic);
            IndexBuffer = RenderDataUtil.CreateIndexBuffer<ushort>("ImGui", 2000, BufferFlags.Dynamic);

            RecreateFontDeviceTexture();

            string VertexSource = @"#version 330 core

uniform mat4 projection_matrix;

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;

out vec4 color;
out vec2 texCoord;

void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";
            string FragmentSource = @"#version 400 core

uniform sampler2D in_fontTexture;

in vec4 color;
in vec2 texCoord;

out vec4 outputColor;

uniform bool useLod;
uniform float lodLevel;

void main()
{
    if (useLod)
    {
        outputColor = color * textureLod(in_fontTexture, texCoord, lodLevel);
    }
    else
    {
        outputColor = color * texture(in_fontTexture, texCoord);
    }
}";

            RenderDataUtil.CreateShaderProgram("ImGui Vertex", ShaderStage.Vertex, VertexSource, out var vertexShader);
            RenderDataUtil.CreateShaderProgram("ImGui Fragment", ShaderStage.Fragment, FragmentSource, out var fragmentShader);

            _shader = RenderDataUtil.CreateEmptyPipeline("ImGui");
            RenderDataUtil.AssembleProgramPipeline(_shader, vertexShader, null, fragmentShader);

            PositionAttribSpec = new AttributeSpecification("ImGui Position", 2, RenderData.AttributeType.Float, false, 0);
            UVAttribSpec =       new AttributeSpecification("ImGui UV",       2, RenderData.AttributeType.Float, false, 8);
            ColorAttribSpec =    new AttributeSpecification("ImGui Color",    4, RenderData.AttributeType.UInt8, true,  16);
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            _textureSampler = RenderDataUtil.CreateSampler2D("ImGui Font Sampler", MagFilter.Nearest, MinFilter.LinearMipmapLinear, 1.0f, WrapMode.Repeat, WrapMode.Repeat);

            GLUtil.CreateTexture("ImGui Text Atlas", TextureTarget.Texture2D, out var glTexture);

            GL.TextureStorage2D(glTexture, 1, SizedInternalFormat.Rgba8, width, height);
            GL.TextureSubImage2D(glTexture, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

            _fontTexture = new Texture("ImGui Text Atlas", glTexture, TextureType.Texture2D, TextureFormat.Rgba8, width, height, 1, 0, 0, 0);

            // Add this as a permanent texture reference
            PermanentRefs.Add(new TextureRef(_fontTexture));

            io.Fonts.SetTexID((IntPtr)PermanentRefs.Count);

            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
        /// or index data has increased beyond the capacity of the existing buffers.
        /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
        /// </summary>
        public void Render()
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData());
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(GameWindow wnd, float deltaSeconds)
        {
            if (_frameBegun)
            {
                ImGui.Render();
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(wnd);

            TextureRefs.Clear();

            _frameBegun = true;
            ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(
                _windowWidth / _scaleFactor.X,
                _windowHeight / _scaleFactor.Y);
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        readonly List<char> PressedChars = new List<char>();

        private void UpdateImGuiInput(GameWindow wnd)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            MouseState MouseState = wnd.MouseState;
            KeyboardState KeyboardState = wnd.KeyboardState;

            io.MouseDown[0] = MouseState.IsButtonDown(MouseButton.Left);
            io.MouseDown[1] = MouseState.IsButtonDown(MouseButton.Right);
            io.MouseDown[2] = MouseState.IsButtonDown(MouseButton.Middle);

            io.MousePos = new System.Numerics.Vector2(MouseState.Position.X, MouseState.Position.Y);

            io.MouseWheel = MouseState.ScrollDelta.Y;
            io.MouseWheelH = MouseState.ScrollDelta.X;

            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key == Keys.Unknown) continue;

                io.KeysDown[(int)key] = KeyboardState.IsKeyDown(key);
            }

            foreach (var c in PressedChars)
            {
                io.AddInputCharacter(c);
            }
            PressedChars.Clear();

            io.KeyCtrl = KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl);
            io.KeyAlt = KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt);
            io.KeyShift = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
            io.KeySuper = KeyboardState.IsKeyDown(Keys.LeftSuper) || KeyboardState.IsKeyDown(Keys.RightSuper);
        }

        internal void PressChar(char keyChar)
        {
            PressedChars.Add(keyChar);
        }

        private static void SetKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
        }

        private void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                int vertexSize = cmd_list.VtxBuffer.Size;
                if (vertexSize > VertexBuffer.Elements)
                {
                    int newSize = (int)Math.Max(VertexBuffer.Elements * 1.5f, vertexSize);
                    RenderDataUtil.ReallocBuffer(ref VertexBuffer, newSize);

                    Console.WriteLine($"Resized dear imgui vertex buffer to new size {VertexBuffer.SizeInBytes}");
                }

                int indexSize = cmd_list.IdxBuffer.Size;
                if (indexSize > IndexBuffer.Elements)
                {
                    int newSize = (int)Math.Max(IndexBuffer.Elements * 1.5f, indexSize);
                    RenderDataUtil.ReallocBuffer(ref IndexBuffer, newSize);

                    Console.WriteLine($"Resized dear imgui index buffer to new size {IndexBuffer.SizeInBytes}");
                }
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
                0.0f,
                io.DisplaySize.X,
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);

            RenderDataUtil.UsePipeline(_shader);

            RenderDataUtil.UniformMatrix4("projection_matrix", ShaderStage.Vertex, false, ref mvp);
            RenderDataUtil.Uniform1("in_fontTexture", ShaderStage.Fragment, 0);

            // Set up vertex attributes and their buffers plus index buffer

            RenderDataUtil.BindIndexBuffer(IndexBuffer);

            RenderDataUtil.BindVertexAttribBuffer(0, VertexBuffer);

            RenderDataUtil.SetAndEnableVertexAttribute(0, PositionAttribSpec);
            RenderDataUtil.SetAndEnableVertexAttribute(1, UVAttribSpec);
            RenderDataUtil.SetAndEnableVertexAttribute(2, ColorAttribSpec);

            RenderDataUtil.LinkAttributeBuffer(0, 0);
            RenderDataUtil.LinkAttributeBuffer(1, 0);
            RenderDataUtil.LinkAttributeBuffer(2, 0);

            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            RenderDataUtil.SetDepthTesting(false);

            RenderDataUtil.BindSampler(0, _textureSampler);

            // Render command lists
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];

                GL.NamedBufferSubData(VertexBuffer.Handle, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
                
                GL.NamedBufferSubData(IndexBuffer.Handle, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);
                
                int vtx_offset = 0;
                int idx_offset = 0;

                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        int texRefIndex = (int)pcmd.TextureId;
                        TextureRef? textureRef = null;
                        if (texRefIndex == 0)
                        {
                            // No texture!
                        }
                        else if (texRefIndex <= PermanentRefs.Count)
                        {
                            textureRef = PermanentRefs[texRefIndex - 1];
                        }
                        else if (texRefIndex <= PermanentRefs.Count + TextureRefs.Count)
                        {
                            textureRef = TextureRefs[texRefIndex - PermanentRefs.Count - 1];
                        }
                        else
                        {
                            throw new Exception();
                        }

                        RenderDataUtil.BindTexture(0, textureRef?.Texture);

                        if (textureRef is TextureRef @ref && @ref.Level != -1)
                        {
                            RenderDataUtil.Uniform1("useLod", ShaderStage.Fragment, 1);
                            RenderDataUtil.Uniform1("lodLevel", ShaderStage.Fragment, @ref.Level);
                        }
                        else
                        {
                            RenderDataUtil.Uniform1("useLod", ShaderStage.Fragment, 0);
                        }

                        // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                        var clip = pcmd.ClipRect;
                        RenderDataUtil.SetScissor(new Recti((int)clip.X, (int)clip.Y, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y)));
                        
                        if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                        {
                            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(idx_offset * sizeof(ushort)), vtx_offset);
                        }
                        else
                        {
                            GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                        }
                    }

                    idx_offset += (int)pcmd.ElemCount;
                }
                vtx_offset += cmd_list.VtxBuffer.Size;
            }

            RenderDataUtil.BindTextureUnsafe(0, 0);

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ScissorTest);
        }
    }
}
