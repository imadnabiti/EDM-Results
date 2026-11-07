using System;
using System.Globalization;

namespace EDMResults.Core
{
    public enum CalibrationKind
    {
        SingleCenterPoint = 0,
        TwoPointBaseline = 1
    }

    public enum AngleUnitKind
    {
        CentesimalGon = 0,
        SexagesimalDms = 1,
        Radians = 2
    }

    public sealed class DisciplineSpec
    {
        public int Code { get; private set; }
        public string NameKey { get; private set; }
        public string DisplayName
        {
            get { return I18n.T(NameKey); }
        }
        public int StandardRadiusMm { get; private set; }
        public CalibrationKind GeometryKind { get; private set; }
        public double SectorDegrees { get; private set; }

        public DisciplineSpec(int code, string nameKey, int standardRadiusMm, CalibrationKind geometryKind, double sectorDegrees)
        {
            Code = code;
            NameKey = nameKey;
            StandardRadiusMm = standardRadiusMm;
            GeometryKind = geometryKind;
            SectorDegrees = sectorDegrees;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public static readonly DisciplineSpec[] Catalog = new DisciplineSpec[]
        {
            new DisciplineSpec(0, "Disc.ShotPut", 1068, CalibrationKind.SingleCenterPoint, 34.92),
            new DisciplineSpec(1, "Disc.Discus", 1250, CalibrationKind.SingleCenterPoint, 34.92),
            new DisciplineSpec(2, "Disc.Javelin", 8000, CalibrationKind.SingleCenterPoint, 28.96),
            new DisciplineSpec(3, "Disc.Hammer", 1068, CalibrationKind.SingleCenterPoint, 34.92),
            new DisciplineSpec(4, "Disc.HorizontalJumps", 0, CalibrationKind.TwoPointBaseline, 0.0)
        };
    }

    /// <summary>
    /// Immutable 2D vector on the horizontal measurement plane (meters).
    /// Uses vector dot/cross products rather than algebraic line equations.
    /// </summary>
    public struct PlanarVector2D
    {
        public readonly double EastingM;
        public readonly double NorthingM;

        public PlanarVector2D(double eastingM, double northingM)
        {
            EastingM = eastingM;
            NorthingM = northingM;
        }

        public double Magnitude
        {
            get { return Math.Sqrt((EastingM * EastingM) + (NorthingM * NorthingM)); }
        }

        public PlanarVector2D Subtract(PlanarVector2D other)
        {
            return new PlanarVector2D(EastingM - other.EastingM, NorthingM - other.NorthingM);
        }

        public double DistanceTo(PlanarVector2D other)
        {
            double dx = EastingM - other.EastingM;
            double dy = NorthingM - other.NorthingM;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        public double Dot(PlanarVector2D other)
        {
            return (EastingM * other.EastingM) + (NorthingM * other.NorthingM);
        }

        public double Cross(PlanarVector2D other)
        {
            return (EastingM * other.NorthingM) - (NorthingM * other.EastingM);
        }
    }

    /// <summary>
    /// Represents a 3D total-station sighting stored in radians and meters,
    /// with seamless conversion between Centesimal Gon and Sexagesimal DMS.
    /// </summary>
    public sealed class PolarReading
    {
        private const double GonPerRadian = 200.0 / Math.PI;
        private const double RadiansPerGon = Math.PI / 200.0;
        private const double DegreesPerRadian = 180.0 / Math.PI;
        private const double RadiansPerDegree = Math.PI / 180.0;

        public double AzimuthRadians { get; private set; }
        public double ZenithRadians { get; private set; }
        public double SlopeMeters { get; private set; }
        public AngleUnitKind NativeAngleUnit { get; private set; }
        public string SourceProtocolName { get; private set; }
        public string RawTelemetryText { get; private set; }
        public DateTime CapturedAtUtc { get; private set; }

        public PlanarVector2D HorizontalProjection { get; private set; }
        public double HorizontalDistanceMeters { get; private set; }
        public double VerticalHeightMeters { get; private set; }

        private PolarReading(
            double azimuthRadians,
            double zenithRadians,
            double slopeMeters,
            AngleUnitKind nativeUnit,
            string protocolName,
            string rawTelemetry)
        {
            AzimuthRadians = NormalizeRadians(azimuthRadians);
            ZenithRadians = zenithRadians;
            SlopeMeters = Math.Max(0.0, slopeMeters);
            NativeAngleUnit = nativeUnit;
            SourceProtocolName = protocolName ?? "Generic EDM";
            RawTelemetryText = rawTelemetry ?? string.Empty;
            CapturedAtUtc = DateTime.UtcNow;

            double horizontalComponent = Math.Abs(SlopeMeters * Math.Sin(ZenithRadians));
            HorizontalDistanceMeters = horizontalComponent;
            VerticalHeightMeters = SlopeMeters * Math.Cos(ZenithRadians);

            HorizontalProjection = new PlanarVector2D(
                horizontalComponent * Math.Cos(AzimuthRadians),
                horizontalComponent * Math.Sin(AzimuthRadians));
        }

