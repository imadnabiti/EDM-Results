using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using EDMResults.Core;

namespace EDMResults.UI
{
    internal enum UiIcon
    {
        None,
        Target,
        History,
        Compass,
        SerialPort,
        Ban,
        Bolt,
        Check,
        Swap,
        Trash,
        Crosshair
    }

    /// <summary>
    /// Modern SaaS Light Palette inspired by AIRES & Comfort Shift:
    /// bright, airy surfaces, soft pastels, crisp dark-slate typography, and vector line icons.
    /// </summary>
    internal static class Theme
    {
        public static readonly Color BgCanvas       = Color.FromArgb(245, 247, 250); // #F5F7FA Soft Airy Gray
        public static readonly Color BgSurface      = Color.FromArgb(255, 255, 255); // #FFFFFF Pure White Card
        public static readonly Color BgPillTrack    = Color.FromArgb(241, 245, 249); // #F1F5F9 Segmented Pill Track
        public static readonly Color BgSubtle       = Color.FromArgb(248, 250, 252); // #F8FAFC Light Field Fill
        public static readonly Color BgSubtleHover  = Color.FromArgb(241, 245, 249); // #F1F5F9

        public static readonly Color BorderSubtle   = Color.FromArgb(226, 232, 240); // #E2E8F0 Crisp 1px Border
        public static readonly Color BorderStrong   = Color.FromArgb(203, 213, 225); // #CBD5E1

        public static readonly Color TextPrimary    = Color.FromArgb(15, 23, 42);    // #0F172A Deep Slate
        public static readonly Color TextSecondary  = Color.FromArgb(71, 85, 105);   // #475569 Medium Slate
        public static readonly Color TextMuted      = Color.FromArgb(100, 116, 139); // #64748B Cool Gray

        // Light-tinted SaaS primary button & accent colors (never heavy or dark)
        public static readonly Color AccentBlue     = Color.FromArgb(37, 99, 235);   // #2563EB Modern Blue
        public static readonly Color AccentSoftBg   = Color.FromArgb(239, 246, 255); // #EFF6FF Soft Sky Surface
        public static readonly Color AccentHoverBg  = Color.FromArgb(219, 234, 254); // #DBEAFE
        public static readonly Color AccentBorder   = Color.FromArgb(147, 197, 253); // #93C5FD

        // Emerald (Valid / Connected)
        public static readonly Color OkText         = Color.FromArgb(21, 128, 61);   // #15803D
        public static readonly Color OkDot          = Color.FromArgb(34, 197, 94);   // #22C55E
        public static readonly Color OkSoftBg       = Color.FromArgb(240, 253, 244); // #F0FDF4
        public static readonly Color OkHoverBg      = Color.FromArgb(220, 252, 231); // #DCFCE7
        public static readonly Color OkBorder       = Color.FromArgb(134, 239, 172); // #86EFAC

        // Amber (Offline / Pending)
        public static readonly Color WarnText       = Color.FromArgb(180, 83, 9);    // #B45309
        public static readonly Color WarnDot        = Color.FromArgb(245, 158, 11);  // #F59E0B
        public static readonly Color WarnSoftBg     = Color.FromArgb(255, 251, 235); // #FFFBEB
        public static readonly Color WarnBorder     = Color.FromArgb(252, 211, 77);  // #FCD34D

        // Coral Rose (Foul / Warning — light-tinted surface, high legibility outdoors)
        public static readonly Color DangerText     = Color.FromArgb(190, 18, 60);   // #BE123C
        public static readonly Color DangerDot      = Color.FromArgb(244, 63, 94);   // #F43F5E
        public static readonly Color DangerSoftBg   = Color.FromArgb(255, 241, 242); // #FFF1F2
        public static readonly Color DangerHoverBg  = Color.FromArgb(255, 228, 230); // #FFE4E6
        public static readonly Color DangerBorder   = Color.FromArgb(253, 164, 175); // #FDA4AF

        public static float GetDpiScale()
        {
            using (Bitmap bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                return Math.Max(1.0f, g.DpiX / 96.0f);
            }
        }

        public static int ScalePx(int basePx)
        {
            return (int)Math.Round(basePx * GetDpiScale());
        }

        public static Color ResolveOpaqueParentColor(Control ctrl, Color fallback)
        {
            Control current = ctrl != null ? ctrl.Parent : null;
            while (current != null)
            {
                SurfaceCard card = current as SurfaceCard;
                if (card != null && card.FillColor.A == 255)
                {
                    return card.FillColor;
                }
                if (current.BackColor != Color.Transparent && current.BackColor.A == 255)
                {
                    return current.BackColor;
                }
                current = current.Parent;
            }
            return fallback;
        }

        public static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Renders crisp anti-aliased vector stroke icons for buttons and navigation pills.
        /// </summary>
        public static void DrawVectorIcon(Graphics g, UiIcon icon, Rectangle box, Color color)
        {
            if (icon == UiIcon.None) return;
            SmoothingMode prev = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = box.X + box.Width / 2f;
            float cy = box.Y + box.Height / 2f;

            using (Pen pen = new Pen(color, 1.75f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (icon)
                {
                    case UiIcon.Target:
                        g.DrawEllipse(pen, cx - 6.5f, cy - 6.5f, 13f, 13f);
                        g.DrawEllipse(pen, cx - 2.5f, cy - 2.5f, 5f, 5f);
                        break;

                    case UiIcon.History:
                        g.DrawEllipse(pen, cx - 6.5f, cy - 6.5f, 13f, 13f);
                        g.DrawLine(pen, cx, cy - 3.5f, cx, cy);
                        g.DrawLine(pen, cx, cy, cx + 3f, cy + 2f);
                        break;

                    case UiIcon.Compass:
                    case UiIcon.Crosshair:
                        g.DrawEllipse(pen, cx - 5.5f, cy - 5.5f, 11f, 11f);
                        g.DrawLine(pen, cx, cy - 8f, cx, cy - 4f);
                        g.DrawLine(pen, cx, cy + 4f, cx, cy + 8f);
                        g.DrawLine(pen, cx - 8f, cy, cx - 4f, cy);
                        g.DrawLine(pen, cx + 4f, cy, cx + 8f, cy);
                        break;

                    case UiIcon.SerialPort:
                        g.DrawRectangle(pen, cx - 5f, cy - 3f, 10f, 7f);
                        g.DrawLine(pen, cx - 2.5f, cy - 6.5f, cx - 2.5f, cy - 3f);
                        g.DrawLine(pen, cx + 2.5f, cy - 6.5f, cx + 2.5f, cy - 3f);
                        g.DrawLine(pen, cx, cy + 4f, cx, cy + 7.5f);
                        break;

                    case UiIcon.Ban:
                        g.DrawEllipse(pen, cx - 6.5f, cy - 6.5f, 13f, 13f);
                        g.DrawLine(pen, cx - 4.2f, cy - 4.2f, cx + 4.2f, cy + 4.2f);
                        break;

                    case UiIcon.Bolt:
                        PointF[] bolt = new PointF[]
                        {
                            new PointF(cx + 1f, cy - 7f),
                            new PointF(cx - 4.5f, cy + 0.5f),
                            new PointF(cx + 0.5f, cy + 0.5f),
                            new PointF(cx - 1f, cy + 7f),
                            new PointF(cx + 4.5f, cy - 0.5f),
                            new PointF(cx - 0.5f, cy - 0.5f)
                        };
                        g.DrawPolygon(pen, bolt);
                        break;

                    case UiIcon.Check:
                        g.DrawLines(pen, new PointF[]
                        {
                            new PointF(cx - 5.5f, cy + 0.5f),
                            new PointF(cx - 1.5f, cy + 4.5f),
                            new PointF(cx + 6f, cy - 4f)
                        });
                        break;

                    case UiIcon.Swap:
                        g.DrawLine(pen, cx - 6f, cy - 2.5f, cx + 5f, cy - 2.5f);
                        g.DrawLines(pen, new PointF[] { new PointF(cx + 2f, cy - 5.5f), new PointF(cx + 5.5f, cy - 2.5f), new PointF(cx + 2f, cy + 0.5f) });
                        g.DrawLine(pen, cx + 6f, cy + 2.5f, cx - 5f, cy + 2.5f);
                        g.DrawLines(pen, new PointF[] { new PointF(cx - 2f, cy - 0.5f), new PointF(cx - 5.5f, cy + 2.5f), new PointF(cx - 2f, cy + 5.5f) });
                        break;

                    case UiIcon.Trash:
                        g.DrawLine(pen, cx - 6f, cy - 4f, cx + 6f, cy - 4f);
                        g.DrawRectangle(pen, cx - 4.5f, cy - 4f, 9f, 10f);
                        g.DrawLine(pen, cx - 2f, cy - 6f, cx + 2f, cy - 6f);
                        break;
                }
            }

            g.SmoothingMode = prev;
        }
    }

    internal sealed class SurfaceCard : Panel
    {
        public Color FillColor { get; set; }
        public Color StrokeColor { get; set; }
        public int CornerRadius { get; set; }
        public float StrokeWidth { get; set; }

