using System;
using System.IO.Ports;
using System.Text;

namespace EDMResults.Core
{
    public sealed class SerialConfiguration
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public bool ForwardToScoreboard { get; set; }

        public SerialConfiguration()
        {
            PortName = "COM1";
            BaudRate = 19200;
            DataBits = 8;
            Parity = Parity.None;
            StopBits = StopBits.One;
            ForwardToScoreboard = true;
        }
    }

    public sealed class SerialTransceiver : IDisposable
    {
        private readonly object _sync = new object();
        private readonly StringBuilder _lineAccumulator = new StringBuilder();
        private SerialPort _serialPort;

        public event Action<SurveyPoint, string> PointReceived;
        public event Action<string> StatusChanged;

        public bool IsConnected
        {
            get
            {
                lock (_sync)
                {
                    return _serialPort != null && _serialPort.IsOpen;
                }
            }
        }

        public bool TryOpen(SerialConfiguration cfg, out string error)
        {
            error = null;
            Close();

            try
            {
                lock (_sync)
                {
                    _serialPort = new SerialPort
                    {
                        PortName = cfg.PortName,
                        BaudRate = cfg.BaudRate,
                        DataBits = cfg.DataBits,
                        Parity = cfg.Parity,
                        StopBits = cfg.StopBits,
                        Encoding = Encoding.Default,
                        DtrEnable = true,
                        RtsEnable = true,
                        ReadTimeout = 500,
                        WriteTimeout = 500
                    };
                    _serialPort.DataReceived += OnSerialDataReceived;
                    _serialPort.Open();
                }

                NotifyStatus("EDM connesso su " + cfg.PortName + " (" + cfg.BaudRate + " bps)");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                NotifyStatus("Porta " + cfg.PortName + " non attiva: " + ex.Message);
                return false;
            }
        }

        public void Close()
        {
            lock (_sync)
            {
                if (_serialPort != null)
                {
                    try
                    {
                        _serialPort.DataReceived -= OnSerialDataReceived;
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Close();
                        }
                        _serialPort.Dispose();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _serialPort = null;
                    }
                }
                _lineAccumulator.Length = 0;
            }
            NotifyStatus("Seriale EDM disconnessa");
        }

        public void SendRaw(string packet)
        {
            if (string.IsNullOrEmpty(packet)) return;
            lock (_sync)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    try
                    {
                        byte[] payload = Encoding.Default.GetBytes(packet);
                        _serialPort.Write(payload, 0, payload.Length);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void InjectSimulatedLine(string rawTelemetryLine)
        {
            ProcessIncomingChunk(rawTelemetryLine + "\r\n");
        }

        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string chunk = null;
            lock (_sync)
            {
                if (_serialPort == null || !_serialPort.IsOpen) return;
                try
                {
                    chunk = _serialPort.ReadExisting();
                }
                catch
                {
                }
            }

            if (!string.IsNullOrEmpty(chunk))
            {
                ProcessIncomingChunk(chunk);
            }
        }

        private void ProcessIncomingChunk(string chunk)
        {
            lock (_sync)
            {
                _lineAccumulator.Append(chunk);
                while (true)
                {
                    string current = _lineAccumulator.ToString();
                    int crIdx = current.IndexOf('\r');
                    int lfIdx = current.IndexOf('\n');

                    if (crIdx < 0 && lfIdx < 0)
                    {
                        break;
                    }

                    int splitEnd;
                    int nextStart;
                    if (crIdx >= 0 && lfIdx >= 0)
                    {
                        splitEnd = Math.Min(crIdx, lfIdx);
                        nextStart = Math.Max(crIdx, lfIdx) + 1;
                    }
                    else
                    {
                        splitEnd = Math.Max(crIdx, lfIdx);
                        nextStart = splitEnd + 1;
                    }

                    string line = current.Substring(0, splitEnd).Trim();
                    _lineAccumulator.Remove(0, nextStart);

                    if (line.Length > 0)
                    {
                        SurveyPoint decoded = TotalStationDecoder.DecodeFrame(line);
                        if (decoded.IsValid)
                        {
                            Action<SurveyPoint, string> handler = PointReceived;
                            if (handler != null)
                            {
                                handler(decoded, line);
                            }
                        }
                    }
                }
            }
        }

        private void NotifyStatus(string statusText)
        {
            Action<string> handler = StatusChanged;
            if (handler != null)
            {
                handler(statusText);
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}

