﻿using AerialRace.Debugging;
using AerialRace.Loading;
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AerialRace.DebugGui
{
    struct TextureRef
    {
        public Texture Texture;
        public float Level;
        public int Layer;

        public TextureRef(Texture tex, float level = -1, int layer = -1)
        {
            Texture = tex;
            Level = level;
            Layer = layer;
        }
    }

    /// <summary>
    /// A modified version of Veldrid.ImGui's ImGuiRenderer.
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
    /// </summary>
    class ImGuiController
    {
        readonly static List<TextureRef> PermanentRefs = new List<TextureRef>();
        readonly static List<TextureRef> TextureRefs = new List<TextureRef>();
        public static int ReferenceTexture(Texture texture, float level = -1)
        {
            if (texture.Type != TextureType.Texture2D)
                Debug.WriteLine($"Referenced ImGui texture '{texture.Name}' is not of type Texture2D, it will probably not render correctly!");
            TextureRefs.Add(new TextureRef(texture, level));
            return PermanentRefs.Count + TextureRefs.Count;
        }

        public static int ReferenceTextureArray(Texture texture, float level = -1, int layer = -1)
        {
            if (texture.Type != TextureType.Texture2DArray)
                Debug.WriteLine($"Referenced ImGui texture '{texture.Name}' is not of type Texture2DArray, it will probably not render correctly!");
            TextureRefs.Add(new TextureRef(texture, level, layer));
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

out gl_PerVertex
{
    vec4 gl_Position;
};

out vec4 color;
out vec2 texCoord;

void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";
            string FragmentSource = @"#version 400 core

uniform sampler2D in_texture;
uniform sampler2DArray in_arrayTexture;

in vec4 color;
in vec2 texCoord;

out vec4 outputColor;

uniform bool useLod;
uniform float lodLevel;

uniform bool useTextureArray;
uniform int layer;

void main()
{
    if (useLod && useTextureArray)
    {
        outputColor = color * textureLod(in_arrayTexture, vec3(texCoord, layer), lodLevel);
    }
    else if (useTextureArray)
    {
        outputColor = color * texture(in_arrayTexture, vec3(texCoord, layer));
    }
    else if (useLod)
    {
        outputColor = color * textureLod(in_texture, texCoord, lodLevel);
    }
    else
    {
        outputColor = color * texture(in_texture, texCoord);
    }
}";

            var vertexShader = ShaderCompiler.CompileProgramFromSource("ImGui Vertex", ShaderStage.Vertex, VertexSource);
            var fragmentShader = ShaderCompiler.CompileProgramFromSource("ImGui Fragment", ShaderStage.Fragment, FragmentSource);
            _shader = ShaderCompiler.CompilePipeline("ImGui", vertexShader, fragmentShader);

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

            _fontTexture = new Texture("ImGui Text Atlas", glTexture, TextureType.Texture2D, TextureFormat.Rgba8, width, height, 1, 0, 0, 1, 1, false);

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

                io.AddKeyEvent(TranslateKey(key), KeyboardState.IsKeyDown(key));
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

        public static ImGuiKey TranslateKey(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
                return key - Keys.D0 + ImGuiKey._0;

            if (key >= Keys.A && key <= Keys.Z)
                return key - Keys.A + ImGuiKey.A;

            if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
                return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

            if (key >= Keys.F1 && key <= Keys.F24)
                return key - Keys.F1 + ImGuiKey.F24;

            switch (key)
            {
                case Keys.Tab: return ImGuiKey.Tab;
                case Keys.Left: return ImGuiKey.LeftArrow;
                case Keys.Right: return ImGuiKey.RightArrow;
                case Keys.Up: return ImGuiKey.UpArrow;
                case Keys.Down: return ImGuiKey.DownArrow;
                case Keys.PageUp: return ImGuiKey.PageUp;
                case Keys.PageDown: return ImGuiKey.PageDown;
                case Keys.Home: return ImGuiKey.Home;
                case Keys.End: return ImGuiKey.End;
                case Keys.Insert: return ImGuiKey.Insert;
                case Keys.Delete: return ImGuiKey.Delete;
                case Keys.Backspace: return ImGuiKey.Backspace;
                case Keys.Space: return ImGuiKey.Space;
                case Keys.Enter: return ImGuiKey.Enter;
                case Keys.Escape: return ImGuiKey.Escape;
                case Keys.Apostrophe: return ImGuiKey.Apostrophe;
                case Keys.Comma: return ImGuiKey.Comma;
                case Keys.Minus: return ImGuiKey.Minus;
                case Keys.Period: return ImGuiKey.Period;
                case Keys.Slash: return ImGuiKey.Slash;
                case Keys.Semicolon: return ImGuiKey.Semicolon;
                case Keys.Equal: return ImGuiKey.Equal;
                case Keys.LeftBracket: return ImGuiKey.LeftBracket;
                case Keys.Backslash: return ImGuiKey.Backslash;
                case Keys.RightBracket: return ImGuiKey.RightBracket;
                case Keys.GraveAccent: return ImGuiKey.GraveAccent;
                case Keys.CapsLock: return ImGuiKey.CapsLock;
                case Keys.ScrollLock: return ImGuiKey.ScrollLock;
                case Keys.NumLock: return ImGuiKey.NumLock;
                case Keys.PrintScreen: return ImGuiKey.PrintScreen;
                case Keys.Pause: return ImGuiKey.Pause;
                case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
                case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
                case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
                case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
                case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
                case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
                case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
                case Keys.LeftShift: return ImGuiKey.LeftShift;
                case Keys.LeftControl: return ImGuiKey.LeftCtrl;
                case Keys.LeftAlt: return ImGuiKey.LeftAlt;
                case Keys.LeftSuper: return ImGuiKey.LeftSuper;
                case Keys.RightShift: return ImGuiKey.RightShift;
                case Keys.RightControl: return ImGuiKey.RightCtrl;
                case Keys.RightAlt: return ImGuiKey.RightAlt;
                case Keys.RightSuper: return ImGuiKey.RightSuper;
                case Keys.Menu: return ImGuiKey.Menu;
                default: return ImGuiKey.None;
            }
        }

        internal void PressChar(char keyChar)
        {
            PressedChars.Add(keyChar);
        }

        private void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdLists[i];

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
            RenderDataUtil.Uniform1("in_texture", ShaderStage.Fragment, 0);
            RenderDataUtil.Uniform1("in_arrayTexture", ShaderStage.Fragment, 1);

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
                ImDrawListPtr cmd_list = draw_data.CmdLists[n];

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

                        if (textureRef?.Texture.Type == TextureType.Texture2DArray)
                        {
                            RenderDataUtil.Uniform1("useTextureArray", ShaderStage.Fragment, 1);
                            RenderDataUtil.Uniform1("layer", ShaderStage.Fragment, textureRef?.Layer ?? 0);
                            RenderDataUtil.BindTexture(1, textureRef?.Texture);
                        }
                        else
                        {
                            RenderDataUtil.Uniform1("useTextureArray", ShaderStage.Fragment, 0);
                            RenderDataUtil.BindTexture(0, textureRef?.Texture);
                        }

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
                            GL.DrawElements(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                        }
                    }

                    idx_offset += (int)pcmd.ElemCount;
                }
                vtx_offset += cmd_list.VtxBuffer.Size;
            }

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ScissorTest);
        }
    }
}
