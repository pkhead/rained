// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Core.Native;

namespace Glib.ImGui
{
    public class ImGuiController : IDisposable
    {
        private Glib.Window _window;
        private Glib.RenderContext _rctx;

        private bool _frameBegun;
        private static readonly Dictionary<Glib.Key, ImGuiKey> keyMap = new Dictionary<Glib.Key, ImGuiKey>();
        //private ImGuiViewports _viewports;

        private Glib.Texture _fontTexture;
        public Glib.Texture FontTexture => _fontTexture;
        private Glib.Shader _shader;
        private Glib.Mesh _drawMesh;
        private List<Glib.Texture> _textures = [];

        private int _windowWidth;
        private int _windowHeight;

        public IntPtr Context;

        public bool ignoreMouseUp = false;

        private unsafe delegate byte* GetClipTextCallback(IntPtr userData);
        private unsafe delegate void SetClipTextCallback(IntPtr userData, byte* text);
 
        private static GetClipTextCallback _getClipCallback = null!;
        private static SetClipTextCallback _setClipCallback = null!;
        private static unsafe byte* _activeClipboardBuffer = null;

        private unsafe void SetClipText(IntPtr userData, byte* text)
        {
            try
            {
                _window.SilkInputContext.Keyboards[0].ClipboardText = SilkMarshal.PtrToString((nint) text);
            }
            catch
            {
            }
        }

        private unsafe byte* GetClipText(IntPtr userData)
        {
            if (_activeClipboardBuffer != null)
                SilkMarshal.Free((nint)_activeClipboardBuffer);
            
            try
            {
                _activeClipboardBuffer = (byte*) SilkMarshal.StringToPtr(_window.SilkInputContext.Keyboards[0].ClipboardText);
            }
            catch
            {
                _activeClipboardBuffer = (byte*) SilkMarshal.Allocate(1);
                _activeClipboardBuffer[0] = 0;
            }
            
            return _activeClipboardBuffer;
        }

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController(Glib.Window window) : this(window, null, null)
        {
        }

        /// <summary>
        /// Constructs a new ImGuiController with font configuration.
        /// </summary>
        public ImGuiController(Glib.Window window, ImGuiFontConfig imGuiFontConfig) : this(window, imGuiFontConfig, null)
        {
        }

        /// <summary>
        /// Constructs a new ImGuiController with an onConfigureIO Action.
        /// </summary>
        public ImGuiController(Glib.Window window, Action onConfigureIO) : this(window, null, onConfigureIO)
        {
        }

        /// <summary>
        /// Constructs a new ImGuiController with font configuration and onConfigure Action.
        /// </summary>
        public ImGuiController(Glib.Window window, ImGuiFontConfig? imGuiFontConfig = null, Action onConfigureIO = null)
        {
            Init(window);

            var io = ImGuiNET.ImGui.GetIO();
            if (imGuiFontConfig is not null)
            {
                var glyphRange = imGuiFontConfig.Value.GetGlyphRange?.Invoke(io) ?? default(IntPtr);

                io.Fonts.AddFontFromFileTTF(imGuiFontConfig.Value.FontPath, imGuiFontConfig.Value.FontSize, null, glyphRange);
            }

            onConfigureIO?.Invoke();

            if (Mesh.IsBaseVertexSupported)
                io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            
            unsafe
            {
                _getClipCallback = new GetClipTextCallback(GetClipText);
                _setClipCallback = new SetClipTextCallback(SetClipText);

                io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_setClipCallback);
                io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_getClipCallback);
            }
            //io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
            //io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;

            CreateDeviceResources();
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            _textures.Clear();
            _textures.Add(_fontTexture);

            //_viewports = null;
            if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
                //_viewports = new ImGuiViewports(this, gl, _view);

