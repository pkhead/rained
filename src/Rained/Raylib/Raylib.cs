using Glib;
using System.Numerics;
namespace Raylib_cs;

static class Raylib
{
    static ConfigFlags configFlags = 0;
    static Window window = null!;
    public static Window GlibWindow => window;

    // what the hell was ray on when he decided to make his apis take angles in degrees
    private const float DEG2RAD = 1.0f / 180.0f * MathF.PI;

    static readonly bool[] mouseButtonsDown = [false, false, false];
    static readonly bool[] mouseButtonsPressed = [false, false, false];
    static readonly bool[] mouseButtonsReleased = [false, false, false];

    private static double lastFrame = 0.0;
    private static double frameTime = 0.0;
    private static Vector2? lastMousePos = null;
    private static Vector2 mouseDelta = Vector2.Zero;

    public static Glib.Color ToGlibColor(Color color)
    {
        return Glib.Color.FromRGBA(color.R, color.G, color.B, color.A);
    }

    private static readonly Dictionary<KeyboardKey, Glib.Key> keyMap = new()
    {
        {KeyboardKey.Apostrophe, Key.Apostrophe},
        {KeyboardKey.Comma, Key.Comma},
        {KeyboardKey.Minus, Key.Minus},
        {KeyboardKey.Period, Key.Period},
        {KeyboardKey.Slash, Key.Slash},
        {KeyboardKey.Zero, Key.Number0},
        {KeyboardKey.One, Key.Number1},
        {KeyboardKey.Two, Key.Number2},
        {KeyboardKey.Three, Key.Number3},
        {KeyboardKey.Four, Key.Number4},
        {KeyboardKey.Five, Key.Number5},
        {KeyboardKey.Six, Key.Number6},
        {KeyboardKey.Seven, Key.Number7},
        {KeyboardKey.Eight, Key.Number8},
        {KeyboardKey.Nine, Key.Number9},
        {KeyboardKey.Semicolon, Key.Semicolon},
        {KeyboardKey.Equal, Key.Equal},
        {KeyboardKey.A, Key.A},
        {KeyboardKey.B, Key.B},
        {KeyboardKey.C, Key.C},
        {KeyboardKey.D, Key.D},
        {KeyboardKey.E, Key.E},
        {KeyboardKey.F, Key.F},
        {KeyboardKey.G, Key.G},
        {KeyboardKey.H, Key.H},
        {KeyboardKey.I, Key.I},
        {KeyboardKey.J, Key.J},
        {KeyboardKey.K, Key.K},
        {KeyboardKey.L, Key.L},
        {KeyboardKey.M, Key.M},
        {KeyboardKey.N, Key.N},
        {KeyboardKey.O, Key.O},
        {KeyboardKey.P, Key.P},
        {KeyboardKey.Q, Key.Q},
        {KeyboardKey.R, Key.R},
        {KeyboardKey.S, Key.S},
        {KeyboardKey.T, Key.T},
        {KeyboardKey.U, Key.U},
        {KeyboardKey.V, Key.V},
        {KeyboardKey.W, Key.W},
        {KeyboardKey.X, Key.X},
        {KeyboardKey.Y, Key.Y},
        {KeyboardKey.Z, Key.Z},
        {KeyboardKey.Space, Key.Space},
        {KeyboardKey.Escape, Key.Escape},
        {KeyboardKey.Enter, Key.Enter},
        {KeyboardKey.Tab, Key.Tab},
        {KeyboardKey.Backspace, Key.Backspace},
        {KeyboardKey.Insert, Key.Insert},
        {KeyboardKey.Delete, Key.Delete},
        {KeyboardKey.Right, Key.Right},
        {KeyboardKey.Left, Key.Left},
        {KeyboardKey.Down, Key.Down},
        {KeyboardKey.Up, Key.Up},
        {KeyboardKey.PageUp, Key.PageUp},
        {KeyboardKey.PageDown, Key.PageDown},
        {KeyboardKey.Home, Key.Home},
        {KeyboardKey.End, Key.End},
        {KeyboardKey.CapsLock, Key.CapsLock},
        {KeyboardKey.ScrollLock, Key.ScrollLock},
        {KeyboardKey.NumLock, Key.NumLock},
        {KeyboardKey.PrintScreen, Key.PrintScreen},
        {KeyboardKey.Pause, Key.Pause},
        {KeyboardKey.F1, Key.F1},
        {KeyboardKey.F2, Key.F2},
        {KeyboardKey.F3, Key.F3},
        {KeyboardKey.F4, Key.F4},
        {KeyboardKey.F5, Key.F5},
        {KeyboardKey.F6, Key.F6},
        {KeyboardKey.F7, Key.F7},
        {KeyboardKey.F8, Key.F8},
        {KeyboardKey.F9, Key.F9},
        {KeyboardKey.F10, Key.F10},
        {KeyboardKey.F11, Key.F11},
        {KeyboardKey.F12, Key.F12},
        {KeyboardKey.LeftShift, Key.ShiftLeft},
        {KeyboardKey.LeftControl, Key.ControlLeft},
        {KeyboardKey.LeftAlt, Key.AltLeft},
        {KeyboardKey.LeftSuper, Key.SuperLeft},
        {KeyboardKey.RightShift, Key.ShiftRight},
        {KeyboardKey.RightControl, Key.ControlRight},
        {KeyboardKey.RightAlt, Key.AltRight},
        {KeyboardKey.RightSuper, Key.SuperRight},
        {KeyboardKey.LeftBracket, Key.LeftBracket},
        {KeyboardKey.Backslash, Key.BackSlash},
        {KeyboardKey.RightBracket, Key.RightBracket},
        {KeyboardKey.Grave, Key.GraveAccent},
        {KeyboardKey.Kp0, Key.Keypad0},
        {KeyboardKey.Kp1, Key.Keypad1},
        {KeyboardKey.Kp2, Key.Keypad2},
        {KeyboardKey.Kp3, Key.Keypad3},
        {KeyboardKey.Kp4, Key.Keypad4},
        {KeyboardKey.Kp5, Key.Keypad5},
        {KeyboardKey.Kp6, Key.Keypad6},
        {KeyboardKey.Kp7, Key.Keypad7},
        {KeyboardKey.Kp8, Key.Keypad8},
        {KeyboardKey.Kp9, Key.Keypad9},
        {KeyboardKey.KpDecimal, Key.KeypadDecimal},
        {KeyboardKey.KpDivide, Key.KeypadDivide},
        {KeyboardKey.KpMultiply, Key.KeypadMultiply},
        {KeyboardKey.KpSubtract, Key.KeypadSubtract},
        {KeyboardKey.KpAdd, Key.KeypadAdd},
        {KeyboardKey.KpEnter, Key.KeypadEnter},
        {KeyboardKey.KpEqual, Key.KeypadEqual},
        //{KeyboardKey.Menu, Key.Menu}
    };

