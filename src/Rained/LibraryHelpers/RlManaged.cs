/**
* Managed wrappers around Raylib resources.
* This is non-exhaustive
*/
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Raylib_cs;

namespace RlManaged
{
    abstract class RlObject : IDisposable
    {
        private readonly static Mutex freeQueueMut = new();
        private readonly static Queue<object> freeQueue = new();

        private bool _disposed = false;
        private long bytesAllocated = 0;

        ~RlObject() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract object GetHandle();

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (bytesAllocated > 0) GC.RemoveMemoryPressure(bytesAllocated);
                bytesAllocated = 0;

                if (disposing)
                {
                    UnloadRlHandle(GetHandle(), true);
                }
                else
                {
                    freeQueueMut.WaitOne();
                    freeQueue.Enqueue(GetHandle());
                    freeQueueMut.ReleaseMutex();
                }

                _disposed = true;
            }
        }

        protected void AddMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated == 0) return;
            GC.AddMemoryPressure(bytesAllocated);
            this.bytesAllocated += bytesAllocated;
        }

        private static void UnloadRlHandle(object h, bool disposed)
        {
            switch (h)
            {
                case Raylib_cs.RenderTexture2D rTex:
                    Raylib.UnloadRenderTexture(rTex);
                    break;
                
                case Raylib_cs.Image img:
                    Raylib.UnloadImage(img);
                    break;
                
                case Raylib_cs.Texture2D tex:
                    Raylib.UnloadTexture(tex);
                    break;
                
                case Raylib_cs.Shader shader:
                    Raylib.UnloadShader(shader);
                    break;
                
                case Raylib_cs.Mesh mesh:
                {
                    // manually wrote the definition of Raylib.UnloadMesh
                    // for some reason, calling the UnloadMesh binding crashes
                    // the program on my Debian Linux machine, and this is the
                    // only workaround i found that fixed it.                    
                    unsafe
                    {
                        Rlgl.UnloadVertexArray(mesh.VaoId);
                        if (mesh.VboId != null) for (int i = 0; i < 7; i++) Rlgl.UnloadVertexBuffer(mesh.VboId[i]);
                        Raylib.MemFree(mesh.VboId);

                        Raylib.MemFree(mesh.Vertices);
                        Raylib.MemFree(mesh.TexCoords);
                        Raylib.MemFree(mesh.Normals);
                        Raylib.MemFree(mesh.Colors);
                        Raylib.MemFree(mesh.Tangents);
                        Raylib.MemFree(mesh.TexCoords2);
                        Raylib.MemFree(mesh.Indices);

                        Raylib.MemFree(mesh.AnimVertices);
                        Raylib.MemFree(mesh.AnimNormals);
                        Raylib.MemFree(mesh.BoneWeights);
                        Raylib.MemFree(mesh.BoneIds);
                    }
                    break;
                }
                
                case Raylib_cs.Material mat:
                    Raylib.UnloadMaterial(mat);
                    break;
            }
        }

        public static void UnloadGCQueue()
        {
            freeQueueMut.WaitOne();
            foreach (var handle in freeQueue)
            {
                UnloadRlHandle(handle, false);
            }
            freeQueue.Clear();
            freeQueueMut.ReleaseMutex();
        }
    }

    class RenderTexture2D : RlObject
    {
        private Raylib_cs.RenderTexture2D raw;
        protected override object GetHandle() => raw;

        private RenderTexture2D(Raylib_cs.RenderTexture2D raw) : base()
        {
            this.raw = raw;
        }

        public static RenderTexture2D Load(int width, int height, bool alpha = true)
            => alpha ? LoadWithAlpha(width, height) : LoadNoAlpha(width, height);

        private static RenderTexture2D LoadWithAlpha(int width, int height)
            => new(Raylib.LoadRenderTexture(width, height));

        // copied from raylib source code, but with a different pixel format
        private unsafe static RenderTexture2D LoadNoAlpha(int width, int height)
        {
            Raylib_cs.RenderTexture2D raw = new()
            {
                Id = Rlgl.LoadFramebuffer(width, height)
            };

            if (raw.Id > 0)
            {
                Rlgl.EnableFramebuffer(raw.Id);

                // create color texture
                raw.Texture.Id = Rlgl.LoadTexture(null, width, height, PixelFormat.UncompressedR8G8B8, 1);
                raw.Texture.Width = width;
                raw.Texture.Height = height;
                raw.Texture.Format = PixelFormat.UncompressedR8G8B8;
                raw.Texture.Mipmaps = 1;

                // create depth renderbuffer/texture
                raw.Depth.Id = Rlgl.LoadTextureDepth(width, height, true);
                raw.Depth.Width = width;
                raw.Depth.Height = height;
                raw.Depth.Format = PixelFormat.CompressedPvrtRgba;
                raw.Depth.Mipmaps = 1;

                Rlgl.FramebufferAttach(raw.Id, raw.Texture.Id, FramebufferAttachType.ColorChannel0, FramebufferAttachTextureType.Texture2D, 0);
                Rlgl.FramebufferAttach(raw.Id, raw.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Renderbuffer, 0);

                // check if fbo is complete with attachments (valid)
                if (Rlgl.FramebufferComplete(raw.Id))
                    Raylib.TraceLog(TraceLogLevel.Info, $"FBO: [ID {raw.Id}] Framebuffer object created successfully");
                
                Rlgl.DisableFramebuffer();
            }
            else Raylib.TraceLog(TraceLogLevel.Warning, "FBO: Framebuffer object can not be created");

            return new RenderTexture2D(raw);
        }

        // OpenGL Framebuffer object ID
        public uint Id { get => raw.Id; }

        // Color buffer attachment texture
        public Raylib_cs.Texture2D Texture { get => raw.Texture; }

        // Depth buffer attachment texture
        public Raylib_cs.Texture2D Depth { get => raw.Depth; }

        public static implicit operator Raylib_cs.RenderTexture2D(RenderTexture2D tex) => tex.raw;
    }

    class Image : RlObject
    {
        private Raylib_cs.Image raw;
        protected override object GetHandle() => raw;

        public int Width { get => raw.Width; }
        public int Height { get => raw.Height; }
        public int Mipmaps { get => raw.Mipmaps; }
        public PixelFormat PixelFormat { get => raw.Format; }
        public unsafe void* Data { get => raw.Data; }

        private static int GetBitsPerPixel(PixelFormat format)
        {
            return format switch
            {
                PixelFormat.UncompressedGrayscale => 8,
                PixelFormat.UncompressedGrayAlpha => 16,
                PixelFormat.UncompressedR5G6B5 => 16,
                PixelFormat.UncompressedR8G8B8 => 24,
                PixelFormat.UncompressedR5G5B5A1 => 16,
                PixelFormat.UncompressedR4G4B4A4 => 16,
                PixelFormat.UncompressedR8G8B8A8 => 32,
                PixelFormat.UncompressedR32 => 32,
                PixelFormat.UncompressedR32G32B32 => 32*3,
                PixelFormat.UncompressedR32G32B32A32 => 32*4,
                PixelFormat.CompressedDxt1Rgb => 4,
                PixelFormat.CompressedDxt1Rgba => 4,
                PixelFormat.CompressedDxt3Rgba => 8,
                PixelFormat.CompressedDxt5Rgba => 8,
                PixelFormat.CompressedEtc1Rgb => 4,
                PixelFormat.CompressedEtc2Rgb => 4,
                PixelFormat.CompressedEtc2EacRgba => 8,
                PixelFormat.CompressedPvrtRgb => 4,
                PixelFormat.CompressedPvrtRgba => 4,
                PixelFormat.CompressedAstc4X4Rgba => 8,
                PixelFormat.CompressedAstc8X8Rgba => 2,
                _ => throw new Exception("Invalid pixel format")
            };
        }
        
        private Image(Raylib_cs.Image raw) : base()
        {
            this.raw = raw;

            if (raw.Format != 0)
                AddMemoryPressure((long)raw.Width * raw.Height * GetBitsPerPixel(raw.Format) / 8);
        }

        public static Image Load(string fileName)
            => new(Raylib.LoadImage(fileName));

        public static Image LoadFromTexture(Raylib_cs.Texture2D texture)
            => new (Raylib.LoadImageFromTexture(texture));
        
        public static Image GenColor(int width, int height, Color color)
            => new(Raylib.GenImageColor(width, height, color));
        
        public static Image FromImage(Raylib_cs.Image image, Rectangle rec)
            => new(Raylib.ImageFromImage(image, rec));
        
        public static Image Copy(Raylib_cs.Image image)
            => new(Raylib.ImageCopy(image));

        public unsafe void DrawPixel(int x, int y, Color color)
        {
            fixed (Raylib_cs.Image* rawPtr = &raw)
                Raylib.ImageDrawPixel(rawPtr, x, y, color);
        }

        public unsafe byte[] ExportToMemory(string format)
        {
            int fileSize = 0;
            byte[] formatBytes = Encoding.ASCII.GetBytes(format);
            char* memBuf = null;

            fixed (byte* formatBytesPtr = formatBytes)
            {
                memBuf = Raylib.ExportImageToMemory(raw, (sbyte*) formatBytesPtr, &fileSize);
            }

            if (memBuf == null)
                throw new Exception();
            
            byte[] result = new byte[fileSize];
            Marshal.Copy((nint)memBuf, result, 0, fileSize);
            Raylib.MemFree(memBuf);

            return result;
        }
        
        public unsafe void UpdateTexture(Raylib_cs.Texture2D texture)
        {
            Raylib.UpdateTexture(texture, raw.Data);
        }
        
        public unsafe void Format(PixelFormat newFormat)
        {
            fixed (Raylib_cs.Image* rawPtr = &raw)
                Raylib.ImageFormat(rawPtr, newFormat);
        }

        public static implicit operator Raylib_cs.Image(Image tex) => tex.raw;

        public ref Raylib_cs.Image Ref() => ref raw;
    }

    class Texture2D : RlObject
    {
        private Raylib_cs.Texture2D raw;
        protected override object GetHandle() => raw;

        public uint Id { get => raw.Id; }
        public int Width { get => raw.Width; }
        public int Height { get => raw.Height; }

        private Texture2D(Raylib_cs.Texture2D raw)
        {
            this.raw = raw;
        }

        public static Texture2D LoadFromImage(Raylib_cs.Image image)
            => new(Raylib.LoadTextureFromImage(image));

        public static Texture2D Load(string fileName)
            => new(Raylib.LoadTexture(fileName));
        
        public static implicit operator Raylib_cs.Texture2D(Texture2D tex) => tex.raw;
    }

    class Shader : RlObject
    {
        private Raylib_cs.Shader raw;
        protected override object GetHandle() => raw;
        
        private Shader(Raylib_cs.Shader src)
        {
            raw = src;
        }

        public static Shader LoadFromMemory(string? vsCode, string? fsCode)
            => new(Raylib.LoadShaderFromMemory(vsCode, fsCode));
        
        public static Shader Load(string? vsFileName, string? fsFileName)
            => new(Raylib.LoadShader(vsFileName, fsFileName));

        public static implicit operator Raylib_cs.Shader(Shader tex) => tex.raw;
    }

    class Mesh : RlObject
    {
        private enum MeshIndex : int
        {
            Vertices = 0,
            TexCoords = 1,
            Normals = 2,
            Colors = 3,
            Tangents = 4,
            TexCoords2 = 5,
            Indices = 6,
        }

        private Raylib_cs.Mesh raw;
        protected override object GetHandle() => raw;

        public Mesh() : base()
        {
            raw = new Raylib_cs.Mesh();
        }

        /*public unsafe void SetVertices(float[] vertices)
        {
            if (vertices.Length % 3 != 0)
                throw new Exception("Vertex array is not a multiple of 3");

            if (raw.Vertices != null)
                Marshal.FreeHGlobal((nint) raw.Vertices);

            raw.VertexCount = vertices.Length / 3;
            raw.Vertices = (float*) Marshal.AllocHGlobal(vertices.Length * sizeof(float));
            Marshal.Copy(vertices, 0, (IntPtr) raw.Vertices, vertices.Length);
        }*/

        public unsafe void SetVertices(ReadOnlySpan<Vector3> vertices)
        {
            if (raw.Vertices != null)
                Raylib.MemFree(raw.Vertices);
            
            raw.VertexCount = vertices.Length;
            AddMemoryPressure(raw.VertexCount * 3 * sizeof(float));

            raw.AllocVertices();
            var k = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                raw.Vertices[k++] = vertices[i].X;
                raw.Vertices[k++] = vertices[i].Y;
                raw.Vertices[k++] = vertices[i].Z;
            }
        }

        public unsafe void UpdateVertices()
        {
            Raylib.UpdateMeshBuffer(raw, (int) MeshIndex.Vertices, raw.Vertices, raw.VertexCount * 3 * sizeof(float), 0);
        }

        public unsafe void SetIndices(ReadOnlySpan<ushort> indices)
        {
            if (indices.Length % 3 != 0)
                throw new Exception("Indices array is not a multiple of 3");
            
            if (raw.Indices != null)
                Raylib.MemFree(raw.Indices);
            
            raw.TriangleCount = indices.Length / 3;
            AddMemoryPressure(raw.TriangleCount * 3 * sizeof(float));

            raw.AllocIndices();
            fixed (ushort* arrPtr = indices)
            {
                Buffer.MemoryCopy(arrPtr, raw.Indices, indices.Length * sizeof(ushort), indices.Length * sizeof(ushort));
            }
        }

        public unsafe void UpdateIndices()
        {
            Raylib.UpdateMeshBuffer(raw, (int) MeshIndex.Indices, raw.Indices, raw.TriangleCount * 3 * sizeof(float), 0);
        }

        /*public unsafe void SetColors(byte[] colors)
        {
            if (colors.Length % 4 != 0)
                throw new Exception("Colors array is not a multiple of 4");
            
            if (raw.Colors != null)
                Marshal.FreeHGlobal((nint) raw.Colors);
            
            raw.Colors = (byte*) Marshal.AllocHGlobal(colors.Length * sizeof(byte));
            Marshal.Copy(colors, 0, (nint) raw.Colors, colors.Length);
        }*/

        public unsafe void SetColors(ReadOnlySpan<Color> colors)
        {
            if (raw.Colors != null)
                Raylib.MemFree(raw.Colors);
            
            AddMemoryPressure(raw.VertexCount * 4 * sizeof(float));

            raw.AllocColors();
            
            int k = 0;
            for (int i = 0; i < colors.Length; i++)
            {
                raw.Colors[k++] = colors[i].R;
                raw.Colors[k++] = colors[i].G;
                raw.Colors[k++] = colors[i].B;
                raw.Colors[k++] = colors[i].A;
            }
        }

        public unsafe void UpdateColors()
        {
            Raylib.UpdateMeshBuffer(raw, (int) MeshIndex.Colors, raw.Colors, raw.VertexCount * 4 * sizeof(byte), 0);
        }

        public unsafe void SetTexCoords(ReadOnlySpan<Vector2> uvs)
        {
            if (raw.TexCoords != null)
                Raylib.MemFree(raw.TexCoords);
            
            AddMemoryPressure(raw.VertexCount * 2 * sizeof(float));

            raw.AllocTexCoords();
            
            int k = 0;
            for (int i = 0; i < uvs.Length; i++)
            {
                raw.TexCoords[k++] = uvs[i].X;
                raw.TexCoords[k++] = uvs[i].Y;
            }
        }

        public unsafe void SetNormals(ReadOnlySpan<Vector3> normals)
        {
            if (raw.Normals != null)
                Raylib.MemFree(raw.Normals);

            AddMemoryPressure(raw.VertexCount * 3 * sizeof(float));
            raw.AllocNormals();

            int k = 0;
            for (int i = 0; i < normals.Length; i++)
            {
                raw.Normals[k++] = normals[i].X;
                raw.Normals[k++] = normals[i].Y;
                raw.Normals[k++] = normals[i].Z;
            }
        }

        public void UploadMesh(bool dynamic)
        {
            Raylib.UploadMesh(ref raw, dynamic);
        }

        public static implicit operator Raylib_cs.Mesh(Mesh mesh) => mesh.raw;
    }

    class Material : RlObject
    {
        private Raylib_cs.Material raw;
        protected override object GetHandle() => raw;

        private Material(Raylib_cs.Material raw) : base()
        {
            this.raw = raw;
        }

        public unsafe MaterialMap* Maps { get => raw.Maps; }

        public static Material LoadMaterialDefault()
            => new(Raylib.LoadMaterialDefault());
        
        public static implicit operator Raylib_cs.Material(Material mat) => mat.raw;
    }
}