        public SurfaceCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgSurface;
            FillColor = Theme.BgSurface;
            StrokeColor = Theme.BorderSubtle;
            CornerRadius = 14;
            StrokeWidth = 1f;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.ResolveOpaqueParentColor(this, Theme.BgCanvas));

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.CreateRoundedRect(rect, CornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(FillColor))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(StrokeColor, StrokeWidth))
                {
                    g.DrawPath(pen, path);
                }
            }
            base.OnPaint(e);
        }
    }

    /// <summary>
    /// Modern SaaS light-mode button with vector icon, rounded pill geometry, and DPI-scaled height that never clips text.
    /// </summary>
    internal sealed class SoftButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;

        public Color NormalColor { get; set; }
        public Color HoverColor { get; set; }
        public Color BorderColor { get; set; }
        public int CornerRadius { get; set; }
        public UiIcon VectorIcon { get; set; }

        public SoftButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            NormalColor = Theme.AccentSoftBg;
            HoverColor = Theme.AccentHoverBg;
            BorderColor = Theme.AccentBorder;
            ForeColor = Theme.AccentBlue;
            CornerRadius = 10;
            VectorIcon = UiIcon.None;
            Cursor = Cursors.Hand;
            Size = new Size(Theme.ScalePx(190), Theme.ScalePx(42));
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Regular);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.ResolveOpaqueParentColor(this, Theme.BgSurface));

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = !Enabled
                ? Theme.BgSubtle
                : (_isPressed ? ControlPaint.Dark(HoverColor, 0.02f) : (_isHovered ? HoverColor : NormalColor));

            Color textCol = Enabled ? ForeColor : Theme.TextMuted;
            Color stroke = !Enabled ? Theme.BorderSubtle : BorderColor;

            int radius = Math.Min(CornerRadius, rect.Height / 2);
            using (GraphicsPath path = Theme.CreateRoundedRect(rect, radius))
            {
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, path);
                }
                if (stroke != Color.Transparent)
                {
                    using (Pen p = new Pen(stroke, 1.2f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }

            Size textSize = TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine);
            int iconSpan = (VectorIcon != UiIcon.None) ? Theme.ScalePx(22) : 0;
            int totalContentWidth = textSize.Width + iconSpan;
            int startX = Math.Max(10, (Width - totalContentWidth) / 2);

            if (VectorIcon != UiIcon.None)
            {
                Rectangle iconRect = new Rectangle(startX, (Height - 16) / 2, 16, 16);
                Theme.DrawVectorIcon(g, VectorIcon, iconRect, textCol);
                startX += iconSpan;
            }

            Rectangle textRect = new Rectangle(startX, 0, Width - startX - 4, Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textRect,
                textCol,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
        }
    }

    internal sealed class StatusChip : Control
    {
        private Color _textColor = Theme.DangerText;
        private Color _dotColor = Theme.DangerDot;
        private Color _bgColor = Theme.DangerSoftBg;
        private Color _borderColor = Theme.DangerBorder;

        public void SetTone(Color text, Color dot, Color bg, Color border)
        {
            _textColor = text;
            _dotColor = dot;
            _bgColor = bg;
            _borderColor = border;
            Invalidate();
        }

        public StatusChip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Regular);
            Size = new Size(Theme.ScalePx(150), Theme.ScalePx(30));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.ResolveOpaqueParentColor(this, Theme.BgSurface));

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.CreateRoundedRect(rect, rect.Height / 2))
            {
                using (SolidBrush fill = new SolidBrush(_bgColor))
                {
                    g.FillPath(fill, path);
                }
                using (Pen border = new Pen(_borderColor, 1f))
                {
                    g.DrawPath(border, path);
                }
            }

            int dotSize = 7;
            using (SolidBrush dotBrush = new SolidBrush(_dotColor))
            {
                g.FillEllipse(dotBrush, 12, (Height - dotSize) / 2, dotSize, dotSize);
            }

            Rectangle textRect = new Rectangle(25, 0, Width - 30, Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textRect,
                _textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
        }
    }

    /// <summary>
    /// AIRES / Comfort Shift style single-bar SaaS navigation header with DPI-aware layout so text is never clipped.
    /// </summary>
    internal sealed class TopNavigationBar : Panel
    {
        private readonly string[] _tabTitles = new string[]
        {
            "Live Result",
            "History",
            "Calibration",
            "Serial Port"
        };

        private readonly UiIcon[] _tabIcons = new UiIcon[]
        {
            UiIcon.Target,
            UiIcon.History,
            UiIcon.Compass,
            UiIcon.SerialPort
        };

        private int _selectedIndex;
        private int _hoveredIndex = -1;

        public string CalibrationText { get; set; }
        public bool CalibrationOk { get; set; }
        public string SerialText { get; set; }
        public bool SerialOk { get; set; }

        public event Action<int> TabSelected;

        public TopNavigationBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = Theme.ScalePx(78);
            BackColor = Theme.BgSurface;
            CalibrationText = "Uncalibrated";
            SerialText = "COM1 Offline";
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= _tabTitles.Length) return;
            _selectedIndex = index;
            Invalidate();
        }

        private Rectangle GetPillTrackRect()
        {
            int leftBrandWidth = Theme.ScalePx(210);
            int rightStatusWidth = Theme.ScalePx(170);
            int availableCenter = Math.Max(Theme.ScalePx(460), Width - leftBrandWidth - rightStatusWidth - Theme.ScalePx(36));
            int trackWidth = Math.Min(Theme.ScalePx(580), availableCenter);
            int trackHeight = Theme.ScalePx(44);
            int trackX = leftBrandWidth + Math.Max(0, (availableCenter - trackWidth) / 2);
            int trackY = (Height - trackHeight) / 2;
            return new Rectangle(trackX, trackY, trackWidth, trackHeight);
        }

        private Rectangle GetTabRect(int index)
        {
            Rectangle track = GetPillTrackRect();
            int pad = 4;
            int innerW = track.Width - (pad * 2);
            int tabW = innerW / _tabTitles.Length;
            return new Rectangle(track.X + pad + (index * tabW), track.Y + pad, tabW, track.Height - (pad * 2));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hit = -1;
            for (int i = 0; i < _tabTitles.Length; i++)
            {
                if (GetTabRect(i).Contains(e.Location))
                {
                    hit = i;
                    break;
                }
            }
            if (hit != _hoveredIndex)
            {
                _hoveredIndex = hit;
                Cursor = hit >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < _tabTitles.Length; i++)
                {
                    if (GetTabRect(i).Contains(e.Location))
                    {
                        _selectedIndex = i;
                        Invalidate();
                        Action<int> handler = TabSelected;
                        if (handler != null) handler(i);
                        break;
                    }
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.BgSurface);

            // 1. Left Brand Badge + Title
            int badgeSize = Theme.ScalePx(38);
            Rectangle logoBadge = new Rectangle(22, (Height - badgeSize) / 2, badgeSize, badgeSize);
            using (GraphicsPath logoPath = Theme.CreateRoundedRect(logoBadge, 10))
            using (SolidBrush logoFill = new SolidBrush(Theme.AccentSoftBg))
            using (Pen logoBorder = new Pen(Theme.AccentBorder, 1f))
            {
                g.FillPath(logoFill, logoPath);
                g.DrawPath(logoBorder, logoPath);
            }
            Theme.DrawVectorIcon(g, UiIcon.Target, logoBadge, Theme.AccentBlue);

            int brandTextX = logoBadge.Right + 10;
            using (Font brandFont = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            {
                Rectangle titleRect = new Rectangle(brandTextX, (Height / 2) - Theme.ScalePx(20), Theme.ScalePx(150), Theme.ScalePx(24));
                Rectangle subRect = new Rectangle(brandTextX, (Height / 2) + Theme.ScalePx(2), Theme.ScalePx(150), Theme.ScalePx(18));
                TextRenderer.DrawText(g, "EDM Results", brandFont, titleRect, Theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
                TextRenderer.DrawText(g, "Field Telemetry", subFont, subRect, Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            // 2. Center Segmented Pill Switcher (AIRES / Comfort Shift style)
            Rectangle trackRect = GetPillTrackRect();
            using (GraphicsPath trackPath = Theme.CreateRoundedRect(trackRect, trackRect.Height / 2))
            using (SolidBrush trackBrush = new SolidBrush(Theme.BgPillTrack))
            using (Pen trackBorder = new Pen(Theme.BorderSubtle, 1f))
            {
                g.FillPath(trackBrush, trackPath);
                g.DrawPath(trackBorder, trackPath);
            }

            using (Font tabFont = new Font("Segoe UI Semibold", 9f, FontStyle.Regular))
            {
                for (int i = 0; i < _tabTitles.Length; i++)
                {
                    Rectangle r = GetTabRect(i);
                    bool active = (i == _selectedIndex);
                    bool hovered = (i == _hoveredIndex);

                    if (active)
                    {
                        using (GraphicsPath pillPath = Theme.CreateRoundedRect(r, r.Height / 2))
                        using (SolidBrush pillFill = new SolidBrush(Theme.BgSurface))
                        using (Pen pillStroke = new Pen(Theme.BorderStrong, 1f))
                        {
                            g.FillPath(pillFill, pillPath);
                            g.DrawPath(pillStroke, pillPath);
                        }
                    }
                    else if (hovered)
                    {
                        using (GraphicsPath pillPath = Theme.CreateRoundedRect(r, r.Height / 2))
                        using (SolidBrush pillFill = new SolidBrush(Color.FromArgb(140, 255, 255, 255)))
                        {
                            g.FillPath(pillFill, pillPath);
                        }
                    }

                    Color fg = active ? Theme.TextPrimary : Theme.TextSecondary;
                    Color iconCol = active ? Theme.AccentBlue : Theme.TextMuted;

                    Size tSize = TextRenderer.MeasureText(g, _tabTitles[i], tabFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine);
                    int contentW = 20 + tSize.Width;
                    int startX = r.X + Math.Max(8, (r.Width - contentW) / 2);

                    Rectangle iconBox = new Rectangle(startX, r.Y + (r.Height - 16) / 2, 16, 16);
                    Theme.DrawVectorIcon(g, _tabIcons[i], iconBox, iconCol);

                    Rectangle textBox = new Rectangle(startX + 20, r.Y, r.Right - (startX + 20), r.Height);
                    TextRenderer.DrawText(
                        g,
                        _tabTitles[i],
                        tabFont,
                        textBox,
                        fg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
                }
            }

            // 3. Right Status Pills
            int rightEdge = Width - 22;
            int chipW = Theme.ScalePx(142);
            int chipH = Theme.ScalePx(26);

            if (Width >= Theme.ScalePx(1120))
            {
                int cy = (Height - chipH) / 2;
                DrawStatusPill(
                    g,
                    new Rectangle(rightEdge - (chipW * 2) - 8, cy, chipW, chipH),
                    CalibrationText,
                    CalibrationOk ? Theme.OkText : Theme.DangerText,
                    CalibrationOk ? Theme.OkDot : Theme.DangerDot,
                    CalibrationOk ? Theme.OkSoftBg : Theme.DangerSoftBg,
                    CalibrationOk ? Theme.OkBorder : Theme.DangerBorder);

                DrawStatusPill(
                    g,
                    new Rectangle(rightEdge - chipW, cy, chipW, chipH),
                    SerialText,
                    SerialOk ? Theme.OkText : Theme.WarnText,
                    SerialOk ? Theme.OkDot : Theme.WarnDot,
                    SerialOk ? Theme.OkSoftBg : Theme.WarnSoftBg,
                    SerialOk ? Theme.OkBorder : Theme.WarnBorder);
            }
            else
            {
                int smallH = Theme.ScalePx(24);
                int topY = (Height - (smallH * 2 + 6)) / 2;
                DrawStatusPill(
                    g,
                    new Rectangle(rightEdge - chipW, topY, chipW, smallH),
                    CalibrationText,
                    CalibrationOk ? Theme.OkText : Theme.DangerText,
                    CalibrationOk ? Theme.OkDot : Theme.DangerDot,
                    CalibrationOk ? Theme.OkSoftBg : Theme.DangerSoftBg,
                    CalibrationOk ? Theme.OkBorder : Theme.DangerBorder);

                DrawStatusPill(
                    g,
                    new Rectangle(rightEdge - chipW, topY + smallH + 6, chipW, smallH),
                    SerialText,
                    SerialOk ? Theme.OkText : Theme.WarnText,
                    SerialOk ? Theme.OkDot : Theme.WarnDot,
                    SerialOk ? Theme.OkSoftBg : Theme.WarnSoftBg,
                    SerialOk ? Theme.OkBorder : Theme.WarnBorder);
            }

            using (Pen bottomPen = new Pen(Theme.BorderSubtle, 1f))
            {
                g.DrawLine(bottomPen, 0, Height - 1, Width, Height - 1);
            }
        }

        private static void DrawStatusPill(Graphics g, Rectangle rect, string label, Color textCol, Color dotCol, Color bgCol, Color borderCol)
        {
            using (GraphicsPath path = Theme.CreateRoundedRect(rect, rect.Height / 2))
            {
                using (SolidBrush fill = new SolidBrush(bgCol))
                {
                    g.FillPath(fill, path);
                }
                using (Pen border = new Pen(borderCol, 1f))
                {
                    g.DrawPath(border, path);
                }
            }

            int dotSize = 6;
            using (SolidBrush dot = new SolidBrush(dotCol))
            {
                g.FillEllipse(dot, rect.X + 10, rect.Y + (rect.Height - dotSize) / 2, dotSize, dotSize);
            }

            Rectangle textRect = new Rectangle(rect.X + 21, rect.Y, rect.Width - 24, rect.Height);
            using (Font f = new Font("Segoe UI Semibold", 8.25f, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    label,
                    f,
                    textRect,
                    textCol,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
            }
        }
    }

    public sealed class CalibrationDialog : Form
    {
        private readonly TextBox _txtHz;
        private readonly TextBox _txtV;
        private readonly TextBox _txtDist;
        private readonly StatusChip _statusChip;
        private SurveyPoint _capturedPoint;
        private bool _hasNewCapture;

        public SurveyPoint CapturedPoint
        {
            get { return _capturedPoint; }
        }

        public bool HasNewCapture
        {
            get { return _hasNewCapture; }
        }

        public CalibrationDialog(int pointIndex, SurveyPoint existingPoint)
        {
            Text = "EDM Results — Calibration";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.ScalePx(480), Theme.ScalePx(360));
            BackColor = Theme.BgCanvas;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                Padding = new Padding(Theme.ScalePx(22)),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(34)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(40)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(54)));

            Label lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = pointIndex > 0 ? ("Capture Reference Point " + pointIndex) : "Capture Circle Center",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _statusChip = new StatusChip
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = Theme.ScalePx(30),
                Text = "Waiting for EDM reading or manual coordinate entry..."
            };
            _statusChip.SetTone(Theme.WarnText, Theme.WarnDot, Theme.WarnSoftBg, Theme.WarnBorder);

            SurfaceCard fieldsCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(Theme.ScalePx(18), Theme.ScalePx(14), Theme.ScalePx(18), Theme.ScalePx(14)),
                FillColor = Theme.BgSurface
            };

            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 3
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.4f));

            _txtHz = CreateFieldInput();
            _txtV = CreateFieldInput();
            _txtDist = CreateFieldInput();

            grid.Controls.Add(CreateFieldLabel("Horizontal Angle (Hz) [gon]"), 0, 0);
            grid.Controls.Add(_txtHz, 1, 0);
            grid.Controls.Add(CreateFieldLabel("Vertical Angle (V) [gon]"), 0, 1);
            grid.Controls.Add(_txtV, 1, 1);
            grid.Controls.Add(CreateFieldLabel("Slope Distance (SD) [mm]"), 0, 2);
            grid.Controls.Add(_txtDist, 1, 2);
            fieldsCard.Controls.Add(grid);

            if (existingPoint != null)
            {
                _capturedPoint = existingPoint;
                _txtHz.Text = existingPoint.HorizontalGon.ToString(CultureInfo.CurrentCulture);
                _txtV.Text = existingPoint.VerticalGon.ToString(CultureInfo.CurrentCulture);
                _txtDist.Text = existingPoint.SlopeDistanceMm.ToString(CultureInfo.CurrentCulture);
            }

            TableLayoutPanel buttonBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            SoftButton btnAccept = new SoftButton
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = Theme.ScalePx(42),
                Text = "Confirm Point",
                VectorIcon = UiIcon.Check,
                NormalColor = Theme.OkSoftBg,
                HoverColor = Theme.OkHoverBg,
                ForeColor = Theme.OkText,
                BorderColor = Theme.OkBorder,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnAccept.Click += OnAcceptClicked;

            SoftButton btnCancel = new SoftButton
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = Theme.ScalePx(42),
                Text = "Cancel",
                NormalColor = Theme.BgSurface,
                HoverColor = Theme.BgSubtleHover,
                ForeColor = Theme.TextSecondary,
                BorderColor = Theme.BorderStrong,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnCancel.Click += OnCancelClicked;

            buttonBar.Controls.Add(btnAccept, 0, 0);
            buttonBar.Controls.Add(btnCancel, 1, 0);

            root.Controls.Add(lblTitle, 0, 0);
            root.Controls.Add(_statusChip, 0, 1);
            root.Controls.Add(fieldsCard, 0, 2);
            root.Controls.Add(buttonBar, 0, 3);

            Controls.Add(root);
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = text,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static TextBox CreateFieldInput()
        {
            return new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Theme.BgSubtle,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Regular),
                TextAlign = HorizontalAlignment.Right
            };
        }

        public void ApplyIncomingSerialPoint(SurveyPoint point)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<SurveyPoint>(ApplyIncomingSerialPoint), point);
                return;
            }

            _capturedPoint = point;
            _hasNewCapture = true;
            _txtHz.Text = point.HorizontalGon.ToString(CultureInfo.CurrentCulture);
            _txtV.Text = point.VerticalGon.ToString(CultureInfo.CurrentCulture);
            _txtDist.Text = point.SlopeDistanceMm.ToString(CultureInfo.CurrentCulture);
            _statusChip.Text = "EDM reading acquired — click Confirm Point to save";
            _statusChip.SetTone(Theme.OkText, Theme.OkDot, Theme.OkSoftBg, Theme.OkBorder);
        }

        private void OnAcceptClicked(object sender, EventArgs e)
        {
            double hz, v, dist;
            if (TryParseField(_txtHz.Text, out hz) && TryParseField(_txtV.Text, out v) && TryParseField(_txtDist.Text, out dist) && dist > 0)
            {
                if (_capturedPoint == null ||
                    Math.Abs(_capturedPoint.HorizontalGon - hz) > 1e-6 ||
                    Math.Abs(_capturedPoint.VerticalGon - v) > 1e-6 ||
                    Math.Abs(_capturedPoint.SlopeDistanceMm - dist) > 1e-6)
                {
                    _capturedPoint = new SurveyPoint(hz, v, dist);
                    _hasNewCapture = true;
                }
            }

            if (_hasNewCapture)
            {
                DialogResult res = MessageBox.Show(
                    "Save the acquired calibration point?",
                    "EDM Results — Confirm",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);
                if (res != DialogResult.OK) return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            if (_hasNewCapture)
            {
                DialogResult res = MessageBox.Show(
                    "Discard the received measurement?",
                    "EDM Results — Confirm",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);
                if (res != DialogResult.OK) return;
            }

            DialogResult = DialogResult.Cancel;
            Close();
        }

        private static bool TryParseField(string text, out double val)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out val)) return true;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }
    }

    public sealed class MainWindow : Form
    {
        private readonly string _configPath;
        private readonly string _historyPath;
        private readonly SerialTransceiver _transceiver;

        private TopNavigationBar _topNav;

        // Views
        private Panel _viewContainer;
        private Panel _viewMeasure;
        private Panel _viewHistory;
        private Panel _viewSetup;
        private Panel _viewSerial;

        // Measure View controls
        private TableLayoutPanel _measureLayout;
        private Label _lblActiveDiscipline;
        private Label _lblActiveOffsetInfo;
        private SurfaceCard _bannerCalibrationWarning;
        private SurfaceCard _displayCard;
        private Label _lblDigitalReadout;
        private Label _lblTimestampReadout;
        private Label _lblAttemptCounter;
        private SoftButton _btnMarkFoul;
        private SoftButton _btnSimulateReading;

        // History View controls
        private ListBox _lstHistory;
        private Label _lblHistoryCount;

        // Setup View controls
        private ComboBox _cbDiscipline;
        private NumericUpDown _numOffsetMm;
        private CheckBox _chkAutoOffset;
        private StatusChip _chipPoint1State;
        private StatusChip _chipPoint2State;
        private SoftButton _btnCaptureP1;
        private SoftButton _btnCaptureP2;
        private SoftButton _btnSwapPoints;
        private Label _lblP1Coords;
        private Label _lblP2Coords;
        private Label _lblBaselineSpanValue;

        // Serial View controls
        private ComboBox _cbPort;
        private ComboBox _cbBaud;
        private ComboBox _cbDataBits;
        private ComboBox _cbParity;
        private ComboBox _cbStopBits;
        private CheckBox _chkSendScoreboard;
        private SoftButton _btnReconnectSerial;
        private StatusChip _chipSerialCardStatus;
        private Label _lblSerialSummary;
        private Label _lblSerialFooter;

        private SurveyPoint _point1;
        private SurveyPoint _point2;
        private PlanarBaseline _baseline;
        private CalibrationDialog _activeCalibrationDialog;
        private bool _suppressEvents;

        public MainWindow()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(baseDir, "Config.txt");
            _historyPath = Path.Combine(baseDir, "Misure.txt");

            _transceiver = new SerialTransceiver();
            _transceiver.PointReceived += OnTelemetryPointReceived;
            _transceiver.StatusChanged += OnSerialStatusChanged;

            BuildUserInterface();
            Shown += OnWindowShown;
            FormClosing += OnWindowClosing;
        }

        private void BuildUserInterface()
        {
            Text = "EDM Results";
            ClientSize = new Size(Theme.ScalePx(960), Theme.ScalePx(630));
            MinimumSize = new Size(Theme.ScalePx(860), Theme.ScalePx(560));
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgCanvas;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EDMRESULTS.ico");
                if (File.Exists(iconPath))
                {
                    try { Icon = new Icon(iconPath); } catch { }
                }
            }

            // 1. Unified Single-Row SaaS Header & Pill Navigation Bar
            _topNav = new TopNavigationBar();
            _topNav.TabSelected += OnTopNavTabSelected;

            // 2. Subtle Bottom Status Bar
            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = Theme.ScalePx(34),
                BackColor = Theme.BgSurface
            };
            footer.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderSubtle, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
                }
            };
            _lblSerialFooter = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "EDM Results ready",
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular)
            };
            footer.Controls.Add(_lblSerialFooter);

            // 3. Main View Container
            _viewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                Padding = new Padding(Theme.ScalePx(24), Theme.ScalePx(18), Theme.ScalePx(24), Theme.ScalePx(18))
            };

            _viewMeasure = BuildMeasureView();
            _viewHistory = BuildHistoryView();
            _viewSetup   = BuildSetupView();
            _viewSerial  = BuildSerialView();

            _viewContainer.Controls.AddRange(new Control[] { _viewMeasure, _viewHistory, _viewSetup, _viewSerial });

            Controls.Add(_viewContainer);
            Controls.Add(_topNav);
            Controls.Add(footer);

            SwitchToTabIndex(0);
        }

        private void OnTopNavTabSelected(int index)
        {
            SwitchToTabIndex(index);
        }

        private void SwitchToTabIndex(int index)
        {
            _topNav.SelectTab(index);
            _viewMeasure.Visible = (index == 0);
            _viewHistory.Visible = (index == 1);
            _viewSetup.Visible   = (index == 2);
            _viewSerial.Visible  = (index == 3);

            if (index == 2 || index == 3)
            {
                _transceiver.Close();
            }
            else if (IsCalibrationComplete && !_transceiver.IsConnected)
            {
                ConnectSerialPort(false);
            }
        }

        private Panel BuildMeasureView()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCanvas };

            _measureLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                ColumnCount = 1,
                RowCount = 4
            };
            // DPI-scaled row heights + Percent inner rows so descenders (p, g, y) are NEVER clipped!
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(92)));  // Discipline card
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(52)));  // Warning banner
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));                // Main readout card
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(60)));  // Action bar

            // Row 0: Discipline & Offset Summary Card
            SurfaceCard infoCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(Theme.ScalePx(22), Theme.ScalePx(12), Theme.ScalePx(22), Theme.ScalePx(12)),
                Margin = new Padding(0, 0, 0, Theme.ScalePx(10))
            };

            TableLayoutPanel infoGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 2
            };
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            // Percent rows (42% / 58%) instead of fixed 18px so text is never clipped at any DPI!
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));

            Label lblSpecTag = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Active Discipline",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblParamTag = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Geometry Setup",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblActiveDiscipline = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Javelin Throw",
                Font = new Font("Segoe UI Semibold", 14.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblActiveOffsetInfo = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Offset: 8000 mm  ·  Circle Center",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Regular),
                ForeColor = Theme.AccentBlue,
                TextAlign = ContentAlignment.MiddleRight
            };

            infoGrid.Controls.Add(lblSpecTag, 0, 0);
            infoGrid.Controls.Add(lblParamTag, 1, 0);
            infoGrid.Controls.Add(_lblActiveDiscipline, 0, 1);
            infoGrid.Controls.Add(_lblActiveOffsetInfo, 1, 1);
            infoCard.Controls.Add(infoGrid);

            // Row 1: Light Pastel Warning Banner when uncalibrated
            _bannerCalibrationWarning = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.WarnSoftBg,
                StrokeColor = Theme.WarnBorder,
                CornerRadius = 10,
                Padding = new Padding(Theme.ScalePx(16), 4, Theme.ScalePx(16), 4),
                Margin = new Padding(0, 0, 0, Theme.ScalePx(10))
            };
            Label lblWarnText = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.WarnSoftBg,
                Text = "Calibration required — open the Calibration tab to capture reference points before measuring.",
                Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Regular),
                ForeColor = Theme.WarnText,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _bannerCalibrationWarning.Controls.Add(lblWarnText);

            // Row 2: Daylight-Readable Digital Readout Card
            _displayCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                StrokeColor = Theme.BorderSubtle,
                StrokeWidth = 1f,
                CornerRadius = 14,
                Padding = new Padding(Theme.ScalePx(24), Theme.ScalePx(16), Theme.ScalePx(24), Theme.ScalePx(16)),
                Margin = new Padding(0, 0, 0, Theme.ScalePx(12))
            };

            TableLayoutPanel readoutGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 3,
                RowCount = 2
            };
            readoutGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            readoutGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            readoutGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            readoutGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(32)));
            readoutGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label lblReadoutCaption = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Latest Official EDM Result",
                Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblTimestampReadout = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Time: --:--",
                Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblAttemptCounter = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Attempts: 0",
                Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Regular),
                ForeColor = Theme.AccentBlue,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblDigitalReadout = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "—",
                Font = new Font("Segoe UI Semibold", 64f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _displayCard.Resize += OnDisplayCardResize;

            readoutGrid.Controls.Add(lblReadoutCaption, 0, 0);
            readoutGrid.Controls.Add(_lblTimestampReadout, 1, 0);
            readoutGrid.Controls.Add(_lblAttemptCounter, 2, 0);
            readoutGrid.Controls.Add(_lblDigitalReadout, 0, 1);
            readoutGrid.SetColumnSpan(_lblDigitalReadout, 3);

            _displayCard.Controls.Add(readoutGrid);

            // Row 3: Light-Tinted Action Buttons (Fixed proportional height)
            TableLayoutPanel actionGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));

            _btnMarkFoul = new SoftButton
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = Theme.ScalePx(46),
                Text = "Mark Foul Attempt",
                VectorIcon = UiIcon.Ban,
                NormalColor = Theme.DangerSoftBg,
                HoverColor = Theme.DangerHoverBg,
                ForeColor = Theme.DangerText,
                BorderColor = Theme.DangerBorder,
                CornerRadius = 11,
                Font = new Font("Segoe UI Semibold", 10.25f, FontStyle.Regular),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnMarkFoul.Click += (s, e) => RecordMeasurement(double.NaN, true);

            _btnSimulateReading = new SoftButton
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = Theme.ScalePx(46),
                Text = "Simulate EDM Read",
                VectorIcon = UiIcon.Bolt,
                NormalColor = Theme.BgSurface,
                HoverColor = Theme.AccentSoftBg,
                ForeColor = Theme.AccentBlue,
                BorderColor = Theme.AccentBorder,
                CornerRadius = 11,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Regular),
                Margin = new Padding(8, 0, 0, 0)
            };
            _btnSimulateReading.Click += OnSimulateReadingClicked;

            actionGrid.Controls.Add(_btnMarkFoul, 0, 0);
            actionGrid.Controls.Add(_btnSimulateReading, 1, 0);

            _measureLayout.Controls.Add(infoCard, 0, 0);
            _measureLayout.Controls.Add(_bannerCalibrationWarning, 0, 1);
            _measureLayout.Controls.Add(_displayCard, 0, 2);
            _measureLayout.Controls.Add(actionGrid, 0, 3);

            panel.Controls.Add(_measureLayout);
            return panel;
        }

        private void OnDisplayCardResize(object sender, EventArgs e)
        {
            if (_displayCard == null || _lblDigitalReadout == null) return;
            int availableHeight = Math.Max(60, _displayCard.ClientSize.Height - Theme.ScalePx(58));
            float targetPt = Math.Max(38f, Math.Min(100f, (availableHeight * 0.40f) / Theme.GetDpiScale()));
            if (Math.Abs(_lblDigitalReadout.Font.Size - targetPt) > 2f)
            {
                _lblDigitalReadout.Font = new Font("Segoe UI Semibold", targetPt, FontStyle.Bold);
            }
        }

        private Panel BuildHistoryView()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCanvas, Visible = false };

            SurfaceCard listCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(Theme.ScalePx(24), Theme.ScalePx(20), Theme.ScalePx(24), Theme.ScalePx(20))
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(34)));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(50)));

            Label lblHeader = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Session Measurement History",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblHistoryCount = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "0 measurements recorded",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular),
                ForeColor = Theme.AccentBlue,
                TextAlign = ContentAlignment.MiddleRight
            };

            SurfaceCard listFrame = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSubtle,
                StrokeColor = Theme.BorderSubtle,
                CornerRadius = 10,
                Padding = new Padding(8),
                Margin = new Padding(0, 8, 0, 12)
            };

            _lstHistory = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSubtle,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = Theme.ScalePx(34),
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Regular)
            };
            _lstHistory.DrawItem += OnHistoryDrawItem;
            listFrame.Controls.Add(_lstHistory);

            SoftButton btnClear = new SoftButton
            {
                Text = "Clear History",
                VectorIcon = UiIcon.Trash,
                Size = new Size(Theme.ScalePx(180), Theme.ScalePx(40)),
                Anchor = AnchorStyles.Left,
                NormalColor = Theme.DangerSoftBg,
                HoverColor = Theme.DangerHoverBg,
                ForeColor = Theme.DangerText,
                BorderColor = Theme.DangerBorder
            };
            btnClear.Click += (s, e) =>
            {
                if (_lstHistory.Items.Count == 0) return;
                if (MessageBox.Show("Clear all recorded EDM measurements for this session?", "EDM Results", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _lstHistory.Items.Clear();
                    UpdateHistoryCounter();
                    try { if (File.Exists(_historyPath)) File.Delete(_historyPath); } catch { }
                }
            };

            layout.Controls.Add(lblHeader, 0, 0);
            layout.Controls.Add(_lblHistoryCount, 1, 0);
            layout.Controls.Add(listFrame, 0, 1);
            layout.SetColumnSpan(listFrame, 2);
            layout.Controls.Add(btnClear, 0, 2);

            listCard.Controls.Add(layout);
            panel.Controls.Add(listCard);
            return panel;
        }

        private void OnHistoryDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _lstHistory.Items.Count) return;
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color rowBg = selected
                ? Theme.AccentSoftBg
                : (e.Index % 2 == 0 ? Theme.BgSurface : Theme.BgSubtle);

            using (SolidBrush b = new SolidBrush(rowBg))
            {
                g.FillRectangle(b, e.Bounds);
            }

            string itemText = _lstHistory.Items[e.Index].ToString();
            bool isFoul = itemText.IndexOf("FOUL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          itemText.IndexOf("NULLO", StringComparison.OrdinalIgnoreCase) >= 0;

            int attemptNum = _lstHistory.Items.Count - e.Index;
            int numColW = Theme.ScalePx(96);
            Rectangle numRect = new Rectangle(e.Bounds.X + 14, e.Bounds.Y, numColW, e.Bounds.Height);
            Rectangle valRect = new Rectangle(e.Bounds.X + 14 + numColW, e.Bounds.Y, e.Bounds.Width - numColW - 24, e.Bounds.Height);

            using (Font numFont = new Font("Segoe UI Semibold", 9f, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    "Attempt #" + attemptNum,
                    numFont,
                    numRect,
                    Theme.TextMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
            }

            TextRenderer.DrawText(
                g,
                itemText,
                e.Font,
                valRect,
                isFoul ? Theme.DangerText : Theme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
        }

        private Panel BuildSetupView()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCanvas, Visible = false };

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                ColumnCount = 2,
                RowCount = 3
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(120)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(68)));

            // Row 0: Discipline & Offset Card (spans 2 columns)
            SurfaceCard specCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(Theme.ScalePx(22), Theme.ScalePx(12), Theme.ScalePx(22), Theme.ScalePx(12)),
                Margin = new Padding(0, 0, 0, Theme.ScalePx(12))
            };

            TableLayoutPanel specGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 3,
                RowCount = 2
            };
            specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.ScalePx(165)));
            specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.ScalePx(225)));
            specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            specGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            specGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            Label lblSpec = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Event Discipline",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cbDiscipline = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgSubtle,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Regular),
                Margin = new Padding(0, 4, 14, 4)
            };
            foreach (DisciplineInfo d in DisciplineInfo.All)
            {
                _cbDiscipline.Items.Add(d);
            }
            _cbDiscipline.SelectedIndexChanged += OnDisciplineSelectionChanged;

            Label lblOffset = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Radius Offset [mm]",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _numOffsetMm = new NumericUpDown
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Minimum = -50000,
                Maximum = 50000,
                Increment = 10,
                BackColor = Theme.BgSubtle,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Regular),
                Margin = new Padding(0, 6, 14, 4)
            };
            _numOffsetMm.ValueChanged += (s, e) => UpdateMeasureHeaderSummary();

            _chkAutoOffset = new CheckBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Use official World Athletics radius automatically",
                Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                Checked = true
            };
            _chkAutoOffset.CheckedChanged += OnAutoOffsetChanged;

            specGrid.Controls.Add(lblSpec, 0, 0);
            specGrid.Controls.Add(_cbDiscipline, 1, 0);
            specGrid.Controls.Add(lblOffset, 0, 1);
            specGrid.Controls.Add(_numOffsetMm, 1, 1);
            specGrid.Controls.Add(_chkAutoOffset, 2, 1);
            specCard.Controls.Add(specGrid);

            // Row 1 Col 0: Point 1 Card
            SurfaceCard p1Card = BuildPointCard(
                "Reference Point 1",
                "Pending",
                "Capture Point 1",
                out _chipPoint1State,
                out _lblP1Coords,
                out _btnCaptureP1);
            p1Card.Margin = new Padding(0, 0, 6, Theme.ScalePx(12));
            _btnCaptureP1.Click += OnCapturePoint1Clicked;

            // Row 1 Col 1: Point 2 Card
            SurfaceCard p2Card = BuildPointCard(
                "Reference Point 2",
                "Not Required",
                "Capture Point 2",
                out _chipPoint2State,
                out _lblP2Coords,
                out _btnCaptureP2);
            p2Card.Margin = new Padding(6, 0, 0, Theme.ScalePx(12));
            _btnCaptureP2.Click += OnCapturePoint2Clicked;

            // Row 2: Baseline Footer Card
            SurfaceCard baselineCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(Theme.ScalePx(22), Theme.ScalePx(10), Theme.ScalePx(22), Theme.ScalePx(10))
            };

            TableLayoutPanel baseGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 1
            };
            baseGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            baseGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            _btnSwapPoints = new SoftButton
            {
                Anchor = AnchorStyles.Left,
                Size = new Size(Theme.ScalePx(350), Theme.ScalePx(40)),
                Text = "Swap Point 1 & Point 2 (invert baseline sign)",
                VectorIcon = UiIcon.Swap,
                NormalColor = Theme.BgSubtle,
                HoverColor = Theme.BgSubtleHover,
                ForeColor = Theme.TextPrimary,
                BorderColor = Theme.BorderStrong
            };
            _btnSwapPoints.Click += OnSwapPointsClicked;

            _lblBaselineSpanValue = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Baseline Span P1–P2:  N/A",
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight
            };

            baseGrid.Controls.Add(_btnSwapPoints, 0, 0);
            baseGrid.Controls.Add(_lblBaselineSpanValue, 1, 0);
            baselineCard.Controls.Add(baseGrid);

            root.Controls.Add(specCard, 0, 0);
            root.SetColumnSpan(specCard, 2);
            root.Controls.Add(p1Card, 0, 1);
            root.Controls.Add(p2Card, 1, 1);
            root.Controls.Add(baselineCard, 0, 2);
            root.SetColumnSpan(baselineCard, 2);

            panel.Controls.Add(root);
            return panel;
        }

        private static SurfaceCard BuildPointCard(
            string title,
            string initialChip,
            string buttonCaption,
            out StatusChip chip,
            out Label coordsLabel,
            out SoftButton captureButton)
        {
            SurfaceCard card = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(Theme.ScalePx(20), Theme.ScalePx(14), Theme.ScalePx(20), Theme.ScalePx(14))
            };

            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 3
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(38)));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(48)));

            Label lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = title,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            chip = new StatusChip
            {
                Anchor = AnchorStyles.Right,
                Size = new Size(Theme.ScalePx(142), Theme.ScalePx(28)),
                Text = initialChip
            };

            coordsLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Horizontal (Hz): —\r\nVertical (V):    —\r\nDistance (SD):   —",
                Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            captureButton = new SoftButton
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = Theme.ScalePx(40),
                Text = buttonCaption,
                VectorIcon = UiIcon.Crosshair,
                NormalColor = Theme.AccentSoftBg,
                HoverColor = Theme.AccentHoverBg,
                ForeColor = Theme.AccentBlue,
                BorderColor = Theme.AccentBorder
            };

            grid.Controls.Add(lblTitle, 0, 0);
            grid.Controls.Add(chip, 1, 0);
            grid.Controls.Add(coordsLabel, 0, 1);
            grid.SetColumnSpan(coordsLabel, 2);
            grid.Controls.Add(captureButton, 0, 2);
            grid.SetColumnSpan(captureButton, 2);
            card.Controls.Add(grid);
            return card;
        }

        private Panel BuildSerialView()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCanvas, Visible = false };

            TableLayoutPanel split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            // Left Card: RS-232 Configuration Form
            SurfaceCard comCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(Theme.ScalePx(24), Theme.ScalePx(20), Theme.ScalePx(24), Theme.ScalePx(20)),
                Margin = new Padding(0, 0, 8, 0)
            };

            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 9
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.ScalePx(165)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(38))); // Title
            for (int i = 0; i < 5; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(42))); // 5 combos
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(42))); // Checkbox
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(54))); // Fixed-height button row
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));               // Elastic bottom spacer

            Label lblHeader = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "RS-232 Serial Communication Settings",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cbPort = CreateSerialCombo();
            for (int i = 1; i <= 16; i++) _cbPort.Items.Add("COM" + i);

            _cbBaud = CreateSerialCombo();
            _cbBaud.Items.AddRange(new object[] { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" });

            _cbDataBits = CreateSerialCombo();
            _cbDataBits.Items.AddRange(new object[] { "7", "8" });

            _cbParity = CreateSerialCombo();
            _cbParity.Items.AddRange(new object[] { "None", "Even", "Odd" });

            _cbStopBits = CreateSerialCombo();
            _cbStopBits.Items.AddRange(new object[] { "1", "2" });

            _chkSendScoreboard = new CheckBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Forward EDM result to PC / Scoreboard (STX/ETX frame)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                Checked = true
            };

            _btnReconnectSerial = new SoftButton
            {
                Text = "Apply & Connect COM Port",
                VectorIcon = UiIcon.Check,
                Size = new Size(Theme.ScalePx(270), Theme.ScalePx(42)),
                Anchor = AnchorStyles.Left,
                NormalColor = Theme.AccentSoftBg,
                HoverColor = Theme.AccentHoverBg,
                ForeColor = Theme.AccentBlue,
                BorderColor = Theme.AccentBorder
            };
            _btnReconnectSerial.Click += (s, e) => ConnectSerialPort(true);

            grid.Controls.Add(lblHeader, 0, 0);
            grid.SetColumnSpan(lblHeader, 2);

            grid.Controls.Add(CreateSerialLabel("Serial Port"), 0, 1);
            grid.Controls.Add(_cbPort, 1, 1);

            grid.Controls.Add(CreateSerialLabel("Baud Rate [bps]"), 0, 2);
            grid.Controls.Add(_cbBaud, 1, 2);

            grid.Controls.Add(CreateSerialLabel("Data Bits"), 0, 3);
            grid.Controls.Add(_cbDataBits, 1, 3);

            grid.Controls.Add(CreateSerialLabel("Parity"), 0, 4);
            grid.Controls.Add(_cbParity, 1, 4);

            grid.Controls.Add(CreateSerialLabel("Stop Bits"), 0, 5);
            grid.Controls.Add(_cbStopBits, 1, 5);

            grid.Controls.Add(_chkSendScoreboard, 0, 6);
            grid.SetColumnSpan(_chkSendScoreboard, 2);

            grid.Controls.Add(_btnReconnectSerial, 0, 7);
            grid.SetColumnSpan(_btnReconnectSerial, 2);

            comCard.Controls.Add(grid);

            // Right Card: Live Telemetry Status & Auto-Detected Protocols
            SurfaceCard infoCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(Theme.ScalePx(24), Theme.ScalePx(20), Theme.ScalePx(24), Theme.ScalePx(20)),
                Margin = new Padding(8, 0, 0, 0)
            };

            TableLayoutPanel rightGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 1,
                RowCount = 5
            };
            rightGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(38)));
            rightGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(42)));
            rightGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(96)));
            rightGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.ScalePx(150)));
            rightGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label lblRightTitle = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "EDM Diagnostics & Protocols",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _chipSerialCardStatus = new StatusChip
            {
                Anchor = AnchorStyles.Left,
                Size = new Size(Theme.ScalePx(170), Theme.ScalePx(30)),
                Text = "COM1 Offline"
            };
            _chipSerialCardStatus.SetTone(Theme.WarnText, Theme.WarnDot, Theme.WarnSoftBg, Theme.WarnBorder);

            _lblSerialSummary = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Active Configuration:\r\nCOM1  ·  19200 bps  ·  8N1\r\nScoreboard Output (STX/ETX): Enabled",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            SurfaceCard protoBox = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSubtle,
                StrokeColor = Theme.BorderSubtle,
                CornerRadius = 10,
                Padding = new Padding(Theme.ScalePx(14), Theme.ScalePx(12), Theme.ScalePx(14), Theme.ScalePx(12))
            };
            Label lblProtos = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSubtle,
                Text = "Auto-detected EDM Total Station Protocols:\r\n\r\n" +
                       "• Nikon Total Station (HA / VA# / SD)\r\n" +
                       "• Stonex / Leica GSI (*21.. 22.. 31..)\r\n" +
                       "• Sokkia SET (23-digit numeric frame)",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary
            };
            protoBox.Controls.Add(lblProtos);

            rightGrid.Controls.Add(lblRightTitle, 0, 0);
            rightGrid.Controls.Add(_chipSerialCardStatus, 0, 1);
            rightGrid.Controls.Add(_lblSerialSummary, 0, 2);
            rightGrid.Controls.Add(protoBox, 0, 3);

            infoCard.Controls.Add(rightGrid);

            split.Controls.Add(comCard, 0, 0);
            split.Controls.Add(infoCard, 1, 0);

            panel.Controls.Add(split);
            return panel;
        }

        private static Label CreateSerialLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static ComboBox CreateSerialCombo()
        {
            return new ComboBox
            {
                Width = Theme.ScalePx(210),
                Anchor = AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgSubtle,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Regular)
            };
        }

        private void OnWindowShown(object sender, EventArgs e)
        {
            LoadProfileAndState();
        }

        private void LoadProfileAndState()
        {
            _suppressEvents = true;
            try
            {
                AppProfile profile = SettingsRepository.LoadConfig(_configPath);

                _cbDiscipline.SelectedIndex = profile.DisciplineIndex;
                _chkAutoOffset.Checked = profile.OffsetLocked;
                _numOffsetMm.Enabled = !profile.OffsetLocked;
                _numOffsetMm.Value = profile.OffsetMm;

                int portIdx = _cbPort.Items.IndexOf(profile.Serial.PortName);
                _cbPort.SelectedIndex = portIdx >= 0 ? portIdx : 0;

                int baudIdx = _cbBaud.Items.IndexOf(profile.Serial.BaudRate.ToString());
                _cbBaud.SelectedIndex = baudIdx >= 0 ? baudIdx : 4;

                _cbDataBits.SelectedIndex = (profile.Serial.DataBits == 7) ? 0 : 1;

                if (profile.Serial.Parity == Parity.Even) _cbParity.SelectedIndex = 1;
                else if (profile.Serial.Parity == Parity.Odd) _cbParity.SelectedIndex = 2;
                else _cbParity.SelectedIndex = 0;

                _cbStopBits.SelectedIndex = (profile.Serial.StopBits == StopBits.Two) ? 1 : 0;
                _chkSendScoreboard.Checked = profile.Serial.ForwardToScoreboard;

                ApplyDisciplineMode(false);
                UpdateSerialSummaryCard();

                if (profile.Point1 != null || profile.Point2 != null)
                {
                    DialogResult resume = MessageBox.Show(
                        "Resume the previous competition using saved calibration points?",
                        "EDM Results",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (resume == DialogResult.Yes)
                    {
                        _point1 = profile.Point1;
                        _point2 = profile.Point2;

                        if (RequiresTwoPoints && _point1 != null && _point2 != null)
                        {
                            PlanarBaseline bl;
                            string err;
                            if (PlanarBaseline.TryValidateSpan(_point1, _point2, out bl, out err))
                            {
                                _baseline = bl;
                            }
                        }
                    }
                }

                UpdateCalibrationVisuals();

                if (IsCalibrationComplete)
                {
                    LoadHistoryFromFile();
                    ConnectSerialPort(false);
                }
                else
                {
                    try { if (File.Exists(_historyPath)) File.Delete(_historyPath); } catch { }
                }
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private bool RequiresTwoPoints
        {
            get
            {
                DisciplineInfo d = _cbDiscipline.SelectedItem as DisciplineInfo;
                return d != null && d.Mode == CalibrationKind.TwoPointBaseline;
            }
        }

        private bool IsCalibrationComplete
        {
            get
            {
                if (_point1 == null) return false;
                if (RequiresTwoPoints) return _point2 != null && _baseline != null;
                return true;
            }
        }

        private void ApplyDisciplineMode(bool resetOffsetToDefault)
        {
            DisciplineInfo d = _cbDiscipline.SelectedItem as DisciplineInfo ?? DisciplineInfo.All[0];

            _point1 = null;
            _point2 = null;
            _baseline = null;

            bool twoPts = (d.Mode == CalibrationKind.TwoPointBaseline);
            _btnCaptureP2.Enabled = twoPts;
            _btnSwapPoints.Enabled = twoPts;

            if (resetOffsetToDefault)
            {
                _chkAutoOffset.Checked = true;
                _numOffsetMm.Enabled = false;
                _numOffsetMm.Value = d.DefaultOffsetMm;
            }

            UpdateMeasureHeaderSummary();
            UpdateCalibrationVisuals();
        }

        private void UpdateMeasureHeaderSummary()
        {
            DisciplineInfo d = _cbDiscipline.SelectedItem as DisciplineInfo ?? DisciplineInfo.All[0];
            _lblActiveDiscipline.Text = d.Name;
            string modeLabel = (d.Mode == CalibrationKind.TwoPointBaseline) ? "2-Point Baseline" : "Circle Center";
            _lblActiveOffsetInfo.Text = string.Format("Offset: {0} mm  ·  {1}", _numOffsetMm.Value, modeLabel);
        }

        private void UpdateCalibrationVisuals()
        {
            if (_point1 != null)
            {
                _chipPoint1State.Text = "Acquired";
                _chipPoint1State.SetTone(Theme.OkText, Theme.OkDot, Theme.OkSoftBg, Theme.OkBorder);
            }
            else
            {
                _chipPoint1State.Text = "Pending";
                _chipPoint1State.SetTone(Theme.DangerText, Theme.DangerDot, Theme.DangerSoftBg, Theme.DangerBorder);
            }
            _lblP1Coords.Text = FormatPointSummary(_point1);

            if (!RequiresTwoPoints)
            {
                _chipPoint2State.Text = "Not Required";
                _chipPoint2State.SetTone(Theme.TextMuted, Theme.BorderStrong, Theme.BgSubtle, Theme.BorderSubtle);
                _lblP2Coords.Text = "Used only for Long Jump\r\nand Triple Jump";
                _lblBaselineSpanValue.Text = "Baseline Span P1–P2:  N/A";
            }
            else
            {
                if (_point2 != null)
                {
                    _chipPoint2State.Text = "Acquired";
                    _chipPoint2State.SetTone(Theme.OkText, Theme.OkDot, Theme.OkSoftBg, Theme.OkBorder);
                }
                else
                {
                    _chipPoint2State.Text = "Pending";
                    _chipPoint2State.SetTone(Theme.DangerText, Theme.DangerDot, Theme.DangerSoftBg, Theme.DangerBorder);
                }
                _lblP2Coords.Text = FormatPointSummary(_point2);
                _lblBaselineSpanValue.Text = (_baseline != null)
                    ? string.Format("Baseline Span P1–P2:  {0:0.0} mm", _baseline.SpanMm)
                    : "Baseline Span P1–P2:  —";
            }

            bool ready = IsCalibrationComplete;
            _bannerCalibrationWarning.Visible = !ready;
            _measureLayout.RowStyles[1].Height = ready ? 0f : Theme.ScalePx(52);
            _btnMarkFoul.Enabled = ready;

            _topNav.CalibrationOk = ready;
            _topNav.CalibrationText = ready ? "EDM Calibrated" : "Uncalibrated";
            _topNav.Invalidate();
        }

        private static string FormatPointSummary(SurveyPoint pt)
        {
            if (pt == null) return "Horizontal (Hz): —\r\nVertical (V):    —\r\nDistance (SD):   —";
            return string.Format(
                "Horizontal (Hz): {0} gon\r\nVertical (V):    {1} gon\r\nDistance (SD):   {2} mm",
                pt.HorizontalGon,
                pt.VerticalGon,
                pt.SlopeDistanceMm);
        }

        private void OnDisciplineSelectionChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            ApplyDisciplineMode(true);
            _lstHistory.Items.Clear();
            UpdateHistoryCounter();
            try { if (File.Exists(_historyPath)) File.Delete(_historyPath); } catch { }
        }

        private void OnAutoOffsetChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            _numOffsetMm.Enabled = !_chkAutoOffset.Checked;
            if (_chkAutoOffset.Checked)
            {
                DisciplineInfo d = _cbDiscipline.SelectedItem as DisciplineInfo;
                if (d != null) _numOffsetMm.Value = d.DefaultOffsetMm;
            }
            UpdateMeasureHeaderSummary();
        }

        private void OnCapturePoint1Clicked(object sender, EventArgs e)
        {
            ConnectSerialPort(false);
            int labelIndex = RequiresTwoPoints ? 1 : 0;
            using (CalibrationDialog dlg = new CalibrationDialog(labelIndex, _point1))
            {
                _activeCalibrationDialog = dlg;
                DialogResult res = dlg.ShowDialog(this);
                _activeCalibrationDialog = null;

                if (res == DialogResult.OK && dlg.HasNewCapture && dlg.CapturedPoint != null)
                {
                    SurveyPoint candidate = dlg.CapturedPoint;
                    if (RequiresTwoPoints && _point2 != null)
                    {
                        PlanarBaseline bl;
                        string err;
                        if (!PlanarBaseline.TryValidateSpan(candidate, _point2, out bl, out err))
                        {
                            MessageBox.Show(err, "EDM Results — Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        _baseline = bl;
                    }

                    _point1 = candidate;
                    UpdateCalibrationVisuals();
                    if (IsCalibrationComplete)
                    {
                        SwitchToTabIndex(0);
                    }
                }
            }
        }

        private void OnCapturePoint2Clicked(object sender, EventArgs e)
        {
            ConnectSerialPort(false);
            using (CalibrationDialog dlg = new CalibrationDialog(2, _point2))
            {
                _activeCalibrationDialog = dlg;
                DialogResult res = dlg.ShowDialog(this);
                _activeCalibrationDialog = null;

                if (res == DialogResult.OK && dlg.HasNewCapture && dlg.CapturedPoint != null)
                {
                    SurveyPoint candidate = dlg.CapturedPoint;
                    if (_point1 != null)
                    {
                        PlanarBaseline bl;
                        string err;
                        if (!PlanarBaseline.TryValidateSpan(_point1, candidate, out bl, out err))
                        {
                            MessageBox.Show(err, "EDM Results — Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        _baseline = bl;
                    }

                    _point2 = candidate;
                    UpdateCalibrationVisuals();
                    if (IsCalibrationComplete)
                    {
                        SwitchToTabIndex(0);
                    }
                }
            }
        }

        private void OnSwapPointsClicked(object sender, EventArgs e)
        {
            if (!RequiresTwoPoints) return;
            SurveyPoint temp = _point1;
            _point1 = _point2;
            _point2 = temp;

            if (_point1 != null && _point2 != null)
            {
                PlanarBaseline bl;
                string err;
                if (PlanarBaseline.TryValidateSpan(_point1, _point2, out bl, out err))
                {
                    _baseline = bl;
                }
            }
            UpdateCalibrationVisuals();
        }

        private void OnTelemetryPointReceived(SurveyPoint point, string rawLine)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<SurveyPoint, string>(OnTelemetryPointReceived), point, rawLine);
                return;
            }

            if (_activeCalibrationDialog != null)
            {
                _activeCalibrationDialog.ApplyIncomingSerialPoint(point);
                return;
            }

            if (!IsCalibrationComplete)
            {
                return;
            }

            double rawDistMm = RequiresTwoPoints
                ? _baseline.SignedDistanceTo(point)
                : _point1.PlanarDistanceTo(point);

            double netDistanceMm = rawDistMm - (double)_numOffsetMm.Value;
            RecordMeasurement(netDistanceMm, false);
        }

        private void RecordMeasurement(double distanceMm, bool isFoul)
        {
            DateTime now = DateTime.Now;
            string timeTag = now.ToString("HH:mm");
            string distTag = EdmResultFormatter.FormatOfficialDistance(distanceMm, isFoul);

            _lblTimestampReadout.Text = "Time: " + timeTag;
            _lblDigitalReadout.Text = distTag.Trim();
            _lblDigitalReadout.ForeColor = isFoul ? Theme.DangerText : Theme.OkText;
            _displayCard.StrokeColor = isFoul ? Theme.DangerBorder : Theme.OkBorder;
            _displayCard.Invalidate();

            string historyRow = timeTag + "    " + distTag;
            _lstHistory.Items.Insert(0, historyRow);
            UpdateHistoryCounter();

            if (!isFoul && distanceMm >= 0 && _chkSendScoreboard.Checked)
            {
                string packet = EdmResultFormatter.BuildScoreboardPacket(distTag);
                _transceiver.SendRaw(packet);
            }

            try
            {
                File.AppendAllText(_historyPath, (timeTag + " " + distTag) + Environment.NewLine);
            }
            catch
            {
                MessageBox.Show("Unable to save EDM result to file.", "EDM Results — Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateHistoryCounter()
        {
            int count = _lstHistory.Items.Count;
            _lblAttemptCounter.Text = "Attempts: " + count;
            _lblHistoryCount.Text = count + (count == 1 ? " measurement recorded" : " measurements recorded");
        }

        private void OnSimulateReadingClicked(object sender, EventArgs e)
        {
            if (!IsCalibrationComplete)
            {
                MessageBox.Show(
                    "Please calibrate Reference Point 1 (and Point 2 for jumps) in the Calibration tab first.",
                    "EDM Results — Calibration Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SwitchToTabIndex(2);
                return;
            }

            Random rng = new Random();
            int hzRaw = (int)((_point1.HorizontalGon + rng.NextDouble() * 15.0 + 5.0) * 100000);
            int vRaw = (int)(_point1.VerticalGon * 100000);
            int distRaw = (int)(_point1.SlopeDistanceMm + (double)_numOffsetMm.Value + rng.Next(12000, 65000));

            string fakeStonex = string.Format(
                "*21....{0:D17} 22....{1:D17} 31....{2:D17}00",
                hzRaw, vRaw, distRaw);

            _transceiver.InjectSimulatedLine(fakeStonex);
        }

        private void UpdateSerialSummaryCard()
        {
            if (_lblSerialSummary == null) return;
            SerialConfiguration cfg = BuildCurrentSerialConfig();
            string parityLabel = cfg.Parity == Parity.Even ? "Even (E)" : (cfg.Parity == Parity.Odd ? "Odd (O)" : "None (N)");
            string stopLabel = cfg.StopBits == StopBits.Two ? "2" : "1";
            _lblSerialSummary.Text = string.Format(
                "Active Configuration:\r\n{0}  ·  {1} bps  ·  {2} data bits  ·  Parity {3}  ·  Stop {4}\r\nScoreboard Output (STX/ETX): {5}",
                cfg.PortName,
                cfg.BaudRate,
                cfg.DataBits,
                parityLabel,
                stopLabel,
                cfg.ForwardToScoreboard ? "Enabled" : "Disabled");
        }

        private void ConnectSerialPort(bool showErrorDialog)
        {
            SerialConfiguration cfg = BuildCurrentSerialConfig();
            UpdateSerialSummaryCard();

            string err;
            if (_transceiver.TryOpen(cfg, out err))
            {
                _topNav.SerialOk = true;
                _topNav.SerialText = cfg.PortName + " Online";
                _topNav.Invalidate();

                if (_chipSerialCardStatus != null)
                {
                    _chipSerialCardStatus.Text = cfg.PortName + " Online";
                    _chipSerialCardStatus.SetTone(Theme.OkText, Theme.OkDot, Theme.OkSoftBg, Theme.OkBorder);
                }
            }
            else
            {
                _topNav.SerialOk = false;
                _topNav.SerialText = cfg.PortName + " Offline";
                _topNav.Invalidate();

                if (_chipSerialCardStatus != null)
                {
                    _chipSerialCardStatus.Text = cfg.PortName + " Offline";
                    _chipSerialCardStatus.SetTone(Theme.WarnText, Theme.WarnDot, Theme.WarnSoftBg, Theme.WarnBorder);
                }

                if (showErrorDialog)
                {
                    MessageBox.Show(
                        "Unable to open serial port " + cfg.PortName + ":\n" + err,
                        "EDM Results — Serial Communication",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private SerialConfiguration BuildCurrentSerialConfig()
        {
            SerialConfiguration cfg = new SerialConfiguration();
            cfg.PortName = _cbPort.SelectedItem != null ? _cbPort.SelectedItem.ToString() : "COM1";

            int bps;
            cfg.BaudRate = (_cbBaud.SelectedItem != null && int.TryParse(_cbBaud.SelectedItem.ToString(), out bps)) ? bps : 19200;
            cfg.DataBits = (_cbDataBits.SelectedIndex == 0) ? 7 : 8;

            if (_cbParity.SelectedIndex == 1) cfg.Parity = Parity.Even;
            else if (_cbParity.SelectedIndex == 2) cfg.Parity = Parity.Odd;
            else cfg.Parity = Parity.None;

            cfg.StopBits = (_cbStopBits.SelectedIndex == 1) ? StopBits.Two : StopBits.One;
            cfg.ForwardToScoreboard = _chkSendScoreboard.Checked;
            return cfg;
        }

        private void LoadHistoryFromFile()
        {
            _lstHistory.Items.Clear();
            if (!File.Exists(_historyPath)) return;
            try
            {
                foreach (string line in File.ReadAllLines(_historyPath))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _lstHistory.Items.Insert(0, line);
                    }
                }
                UpdateHistoryCounter();
            }
            catch
            {
            }
        }

        private void OnSerialStatusChanged(string statusText)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnSerialStatusChanged), statusText);
                return;
            }
            _lblSerialFooter.Text = statusText;
            if (!_transceiver.IsConnected && _topNav != null)
            {
                string portName = (_cbPort != null && _cbPort.SelectedItem != null) ? _cbPort.SelectedItem.ToString() : "COM";
                _topNav.SerialOk = false;
                _topNav.SerialText = portName + " Offline";
                _topNav.Invalidate();

                if (_chipSerialCardStatus != null)
                {
                    _chipSerialCardStatus.Text = portName + " Offline";
                    _chipSerialCardStatus.SetTone(Theme.WarnText, Theme.WarnDot, Theme.WarnSoftBg, Theme.WarnBorder);
                }
            }
        }

        private void OnWindowClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Exit EDM Results?",
                "EDM Results — Confirm Exit",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                AppProfile profile = new AppProfile
                {
                    DisciplineIndex = _cbDiscipline.SelectedIndex >= 0 ? _cbDiscipline.SelectedIndex : 0,
                    OffsetLocked = _chkAutoOffset.Checked,
                    OffsetMm = (int)_numOffsetMm.Value,
                    Point1 = _point1,
                    Point2 = _point2,
                    Serial = BuildCurrentSerialConfig()
                };
                SettingsRepository.SaveConfig(_configPath, profile);
            }
            catch
            {
            }

            _transceiver.Dispose();
        }
    }
}
