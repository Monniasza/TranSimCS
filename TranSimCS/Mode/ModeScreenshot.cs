using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using ImGuiNET;
using Silk.NET.OpenGL;
using TranSimCS.SilkNet;

namespace TranSimCS.Mode {
    public class ModeScreenshot(GameWindow window) : IMode {
        public int Scale = 1;
        public string Filename = "screenshot.png";

        public string Title() => "Screenshot";

        void IMode.DrawUI() {
            if (ImGui.Begin("Screenshot")) {
                ImGui.Text("Capture high-resolution screenshots");

                ImGui.DragInt(
                    "Scale factor",
                    ref Scale,
                    0.005f,
                    1,
                    16);

                var width = Scale * window.SilkWindow.Size.X;
                var height = Scale * window.SilkWindow.Size.Y;

                ImGui.Text($"Resolution: {width} * {height}");

                ImGui.InputText(
                    "File name",
                    ref Filename,
                    1024);

                if (ImGui.Button("Shoot!"))
                    Shoot(width, height);
            }

            ImGui.End();
        }

        private unsafe void Shoot(int width, int height) {
            var gl = window.OpenGL;

            var framebuffer = gl.GenFramebuffer();
            var colorTexture = gl.GenTexture();
            var depthBuffer = gl.GenRenderbuffer();

            try {
                // Create color texture.
                gl.BindTexture(TextureTarget.Texture2D, colorTexture);

                gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba8,
                    (uint)width,
                    (uint)height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    null);

                gl.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureMinFilter,
                    (int)GLEnum.Linear);

                gl.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureMagFilter,
                    (int)GLEnum.Linear);

                // Create depth buffer.
                gl.BindRenderbuffer(
                    RenderbufferTarget.Renderbuffer,
                    depthBuffer);

                gl.RenderbufferStorage(
                    RenderbufferTarget.Renderbuffer,
                    InternalFormat.DepthComponent24,
                    (uint)width,
                    (uint)height);

                // Create framebuffer.
                gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    framebuffer);

                gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    TextureTarget.Texture2D,
                    colorTexture,
                    0);

                gl.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer,
                    depthBuffer);

                gl.DrawBuffers(1, [DrawBufferMode.ColorAttachment0]);

                var status = gl.CheckFramebufferStatus(
                    FramebufferTarget.Framebuffer);

                if (status != GLEnum.FramebufferComplete)
                    throw new Exception(
                        $"Screenshot framebuffer incomplete: {status}");

                // Render scene.
                var scene = window.RenderContents;

                scene.ScreenSize = new(width, height);
                scene.RenderTargetHandle = framebuffer;
                Debug.Assert(scene.SceneGeometry != null, "No scene geometry");

                window.RenderManager.Render(scene);

                // Read RGBA pixels.
                var pixels = new byte[width * height * 4];
                gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
                unsafe {
                    fixed (byte* ptr = pixels) {
                        gl.ReadPixels(
                            0,
                            0,
                            (uint)width,
                            (uint)height,
                            PixelFormat.Rgba,
                            PixelType.UnsignedByte,
                            ptr);
                    }
                }

                // OpenGL's origin is bottom-left.
                FlipVertically(pixels, width, height);

                using var image = new MagickImage();
                var settings = new PixelReadSettings(
                    (uint)width, (uint)height,
                    StorageType.Char, PixelMapping.RGBA
                );

                image.ReadPixels(pixels, settings);

                var directory = Path.Combine(Program.UserRoot, "screenshots", Filename);
                image.Write(directory);
            } finally {
                gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    0);

                gl.DeleteFramebuffer(framebuffer);
                gl.DeleteTexture(colorTexture);
                gl.DeleteRenderbuffer(depthBuffer);
            }
        }

        private static void FlipVertically(
            byte[] pixels,
            int width,
            int height) {
            int rowSize = width * 4;
            byte[] row = new byte[rowSize];

            for (int y = 0; y < height / 2; y++) {
                int top = y * rowSize;
                int bottom = (height - 1 - y) * rowSize;

                pixels.AsSpan(top, rowSize)
                    .CopyTo(row);

                pixels.AsSpan(bottom, rowSize)
                    .CopyTo(pixels.AsSpan(top, rowSize));

                row.AsSpan()
                    .CopyTo(pixels.AsSpan(bottom, rowSize));
            }
        }
    }
}
