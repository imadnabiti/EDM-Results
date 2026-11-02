using System;

namespace EDMResults.Core
{
    public enum CalibrationKind
    {
        SingleCenterPoint,
        TwoPointBaseline
    }

    public sealed class DisciplineInfo
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int DefaultOffsetMm { get; private set; }
        public CalibrationKind Mode { get; private set; }

        public DisciplineInfo(int id, string name, int defaultOffsetMm, CalibrationKind mode)
        {
            Id = id;
            Name = name;
            DefaultOffsetMm = defaultOffsetMm;
            Mode = mode;
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly DisciplineInfo[] All = new DisciplineInfo[]
        {
            new DisciplineInfo(0, "Peso",           1068, CalibrationKind.SingleCenterPoint),
            new DisciplineInfo(1, "Disco",          1250, CalibrationKind.SingleCenterPoint),
            new DisciplineInfo(2, "Martello",       1068, CalibrationKind.SingleCenterPoint),
            new DisciplineInfo(3, "Giavellotto",    8000, CalibrationKind.SingleCenterPoint),
            new DisciplineInfo(4, "Salto in Lungo",    0, CalibrationKind.TwoPointBaseline),
            new DisciplineInfo(5, "Salto Triplo",      0, CalibrationKind.TwoPointBaseline)
        };
    }

    public sealed class SurveyPoint
    {
        private const double GonToRad = Math.PI / 200.0;

        public double HorizontalGon { get; private set; }
        public double VerticalGon { get; private set; }
        public double SlopeDistanceMm { get; private set; }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public bool IsValid
        {
            get { return SlopeDistanceMm > 0.0; }
        }

        public SurveyPoint(double horizontalGon, double verticalGon, double slopeDistanceMm)
        {
            HorizontalGon = horizontalGon;
            VerticalGon = verticalGon;
            SlopeDistanceMm = slopeDistanceMm;

            double hzRad = horizontalGon * GonToRad;
            double vRad = verticalGon * GonToRad;

            double horizontalProjection = Math.Sin(vRad) * slopeDistanceMm;
            X = Math.Cos(hzRad) * horizontalProjection;
            Y = Math.Sin(hzRad) * horizontalProjection;
            Z = Math.Cos(vRad) * slopeDistanceMm;
        }

        public double PlanarDistanceTo(SurveyPoint other)
        {
            if (other == null) return 0.0;
            double dx = other.X - X;
            double dy = other.Y - Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }

    public sealed class PlanarBaseline
    {
        private readonly double _coeffA;
        private readonly double _coeffB;
        private readonly double _coeffC;
        private readonly double _norm;

        public double SpanMm { get; private set; }

        public PlanarBaseline(SurveyPoint first, SurveyPoint second)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (second == null) throw new ArgumentNullException("second");

            _coeffA = first.Y - second.Y;
            _coeffB = second.X - first.X;
            _coeffC = -((_coeffA * first.X) + (_coeffB * first.Y));
            _norm = Math.Sqrt((_coeffA * _coeffA) + (_coeffB * _coeffB));
            SpanMm = first.PlanarDistanceTo(second);
        }

        public double SignedDistanceTo(SurveyPoint target)
        {
            if (target == null || _norm <= 1e-9) return 0.0;
            return ((_coeffA * target.X) + (_coeffB * target.Y) + _coeffC) / _norm;
        }

        public static bool TryValidateSpan(SurveyPoint first, SurveyPoint second, out PlanarBaseline baseline, out string errorMessage)
        {
            baseline = null;
            errorMessage = null;

            double span = first.PlanarDistanceTo(second);
            if (span < 500.0)
            {
                errorMessage = "I due punti di controllo sono più vicini di mezzo metro (min 500 mm).";
                return false;
            }
            if (span > 2000.0)
            {
                errorMessage = "I due punti di controllo sono più distanti di 2 metri (max 2000 mm).";
                return false;
            }

            baseline = new PlanarBaseline(first, second);
            return true;
        }
    }

    public static class EdmResultFormatter
    {
        /// <summary>
        /// Formats a distance in millimeters into official EDM athletics notation (floored to whole centimeters, e.g. " 18,42 m").
        /// </summary>
        public static string FormatOfficialDistance(double distanceMm, bool isFoul)
        {
            if (isFoul || double.IsNaN(distanceMm))
            {
                return "NULLO";
            }

            int totalCentimeters = (int)Math.Abs(Math.Floor(distanceMm / 10.0));
            string digits = totalCentimeters.ToString().PadLeft(3, '0');

            char decimalSeparator = (0.1).ToString()[1];
            string metersPart = digits.Substring(0, digits.Length - 2);
            string cmPart = digits.Substring(digits.Length - 2, 2);

            string formatted = metersPart + decimalSeparator + cmPart;
            if (distanceMm < 0)
            {
                formatted = "-" + formatted;
            }

            if (formatted.Length < 7)
            {
                formatted = formatted.PadLeft(7, ' ');
            }

            return formatted + " m";
        }

        /// <summary>
        /// Builds the STX/ETX frame sent back over RS-232 to an external PC/scoreboard when enabled.
        /// </summary>
        public static string BuildScoreboardPacket(string formattedDistance)
        {
            if (string.IsNullOrEmpty(formattedDistance) || formattedDistance.Length < 7)
            {
                return string.Empty;
            }

            string meters = formattedDistance.Substring(1, 3);
            string centimeters = formattedDistance.Substring(5, 2);
            return "\x02" + meters + "." + centimeters + "\x03";
        }
    }
}

