using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using ImGuiNET;
using NLog;
using Silk.NET.OpenGL;
using TranSimCS.SilkNet;

namespace TranSimCS.Mode {
    public class ModeScreenshot(GameWindow window) : IMode {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public int Scale = 1;
        public string Filename = "screenshot.jpg";
        public int Quality = 95;

        public string Title() => "Screenshot";

        void IMode.DrawUI() {
            if (ImGui.Begin("Screenshot")) {
                ImGui.Text("Capture high-resolution screenshots");
                ImGui.DragInt("Quality (lossy compression)", ref Quality, 0.1f, 0, 100);

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
            //Measure times
            Stopwatch createBuffersStopwatch = new();
            Stopwatch renderSceneStopwatch = new();
            Stopwatch dumpPixelsStopwatch = new();
            Stopwatch saveStopwatch = new();

            var gl = window.OpenGL;

            var framebuffer = gl.GenFramebuffer();
            var colorTexture = gl.GenTexture();
            var depthBuffer = gl.GenRenderbuffer();
            logger.Info($"Shooting {Filename} at {width}*{height}");

            try {
                // Create color texture.
                createBuffersStopwatch.Start();
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
                createBuffersStopwatch.Stop();
                logger.Info($"Create buffers: {createBuffersStopwatch.ElapsedMilliseconds} ms");

                // Render scene.
                renderSceneStopwatch.Start();
                var scene = window.RenderContents;

                scene.ScreenSize = new(width, height);
                scene.RenderTargetHandle = framebuffer;
                Debug.Assert(scene.SceneGeometry != null, "No scene geometry");

                window.RenderManager.Render(scene);
                renderSceneStopwatch.Stop();

                logger.Info($"Render: {renderSceneStopwatch.ElapsedMilliseconds} ms");


                // Read RGBA pixels.
                dumpPixelsStopwatch.Start();
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
                dumpPixelsStopwatch.Stop();

                logger.Info($"GL to RAM pixel dump: {dumpPixelsStopwatch.ElapsedMilliseconds} ms");

                Stopwatch toMagickSW = Stopwatch.StartNew();
                using var image = new MagickImage();
                var settings = new PixelReadSettings(
                    (uint)width, (uint)height,
                    StorageType.Char, PixelMapping.RGBA
                );
                image.ReadPixels(pixels, settings);
                image.Quality = (uint)Quality;
                toMagickSW.Stop();
                logger.Info($"RAM to Magick.NET dump: {toMagickSW.ElapsedMilliseconds} ms");

                Stopwatch flip = Stopwatch.StartNew();
                image.Flip();
                flip.Stop();
                logger.Info($"Flip: {flip.ElapsedMilliseconds} ms");

                saveStopwatch.Start();
                var directory = Path.Combine(Program.UserRoot, "screenshots", Filename);
                DataUtil.ValidateDeviceName(directory);
                image.Write(directory);
                saveStopwatch.Stop();
                logger.Info($"Save: {saveStopwatch.ElapsedMilliseconds} ms");
            } catch(Exception e){
                logger.Error(e);
                var message = Message.ErrorMessage("Failed to save the screenshot", e, window);
                window.CurrentlyOpenModal = message.ShowMessage;
            }finally {
                gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    0);

                gl.DeleteFramebuffer(framebuffer);
                gl.DeleteTexture(colorTexture);
                gl.DeleteRenderbuffer(depthBuffer);
            }
        }
    }
}
