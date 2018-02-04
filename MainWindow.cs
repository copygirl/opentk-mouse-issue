using System;
using System.Diagnostics;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace ModelViewer
{
	class MainWindow : GameWindow
	{
		public MonoBitmapFont DebugFont { get; private set; }
		
		
		private bool _mousePressed;
		private bool MousePressed {
			get => _mousePressed;
			set {
				_mousePressed = value;
				if (UseCursorGrabbed) CursorGrabbed = value;
				if (UseCursorVisible) CursorVisible = !value;
				if (UseCursorEmpty) Cursor = value
					? MouseCursor.Empty : MouseCursor.Default;
				if (value ? CenterOnMouseDown : CenterOnMouseUp)
					CenterMouse();
			}
		}
		
		private Point MouseMovePos { get; set; }
		private Point MouseMoveDelta { get; set; }
		
		private bool UseCursorGrabbed { get; set; } = true;
		private bool UseCursorVisible { get; set; } = true;
		private bool UseCursorEmpty { get; set; } = false;
		
		private bool CenterOnMouseDown { get; set; } = true;
		private bool CenterOnMouseUp { get; set; } = false;
		private bool CenterOnMouseMove { get; set; } = true;
		private bool CenterOnUpdateFrame { get; set; } = false;
		
		private Point? _swallowMouseMovePos = null;
		private bool SwallowSetPositionMouseMove { get; set; } = true;
		
		
		static void Main(string[] args)
			=> new MainWindow().Run();
		
		protected override void OnLoad(EventArgs e)
		{
			DebugFont = MonoBitmapFont.GenerateFromDefaultMonoFont(10);
			
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.DepthMask(false);
		}
		
		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(ClientRectangle);
			
			Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, Width, Height, 0, -100, 100);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);
		}
		
		
		protected override void OnMouseDown(MouseButtonEventArgs e)
			=> MousePressed = true;
		
		protected override void OnMouseUp(MouseButtonEventArgs e)
			=> MousePressed = false;
		
		protected override void OnMouseMove(MouseMoveEventArgs e)
		{
			MouseMovePos = new Point(e.X, e.Y);
			if (_swallowMouseMovePos == MouseMovePos)
				{ _swallowMouseMovePos = null; return; }
			MouseMoveDelta = new Point(e.XDelta, e.YDelta);
			if (MousePressed && CenterOnMouseMove)
				CenterMouse();
		}
		
		protected override void OnKeyDown(KeyboardKeyEventArgs e)
		{
			switch (e.Key) {
				case Key.Escape:
					Exit();
					break;
				
				case Key.Number1:
					if (MousePressed) break;
					UseCursorGrabbed = !UseCursorGrabbed;
					break;
				case Key.Number2:
					if (MousePressed) break;
					UseCursorVisible = !UseCursorVisible;
					UseCursorEmpty = false;
					break;
				case Key.Number3:
					if (MousePressed) break;
					UseCursorEmpty = !UseCursorEmpty;
					UseCursorVisible = false;
					break;
				
				case Key.Number4:
					CenterOnMouseDown = !CenterOnMouseDown;
					break;
				case Key.Number5:
					CenterOnMouseUp = !CenterOnMouseUp;
					break;
				case Key.Number6:
					CenterOnMouseMove = !CenterOnMouseMove;
					CenterOnUpdateFrame = false;
					break;
				case Key.Number7:
					CenterOnUpdateFrame = !CenterOnUpdateFrame;
					CenterOnMouseMove = false;
					break;
				
				case Key.Number8:
					SwallowSetPositionMouseMove = !SwallowSetPositionMouseMove;
					break;
			}
		}
		
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			if (MousePressed && CenterOnUpdateFrame)
				CenterMouse();
		}
		
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GL.ClearColor(Color.MidnightBlue);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			
			void DrawCursor(Point pos, Color color, int size)
			{
				GL.Begin(PrimitiveType.Triangles);
					GL.Color3(color); GL.Vertex3(pos.X, pos.Y, 0);
					GL.Color3(color); GL.Vertex3(pos.X, pos.Y - size, 0);
					GL.Color3(color); GL.Vertex3(pos.X - size, pos.Y, 0);
				GL.End();
			}
			
			var cursorState = Mouse.GetCursorState();
			var cursorPos   = PointToClient(new Point(cursorState.X, cursorState.Y));
			DrawCursor(cursorPos, Color.Gold, 16);
			var mouseState = Mouse.GetState();
			var mousePos   = new Point(mouseState.X, mouseState.Y);
			DrawCursor(mousePos, Color.DarkRed, 12);
			DrawCursor(MouseMovePos, Color.DarkGreen, 10);
			
			GL.BindTexture(TextureTarget.Texture2D, DebugFont.GLTexture);
			GL.Enable(EnableCap.Texture2D);
			
			void DrawTextWithOutline(int x, int y, Color color, String text)
			{
				GL.Color3(Color.Black);
				GL.Begin(PrimitiveType.Quads);
					DebugFont.DrawVerts(x + 1, y + 1, text);
				GL.End();
				
				GL.Color3(color);
				GL.Begin(PrimitiveType.Quads);
					DebugFont.DrawVerts(x, y, text);
				GL.End();
			}
			
			DrawTextWithOutline(4, 4, Color.Red,
				$"Mouse Position:  { mousePos }");
			
			DrawTextWithOutline(4, 4 + DebugFont.GlyphSpacing.Height * 1, Color.Gold,
				$"Cursor Position: { cursorPos }\n");
			
			DrawTextWithOutline(4, 4 + DebugFont.GlyphSpacing.Height * 2, Color.LimeGreen,
				$"Move Position:   { MouseMovePos }\n" +
				$"Move Delta:      { MouseMoveDelta }");
			
			DrawTextWithOutline(4, 4 + DebugFont.GlyphSpacing.Height * 5, Color.Silver,
				$"Press number keys to enable/disable features:\n" +
				$"(1) { nameof(UseCursorGrabbed) }: { UseCursorGrabbed }\n" +
				$"(2) { nameof(UseCursorVisible) }: { UseCursorVisible }\n" +
				$"(3) { nameof(UseCursorEmpty) }: { UseCursorEmpty }\n" +
				$"\n" +
				$"(4) { nameof(CenterOnMouseDown) }: { CenterOnMouseDown }\n" +
				$"(5) { nameof(CenterOnMouseUp) }: { CenterOnMouseUp }\n" +
				$"(6) { nameof(CenterOnMouseMove) }: { CenterOnMouseMove }\n" +
				$"(7) { nameof(CenterOnUpdateFrame) }: { CenterOnUpdateFrame }\n" +
				$"\n" +
				$"(8) { nameof(SwallowSetPositionMouseMove) }: { SwallowSetPositionMouseMove }\n" +
				$"\n" +
				$"(Esc) Exit");
			
			GL.Disable(EnableCap.Texture2D);
			
			SwapBuffers();
		}
		
		
		private void CenterMouse()
		{
			var centerWindow = new Point(Width / 2, Height / 2);
			var centerScreen = PointToScreen(centerWindow);
			Mouse.SetPosition(centerScreen.X, centerScreen.Y);
			if (SwallowSetPositionMouseMove)
				_swallowMouseMovePos = centerWindow;
		}
	}
}
