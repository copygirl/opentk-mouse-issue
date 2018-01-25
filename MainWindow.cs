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
		
		
		private int _moveX, _moveY;
		private int _deltaX, _deltaY;
		
		private bool _grabCursor = false;
		private bool GrabCursor {
			get => _grabCursor;
			set {
				_grabCursor = value;
				if (HideCursorWhenGrabbing) {
					if (UseEmptyCursorInstead)
						Cursor = value ? MouseCursor.Empty : MouseCursor.Default;
					else CursorVisible = !value;
				}
			}
		}
		
		private bool HideCursorWhenGrabbing { get; set; } = true;
		private bool CenterOnMouseUp { get; set; } = true;
		private bool CenterOnMouseMove { get; set; } = true;
		private bool CenterOnUpdateFrame { get; set; } = false;
		private bool UseEmptyCursorInstead { get; set; } = false;
		
		
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
		{
			GrabCursor = true;
			CenterMouse();
		}
		
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			GrabCursor = false;
			if (CenterOnMouseUp)
				CenterMouse();
		}
		
		protected override void OnMouseMove(MouseMoveEventArgs e)
		{
			_moveX = e.X;
			_moveY = e.Y;
			_deltaX = e.XDelta;
			_deltaY = e.YDelta;
			if (CenterOnMouseMove && GrabCursor)
				CenterMouse();
		}
		
		protected override void OnKeyDown(KeyboardKeyEventArgs e)
		{
			switch (e.Key) {
				case Key.Escape:
					Close();
					break;
				case Key.Number1:
					if (!GrabCursor)
						HideCursorWhenGrabbing = !HideCursorWhenGrabbing;
					break;
				case Key.Number2:
					CenterOnMouseUp = !CenterOnMouseUp;
					break;
				case Key.Number3:
					CenterOnMouseMove = !CenterOnMouseMove;
					CenterOnUpdateFrame = false;
					break;
				case Key.Number4:
					CenterOnUpdateFrame = !CenterOnUpdateFrame;
					CenterOnMouseMove = false;
					break;
				case Key.Number5:
					if (!GrabCursor)
						UseEmptyCursorInstead = !UseEmptyCursorInstead;
					break;
			}
		}
		
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			if (CenterOnUpdateFrame && GrabCursor)
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
			DrawCursor(new Point(_moveX, _moveY), Color.DarkGreen, 10);
			
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
				$"Mouse Position:  { mousePos.X } : { mousePos.Y }");
			
			DrawTextWithOutline(4, 4 + DebugFont.GlyphSpacing.Height * 1, Color.Gold,
				$"Cursor Position: { cursorPos.X } : { cursorPos.Y }\n");
			
			DrawTextWithOutline(4, 4 + DebugFont.GlyphSpacing.Height * 2, Color.LimeGreen,
				$"Move Position:   { _moveX } : { _moveY }\n" +
				$"Move Delta:      { _deltaX } : { _deltaY }");
			
			DrawTextWithOutline(4, 4 + DebugFont.GlyphSpacing.Height * 5, Color.Silver,
				$"Click left mouse button to grab mouse.\n" +
				$"Press number keys to enable/disable features:\n" +
				$"(1) { nameof(HideCursorWhenGrabbing) }: { HideCursorWhenGrabbing }\n" +
				$"(2) { nameof(CenterOnMouseUp) }: { CenterOnMouseUp }\n" +
				$"(3) { nameof(CenterOnMouseMove) }: { CenterOnMouseMove }\n" +
				$"(4) { nameof(CenterOnUpdateFrame) }: { CenterOnUpdateFrame }\n" +
				$"(5) { nameof(UseEmptyCursorInstead) }: { UseEmptyCursorInstead }");
			
			GL.Disable(EnableCap.Texture2D);
			
			SwapBuffers();
		}
		
		
		private void CenterMouse()
		{
			var centerX = Bounds.Left + Bounds.Width / 2;
			var centerY = Bounds.Top + Bounds.Height / 2;
			Mouse.SetPosition(centerX, centerY);
		}
	}
}
