using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using EDMResults.Core;

namespace EDMResults.UI
{
    internal enum VectorGlyph
    {
        None = 0,
        Crosshair,
        Ledger,
        Sliders,
        SerialPort,
        Ban,
        Bolt,
        LockClosed,
        CheckCircle,
        SwapArrows,
        TrashCan,
        CopyClipboard,
        DownloadCsv,
        Radar,
        Globe
    }

    internal static class Palette
    {
        public static readonly Color AppBackground = Color.FromArgb(244, 247, 251);
        public static readonly Color CardSurface = Color.FromArgb(255, 255, 255);
        public static readonly Color NavPillTrack = Color.FromArgb(233, 238, 246);
        public static readonly Color MutedWell = Color.FromArgb(248, 250, 253);
        public static readonly Color MutedWellHover = Color.FromArgb(238, 243, 250);

        public static readonly Color HairlineBorder = Color.FromArgb(222, 229, 239);
        public static readonly Color FocusBorder = Color.FromArgb(196, 208, 224);

        public static readonly Color InkPrimary = Color.FromArgb(15, 23, 42);
        public static readonly Color InkSecondary = Color.FromArgb(71, 85, 105);
        public static readonly Color InkTertiary = Color.FromArgb(132, 146, 166);

        public static readonly Color BrandBlue = Color.FromArgb(37, 99, 235);
        public static readonly Color BrandBlueTint = Color.FromArgb(239, 246, 255);
        public static readonly Color BrandBlueHover = Color.FromArgb(219, 234, 254);
        public static readonly Color BrandBlueBorder = Color.FromArgb(147, 197, 253);

        public static readonly Color EmeraldText = Color.FromArgb(5, 122, 85);
        public static readonly Color EmeraldDot = Color.FromArgb(16, 185, 129);
        public static readonly Color EmeraldTint = Color.FromArgb(236, 253, 245);
        public static readonly Color EmeraldHover = Color.FromArgb(209, 250, 229);
        public static readonly Color EmeraldBorder = Color.FromArgb(110, 231, 183);

        public static readonly Color AmberText = Color.FromArgb(180, 83, 9);
        public static readonly Color AmberDot = Color.FromArgb(245, 158, 11);
        public static readonly Color AmberTint = Color.FromArgb(255, 251, 235);
        public static readonly Color AmberBorder = Color.FromArgb(252, 211, 77);

        public static readonly Color CrimsonText = Color.FromArgb(190, 18, 60);
        public static readonly Color CrimsonDot = Color.FromArgb(244, 63, 94);
        public static readonly Color CrimsonTint = Color.FromArgb(255, 241, 242);
        public static readonly Color CrimsonHover = Color.FromArgb(255, 228, 230);
        public static readonly Color CrimsonBorder = Color.FromArgb(253, 164, 175);

        public static Font UiFont(float pt, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", pt, style, GraphicsUnit.Point);
        }

        public static Font MonoFont(float pt, FontStyle style = FontStyle.Regular)
        {
            return new Font("Consolas", pt, style, GraphicsUnit.Point);
        }