            BeginFrame();
        }

        public void MakeCurrent()
        {
            ImGuiNET.ImGui.SetCurrentContext(Context);
        }

        private void Init(Glib.Window window)
        {
            _window = window;
            _rctx = RenderContext.Instance!;
            _windowWidth = window.Width;
            _windowHeight = window.Height;

            Context = ImGuiNET.ImGui.CreateContext();
            ImGuiNET.ImGui.SetCurrentContext(Context);
            ImGuiNET.ImGui.StyleColorsDark();

            _window.Resize += WindowResized;
            _window.KeyChar += OnKeyChar;
            _window.KeyDown += OnKeyDown;
            _window.KeyUp += OnKeyUp;

            _window.MouseDown += (Glib.MouseButton button) =>
            {
                var io = ImGuiNET.ImGui.GetIO();

                switch (button)
                {
                    case Glib.MouseButton.Left:
                        io.AddMouseButtonEvent(0, true);
                        break;

                    case Glib.MouseButton.Right:
                        io.AddMouseButtonEvent(1, true);
                        break;

                    case Glib.MouseButton.Middle:
                        io.AddMouseButtonEvent(2, true);
                        break;
                };
            };

            _window.MouseUp += (Glib.MouseButton button) =>
            {
                if (ignoreMouseUp)
                {
                    ignoreMouseUp = false;
                    return;
                }

                var io = ImGuiNET.ImGui.GetIO();

                switch (button)
                {
                    case Glib.MouseButton.Left:
                        io.AddMouseButtonEvent(0, false);
                        break;

                    case Glib.MouseButton.Right:
                        io.AddMouseButtonEvent(1, false);
                        break;

                    case Glib.MouseButton.Middle:
                        io.AddMouseButtonEvent(2, false);
                        break;
                };
            };
        }

        private void BeginFrame()
        {
            ImGuiNET.ImGui.NewFrame();
            _frameBegun = true;
        }

        private void OnKeyChar(char k)
        {
            var io = ImGuiNET.ImGui.GetIO();
            io.AddInputCharacter(k);
        }

        private void OnKeyDown(Glib.Key key, int code)
        {
            AddKeyEvent(ImGuiNET.ImGui.GetIO(), key, true);
        }

        private void OnKeyUp(Glib.Key key, int code)
        {
            AddKeyEvent(ImGuiNET.ImGui.GetIO(), key, false);
        }

        private void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        private ImGuiMouseCursor lastCursorMode = ImGuiMouseCursor.Arrow;

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
                var oldCtx = ImGuiNET.ImGui.GetCurrentContext();

                if (oldCtx != Context)
                {
                    ImGuiNET.ImGui.SetCurrentContext(Context);
                }

                _frameBegun = false;

                ImGuiNET.ImGui.Render();

                // update mouse
                var io = ImGuiNET.ImGui.GetIO();
                if (!io.ConfigFlags.HasFlag(ImGuiConfigFlags.NoMouseCursorChange))
                {
                    var imguiCursor = ImGuiNET.ImGui.GetMouseCursor();

                    if (imguiCursor != lastCursorMode)
                    {
                        lastCursorMode = imguiCursor;
                        
                        if (io.MouseDrawCursor || imguiCursor == ImGuiMouseCursor.None)
                        {
                            _window.MouseMode = Glib.MouseMode.Normal;
                        }
                        else
                        {
                            _window.MouseMode = Glib.MouseMode.Normal;
                            var newCursor = imguiCursor switch
                            {
                                ImGuiMouseCursor.Arrow => Glib.MouseCursorIcon.Arrow,
                                ImGuiMouseCursor.TextInput => Glib.MouseCursorIcon.IBeam,
                                ImGuiMouseCursor.ResizeAll => Glib.MouseCursorIcon.ResizeAll,
                                ImGuiMouseCursor.ResizeNS => Glib.MouseCursorIcon.VResize,
                                ImGuiMouseCursor.ResizeEW => Glib.MouseCursorIcon.HResize,
                                ImGuiMouseCursor.ResizeNESW => Glib.MouseCursorIcon.NeswResize,
                                ImGuiMouseCursor.ResizeNWSE => Glib.MouseCursorIcon.NwseResize,
                                ImGuiMouseCursor.Hand => Glib.MouseCursorIcon.Hand,
                                ImGuiMouseCursor.NotAllowed => Glib.MouseCursorIcon.NotAllowed,
                                _ => Glib.MouseCursorIcon.Arrow
                            };

                            if (newCursor != _window.CursorIcon)
                            {
                                _window.CursorIcon = newCursor;
                            }
                        }
                    }
                }

                RenderImDrawData(ImGuiNET.ImGui.GetDrawData());

                // reset texture cache
                _textures.Clear();
                _textures.Add(_fontTexture);

                // update and render additional platform windows
                if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
                {
                    ImGuiNET.ImGui.UpdatePlatformWindows();
                    ImGuiNET.ImGui.RenderPlatformWindowsDefault();
                    _window.MakeCurrent();
                }

                if (oldCtx != Context)
                {
                    ImGuiNET.ImGui.SetCurrentContext(oldCtx);
                }
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds)
        {
            var oldCtx = ImGuiNET.ImGui.GetCurrentContext();

            if (oldCtx != Context)
            {
                ImGuiNET.ImGui.SetCurrentContext(Context);
            }

            if (_frameBegun)
            {
                ImGuiNET.ImGui.Render();
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput();

            _frameBegun = true;
            ImGuiNET.ImGui.NewFrame();

            if (oldCtx != Context)
            {
                ImGuiNET.ImGui.SetCurrentContext(oldCtx);
            }
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            var io = ImGuiNET.ImGui.GetIO();
            io.DisplaySize = new Vector2(_windowWidth, _windowHeight);

            if (_windowWidth > 0 && _windowHeight > 0)
            {
                io.DisplayFramebufferScale = new Vector2(_window.PixelWidth / _windowWidth,
                    _window.PixelHeight / _windowHeight);
            }

            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        internal void AddKeyEvent(ImGuiIOPtr io, Glib.Key k, bool down)
        {
            // Rained-specific modification:
            // ImGui ignores the tab key
            if (k == Glib.Key.Tab) return;

            if (keyMap.TryGetValue(k, out ImGuiKey imKey))
            {
                io.AddKeyEvent(ImGuiKey.ModCtrl, _window.IsKeyDown(Glib.Key.ControlLeft) || _window.IsKeyDown(Glib.Key.ControlRight));
                io.AddKeyEvent(ImGuiKey.ModAlt, _window.IsKeyDown(Glib.Key.AltLeft) || _window.IsKeyDown(Glib.Key.AltRight));
                io.AddKeyEvent(ImGuiKey.ModShift, _window.IsKeyDown(Glib.Key.ShiftLeft) || _window.IsKeyDown(Glib.Key.ShiftRight));
                io.AddKeyEvent(ImGuiKey.ModSuper, _window.IsKeyDown(Glib.Key.SuperLeft) || _window.IsKeyDown(Glib.Key.SuperRight));

                io.AddKeyEvent(imKey, down);
            }
        }

        private static Glib.Key[] keyEnumArr = (Glib.Key[]) Enum.GetValues(typeof(Glib.Key));
        private void UpdateImGuiInput()
        {
            var io = ImGuiNET.ImGui.GetIO();

            //io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
            //io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
            //io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);

            Point point = new((int) _window.MouseX, (int) _window.MouseY);
            if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                throw new NotImplementedException("Viewports not implemented");
                //var windowPos = _window.Position;
                //point.X += windowPos.X;
                //point.Y += windowPos.Y;
            }
            
            io.AddMousePosEvent(point.X, point.Y);

            var wheel = _window.MouseWheel;
            io.AddMouseWheelEvent(wheel.X, wheel.Y);
        }

        private static void SetKeyMappings()
        {
            if (keyMap.Count > 0) return;

            keyMap[Glib.Key.Apostrophe] = ImGuiKey.Apostrophe;
            keyMap[Glib.Key.Comma] = ImGuiKey.Comma;
            keyMap[Glib.Key.Minus] = ImGuiKey.Minus;
            keyMap[Glib.Key.Period] = ImGuiKey.Period;
            keyMap[Glib.Key.Slash] = ImGuiKey.Slash;
            keyMap[Glib.Key.Number0] = ImGuiKey._0;
            keyMap[Glib.Key.Number1] = ImGuiKey._1;
            keyMap[Glib.Key.Number2] = ImGuiKey._2;
            keyMap[Glib.Key.Number3] = ImGuiKey._3;
            keyMap[Glib.Key.Number4] = ImGuiKey._4;
            keyMap[Glib.Key.Number5] = ImGuiKey._5;
            keyMap[Glib.Key.Number6] = ImGuiKey._6;
            keyMap[Glib.Key.Number7] = ImGuiKey._7;
            keyMap[Glib.Key.Number8] = ImGuiKey._8;
            keyMap[Glib.Key.Number9] = ImGuiKey._9;
            keyMap[Glib.Key.Semicolon] = ImGuiKey.Semicolon;
            keyMap[Glib.Key.Equal] = ImGuiKey.Equal;
            keyMap[Glib.Key.A] = ImGuiKey.A;
            keyMap[Glib.Key.B] = ImGuiKey.B;
            keyMap[Glib.Key.C] = ImGuiKey.C;
            keyMap[Glib.Key.D] = ImGuiKey.D;
            keyMap[Glib.Key.E] = ImGuiKey.E;
            keyMap[Glib.Key.F] = ImGuiKey.F;
            keyMap[Glib.Key.G] = ImGuiKey.G;
            keyMap[Glib.Key.H] = ImGuiKey.H;
            keyMap[Glib.Key.I] = ImGuiKey.I;
            keyMap[Glib.Key.J] = ImGuiKey.J;
            keyMap[Glib.Key.K] = ImGuiKey.K;
            keyMap[Glib.Key.L] = ImGuiKey.L;
            keyMap[Glib.Key.M] = ImGuiKey.M;
            keyMap[Glib.Key.N] = ImGuiKey.N;
            keyMap[Glib.Key.O] = ImGuiKey.O;
            keyMap[Glib.Key.P] = ImGuiKey.P;
            keyMap[Glib.Key.Q] = ImGuiKey.Q;
            keyMap[Glib.Key.R] = ImGuiKey.R;
            keyMap[Glib.Key.S] = ImGuiKey.S;
            keyMap[Glib.Key.T] = ImGuiKey.T;
            keyMap[Glib.Key.U] = ImGuiKey.U;
            keyMap[Glib.Key.V] = ImGuiKey.V;
            keyMap[Glib.Key.W] = ImGuiKey.W;
            keyMap[Glib.Key.X] = ImGuiKey.X;
            keyMap[Glib.Key.Y] = ImGuiKey.Y;
            keyMap[Glib.Key.Z] = ImGuiKey.Z;
            keyMap[Glib.Key.Space] = ImGuiKey.Space;
            keyMap[Glib.Key.Escape] = ImGuiKey.Escape;
            keyMap[Glib.Key.Enter] = ImGuiKey.Enter;
            keyMap[Glib.Key.Tab] = ImGuiKey.Tab;
            keyMap[Glib.Key.Backspace] = ImGuiKey.Backspace;
            keyMap[Glib.Key.Insert] = ImGuiKey.Insert;
            keyMap[Glib.Key.Delete] = ImGuiKey.Delete;
            keyMap[Glib.Key.Right] = ImGuiKey.RightArrow;
            keyMap[Glib.Key.Left] = ImGuiKey.LeftArrow;
            keyMap[Glib.Key.Down] = ImGuiKey.DownArrow;
            keyMap[Glib.Key.Up] = ImGuiKey.UpArrow;
            keyMap[Glib.Key.PageUp] = ImGuiKey.PageUp;
            keyMap[Glib.Key.PageDown] = ImGuiKey.PageDown;
            keyMap[Glib.Key.Home] = ImGuiKey.Home;
            keyMap[Glib.Key.End] = ImGuiKey.End;
            keyMap[Glib.Key.CapsLock] = ImGuiKey.CapsLock;
            keyMap[Glib.Key.ScrollLock] = ImGuiKey.ScrollLock;
            keyMap[Glib.Key.NumLock] = ImGuiKey.NumLock;
            keyMap[Glib.Key.PrintScreen] = ImGuiKey.PrintScreen;
            keyMap[Glib.Key.Pause] = ImGuiKey.Pause;
            keyMap[Glib.Key.F1] = ImGuiKey.F1;
            keyMap[Glib.Key.F2] = ImGuiKey.F2;
            keyMap[Glib.Key.F3] = ImGuiKey.F3;
            keyMap[Glib.Key.F4] = ImGuiKey.F4;
            keyMap[Glib.Key.F5] = ImGuiKey.F5;
            keyMap[Glib.Key.F6] = ImGuiKey.F6;
            keyMap[Glib.Key.F7] = ImGuiKey.F7;
            keyMap[Glib.Key.F8] = ImGuiKey.F8;
            keyMap[Glib.Key.F9] = ImGuiKey.F9;
            keyMap[Glib.Key.F10] = ImGuiKey.F10;
            keyMap[Glib.Key.F11] = ImGuiKey.F11;
            keyMap[Glib.Key.F12] = ImGuiKey.F12;
            keyMap[Glib.Key.ShiftLeft] = ImGuiKey.LeftShift;
            keyMap[Glib.Key.ControlLeft] = ImGuiKey.LeftCtrl;
            keyMap[Glib.Key.AltLeft] = ImGuiKey.LeftAlt;
            keyMap[Glib.Key.SuperLeft] = ImGuiKey.LeftSuper;
            keyMap[Glib.Key.ShiftRight] = ImGuiKey.RightShift;
            keyMap[Glib.Key.ControlRight] = ImGuiKey.RightCtrl;
            keyMap[Glib.Key.AltRight] = ImGuiKey.RightAlt;
            keyMap[Glib.Key.SuperRight] = ImGuiKey.RightSuper;
            keyMap[Glib.Key.Menu] = ImGuiKey.Menu;
            keyMap[Glib.Key.LeftBracket] = ImGuiKey.LeftBracket;
            keyMap[Glib.Key.BackSlash] = ImGuiKey.Backslash;
            keyMap[Glib.Key.RightBracket] = ImGuiKey.RightBracket;
            keyMap[Glib.Key.GraveAccent] = ImGuiKey.GraveAccent;
            keyMap[Glib.Key.Keypad0] = ImGuiKey.Keypad0;
            keyMap[Glib.Key.Keypad1] = ImGuiKey.Keypad1;
            keyMap[Glib.Key.Keypad2] = ImGuiKey.Keypad2;
            keyMap[Glib.Key.Keypad3] = ImGuiKey.Keypad3;
            keyMap[Glib.Key.Keypad4] = ImGuiKey.Keypad4;
            keyMap[Glib.Key.Keypad5] = ImGuiKey.Keypad5;
            keyMap[Glib.Key.Keypad6] = ImGuiKey.Keypad6;
            keyMap[Glib.Key.Keypad7] = ImGuiKey.Keypad7;
            keyMap[Glib.Key.Keypad8] = ImGuiKey.Keypad8;
            keyMap[Glib.Key.Keypad9] = ImGuiKey.Keypad9;
            keyMap[Glib.Key.KeypadDecimal] = ImGuiKey.KeypadDecimal;
            keyMap[Glib.Key.KeypadDivide] = ImGuiKey.KeypadDivide;
            keyMap[Glib.Key.KeypadMultiply] = ImGuiKey.KeypadMultiply;
            keyMap[Glib.Key.KeypadSubtract] = ImGuiKey.KeypadSubtract;
            keyMap[Glib.Key.KeypadAdd] = ImGuiKey.KeypadAdd;
            keyMap[Glib.Key.KeypadEnter] = ImGuiKey.KeypadEnter;
            keyMap[Glib.Key.KeypadEqual] = ImGuiKey.KeypadEqual;
        }

        private unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight)
        {
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
            var rctx = RenderContext.Instance!;
            
            rctx.Flags = RenderFlags.None;
            rctx.Shader = null; // default shader is good enough for our purposes
            rctx.DrawColor = Color.White;
            rctx.CullMode = CullMode.None;
            rctx.BlendMode = BlendMode.Normal;

            float L = drawDataPtr.DisplayPos.X;
            float R = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
            float T = drawDataPtr.DisplayPos.Y;
            float B = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

            //Matrix4x4 orthoProjection = new Matrix4x4(
            //    2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            //    0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            //    0.0f, 0.0f, -1.0f, 0.0f,
            //    (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f
            //);

            //_shader.UseShader();
            //_gl.Uniform1(_attribLocationTex, 0);
            //_gl.UniformMatrix4(_attribLocationProjMtx, 1, false, orthoProjection);
            //_gl.CheckGlError("Projection");

            //_gl.BindSampler(0, 0);

            // Create vertex layout and transient buffers
            
        }

        internal unsafe void RenderImDrawData(ImDrawDataPtr drawDataPtr)
        {
            Debug.Assert(drawDataPtr.Valid);
            Debug.Assert(drawDataPtr.CmdListsCount == drawDataPtr.CmdLists.Size);
            int framebufferWidth = (int) (drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
            int framebufferHeight = (int) (drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
            if (framebufferWidth <= 0 || framebufferHeight <= 0)
                return;
            
            var rctx = RenderContext.Instance!;

            // Backup GL state
            var lastProgram = rctx.Shader;
            Span<int> lastScissorBox = stackalloc int[4];
            var lastScissor = rctx.GetScissor(out lastScissorBox[0], out lastScissorBox[1], out lastScissorBox[2], out lastScissorBox[3]);
            var lastFlags = rctx.Flags;
            var lastDrawColor = rctx.DrawColor;
            var lastCullMode = rctx.CullMode;
            var lastBlendMode = rctx.BlendMode;

            SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);

            // Will project scissor/clipping rectangles into framebuffer space
            Vector2 clipOff = drawDataPtr.DisplayPos;         // (0,0) unless using multi-viewports
            Vector2 clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

            // Render command lists
            for (int n = 0; n < drawDataPtr.CmdListsCount; n++)
            {
                ImDrawListPtr cmdListPtr = drawDataPtr.CmdLists[n];

                // Upload vertex/index buffers
                var vtxData = new ReadOnlySpan<ImDrawVert>((void*)cmdListPtr.VtxBuffer.Data, cmdListPtr.VtxBuffer.Size);
                var idxData = new ReadOnlySpan<ushort>((void*)cmdListPtr.IdxBuffer.Data, cmdListPtr.IdxBuffer.Size);
                
                _drawMesh.SetBufferData(0, vtxData);
                _drawMesh.SetIndexBuffer(idxData);
                _drawMesh.Upload();

                using var activeMesh = rctx.UseMesh(_drawMesh);

                for (int cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr cmdPtr = cmdListPtr.CmdBuffer[cmd_i];

                    if (cmdPtr.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        Vector4 clipRect;
                        clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
                        clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
                        clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
                        clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

                        if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
                        {
                            // Apply scissor/clipping rectangle
                            rctx.SetScissorBox((int) clipRect.X, (int) clipRect.Y, (int) (clipRect.Z - clipRect.X), (int) (clipRect.W - clipRect.Y));

                            // Bind texture, Draw
                            activeMesh.SetIndexedSlice(cmdPtr.IdxOffset, cmdPtr.VtxOffset, cmdPtr.ElemCount);
                            activeMesh.Draw(_textures[(int)cmdPtr.TextureId - 1]);
                        }
                    }
                }

                activeMesh.Dispose();
            }

            // Restore modified GL state
            rctx.Shader = lastProgram;
            if (lastScissor)
            {
                rctx.SetScissorBox(lastScissorBox[0], lastScissorBox[1], lastScissorBox[2], lastScissorBox[3]);
            }
            else
            {
                rctx.ClearScissorBox();
            }
            rctx.Flags = lastFlags;
            rctx.DrawColor = lastDrawColor;
            rctx.CullMode = lastCullMode;
            rctx.BlendMode = lastBlendMode;
        }

        private void CreateDeviceResources()
        {
            _shader = Glib.Shader.Load("imgui.vert", "imgui.frag");

            _drawMesh = new Glib.MeshConfiguration()
                .AddBuffer(
                    new Glib.MeshBufferConfiguration()
                        .SetUsage(Glib.MeshBufferUsage.Transient)
                        .LayoutAdd(Glib.AttributeName.Position, Glib.DataType.Float, 2)
                        .LayoutAdd(Glib.AttributeName.TexCoord0, Glib.DataType.Float, 2)
                        .LayoutAdd(Glib.AttributeName.Color0, Glib.DataType.Byte, 4, true)
                )
                .SetIndexed(false, Glib.MeshBufferUsage.Transient)
                .Create(0, 0); // all buffers are transient
            
            RecreateFontDeviceTexture();
        }

        /// <summary>
        /// Creates the texture used to render text.
        /// </summary>
        public unsafe void RecreateFontDeviceTexture()
        {
            // Build texture atlas
            var io = ImGuiNET.ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);   // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small) because it is more likely to be compatible with user's existing shaders. If your ImTextureId represent a higher-level concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.

            // Upload texture to graphics system

            _fontTexture?.Dispose();
            //_fontTexture = Glib.Texture.Create(width, height, pixels);
            _fontTexture = Glib.Texture.Create(width, height, Glib.PixelFormat.RGBA);
            _fontTexture.UpdateFromImage(new Span<byte>((void*)pixels, width * height * bytesPerPixel));

            // Store our identifier
            io.Fonts.SetTexID(1);
        }

        public nint UseTexture(Glib.Texture texture)
        {
            var idx = _textures.IndexOf(texture);
            if (idx >= 0) return (nint)idx + 1;
            _textures.Add(texture);
            return _textures.Count;
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            //_viewports?.Dispose();
            _window.MakeCurrent();

            _window.Resize -= WindowResized;
            _window.KeyChar -= OnKeyChar;
            _window.KeyDown -= OnKeyDown;
            _window.KeyUp -= OnKeyUp;

            _textures = null;
            _shader.Dispose();
            _drawMesh.Dispose();
            _fontTexture.Dispose();

            ImGuiNET.ImGui.DestroyContext(Context);
            GC.SuppressFinalize(this);
        }
    }
}