    #region Windowing

    /// <summary>
    /// Initialize window and OpenGL context
    /// </summary>
    public static void InitWindow(Window win)
    {
        window = win;

        window.MouseDown += (Glib.MouseButton btn) =>
        {
            switch (btn)
            {
                case Glib.MouseButton.Left:
                    mouseButtonsDown[(int) MouseButton.Left] = true;
                    mouseButtonsPressed[(int) MouseButton.Left] = true;
                    break;

                case Glib.MouseButton.Middle:
                    mouseButtonsDown[(int) MouseButton.Middle] = true;
                    mouseButtonsPressed[(int) MouseButton.Middle] = true;
                    break;

                case Glib.MouseButton.Right:
                    mouseButtonsDown[(int) MouseButton.Right] = true;
                    mouseButtonsPressed[(int) MouseButton.Right] = true;
                    break;
            }
        };

        window.MouseUp += (Glib.MouseButton btn) =>
        {
            switch (btn)
            {
                case Glib.MouseButton.Left:
                    mouseButtonsDown[(int) MouseButton.Left] = false;
                    mouseButtonsReleased[(int) MouseButton.Left] = true;
                    break;

                case Glib.MouseButton.Middle:
                    mouseButtonsDown[(int) MouseButton.Middle] = false;
                    mouseButtonsReleased[(int) MouseButton.Middle] = true;
                    break;

                case Glib.MouseButton.Right:
                    mouseButtonsDown[(int) MouseButton.Right] = false;
                    mouseButtonsReleased[(int) MouseButton.Right] = true;
                    break;
            }
        };
        
        lastFrame = window.Time;
        window.RenderContext!.DefaultTextureMinFilter = TextureFilterMode.Nearest;
        window.RenderContext!.DefaultTextureMagFilter = TextureFilterMode.Nearest;
    }

    /// <summary>
    /// Close window and unload OpenGL context
    /// </summary>
    public static void CloseWindow()
    {
        window.Dispose();
    }

    /// <summary>
    /// Check if application should close (KEY_ESCAPE pressed or windows close icon clicked)
    /// </summary>
    public static bool WindowShouldClose()
    {
        return window.IsClosing;
    }

