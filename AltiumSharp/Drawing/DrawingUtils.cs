﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;

namespace AltiumSharp.Drawing
{
    internal static class DrawingUtils
    {
        /// <summary>
        /// Used for configuring rendering on <paramref name="graphics"/>, according to
        /// <paramref name="fastRendering"/> to either allow high quality rendering
        /// or faster rendering.
        /// </summary>
        internal static void SetupGraphics(Graphics graphics, bool fastRendering)
        {
            if (fastRendering)
            {
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.TextRenderingHint = TextRenderingHint.SystemDefault;
                graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            }
            else
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.PixelOffsetMode = PixelOffsetMode.None;
            }
        }

        /// <summary>
        /// Calculates the font size that matches a given desired <paramref name="desiredBaseline"/> value.
        /// </summary>
        internal static float CalculateFontSizeForBaseline(Graphics g, FontFamily fontFamily, FontStyle fontStyle, float desiredBaseline)
        {
            return search(desiredBaseline); // use baseline as initial guess

            float search(float fontSize)
            {
                using (var font = new Font(fontFamily, fontSize, fontStyle))
                {
                    var baseline = CalculateFontBaseline(g, font);

                    if (Math.Abs(baseline - desiredBaseline) < 1.0f)
                    {
                        return fontSize;
                    }
                    else if (baseline > desiredBaseline)
                    {
                        return search(fontSize * 0.5f);
                    }
                    else
                    {
                        return search(fontSize * 1.5f);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the font size that matches a given desired <paramref name="desiredHeight"/> value.
        /// </summary>
        internal static float CalculateFontSizeForHeight(Graphics g, FontFamily fontFamily, FontStyle fontStyle, float desiredHeight)
        {
            return search(desiredHeight); // use height as initial guess

            float search(float fontSize)
            {
                using (var font = new Font(fontFamily, fontSize, fontStyle))
                {
                    var height = font.GetHeight(g);

                    if (Math.Abs(height - desiredHeight) < 1.0f)
                    {
                        return fontSize;
                    }
                    else if (height > desiredHeight)
                    {
                        return search(fontSize * 0.5f);
                    }
                    else
                    {
                        return search(fontSize * 1.5f);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the position from the top of the font to the font baseline.
        /// </summary>
        internal static float CalculateFontBaseline(Graphics g, Font font)
        {
            return font.GetHeight(g) / font.FontFamily.GetLineSpacing(font.Style) *
                font.FontFamily.GetCellAscent(font.Style);
        }


        /// <summary>
        /// Calculates the internal leading position from the top of the font.
        /// </summary>
        internal static float CalculateFontInternalLeading(Graphics g, Font font)
        {
            return CalculateFontBaseline(g, font) - font.Size;
        }

        /// <summary>
        /// Used when rendering rectangles to round them to the closest integer, and correct
        /// weird behavior where rectangle width and height are 1 pixel larger.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        private static Rectangle FixRectangle(in RectangleF rect) =>
            new Rectangle(
                (int)Math.Round(rect.X),
                (int)Math.Round(rect.Y),
                (int)Math.Round(rect.Width - 1),
                (int)Math.Round(rect.Height - 1));

        /// <summary>
        /// Fixes <see cref="Graphics.FillRectangle(Brush, RectangleF)"/> behavior
        /// of drawing a larger rectangle when anti-aliased.
        /// </summary>
        internal static void FillRectangle(Graphics g, Brush brush, in RectangleF rect)
        {
            g.FillRectangle(brush, FixRectangle(rect));
        }

        /// <summary>
        /// Fixes <see cref="Graphics.FillRectangle(Brush, float, float, float, float)"/> behavior
        /// of drawing a larger rectangle when anti-aliased.
        /// </summary>
        internal static void FillRectangle(Graphics g, Brush brush, float x, float y, float width, float height)
        {
            g.FillRectangle(brush, FixRectangle(new RectangleF(x, y, width, height)));
        }

        /// <summary>
        /// Fixes <see cref="Graphics.DrawRectangle(Pen, Rectangle)"/> behavior
        /// of drawing a larger rectangle when anti-aliased.
        /// </summary>
        internal static void DrawRectangle(Graphics g, Pen pen, in RectangleF rect)
        {
            // Fixes Graphics.DrawRectangle() behavior of drawing a larger rectangle
            g.DrawRectangle(pen, FixRectangle(rect));
        }

        /// <summary>
        /// Fixes <see cref="Graphics.DrawRectangles(Pen, RectangleF[])"/> behavior
        /// of drawing a larger rectangle when anti-aliased.
        /// </summary>
        internal static void DrawRectangles(Graphics g, Pen pen, in RectangleF[] rects)
        {
            foreach (var rect in rects)
            {
                DrawRectangle(g, pen, rect);
            }
        }

        /// <summary>
        /// Draws a string to the given <paramref name="g"/> using narrower margins for text drawing.
        /// </summary>
        internal static void DrawString(Graphics g, string text, Font font, Brush brush, float x, float y,
            StringAlignment horizontalAlignment, StringAlignment verticalAlignment, bool useTightAlignment)
        {
            using (var stringFormat = new StringFormat())
            {
                stringFormat.Alignment = horizontalAlignment;
                if (useTightAlignment)
                {
                    stringFormat.LineAlignment = StringAlignment.Near;
                    var fontInternalLeading = CalculateFontInternalLeading(g, font);
                    y -= fontInternalLeading;
                    switch (verticalAlignment)
                    {
                        case StringAlignment.Center:
                            y -= font.Size * 0.5f;
                            break;
                        case StringAlignment.Far:
                            y -= font.Size;
                            break;
                        case StringAlignment.Near:
                            break;
                    }
                }
                else
                {
                    stringFormat.LineAlignment = verticalAlignment;
                }
                g.DrawString(text, font, brush, x, y, stringFormat);
            }
        }

        internal static void DrawString(Graphics g, string text, Font font, Brush brush, RectangleF layoutRectangle,
            StringAlignment horizontalAlignment, StringAlignment verticalAlignment, bool clip = false, bool wrap = false)
        {
            using (var stringFormat = new StringFormat())
            {
                stringFormat.Alignment = horizontalAlignment;
                stringFormat.LineAlignment = StringAlignment.Near;
                stringFormat.FormatFlags = StringFormatFlags.NoClip; // clipping will be done manually
                if (!wrap)
                {
                    stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
                }
                stringFormat.Trimming = StringTrimming.None;
                switch (verticalAlignment)
                {
                    case StringAlignment.Center:
                        layoutRectangle.Y -= font.Size * 0.5f;
                        break;
                    case StringAlignment.Far:
                        layoutRectangle.Y -= font.Size;
                        break;
                    case StringAlignment.Near:
                        break;
                }

                var leadingSpace = font.Size * 0.15f;
                layoutRectangle.X -= leadingSpace; // remove leading space in text
                layoutRectangle.Width += leadingSpace;

                var savedClip = g.Clip;
                if (clip)
                {
                    g.SetClip(layoutRectangle);
                }
                g.DrawString(text, font, brush, layoutRectangle, stringFormat);
                g.Clip = savedClip;
            }
        }

        internal static void FillRoundedRect(Graphics g, Brush brush, in RectangleF rect, int radiusPercent)
        {
            using (var path = GetRoundedRectPath(rect, radiusPercent))
            {
                g.FillPath(brush, path);
            }
        }

        internal static GraphicsPath GetRoundedRectPath(in RectangleF rect, int radiusPercent)
        {
            var path = new GraphicsPath();
            if (rect.IsEmpty)
            {
                return path;
            }
            else if (radiusPercent <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var diameter = Math.Min(rect.Width, rect.Height) * radiusPercent * 0.01f;
            var size = new SizeF(diameter, diameter);
            var arc = new RectangleF(rect.Location, size);

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            path.Flatten(null, 0.5f);
            return path;
        }

        internal static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radiusX, float radiusY)
        {
            using (var path = GetRoundedRectPath(rect, radiusX, radiusY))
            {
                g.FillPath(brush, path);
            }
        }

        internal static void DrawRoundedRect(Graphics g, Pen pen, RectangleF rect, float radiusX, float radiusY)
        {
            using (var path = GetRoundedRectPath(rect, radiusX, radiusY))
            {
                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// Creates a rounded rectangle path.
        /// </summary>
        /// <param name="rect">Rectangle used for reference for the rounded rectangle size and positioning.</param>
        /// <param name="radiusX">Radius in the horizontal direction for drawing the corners</param>
        /// <param name="radiusY">Radius in the vertical drection for drawing the corners.</param>
        /// <returns></returns>
        internal static GraphicsPath GetRoundedRectPath(in RectangleF rect, float radiusX, float radiusY)
        {
            var path = new GraphicsPath();
            if (rect.IsEmpty)
            {
                return path;
            }

            var size = new SizeF(Math.Min(rect.Width - 1, radiusX * 2.0f), Math.Min(rect.Height - 1, radiusY * 2.0f));
            var arc = new RectangleF(rect.Location, size);

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = rect.Right - size.Width;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = rect.Bottom - size.Height;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            path.Flatten(null, 0.5f);
            return path;
        }

        /// <summary>
        /// Fills an octagon.
        /// </summary>
        /// <param name="g">Where to draw.</param>
        /// <param name="brush">Brush used for filling the octagon.</param>
        /// <param name="rect">Rectangle used for reference for the octagon size and positioning</param>
        /// <param name="cornerPercent">Percent distance for octagon corners.</param>
        internal static void FillOctagon(Graphics g, Brush brush, in RectangleF rect, int cornerPercent = 50)
        {
            using (var path = GetOctagonPath(rect, cornerPercent))
            {
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// Creates an octagon path.
        /// </summary>
        /// <param name="rect">Reference rectangle.</param>
        /// <param name="cornerPercent">Percent distance for octagon corners.</param>
        /// <returns></returns>
        private static GraphicsPath GetOctagonPath(in RectangleF rect, int cornerPercent = 50)
        {
            var path = new GraphicsPath();
            if (rect.IsEmpty)
            {
                return path;
            }
            else if (cornerPercent <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var cornerSize = Math.Min(rect.Width, rect.Height) * (cornerPercent * 0.5f * 0.01f);
            path.AddPolygon(new[]
            {
                new PointF(rect.Left + cornerSize, rect.Top),
                new PointF(rect.Right - cornerSize, rect.Top),
                new PointF(rect.Right, rect.Top + cornerSize),
                new PointF(rect.Right, rect.Bottom - cornerSize),
                new PointF(rect.Right - cornerSize, rect.Bottom),
                new PointF(rect.Left + cornerSize, rect.Bottom),
                new PointF(rect.Left, rect.Bottom - cornerSize),
                new PointF(rect.Left, rect.Top + cornerSize),
            });
            path.CloseFigure();
            return path;
        }
    }
}