        public static PolarReading FromRadians(
            double azimuthRadians,
            double zenithRadians,
            double slopeMeters,
            AngleUnitKind nativeUnit,
            string protocolName,
            string rawTelemetry)
        {
            return new PolarReading(azimuthRadians, zenithRadians, slopeMeters, nativeUnit, protocolName, rawTelemetry);
        }

        public static PolarReading FromGon(
            double horizontalGon,
            double verticalGon,
            double slopeMeters,
            string protocolName = "Sokkia/SET (Gon)",
            string rawTelemetry = "")
        {
            return new PolarReading(
                horizontalGon * RadiansPerGon,
                verticalGon * RadiansPerGon,
                slopeMeters,
                AngleUnitKind.CentesimalGon,
                protocolName,
                rawTelemetry);
        }

        public static PolarReading FromSexagesimalDegrees(
            double horizontalDegrees,
            double verticalDegrees,
            double slopeMeters,
            string protocolName = "MATO/DMS (° ' \")",
            string rawTelemetry = "")
        {
            return new PolarReading(
                horizontalDegrees * RadiansPerDegree,
                verticalDegrees * RadiansPerDegree,
                slopeMeters,
                AngleUnitKind.SexagesimalDms,
                protocolName,
                rawTelemetry);
        }

        public double HorizontalGon
        {
            get { return AzimuthRadians * GonPerRadian; }
        }

        public double VerticalGon
        {
            get { return ZenithRadians * GonPerRadian; }
        }

        public double HorizontalDegrees
        {
            get { return AzimuthRadians * DegreesPerRadian; }
        }

        public double VerticalDegrees
        {
            get { return ZenithRadians * DegreesPerRadian; }
        }

        public double SlopeDistanceM
        {
            get { return SlopeMeters; }
        }

        public double X
        {
            get { return HorizontalProjection.EastingM; }
        }

        public double Y
        {
            get { return HorizontalProjection.NorthingM; }
        }

        public double Z
        {
            get { return VerticalHeightMeters; }
        }

        public double PlanarDistanceTo(PolarReading other)
        {
            if (other == null) return 0.0;
            return HorizontalProjection.DistanceTo(other.HorizontalProjection);
        }

        public string FormatHorizontalDms()
        {
            return FormatAngleAsDms(HorizontalDegrees);
        }

        public string FormatVerticalDms()
        {
            return FormatAngleAsDms(VerticalDegrees);
        }

        public static string FormatAngleAsDms(double decimalDegrees)
        {
            double normalized = decimalDegrees % 360.0;
            if (normalized < 0.0) normalized += 360.0;

            int totalArcSeconds = (int)Math.Round(normalized * 3600.0, MidpointRounding.AwayFromZero);
            int deg = (totalArcSeconds / 3600) % 360;
            int rem = totalArcSeconds % 3600;
            int min = rem / 60;
            int sec = rem % 60;

            return string.Format(CultureInfo.InvariantCulture, "{0:000}°{1:00}'{2:00}\"", deg, min, sec);
        }

        private static double NormalizeRadians(double radians)
        {
            double twoPi = 2.0 * Math.PI;
            double wrapped = radians % twoPi;
            return wrapped < 0.0 ? wrapped + twoPi : wrapped;
        }
    }

    /// <summary>
    /// Orthogonal vector projector for jump take-off board baselines.
    /// Uses unit direction and normal vectors instead of Ax + By + C line parameters.
    /// </summary>
    public sealed class BaselineProjector2D
    {
        private readonly PlanarVector2D _anchorA;
        private readonly PlanarVector2D _anchorB;
        private readonly PlanarVector2D _unitTangent;
        private readonly PlanarVector2D _unitNormal;
        private readonly double _spanLengthMeters;

        public bool IsValidSpan
        {
            get { return _spanLengthMeters > 1e-5; }
        }

        public double SpanLengthMeters
        {
            get { return _spanLengthMeters; }
        }

        public PlanarVector2D AnchorA
        {
            get { return _anchorA; }
        }

        public PlanarVector2D AnchorB
        {
            get { return _anchorB; }
        }

        public BaselineProjector2D(PolarReading firstAnchor, PolarReading secondAnchor)
        {
            if (firstAnchor == null || secondAnchor == null)
            {
                _spanLengthMeters = 0.0;
                return;
            }

            _anchorA = firstAnchor.HorizontalProjection;
            _anchorB = secondAnchor.HorizontalProjection;

            PlanarVector2D delta = _anchorB.Subtract(_anchorA);
            double norm = delta.Magnitude;
            _spanLengthMeters = norm;

            if (norm > 1e-5)
            {
                double invNorm = 1.0 / norm;
                _unitTangent = new PlanarVector2D(delta.EastingM * invNorm, delta.NorthingM * invNorm);
                _unitNormal = new PlanarVector2D(-_unitTangent.NorthingM, _unitTangent.EastingM);
            }
        }