    /// <summary>
    /// Check if window has been initialized successfully
    /// </summary>
    public static bool IsWindowReady()
    {
        return window != null;
    }

    public static void SetConfigFlags(ConfigFlags flags)
    {
        configFlags = flags;
    }

    public static void ClearWindowState(ConfigFlags flags)
    {
        if (flags.HasFlag(ConfigFlags.HiddenWindow))
        {
            window.Visible = false;
        }
        else
        {
            throw new NotImplementedException();
        }
    }
    
    public static void SetTraceLogLevel(TraceLogLevel _) {}

    public static void SetTargetFPS(int targetFps)
    {
        // no-op
    }

    public static float GetFrameTime()
    {
        return (float)frameTime;
    }

    public static double GetTime()
    {
        return window.Time;
    }

    public static int GetScreenWidth()
    {
        return window.Width;
    }

    public static int GetScreenHeight()
    {
        return window.Height;
    }

    public static bool IsWindowMaximized()
    {
        return window.WindowState == Silk.NET.Windowing.WindowState.Maximized;
    }

    public static void SetWindowTitle(string title)
    {
        window.Title = title;
    }

    public static void SetExitKey(KeyboardKey key)
    {
        // no-op
    }

    // Input-related functions: keyboard
    public static bool IsKeyPressed(KeyboardKey key)
    {
        if (keyMap.TryGetValue(key, out var k))
            return window.IsKeyPressed(k);
        return false;
    }

    public static bool IsKeyPressedRepeat(KeyboardKey key)
    {
        throw new NotImplementedException();
    }

    public static bool IsKeyDown(KeyboardKey key)
    {
        if (keyMap.TryGetValue(key, out var k))
            return window.IsKeyDown(k);
        return false;
    }

    public static bool IsKeyReleased(KeyboardKey key)
    {
        if (keyMap.TryGetValue(key, out var k))
            return window.IsKeyReleased(k);
        return false;
    }

    public static bool IsKeyUp(KeyboardKey key)
    {
        if (keyMap.TryGetValue(key, out var k))
            return !window.IsKeyDown(k);
        return false;
    }

