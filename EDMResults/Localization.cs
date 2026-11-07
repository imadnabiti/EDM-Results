using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EDMResults.Core
{
    public sealed class LanguagePack
    {
        public string Code { get; private set; }
        public string NativeName { get; private set; }
        public string ShortBadge { get; private set; }
        private readonly Dictionary<string, string> _strings;

        public LanguagePack(string code, string nativeName, string shortBadge, Dictionary<string, string> strings)
        {
            Code = (code ?? "it").Trim().ToLowerInvariant();
            NativeName = nativeName ?? Code;
            ShortBadge = (shortBadge ?? Code).ToUpperInvariant();
            _strings = strings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGet(string key, out string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = string.Empty;
                return false;
            }
            return _strings.TryGetValue(key, out value);
        }

        public void SetTranslation(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _strings[key] = value ?? string.Empty;
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} ({1})", NativeName, ShortBadge);
        }
    }

    /// <summary>
    /// Central localization and internationalization service (I18n).
    /// Supports built-in Italian ("it") and English ("en") packs, and automatically discovers
    /// any additional external language files ("Languages/*.lang") for future extensibility.
    /// </summary>
    public static class I18n
    {
        private static readonly List<LanguagePack> _registeredPacks = new List<LanguagePack>();
        private static LanguagePack _currentPack;
        private static LanguagePack _fallbackPack;

        public static event Action LanguageChanged;

        static I18n()
        {
            LanguagePack italian = BuildItalianPack();
            LanguagePack english = BuildEnglishPack();

            _registeredPacks.Add(italian);
            _registeredPacks.Add(english);

            _fallbackPack = english;
            _currentPack = italian;

            DiscoverExternalLanguageFiles();
        }

        public static IList<LanguagePack> AvailableLanguages
        {
            get { return _registeredPacks.AsReadOnly(); }
        }

        public static LanguagePack CurrentLanguage
        {
            get { return _currentPack; }
        }

        public static string CurrentLanguageCode
        {
            get { return _currentPack != null ? _currentPack.Code : "it"; }
        }

        public static void SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) return;
            string normalized = languageCode.Trim().ToLowerInvariant();

            for (int i = 0; i < _registeredPacks.Count; i++)
            {
                if (string.Equals(_registeredPacks[i].Code, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    if (_currentPack != _registeredPacks[i])
                    {
                        _currentPack = _registeredPacks[i];
                        Action handler = LanguageChanged;
                        if (handler != null)
                        {
                            try { handler(); } catch { }
                        }
                    }
                    return;
                }
            }
        }

        public static void CycleNextLanguage()
        {
            if (_registeredPacks.Count <= 1) return;
            int idx = _registeredPacks.IndexOf(_currentPack);
            int nextIdx = (idx + 1) % _registeredPacks.Count;
            SetLanguage(_registeredPacks[nextIdx].Code);
        }

        /// <summary>
        /// Registers a new language pack at runtime or overrides an existing language code.
        /// </summary>
        public static void RegisterLanguage(LanguagePack pack)
        {
            if (pack == null) return;
            for (int i = 0; i < _registeredPacks.Count; i++)
            {
                if (string.Equals(_registeredPacks[i].Code, pack.Code, StringComparison.OrdinalIgnoreCase))
                {
                    _registeredPacks[i] = pack;
                    return;
                }
            }
            _registeredPacks.Add(pack);
        }

        /// <summary>
        /// Loads custom *.lang files from the optional "Languages" folder next to the executable.
        /// Format: UTF-8 key=value lines, with metadata keys "Meta.Code=fr", "Meta.Name=Français", "Meta.Badge=FR".
        /// </summary>
        public static void DiscoverExternalLanguageFiles()
        {
            try
            {
                string langDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");
                if (!Directory.Exists(langDir)) return;

                string[] files = Directory.GetFiles(langDir, "*.lang");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                for (int f = 0; f < files.Length; f++)
                {
                    TryLoadExternalLanguageFile(files[f]);
                }
            }
            catch
            {
            }
        }

        private static void TryLoadExternalLanguageFile(string filePath)
        {
            try
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim().Replace("\\r\\n", "\r\n").Replace("\\n", "\r\n");
                    dict[k] = v;
                }

                string code;
                if (!dict.TryGetValue("Meta.Code", out code) || string.IsNullOrWhiteSpace(code))
                {
                    code = Path.GetFileNameWithoutExtension(filePath);
                }

                string name;
                if (!dict.TryGetValue("Meta.Name", out name) || string.IsNullOrWhiteSpace(name))
                {
                    name = code.ToUpperInvariant();
                }

                string badge;
                if (!dict.TryGetValue("Meta.Badge", out badge) || string.IsNullOrWhiteSpace(badge))
                {
                    badge = code.Length > 3 ? code.Substring(0, 2).ToUpperInvariant() : code.ToUpperInvariant();
                }

                RegisterLanguage(new LanguagePack(code, name, badge, dict));
            }
            catch
            {
            }
        }

        public static string T(string key)
        {
            string val;
            if (_currentPack != null && _currentPack.TryGet(key, out val))
            {
                return val;
            }
            if (_fallbackPack != null && _fallbackPack.TryGet(key, out val))
            {
                return val;
            }
            return key ?? string.Empty;
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = T(key);
            try
            {
                return string.Format(CultureInfo.InvariantCulture, pattern, args);
            }
            catch
            {
                return pattern;
            }
        }

        private static LanguagePack BuildItalianPack()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Window & Header
                { "App.WindowTitle", "EDM Results — Telemetria Ufficiale Atletica Leggera" },
                { "App.HeaderSubtitle", "Sistema Ufficiale Telemetria Atletica Leggera" },
                { "App.LanguageLabel", "Lingua" },
                { "App.LanguageTooltip", "Cambia lingua interfaccia" },

                // Navigation Tabs
                { "Nav.LiveMeasure", "Misura Live" },
                { "Nav.History", "Storico Misure" },
                { "Nav.Setup", "Configurazione" },
                { "Nav.Serial", "Porta Seriale" },

                // Disciplines
                { "Disc.ShotPut", "Lancio del Peso" },
                { "Disc.Discus", "Lancio del Disco" },
                { "Disc.Javelin", "Lancio del Giavellotto" },
                { "Disc.Hammer", "Lancio del Martello" },
                { "Disc.HorizontalJumps", "Salti in Estensione (Lungo / Triplo)" },

                // Evaluation & Marks
                { "Mark.Foul", "NULLO" },
                { "Mark.Valid", "VALIDO" },
                { "Mark.BestBadge", "MIGLIORE" },
                { "Ledger.DefaultDiscipline", "Gara" },
                { "Ledger.EmptySession", "Nessuna misura registrata in questa sessione." },
                { "Profile.Saved", "Profilo Salvato" },
                { "Eval.IncompleteCalibration", "Calibrazione incompleta" },
                { "Eval.InvalidBaseline", "Linea di battuta P1-P2 non valida" },
                { "Eval.InsideCircleRadius", "Punto acquisito interno al raggio di offset" },
                { "Eval.ProjectionError", "Errore geometrico di proiezione" },
                { "Eval.DiagnosticFormat", "Lordo {0:0.000} m | Offset {1:+0.000;-0.000;0.000} m | Netto {2:0.000} m" },

                // Live Measure Page
                { "Live.BestMarkChip", "Miglior Misura: {0}" },
                { "Live.UncalibratedBanner", "Calibrazione pedana/battuta non completata. Acquisisci i punti di riferimento in Configurazione prima di misurare." },
                { "Live.OpenSetupBtn", "Apri Configurazione" },
                { "Live.NoOffsetBadge", "NESSUN OFFSET (0 MM)" },
                { "Live.RadiusOffsetBadge", "OFFSET RAGGIO {0:+0;-0} MM" },
                { "Live.ReadySubline", "Pronto per ricevere la collimazione dalla stazione totale" },
                { "Live.UncalibratedSubline", "Lettura ricevuta ma calibrazione P1/P2 incompleta — apri Configurazione." },
                { "Live.TelemetryWaiting", "Telemetria: in attesa di pacchetto seriale (MATO 21d DMS / Sokkia 23d Gon / Leica GSI / Nikon)" },
                { "Live.RadarHeader", " Geometria Campo 2D (EDM / Pedana / Bersaglio)" },
                { "Live.MiniLogHeader", " Ultime Misure Registrate" },
                { "Live.FoulBtn", "PROVA NULLA  (F2)" },
                { "Live.FoulSubline", "Prova annullata dal giudice (F2)" },
                { "Live.SimulateBtn", "Simula Lettura EDM  (F5)" },
                { "Live.CopyBtn", "Copia Misura  (Ctrl+C)" },
                { "Live.CopiedSubline", "Misura \"{0}\" copiata negli appunti." },

                // Radar Nodes
                { "Radar.EdmOrigin", "EDM (0,0)" },
                { "Radar.CenterP1", "Centro P1" },
                { "Radar.Point1", "P1" },
                { "Radar.Point2", "P2" },
                { "Radar.Target", "Bersaglio" },

                // History Page
                { "History.Title", "Storico Ufficiale Misure (Misure.txt)" },
                { "History.StatsFormat", "Totale prove registrate: {0}  |  Miglior misura della sessione: {1}" },
                { "History.ExportCsvBtn", "Esporta CSV" },
                { "History.CopyAllBtn", "Copia Tutto" },
                { "History.ClearBtn", "Svuota Storico" },
                { "History.ConfirmClearText", "Vuoi davvero cancellare tutte le misure salvate in Misure.txt?" },
                { "History.ConfirmClearTitle", "Conferma Cancellazione Storico" },
                { "History.ExportDialogTitle", "Esporta Storico Misure in CSV" },
                { "History.ExportDialogFilter", "File CSV (*.csv)|*.csv|Tutti i file (*.*)|*.*" },
                { "History.CsvHeader", "Prova;Ora;Disciplina;MisuraUfficiale;Metri;Stato" },
                { "History.ExportErrorText", "Errore durante l'esportazione CSV: {0}" },
                { "History.ExportErrorTitle", "Errore Esportazione" },

                // Setup / Calibration Page
                { "Setup.LeftTitle", "Disciplina e Raggio Pedana / Battuta" },
                { "Setup.LeftSubtitle", "Seleziona la specialità atletica. Il raggio standard World Athletics / FIDAL viene impostato automaticamente ma può essere rifinito al millimetro." },
                { "Setup.LanguageCaption", "Lingua Interfaccia / Language" },
                { "Setup.DisciplineCaption", "Specialità di Gara" },
                { "Setup.OffsetCaption", "Offset Raggio Cerchio / Arco (mm)" },
                { "Setup.PresetShotHammer", "Peso/Mart. 1068" },
                { "Setup.PresetDiscus", "Disco 1250" },
                { "Setup.PresetJavelin", "Giav. 8000" },
                { "Setup.PresetJumps", "Salti 0" },
                { "Setup.LockCheckbox", "Blocca modifiche accidentali a calibrazione e offset" },
                { "Setup.LivePreviewEmpty", "Ultima telemetria ricevuta: nessuna lettura in cache." },
                { "Setup.LivePreviewReady", "Ultima lettura pronta per calibrazione:\r\n{0}" },

                { "Setup.RightTitle", "Calibrazione Punti di Riferimento (P1 / P2)" },
                { "Setup.RightSubtitle", "Per i Lanci acquisisci il centro pedana (P1). Per i Salti in Estensione acquisisci due punti (P1 e P2) lungo la linea di battuta." },
                { "Setup.P1Header", "Punto 1 (Centro Pedana / Estremo A Battuta)" },
                { "Setup.P2Header", "Punto 2 (Estremo B Linea di Battuta Salti)" },
                { "Setup.StatusP1Calibrated", "P1 Calibrato" },
                { "Setup.StatusP2Calibrated", "P2 Calibrato" },
                { "Setup.StatusToAcquire", "Da Acquisire" },
                { "Setup.StatusNotRequired", "Non Richiesto" },
                { "Setup.P1EmptyText", "Nessuna coordinata acquisita per il Punto 1." },
                { "Setup.P2NotRequiredText", "La specialità selezionata utilizza il solo centro pedana (P1)." },
                { "Setup.P2EmptyText", "Acquisisci il secondo estremo della linea di battuta (P2)." },
                { "Setup.AcquireP1Btn", "Acquisisci P1..." },
                { "Setup.AcquireP2Btn", "Acquisisci P2..." },
                { "Setup.UseLastReadingBtn", "Usa Ultima Lettura" },
                { "Setup.SwapBtn", "Scambia P1 ↔ P2" },
                { "Setup.ClearCalibrationBtn", "Azzera Calibrazione" },
                { "Setup.BadgeCalibrationRequired", "Calibrazione Richiesta" },
                { "Setup.ModeBaselineActive", "Battuta P1-P2 attiva (ampiezza {0:0.000} m)" },
                { "Setup.ModeCenterActive", "Centro Pedana P1 attivo (Dh = {0:0.000} m)" },
                { "Setup.GeometryActivePrefix", "Stato geometria: {0}" },
                { "Setup.GeometryIncompleteFormat", "Stato geometria: calibrazione incompleta per {0}" },
                { "Setup.Point1Name", "Punto 1" },
                { "Setup.Point2Name", "Punto 2" },
                { "Setup.InvalidBaselinePointsTooClose", "Il Punto 1 e il Punto 2 sono troppo vicini (< 5 cm). Collima due estremi distinti della linea di battuta." },
                { "Setup.InvalidBaselineTitle", "Geometria Battuta Non Valida" },

                // Guided Calibration Modal
                { "Modal.WindowTitle", "Acquisizione Guidata {0}" },
                { "Modal.HeaderTitle", "Acquisizione {0}" },
                { "Modal.HintText", "Collimare il prisma sul punto di riferimento e inviare la lettura dalla stazione totale (oppure confermare l'ultima lettura valida)." },
                { "Modal.WaitingTelemetry", "In attesa di telemetria dalla stazione totale..." },
                { "Modal.ReadingAcquired", "Lettura acquisita — pronta per la conferma" },
                { "Modal.MetricHorizGon", "Angolo Orizzontale (Gon)" },
                { "Modal.MetricDms", "Angolo Orizz. / Vert. (DMS)" },
                { "Modal.MetricZenithGon", "Angolo Zenitale (Gon)" },
                { "Modal.MetricSlopeDist", "Distanza Inclinata (Ds)" },
                { "Modal.MetricHorizDist", "Distanza Orizzontale (Dh)" },
                { "Modal.ProtocolPrefix", "Protocollo rilevato: {0}" },
                { "Modal.ConfirmBtn", "Conferma Punto" },
                { "Modal.CancelBtn", "Annulla" },

                // Serial Port & Telemetry Monitor Page
                { "Serial.LeftTitle", "Parametri Connessione RS-232 / USB" },
                { "Serial.LeftSubtitle", "La connessione rimane sempre attiva durante il cambio scheda per non perdere alcuna misura inviata dalla stazione totale." },
                { "Serial.ComPortLabel", "Porta COM" },
                { "Serial.BaudRateLabel", "Velocità (Baud Rate)" },
                { "Serial.DataBitsLabel", "Bit di Dati" },
                { "Serial.ParityLabel", "Parità" },
                { "Serial.StopBitsLabel", "Bit di Stop" },
                { "Serial.ProtocolLabel", "Protocollo EDM" },
                { "Serial.ParityNone", "Nessuna (None)" },
                { "Serial.ParityOdd", "Dispari (Odd)" },
                { "Serial.ParityEven", "Pari (Even)" },
                { "Serial.ProtoAuto", "Auto-Riconoscimento (MATO / Sokkia / GSI / Nikon)" },
                { "Serial.ProtoMato", "MATO MTS-602R+ (21 cifre DMS ° ' \")" },
                { "Serial.ProtoSokkia", "Sokkia / SET (23 cifre Gon)" },
                { "Serial.ProtoGsi", "Leica / Stonex GSI (Word 21/22/31)" },
                { "Serial.ProtoNikon", "Nikon Raw ASCII (HA/VA/SD)" },
                { "Serial.ApplyReconnectBtn", "Applica e Riconnetti Porta Seriale" },
                { "Serial.RefreshPortsBtn", "Aggiorna COM" },
                { "Serial.RightTitle", "Monitor Telemetria & Diagnostica Pacchetti" },
                { "Serial.EndpointSummaryFormat", "Configurazione: {0} @ {1} bps, {2} bit, Parità {3}, Stop {4}" },
                { "Serial.LastPacketHeader", "Ultimo Pacchetto Ricevuto & Decodifica:" },
                { "Serial.WaitingSerialData", "In attesa di dati sulla porta seriale..." },
                { "Serial.RawPacketFormat", "Pacchetto #{0} ({1} byte): \"{2}\"\r\nOra ricezione: {3:HH:mm:ss.fff}" },
                { "Serial.DecodedPacketFormat", "Pacchetto #{0}: \"{1}\"\r\nDecodificato: {2}" },
                { "Serial.SimulatorTitle", "Verifica Decodifica / Iniezione Pacchetto Raw (MATO / Sokkia / GSI)" },
                { "Serial.InjectBtn", "Inietta Frame" },
                { "Serial.TestMatoP1Btn", "Test MATO P1 (2,53m)" },
                { "Serial.TestMatoShotBtn", "Test MATO Lancio (6,53m)" },
                { "Serial.TestSokkiaBtn", "Test Sokkia 23d" },
                { "Serial.StatusActive", "{0} attiva ({1} bps)" },
                { "Serial.StatusConnected", "{0} connessa ({1} bps)" },
                { "Serial.StatusUnavailable", "{0} non disponibile: {1}" },
                { "Serial.StatusDisconnected", "Porta seriale disconnessa" },
                { "Serial.BadgeOfflineFormat", "Seriale Offline ({0})" }
            };

            return new LanguagePack("it", "Italiano", "IT", d);
        }

        private static LanguagePack BuildEnglishPack()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Window & Header
                { "App.WindowTitle", "EDM Results — Official Track & Field Telemetry" },
                { "App.HeaderSubtitle", "Official Track & Field Total Station Telemetry" },
                { "App.LanguageLabel", "Language" },
                { "App.LanguageTooltip", "Switch interface language" },

                // Navigation Tabs
                { "Nav.LiveMeasure", "Live Measure" },
                { "Nav.History", "Measure History" },
                { "Nav.Setup", "Configuration" },
                { "Nav.Serial", "Serial Port" },

                // Disciplines
                { "Disc.ShotPut", "Shot Put" },
                { "Disc.Discus", "Discus Throw" },
                { "Disc.Javelin", "Javelin Throw" },
                { "Disc.Hammer", "Hammer Throw" },
                { "Disc.HorizontalJumps", "Horizontal Jumps (Long / Triple)" },

                // Evaluation & Marks
                { "Mark.Foul", "FOUL" },
                { "Mark.Valid", "VALID" },
                { "Mark.BestBadge", "BEST" },
                { "Ledger.DefaultDiscipline", "Event" },
                { "Ledger.EmptySession", "No measurements recorded in this session." },
                { "Profile.Saved", "Saved Profile" },
                { "Eval.IncompleteCalibration", "Incomplete calibration" },
                { "Eval.InvalidBaseline", "Invalid P1-P2 take-off baseline" },
                { "Eval.InsideCircleRadius", "Target acquired inside offset radius" },
                { "Eval.ProjectionError", "Geometric projection error" },
                { "Eval.DiagnosticFormat", "Gross {0:0.000} m | Offset {1:+0.000;-0.000;0.000} m | Net {2:0.000} m" },

                // Live Measure Page
                { "Live.BestMarkChip", "Best Mark: {0}" },
                { "Live.UncalibratedBanner", "Circle/board calibration is incomplete. Acquire reference points in Configuration before measuring." },
                { "Live.OpenSetupBtn", "Open Configuration" },
                { "Live.NoOffsetBadge", "NO OFFSET (0 MM)" },
                { "Live.RadiusOffsetBadge", "RADIUS OFFSET {0:+0;-0} MM" },
                { "Live.ReadySubline", "Ready to receive sighting from the total station" },
                { "Live.UncalibratedSubline", "Reading received, but P1/P2 calibration is incomplete — open Configuration." },
                { "Live.TelemetryWaiting", "Telemetry: waiting for serial packet (MATO 21d DMS / Sokkia 23d Gon / Leica GSI / Nikon)" },
                { "Live.RadarHeader", " 2D Field Geometry (EDM / Circle / Target)" },
                { "Live.MiniLogHeader", " Latest Recorded Marks" },
                { "Live.FoulBtn", "FOUL ATTEMPT  (F2)" },
                { "Live.FoulSubline", "Attempt marked as foul by official (F2)" },
                { "Live.SimulateBtn", "Simulate EDM Shot  (F5)" },
                { "Live.CopyBtn", "Copy Mark  (Ctrl+C)" },
                { "Live.CopiedSubline", "Mark \"{0}\" copied to clipboard." },

                // Radar Nodes
                { "Radar.EdmOrigin", "EDM (0,0)" },
                { "Radar.CenterP1", "Center P1" },
                { "Radar.Point1", "P1" },
                { "Radar.Point2", "P2" },
                { "Radar.Target", "Target" },

                // History Page
                { "History.Title", "Official Measurement Ledger (Misure.txt)" },
                { "History.StatsFormat", "Total recorded attempts: {0}  |  Session best mark: {1}" },
                { "History.ExportCsvBtn", "Export CSV" },
                { "History.CopyAllBtn", "Copy All" },
                { "History.ClearBtn", "Clear History" },
                { "History.ConfirmClearText", "Are you sure you want to delete all recorded measurements in Misure.txt?" },
                { "History.ConfirmClearTitle", "Confirm Clear History" },
                { "History.ExportDialogTitle", "Export Measurement History to CSV" },
                { "History.ExportDialogFilter", "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*" },
                { "History.CsvHeader", "Attempt;Time;Discipline;OfficialMark;Meters;Status" },
                { "History.ExportErrorText", "Error while exporting CSV: {0}" },
                { "History.ExportErrorTitle", "Export Error" },

                // Setup / Calibration Page
                { "Setup.LeftTitle", "Discipline & Circle / Board Offset" },
                { "Setup.LeftSubtitle", "Select the field event discipline. Standard World Athletics radius is applied automatically and can be fine-tuned in millimeters." },
                { "Setup.LanguageCaption", "Interface Language / Lingua" },
                { "Setup.DisciplineCaption", "Competition Event" },
                { "Setup.OffsetCaption", "Circle / Arc Radius Offset (mm)" },
                { "Setup.PresetShotHammer", "Shot/Ham. 1068" },
                { "Setup.PresetDiscus", "Discus 1250" },
                { "Setup.PresetJavelin", "Javelin 8000" },
                { "Setup.PresetJumps", "Jumps 0" },
                { "Setup.LockCheckbox", "Lock calibration and offset against accidental changes" },
                { "Setup.LivePreviewEmpty", "Latest telemetry: no reading cached yet." },
                { "Setup.LivePreviewReady", "Latest reading ready for calibration:\r\n{0}" },

                { "Setup.RightTitle", "Reference Point Calibration (P1 / P2)" },
                { "Setup.RightSubtitle", "For Throws acquire the circle center (P1). For Horizontal Jumps acquire two points (P1 and P2) along the take-off line." },
                { "Setup.P1Header", "Point 1 (Circle Center / Take-Off Board End A)" },
                { "Setup.P2Header", "Point 2 (Take-Off Board End B — Jumps)" },
                { "Setup.StatusP1Calibrated", "P1 Calibrated" },
                { "Setup.StatusP2Calibrated", "P2 Calibrated" },
                { "Setup.StatusToAcquire", "Required" },
                { "Setup.StatusNotRequired", "Not Required" },
                { "Setup.P1EmptyText", "No coordinates acquired for Point 1." },
                { "Setup.P2NotRequiredText", "Selected discipline uses single circle center point (P1) only." },
                { "Setup.P2EmptyText", "Acquire the second end of the take-off board line (P2)." },
                { "Setup.AcquireP1Btn", "Acquire P1..." },
                { "Setup.AcquireP2Btn", "Acquire P2..." },
                { "Setup.UseLastReadingBtn", "Use Last Reading" },
                { "Setup.SwapBtn", "Swap P1 ↔ P2" },
                { "Setup.ClearCalibrationBtn", "Reset Calibration" },
                { "Setup.BadgeCalibrationRequired", "Calibration Required" },
                { "Setup.ModeBaselineActive", "Baseline P1-P2 active (span {0:0.000} m)" },
                { "Setup.ModeCenterActive", "Circle Center P1 active (Dh = {0:0.000} m)" },
                { "Setup.GeometryActivePrefix", "Geometry status: {0}" },
                { "Setup.GeometryIncompleteFormat", "Geometry status: incomplete calibration for {0}" },
                { "Setup.Point1Name", "Point 1" },
                { "Setup.Point2Name", "Point 2" },
                { "Setup.InvalidBaselinePointsTooClose", "Point 1 and Point 2 are too close (< 5 cm). Sight two distinct points along the take-off line." },
                { "Setup.InvalidBaselineTitle", "Invalid Baseline Geometry" },

                // Guided Calibration Modal
                { "Modal.WindowTitle", "Guided Capture — {0}" },
                { "Modal.HeaderTitle", "Acquire {0}" },
                { "Modal.HintText", "Sight the prism on the reference point and trigger a measurement on the total station (or confirm the last valid reading)." },
                { "Modal.WaitingTelemetry", "Waiting for telemetry from total station..." },
                { "Modal.ReadingAcquired", "Reading acquired — ready to confirm" },
                { "Modal.MetricHorizGon", "Horizontal Angle (Gon)" },
                { "Modal.MetricDms", "Horiz. / Vert. Angle (DMS)" },
                { "Modal.MetricZenithGon", "Zenith Angle (Gon)" },
                { "Modal.MetricSlopeDist", "Slope Distance (Ds)" },
                { "Modal.MetricHorizDist", "Horizontal Distance (Dh)" },
                { "Modal.ProtocolPrefix", "Detected protocol: {0}" },
                { "Modal.ConfirmBtn", "Confirm Point" },
                { "Modal.CancelBtn", "Cancel" },

                // Serial Port & Telemetry Monitor Page
                { "Serial.LeftTitle", "RS-232 / USB Connection Parameters" },
                { "Serial.LeftSubtitle", "The serial connection stays active across tabs so no measurement sent by the total station is ever missed." },
                { "Serial.ComPortLabel", "COM Port" },
                { "Serial.BaudRateLabel", "Baud Rate (bps)" },
                { "Serial.DataBitsLabel", "Data Bits" },
                { "Serial.ParityLabel", "Parity" },
                { "Serial.StopBitsLabel", "Stop Bits" },
                { "Serial.ProtocolLabel", "EDM Protocol" },
                { "Serial.ParityNone", "None" },
                { "Serial.ParityOdd", "Odd" },
                { "Serial.ParityEven", "Even" },
                { "Serial.ProtoAuto", "Auto-Detect (MATO / Sokkia / GSI / Nikon)" },
                { "Serial.ProtoMato", "MATO MTS-602R+ (21-digit DMS ° ' \")" },
                { "Serial.ProtoSokkia", "Sokkia / SET (23-digit Gon)" },
                { "Serial.ProtoGsi", "Leica / Stonex GSI (Word 21/22/31)" },
                { "Serial.ProtoNikon", "Nikon Raw ASCII (HA/VA/SD)" },
                { "Serial.ApplyReconnectBtn", "Apply & Reconnect Serial Port" },
                { "Serial.RefreshPortsBtn", "Refresh COM" },
                { "Serial.RightTitle", "Telemetry Monitor & Packet Diagnostics" },
                { "Serial.EndpointSummaryFormat", "Configuration: {0} @ {1} bps, {2} bits, Parity {3}, Stop {4}" },
                { "Serial.LastPacketHeader", "Last Received Packet & Decoded Output:" },
                { "Serial.WaitingSerialData", "Waiting for incoming serial data..." },
                { "Serial.RawPacketFormat", "Packet #{0} ({1} bytes): \"{2}\"\r\nReceived at: {3:HH:mm:ss.fff}" },
                { "Serial.DecodedPacketFormat", "Packet #{0}: \"{1}\"\r\nDecoded: {2}" },
                { "Serial.SimulatorTitle", "Decoder Verification / Raw Frame Injection (MATO / Sokkia / GSI)" },
                { "Serial.InjectBtn", "Inject Frame" },
                { "Serial.TestMatoP1Btn", "Test MATO P1 (2.53m)" },
                { "Serial.TestMatoShotBtn", "Test MATO Throw (6.53m)" },
                { "Serial.TestSokkiaBtn", "Test Sokkia 23d" },
                { "Serial.StatusActive", "{0} active ({1} bps)" },
                { "Serial.StatusConnected", "{0} connected ({1} bps)" },
                { "Serial.StatusUnavailable", "{0} unavailable: {1}" },
                { "Serial.StatusDisconnected", "Serial port disconnected" },
                { "Serial.BadgeOfflineFormat", "Serial Offline ({0})" }
            };

            return new LanguagePack("en", "English", "EN", d);
        }
    }
}