        public double OrthogonalDistanceTo(PolarReading targetReading)
        {
            if (targetReading == null || !IsValidSpan) return 0.0;
            PlanarVector2D relative = targetReading.HorizontalProjection.Subtract(_anchorA);
            return Math.Abs(relative.Dot(_unitNormal));
        }

        public double LongitudinalOffsetAlongBoard(PolarReading targetReading)
        {
            if (targetReading == null || !IsValidSpan) return 0.0;
            PlanarVector2D relative = targetReading.HorizontalProjection.Subtract(_anchorA);
            return relative.Dot(_unitTangent);
        }

        public PlanarVector2D ClosestFootPointOnBaseline(PolarReading targetReading)
        {
            if (targetReading == null || !IsValidSpan) return _anchorA;
            PlanarVector2D relative = targetReading.HorizontalProjection.Subtract(_anchorA);
            double along = relative.Dot(_unitTangent);
            return new PlanarVector2D(
                _anchorA.EastingM + (_unitTangent.EastingM * along),
                _anchorA.NorthingM + (_unitTangent.NorthingM * along));
        }
    }

    public struct EvaluationOutcome
    {
        public bool IsValid;
        public double NetDistanceMeters;
        public double GrossPlanarMeters;
        public double LateralBoardOffsetMeters;
        public string FormattedOfficialMark;
        public string DiagnosticSummary;
    }

    public static class AthleticsMeasurementEngine
    {
        public static EvaluationOutcome EvaluateAttempt(
            CalibrationKind kind,
            PolarReading anchor1,
            PolarReading anchor2,
            BaselineProjector2D baseline,
            PolarReading targetShot,
            int radiusOffsetMillimeters)
        {
            var outcome = new EvaluationOutcome();
            if (targetShot == null || anchor1 == null)
            {
                outcome.IsValid = false;
                outcome.FormattedOfficialMark = "--,-- m";
                outcome.DiagnosticSummary = I18n.T("Eval.IncompleteCalibration");
                return outcome;
            }

            double offsetMeters = radiusOffsetMillimeters * 0.001;
            double grossMeters;
            double lateralMeters = 0.0;

            if (kind == CalibrationKind.SingleCenterPoint)
            {
                grossMeters = anchor1.PlanarDistanceTo(targetShot);
            }
            else
            {
                if (anchor2 == null || baseline == null || !baseline.IsValidSpan)
                {
                    outcome.IsValid = false;
                    outcome.FormattedOfficialMark = "--,-- m";
                    outcome.DiagnosticSummary = I18n.T("Eval.InvalidBaseline");
                    return outcome;
                }
                grossMeters = baseline.OrthogonalDistanceTo(targetShot);
                lateralMeters = baseline.LongitudinalOffsetAlongBoard(targetShot);
            }

            double netMeters = grossMeters - offsetMeters;
            outcome.GrossPlanarMeters = grossMeters;
            outcome.NetDistanceMeters = netMeters;
            outcome.LateralBoardOffsetMeters = lateralMeters;

            if (double.IsNaN(netMeters) || double.IsInfinity(netMeters) || netMeters < 0.0)
            {
                outcome.IsValid = false;
                outcome.FormattedOfficialMark = netMeters < 0.0 ? "0,00 m" : "--,-- m";
                outcome.DiagnosticSummary = netMeters < 0.0
                    ? I18n.T("Eval.InsideCircleRadius")
                    : I18n.T("Eval.ProjectionError");
                return outcome;
            }

            outcome.IsValid = true;
            outcome.FormattedOfficialMark = FormatOfficialMark(netMeters);
            outcome.DiagnosticSummary = I18n.Format(
                "Eval.DiagnosticFormat",
                grossMeters,
                -offsetMeters,
                netMeters);
            return outcome;
        }

        /// <summary>
        /// World Athletics / FIDAL rule: round down to the nearest whole centimeter
        /// using integer division/modulo rather than string manipulation.
        /// </summary>
        public static string FormatOfficialMark(double netMeters)
        {
            if (double.IsNaN(netMeters) || double.IsInfinity(netMeters) || netMeters < 0.0)
            {
                return "   0,00 m";
            }

            long wholeCentimeters = (long)Math.Floor((netMeters + 1e-9) * 100.0);
            if (wholeCentimeters < 0) wholeCentimeters = 0;

            long wholeMeters = wholeCentimeters / 100;
            long remCentimeters = wholeCentimeters % 100;

            string numericToken = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1:00}",
                wholeMeters,
                remCentimeters);

            return numericToken.PadLeft(7, ' ') + " m";
        }
    }
}
