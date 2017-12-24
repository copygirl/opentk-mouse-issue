using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace MouseIssue
{
    class Game : GameWindow
    {
        static void Main(string[] args)
            => new Game().Run();

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(ClientRectangle);

            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, Width, Height, 0, -100, 100);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
            => CursorVisible = false;

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
            => CursorVisible = true;

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!CursorVisible) {
                var centerX = Bounds.Left + Bounds.Width / 2;
                var centerY = Bounds.Top + Bounds.Height / 2;
                Mouse.SetPosition(centerX, centerY);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var mouseState = Mouse.GetCursorState();
            var mousePos = PointToClient(new Point(mouseState.X, mouseState.Y));
            var cursorSize = 12;
            GL.Begin(PrimitiveType.Triangles);
                GL.Color3(1.0f, 1.0f, 0.0f); GL.Vertex3(mousePos.X, mousePos.Y, 0);
                GL.Color3(1.0f, 0.0f, 0.0f); GL.Vertex3(mousePos.X + cursorSize, mousePos.Y, 0);
                GL.Color3(0.2f, 0.9f, 1.0f); GL.Vertex3(mousePos.X, mousePos.Y + cursorSize, 0);
            GL.End();

            SwapBuffers();
        }
    }
}
