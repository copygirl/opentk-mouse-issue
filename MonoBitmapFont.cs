using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using OpenTK.Graphics.OpenGL;

namespace ModelViewer
{
	using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
	using OpenGLPixelFormat  = OpenTK.Graphics.OpenGL.PixelFormat;
	
	public class MonoBitmapFont : IDisposable
	{
		public Bitmap Bitmap { get; }
		public int GLTexture { get; }
		
		public int GlyphsPerLine { get; }
		public int NumGlyphs { get; }
		public int NumLines { get; }
		
		public char StartChar { get; }
		public char EndChar { get; }
		
		public Size GlyphSize { get; }
		public Size GlyphSpacing { get; }
		
		public char UnknownCharacter { get; set; } = '?';
		public bool Disposed { get; private set; }
		
		public MonoBitmapFont(Bitmap bitmap,
			Size glyphSpacing, int glyphsPerLine = 16,
			char startChar = ' ', char endChar = '~')
		{
			if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
			if (glyphsPerLine <= 0) throw new ArgumentException(
				"glyphsPerLine must be positive", nameof(glyphsPerLine));
			if (endChar < startChar) throw new ArgumentException(
				"endChar can't be before startChar");
			
			Bitmap = bitmap;
			
			GlyphsPerLine = glyphsPerLine;
			NumGlyphs     = (int)endChar - (int)startChar + 1;
			NumLines      = (NumGlyphs - 1) / glyphsPerLine + 1;
			
			StartChar = startChar;
			EndChar   = endChar;
			
			if (bitmap.Width % GlyphsPerLine != 0) throw new ArgumentException(
				$"bitmap.Width ({ bitmap.Width }) is not divisible by glyphsPerLine ({ glyphsPerLine })");
			if (bitmap.Height % NumLines != 0) throw new ArgumentException(
				$"bitmap.Height ({ bitmap.Height }) is not divisible by NumLines ({ NumLines })");
			
			GlyphSize = new Size(bitmap.Width / GlyphsPerLine,
			                     bitmap.Height / NumLines);
			GlyphSpacing = glyphSpacing;
			
			
			GLTexture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, GLTexture);
			
			var rect = new Rectangle(Point.Empty, bitmap.Size);
			var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, DrawingPixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D,
				0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height,
				0, OpenGLPixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			bitmap.UnlockBits(data);
			
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
			                                         (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
			                                         (int)TextureMagFilter.Nearest);
		}
		
		public void Dispose()
		{
			if (Disposed) return;
			Bitmap.Dispose();
			GL.DeleteTexture(GLTexture);
			Disposed = true;
		}
		
		
		public static MonoBitmapFont GenerateFromDefaultMonoFont(
			int size, FontStyle style = FontStyle.Bold,
			SmoothingMode smoothingMode     = SmoothingMode.HighSpeed,
			TextRenderingHint renderingHint = TextRenderingHint.AntiAliasGridFit)
				=> GenerateFromFont(new Font(FontFamily.GenericMonospace, size, style),
				                    16, ' ', '~', smoothingMode, renderingHint);
		
		public static MonoBitmapFont GenerateFromFont(Font font,
			int glyphsPerLine = 16, char startChar = ' ', char endChar = '~',
			SmoothingMode smoothingMode     = SmoothingMode.HighSpeed,
			TextRenderingHint renderingHint = TextRenderingHint.AntiAliasGridFit)
		{
			if (font == null) throw new ArgumentNullException(nameof(font));
			if (glyphsPerLine <= 0) throw new ArgumentException(
				"glyphsPerLine must be positive", nameof(glyphsPerLine));
			if (endChar < startChar) throw new ArgumentException(
				"endChar can't be before startChar");
			
			var numGlyphs = (int)endChar - (int)startChar + 1;
			var numLines  = (numGlyphs - 1) / glyphsPerLine + 1;
			
			Size glyphSize, glyphSpacing;
			using (var gfx = Graphics.FromHwnd(IntPtr.Zero)) {
				gfx.TextRenderingHint = TextRenderingHint.AntiAlias;
				glyphSize    = Size.Ceiling(gfx.MeasureString(" ", font) + new Size(1, 1));
				glyphSpacing = Size.Round(gfx.MeasureString(" ", font,
					int.MaxValue, StringFormat.GenericTypographic) + new Size(1, 2));
			}
			
			var width  = glyphSize.Width  * glyphsPerLine;
			var height = glyphSize.Height * numLines;
			var bitmap = new Bitmap(width, height, DrawingPixelFormat.Format32bppArgb);
			
			using (var gfx = Graphics.FromImage(bitmap)) {
				gfx.SmoothingMode     = smoothingMode;
				gfx.TextRenderingHint = renderingHint;
				
				for (var i = 0; i < numGlyphs; i++) {
					int x = (i % glyphsPerLine) * glyphSize.Width;
					int y = (i / glyphsPerLine) * glyphSize.Height;
					var chr = (char)(startChar + i);
					gfx.DrawString(chr.ToString(), font, Brushes.White, x, y);
				}
			}
			
			return new MonoBitmapFont(bitmap, glyphSpacing,
			                          glyphsPerLine, startChar, endChar);
		}
		
		
		public void DrawVerts(int x, int y, string text)
		{
			var startX = x;
			foreach (var chr in text) {
				switch (chr) {
					case '\t':
						x += GlyphSpacing.Width * 4;
						break;
					case '\n':
						x  = startX;
						y += GlyphSpacing.Height;
						break;
					default:
						DrawVerts(x, y, chr);
						x += GlyphSpacing.Width;
						break;
				}
			}
		}
		public void DrawVerts(int x, int y, char chr)
		{
			if ((chr < StartChar) || (chr > EndChar))
				chr = UnknownCharacter;
			
			int index = chr - StartChar;
			if ((index < 0) || (index >= NumGlyphs))
				return;
			
			var w = GlyphSize.Width;
			var h = GlyphSize.Height;
			
			var uStep = (float)w / Bitmap.Width;
			var vStep = (float)h / Bitmap.Height;
			var u = (index % GlyphsPerLine) * uStep;
			var v = (index / GlyphsPerLine) * vStep;
			
			GL.TexCoord2(u        , v        ); GL.Vertex2(x    , y    );
			GL.TexCoord2(u + uStep, v        ); GL.Vertex2(x + w, y    );
			GL.TexCoord2(u + uStep, v + vStep); GL.Vertex2(x + w, y + h);
			GL.TexCoord2(u        , v + vStep); GL.Vertex2(x    , y + h);
		}
	}
}
