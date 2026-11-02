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
    /// <summary>
    /// Soft, daylight-readable Swiss/instrument palette — high contrast outdoors without harsh "AI slop" neon/cartoon styling.
    /// </summary>
    internal static class Theme
    {
        public static readonly Color BgCanvas       = Color.FromArgb(244, 246, 249); // #F4F6F9 Soft Warm Daylight Gray
        public static readonly Color BgSurface      = Color.FromArgb(255, 255, 255); // #FFFFFF Pure Surface White
        public static readonly Color BgMuted        = Color.FromArgb(248, 250, 252); // #F8FAFC Subtle Field Fill
        public static readonly Color BgSubtleHover  = Color.FromArgb(238, 242, 246); // #EEF2F6

        public static readonly Color BorderHairline = Color.FromArgb(220, 226, 235); // #DCE2EB Soft 1px Hairline
        public static readonly Color BorderStrong   = Color.FromArgb(195, 205, 218); // #C3CDDA

        public static readonly Color TextPrimary    = Color.FromArgb(20, 28, 40);    // #141C28 Deep Charcoal
        public static readonly Color TextSecondary  = Color.FromArgb(71, 84, 103);   // #475467 Slate Gray
        public static readonly Color TextMuted      = Color.FromArgb(115, 128, 148); // #738094

        public static readonly Color Accent         = Color.FromArgb(21, 94, 239);   // #155EEF Precision Cobalt
        public static readonly Color AccentHover    = Color.FromArgb(16, 75, 196);   // #104BC4
        public static readonly Color AccentSoftBg   = Color.FromArgb(239, 244, 255); // #EFF4FF

        public static readonly Color OkText         = Color.FromArgb(18, 110, 62);   // #126E3E Deep Green
        public static readonly Color OkDot          = Color.FromArgb(22, 163, 74);   // #16A34A
        public static readonly Color OkSoftBg       = Color.FromArgb(236, 253, 243); // #ECFDF3
        public static readonly Color OkBorder       = Color.FromArgb(171, 239, 198); // #ABEFC6

        public static readonly Color WarnText       = Color.FromArgb(154, 74, 12);   // #9A4A0C Warm Amber
        public static readonly Color WarnDot        = Color.FromArgb(217, 119, 6);   // #D97706
        public static readonly Color WarnSoftBg     = Color.FromArgb(255, 248, 235); // #FFF8EB
        public static readonly Color WarnBorder     = Color.FromArgb(249, 219, 166); // #F9DBA6

        public static readonly Color ErrorText      = Color.FromArgb(170, 32, 32);   // #AA2020 Brick Red
        public static readonly Color ErrorHover     = Color.FromArgb(145, 24, 24);
        public static readonly Color ErrorDot       = Color.FromArgb(220, 38, 38);   // #DC2626
        public static readonly Color ErrorSoftBg    = Color.FromArgb(254, 242, 242); // #FEF2F2
        public static readonly Color ErrorBorder    = Color.FromArgb(248, 198, 198); // #F8C6C6

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
            StrokeColor = Theme.BorderHairline;
            CornerRadius = 8;
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

    internal sealed class SoftButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;

        public Color NormalColor { get; set; }
        public Color HoverColor { get; set; }
        public Color BorderColor { get; set; }
        public int CornerRadius { get; set; }

        public SoftButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            NormalColor = Theme.Accent;
            HoverColor = Theme.AccentHover;
            BorderColor = Color.Transparent;
            ForeColor = Color.White;
            CornerRadius = 6;
            Cursor = Cursors.Hand;
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
                ? Color.FromArgb(235, 239, 245)
                : (_isPressed ? ControlPaint.Dark(HoverColor, 0.04f) : (_isHovered ? HoverColor : NormalColor));

            Color textCol = Enabled ? ForeColor : Theme.TextMuted;

            using (GraphicsPath path = Theme.CreateRoundedRect(rect, CornerRadius))
            {
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, path);
                }
                Color stroke = !Enabled ? Theme.BorderHairline : BorderColor;
                if (stroke != Color.Transparent)
                {
                    using (Pen p = new Pen(stroke, 1f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rect,
                textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>
    /// Subtle, compact status chip with a small indicator dot and soft 6px border radius.
    /// </summary>
    internal sealed class StatusChip : Control
    {
        private Color _textColor = Theme.ErrorText;
        private Color _dotColor = Theme.ErrorDot;
        private Color _bgColor = Theme.ErrorSoftBg;
        private Color _borderColor = Theme.ErrorBorder;

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
            Size = new Size(165, 28);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.ResolveOpaqueParentColor(this, Theme.BgSurface));

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.CreateRoundedRect(rect, 6))
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
                g.FillEllipse(dotBrush, 11, (Height - dotSize) / 2, dotSize, dotSize);
            }

            Rectangle textRect = new Rectangle(24, 0, Width - 30, Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textRect,
                _textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>
    /// Single unified top navigation and status header painted in one surface so badges and tabs never clip or collide at any DPI.
    /// </summary>
    internal sealed class TopNavigationBar : Panel
    {
        private readonly string[] _tabTitles = new string[]
        {
            "Risultato live",
            "Storico misure",
            "Calibrazione",
            "Porta seriale"
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
            Height = 104;
            BackColor = Theme.BgSurface;
            CalibrationText = "Non calibrato";
            SerialText = "COM1 offline";
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= _tabTitles.Length) return;
            _selectedIndex = index;
            Invalidate();
        }

        private Rectangle GetTabRect(int index)
        {
            int leftMargin = 24;
            int tabTop = 58;
            int tabHeight = 36;
            int tabWidth = 148;
            int gap = 8;
            return new Rectangle(leftMargin + index * (tabWidth + gap), tabTop, tabWidth, tabHeight);
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

            // 1. Brand Title & Subtitle on Top Left
            using (Font brandFont = new Font("Segoe UI Semibold", 14.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    "EDM Results",
                    brandFont,
                    new Point(22, 13),
                    Theme.TextPrimary);

                TextRenderer.DrawText(
                    g,
                    "Telemetria stazioni totali per atletica leggera",
                    subFont,
                    new Point(158, 19),
                    Theme.TextMuted);
            }

            // 2. Top-Right Status Chips (mathematically positioned at Y=14, Height=28 so they NEVER clip)
            DrawStatusChip(
                g,
                new Rectangle(Width - 364, 14, 162, 28),
                CalibrationText,
                CalibrationOk ? Theme.OkText : Theme.ErrorText,
                CalibrationOk ? Theme.OkDot : Theme.ErrorDot,
                CalibrationOk ? Theme.OkSoftBg : Theme.ErrorSoftBg,
                CalibrationOk ? Theme.OkBorder : Theme.ErrorBorder);

            DrawStatusChip(
                g,
                new Rectangle(Width - 192, 14, 168, 28),
                SerialText,
                SerialOk ? Theme.OkText : Theme.WarnText,
                SerialOk ? Theme.OkDot : Theme.WarnDot,
                SerialOk ? Theme.OkSoftBg : Theme.WarnSoftBg,
                SerialOk ? Theme.OkBorder : Theme.WarnBorder);

            // 3. Subtle divider between header row and tab strip
            using (Pen divPen = new Pen( Color.FromArgb(236, 240, 245), 1f))
            {
                g.DrawLine(divPen, 24, 50, Width - 24, 50);
            }

            // 4. Clean, soft segmented navigation tabs
            using (Font tabFont = new Font("Segoe UI Semibold", 9.25f, FontStyle.Regular))
            {
                for (int i = 0; i < _tabTitles.Length; i++)
                {
                    Rectangle r = GetTabRect(i);
                    bool active = (i == _selectedIndex);
                    bool hovered = (i == _hoveredIndex);

                    if (active)
                    {
                        using (GraphicsPath path = Theme.CreateRoundedRect(r, 6))
                        using (SolidBrush fill = new SolidBrush(Theme.AccentSoftBg))
                        using (Pen border = new Pen( Color.FromArgb(180, 206, 255), 1f))
                        {
                            g.FillPath(fill, path);
                            g.DrawPath(border, path);
                        }
                    }
                    else if (hovered)
                    {
                        using (GraphicsPath path = Theme.CreateRoundedRect(r, 6))
                        using (SolidBrush fill = new SolidBrush(Theme.BgSubtleHover))
                        {
                            g.FillPath(fill, path);
                        }
                    }

                    TextRenderer.DrawText(
                        g,
                        _tabTitles[i],
                        tabFont,
                        r,
                        active ? Theme.Accent : Theme.TextSecondary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // Bottom border hairline
            using (Pen bottomPen = new Pen(Theme.BorderHairline, 1f))
            {
                g.DrawLine(bottomPen, 0, Height - 1, Width, Height - 1);
            }
        }

        private static void DrawStatusChip(Graphics g, Rectangle rect, string label, Color textCol, Color dotCol, Color bgCol, Color borderCol)
        {
            using (GraphicsPath path = Theme.CreateRoundedRect(rect, 6))
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

            int dotSize = 7;
            using (SolidBrush dot = new SolidBrush(dotCol))
            {
                g.FillEllipse(dot, rect.X + 11, rect.Y + (rect.Height - dotSize) / 2, dotSize, dotSize);
            }

            Rectangle textRect = new Rectangle(rect.X + 24, rect.Y, rect.Width - 28, rect.Height);
            using (Font f = new Font("Segoe UI Semibold", 8.5f, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    label,
                    f,
                    textRect,
                    textCol,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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
            Text = "EDM Results — Calibrazione";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 340);
            BackColor = Theme.BgCanvas;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                Padding = new Padding(22),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));

            Label lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = pointIndex > 0 ? ("Acquisizione Punto di riferimento " + pointIndex) : "Acquisizione Centro pedana",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _statusChip = new StatusChip
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 28,
                Text = "In attesa di lettura dallo strumento o inserimento manuale"
            };
            _statusChip.SetTone(Theme.WarnText, Theme.WarnDot, Theme.WarnSoftBg, Theme.WarnBorder);

            SurfaceCard fieldsCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 14),
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

            grid.Controls.Add(CreateFieldLabel("Angolo orizzontale (Hz) [gon]"), 0, 0);
            grid.Controls.Add(_txtHz, 1, 0);
            grid.Controls.Add(CreateFieldLabel("Angolo verticale (V) [gon]"), 0, 1);
            grid.Controls.Add(_txtV, 1, 1);
            grid.Controls.Add(CreateFieldLabel("Distanza inclinata (SD) [mm]"), 0, 2);
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
                Margin = new Padding(0, 10, 0, 0)
            };
            buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            buttonBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            SoftButton btnAccept = new SoftButton
            {
                Dock = DockStyle.Fill,
                Text = "Conferma punto",
                NormalColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnAccept.Click += OnAcceptClicked;

            SoftButton btnCancel = new SoftButton
            {
                Dock = DockStyle.Fill,
                Text = "Annulla",
                NormalColor = Theme.BgSurface,
                HoverColor = Theme.BgSubtleHover,
                ForeColor = Theme.TextPrimary,
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
                BackColor = Theme.BgMuted,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11f, FontStyle.Regular),
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
            _statusChip.Text = "Lettura acquisita dallo strumento — premi Conferma";
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
                    "Memorizzare il punto di calibrazione acquisito?",
                    "EDM Results — Conferma",
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
                    "Ignorare la misura ricevuta?",
                    "EDM Results — Conferma",
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
            ClientSize = new Size(840, 600);
            MinimumSize = new Size(760, 540);
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

            // 1. Unified Custom-Painted Header & Tab Strip (zero clipping at any DPI)
            _topNav = new TopNavigationBar();
            _topNav.TabSelected += OnTopNavTabSelected;

            // 2. Subtle Bottom Status Bar
            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Theme.BgSurface
            };
            footer.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderHairline, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
                }
            };
            _lblSerialFooter = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "EDM Results pronto",
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(22, 0, 0, 0),
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular)
            };
            footer.Controls.Add(_lblSerialFooter);

            // 3. Main View Container
            _viewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                Padding = new Padding(24, 18, 24, 18)
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
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // Discipline card
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));  // Soft warning banner
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Main readout card
            _measureLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Bottom action bar

            // Row 0: Discipline & Offset Summary Card
            SurfaceCard infoCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(20, 10, 20, 10),
                Margin = new Padding(0, 0, 0, 10)
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
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label lblSpecTag = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Specialità attiva",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblParamTag = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Configurazione geometrica",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblActiveDiscipline = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Giavellotto",
                Font = new Font("Segoe UI Semibold", 14.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblActiveOffsetInfo = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Offset: 8000 mm  ·  Centro pedana",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight
            };

            infoGrid.Controls.Add(lblSpecTag, 0, 0);
            infoGrid.Controls.Add(lblParamTag, 1, 0);
            infoGrid.Controls.Add(_lblActiveDiscipline, 0, 1);
            infoGrid.Controls.Add(_lblActiveOffsetInfo, 1, 1);
            infoCard.Controls.Add(infoGrid);

            // Row 1: Subtle Warning Banner when uncalibrated
            _bannerCalibrationWarning = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.WarnSoftBg,
                StrokeColor = Theme.WarnBorder,
                CornerRadius = 6,
                Padding = new Padding(12, 4, 12, 4),
                Margin = new Padding(0, 0, 0, 10)
            };
            Label lblWarnText = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.WarnSoftBg,
                Text = "Calibrazione non completata — apri la scheda Calibrazione per acquisire i punti di riferimento.",
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
                StrokeColor = Theme.BorderHairline,
                StrokeWidth = 1f,
                CornerRadius = 10,
                Padding = new Padding(24, 16, 24, 16),
                Margin = new Padding(0, 0, 0, 12)
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
            readoutGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            readoutGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label lblReadoutCaption = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Misura rilevata",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblTimestampReadout = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Ora: --:--",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblAttemptCounter = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Prove: 0",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblDigitalReadout = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "—",
                Font = new Font("Bahnschrift", 64f, FontStyle.Bold),
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

            // Row 3: Clean Action Bar
            TableLayoutPanel actionGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCanvas,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            _btnMarkFoul = new SoftButton
            {
                Dock = DockStyle.Fill,
                Text = "Segna prova nulla",
                NormalColor = Theme.ErrorText,
                HoverColor = Theme.ErrorHover,
                ForeColor = Color.White,
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Regular),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnMarkFoul.Click += (s, e) => RecordMeasurement(double.NaN, true);

            _btnSimulateReading = new SoftButton
            {
                Dock = DockStyle.Fill,
                Text = "Simula lettura EDM",
                NormalColor = Theme.BgSurface,
                HoverColor = Theme.BgSubtleHover,
                ForeColor = Theme.TextPrimary,
                BorderColor = Theme.BorderStrong,
                CornerRadius = 8,
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
            int availableHeight = Math.Max(60, _displayCard.ClientSize.Height - 54);
            float targetPt = Math.Max(42f, Math.Min(115f, availableHeight * 0.46f));
            if (Math.Abs(_lblDigitalReadout.Font.Size - targetPt) > 2f)
            {
                _lblDigitalReadout.Font = new Font("Bahnschrift", targetPt, FontStyle.Bold);
            }
        }

        private Panel BuildHistoryView()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCanvas, Visible = false };

            SurfaceCard listCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(22, 18, 22, 18)
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

            Label lblHeader = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Cronologia risultati della sessione",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblHistoryCount = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "0 misure registrate",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Owner-drawn soft list container so there is no harsh black 1990s WinForms border
            SurfaceCard listFrame = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgMuted,
                StrokeColor = Theme.BorderHairline,
                CornerRadius = 6,
                Padding = new Padding(6),
                Margin = new Padding(0, 8, 0, 12)
            };

            _lstHistory = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgMuted,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 32,
                Font = new Font("Consolas", 12f, FontStyle.Regular)
            };
            _lstHistory.DrawItem += OnHistoryDrawItem;
            listFrame.Controls.Add(_lstHistory);

            SoftButton btnClear = new SoftButton
            {
                Text = "Svuota storico",
                Width = 170,
                Dock = DockStyle.Left,
                NormalColor = Theme.BgSurface,
                HoverColor = Theme.ErrorSoftBg,
                ForeColor = Theme.ErrorText,
                BorderColor = Theme.ErrorBorder
            };
            btnClear.Click += (s, e) =>
            {
                if (_lstHistory.Items.Count == 0) return;
                if (MessageBox.Show("Cancellare tutti i risultati EDM della sessione?", "EDM Results", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                : (e.Index % 2 == 0 ? Theme.BgSurface : Theme.BgMuted);

            using (SolidBrush b = new SolidBrush(rowBg))
            {
                g.FillRectangle(b, e.Bounds);
            }

            string itemText = _lstHistory.Items[e.Index].ToString();
            bool isFoul = itemText.IndexOf("NULLO", StringComparison.OrdinalIgnoreCase) >= 0;

            int attemptNum = _lstHistory.Items.Count - e.Index;
            Rectangle numRect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, 60, e.Bounds.Height);
            Rectangle valRect = new Rectangle(e.Bounds.X + 76, e.Bounds.Y, e.Bounds.Width - 88, e.Bounds.Height);

            using (Font numFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    "#" + attemptNum,
                    numFont,
                    numRect,
                    Theme.TextMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            TextRenderer.DrawText(
                g,
                itemText,
                e.Font,
                valRect,
                isFoul ? Theme.ErrorText : Theme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));

            // Row 0: Discipline & Offset Card (spans 2 columns)
            SurfaceCard specCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(20, 12, 20, 12),
                Margin = new Padding(0, 0, 0, 10)
            };

            TableLayoutPanel specGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 3,
                RowCount = 2
            };
            specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155f));
            specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            specGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            specGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            Label lblSpec = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Specialità gara",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cbDiscipline = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgMuted,
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
                Text = "Offset raggio [mm]",
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
                BackColor = Theme.BgMuted,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 11f, FontStyle.Regular),
                Margin = new Padding(0, 6, 14, 4)
            };
            _numOffsetMm.ValueChanged += (s, e) => UpdateMeasureHeaderSummary();

            _chkAutoOffset = new CheckBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Usa raggio regolamentare automatico (World Athletics)",
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
                "Punto di riferimento 1",
                "Da acquisire",
                "Acquisisci Punto 1",
                out _chipPoint1State,
                out _lblP1Coords,
                out _btnCaptureP1);
            p1Card.Margin = new Padding(0, 0, 6, 10);
            _btnCaptureP1.Click += OnCapturePoint1Clicked;

            // Row 1 Col 1: Point 2 Card
            SurfaceCard p2Card = BuildPointCard(
                "Punto di riferimento 2",
                "Non richiesto",
                "Acquisisci Punto 2",
                out _chipPoint2State,
                out _lblP2Coords,
                out _btnCaptureP2);
            p2Card.Margin = new Padding(6, 0, 0, 10);
            _btnCaptureP2.Click += OnCapturePoint2Clicked;

            // Row 2: Baseline Footer Card
            SurfaceCard baselineCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(20, 10, 20, 10)
            };

            TableLayoutPanel baseGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 1
            };
            baseGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            baseGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            _btnSwapPoints = new SoftButton
            {
                Dock = DockStyle.Fill,
                Text = "Inverti Punto 1 e Punto 2 (cambia segno retta)",
                NormalColor = Theme.BgMuted,
                HoverColor = Theme.BgSubtleHover,
                ForeColor = Theme.TextPrimary,
                BorderColor = Theme.BorderStrong
            };
            _btnSwapPoints.Click += OnSwapPointsClicked;

            _lblBaselineSpanValue = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Distanza base P1–P2:  N/A",
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
                Padding = new Padding(20, 14, 20, 14)
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
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));

            Label lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = title,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            chip = new StatusChip
            {
                Anchor = AnchorStyles.Right,
                Size = new Size(148, 26),
                Text = initialChip
            };

            coordsLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Hz:   —\r\nV:    —\r\nDist: —",
                Font = new Font("Consolas", 10.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            captureButton = new SoftButton
            {
                Dock = DockStyle.Fill,
                Text = buttonCaption,
                NormalColor = Theme.Accent,
                HoverColor = Theme.AccentHover
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

            SurfaceCard comCard = new SurfaceCard
            {
                Dock = DockStyle.Fill,
                FillColor = Theme.BgSurface,
                Padding = new Padding(26, 20, 26, 20)
            };

            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                ColumnCount = 2,
                RowCount = 8
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            for (int i = 0; i < 5; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));

            Label lblHeader = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Parametri comunicazione seriale RS-232",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
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
            _cbParity.Items.AddRange(new object[] { "Nessuna", "Pari", "Dispari" });

            _cbStopBits = CreateSerialCombo();
            _cbStopBits.Items.AddRange(new object[] { "1", "2" });

            _chkSendScoreboard = new CheckBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSurface,
                Text = "Ritrasmetti misura al PC / tabellone su porta seriale (protocollo STX/ETX)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                Checked = true
            };

            _btnReconnectSerial = new SoftButton
            {
                Text = "Applica e connetti porta COM",
                Width = 260,
                Dock = DockStyle.Left,
                NormalColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                Margin = new Padding(0, 6, 0, 0)
            };
            _btnReconnectSerial.Click += (s, e) => ConnectSerialPort(true);

            grid.Controls.Add(lblHeader, 0, 0);
            grid.SetColumnSpan(lblHeader, 2);

            grid.Controls.Add(CreateSerialLabel("Porta seriale"), 0, 1);
            grid.Controls.Add(_cbPort, 1, 1);

            grid.Controls.Add(CreateSerialLabel("Velocità (baud rate)"), 0, 2);
            grid.Controls.Add(_cbBaud, 1, 2);

            grid.Controls.Add(CreateSerialLabel("Bit di dati"), 0, 3);
            grid.Controls.Add(_cbDataBits, 1, 3);

            grid.Controls.Add(CreateSerialLabel("Controllo parità"), 0, 4);
            grid.Controls.Add(_cbParity, 1, 4);

            grid.Controls.Add(CreateSerialLabel("Bit di stop"), 0, 5);
            grid.Controls.Add(_cbStopBits, 1, 5);

            grid.Controls.Add(_chkSendScoreboard, 0, 6);
            grid.SetColumnSpan(_chkSendScoreboard, 2);

            grid.Controls.Add(_btnReconnectSerial, 0, 7);
            grid.SetColumnSpan(_btnReconnectSerial, 2);

            comCard.Controls.Add(grid);
            panel.Controls.Add(comCard);
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
                Width = 220,
                Anchor = AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgMuted,
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

                if (profile.Point1 != null || profile.Point2 != null)
                {
                    DialogResult resume = MessageBox.Show(
                        "Continuare la gara precedente utilizzando i punti di calibrazione salvati?",
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
            string modeLabel = (d.Mode == CalibrationKind.TwoPointBaseline) ? "Linea 2 punti" : "Centro pedana";
            _lblActiveOffsetInfo.Text = string.Format("Offset: {0} mm  ·  {1}", _numOffsetMm.Value, modeLabel);
        }

        private void UpdateCalibrationVisuals()
        {
            if (_point1 != null)
            {
                _chipPoint1State.Text = "Acquisito";
                _chipPoint1State.SetTone(Theme.OkText, Theme.OkDot, Theme.OkSoftBg, Theme.OkBorder);
            }
            else
            {
                _chipPoint1State.Text = "Da acquisire";
                _chipPoint1State.SetTone(Theme.ErrorText, Theme.ErrorDot, Theme.ErrorSoftBg, Theme.ErrorBorder);
            }
            _lblP1Coords.Text = FormatPointSummary(_point1);

            if (!RequiresTwoPoints)
            {
                _chipPoint2State.Text = "Non richiesto";
                _chipPoint2State.SetTone(Theme.TextMuted, Theme.BorderStrong, Theme.BgMuted, Theme.BorderHairline);
                _lblP2Coords.Text = "Usato solo per Salto in Lungo\r\ne Salto Triplo";
                _lblBaselineSpanValue.Text = "Distanza base P1–P2:  N/A";
            }
            else
            {
                if (_point2 != null)
                {
                    _chipPoint2State.Text = "Acquisito";
                    _chipPoint2State.SetTone(Theme.OkText, Theme.OkDot, Theme.OkSoftBg, Theme.OkBorder);
                }
                else
                {
                    _chipPoint2State.Text = "Da acquisire";
                    _chipPoint2State.SetTone(Theme.ErrorText, Theme.ErrorDot, Theme.ErrorSoftBg, Theme.ErrorBorder);
                }
                _lblP2Coords.Text = FormatPointSummary(_point2);
                _lblBaselineSpanValue.Text = (_baseline != null)
                    ? string.Format("Distanza base P1–P2:  {0:0.0} mm", _baseline.SpanMm)
                    : "Distanza base P1–P2:  —";
            }

            bool ready = IsCalibrationComplete;
            _bannerCalibrationWarning.Visible = !ready;
            _measureLayout.RowStyles[1].Height = ready ? 0f : 46f;
            _btnMarkFoul.Enabled = ready;

            _topNav.CalibrationOk = ready;
            _topNav.CalibrationText = ready ? "EDM calibrato" : "Non calibrato";
            _topNav.Invalidate();
        }

        private static string FormatPointSummary(SurveyPoint pt)
        {
            if (pt == null) return "Hz:   —\r\nV:    —\r\nDist: —";
            return string.Format(
                "Hz:   {0} gon\r\nV:    {1} gon\r\nDist: {2} mm",
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
                            MessageBox.Show(err, "EDM Results — Errore Calibrazione", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            MessageBox.Show(err, "EDM Results — Errore Calibrazione", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            _lblTimestampReadout.Text = "Ora: " + timeTag;
            _lblDigitalReadout.Text = distTag.Trim();
            _lblDigitalReadout.ForeColor = isFoul ? Theme.ErrorText : Theme.OkText;
            _displayCard.StrokeColor = isFoul ? Theme.ErrorBorder : Theme.OkBorder;
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
                MessageBox.Show("Impossibile salvare il risultato EDM su file.", "EDM Results — Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateHistoryCounter()
        {
            int count = _lstHistory.Items.Count;
            _lblAttemptCounter.Text = "Prove: " + count;
            _lblHistoryCount.Text = count + (count == 1 ? " misura registrata" : " misure registrate");
        }

        private void OnSimulateReadingClicked(object sender, EventArgs e)
        {
            if (!IsCalibrationComplete)
            {
                MessageBox.Show(
                    "Calibra prima il Punto 1 (e il Punto 2 per i salti) nella scheda Calibrazione.",
                    "EDM Results — Calibrazione richiesta",
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

        private void ConnectSerialPort(bool showErrorDialog)
        {
            SerialConfiguration cfg = BuildCurrentSerialConfig();
            string err;
            if (_transceiver.TryOpen(cfg, out err))
            {
                _topNav.SerialOk = true;
                _topNav.SerialText = cfg.PortName + " connessa";
                _topNav.Invalidate();
            }
            else
            {
                _topNav.SerialOk = false;
                _topNav.SerialText = cfg.PortName + " offline";
                _topNav.Invalidate();
                if (showErrorDialog)
                {
                    MessageBox.Show(
                        "Impossibile aprire la porta " + cfg.PortName + ":\n" + err,
                        "EDM Results — Comunicazione seriale",
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
                _topNav.SerialText = portName + " offline";
                _topNav.Invalidate();
            }
        }

        private void OnWindowClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Uscire da EDM Results?",
                "EDM Results — Conferma uscita",
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