    // Input-related functions: mouse
    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return mouseButtonsPressed[(int) button];
    }

    public static bool IsMouseButtonDown(MouseButton button)
    {
        return mouseButtonsDown[(int) button];
    }

    public static bool IsMouseButtonReleased(MouseButton button)
    {
        return mouseButtonsReleased[(int) button];
    }

    public static bool IsMouseButtonUp(MouseButton button)
    {
        return !mouseButtonsDown[(int) button];
    }

    public static int GetMouseX()
    {
        return (int)window.MouseX;
    }

    public static int GetMouseY()
    {
        return (int)window.MouseY;
    }

    public static Vector2 GetMousePosition()
    {
        return new Vector2(window.MouseX, window.MouseY);
    }

    public static Vector2 GetMouseDelta()
    {
        return mouseDelta;
    }

    public static float GetMouseWheelMove()
    {
        return window.MouseWheel;
    }

    public static void HideCursor()
    {
        var cursor = window.SilkInputContext.Mice[0].Cursor;
        cursor.CursorMode = Silk.NET.Input.CursorMode.Hidden;
    }

    public static void ShowCursor()
    {
        var cursor = window.SilkInputContext.Mice[0].Cursor;
        cursor.CursorMode = Silk.NET.Input.CursorMode.Normal;
    }

    public static void SetMousePosition(int x, int y)
    {
        window.MousePosition = new Vector2(x, y);
        lastMousePos = new Vector2(x, y);
    }

    public static int GetRandomValue(int min, int max)
    {
        return Random.Shared.Next(min, max+1);
    }

    public static Shader LoadShaderFromMemory(string? vsCode, string? fsCode)
    {
        try
        {
            var shader = window.RenderContext!.CreateShader(vsCode, fsCode);
            return new Shader()
            {
                ID = shader
            };
        }
        catch (ShaderCompilationException e)
        {
            RainEd.RainEd.Logger.Error(e.ToString());
            return new Shader();
        }
    }

    public static Shader LoadShader(string? vsPath, string? fsPath)
    {
        string? vsCode = null;
        string? fsCode = null;

        try
        {
            if (vsPath is not null)
                vsCode = File.ReadAllText(vsPath);

            if (fsPath is not null)
                fsCode = File.ReadAllText(fsPath);
        }
        catch (Exception e)
        {
            RainEd.RainEd.Logger.Error(e.ToString());
            return new Shader();
        }
        
        return LoadShaderFromMemory(vsCode, fsCode);
    }

    #endregion

    #region Drawing
    public static void ClearBackground(Color color)
    {
        window.RenderContext!.BackgroundColor = Glib.Color.FromRGBA(color.R, color.G, color.B, color.A);
        window.RenderContext!.Clear();
    }

    public static void BeginDrawing()
    {
        frameTime = window.Time - lastFrame;
        lastFrame = window.Time;

        for (int i = 0; i < 3; i++)
        {
            mouseButtonsPressed[i] = false;
            mouseButtonsReleased[i] = false;
        }

        window.PollEvents();

        if (lastMousePos is null)
        {
            lastMousePos = new Vector2(window.MouseX, window.MouseY);
            mouseDelta = Vector2.Zero;
        }
        else
        {
            mouseDelta = new Vector2(window.MouseX, window.MouseY) - lastMousePos.Value;
            lastMousePos = new Vector2(window.MouseX, window.MouseY);
        }

        window.BeginRender();
    }

    public static void EndDrawing()
    {
        window.EndRender();
        window.SwapBuffers();
    }

    public static void BeginTextureMode(RenderTexture2D rtex)
    {
        window.RenderContext!.PopFramebuffer();
        window.RenderContext!.PushFramebuffer(rtex.ID!);
    }

    public static void EndTextureMode()
    {
        window.RenderContext!.PopFramebuffer();
    }

    public static void BeginShaderMode(Shader shader)
    {
        window.RenderContext!.Shader = shader.ID;
    }

    public static void EndShaderMode()
    {
        window.RenderContext!.Shader = null;
    }

    public static void BeginScissorMode(int x, int y, int width, int height)
    {
        window.RenderContext!.SetEnabled(Feature.ScissorTest, true);
        window.RenderContext!.SetScissorBounds(x, y, width, height);
    }

    public static void EndScissorMode()
    {
        window.RenderContext!.ResetScissorBounds();
        window.RenderContext!.SetEnabled(Feature.ScissorTest, false);
    }
    #endregion

    #region rshapes
    public static void DrawLineEx(Vector2 startPos, Vector2 endPos, float thick, Color color)
    {
        window.RenderContext!.LineWidth = thick;
        window.RenderContext!.UseGlLines = false;
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawLine(startPos, endPos);
    }

    public static void DrawLineV(Vector2 startPos, Vector2 endPos, Color color)
    {
        window.RenderContext!.UseGlLines = true;
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawLine(startPos, endPos);
    }

    public static void DrawLine(int startPosX, int startPosY, int endPosX, int endPosY, Color color)
    {
        DrawLineV(new Vector2(startPosX, startPosY), new Vector2(endPosX, endPosY), color);
    }

    public static void DrawCircleV(Vector2 center, float radius, Color color)
    {
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawCircle(center.X, center.Y, radius);
    }

    public static void DrawCircle(int centerX, int centerY, float radius, Color color)
    {
        DrawCircleV(new Vector2(centerX, centerY), radius, color);
    }

    public static void DrawCircleLinesV(Vector2 center, float radius, Color color)
    {
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.LineWidth = 1f;
        window.RenderContext!.UseGlLines = true;
        window.RenderContext!.DrawRing(center, radius);
    }

    public static void DrawCircleLines(int centerX, int centerY, float radius, Color color)
    {
        DrawCircleLinesV(new Vector2(centerX, centerY), radius, color);
    }

    public static void DrawRectanglePro(Rectangle rec, Vector2 origin, float rotation, Color color)
    {
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.PushTransform();
        window.RenderContext!.Translate(rec.X, rec.Y, 0f);
        window.RenderContext!.Rotate(rotation * DEG2RAD);
        window.RenderContext!.DrawRectangle(-origin.X, -origin.Y, rec.Width, rec.Height);
        window.RenderContext!.PopTransform();
    }

    public static void DrawRectangleRec(Rectangle rec, Color color)
    {
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawRectangle(rec.X, rec.Y, rec.Width, rec.Height);
    }

    public static void DrawRectangleV(Vector2 position, Vector2 size, Color color)
    {
        DrawRectangleRec(new Rectangle(position, size), color);
    }

    public static void DrawRectangle(int posX, int posY, int width, int height, Color color)
    {
        DrawRectangleRec(new Rectangle(posX, posY, width, height), color);
    }

    public static void DrawRectangleLinesEx(Rectangle rec, float lineThick, Color color)
    {
        window.RenderContext!.LineWidth = lineThick;
        window.RenderContext!.UseGlLines = false;
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawRectangleLines(rec.X, rec.Y, rec.Width, rec.Height);
    }

    public static void DrawRectangleLines(int posX, int posY, int width, int height, Color color)
    {
        window.RenderContext!.UseGlLines = true;
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawRectangleLines(posX, posY, width, height);
    }

    public static void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Color color)
    {
        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawTriangle(v1, v2, v3);
    }

    public static void DrawTriangleLines(Vector2 v1, Vector2 v2, Vector2 v3, Color color)
    {
        window.RenderContext!.UseGlLines = true;

        window.RenderContext!.DrawColor = ToGlibColor(color);
        window.RenderContext!.DrawLine(v1, v2);
        window.RenderContext!.DrawLine(v2, v3);
        window.RenderContext!.DrawLine(v3, v1);
    }
    #endregion

    #region rtextures
    public static Image LoadImage(string fileName)
    {
        var obj = new Image()
        {
            image = null
        };

        try
        {
            obj.image = Glib.Image.FromFile(fileName);
        }
        catch (Exception e)
        {
            RainEd.RainEd.Logger.Error("Error while loading image {ImageName}:\n{Exception}", fileName, e);
            obj.image = null;
        }

        return obj;
    }

    public static Image LoadImageFromTexture(Texture2D tex)
    {
        return new Image()
        {
            image = tex.ID!.ToImage()
        };
    }

    public static Image ImageCopy(Image src)
    {
        var srcPixels = src.image!.Pixels;
        var newPixels = new byte[srcPixels.Length];
        srcPixels.CopyTo(newPixels, 0);
        var newImage = new Glib.Image(newPixels, src.Width, src.Height, src.image!.PixelFormat);

        return new Image()
        {
            image = newImage
        };
    }

    public static void ImageDrawPixel(Image image, int x, int y, Color color)
    {
        image.image!.SetPixel(x, y, ToGlibColor(color));
    }

    /// <summary>
    /// Update GPU texture with new data.
    /// The original Raylib function has a byte array as the second argument,
    /// but I chose to change it so it takes an Image instead.
    /// </summary>
    public static void UpdateTexture(Texture2D tex, Image image)
    {
        tex.ID!.UpdateFromImage(image.image!);
    }

    public static bool IsImageReady(Image image)
    {
        return image.image != null;
    }

    public static void UnloadImage(ref Image image)
    {
        image.image = null;
    }

    public static bool ExportImage(Image image, string fileName)
    {
        throw new NotImplementedException();
    }

    public static Image GenImageColor(int width, int height, Color color)
    {
        return new Image()
        {
            image = Glib.Image.FromColor(width, height, ToGlibColor(color))
        };
    }

    public static void ImageFormat(ref Image image, PixelFormat newFormat)
    {
        image.image = image.image!.ConvertToFormat(newFormat switch
        {
            PixelFormat.UncompressedGrayscale => Glib.PixelFormat.Grayscale,
            PixelFormat.UncompressedR8G8B8A8 => Glib.PixelFormat.RGBA,
            _ => throw new ArgumentOutOfRangeException(nameof(newFormat))   
        });
    }

    private static void ImageDraw(Glib.Image dstImage, Glib.Image srcImage, Glib.Rectangle srcRec, Glib.Rectangle dstRec, Glib.Color tintCol)
    {
        var srcStartX = (int)srcRec.X;
        var srcStartY = (int)srcRec.Y;
        var srcW = (int)srcRec.Width;
        var srcH = (int)srcRec.Height;
        
        var dstStartX = (int)dstRec.X;
        var dstStartY = (int)dstRec.Y;
        var dstW = (int)dstRec.Width;
        var dstH = (int)dstRec.Height;

        // if the src rect is outside the bounds of the source image,
        // all pixels will be discarded. therefore, there is no need
        // to do anything processing if this is the case
        if (srcStartX >= srcImage.Width || srcStartY >= srcImage.Height || srcStartX + srcW < 1 || srcStartY + srcH < 1)
            return;

        // similarly, if the dst rect is outside the bounds of the dest image,
        // there is no need to do any processing
        if (dstStartX >= dstImage.Width || dstStartY >= dstImage.Height || dstStartX + dstW < 1 || dstStartY + dstH < 1)
            return;

        // no scaling needs to be performed, so the draw operation
        // can be optimized
        if (srcW == dstW && srcH == dstH)
        {
            // if the two pixel formats are the same, and tint is white
            // each section of the image row can just be blitted to the destination image
            bool isTintWhite = tintCol.R == 1.0 && tintCol.G == 1.0 && tintCol.B == 1.0 && tintCol.A == 1.0;
            if (srcImage.PixelFormat == dstImage.PixelFormat && isTintWhite)
            {
                int pDstStartX = Math.Max(0, dstStartX - Math.Min(srcStartX, 0));
                int pDstStartY = Math.Max(0, dstStartY - Math.Min(srcStartY, 0));

                int bpp = (int)srcImage.BytesPerPixel;
                int srcRowOffset = Math.Max(srcStartX - Math.Min(dstStartX, 0), 0) * bpp;
                int dstRowOffset = pDstStartX * bpp;
                int rowLen = Math.Min(
                    Math.Min(pDstStartX + dstW, dstImage.Width) - pDstStartX,
                    Math.Min(srcStartX + srcW, srcImage.Width) - srcStartX
                ) * bpp;

                int srcRowLen = srcImage.Width * bpp;
                int dstRowLen = dstImage.Width * bpp;

                int srcColOffset = Math.Max(srcStartY - Math.Min(dstStartY, 0), 0);
                int dstColOffset = pDstStartY;
                int colLen = Math.Min(
                    Math.Min(pDstStartY + dstH, dstImage.Height) - dstStartY,
                    Math.Min(srcStartY + srcH, srcImage.Height) - srcStartY
                );

                var srcPixels = srcImage.Pixels;
                var dstPixels = dstImage.Pixels;

                int dstY = dstColOffset;
                int srcY = srcColOffset;
                for (int i = 0; i < colLen; i++)
                {
                    Buffer.BlockCopy(srcPixels, srcY * srcRowLen + srcRowOffset, dstPixels, dstY * dstRowLen + dstRowOffset, rowLen);
                    srcY++; dstY++;
                }
            }

            // but if they are not the same, looping through each individual pixel is required
            // since format conversions need to be done
            else
            {
                int srcX = srcStartX;

                for (int dstX = dstStartX; dstX < dstStartX + dstW; dstX++)
                {
                    if (dstX < 0 || dstX >= dstImage.Width) continue;
                    
                    int srcY = srcStartY;
                    for (int dstY = dstStartY; dstY < dstStartY + dstH; dstY++)
                    {
                        if (dstY < 0 || dstY >= dstImage.Height) continue;

                        // discard pixel if reading from outside source bounds
                        if (srcX >= 0 && srcY >= 0 && srcX < srcImage.Width && srcY < srcImage.Height)
                        {
                            var col = srcImage.GetPixel(srcX, srcY);
                            dstImage.SetPixel(dstX, dstY, new Glib.Color(col.R * tintCol.R, col.G * tintCol.G, col.B * tintCol.B, col.A * tintCol.A));
                        }

                        srcY++;
                    }

                    srcX++;
                }
            }
        }

        // here nearest-filtering scaling needs to be
        // performed
        else
        {
            float u;
            for (int dstX = dstStartX; dstX < dstStartX + dstW; dstX++)
            {
                if (dstX < 0 || dstX >= dstImage.Width) continue;
                u = (float)(dstX - dstStartX) / dstW;
                int srcX = (int)(u * srcW + srcStartX);

                float v;
                for (int dstY = dstStartY; dstY < dstStartY + dstH; dstY++)
                {
                    if (dstY < 0 || dstY >= dstImage.Height) continue;
                    v = (float)(dstY - dstStartY) / dstH;
                    int srcY = (int)(v * srcH + srcStartY);

                    // discard pixel if reading from outside source bounds
                    if (srcX >= 0 && srcY >= 0 && srcX < srcImage.Width && srcY < srcImage.Height)
                    {
                        var col = srcImage.GetPixel(srcX, srcY);
                        dstImage.SetPixel(dstX, dstY, new Glib.Color(col.R * tintCol.R, col.G * tintCol.G, col.B * tintCol.B, col.A * tintCol.A));
                    }
                }
            }
        }
    }

    public static void ImageDraw(Image dst, Image src, Rectangle srcRec, Rectangle dstRec, Color tint)
    {
        var glibSrcRec = new Glib.Rectangle(srcRec.Position, srcRec.Size);
        var glibDstRec = new Glib.Rectangle(dstRec.Position, dstRec.Size);
        ImageDraw(dst.image!, src.image!, glibSrcRec, glibDstRec, ToGlibColor(tint));
    }

    public static void ImageCrop(ref Image image, Rectangle crop)
    {
        var srcImage = image.image!;

        var startX = (int)crop.X;
        var startY = (int)crop.Y;
        var endX = (int)(crop.X + crop.Width);
        var endY = (int)(crop.Y + crop.Height);

        var newImage = Glib.Image.FromColor(endX - startX, endY - startY, Glib.Color.Transparent, srcImage.PixelFormat);

        ImageDraw(newImage, srcImage, new Glib.Rectangle(crop.Position, crop.Size), new Glib.Rectangle(Vector2.Zero, crop.Size), Glib.Color.White);
        // copy slices of each row that are within the crop rectangle
        // using the pixels array directly
        /*int bpp = (int)srcImage.BytesPerPixel;
        int rowLen = (endX - startX) * bpp;
        int rowOffset = startX * bpp;
        int rowSize = srcImage.Width * bpp;
        int dstRowSize = newImage.Width * bpp;

        for (int y = startY; y < endY; y++)
        {
            Buffer.BlockCopy(srcImage.Pixels, y * rowSize + rowOffset, newImage.Pixels, (y - startY) * dstRowSize, rowLen);
        }*/

        image.image = newImage;
    }

    public static void ImageFlipVertical(Image image)
    {
        var oldImage = image.image!;
        var newImage = Glib.Image.FromColor(oldImage.Width, oldImage.Height, Glib.Color.Transparent, oldImage.PixelFormat);

        int bpp = (int)Glib.Image.GetBytesPerPixel(oldImage.PixelFormat);
        int rowSize = oldImage.Width * bpp;
        for (int y = 0; y < oldImage.Height; y++)
        {
            Buffer.BlockCopy(oldImage.Pixels, y * rowSize, newImage.Pixels, (oldImage.Height - y - 1) * rowSize, rowSize);
        }
    }

    public static void ImageResizeCanvas(ref Image image, int newWidth, int newHeight, int offsetX, int offsetY, Color fill)
    {
        // i forgot what offsetX and offsetY meant so i'm just copying
        // the raylib code verbatim
        /*var srcImage = image.image!;

        if (newWidth == image.Width && newHeight == image.Height) return;

        var srcRec = new Glib.Rectangle(0f, 0f, image.Width, image.Height);
        var dstPos = new Vector2(offsetX, offsetY);

        if (offsetX < 0)
        {
            srcRec.X = -offsetX;
            srcRec.Width += offsetX;
            dstPos.X = 0;
        }
        else if ((offsetX + image.Width) > newWidth)
        {
            srcRec.Width = newWidth - offsetX;
        }

        if (offsetY < 0)
        {
            srcRec.Y = -offsetY;
            srcRec.Height += offsetY;
            dstPos.Y = 0;
        }
        else if ((offsetY + image.Height) > newHeight)
        {
            srcRec.Height = newHeight - offsetY;
        }

        if (newWidth < srcRec.Width) srcRec.Width = newWidth;
        if (newHeight < srcRec.Height) srcRec.Height = newHeight;

        int bytesPerPixel = (int)srcImage.BytesPerPixel;
        var resizedImage = Glib.Image.FromColor(newWidth, newHeight, ToGlibColor(fill), srcImage.PixelFormat);
        var resizedData = resizedImage.Pixels;

        // copy old image to resized canvas
        var srcPixels = srcImage.Pixels;
        int dstOffsetSize = ((int)dstPos.Y * newWidth + (int)dstPos.X) * bytesPerPixel;

        for (int y = 0; y < (int)srcRec.Height; y++)
        {
            Buffer.BlockCopy(srcPixels, ((y + (int)srcRec.Y) * image.Width + (int)srcRec.X) * bytesPerPixel, resizedData, dstOffsetSize, (int)srcRec.Width * bytesPerPixel);
            dstOffsetSize += newWidth * bytesPerPixel;
        }

        image.image = resizedImage;*/
        var srcImage = image.image!;
        var newImage = Glib.Image.FromColor(newWidth, newHeight, ToGlibColor(fill), srcImage.PixelFormat);

        var dstRec = new Glib.Rectangle(offsetX, offsetY, newWidth, newHeight);

        ImageDraw(
            dstImage: newImage,
            srcImage: srcImage,
            srcRec: new Glib.Rectangle(0f, 0f, newWidth, newHeight),
            dstRec: new Glib.Rectangle(offsetX, offsetY, newWidth, newHeight),
            tintCol: Glib.Color.White
        );

        image.image = newImage;
    }

    public static Color GetImageColor(Image image, int x, int y)
    {
        var gcol = image.image!.GetPixel(x, y);
        return new Color(
            (byte) Math.Clamp(gcol.R * 255f, 0f, 255f),
            (byte) Math.Clamp(gcol.G * 255f, 0f, 255f),
            (byte) Math.Clamp(gcol.B * 255f, 0f, 255f),
            (byte) Math.Clamp(gcol.A * 255f, 0f, 255f)
        );
    }

    public static Texture2D LoadTexture(string fileName)
    {
        try
        {
            Glib.Texture? texture = window.RenderContext!.LoadTexture(fileName);
            return new Texture2D()
            {
                ID = texture
            };
        }
        catch (Exception e)
        {
            RainEd.RainEd.Logger.Error("Error while loading texture {ImageName}:\n{Exception}", fileName, e);
            return new Texture2D()
            {
                ID = null
            };
        }
    }

    public static Texture2D LoadTextureFromImage(Image image)
    {
        return new Texture2D()
        {
            ID = window.RenderContext!.CreateTexture(image.image!)
        };
    }

    public static RenderTexture2D LoadRenderTexture(int width, int height)
    {
        return new RenderTexture2D()
        {
            ID = window.RenderContext!.CreateFramebuffer(FramebufferConfiguration.Standard(width, height))
        };
    }

    public static bool IsTextureReady(Texture2D texture)
    {
        return texture.ID != null;
    }

    public static void UnloadTexture(Texture2D texture)
    {
        texture.ID?.Dispose();
        texture.ID = null;
    }

    public static bool IsRenderTextureReady(RenderTexture2D target)
    {
        return target.ID != null;
    }

    public static void UnloadRenderTexture(RenderTexture2D target)
    {
        target.ID?.Dispose();
        target.ID = null;
    }

    public static void DrawTexturePro(Texture2D texture, Rectangle source, Rectangle dest, Vector2 origin, float rotation, Color tint)
    {
        window.RenderContext!.DrawColor = ToGlibColor(tint);
        /*window.RenderContext!.Draw(
            tex: texture.ID!,
            src: new Glib.Rectangle(source.X, source.Y, source.Width, source.Height),
            dst: new Glib.Rectangle(dest.X, dest.Y, dest.Width, dest.Height)
        );*/
        window.RenderContext!.PushTransform();
        window.RenderContext!.Translate(dest.X, dest.Y, 0f);
        window.RenderContext!.Rotate(rotation * DEG2RAD);
        window.RenderContext!.Draw(
            tex: texture.ID!,
            src: new Glib.Rectangle(source.X, source.Y, source.Width, source.Height),
            dst: new Glib.Rectangle(-origin.X, -origin.Y, dest.Width, dest.Height)
        );
        window.RenderContext!.PopTransform();
    }

    public static void DrawTextureRec(Texture2D texture, Rectangle source, Vector2 position, Color tint)
    {
        DrawTexturePro(texture, source, new Rectangle(position, source.Width, source.Height), Vector2.Zero, 0f, tint);
    }

    public static void DrawTextureEx(Texture2D texture, Vector2 position, float rotation, float scale, Color tint)
    {
        var w = texture.ID!.Width;
        var h = texture.ID!.Height;

        DrawTexturePro(
            texture,
            new Rectangle(0f, 0f, w, h),
            new Rectangle(position.X, position.Y, w * scale, h * scale),
            Vector2.Zero, rotation,
            tint
        );
    }

    public static void DrawTextureV(Texture2D texture, Vector2 position, Color tint)
    {
        DrawTextureRec(texture, new Rectangle(0f, 0f, texture.ID!.Width, texture.ID!.Height), position, tint);
    }

    public static void DrawTexture(Texture2D texture, int posX, int posY, Color tint)
    {
        DrawTextureV(texture, new Vector2(posX, posY), tint);
    }

    // Not actually a function in Raylib. I just added this because
    // drawing the contents of a framebuffer will be upside down.
    public static void DrawRenderTextureV(RenderTexture2D target, Vector2 pos, Color tint)
    {
        var tex = target.Texture;
        DrawTexturePro(
            texture: target.Texture,
            source: new Rectangle(0f, tex.Height, tex.Width, -tex.Height),
            dest: new Rectangle(pos, tex.Width, tex.Height),
            origin: Vector2.Zero,
            rotation: 0f,
            tint: tint
        );
    }
    
    public static void DrawRenderTexture(RenderTexture2D target, int posX, int posY, Color tint)
        => DrawRenderTextureV(target, new Vector2(posX, posY), tint);

    #endregion

    #region rtext
    public static void DrawText(string text, int posX, int posY, int fontSize, Color color)
    {
        throw new NotImplementedException();
    }
    #endregion
}