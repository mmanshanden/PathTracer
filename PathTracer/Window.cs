using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;

namespace PathTracer
{
    class Window : GameWindow
    {
        private readonly Game game;

        private string fps;
        private int frames;
        private double time;

        private KeyboardState keystate;

        public Window(int width, int height)
            : base(width, height)
        {
            this.game = new Game();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Title = string.Empty;
            this.VSync = VSyncMode.Off;
            this.game.Initialize();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, this.Width, this.Height);
            this.game.Resize(this.Width, this.Height);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                this.game.Dispose();
                this.Exit();
            }

            this.keystate = e.Keyboard;
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            this.keystate = e.Keyboard;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            
            this.game.Update((float)e.Time, this.keystate);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            this.game.Draw((float)e.Time);
            this.SwapBuffers();

            if (this.time > 0.5)
            {
                this.fps = (this.frames << 1) + " | " + (this.time / this.frames) + " | " + this.game.Samples;
                this.time = 0.0;
                this.frames = 0;
            }

            this.time += e.Time;
            this.frames += 1;

            this.Title = fps;
        }
    }
}