        public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0f || bounds.Height <= 0f) return path;
            if (radius <= 0.5f)
            {
                path.AddRectangle(bounds);
                return path;
            }
            float d = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
            path.AddArc(bounds.X, bounds.Y, d, d, 180f, 90f);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270f, 90f);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0f, 90f);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void RenderGlyph(Graphics g, VectorGlyph glyph, RectangleF box, Color strokeColor, float thickness = 1.75f)
        {
            if (glyph == VectorGlyph.None) return;
            var prevSmooth = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = box.X + (box.Width * 0.5f);
            float cy = box.Y + (box.Height * 0.5f);
            float r = Math.Min(box.Width, box.Height) * 0.40f;

            using (var pen = new Pen(strokeColor, thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (glyph)
                {
                    case VectorGlyph.Crosshair:
                        g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);
                        g.DrawLine(pen, cx - r - 2.5f, cy, cx - (r * 0.35f), cy);
                        g.DrawLine(pen, cx + (r * 0.35f), cy, cx + r + 2.5f, cy);
                        g.DrawLine(pen, cx, cy - r - 2.5f, cx, cy - (r * 0.35f));
                        g.DrawLine(pen, cx, cy + (r * 0.35f), cx, cy + r + 2.5f);
                        break;

                    case VectorGlyph.Ledger:
                        for (int row = -1; row <= 1; row++)
                        {
                            float ry = cy + (row * (r * 0.65f));
                            using (var brush = new SolidBrush(strokeColor))
                            {
                                g.FillEllipse(brush, cx - r - 1f, ry - 1.6f, 3.2f, 3.2f);
                            }
                            g.DrawLine(pen, cx - (r * 0.45f), ry, cx + r, ry);
                        }
                        break;

                    case VectorGlyph.Sliders:
                        float yTop = cy - (r * 0.55f);
                        float yBot = cy + (r * 0.55f);
                        g.DrawLine(pen, cx - r, yTop, cx + r, yTop);
                        g.DrawLine(pen, cx - r, yBot, cx + r, yBot);
                        using (var fill = new SolidBrush( Color.White))
                        {
                            g.FillEllipse(fill, cx - (r * 0.45f) - 2.8f, yTop - 2.8f, 5.6f, 5.6f);
                            g.DrawEllipse(pen, cx - (r * 0.45f) - 2.8f, yTop - 2.8f, 5.6f, 5.6f);
                            g.FillEllipse(fill, cx + (r * 0.35f) - 2.8f, yBot - 2.8f, 5.6f, 5.6f);
                            g.DrawEllipse(pen, cx + (r * 0.35f) - 2.8f, yBot - 2.8f, 5.6f, 5.6f);
                        }
                        break;

                    case VectorGlyph.SerialPort:
                        g.DrawLine(pen, cx - r, cy + r, cx - (r * 0.25f), cy + (r * 0.25f));
                        g.DrawArc(pen, cx - (r * 0.65f), cy - (r * 0.65f), r * 1.1f, r * 1.1f, -45f, 180f);
                        g.DrawLine(pen, cx + (r * 0.1f), cy - (r * 0.5f), cx + (r * 0.65f), cy - r);
                        g.DrawLine(pen, cx + (r * 0.5f), cy - (r * 0.1f), cx + r, cy - (r * 0.65f));
                        break;

                    case VectorGlyph.Ban:
                        g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);
                        float diag = r * 0.7f;
                        g.DrawLine(pen, cx - diag, cy - diag, cx + diag, cy + diag);
                        break;

                    case VectorGlyph.Bolt:
                        PointF[] bolt = new PointF[]
                        {
                            new PointF(cx + (r * 0.12f), cy - r),
                            new PointF(cx - (r * 0.65f), cy + (r * 0.08f)),
                            new PointF(cx + (r * 0.05f), cy + (r * 0.08f)),
                            new PointF(cx - (r * 0.12f), cy + r),
                            new PointF(cx + (r * 0.65f), cy - (r * 0.08f)),
                            new PointF(cx - (r * 0.05f), cy - (r * 0.08f))
                        };
                        g.DrawPolygon(pen, bolt);
                        break;

                    case VectorGlyph.LockClosed:
                        RectangleF body = new RectangleF(cx - (r * 0.75f), cy - (r * 0.1f), r * 1.5f, r * 1.1f);
                        using (GraphicsPath gp = RoundedRect(body, 2.5f))
                        {
                            g.DrawPath(pen, gp);
                        }
                        g.DrawArc(pen, cx - (r * 0.45f), cy - (r * 0.95f), r * 0.9f, r * 0.95f, 180f, 180f);
                        break;

                    case VectorGlyph.CheckCircle:
                        g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);
                        g.DrawLines(pen, new PointF[]
                        {
                            new PointF(cx - (r * 0.42f), cy + (r * 0.02f)),
                            new PointF(cx - (r * 0.08f), cy + (r * 0.38f)),
                            new PointF(cx + (r * 0.46f), cy - (r * 0.30f))
                        });
                        break;

                    case VectorGlyph.SwapArrows:
                        float topY = cy - (r * 0.38f);
                        float botY = cy + (r * 0.38f);
                        g.DrawLine(pen, cx - r, topY, cx + r, topY);
                        g.DrawLines(pen, new PointF[]
                        {
                            new PointF(cx + (r * 0.45f), topY - (r * 0.38f)),
                            new PointF(cx + r, topY),
                            new PointF(cx + (r * 0.45f), topY + (r * 0.38f))
                        });
                        g.DrawLine(pen, cx + r, botY, cx - r, botY);
                        g.DrawLines(pen, new PointF[]
                        {
                            new PointF(cx - (r * 0.45f), botY - (r * 0.38f)),
                            new PointF(cx - r, botY),
                            new PointF(cx - (r * 0.45f), botY + (r * 0.38f))
                        });
                        break;

                    case VectorGlyph.TrashCan:
                        g.DrawLine(pen, cx - (r * 0.85f), cy - (r * 0.55f), cx + (r * 0.85f), cy - (r * 0.55f));
                        g.DrawLine(pen, cx - (r * 0.35f), cy - (r * 0.85f), cx + (r * 0.35f), cy - (r * 0.85f));
                        g.DrawRectangle(pen, cx - (r * 0.62f), cy - (r * 0.55f), r * 1.24f, r * 1.45f);
                        break;

                    case VectorGlyph.CopyClipboard:
                        g.DrawRectangle(pen, cx - (r * 0.7f), cy - (r * 0.7f), r * 1.05f, r * 1.15f);
                        g.DrawRectangle(pen, cx - (r * 0.25f), cy - (r * 0.25f), r * 1.05f, r * 1.15f);
                        break;

                    case VectorGlyph.DownloadCsv:
                        g.DrawLine(pen, cx, cy - (r * 0.85f), cx, cy + (r * 0.35f));
                        g.DrawLines(pen, new PointF[]
                        {
                            new PointF(cx - (r * 0.45f), cy - (r * 0.05f)),
                            new PointF(cx, cy + (r * 0.42f)),
                            new PointF(cx + (r * 0.45f), cy - (r * 0.05f))
                        });
                        g.DrawLine(pen, cx - (r * 0.8f), cy + (r * 0.85f), cx + (r * 0.8f), cy + (r * 0.85f));
                        break;

                    case VectorGlyph.Radar:
                        g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);
                        g.DrawEllipse(pen, cx - (r * 0.5f), cy - (r * 0.5f), r, r);
                        g.DrawLine(pen, cx, cy, cx + (r * 0.72f), cy - (r * 0.68f));
                        break;

                    case VectorGlyph.Globe:
                        g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);
                        g.DrawEllipse(pen, cx - (r * 0.48f), cy - r, r * 0.96f, r * 2f);
                        g.DrawLine(pen, cx - r, cy, cx + r, cy);
                        break;
                }
            }

            g.SmoothingMode = prevSmooth;
        }
    }

    internal sealed class RoundedCardPanel : Panel
    {
        public Color SurfaceFill { get; set; }
        public Color BorderStroke { get; set; }
        public int RadiusPx { get; set; }
        public float BorderThickness { get; set; }

        public RoundedCardPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.Transparent;
            SurfaceFill = Palette.CardSurface;
            BorderStroke = Palette.HairlineBorder;
            RadiusPx = 12;
            BorderThickness = 1f;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath gp = Palette.RoundedRect(rect, RadiusPx))
            using (SolidBrush fill = new SolidBrush(SurfaceFill))
            using (Pen stroke = new Pen(BorderStroke, BorderThickness))
            {
                e.Graphics.FillPath(fill, gp);
                e.Graphics.DrawPath(stroke, gp);
            }
            base.OnPaint(e);
        }
    }

    internal sealed class ActionPillButton : Button
    {
        private bool _hovering;
        private bool _pressing;

        public Color BaseFill { get; set; }
        public Color HoverFill { get; set; }
        public Color OutlineColor { get; set; }
        public int CornerRadius { get; set; }
        public VectorGlyph Glyph { get; set; }

        public ActionPillButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Palette.InkPrimary;
            BaseFill = Palette.MutedWell;
            HoverFill = Palette.MutedWellHover;
            OutlineColor = Palette.HairlineBorder;
            CornerRadius = 8;
            Glyph = VectorGlyph.None;
            Cursor = Cursors.Hand;
            Font = Palette.UiFont(9.5f, FontStyle.Bold);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            _pressing = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _pressing = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressing = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color parentColor = Parent != null && Parent.BackColor != Color.Transparent
                ? Parent.BackColor
                : Palette.CardSurface;
            g.Clear(parentColor);

            Color fill = !Enabled ? Color.FromArgb(241, 245, 249) : (_hovering ? HoverFill : BaseFill);
            if (_pressing && Enabled)
            {
                fill = ControlPaint.Dark(fill, 0.03f);
            }
            Color fg = !Enabled ? Palette.InkTertiary : ForeColor;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath gp = Palette.RoundedRect(rect, CornerRadius))
            using (SolidBrush br = new SolidBrush(fill))
            {
                g.FillPath(br, gp);
                if (OutlineColor != Color.Transparent)
                {
                    using (Pen pen = new Pen(!Enabled ? Palette.HairlineBorder : OutlineColor, 1f))
                    {
                        g.DrawPath(pen, gp);
                    }
                }
            }

            if (Glyph != VectorGlyph.None)
            {
                Size txtSz = TextRenderer.MeasureText(g, Text, Font, Size.Empty, TextFormatFlags.NoPadding);
                float iconW = 15f;
                float gap = string.IsNullOrEmpty(Text) ? 0f : 7f;
                float totalW = iconW + gap + txtSz.Width;
                float startX = Math.Max(8f, (Width - totalW) * 0.5f);

                Palette.RenderGlyph(g, Glyph, new RectangleF(startX, (Height - iconW) * 0.5f, iconW, iconW), fg, 1.8f);
                if (!string.IsNullOrEmpty(Text))
                {
                    Rectangle textBounds = new Rectangle(
                        (int)Math.Round(startX + iconW + gap),
                        0,
                        txtSz.Width + 6,
                        Height);
                    TextRenderer.DrawText(g, Text, Font, textBounds, fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                }
            }
            else
            {
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }

    internal sealed class BadgeStatusIndicator : Control
    {
        private Color _fg = Palette.EmeraldText;
        private Color _dot = Palette.EmeraldDot;
        private Color _bg = Palette.EmeraldTint;
        private Color _border = Palette.EmeraldBorder;

        public BadgeStatusIndicator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Palette.UiFont(8.5f, FontStyle.Bold);
            Size = new Size(150, 28);
        }

        public void ApplyTone(string label, Color fg, Color dot, Color bg, Color border)
        {
            Text = label ?? string.Empty;
            _fg = fg;
            _dot = dot;
            _bg = bg;
            _border = border;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath gp = Palette.RoundedRect(rect, (Height - 1f) * 0.5f))
            using (SolidBrush fill = new SolidBrush(_bg))
            using (Pen stroke = new Pen(_border, 1f))
            {
                g.FillPath(fill, gp);
                g.DrawPath(stroke, gp);
            }

            float dotSize = 7.5f;
            float dotX = 11f;
            float dotY = (Height - dotSize) * 0.5f;
            using (SolidBrush dotBr = new SolidBrush(_dot))
            {
                g.FillEllipse(dotBr, dotX, dotY, dotSize, dotSize);
            }

            Rectangle textArea = new Rectangle(24, 0, Width - 30, Height);
            TextRenderer.DrawText(g, Text, Font, textArea, _fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>
    /// Custom millimeter offset adjuster replacing NumericUpDown with fast step buttons and direct input.
    /// </summary>
    internal sealed class MillimeterOffsetStepper : UserControl
    {
        private readonly TextBox _txtMillimeters;
        private int _valueMm = 1068;
        private bool _suppressEvents;

        public event EventHandler ValueChanged;

        public int ValueMillimeters
        {
            get { return _valueMm; }
            set
            {
                int clamped = Math.Max(-100000, Math.Min(100000, value));
                if (_valueMm == clamped && _txtMillimeters.Text == clamped.ToString(CultureInfo.InvariantCulture))
                {
                    return;
                }
                _valueMm = clamped;
                _suppressEvents = true;
                _txtMillimeters.Text = _valueMm.ToString(CultureInfo.InvariantCulture);
                _suppressEvents = false;
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        public MillimeterOffsetStepper()
        {
            Height = 36;
            Width = 280;
            BackColor = Color.Transparent;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48f));

            var btnMinus10 = BuildStepButton("-10", -10);
            var btnMinus1 = BuildStepButton("-1", -1);
            var btnPlus1 = BuildStepButton("+1", +1);
            var btnPlus10 = BuildStepButton("+10", +10);

            var centerBox = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                SurfaceFill = Palette.CardSurface,
                BorderStroke = Palette.FocusBorder,
                RadiusPx = 6,
                Margin = new Padding(3, 0, 3, 0),
                Padding = new Padding(6, 5, 6, 4)
            };

            _txtMillimeters = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = Palette.UiFont(10.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                TextAlign = HorizontalAlignment.Center,
                Text = "1068"
            };
            _txtMillimeters.TextChanged += OnTextEdited;
            _txtMillimeters.Leave += OnTextBoxLeave;
            centerBox.Controls.Add(_txtMillimeters);

            layout.Controls.Add(btnMinus10, 0, 0);
            layout.Controls.Add(btnMinus1, 1, 0);
            layout.Controls.Add(centerBox, 2, 0);
            layout.Controls.Add(btnPlus1, 3, 0);
            layout.Controls.Add(btnPlus10, 4, 0);

            Controls.Add(layout);
        }

        private ActionPillButton BuildStepButton(string caption, int delta)
        {
            var btn = new ActionPillButton
            {
                Text = caption,
                Dock = DockStyle.Fill,
                Margin = new Padding(1, 0, 1, 0),
                CornerRadius = 6,
                Font = Palette.UiFont(8.5f, FontStyle.Bold),
                BaseFill = Palette.MutedWell,
                HoverFill = Palette.BrandBlueTint,
                OutlineColor = Palette.HairlineBorder,
                ForeColor = Palette.InkSecondary
            };
            btn.Click += (s, e) => { ValueMillimeters = _valueMm + delta; };
            return btn;
        }

        private void OnTextEdited(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            int parsed;
            if (int.TryParse(_txtMillimeters.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                _valueMm = Math.Max(-100000, Math.Min(100000, parsed));
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private void OnTextBoxLeave(object sender, EventArgs e)
        {
            _suppressEvents = true;
            _txtMillimeters.Text = _valueMm.ToString(CultureInfo.InvariantCulture);
            _suppressEvents = false;
        }
    }

    /// <summary>
    /// Owner-drawn Attempt Ledger replacing standard WinForms ListBox, with PB highlighting and rich rows.
    /// </summary>
    internal sealed class AttemptLedgerView : ScrollableControl
    {
        private const int RowHeightPx = 38;
        private readonly List<RecordedAttemptEntry> _entries = new List<RecordedAttemptEntry>();
        private int _bestAttemptSequence = -1;
        private double _bestAttemptMeters = -1.0;
        private int _hoveredIndex = -1;
        private bool _compactMode;

        public bool CompactMode
        {
            get { return _compactMode; }
            set
            {
                _compactMode = value;
                Invalidate();
            }
        }

        public int RecordCount
        {
            get { return _entries.Count; }
        }

        public double BestMarkMeters
        {
            get { return _bestAttemptMeters; }
        }

        public string BestMarkFormatted
        {
            get
            {
                if (_bestAttemptMeters < 0.0) return "--,-- m";
                return AthleticsMeasurementEngine.FormatOfficialMark(_bestAttemptMeters).Trim();
            }
        }

        public IList<RecordedAttemptEntry> Entries
        {
            get { return _entries.AsReadOnly(); }
        }

        public AttemptLedgerView()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            AutoScroll = true;
            BackColor = Palette.MutedWell;
        }

        public void LoadEntries(IEnumerable<RecordedAttemptEntry> records)
        {
            _entries.Clear();
            if (records != null)
            {
                int seq = 1;
                foreach (RecordedAttemptEntry r in records)
                {
                    r.SequenceNumber = seq++;
                    _entries.Add(r);
                }
            }
            RecomputeBestAttempt();
            UpdateScrollExtent(scrollToBottom: true);
        }

        public void AppendEntry(RecordedAttemptEntry record)
        {
            if (record == null) return;
            record.SequenceNumber = _entries.Count + 1;
            _entries.Add(record);
            RecomputeBestAttempt();
            UpdateScrollExtent(scrollToBottom: true);
        }

        public void ClearAll()
        {
            _entries.Clear();
            _bestAttemptSequence = -1;
            _bestAttemptMeters = -1.0;
            UpdateScrollExtent(scrollToBottom: false);
        }

        private void RecomputeBestAttempt()
        {
            _bestAttemptSequence = -1;
            _bestAttemptMeters = -1.0;
            for (int i = 0; i < _entries.Count; i++)
            {
                RecordedAttemptEntry item = _entries[i];
                if (!item.IsFoulOrNull && item.NumericMeters > _bestAttemptMeters)
                {
                    _bestAttemptMeters = item.NumericMeters;
                    _bestAttemptSequence = item.SequenceNumber;
                }
            }
        }

        private void UpdateScrollExtent(bool scrollToBottom)
        {
            int totalHeight = Math.Max(0, _entries.Count * RowHeightPx + 8);
            AutoScrollMinSize = new Size(0, totalHeight);
            if (scrollToBottom && totalHeight > ClientSize.Height)
            {
                AutoScrollPosition = new Point(0, totalHeight);
            }
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int yOffset = e.Y - AutoScrollPosition.Y - 4;
            int idx = yOffset >= 0 ? (yOffset / RowHeightPx) : -1;
            if (idx < 0 || idx >= _entries.Count) idx = -1;
            if (idx != _hoveredIndex)
            {
                _hoveredIndex = idx;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);

            if (_entries.Count == 0)
            {
                using (Font emptyFont = Palette.UiFont(9.5f, FontStyle.Regular))
                {
                    TextRenderer.DrawText(
                        g,
                        I18n.T("Ledger.EmptySession"),
                        emptyFont,
                        ClientRectangle,
                        Palette.InkTertiary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return;
            }

            int scrollY = AutoScrollPosition.Y;
            int clientW = ClientSize.Width;

            using (Font fSeq = Palette.MonoFont(8.5f, FontStyle.Bold))
            using (Font fTime = Palette.MonoFont(9f, FontStyle.Regular))
            using (Font fMark = Palette.MonoFont(_compactMode ? 10.5f : 11.5f, FontStyle.Bold))
            using (Font fBadge = Palette.UiFont(7.5f, FontStyle.Bold))
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    int rowTop = 4 + (i * RowHeightPx) + scrollY;
                    if (rowTop + RowHeightPx < 0 || rowTop > ClientSize.Height) continue;

                    RecordedAttemptEntry item = _entries[i];
                    bool isBest = (!item.IsFoulOrNull && item.SequenceNumber == _bestAttemptSequence && _bestAttemptMeters > 0.0);
                    bool isLast = (i == _entries.Count - 1);

                    RectangleF rowRect = new RectangleF(6f, rowTop + 2f, clientW - 12f, RowHeightPx - 4f);
                    Color rowBg = i == _hoveredIndex
                        ? Palette.MutedWellHover
                        : (isLast ? Palette.BrandBlueTint : Color.White);
                    Color rowBorder = isBest
                        ? Palette.EmeraldBorder
                        : (isLast ? Palette.BrandBlueBorder : Palette.HairlineBorder);

                    using (GraphicsPath gp = Palette.RoundedRect(rowRect, 7f))
                    using (SolidBrush br = new SolidBrush(rowBg))
                    using (Pen pen = new Pen(rowBorder, isBest ? 1.4f : 1f))
                    {
                        g.FillPath(br, gp);
                        g.DrawPath(pen, gp);
                    }

                    // Sequence pill (#01)
                    string seqText = string.Format(CultureInfo.InvariantCulture, "#{0:00}", item.SequenceNumber);
                    Rectangle seqRect = new Rectangle(16, rowTop + 2, 40, RowHeightPx - 4);
                    TextRenderer.DrawText(g, seqText, fSeq, seqRect, Palette.InkTertiary, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                    // Time
                    Rectangle timeRect = new Rectangle(56, rowTop + 2, 54, RowHeightPx - 4);
                    TextRenderer.DrawText(g, item.TimestampText, fTime, timeRect, Palette.InkSecondary, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                    // PB / BEST badge in full mode
                    if (isBest)
                    {
                        RectangleF pbRect = new RectangleF(_compactMode ? 108f : 118f, rowTop + 9f, _compactMode ? 32f : 68f, 20f);
                        using (GraphicsPath pbPath = Palette.RoundedRect(pbRect, 10f))
                        using (SolidBrush pbFill = new SolidBrush(Palette.EmeraldTint))
                        using (Pen pbPen = new Pen(Palette.EmeraldBorder, 1f))
                        {
                            g.FillPath(pbFill, pbPath);
                            g.DrawPath(pbPen, pbPath);
                        }
                        TextRenderer.DrawText(
                            g,
                            _compactMode ? "PB" : I18n.T("Mark.BestBadge"),
                            fBadge,
                            Rectangle.Round(pbRect),
                            Palette.EmeraldText,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    // Official Mark right-aligned
                    Color markColor = item.IsFoulOrNull
                        ? Palette.CrimsonText
                        : (isBest ? Palette.EmeraldText : Palette.InkPrimary);
                    string markText = item.IsFoulOrNull ? I18n.T("Mark.Foul") : item.OfficialMarkText.Trim();
                    Rectangle markRect = new Rectangle(Math.Max(110, clientW - 140), rowTop + 2, 124, RowHeightPx - 4);
                    TextRenderer.DrawText(
                        g,
                        markText,
                        fMark,
                        markRect,
                        markColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
                }
            }
        }
    }

    /// <summary>
    /// Real-time 2D geometric field radar plotting Total Station (0,0), Circle Center P1 / Jump Line P1-P2,
    /// and the latest target landing vector.
    /// </summary>
    internal sealed class FieldGeometryRadarCanvas : Control
    {
        private CalibrationKind _mode = CalibrationKind.SingleCenterPoint;
        private PolarReading _p1;
        private PolarReading _p2;
        private PolarReading _target;
        private BaselineProjector2D _baseline;
        private int _offsetMm = 1068;

        public FieldGeometryRadarCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.MutedWell;
        }

        public void UpdateGeometryState(
            CalibrationKind mode,
            PolarReading p1,
            PolarReading p2,
            BaselineProjector2D baseline,
            PolarReading target,
            int offsetMm)
        {
            _mode = mode;
            _p1 = p1;
            _p2 = p2;
            _baseline = baseline;
            _target = target;
            _offsetMm = offsetMm;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF box = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath gp = Palette.RoundedRect(box, 10f))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(245, 248, 253)))
            using (Pen border = new Pen(Palette.HairlineBorder, 1f))
            {
                g.FillPath(bg, gp);
                g.DrawPath(border, gp);
            }

            // Collect active points to auto-scale viewport
            var pts = new List<PlanarVector2D> { new PlanarVector2D(0.0, 0.0) };
            if (_p1 != null) pts.Add(_p1.HorizontalProjection);
            if (_mode == CalibrationKind.TwoPointBaseline && _p2 != null) pts.Add(_p2.HorizontalProjection);
            if (_target != null) pts.Add(_target.HorizontalProjection);

            double minX = 0.0, maxX = 0.0, minY = 0.0, maxY = 0.0;
            for (int i = 0; i < pts.Count; i++)
            {
                if (pts[i].EastingM < minX) minX = pts[i].EastingM;
                if (pts[i].EastingM > maxX) maxX = pts[i].EastingM;
                if (pts[i].NorthingM < minY) minY = pts[i].NorthingM;
                if (pts[i].NorthingM > maxY) maxY = pts[i].NorthingM;
            }

            double padM = Math.Max(2.0, Math.Max(maxX - minX, maxY - minY) * 0.22);
            minX -= padM;
            maxX += padM;
            minY -= padM;
            maxY += padM;

            float padPx = 24f;
            float availW = Math.Max(40f, Width - (padPx * 2f));
            float availH = Math.Max(40f, Height - (padPx * 2f));
            double scaleX = availW / Math.Max(0.1, maxX - minX);
            double scaleY = availH / Math.Max(0.1, maxY - minY);
            double scale = Math.Min(scaleX, scaleY);

            double worldCx = (minX + maxX) * 0.5;
            double worldCy = (minY + maxY) * 0.5;
            float screenCx = Width * 0.5f;
            float screenCy = Height * 0.5f;

            Func<PlanarVector2D, PointF> mapPt = (PlanarVector2D v) => new PointF(
                screenCx + (float)((v.EastingM - worldCx) * scale),
                screenCy - (float)((v.NorthingM - worldCy) * scale));

            // Subtle radar rings around Total Station (0,0)
            PointF originScreen = mapPt(new PlanarVector2D(0.0, 0.0));
            using (Pen gridPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                gridPen.DashStyle = DashStyle.Dot;
                g.DrawLine(gridPen, 8f, originScreen.Y, Width - 8f, originScreen.Y);
                g.DrawLine(gridPen, originScreen.X, 8f, originScreen.X, Height - 8f);
            }

            // Draw Sight Lines from Total Station
            using (Pen sightPen = new Pen(Color.FromArgb(148, 163, 184), 1f))
            {
                sightPen.DashStyle = DashStyle.Dash;
                if (_p1 != null) g.DrawLine(sightPen, originScreen, mapPt(_p1.HorizontalProjection));
                if (_mode == CalibrationKind.TwoPointBaseline && _p2 != null)
                {
                    g.DrawLine(sightPen, originScreen, mapPt(_p2.HorizontalProjection));
                }
                if (_target != null) g.DrawLine(sightPen, originScreen, mapPt(_target.HorizontalProjection));
            }

            // Draw Circle or Baseline + Measured Vector
            if (_mode == CalibrationKind.SingleCenterPoint && _p1 != null)
            {
                PointF p1Screen = mapPt(_p1.HorizontalProjection);
                float radiusPx = (float)((_offsetMm * 0.001) * scale);
                if (radiusPx > 3f && radiusPx < 400f)
                {
                    using (Pen circlePen = new Pen(Palette.BrandBlueBorder, 1.5f))
                    using (SolidBrush circleFill = new SolidBrush(Color.FromArgb(28, 37, 99, 235)))
                    {
                        g.FillEllipse(circleFill, p1Screen.X - radiusPx, p1Screen.Y - radiusPx, radiusPx * 2f, radiusPx * 2f);
                        g.DrawEllipse(circlePen, p1Screen.X - radiusPx, p1Screen.Y - radiusPx, radiusPx * 2f, radiusPx * 2f);
                    }
                }

                if (_target != null)
                {
                    PointF targetScreen = mapPt(_target.HorizontalProjection);
                    using (Pen measurePen = new Pen(Palette.EmeraldDot, 2.4f))
                    {
                        g.DrawLine(measurePen, p1Screen, targetScreen);
                    }
                }
            }
            else if (_mode == CalibrationKind.TwoPointBaseline && _p1 != null && _p2 != null && _baseline != null && _baseline.IsValidSpan)
            {
                PointF p1Screen = mapPt(_p1.HorizontalProjection);
                PointF p2Screen = mapPt(_p2.HorizontalProjection);
                using (Pen basePen = new Pen(Palette.BrandBlue, 2.2f))
                {
                    g.DrawLine(basePen, p1Screen, p2Screen);
                }

                if (_target != null)
                {
                    PlanarVector2D foot = _baseline.ClosestFootPointOnBaseline(_target);
                    PointF footScreen = mapPt(foot);
                    PointF targetScreen = mapPt(_target.HorizontalProjection);
                    using (Pen orthoPen = new Pen(Palette.EmeraldDot, 2.4f))
                    {
                        g.DrawLine(orthoPen, footScreen, targetScreen);
                    }
                }
            }

            // Draw Markers
            DrawNodeMarker(g, originScreen, I18n.T("Radar.EdmOrigin"), Palette.InkSecondary);
            if (_p1 != null)
            {
                DrawNodeMarker(
                    g,
                    mapPt(_p1.HorizontalProjection),
                    _mode == CalibrationKind.SingleCenterPoint ? I18n.T("Radar.CenterP1") : I18n.T("Radar.Point1"),
                    Palette.BrandBlue);
            }
            if (_mode == CalibrationKind.TwoPointBaseline && _p2 != null)
            {
                DrawNodeMarker(g, mapPt(_p2.HorizontalProjection), I18n.T("Radar.Point2"), Palette.BrandBlue);
            }
            if (_target != null)
            {
                DrawNodeMarker(g, mapPt(_target.HorizontalProjection), I18n.T("Radar.Target"), Palette.EmeraldText);
            }
        }

        private static void DrawNodeMarker(Graphics g, PointF pt, string label, Color color)
        {
            using (SolidBrush halo = new SolidBrush(Color.FromArgb(45, color)))
            using (SolidBrush core = new SolidBrush(color))
            using (Font f = Palette.UiFont(7.5f, FontStyle.Bold))
            {
                g.FillEllipse(halo, pt.X - 7f, pt.Y - 7f, 14f, 14f);
                g.FillEllipse(core, pt.X - 3.5f, pt.Y - 3.5f, 7f, 7f);
                TextRenderer.DrawText(g, label, f, new Point((int)pt.X + 6, (int)pt.Y - 8), color);
            }
        }
    }

    internal sealed class HeaderTabSwitcher : Control
    {
        private readonly string[] _captions = new string[4];
        private int _historyCount;

        private readonly VectorGlyph[] _glyphs = new VectorGlyph[]
        {
            VectorGlyph.Crosshair,
            VectorGlyph.Ledger,
            VectorGlyph.Sliders,
            VectorGlyph.SerialPort
        };

        private int _activeTab;
        private int _hoverTab = -1;
        private bool _hoverLangPill;

        public event Action<int> ActiveTabChanged;

        public int ActiveTabIndex
        {
            get { return _activeTab; }
            set
            {
                if (value >= 0 && value < _captions.Length && _activeTab != value)
                {
                    _activeTab = value;
                    Invalidate();
                    if (ActiveTabChanged != null) ActiveTabChanged(_activeTab);
                }
            }
        }

        public HeaderTabSwitcher()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 62;
            BackColor = Palette.CardSurface;
            RefreshLocalizedCaptions();
        }

        public void RefreshLocalizedCaptions()
        {
            _captions[0] = I18n.T("Nav.LiveMeasure");
            string histBase = I18n.T("Nav.History");
            _captions[1] = _historyCount > 0 ? histBase + " (" + _historyCount + ")" : histBase;
            _captions[2] = I18n.T("Nav.Setup");
            _captions[3] = I18n.T("Nav.Serial");
            Invalidate();
        }

        public void SetHistoryCounter(int totalCount)
        {
            _historyCount = totalCount;
            RefreshLocalizedCaptions();
        }

        private RectangleF GetLangPillBounds()
        {
            return new RectangleF(Width - 94f, 13f, 72f, 36f);
        }

        private RectangleF GetPillBounds()
        {
            RectangleF langRect = GetLangPillBounds();
            float pillW = Math.Min(610f, Math.Max(410f, langRect.X - 335f));
            return new RectangleF(langRect.X - pillW - 10f, 11f, pillW, 40f);
        }

        private RectangleF GetTabSlot(int index)
        {
            RectangleF pill = GetPillBounds();
            float pad = 4f;
            float slotW = (pill.Width - (pad * 2f)) / _captions.Length;
            return new RectangleF(pill.X + pad + (index * slotW), pill.Y + pad, slotW, pill.Height - (pad * 2f));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool langHit = GetLangPillBounds().Contains(e.Location);
            int hit = -1;
            if (!langHit)
            {
                for (int i = 0; i < _captions.Length; i++)
                {
                    if (GetTabSlot(i).Contains(e.Location))
                    {
                        hit = i;
                        break;
                    }
                }
            }
            if (hit != _hoverTab || langHit != _hoverLangPill)
            {
                _hoverTab = hit;
                _hoverLangPill = langHit;
                Cursor = (_hoverTab >= 0 || _hoverLangPill) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverTab != -1 || _hoverLangPill)
            {
                _hoverTab = -1;
                _hoverLangPill = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            if (_hoverLangPill)
            {
                if (I18n.AvailableLanguages.Count <= 2)
                {
                    I18n.CycleNextLanguage();
                }
                else
                {
                    var menu = new ContextMenuStrip();
                    foreach (LanguagePack pack in I18n.AvailableLanguages)
                    {
                        LanguagePack captured = pack;
                        var item = new ToolStripMenuItem(captured.ToString())
                        {
                            Checked = (captured == I18n.CurrentLanguage)
                        };
                        item.Click += (s, ev) => I18n.SetLanguage(captured.Code);
                        menu.Items.Add(item);
                    }
                    RectangleF lb = GetLangPillBounds();
                    menu.Show(this, new Point((int)lb.X, (int)lb.Bottom + 2));
                }
                return;
            }

            if (_hoverTab >= 0)
            {
                ActiveTabIndex = _hoverTab;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);

            using (Pen bottomBorder = new Pen(Palette.HairlineBorder, 1f))
            {
                g.DrawLine(bottomBorder, 0, Height - 1, Width, Height - 1);
            }

            RectangleF badgeRect = new RectangleF(22f, 13f, 36f, 36f);
            using (GraphicsPath gp = Palette.RoundedRect(badgeRect, 9f))
            using (SolidBrush br = new SolidBrush(Palette.BrandBlue))
            {
                g.FillPath(br, gp);
            }
            Palette.RenderGlyph(g, VectorGlyph.Crosshair, new RectangleF(30f, 21f, 20f, 20f), Color.White, 2f);

            using (Font fBrand = Palette.UiFont(12.5f, FontStyle.Bold))
            using (Font fSub = Palette.UiFont(8f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, "EDM Results", fBrand, new Point(66, 11), Palette.InkPrimary);
                TextRenderer.DrawText(g, I18n.T("App.HeaderSubtitle"), fSub, new Point(67, 33), Palette.InkSecondary);
            }

            RectangleF pill = GetPillBounds();
            using (GraphicsPath gp = Palette.RoundedRect(pill, 10f))
            using (SolidBrush trackFill = new SolidBrush(Palette.NavPillTrack))
            {
                g.FillPath(trackFill, gp);
            }

            for (int i = 0; i < _captions.Length; i++)
            {
                RectangleF slot = GetTabSlot(i);
                bool active = (i == _activeTab);
                bool hovered = (i == _hoverTab);

                if (active)
                {
                    using (GraphicsPath gp = Palette.RoundedRect(slot, 7f))
                    using (SolidBrush fill = new SolidBrush(Color.White))
                    using (Pen pen = new Pen(Palette.HairlineBorder, 1f))
                    {
                        g.FillPath(fill, gp);
                        g.DrawPath(pen, gp);
                    }
                }
                else if (hovered)
                {
                    using (GraphicsPath gp = Palette.RoundedRect(slot, 7f))
                    using (SolidBrush fill = new SolidBrush(Color.FromArgb(130, 255, 255, 255)))
                    {
                        g.FillPath(fill, gp);
                    }
                }

                Color fg = active ? Palette.BrandBlue : (hovered ? Palette.InkPrimary : Palette.InkSecondary);
                using (Font tabFont = Palette.UiFont(8.8f, active ? FontStyle.Bold : FontStyle.Regular))
                {
                    Size txtSz = TextRenderer.MeasureText(g, _captions[i], tabFont, Size.Empty, TextFormatFlags.NoPadding);
                    float iconW = 14f;
                    float gap = 5f;
                    float totalW = iconW + gap + txtSz.Width;
                    float startX = slot.X + Math.Max(6f, (slot.Width - totalW) * 0.5f);

                    Palette.RenderGlyph(g, _glyphs[i], new RectangleF(startX, slot.Y + ((slot.Height - iconW) * 0.5f), iconW, iconW), fg, active ? 1.8f : 1.5f);
                    Rectangle textRect = new Rectangle((int)Math.Round(startX + iconW + gap), (int)slot.Y, txtSz.Width + 6, (int)slot.Height);
                    TextRenderer.DrawText(g, _captions[i], tabFont, textRect, fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                }
            }

            // Language Switcher Pill on right edge
            RectangleF langBounds = GetLangPillBounds();
            Color langBg = _hoverLangPill ? Palette.BrandBlueTint : Palette.MutedWell;
            Color langBorder = _hoverLangPill ? Palette.BrandBlueBorder : Palette.HairlineBorder;
            Color langFg = _hoverLangPill ? Palette.BrandBlue : Palette.InkPrimary;
            using (GraphicsPath gp = Palette.RoundedRect(langBounds, 9f))
            using (SolidBrush fill = new SolidBrush(langBg))
            using (Pen pen = new Pen(langBorder, 1f))
            {
                g.FillPath(fill, gp);
                g.DrawPath(pen, gp);
            }

            string badgeCode = I18n.CurrentLanguage != null ? I18n.CurrentLanguage.ShortBadge : "IT";
            Palette.RenderGlyph(g, VectorGlyph.Globe, new RectangleF(langBounds.X + 11f, langBounds.Y + 10.5f, 15f, 15f), langFg, 1.6f);
            using (Font langFont = Palette.UiFont(9f, FontStyle.Bold))
            {
                Rectangle langTextRect = new Rectangle((int)langBounds.X + 28, (int)langBounds.Y, (int)langBounds.Width - 32, (int)langBounds.Height);
                TextRenderer.DrawText(g, badgeCode, langFont, langTextRect, langFg, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
        }
    }

    internal sealed class GuidedCalibrationDialog : Form
    {
        private readonly Label _lblAzimuthGon;
        private readonly Label _lblZenithGon;
        private readonly Label _lblAzimuthDms;
        private readonly Label _lblSlopeDist;
        private readonly Label _lblHorizDist;
        private readonly Label _lblProtocolName;
        private readonly ActionPillButton _btnConfirm;
        private readonly BadgeStatusIndicator _badgeState;
        private PolarReading _latestPoint;

        public PolarReading AcceptedPoint
        {
            get { return _latestPoint; }
        }

        public GuidedCalibrationDialog(string targetPointTitle, PolarReading initialReading)
        {
            Text = I18n.Format("Modal.WindowTitle", targetPointTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(470, 385);
            BackColor = Palette.AppBackground;
            Font = Palette.UiFont(9.5f);

            var card = new RoundedCardPanel
            {
                Location = new Point(16, 16),
                Size = new Size(438, 353),
                Padding = new Padding(22)
            };
            Controls.Add(card);

            var lblTitle = new Label
            {
                Text = I18n.Format("Modal.HeaderTitle", targetPointTitle),
                Font = Palette.UiFont(13f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(22, 18),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            var lblHint = new Label
            {
                Text = I18n.T("Modal.HintText"),
                Font = Palette.UiFont(8.8f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 46),
                Size = new Size(394, 34)
            };
            card.Controls.Add(lblHint);

            _badgeState = new BadgeStatusIndicator
            {
                Location = new Point(22, 86),
                Size = new Size(394, 30)
            };
            _badgeState.ApplyTone(I18n.T("Modal.WaitingTelemetry"), Palette.AmberText, Palette.AmberDot, Palette.AmberTint, Palette.AmberBorder);
            card.Controls.Add(_badgeState);

            var metricsBox = new RoundedCardPanel
            {
                Location = new Point(22, 126),
                Size = new Size(394, 156),
                SurfaceFill = Palette.MutedWell,
                BorderStroke = Palette.HairlineBorder,
                RadiusPx = 10
            };
            card.Controls.Add(metricsBox);

            _lblAzimuthGon = AddMetricLine(metricsBox, I18n.T("Modal.MetricHorizGon"), "--", 12);
            _lblAzimuthDms = AddMetricLine(metricsBox, I18n.T("Modal.MetricDms"), "--", 38);
            _lblZenithGon = AddMetricLine(metricsBox, I18n.T("Modal.MetricZenithGon"), "--", 64);
            _lblSlopeDist = AddMetricLine(metricsBox, I18n.T("Modal.MetricSlopeDist"), "--", 90);
            _lblHorizDist = AddMetricLine(metricsBox, I18n.T("Modal.MetricHorizDist"), "--", 116);

            _lblProtocolName = new Label
            {
                Text = I18n.Format("Modal.ProtocolPrefix", "--"),
                Font = Palette.UiFont(8f, FontStyle.Italic),
                ForeColor = Palette.InkTertiary,
                Location = new Point(24, 286),
                Size = new Size(390, 16)
            };
            card.Controls.Add(_lblProtocolName);

            _btnConfirm = new ActionPillButton
            {
                Text = I18n.T("Modal.ConfirmBtn"),
                Glyph = VectorGlyph.CheckCircle,
                Location = new Point(22, 306),
                Size = new Size(190, 36),
                BaseFill = Palette.BrandBlue,
                HoverFill = Color.FromArgb(29, 78, 216),
                OutlineColor = Color.Transparent,
                ForeColor = Color.White,
                Enabled = false
            };
            _btnConfirm.Click += (s, e) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            card.Controls.Add(_btnConfirm);

            var btnCancel = new ActionPillButton
            {
                Text = I18n.T("Modal.CancelBtn"),
                Location = new Point(226, 306),
                Size = new Size(190, 36),
                BaseFill = Palette.MutedWell,
                HoverFill = Palette.MutedWellHover,
                OutlineColor = Palette.HairlineBorder,
                ForeColor = Palette.InkSecondary
            };
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            card.Controls.Add(btnCancel);

            if (initialReading != null)
            {
                BindReading(initialReading);
            }
        }

        private static Label AddMetricLine(Control parent, string caption, string initialValue, int topY)
        {
            var lblKey = new Label
            {
                Text = caption,
                Font = Palette.UiFont(8.8f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(14, topY),
                Size = new Size(185, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblVal = new Label
            {
                Text = initialValue,
                Font = Palette.MonoFont(9.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(200, topY),
                Size = new Size(180, 22),
                TextAlign = ContentAlignment.MiddleRight
            };
            parent.Controls.Add(lblKey);
            parent.Controls.Add(lblVal);
            return lblVal;
        }

        public void BindReading(PolarReading reading)
        {
            if (reading == null) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<PolarReading>(BindReading), reading);
                return;
            }

            _latestPoint = reading;
            _lblAzimuthGon.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.0000} gon", reading.HorizontalGon);
            _lblAzimuthDms.Text = reading.FormatHorizontalDms() + " / " + reading.FormatVerticalDms();
            _lblZenithGon.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.0000} gon", reading.VerticalGon);
            _lblSlopeDist.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.000} m", reading.SlopeDistanceM);
            _lblHorizDist.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.000} m", reading.HorizontalDistanceMeters);
            _lblProtocolName.Text = I18n.Format("Modal.ProtocolPrefix", reading.SourceProtocolName);

            _badgeState.ApplyTone(I18n.T("Modal.ReadingAcquired"), Palette.EmeraldText, Palette.EmeraldDot, Palette.EmeraldTint, Palette.EmeraldBorder);
            _btnConfirm.Enabled = true;
        }
    }

    public sealed class MainWindow : Form
    {
        private readonly SerialLinkController _serialController = new SerialLinkController();

        private HeaderTabSwitcher _headerNav;
        private Panel _stageContainer;
        private Panel _pageMeasure;
        private Panel _pageHistory;
        private Panel _pageSetup;
        private Panel _pageSerial;

        // Live Measure Page
        private TableLayoutPanel _measureGrid;
        private BadgeStatusIndicator _badgeSerialHeader;
        private BadgeStatusIndicator _badgeCalibrationHeader;
        private RoundedCardPanel _bannerUncalibrated;
        private Label _lblBannerWarnText;
        private ActionPillButton _btnBannerOpenSetup;
        private RoundedCardPanel _heroScoreboardCard;
        private Label _lblDisciplineBadge;
        private Label _lblHeroMark;
        private Label _lblDiagnosticSubline;
        private Label _lblTelemetryStrip;
        private Label _lblRadarHdr;
        private Label _lblMiniLogHdr;
        private ActionPillButton _btnRecordFoul;
        private ActionPillButton _btnInjectTestShot;
        private ActionPillButton _btnCopyLastMark;
        private FieldGeometryRadarCanvas _radarCanvas;
        private AttemptLedgerView _miniAttemptLedger;
        private Label _lblBestMeasureChip;

        // History Page
        private Label _lblHistoryTitle;
        private AttemptLedgerView _fullAttemptLedger;
        private Label _lblHistoryStats;
        private ActionPillButton _btnExportCsv;
        private ActionPillButton _btnCopyAll;
        private ActionPillButton _btnClearLedger;

        // Setup / Calibration Page
        private Label _lblSetupLeftTitle;
        private Label _lblSetupLeftSub;
        private Label _lblLanguageCap;
        private ComboBox _cboLanguage;
        private Label _lblDiscCap;
        private ComboBox _cboDiscipline;
        private Label _lblOffsetCap;
        private MillimeterOffsetStepper _offsetStepper;
        private ActionPillButton _btnPresetShotHammer;
        private ActionPillButton _btnPresetDiscus;
        private ActionPillButton _btnPresetJavelin;
        private ActionPillButton _btnPresetJumps;
        private CheckBox _chkLockCalibration;
        private Label _lblSetupRightTitle;
        private Label _lblSetupRightSub;
        private Label _lblP1Hdr;
        private Label _lblP2Hdr;
        private BadgeStatusIndicator _badgeP1Status;
        private BadgeStatusIndicator _badgeP2Status;
        private Label _lblP1Coordinates;
        private Label _lblP2Coordinates;
        private Label _lblBaselineSpanInfo;
        private Label _lblSetupLivePreview;
        private ActionPillButton _btnAcquireP1Modal;
        private ActionPillButton _btnUseLastAsP1;
        private ActionPillButton _btnAcquireP2Modal;
        private ActionPillButton _btnUseLastAsP2;
        private ActionPillButton _btnSwapP1P2;
        private ActionPillButton _btnClearCalibration;

        // Serial Port & Telemetry Inspector Page
        private Label _lblSerialTitle;
        private Label _lblSerialSub;
        private Label _lblComPortCaption;
        private Label _lblBaudRateCaption;
        private Label _lblDataBitsCaption;
        private Label _lblParityCaption;
        private Label _lblStopBitsCaption;
        private Label _lblProtocolCaption;
        private ComboBox _cboComPort;
        private ComboBox _cboBaudRate;
        private ComboBox _cboDataBits;
        private ComboBox _cboParity;
        private ComboBox _cboStopBits;
        private ComboBox _cboProtocolMode;
        private ActionPillButton _btnApplySerial;
        private ActionPillButton _btnRefreshPorts;
        private Label _lblMonTitle;
        private BadgeStatusIndicator _badgeSerialDetail;
        private Label _lblSerialEndpointSummary;
        private Label _lblRawHdr;
        private Label _lblLastRawPacketBox;
        private Label _lblSimTitle;
        private TextBox _txtCustomFrameInput;
        private ActionPillButton _btnInjectRaw;
        private ActionPillButton _btnPresetMatoP1;
        private ActionPillButton _btnPresetMatoShot;
        private ActionPillButton _btnPresetSet23;

        // Domain State
        private PolarReading _anchor1;
        private PolarReading _anchor2;
        private PolarReading _lastReceivedReading;
        private BaselineProjector2D _baselineProjector;
        private GuidedCalibrationDialog _openCalibrationModal;
        private bool _suppressDisciplineChangeHandler;
        private bool _suppressLanguageChangeHandler;
        private string _lastSerialStatusMessage = string.Empty;

        public MainWindow()
        {
            Text = I18n.T("App.WindowTitle");
            MinimumSize = new Size(920, 620);
            ClientSize = new Size(1060, 690);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Palette.AppBackground;
            Font = Palette.UiFont(9.5f);
            DoubleBuffered = true;
            KeyPreview = true;

            TryLoadApplicationIcon();
            ConstructUserInterface();

            _serialController.ReadingDecoded += OnReadingDecodedFromInstrument;
            _serialController.RawFrameReceived += OnRawFrameFromInstrument;
            _serialController.LinkStateChanged += OnSerialLinkStateChanged;
            I18n.LanguageChanged += OnApplicationLanguageChanged;

            Load += OnMainWindowShown;
            FormClosing += OnMainWindowClosing;
            KeyDown += OnMainWindowKeyDown;
        }

        private void TryLoadApplicationIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EDMRESULTS.ico");
                if (!File.Exists(icoPath))
                {
                    icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                }
                if (File.Exists(icoPath))
                {
                    Icon = new Icon(icoPath);
                }
            }
            catch
            {
            }
        }

        private void ConstructUserInterface()
        {
            _headerNav = new HeaderTabSwitcher { Dock = DockStyle.Top };
            _headerNav.ActiveTabChanged += SwitchVisiblePage;

            _stageContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 16, 22, 20),
                BackColor = Palette.AppBackground
            };

            _pageMeasure = BuildMeasureDashboardPage();
            _pageHistory = BuildHistoryLedgerPage();
            _pageSetup = BuildCalibrationSetupPage();
            _pageSerial = BuildSerialTelemetryPage();

            _stageContainer.Controls.Add(_pageMeasure);
            _stageContainer.Controls.Add(_pageHistory);
            _stageContainer.Controls.Add(_pageSetup);
            _stageContainer.Controls.Add(_pageSerial);

            Controls.Add(_stageContainer);
            Controls.Add(_headerNav);

            SwitchVisiblePage(0);
        }

        // ====================================================================
        // PAGE 1: LIVE MEASUREMENT DASHBOARD + FIELD RADAR + MINI LEDGER
        // ====================================================================
        private Panel BuildMeasureDashboardPage()
        {
            var page = new Panel { Dock = DockStyle.Fill, BackColor = Palette.AppBackground };

            _measureGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4
            };
            _measureGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63f));
            _measureGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37f));
            _measureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            _measureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            _measureGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _measureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));

            // Row 0: Status Chips + Best Mark Chip
            var topStatusStrip = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
            _badgeSerialHeader = new BadgeStatusIndicator { Location = new Point(0, 2), Size = new Size(245, 30) };
            _badgeCalibrationHeader = new BadgeStatusIndicator { Location = new Point(255, 2), Size = new Size(275, 30) };
            _lblBestMeasureChip = new Label
            {
                Text = "Miglior Misura: --,-- m",
                Font = Palette.UiFont(9.5f, FontStyle.Bold),
                ForeColor = Palette.EmeraldText,
                AutoSize = false,
                Size = new Size(240, 30),
                Location = new Point(540, 2),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            topStatusStrip.Controls.Add(_badgeSerialHeader);
            topStatusStrip.Controls.Add(_badgeCalibrationHeader);
            topStatusStrip.Controls.Add(_lblBestMeasureChip);
            topStatusStrip.Resize += (s, e) =>
            {
                _lblBestMeasureChip.Location = new Point(Math.Max(535, topStatusStrip.Width - 245), 2);
            };
            _measureGrid.Controls.Add(topStatusStrip, 0, 0);
            _measureGrid.SetColumnSpan(topStatusStrip, 2);

            // Row 1: Calibration warning banner
            _bannerUncalibrated = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                SurfaceFill = Palette.AmberTint,
                BorderStroke = Palette.AmberBorder,
                RadiusPx = 9,
                Margin = new Padding(0, 0, 0, 8)
            };
            _lblBannerWarnText = new Label
            {
                Text = I18n.T("Live.UncalibratedBanner"),
                Font = Palette.UiFont(9f, FontStyle.Bold),
                ForeColor = Palette.AmberText,
                Location = new Point(14, 10),
                AutoSize = true
            };
            _btnBannerOpenSetup = new ActionPillButton
            {
                Text = I18n.T("Live.OpenSetupBtn"),
                Glyph = VectorGlyph.Sliders,
                Size = new Size(168, 28),
                BaseFill = Color.White,
                HoverFill = Palette.AmberTint,
                OutlineColor = Palette.AmberBorder,
                ForeColor = Palette.AmberText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnBannerOpenSetup.Click += (s, e) => { _headerNav.ActiveTabIndex = 2; };
            _bannerUncalibrated.Controls.Add(_lblBannerWarnText);
            _bannerUncalibrated.Controls.Add(_btnBannerOpenSetup);
            _bannerUncalibrated.Resize += (s, e) =>
            {
                _btnBannerOpenSetup.Location = new Point(_bannerUncalibrated.Width - _btnBannerOpenSetup.Width - 10, (_bannerUncalibrated.Height - _btnBannerOpenSetup.Height) / 2);
            };
            _measureGrid.Controls.Add(_bannerUncalibrated, 0, 1);
            _measureGrid.SetColumnSpan(_bannerUncalibrated, 2);

            // Row 2, Col 0: Hero Scoreboard + Telemetry Subbar
            _heroScoreboardCard = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 10)
            };

            var heroLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(22, 16, 22, 14)
            };
            heroLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));

            _lblDisciplineBadge = new Label
            {
                Text = "SHOT PUT  •  OFFSET -1068 MM",
                Font = Palette.UiFont(9.5f, FontStyle.Bold),
                ForeColor = Palette.BrandBlue,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblHeroMark = new Label
            {
                Text = "--,-- m",
                Font = Palette.UiFont(66f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblDiagnosticSubline = new Label
            {
                Text = I18n.T("Live.ReadySubline"),
                Font = Palette.UiFont(9.2f, FontStyle.Regular),
                ForeColor = Palette.InkSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblTelemetryStrip = new Label
            {
                Text = I18n.T("Live.TelemetryWaiting"),
                Font = Palette.MonoFont(8.5f, FontStyle.Regular),
                ForeColor = Palette.InkTertiary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            heroLayout.Controls.Add(_lblDisciplineBadge, 0, 0);
            heroLayout.Controls.Add(_lblHeroMark, 0, 1);
            heroLayout.Controls.Add(_lblDiagnosticSubline, 0, 2);
            heroLayout.Controls.Add(_lblTelemetryStrip, 0, 3);
            _heroScoreboardCard.Controls.Add(heroLayout);
            _heroScoreboardCard.Resize += (s, e) => AdjustHeroMarkTypography();
            _measureGrid.Controls.Add(_heroScoreboardCard, 0, 2);

            // Row 2, Col 1: Split Right Column (Top: 2D Field Radar, Bottom: Mini Attempt Ledger)
            var rightSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 10)
            };
            rightSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));
            rightSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));

            var radarCard = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 8)
            };
            _lblRadarHdr = new Label
            {
                Text = I18n.T("Live.RadarHeader"),
                Font = Palette.UiFont(8.8f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Dock = DockStyle.Top,
                Height = 20
            };
            _radarCanvas = new FieldGeometryRadarCanvas { Dock = DockStyle.Fill };
            radarCard.Controls.Add(_radarCanvas);
            radarCard.Controls.Add(_lblRadarHdr);

            var miniLogCard = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };
            _lblMiniLogHdr = new Label
            {
                Text = I18n.T("Live.MiniLogHeader"),
                Font = Palette.UiFont(8.8f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Dock = DockStyle.Top,
                Height = 22
            };
            _miniAttemptLedger = new AttemptLedgerView
            {
                Dock = DockStyle.Fill,
                CompactMode = true
            };
            miniLogCard.Controls.Add(_miniAttemptLedger);
            miniLogCard.Controls.Add(_lblMiniLogHdr);

            rightSplit.Controls.Add(radarCard, 0, 0);
            rightSplit.Controls.Add(miniLogCard, 0, 1);
            _measureGrid.Controls.Add(rightSplit, 1, 2);

            // Row 3: Action Bar
            var bottomActions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            bottomActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
            bottomActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            bottomActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));

            _btnRecordFoul = new ActionPillButton
            {
                Text = I18n.T("Live.FoulBtn"),
                Glyph = VectorGlyph.Ban,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Font = Palette.UiFont(10.5f, FontStyle.Bold),
                BaseFill = Palette.CrimsonTint,
                HoverFill = Palette.CrimsonHover,
                OutlineColor = Palette.CrimsonBorder,
                ForeColor = Palette.CrimsonText
            };
            _btnRecordFoul.Click += (s, e) => RecordFoulAttempt();

            _btnInjectTestShot = new ActionPillButton
            {
                Text = I18n.T("Live.SimulateBtn"),
                Glyph = VectorGlyph.Bolt,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Font = Palette.UiFont(9.5f, FontStyle.Bold),
                BaseFill = Palette.CardSurface,
                HoverFill = Palette.BrandBlueTint,
                OutlineColor = Palette.HairlineBorder,
                ForeColor = Palette.InkSecondary
            };
            _btnInjectTestShot.Click += OnSimulateReadingButtonClicked;

            _btnCopyLastMark = new ActionPillButton
            {
                Text = I18n.T("Live.CopyBtn"),
                Glyph = VectorGlyph.CopyClipboard,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Font = Palette.UiFont(9.5f, FontStyle.Bold),
                BaseFill = Palette.CardSurface,
                HoverFill = Palette.MutedWellHover,
                OutlineColor = Palette.HairlineBorder,
                ForeColor = Palette.InkSecondary
            };
            _btnCopyLastMark.Click += (s, e) => CopyHeroMeasureToClipboard();

            bottomActions.Controls.Add(_btnRecordFoul, 0, 0);
            bottomActions.Controls.Add(_btnInjectTestShot, 1, 0);
            bottomActions.Controls.Add(_btnCopyLastMark, 2, 0);
            _measureGrid.Controls.Add(bottomActions, 0, 3);
            _measureGrid.SetColumnSpan(bottomActions, 2);

            page.Controls.Add(_measureGrid);
            return page;
        }

        private void AdjustHeroMarkTypography()
        {
            if (_heroScoreboardCard == null || _lblHeroMark == null) return;
            float targetPt = Math.Max(42f, Math.Min(86f, Math.Min(_heroScoreboardCard.Width * 0.115f, _heroScoreboardCard.Height * 0.25f)));
            if (Math.Abs(_lblHeroMark.Font.Size - targetPt) > 1.5f)
            {
                Font old = _lblHeroMark.Font;
                _lblHeroMark.Font = Palette.UiFont(targetPt, FontStyle.Bold);
                old.Dispose();
            }
        }

        // ====================================================================
        // PAGE 2: FULL ATTEMPT LEDGER + EXPORT / CLIPBOARD
        // ====================================================================
        private Panel BuildHistoryLedgerPage()
        {
            var page = new Panel { Dock = DockStyle.Fill, BackColor = Palette.AppBackground };

            var card = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22)
            };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 52 };
            _lblHistoryTitle = new Label
            {
                Text = I18n.T("History.Title"),
                Font = Palette.UiFont(13f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(0, 2),
                AutoSize = true
            };
            _lblHistoryStats = new Label
            {
                Text = I18n.Format("History.StatsFormat", 0, "--,-- m"),
                Font = Palette.UiFont(9f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(0, 27),
                AutoSize = true
            };

            _btnExportCsv = new ActionPillButton
            {
                Text = I18n.T("History.ExportCsvBtn"),
                Glyph = VectorGlyph.DownloadCsv,
                Size = new Size(138, 36),
                BaseFill = Palette.BrandBlueTint,
                HoverFill = Palette.BrandBlueHover,
                OutlineColor = Palette.BrandBlueBorder,
                ForeColor = Palette.BrandBlue,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnExportCsv.Click += (s, e) => ExportHistoryToCsv();

            _btnCopyAll = new ActionPillButton
            {
                Text = I18n.T("History.CopyAllBtn"),
                Glyph = VectorGlyph.CopyClipboard,
                Size = new Size(132, 36),
                BaseFill = Palette.MutedWell,
                HoverFill = Palette.MutedWellHover,
                OutlineColor = Palette.HairlineBorder,
                ForeColor = Palette.InkSecondary,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnCopyAll.Click += (s, e) => CopyAllHistoryToClipboard();

            _btnClearLedger = new ActionPillButton
            {
                Text = I18n.T("History.ClearBtn"),
                Glyph = VectorGlyph.TrashCan,
                Size = new Size(142, 36),
                BaseFill = Palette.CrimsonTint,
                HoverFill = Palette.CrimsonHover,
                OutlineColor = Palette.CrimsonBorder,
                ForeColor = Palette.CrimsonText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClearLedger.Click += (s, e) =>
            {
                if (_fullAttemptLedger.RecordCount == 0) return;
                if (MessageBox.Show(
                        this,
                        I18n.T("History.ConfirmClearText"),
                        I18n.T("History.ConfirmClearTitle"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    WorkspacePersistenceService.ClearSavedLedger();
                    _miniAttemptLedger.ClearAll();
                    _fullAttemptLedger.ClearAll();
                    RefreshLedgerCounters();
                }
            };

            topBar.Controls.Add(_lblHistoryTitle);
            topBar.Controls.Add(_lblHistoryStats);
            topBar.Controls.Add(_btnExportCsv);
            topBar.Controls.Add(_btnCopyAll);
            topBar.Controls.Add(_btnClearLedger);
            topBar.Resize += (s, e) =>
            {
                _btnClearLedger.Location = new Point(topBar.Width - _btnClearLedger.Width, 6);
                _btnCopyAll.Location = new Point(_btnClearLedger.Left - _btnCopyAll.Width - 8, 6);
                _btnExportCsv.Location = new Point(_btnCopyAll.Left - _btnExportCsv.Width - 8, 6);
            };

            var ledgerWrapper = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                SurfaceFill = Palette.MutedWell,
                BorderStroke = Palette.HairlineBorder,
                RadiusPx = 10,
                Padding = new Padding(6)
            };
            _fullAttemptLedger = new AttemptLedgerView
            {
                Dock = DockStyle.Fill,
                CompactMode = false
            };
            ledgerWrapper.Controls.Add(_fullAttemptLedger);

            card.Controls.Add(ledgerWrapper);
            card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
            card.Controls.Add(topBar);

            page.Controls.Add(card);
            return page;
        }

        // ====================================================================
        // PAGE 3: CALIBRATION & DISCIPLINE SETUP (INLINE + MODAL SUPPORT)
        // ====================================================================
        private Panel BuildCalibrationSetupPage()
        {
            var page = new Panel { Dock = DockStyle.Fill, BackColor = Palette.AppBackground };

            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

            // Left Card: Discipline & Offset
            var cardLeft = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(22)
            };

            _lblSetupLeftTitle = new Label
            {
                Text = I18n.T("Setup.LeftTitle"),
                Font = Palette.UiFont(12.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(22, 18),
                AutoSize = true
            };
            cardLeft.Controls.Add(_lblSetupLeftTitle);

            _lblSetupLeftSub = new Label
            {
                Text = I18n.T("Setup.LeftSubtitle"),
                Font = Palette.UiFont(8.8f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 46),
                Size = new Size(410, 34)
            };
            cardLeft.Controls.Add(_lblSetupLeftSub);

            _lblLanguageCap = new Label
            {
                Text = I18n.T("Setup.LanguageCaption"),
                Font = Palette.UiFont(9f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 86),
                AutoSize = true
            };
            cardLeft.Controls.Add(_lblLanguageCap);

            _cboLanguage = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Palette.UiFont(10f),
                Location = new Point(22, 108),
                Size = new Size(390, 30)
            };
            foreach (LanguagePack pack in I18n.AvailableLanguages)
            {
                _cboLanguage.Items.Add(pack);
            }
            _cboLanguage.SelectedItem = I18n.CurrentLanguage;
            _cboLanguage.SelectedIndexChanged += OnLanguageComboChanged;
            cardLeft.Controls.Add(_cboLanguage);

            _lblDiscCap = new Label
            {
                Text = I18n.T("Setup.DisciplineCaption"),
                Font = Palette.UiFont(9f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 148),
                AutoSize = true
            };
            cardLeft.Controls.Add(_lblDiscCap);

            _cboDiscipline = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Palette.UiFont(10f),
                Location = new Point(22, 170),
                Size = new Size(390, 30)
            };
            for (int i = 0; i < DisciplineSpec.Catalog.Length; i++)
            {
                _cboDiscipline.Items.Add(DisciplineSpec.Catalog[i]);
            }
            _cboDiscipline.SelectedIndex = 0;
            _cboDiscipline.SelectedIndexChanged += OnDisciplineComboChanged;
            cardLeft.Controls.Add(_cboDiscipline);

            _lblOffsetCap = new Label
            {
                Text = I18n.T("Setup.OffsetCaption"),
                Font = Palette.UiFont(9f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 210),
                AutoSize = true
            };
            cardLeft.Controls.Add(_lblOffsetCap);

            _offsetStepper = new MillimeterOffsetStepper
            {
                Location = new Point(22, 232),
                Size = new Size(390, 36)
            };
            _offsetStepper.ValueChanged += (s, e) => RefreshHeaderAndHeroLabels();
            cardLeft.Controls.Add(_offsetStepper);

            // Quick preset pills
            var presetPanel = new FlowLayoutPanel
            {
                Location = new Point(22, 276),
                Size = new Size(395, 34),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            _btnPresetShotHammer = BuildOffsetPresetChip(I18n.T("Setup.PresetShotHammer"), 1068);
            _btnPresetDiscus = BuildOffsetPresetChip(I18n.T("Setup.PresetDiscus"), 1250);
            _btnPresetJavelin = BuildOffsetPresetChip(I18n.T("Setup.PresetJavelin"), 8000);
            _btnPresetJumps = BuildOffsetPresetChip(I18n.T("Setup.PresetJumps"), 0);
            presetPanel.Controls.Add(_btnPresetShotHammer);
            presetPanel.Controls.Add(_btnPresetDiscus);
            presetPanel.Controls.Add(_btnPresetJavelin);
            presetPanel.Controls.Add(_btnPresetJumps);
            cardLeft.Controls.Add(presetPanel);

            var lockCard = new RoundedCardPanel
            {
                Location = new Point(22, 318),
                Size = new Size(390, 50),
                SurfaceFill = Palette.MutedWell,
                BorderStroke = Palette.HairlineBorder,
                RadiusPx = 9
            };
            _chkLockCalibration = new CheckBox
            {
                Text = I18n.T("Setup.LockCheckbox"),
                Font = Palette.UiFont(9.2f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(14, 14),
                AutoSize = true
            };
            _chkLockCalibration.CheckedChanged += (s, e) => UpdateSetupControlsLockState();
            lockCard.Controls.Add(_chkLockCalibration);
            cardLeft.Controls.Add(lockCard);

            _lblSetupLivePreview = new Label
            {
                Text = I18n.T("Setup.LivePreviewEmpty"),
                Font = Palette.MonoFont(8.5f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 378),
                Size = new Size(390, 44)
            };
            cardLeft.Controls.Add(_lblSetupLivePreview);

            cardLeft.Resize += (s, e) =>
            {
                int w = Math.Max(260, cardLeft.Width - 44);
                _lblSetupLeftSub.Width = w;
                _cboLanguage.Width = w;
                _cboDiscipline.Width = w;
                _offsetStepper.Width = w;
                presetPanel.Width = w;
                lockCard.Width = w;
                _lblSetupLivePreview.Width = w;
            };

            // Right Card: Reference Point Calibration (P1 & P2)
            var cardRight = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 0, 0, 0),
                Padding = new Padding(22)
            };

            _lblSetupRightTitle = new Label
            {
                Text = I18n.T("Setup.RightTitle"),
                Font = Palette.UiFont(12.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(22, 18),
                AutoSize = true
            };
            cardRight.Controls.Add(_lblSetupRightTitle);

            _lblSetupRightSub = new Label
            {
                Text = I18n.T("Setup.RightSubtitle"),
                Font = Palette.UiFont(8.8f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 46),
                Size = new Size(430, 34)
            };
            cardRight.Controls.Add(_lblSetupRightSub);

            // Point 1 Box
            var boxP1 = new RoundedCardPanel
            {
                Location = new Point(22, 88),
                Size = new Size(430, 118),
                SurfaceFill = Palette.MutedWell,
                BorderStroke = Palette.HairlineBorder,
                RadiusPx = 10
            };
            _lblP1Hdr = new Label
            {
                Text = I18n.T("Setup.P1Header"),
                Font = Palette.UiFont(9.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(14, 12),
                AutoSize = true
            };
            _badgeP1Status = new BadgeStatusIndicator
            {
                Location = new Point(285, 9),
                Size = new Size(132, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _lblP1Coordinates = new Label
            {
                Text = I18n.T("Setup.P1EmptyText"),
                Font = Palette.MonoFont(8.8f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(14, 40),
                Size = new Size(400, 26)
            };
            _btnAcquireP1Modal = new ActionPillButton
            {
                Text = I18n.T("Setup.AcquireP1Btn"),
                Glyph = VectorGlyph.Crosshair,
                Location = new Point(14, 72),
                Size = new Size(195, 34),
                BaseFill = Palette.BrandBlue,
                HoverFill = Color.FromArgb(29, 78, 216),
                OutlineColor = Color.Transparent,
                ForeColor = Color.White
            };
            _btnAcquireP1Modal.Click += (s, e) => OpenGuidedPointCapture(1);

            _btnUseLastAsP1 = new ActionPillButton
            {
                Text = I18n.T("Setup.UseLastReadingBtn"),
                Glyph = VectorGlyph.CheckCircle,
                Location = new Point(219, 72),
                Size = new Size(195, 34),
                BaseFill = Color.White,
                HoverFill = Palette.BrandBlueTint,
                OutlineColor = Palette.BrandBlueBorder,
                ForeColor = Palette.BrandBlue
            };
            _btnUseLastAsP1.Click += (s, e) => AssignLastReadingDirectly(1);

            boxP1.Controls.Add(_lblP1Hdr);
            boxP1.Controls.Add(_badgeP1Status);
            boxP1.Controls.Add(_lblP1Coordinates);
            boxP1.Controls.Add(_btnAcquireP1Modal);
            boxP1.Controls.Add(_btnUseLastAsP1);
            cardRight.Controls.Add(boxP1);

            // Point 2 Box
            var boxP2 = new RoundedCardPanel
            {
                Location = new Point(22, 216),
                Size = new Size(430, 118),
                SurfaceFill = Palette.MutedWell,
                BorderStroke = Palette.HairlineBorder,
                RadiusPx = 10
            };
            _lblP2Hdr = new Label
            {
                Text = I18n.T("Setup.P2Header"),
                Font = Palette.UiFont(9.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(14, 12),
                AutoSize = true
            };
            _badgeP2Status = new BadgeStatusIndicator
            {
                Location = new Point(285, 9),
                Size = new Size(132, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _lblP2Coordinates = new Label
            {
                Text = I18n.T("Setup.P2NotRequiredText"),
                Font = Palette.MonoFont(8.8f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(14, 40),
                Size = new Size(400, 26)
            };
            _btnAcquireP2Modal = new ActionPillButton
            {
                Text = I18n.T("Setup.AcquireP2Btn"),
                Glyph = VectorGlyph.Crosshair,
                Location = new Point(14, 72),
                Size = new Size(195, 34),
                BaseFill = Palette.BrandBlue,
                HoverFill = Color.FromArgb(29, 78, 216),
                OutlineColor = Color.Transparent,
                ForeColor = Color.White
            };
            _btnAcquireP2Modal.Click += (s, e) => OpenGuidedPointCapture(2);

            _btnUseLastAsP2 = new ActionPillButton
            {
                Text = I18n.T("Setup.UseLastReadingBtn"),
                Glyph = VectorGlyph.CheckCircle,
                Location = new Point(219, 72),
                Size = new Size(195, 34),
                BaseFill = Color.White,
                HoverFill = Palette.BrandBlueTint,
                OutlineColor = Palette.BrandBlueBorder,
                ForeColor = Palette.BrandBlue
            };
            _btnUseLastAsP2.Click += (s, e) => AssignLastReadingDirectly(2);

            boxP2.Controls.Add(_lblP2Hdr);
            boxP2.Controls.Add(_badgeP2Status);
            boxP2.Controls.Add(_lblP2Coordinates);
            boxP2.Controls.Add(_btnAcquireP2Modal);
            boxP2.Controls.Add(_btnUseLastAsP2);
            cardRight.Controls.Add(boxP2);

            _lblBaselineSpanInfo = new Label
            {
                Text = string.Empty,
                Font = Palette.UiFont(8.8f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 344),
                Size = new Size(430, 22)
            };
            cardRight.Controls.Add(_lblBaselineSpanInfo);

            _btnSwapP1P2 = new ActionPillButton
            {
                Text = I18n.T("Setup.SwapBtn"),
                Glyph = VectorGlyph.SwapArrows,
                Location = new Point(22, 374),
                Size = new Size(208, 36),
                BaseFill = Palette.MutedWell,
                HoverFill = Palette.MutedWellHover,
                OutlineColor = Palette.HairlineBorder,
                ForeColor = Palette.InkSecondary
            };
            _btnSwapP1P2.Click += (s, e) => SwapCalibrationAnchors();
            cardRight.Controls.Add(_btnSwapP1P2);

            _btnClearCalibration = new ActionPillButton
            {
                Text = I18n.T("Setup.ClearCalibrationBtn"),
                Glyph = VectorGlyph.TrashCan,
                Location = new Point(242, 374),
                Size = new Size(208, 36),
                BaseFill = Palette.CrimsonTint,
                HoverFill = Palette.CrimsonHover,
                OutlineColor = Palette.CrimsonBorder,
                ForeColor = Palette.CrimsonText
            };
            _btnClearCalibration.Click += (s, e) =>
            {
                _anchor1 = null;
                _anchor2 = null;
                _baselineProjector = null;
                RefreshCalibrationWidgets();
                PersistCurrentWorkspaceState();
            };
            cardRight.Controls.Add(_btnClearCalibration);

            cardRight.Resize += (s, e) =>
            {
                int w = Math.Max(300, cardRight.Width - 44);
                _lblSetupRightSub.Width = w;
                boxP1.Width = w;
                boxP2.Width = w;
                _lblBaselineSpanInfo.Width = w;
                int half = (w - 12) / 2;
                _btnSwapP1P2.Width = half;
                _btnClearCalibration.Location = new Point(22 + half + 12, 374);
                _btnClearCalibration.Width = w - half - 12;
            };

            split.Controls.Add(cardLeft, 0, 0);
            split.Controls.Add(cardRight, 1, 0);
            page.Controls.Add(split);
            return page;
        }

        private ActionPillButton BuildOffsetPresetChip(string label, int mm)
        {
            var btn = new ActionPillButton
            {
                Text = label,
                Size = new Size(92, 28),
                Margin = new Padding(0, 0, 6, 0),
                CornerRadius = 6,
                Font = Palette.UiFont(8f, FontStyle.Bold),
                BaseFill = Palette.BrandBlueTint,
                HoverFill = Palette.BrandBlueHover,
                OutlineColor = Palette.BrandBlueBorder,
                ForeColor = Palette.BrandBlue
            };
            btn.Click += (s, e) =>
            {
                if (!_chkLockCalibration.Checked)
                {
                    _offsetStepper.ValueMillimeters = mm;
                }
            };
            return btn;
        }

        // ====================================================================
        // PAGE 4: SERIAL PORT & MULTI-PROTOCOL TELEMETRY INSPECTOR
        // ====================================================================
        private Panel BuildSerialTelemetryPage()
        {
            var page = new Panel { Dock = DockStyle.Fill, BackColor = Palette.AppBackground };

            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

            // Left Card: RS-232 / USB-Serial Parameters
            var cardSerial = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(22)
            };

            _lblSerialTitle = new Label
            {
                Text = I18n.T("Serial.LeftTitle"),
                Font = Palette.UiFont(12.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(22, 18),
                AutoSize = true
            };
            cardSerial.Controls.Add(_lblSerialTitle);

            _lblSerialSub = new Label
            {
                Text = I18n.T("Serial.LeftSubtitle"),
                Font = Palette.UiFont(8.8f),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 46),
                Size = new Size(400, 32)
            };
            cardSerial.Controls.Add(_lblSerialSub);

            _cboComPort = AddLabeledDropdown(cardSerial, I18n.T("Serial.ComPortLabel"), SerialLinkController.DiscoverSystemPorts(), 88, out _lblComPortCaption);
            _cboBaudRate = AddLabeledDropdown(cardSerial, I18n.T("Serial.BaudRateLabel"), new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 }, 132, out _lblBaudRateCaption);
            _cboDataBits = AddLabeledDropdown(cardSerial, I18n.T("Serial.DataBitsLabel"), new object[] { 7, 8 }, 176, out _lblDataBitsCaption);
            _cboParity = AddLabeledDropdown(
                cardSerial,
                I18n.T("Serial.ParityLabel"),
                new object[] { I18n.T("Serial.ParityNone"), I18n.T("Serial.ParityOdd"), I18n.T("Serial.ParityEven") },
                220,
                out _lblParityCaption);
            _cboStopBits = AddLabeledDropdown(cardSerial, I18n.T("Serial.StopBitsLabel"), new object[] { "1 bit", "2 bit", "1.5 bit" }, 264, out _lblStopBitsCaption);
            _cboProtocolMode = AddLabeledDropdown(
                cardSerial,
                I18n.T("Serial.ProtocolLabel"),
                new object[]
                {
                    I18n.T("Serial.ProtoAuto"),
                    I18n.T("Serial.ProtoMato"),
                    I18n.T("Serial.ProtoSokkia"),
                    I18n.T("Serial.ProtoGsi"),
                    I18n.T("Serial.ProtoNikon")
                },
                308,
                out _lblProtocolCaption);

            _cboBaudRate.SelectedItem = 9600;
            _cboDataBits.SelectedItem = 8;
            _cboParity.SelectedIndex = 0;
            _cboStopBits.SelectedIndex = 0;
            _cboProtocolMode.SelectedIndex = 0;

            _btnApplySerial = new ActionPillButton
            {
                Text = I18n.T("Serial.ApplyReconnectBtn"),
                Glyph = VectorGlyph.SerialPort,
                Location = new Point(22, 362),
                Size = new Size(270, 38),
                BaseFill = Palette.BrandBlue,
                HoverFill = Color.FromArgb(29, 78, 216),
                OutlineColor = Color.Transparent,
                ForeColor = Color.White
            };
            _btnApplySerial.Click += (s, e) =>
            {
                RefreshPortDropdownPreserveSelection();
                ApplyAndConnectSerial(forceReopen: true);
            };
            cardSerial.Controls.Add(_btnApplySerial);

            _btnRefreshPorts = new ActionPillButton
            {
                Text = I18n.T("Serial.RefreshPortsBtn"),
                Location = new Point(302, 362),
                Size = new Size(120, 38),
                BaseFill = Palette.MutedWell,
                HoverFill = Palette.MutedWellHover,
                OutlineColor = Palette.HairlineBorder,
                ForeColor = Palette.InkSecondary
            };
            _btnRefreshPorts.Click += (s, e) => RefreshPortDropdownPreserveSelection();
            cardSerial.Controls.Add(_btnRefreshPorts);

            // Right Card: Live Telemetry Monitor & Frame Tester
            var cardMonitor = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 0, 0, 0),
                Padding = new Padding(22)
            };

            _lblMonTitle = new Label
            {
                Text = I18n.T("Serial.RightTitle"),
                Font = Palette.UiFont(12.5f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(22, 18),
                AutoSize = true
            };
            cardMonitor.Controls.Add(_lblMonTitle);

            _badgeSerialDetail = new BadgeStatusIndicator
            {
                Location = new Point(22, 52),
                Size = new Size(420, 30)
            };
            cardMonitor.Controls.Add(_badgeSerialDetail);

            _lblSerialEndpointSummary = new Label
            {
                Text = string.Empty,
                Font = Palette.MonoFont(9f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, 92),
                Size = new Size(420, 22)
            };
            cardMonitor.Controls.Add(_lblSerialEndpointSummary);

            var rawBoxCard = new RoundedCardPanel
            {
                Location = new Point(22, 120),
                Size = new Size(420, 120),
                SurfaceFill = Palette.MutedWell,
                BorderStroke = Palette.HairlineBorder,
                RadiusPx = 9,
                Padding = new Padding(12)
            };
            _lblRawHdr = new Label
            {
                Text = I18n.T("Serial.LastPacketHeader"),
                Font = Palette.UiFont(8.5f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Dock = DockStyle.Top,
                Height = 20
            };
            _lblLastRawPacketBox = new Label
            {
                Text = I18n.T("Serial.WaitingSerialData"),
                Font = Palette.MonoFont(8.8f),
                ForeColor = Palette.InkPrimary,
                Dock = DockStyle.Fill
            };
            rawBoxCard.Controls.Add(_lblLastRawPacketBox);
            rawBoxCard.Controls.Add(_lblRawHdr);
            cardMonitor.Controls.Add(rawBoxCard);

            // Frame Simulator / Hardware Verification Box
            _lblSimTitle = new Label
            {
                Text = I18n.T("Serial.SimulatorTitle"),
                Font = Palette.UiFont(9f, FontStyle.Bold),
                ForeColor = Palette.InkPrimary,
                Location = new Point(22, 254),
                AutoSize = true
            };
            cardMonitor.Controls.Add(_lblSimTitle);

            _txtCustomFrameInput = new TextBox
            {
                Font = Palette.MonoFont(9.5f),
                Location = new Point(22, 278),
                Size = new Size(295, 28),
                Text = "000555309758030525604"
            };
            cardMonitor.Controls.Add(_txtCustomFrameInput);

            _btnInjectRaw = new ActionPillButton
            {
                Text = I18n.T("Serial.InjectBtn"),
                Glyph = VectorGlyph.Bolt,
                Location = new Point(325, 276),
                Size = new Size(117, 30),
                BaseFill = Palette.BrandBlueTint,
                HoverFill = Palette.BrandBlueHover,
                OutlineColor = Palette.BrandBlueBorder,
                ForeColor = Palette.BrandBlue
            };
            _btnInjectRaw.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(_txtCustomFrameInput.Text))
                {
                    _serialController.InjectSyntheticFrame(_txtCustomFrameInput.Text);
                }
            };
            cardMonitor.Controls.Add(_btnInjectRaw);

            _btnPresetMatoP1 = new ActionPillButton
            {
                Text = I18n.T("Serial.TestMatoP1Btn"),
                Location = new Point(22, 316),
                Size = new Size(135, 30),
                CornerRadius = 6,
                Font = Palette.UiFont(8f, FontStyle.Bold)
            };
            _btnPresetMatoP1.Click += (s, e) =>
            {
                _txtCustomFrameInput.Text = "000253210728482681220";
                _serialController.InjectSyntheticFrame(_txtCustomFrameInput.Text);
            };

            _btnPresetMatoShot = new ActionPillButton
            {
                Text = I18n.T("Serial.TestMatoShotBtn"),
                Location = new Point(163, 316),
                Size = new Size(145, 30),
                CornerRadius = 6,
                Font = Palette.UiFont(8f, FontStyle.Bold)
            };
            _btnPresetMatoShot.Click += (s, e) =>
            {
                _txtCustomFrameInput.Text = "000555309758030525604";
                _serialController.InjectSyntheticFrame(_txtCustomFrameInput.Text);
            };

            _btnPresetSet23 = new ActionPillButton
            {
                Text = I18n.T("Serial.TestSokkiaBtn"),
                Location = new Point(314, 316),
                Size = new Size(128, 30),
                CornerRadius = 6,
                Font = Palette.UiFont(8f, FontStyle.Bold)
            };
            _btnPresetSet23.Click += (s, e) =>
            {
                _txtCustomFrameInput.Text = "00184500100000000452300";
                _serialController.InjectSyntheticFrame(_txtCustomFrameInput.Text);
            };

            cardMonitor.Controls.Add(_btnPresetMatoP1);
            cardMonitor.Controls.Add(_btnPresetMatoShot);
            cardMonitor.Controls.Add(_btnPresetSet23);

            split.Controls.Add(cardSerial, 0, 0);
            split.Controls.Add(cardMonitor, 1, 0);
            page.Controls.Add(split);
            return page;
        }

        private static ComboBox AddLabeledDropdown(Control container, string caption, object[] values, int topY, out Label createdLabel)
        {
            createdLabel = new Label
            {
                Text = caption,
                Font = Palette.UiFont(9f, FontStyle.Bold),
                ForeColor = Palette.InkSecondary,
                Location = new Point(22, topY + 4),
                Size = new Size(150, 24)
            };
            var cbo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Palette.UiFont(9.5f),
                Location = new Point(180, topY),
                Size = new Size(242, 28)
            };
            cbo.Items.AddRange(values);
            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
            container.Controls.Add(createdLabel);
            container.Controls.Add(cbo);
            return cbo;
        }

        // ====================================================================
        // NAVIGATION & LIFECYCLE (SERIAL STAYS OPEN ACROSS ALL PAGES)
        // ====================================================================
        private void SwitchVisiblePage(int pageIndex)
        {
            _pageMeasure.Visible = (pageIndex == 0);
            _pageHistory.Visible = (pageIndex == 1);
            _pageSetup.Visible = (pageIndex == 2);
            _pageSerial.Visible = (pageIndex == 3);

            if (pageIndex == 3)
            {
                RefreshPortDropdownPreserveSelection();
                UpdateSerialSummaryLabels();
            }
        }

        private void OnMainWindowShown(object sender, EventArgs e)
        {
            RestoreSavedWorkspaceState();
            LoadSavedHistoryLedger();
            ApplyAndConnectSerial(forceReopen: false);
        }

        private void OnMainWindowClosing(object sender, FormClosingEventArgs e)
        {
            I18n.LanguageChanged -= OnApplicationLanguageChanged;
            PersistCurrentWorkspaceState();
            _serialController.Dispose();
        }

        private void OnMainWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                RecordFoulAttempt();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                OnSimulateReadingButtonClicked(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.C && _headerNav.ActiveTabIndex == 0)
            {
                CopyHeroMeasureToClipboard();
                e.Handled = true;
            }
        }

        private void OnLanguageComboChanged(object sender, EventArgs e)
        {
            if (_suppressLanguageChangeHandler) return;
            LanguagePack selected = _cboLanguage.SelectedItem as LanguagePack;
            if (selected != null)
            {
                I18n.SetLanguage(selected.Code);
                PersistCurrentWorkspaceState();
            }
        }

        private void OnApplicationLanguageChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnApplicationLanguageChanged));
                return;
            }
            ApplyLocalizationToUi();
            PersistCurrentWorkspaceState();
        }

        private void ApplyLocalizationToUi()
        {
            Text = I18n.T("App.WindowTitle");
            _headerNav.RefreshLocalizedCaptions();

            _suppressLanguageChangeHandler = true;
            _cboLanguage.SelectedItem = I18n.CurrentLanguage;
            _suppressLanguageChangeHandler = false;

            // Refresh discipline combo display text while keeping selection
            int discIdx = _cboDiscipline.SelectedIndex >= 0 ? _cboDiscipline.SelectedIndex : 0;
            _suppressDisciplineChangeHandler = true;
            _cboDiscipline.BeginUpdate();
            _cboDiscipline.Items.Clear();
            for (int i = 0; i < DisciplineSpec.Catalog.Length; i++)
            {
                _cboDiscipline.Items.Add(DisciplineSpec.Catalog[i]);
            }
            _cboDiscipline.SelectedIndex = discIdx;
            _cboDiscipline.EndUpdate();
            _suppressDisciplineChangeHandler = false;

            // Live Measure Page
            _lblBannerWarnText.Text = I18n.T("Live.UncalibratedBanner");
            _btnBannerOpenSetup.Text = I18n.T("Live.OpenSetupBtn");
            _lblRadarHdr.Text = I18n.T("Live.RadarHeader");
            _lblMiniLogHdr.Text = I18n.T("Live.MiniLogHeader");
            _btnRecordFoul.Text = I18n.T("Live.FoulBtn");
            _btnInjectTestShot.Text = I18n.T("Live.SimulateBtn");
            _btnCopyLastMark.Text = I18n.T("Live.CopyBtn");

            if (_lastReceivedReading == null)
            {
                _lblDiagnosticSubline.Text = I18n.T("Live.ReadySubline");
                _lblTelemetryStrip.Text = I18n.T("Live.TelemetryWaiting");
                _lblSetupLivePreview.Text = I18n.T("Setup.LivePreviewEmpty");
            }
            else
            {
                string telemetryInfo = FormatTelemetryLine(_lastReceivedReading);
                _lblSetupLivePreview.Text = I18n.Format("Setup.LivePreviewReady", telemetryInfo);
                if (!IsCalibrationReady)
                {
                    _lblDiagnosticSubline.Text = I18n.T("Live.UncalibratedSubline");
                }
                else
                {
                    EvaluationOutcome eval = AthleticsMeasurementEngine.EvaluateAttempt(
                        CurrentDiscipline.GeometryKind,
                        _anchor1,
                        _anchor2,
                        _baselineProjector,
                        _lastReceivedReading,
                        _offsetStepper.ValueMillimeters);
                    if (_lblHeroMark.Text == "NULLO" || _lblHeroMark.Text == "FOUL")
                    {
                        _lblHeroMark.Text = I18n.T("Mark.Foul");
                        _lblDiagnosticSubline.Text = I18n.T("Live.FoulSubline");
                    }
                    else
                    {
                        _lblDiagnosticSubline.Text = eval.DiagnosticSummary;
                    }
                }
            }

            if (_lblHeroMark.Text == "NULLO" || _lblHeroMark.Text == "FOUL")
            {
                _lblHeroMark.Text = I18n.T("Mark.Foul");
            }

            // History Page
            _lblHistoryTitle.Text = I18n.T("History.Title");
            _btnExportCsv.Text = I18n.T("History.ExportCsvBtn");
            _btnCopyAll.Text = I18n.T("History.CopyAllBtn");
            _btnClearLedger.Text = I18n.T("History.ClearBtn");

            // Setup Page
            _lblSetupLeftTitle.Text = I18n.T("Setup.LeftTitle");
            _lblSetupLeftSub.Text = I18n.T("Setup.LeftSubtitle");
            _lblLanguageCap.Text = I18n.T("Setup.LanguageCaption");
            _lblDiscCap.Text = I18n.T("Setup.DisciplineCaption");
            _lblOffsetCap.Text = I18n.T("Setup.OffsetCaption");
            _btnPresetShotHammer.Text = I18n.T("Setup.PresetShotHammer");
            _btnPresetDiscus.Text = I18n.T("Setup.PresetDiscus");
            _btnPresetJavelin.Text = I18n.T("Setup.PresetJavelin");
            _btnPresetJumps.Text = I18n.T("Setup.PresetJumps");
            _chkLockCalibration.Text = I18n.T("Setup.LockCheckbox");

            _lblSetupRightTitle.Text = I18n.T("Setup.RightTitle");
            _lblSetupRightSub.Text = I18n.T("Setup.RightSubtitle");
            _lblP1Hdr.Text = I18n.T("Setup.P1Header");
            _lblP2Hdr.Text = I18n.T("Setup.P2Header");
            _btnAcquireP1Modal.Text = I18n.T("Setup.AcquireP1Btn");
            _btnAcquireP2Modal.Text = I18n.T("Setup.AcquireP2Btn");
            _btnUseLastAsP1.Text = I18n.T("Setup.UseLastReadingBtn");
            _btnUseLastAsP2.Text = I18n.T("Setup.UseLastReadingBtn");
            _btnSwapP1P2.Text = I18n.T("Setup.SwapBtn");
            _btnClearCalibration.Text = I18n.T("Setup.ClearCalibrationBtn");

            // Serial Page
            _lblSerialTitle.Text = I18n.T("Serial.LeftTitle");
            _lblSerialSub.Text = I18n.T("Serial.LeftSubtitle");
            _lblComPortCaption.Text = I18n.T("Serial.ComPortLabel");
            _lblBaudRateCaption.Text = I18n.T("Serial.BaudRateLabel");
            _lblDataBitsCaption.Text = I18n.T("Serial.DataBitsLabel");
            _lblParityCaption.Text = I18n.T("Serial.ParityLabel");
            _lblStopBitsCaption.Text = I18n.T("Serial.StopBitsLabel");
            _lblProtocolCaption.Text = I18n.T("Serial.ProtocolLabel");

            int parityIdx = _cboParity.SelectedIndex >= 0 ? _cboParity.SelectedIndex : 0;
            _cboParity.BeginUpdate();
            _cboParity.Items.Clear();
            _cboParity.Items.AddRange(new object[] { I18n.T("Serial.ParityNone"), I18n.T("Serial.ParityOdd"), I18n.T("Serial.ParityEven") });
            _cboParity.SelectedIndex = parityIdx;
            _cboParity.EndUpdate();

            int protoIdx = _cboProtocolMode.SelectedIndex >= 0 ? _cboProtocolMode.SelectedIndex : 0;
            _cboProtocolMode.BeginUpdate();
            _cboProtocolMode.Items.Clear();
            _cboProtocolMode.Items.AddRange(new object[]
            {
                I18n.T("Serial.ProtoAuto"),
                I18n.T("Serial.ProtoMato"),
                I18n.T("Serial.ProtoSokkia"),
                I18n.T("Serial.ProtoGsi"),
                I18n.T("Serial.ProtoNikon")
            });
            _cboProtocolMode.SelectedIndex = protoIdx;
            _cboProtocolMode.EndUpdate();

            _btnApplySerial.Text = I18n.T("Serial.ApplyReconnectBtn");
            _btnRefreshPorts.Text = I18n.T("Serial.RefreshPortsBtn");
            _lblMonTitle.Text = I18n.T("Serial.RightTitle");
            _lblRawHdr.Text = I18n.T("Serial.LastPacketHeader");
            if (_serialController.TotalPacketsReceived == 0)
            {
                _lblLastRawPacketBox.Text = I18n.T("Serial.WaitingSerialData");
            }
            _lblSimTitle.Text = I18n.T("Serial.SimulatorTitle");
            _btnInjectRaw.Text = I18n.T("Serial.InjectBtn");
            _btnPresetMatoP1.Text = I18n.T("Serial.TestMatoP1Btn");
            _btnPresetMatoShot.Text = I18n.T("Serial.TestMatoShotBtn");
            _btnPresetSet23.Text = I18n.T("Serial.TestSokkiaBtn");

            RefreshCalibrationWidgets();
            UpdateSerialSummaryLabels();
            OnSerialLinkStateChanged(_serialController.IsConnected, _lastSerialStatusMessage);
            _miniAttemptLedger.Invalidate();
            _fullAttemptLedger.Invalidate();
            RefreshLedgerCounters();
        }

        // ====================================================================
        // STATE PERSISTENCE & CALIBRATION LOGIC
        // ====================================================================
        private DisciplineSpec CurrentDiscipline
        {
            get
            {
                int idx = _cboDiscipline != null ? _cboDiscipline.SelectedIndex : 0;
                if (idx < 0 || idx >= DisciplineSpec.Catalog.Length) idx = 0;
                return DisciplineSpec.Catalog[idx];
            }
        }

        private bool IsCalibrationReady
        {
            get
            {
                DisciplineSpec spec = CurrentDiscipline;
                if (spec.GeometryKind == CalibrationKind.SingleCenterPoint)
                {
                    return _anchor1 != null;
                }
                return _anchor1 != null && _anchor2 != null && _baselineProjector != null && _baselineProjector.IsValidSpan;
            }
        }

        private void RestoreSavedWorkspaceState()
        {
            WorkspaceStateSnapshot snap = WorkspacePersistenceService.LoadWorkspaceState();

            if (!string.IsNullOrWhiteSpace(snap.LanguageCode))
            {
                I18n.SetLanguage(snap.LanguageCode);
            }

            _suppressLanguageChangeHandler = true;
            _cboLanguage.SelectedItem = I18n.CurrentLanguage;
            _suppressLanguageChangeHandler = false;

            _suppressDisciplineChangeHandler = true;
            _cboDiscipline.SelectedIndex = snap.DisciplineCode;
            _suppressDisciplineChangeHandler = false;

            _offsetStepper.ValueMillimeters = snap.CircleRadiusOffsetMm;
            _chkLockCalibration.Checked = snap.LockCalibrationControls;

            _anchor1 = snap.CalibratedPoint1;
            _anchor2 = snap.CalibratedPoint2;
            RebuildBaselineProjectorIfNeeded();

            // Populate serial dropdowns
            RefreshPortDropdownPreserveSelection(snap.SerialEndpoint.PortName);
            _cboBaudRate.SelectedItem = snap.SerialEndpoint.BaudRate;
            if (_cboBaudRate.SelectedIndex < 0) _cboBaudRate.SelectedItem = 9600;

            _cboDataBits.SelectedItem = snap.SerialEndpoint.DataBits;
            if (_cboDataBits.SelectedIndex < 0) _cboDataBits.SelectedItem = 8;

            int parityIdx = (int)snap.SerialEndpoint.ParityMode;
            if (parityIdx >= 0 && parityIdx < _cboParity.Items.Count) _cboParity.SelectedIndex = parityIdx;

            int stopIdx = snap.SerialEndpoint.StopBitsMode == StopBits.Two ? 1
                : (snap.SerialEndpoint.StopBitsMode == StopBits.OnePointFive ? 2 : 0);
            _cboStopBits.SelectedIndex = stopIdx;

            int protoIdx = (int)snap.SerialEndpoint.ProtocolPreference;
            if (protoIdx >= 0 && protoIdx < _cboProtocolMode.Items.Count) _cboProtocolMode.SelectedIndex = protoIdx;

            ApplyLocalizationToUi();

            if (!IsCalibrationReady)
            {
                _headerNav.ActiveTabIndex = 2;
            }
        }

        private void PersistCurrentWorkspaceState()
        {
            var snap = new WorkspaceStateSnapshot
            {
                LanguageCode = I18n.CurrentLanguageCode,
                DisciplineCode = _cboDiscipline.SelectedIndex >= 0 ? _cboDiscipline.SelectedIndex : 0,
                LockCalibrationControls = _chkLockCalibration.Checked,
                CircleRadiusOffsetMm = _offsetStepper.ValueMillimeters,
                CalibratedPoint1 = _anchor1,
                CalibratedPoint2 = _anchor2,
                SerialEndpoint = BuildSerialProfileFromControls()
            };
            WorkspacePersistenceService.SaveWorkspaceState(snap);
        }

        private void OnDisciplineComboChanged(object sender, EventArgs e)
        {
            if (_suppressDisciplineChangeHandler) return;
            DisciplineSpec spec = CurrentDiscipline;
            _offsetStepper.ValueMillimeters = spec.StandardRadiusMm;
            RebuildBaselineProjectorIfNeeded();
            RefreshCalibrationWidgets();
        }

        private void UpdateSetupControlsLockState()
        {
            bool unlocked = !_chkLockCalibration.Checked;
            _cboDiscipline.Enabled = unlocked;
            _offsetStepper.Enabled = unlocked;
            _btnAcquireP1Modal.Enabled = unlocked;
            _btnUseLastAsP1.Enabled = unlocked && (_lastReceivedReading != null);
            _btnClearCalibration.Enabled = unlocked;

            bool needsP2 = CurrentDiscipline.GeometryKind == CalibrationKind.TwoPointBaseline;
            _btnAcquireP2Modal.Enabled = unlocked && needsP2;
            _btnUseLastAsP2.Enabled = unlocked && needsP2 && (_lastReceivedReading != null);
            _btnSwapP1P2.Enabled = unlocked && needsP2 && _anchor1 != null && _anchor2 != null;
        }

        private void RebuildBaselineProjectorIfNeeded()
        {
            if (CurrentDiscipline.GeometryKind == CalibrationKind.TwoPointBaseline && _anchor1 != null && _anchor2 != null)
            {
                _baselineProjector = new BaselineProjector2D(_anchor1, _anchor2);
            }
            else
            {
                _baselineProjector = null;
            }
        }

        private void RefreshCalibrationWidgets()
        {
            RebuildBaselineProjectorIfNeeded();
            DisciplineSpec spec = CurrentDiscipline;
            bool isJumps = (spec.GeometryKind == CalibrationKind.TwoPointBaseline);

            if (_anchor1 != null)
            {
                _badgeP1Status.ApplyTone(I18n.T("Setup.StatusP1Calibrated"), Palette.EmeraldText, Palette.EmeraldDot, Palette.EmeraldTint, Palette.EmeraldBorder);
                _lblP1Coordinates.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Hz: {0:0.0000} gon ({1})  |  Dh: {2:0.000} m",
                    _anchor1.HorizontalGon,
                    _anchor1.FormatHorizontalDms(),
                    _anchor1.HorizontalDistanceMeters);
            }
            else
            {
                _badgeP1Status.ApplyTone(I18n.T("Setup.StatusToAcquire"), Palette.CrimsonText, Palette.CrimsonDot, Palette.CrimsonTint, Palette.CrimsonBorder);
                _lblP1Coordinates.Text = I18n.T("Setup.P1EmptyText");
            }

            if (!isJumps)
            {
                _badgeP2Status.ApplyTone(I18n.T("Setup.StatusNotRequired"), Palette.InkSecondary, Palette.InkTertiary, Palette.NavPillTrack, Palette.HairlineBorder);
                _lblP2Coordinates.Text = I18n.T("Setup.P2NotRequiredText");
            }
            else if (_anchor2 != null)
            {
                _badgeP2Status.ApplyTone(I18n.T("Setup.StatusP2Calibrated"), Palette.EmeraldText, Palette.EmeraldDot, Palette.EmeraldTint, Palette.EmeraldBorder);
                _lblP2Coordinates.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Hz: {0:0.0000} gon ({1})  |  Dh: {2:0.000} m",
                    _anchor2.HorizontalGon,
                    _anchor2.FormatHorizontalDms(),
                    _anchor2.HorizontalDistanceMeters);
            }
            else
            {
                _badgeP2Status.ApplyTone(I18n.T("Setup.StatusToAcquire"), Palette.CrimsonText, Palette.CrimsonDot, Palette.CrimsonTint, Palette.CrimsonBorder);
                _lblP2Coordinates.Text = I18n.T("Setup.P2EmptyText");
            }

            bool ready = IsCalibrationReady;
            if (ready)
            {
                string modeDesc = isJumps
                    ? I18n.Format("Setup.ModeBaselineActive", _baselineProjector.SpanLengthMeters)
                    : I18n.Format("Setup.ModeCenterActive", _anchor1.HorizontalDistanceMeters);

                _badgeCalibrationHeader.ApplyTone(modeDesc, Palette.EmeraldText, Palette.EmeraldDot, Palette.EmeraldTint, Palette.EmeraldBorder);
                _lblBaselineSpanInfo.Text = I18n.Format("Setup.GeometryActivePrefix", modeDesc);
                _lblBaselineSpanInfo.ForeColor = Palette.EmeraldText;
            }
            else
            {
                _badgeCalibrationHeader.ApplyTone(I18n.T("Setup.BadgeCalibrationRequired"), Palette.AmberText, Palette.AmberDot, Palette.AmberTint, Palette.AmberBorder);
                _lblBaselineSpanInfo.Text = I18n.Format("Setup.GeometryIncompleteFormat", spec.DisplayName);
                _lblBaselineSpanInfo.ForeColor = Palette.AmberText;
            }

            _bannerUncalibrated.Visible = !ready;
            _measureGrid.RowStyles[1].Height = ready ? 0f : 48f;

            UpdateSetupControlsLockState();
            RefreshHeaderAndHeroLabels();
            _radarCanvas.UpdateGeometryState(spec.GeometryKind, _anchor1, _anchor2, _baselineProjector, _lastReceivedReading, _offsetStepper.ValueMillimeters);
        }

        private void RefreshHeaderAndHeroLabels()
        {
            DisciplineSpec spec = CurrentDiscipline;
            int offsetMm = _offsetStepper.ValueMillimeters;
            string offsetLabel = offsetMm == 0
                ? I18n.T("Live.NoOffsetBadge")
                : I18n.Format("Live.RadiusOffsetBadge", -offsetMm);

            _lblDisciplineBadge.Text = spec.DisplayName.ToUpperInvariant() + "  •  " + offsetLabel;
            _radarCanvas.UpdateGeometryState(spec.GeometryKind, _anchor1, _anchor2, _baselineProjector, _lastReceivedReading, offsetMm);
        }

        private void OpenGuidedPointCapture(int pointNumber)
        {
            if (_chkLockCalibration.Checked) return;
            string title = pointNumber == 1 ? I18n.T("Setup.Point1Name") : I18n.T("Setup.Point2Name");

            using (var dlg = new GuidedCalibrationDialog(title, _lastReceivedReading))
            {
                _openCalibrationModal = dlg;
                DialogResult res = dlg.ShowDialog(this);
                _openCalibrationModal = null;

                if (res == DialogResult.OK && dlg.AcceptedPoint != null)
                {
                    AssignCalibratedPoint(pointNumber, dlg.AcceptedPoint);
                }
            }
        }

        private void AssignLastReadingDirectly(int pointNumber)
        {
            if (_chkLockCalibration.Checked || _lastReceivedReading == null) return;
            AssignCalibratedPoint(pointNumber, _lastReceivedReading);
        }

        private void AssignCalibratedPoint(int pointNumber, PolarReading reading)
        {
            if (reading == null) return;

            if (pointNumber == 1)
            {
                if (CurrentDiscipline.GeometryKind == CalibrationKind.TwoPointBaseline &&
                    _anchor2 != null &&
                    reading.PlanarDistanceTo(_anchor2) < 0.05)
                {
                    MessageBox.Show(this, I18n.T("Setup.InvalidBaselinePointsTooClose"), I18n.T("Setup.InvalidBaselineTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _anchor1 = reading;
            }
            else
            {
                if (_anchor1 != null && reading.PlanarDistanceTo(_anchor1) < 0.05)
                {
                    MessageBox.Show(this, I18n.T("Setup.InvalidBaselinePointsTooClose"), I18n.T("Setup.InvalidBaselineTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _anchor2 = reading;
            }

            RefreshCalibrationWidgets();
            PersistCurrentWorkspaceState();
        }

        private void SwapCalibrationAnchors()
        {
            if (_chkLockCalibration.Checked || _anchor1 == null || _anchor2 == null) return;
            PolarReading temp = _anchor1;
            _anchor1 = _anchor2;
            _anchor2 = temp;
            RefreshCalibrationWidgets();
            PersistCurrentWorkspaceState();
        }

        // ====================================================================
        // TELEMETRY PROCESSING & MEASUREMENT EVALUATION
        // ====================================================================
        private static string FormatTelemetryLine(PolarReading reading)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[{0}]  Hz: {1} ({2:0.0000} gon)  |  V: {3} ({4:0.0000} gon)  |  Ds: {5:0.000} m  |  Dh: {6:0.000} m",
                reading.SourceProtocolName,
                reading.FormatHorizontalDms(),
                reading.HorizontalGon,
                reading.FormatVerticalDms(),
                reading.VerticalGon,
                reading.SlopeDistanceM,
                reading.HorizontalDistanceMeters);
        }

        private void OnRawFrameFromInstrument(string rawFrame)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnRawFrameFromInstrument), rawFrame);
                return;
            }

            _lblLastRawPacketBox.Text = I18n.Format(
                "Serial.RawPacketFormat",
                _serialController.TotalPacketsReceived,
                rawFrame.Length,
                rawFrame,
                DateTime.Now);
        }

        private void OnReadingDecodedFromInstrument(PolarReading reading)
        {
            if (IsDisposed || reading == null) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<PolarReading>(OnReadingDecodedFromInstrument), reading);
                return;
            }

            _lastReceivedReading = reading;

            string telemetryInfo = FormatTelemetryLine(reading);

            _lblTelemetryStrip.Text = telemetryInfo;
            _lblSetupLivePreview.Text = I18n.Format("Setup.LivePreviewReady", telemetryInfo);
            _lblLastRawPacketBox.Text = I18n.Format(
                "Serial.DecodedPacketFormat",
                _serialController.TotalPacketsReceived,
                reading.RawTelemetryText,
                telemetryInfo);

            UpdateSetupControlsLockState();

            // Forward to modal capture dialog if open
            if (_openCalibrationModal != null && !_openCalibrationModal.IsDisposed)
            {
                _openCalibrationModal.BindReading(reading);
                return;
            }

            // If user is on Setup or Serial tab, update radar preview without recording an official attempt
            if (_headerNav.ActiveTabIndex == 2 || _headerNav.ActiveTabIndex == 3)
            {
                _radarCanvas.UpdateGeometryState(
                    CurrentDiscipline.GeometryKind,
                    _anchor1,
                    _anchor2,
                    _baselineProjector,
                    reading,
                    _offsetStepper.ValueMillimeters);
                return;
            }

            if (!IsCalibrationReady)
            {
                _lblDiagnosticSubline.Text = I18n.T("Live.UncalibratedSubline");
                _lblDiagnosticSubline.ForeColor = Palette.AmberText;
                return;
            }

            EvaluateAndRecordOfficialAttempt(reading);
        }

        private void EvaluateAndRecordOfficialAttempt(PolarReading targetReading)
        {
            DisciplineSpec spec = CurrentDiscipline;
            EvaluationOutcome eval = AthleticsMeasurementEngine.EvaluateAttempt(
                spec.GeometryKind,
                _anchor1,
                _anchor2,
                _baselineProjector,
                targetReading,
                _offsetStepper.ValueMillimeters);

            _radarCanvas.UpdateGeometryState(
                spec.GeometryKind,
                _anchor1,
                _anchor2,
                _baselineProjector,
                targetReading,
                _offsetStepper.ValueMillimeters);

            if (!eval.IsValid)
            {
                _lblHeroMark.Text = eval.FormattedOfficialMark;
                _lblHeroMark.ForeColor = Palette.AmberText;
                _lblDiagnosticSubline.Text = eval.DiagnosticSummary;
                _lblDiagnosticSubline.ForeColor = Palette.AmberText;
                return;
            }

            _lblHeroMark.Text = eval.FormattedOfficialMark.Trim();
            _lblHeroMark.ForeColor = Palette.InkPrimary;
            _lblDiagnosticSubline.Text = eval.DiagnosticSummary;
            _lblDiagnosticSubline.ForeColor = Palette.EmeraldText;

            AppendOfficialMarkToLedgers(eval.FormattedOfficialMark);
        }

        private void RecordFoulAttempt()
        {
            _lblHeroMark.Text = I18n.T("Mark.Foul");
            _lblHeroMark.ForeColor = Palette.CrimsonText;
            _lblDiagnosticSubline.Text = I18n.T("Live.FoulSubline");
            _lblDiagnosticSubline.ForeColor = Palette.CrimsonText;
            AppendOfficialMarkToLedgers("    NULLO");
        }

        private void AppendOfficialMarkToLedgers(string formattedMark)
        {
            RecordedAttemptEntry entry = WorkspacePersistenceService.AppendLedgerRecord(formattedMark, CurrentDiscipline.DisplayName);
            _miniAttemptLedger.AppendEntry(entry);

            var fullCopy = RecordedAttemptEntry.ParseLedgerLine(_fullAttemptLedger.RecordCount + 1, entry.RawLedgerLine, CurrentDiscipline.DisplayName);
            _fullAttemptLedger.AppendEntry(fullCopy);
            RefreshLedgerCounters();
        }

        private void LoadSavedHistoryLedger()
        {
            List<RecordedAttemptEntry> entries = WorkspacePersistenceService.ReadSavedLedger(CurrentDiscipline.DisplayName);
            _miniAttemptLedger.LoadEntries(entries);

            List<RecordedAttemptEntry> entriesCopy = WorkspacePersistenceService.ReadSavedLedger(CurrentDiscipline.DisplayName);
            _fullAttemptLedger.LoadEntries(entriesCopy);
            RefreshLedgerCounters();
        }

        private void RefreshLedgerCounters()
        {
            int count = _fullAttemptLedger.RecordCount;
            string best = _fullAttemptLedger.BestMarkFormatted;
            _headerNav.SetHistoryCounter(count);
            _lblBestMeasureChip.Text = I18n.Format("Live.BestMarkChip", best);
            _lblHistoryStats.Text = I18n.Format("History.StatsFormat", count, best);
        }

        private void OnSimulateReadingButtonClicked(object sender, EventArgs e)
        {
            DisciplineSpec spec = CurrentDiscipline;
            if (!IsCalibrationReady)
            {
                // Auto-seed realistic MATO MTS-602R+ calibration anchors so the user can test immediately
                _anchor1 = TelemetryFrameClassifier.TryParsePacket("000253210728482681220");
                if (spec.GeometryKind == CalibrationKind.TwoPointBaseline)
                {
                    _anchor2 = PolarReading.FromSexagesimalDegrees(295.0, 102.0, 3.250, "MATO MTS-602R+ (Sim)");
                }
                RefreshCalibrationWidgets();
            }

            // Generate a realistic field attempt using a raw MATO 21-digit frame or polar reading
            var rng = new Random(Environment.TickCount);
            double simulatedDistMm = 5553 + rng.Next(0, 9500);
            int degH = 45 + rng.Next(0, 35);
            int minH = rng.Next(0, 59);
            int secH = rng.Next(0, 59);
            string syntheticMato21 = string.Format(
                CultureInfo.InvariantCulture,
                "{0:0000000}0975803{1:000}{2:00}{3:00}",
                (int)simulatedDistMm,
                degH,
                minH,
                secH);

            _headerNav.ActiveTabIndex = 0;
            _serialController.InjectSyntheticFrame(syntheticMato21);
        }

        // ====================================================================
        // CLIPBOARD & CSV EXPORT
        // ====================================================================
        private void CopyHeroMeasureToClipboard()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_lblHeroMark.Text))
                {
                    string mark = _lblHeroMark.Text.Trim();
                    Clipboard.SetText(mark);
                    _lblDiagnosticSubline.Text = I18n.Format("Live.CopiedSubline", mark);
                }
            }
            catch
            {
            }
        }

        private void CopyAllHistoryToClipboard()
        {
            if (_fullAttemptLedger.RecordCount == 0) return;
            try
            {
                var sb = new StringBuilder();
                foreach (RecordedAttemptEntry r in _fullAttemptLedger.Entries)
                {
                    string markText = r.IsFoulOrNull ? I18n.T("Mark.Foul") : r.OfficialMarkText;
                    sb.AppendFormat(CultureInfo.InvariantCulture, "#{0:00}\t{1}\t{2}", r.SequenceNumber, r.TimestampText, markText);
                    sb.AppendLine();
                }
                Clipboard.SetText(sb.ToString());
            }
            catch
            {
            }
        }

        private void ExportHistoryToCsv()
        {
            if (_fullAttemptLedger.RecordCount == 0) return;
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = I18n.T("History.ExportDialogTitle");
                sfd.Filter = I18n.T("History.ExportDialogFilter");
                sfd.FileName = "EDM_Results_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) + ".csv";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var lines = new List<string> { I18n.T("History.CsvHeader") };
                        foreach (RecordedAttemptEntry r in _fullAttemptLedger.Entries)
                        {
                            string markText = r.IsFoulOrNull ? I18n.T("Mark.Foul") : r.OfficialMarkText.Trim();
                            lines.Add(string.Format(
                                CultureInfo.InvariantCulture,
                                "{0};{1};{2};{3};{4};{5}",
                                r.SequenceNumber,
                                r.TimestampText,
                                r.DisciplineLabel,
                                markText,
                                r.IsFoulOrNull ? "" : r.NumericMeters.ToString("0.00", CultureInfo.InvariantCulture),
                                r.IsFoulOrNull ? I18n.T("Mark.Foul") : I18n.T("Mark.Valid")));
                        }
                        File.WriteAllLines(sfd.FileName, lines.ToArray(), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, I18n.Format("History.ExportErrorText", ex.Message), I18n.T("History.ExportErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ====================================================================
        // SERIAL CONFIGURATION HELPERS
        // ====================================================================
        private void RefreshPortDropdownPreserveSelection(string preferredPort = null)
        {
            string current = preferredPort ?? (_cboComPort.SelectedItem != null ? _cboComPort.SelectedItem.ToString() : "COM1");
            string[] ports = SerialLinkController.DiscoverSystemPorts();

            _cboComPort.BeginUpdate();
            _cboComPort.Items.Clear();
            for (int i = 0; i < ports.Length; i++)
            {
                _cboComPort.Items.Add(ports[i]);
            }
            if (!string.IsNullOrWhiteSpace(current) && !_cboComPort.Items.Contains(current))
            {
                _cboComPort.Items.Add(current);
            }
            _cboComPort.SelectedItem = current;
            if (_cboComPort.SelectedIndex < 0 && _cboComPort.Items.Count > 0)
            {
                _cboComPort.SelectedIndex = 0;
            }
            _cboComPort.EndUpdate();
        }

        private SerialEndpointProfile BuildSerialProfileFromControls()
        {
            var profile = new SerialEndpointProfile();
            if (_cboComPort.SelectedItem != null)
            {
                profile.PortName = _cboComPort.SelectedItem.ToString();
            }

            int baud;
            if (_cboBaudRate.SelectedItem != null && int.TryParse(_cboBaudRate.SelectedItem.ToString(), out baud))
            {
                profile.BaudRate = baud;
            }

            int bits;
            if (_cboDataBits.SelectedItem != null && int.TryParse(_cboDataBits.SelectedItem.ToString(), out bits))
            {
                profile.DataBits = bits;
            }

            if (_cboParity.SelectedIndex == 1) profile.ParityMode = Parity.Odd;
            else if (_cboParity.SelectedIndex == 2) profile.ParityMode = Parity.Even;
            else profile.ParityMode = Parity.None;

            if (_cboStopBits.SelectedIndex == 1) profile.StopBitsMode = StopBits.Two;
            else if (_cboStopBits.SelectedIndex == 2) profile.StopBitsMode = StopBits.OnePointFive;
            else profile.StopBitsMode = StopBits.One;

            int protoIdx = _cboProtocolMode.SelectedIndex;
            if (Enum.IsDefined(typeof(InstrumentProtocolPreference), protoIdx))
            {
                profile.ProtocolPreference = (InstrumentProtocolPreference)protoIdx;
            }

            return profile;
        }

        private void ApplyAndConnectSerial(bool forceReopen)
        {
            SerialEndpointProfile profile = BuildSerialProfileFromControls();
            _serialController.Connect(profile, forceReopen);
            UpdateSerialSummaryLabels();
            PersistCurrentWorkspaceState();
        }

        private void OnSerialLinkStateChanged(bool connected, string statusMessage)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool, string>(OnSerialLinkStateChanged), connected, statusMessage);
                return;
            }

            _lastSerialStatusMessage = statusMessage ?? string.Empty;
            if (connected)
            {
                _badgeSerialHeader.ApplyTone(statusMessage, Palette.EmeraldText, Palette.EmeraldDot, Palette.EmeraldTint, Palette.EmeraldBorder);
                _badgeSerialDetail.ApplyTone(statusMessage, Palette.EmeraldText, Palette.EmeraldDot, Palette.EmeraldTint, Palette.EmeraldBorder);
            }
            else
            {
                string portName = _cboComPort.SelectedItem != null ? _cboComPort.SelectedItem.ToString() : "COM1";
                _badgeSerialHeader.ApplyTone(I18n.Format("Serial.BadgeOfflineFormat", portName), Palette.CrimsonText, Palette.CrimsonDot, Palette.CrimsonTint, Palette.CrimsonBorder);
                _badgeSerialDetail.ApplyTone(statusMessage, Palette.CrimsonText, Palette.CrimsonDot, Palette.CrimsonTint, Palette.CrimsonBorder);
            }
        }

        private void UpdateSerialSummaryLabels()
        {
            SerialEndpointProfile p = BuildSerialProfileFromControls();
            _lblSerialEndpointSummary.Text = I18n.Format(
                "Serial.EndpointSummaryFormat",
                p.PortName,
                p.BaudRate,
                p.DataBits,
                p.ParityMode,
                p.StopBitsMode);
        }
    }
}
