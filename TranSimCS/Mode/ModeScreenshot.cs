using System;
using System.Collections.Generic;
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

        public string Title() => "Screenshot";
        public string Filename = "screenshot.png";

        void IMode.DrawUI() {
            if (ImGui.Begin("Screenshot")) {
                ImGui.Text("Capture high-resolution screenshots");
                ImGui.DragInt("Scale factor", ref Scale, 0.005f, 1, 16);

                var width = Scale * window.SilkWindow.Size.X;
                var height = Scale * window.SilkWindow.Size.Y;
                ImGui.Text($"Resolution: {width} * {height}");
                if (ImGui.Button("Shoot!")) {
                    Shoot();
                }
            }
        }

        private void Shoot() {
            //Create a temporary buffer, render to it, dump pixels, and export
            var gl = window.OpenGL;

            var windowSize = window.SilkWindow.Size;

            var width = Scale * windowSize.X;
            var height = Scale * windowSize.Y;

            var scene = window.RenderContents;

            var oldSize = scene.ScreenSize;
            var oldTarget = scene.RenderTargetHandle;

            uint framebuffer = 0;
            uint colorTexture = 0;
            uint depthBuffer = 0;

            try {
                framebuffer = CreateFramebuffer(
                    gl,
                    width,
                    height,
                    out colorTexture,
                    out depthBuffer);

                scene.ScreenSize = new(width, height);
                scene.RenderTargetHandle = framebuffer;

                window.RenderManager.Render(scene);

                // Read pixels here.
                var pixels = new byte[width * height * 4];

                gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    framebuffer);

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

                // Export pixels here.
                // OpenGL's origin is bottom-left.
                FlipVertically(pixels, width, height);

                using var image = new MagickImage(
                    pixels,
                    new MagickReadSettings {
                        Width = (uint)width,
                        Height = (uint)height,
                        Format = MagickFormat.Rgba
                    });

                image.Write(Filename);
            } finally {
                scene.ScreenSize = oldSize;
                scene.RenderTargetHandle = oldTarget;

                gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    0);

                if (framebuffer != 0)
                    gl.DeleteFramebuffer(framebuffer);

                if (colorTexture != 0)
                    gl.DeleteTexture(colorTexture);

                if (depthBuffer != 0)
                    gl.DeleteRenderbuffer(depthBuffer);
            }
        }

        private static unsafe uint CreateFramebuffer(
            GL gl,
            int width,
            int height,
            out uint colorTexture,
            out uint depthBuffer) {
            colorTexture = gl.GenTexture();

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

            depthBuffer = gl.GenRenderbuffer();

            gl.BindRenderbuffer(
                RenderbufferTarget.Renderbuffer,
                depthBuffer);

            gl.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                InternalFormat.Depth24,
                (uint)width,
                (uint)height);

            var framebuffer = gl.GenFramebuffer();

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

            var status = gl.CheckFramebufferStatus(
                FramebufferTarget.Framebuffer);

            if (status != GLEnum.FramebufferComplete)
                throw new Exception(
                    $"Snapshot framebuffer is incomplete: {status}");

            gl.BindFramebuffer(
                FramebufferTarget.Framebuffer,
                0);

            return framebuffer;
        }
    }
}
