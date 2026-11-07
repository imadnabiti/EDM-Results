using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace EDMResults.Core
{
    public sealed class SerialEndpointProfile
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity ParityMode { get; set; }
        public StopBits StopBitsMode { get; set; }
        public InstrumentProtocolPreference ProtocolPreference { get; set; }

        public SerialEndpointProfile()
        {
            PortName = "COM1";
            BaudRate = 9600;
            DataBits = 8;
            ParityMode = Parity.None;
            StopBitsMode = StopBits.One;
            ProtocolPreference = InstrumentProtocolPreference.AutoRecognize;
        }

        public bool MatchesHardwareSettings(SerialEndpointProfile other)
        {
            if (other == null) return false;
            return string.Equals(PortName, other.PortName, StringComparison.OrdinalIgnoreCase) &&
                   BaudRate == other.BaudRate &&
                   DataBits == other.DataBits &&
                   ParityMode == other.ParityMode &&
                   StopBitsMode == other.StopBitsMode;
        }
    }

    public sealed class SerialLinkController : IDisposable
    {
        private const int IdleFrameFlushMs = 55;

        private readonly object _gate = new object();
        private readonly StringBuilder _rxBuffer = new StringBuilder(512);
        private readonly Timer _idleFrameTimer;
        private SerialPort _port;
        private SerialEndpointProfile _activeProfile = new SerialEndpointProfile();
        private bool _disposed;

        public event Action<PolarReading> ReadingDecoded;
        public event Action<string> RawFrameReceived;
        public event Action<bool, string> LinkStateChanged;

        public int TotalPacketsReceived { get; private set; }
        public string LastRawPacket { get; private set; }

        public SerialLinkController()
        {
            LastRawPacket = string.Empty;
            _idleFrameTimer = new Timer(OnIdleFrameTimeout, null, Timeout.Infinite, Timeout.Infinite);
        }

        public bool IsConnected
        {
            get
            {
                lock (_gate)
                {
                    return _port != null && _port.IsOpen;
                }
            }
        }

        public SerialEndpointProfile ActiveProfile
        {
            get { return _activeProfile; }
        }

        public static string[] DiscoverSystemPorts()
        {
            try
            {
                string[] detected = SerialPort.GetPortNames();
                if (detected != null && detected.Length > 0)
                {
                    Array.Sort(detected, StringComparer.OrdinalIgnoreCase);
                    return detected;
                }
            }
            catch
            {
            }
            return new string[] { "COM1", "COM2", "COM3", "COM4" };
        }

        public bool Connect(SerialEndpointProfile profile, bool forceReopen = false)
        {
            if (profile == null) return false;

            bool stateResult = false;
            string statusNote = string.Empty;

            lock (_gate)
            {
                if (_disposed) return false;

                if (!forceReopen && _port != null && _port.IsOpen && _activeProfile.MatchesHardwareSettings(profile))
                {
                    _activeProfile.ProtocolPreference = profile.ProtocolPreference;
                    stateResult = true;
                    statusNote = I18n.Format("Serial.StatusActive", _port.PortName, _port.BaudRate);
                }
                else
                {
                    ReleasePortUnderLock();
                    _activeProfile = profile;

                    try
                    {
                        _port = new SerialPort
                        {
                            PortName = string.IsNullOrWhiteSpace(profile.PortName) ? "COM1" : profile.PortName.Trim(),
                            BaudRate = profile.BaudRate > 0 ? profile.BaudRate : 9600,
                            DataBits = (profile.DataBits >= 5 && profile.DataBits <= 8) ? profile.DataBits : 8,
                            Parity = profile.ParityMode,
                            StopBits = profile.StopBitsMode == StopBits.None ? StopBits.One : profile.StopBitsMode,
                            Handshake = Handshake.None,
                            DtrEnable = true,
                            RtsEnable = true,
                            Encoding = Encoding.ASCII,
                            ReadTimeout = 500,
                            WriteTimeout = 500,
                            ReceivedBytesThreshold = 1
                        };
                        _port.DataReceived += OnSerialBytesAvailable;
                        _port.Open();

                        stateResult = true;
                        statusNote = I18n.Format("Serial.StatusConnected", _port.PortName, _port.BaudRate);
                    }
                    catch (Exception ex)
                    {
                        ReleasePortUnderLock();
                        stateResult = false;
                        statusNote = I18n.Format("Serial.StatusUnavailable", profile.PortName, ex.Message);
                    }
                }
            }

            NotifyLinkState(stateResult, statusNote);
            return stateResult;
        }

        public void Disconnect()
        {
            lock (_gate)
            {
                ReleasePortUnderLock();
            }
            NotifyLinkState(false, I18n.T("Serial.StatusDisconnected"));
        }

        /// <summary>
        /// Allows injecting a synthetic raw frame (e.g. MATO 21-digit or Sokkia/GSI) for testing and simulation.
        /// </summary>
        public void InjectSyntheticFrame(string rawTelemetry)
        {
            if (string.IsNullOrWhiteSpace(rawTelemetry)) return;
            DispatchCompletedFrame(rawTelemetry.Trim());
        }

        private void OnSerialBytesAvailable(object sender, SerialDataReceivedEventArgs e)
        {
            string incomingChunk = null;
            lock (_gate)
            {
                if (_port == null || !_port.IsOpen) return;
                try
                {
                    incomingChunk = _port.ReadExisting();
                }
                catch
                {
                    return;
                }
            }

            if (string.IsNullOrEmpty(incomingChunk)) return;

            lock (_gate)
            {
                for (int i = 0; i < incomingChunk.Length; i++)
                {
                    char c = incomingChunk[i];
                    if (c == '\r' || c == '\n' || c == '\x03' || c == '\x04')
                    {
                        FlushAccumulatedFrameUnderLock();
                    }
                    else
                    {
                        _rxBuffer.Append(c);
                        if (_rxBuffer.Length > 1024)
                        {
                            _rxBuffer.Remove(0, 512);
                        }
                    }
                }

                // Schedule idle-gap frame completion for un-terminated bursts (such as MATO MTS-602R+ 21-byte frames)
                if (_rxBuffer.Length > 0 && !_disposed)
                {
                    _idleFrameTimer.Change(IdleFrameFlushMs, Timeout.Infinite);
                }
                else if (!_disposed)
                {
                    _idleFrameTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private void OnIdleFrameTimeout(object state)
        {
            string burstFrame = null;
            lock (_gate)
            {
                if (_rxBuffer.Length > 0)
                {
                    burstFrame = _rxBuffer.ToString();
                    _rxBuffer.Clear();
                }
            }

            if (!string.IsNullOrWhiteSpace(burstFrame))
            {
                DispatchCompletedFrame(burstFrame);
            }
        }

        private void FlushAccumulatedFrameUnderLock()
        {
            if (_rxBuffer.Length == 0) return;
            string packet = _rxBuffer.ToString();
            _rxBuffer.Clear();
            if (!_disposed)
            {
                _idleFrameTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            ThreadPool.QueueUserWorkItem(_ => DispatchCompletedFrame(packet));
        }

        private void DispatchCompletedFrame(string rawPacket)
        {
            string trimmed = rawPacket.Trim();
            if (trimmed.Length == 0) return;

            LastRawPacket = trimmed;
            TotalPacketsReceived++;

            Action<string> rawHandler = RawFrameReceived;
            if (rawHandler != null)
            {
                try { rawHandler(trimmed); } catch { }
            }

            InstrumentProtocolPreference pref = _activeProfile != null
                ? _activeProfile.ProtocolPreference
                : InstrumentProtocolPreference.AutoRecognize;

            PolarReading reading = TelemetryFrameClassifier.TryParsePacket(trimmed, pref);
            if (reading != null)
            {
                Action<PolarReading> decodedHandler = ReadingDecoded;
                if (decodedHandler != null)
                {
                    try { decodedHandler(reading); } catch { }
                }
            }
        }

        private void ReleasePortUnderLock()
        {
            try
            {
                _idleFrameTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch
            {
            }

            if (_port != null)
            {
                try
                {
                    _port.DataReceived -= OnSerialBytesAvailable;
                    if (_port.IsOpen) _port.Close();
                    _port.Dispose();
                }
                catch
                {
                }
                _port = null;
            }
            _rxBuffer.Clear();
        }

        private void NotifyLinkState(bool connected, string message)
        {
            Action<bool, string> handler = LinkStateChanged;
            if (handler != null)
            {
                try { handler(connected, message); } catch { }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                ReleasePortUnderLock();
                try { _idleFrameTimer.Dispose(); } catch { }
            }
        }
    }
}
