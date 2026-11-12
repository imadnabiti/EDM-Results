<p align="center">
  <img src="assets/EDM_Results.gif" width="320" alt="EDM Results Preview">
</p>

<h1 align="center">EDM Results</h1>

<p align="center">
  <strong>Official Track &amp; Field Total Station Telemetry &amp; Measurement Software</strong><br>
  Connects Electronic Distance Meters (EDM) / Total Stations via RS-232 or USB-to-Serial to automate field-event measurement acquisition, geometric projection, and World Athletics rule-compliant result formatting.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Version-2.2.0-blue" alt="Version 2.2.0">
  <img src="https://img.shields.io/badge/Platform-Windows-lightgrey" alt="Windows">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.0%2B-purple" alt=".NET Framework 4.0+">
  <img src="https://img.shields.io/badge/License-MIT-green" alt="MIT License">
</p>

---

## Overview

**EDM Results** is a lightweight, zero-dependency Windows Forms desktop application built for track and field officials and timing teams. It receives real-time polar sightings (horizontal angle, vertical/zenith angle, and slope distance) directly from a total station, transforms them relative to calibrated circle centers or jump take-off baselines, applies World Athletics / FIDAL centimeter rounding-down rules, and logs every attempt to a persistent competition ledger.

### Key Features

- **Multi-Protocol Serial Telemetry:** Persistent RS-232/USB listener across all tabs with automatic protocol classification for **MATO**, **Sokkia/SET**, **Leica/Stonex/GeoMax GSI**, and **Nikon Raw ASCII**, plus an idle-gap burst timer (`55 ms`) for un-terminated frames.
- **5 Track & Field Disciplines:** Built-in geometry and standard radius presets for Shot Put, Discus Throw, Javelin Throw, Hammer Throw, and Horizontal Jumps (Long / Triple), with millimeter fine-tuning.
- **2D Field Geometry Radar:** Live planar visualization showing the EDM origin `(0,0)`, calibrated anchor(s) `P1`/`P2`, throwing circle/arc or take-off baseline, and acquired target point.
- **Guided Calibration & Safety Lock:** Modal guided capture for `P1` and `P2`, `Use Last Reading`, `Swap P1 ↔ P2`, and a calibration lock toggle to prevent accidental mid-competition changes.
- **Official Ledger & Export:** Automatic timestamped logging to `Misure.txt`, session best-mark tracking, one-click clipboard copy, and semicolon-delimited UTF-8 **CSV export**.
- **Built-in Simulator & Diagnostics:** Raw frame inspector and packet injection simulator (including one-click MATO and Sokkia test frames and `F5` live-shot simulation) for testing without hardware attached.
- **Extensible Localization (i18n):** Built-in **English (`en`)** and **Italian (`it`)** with live switching, plus automatic discovery of custom `Languages/*.lang` packs.

---

## Supported Disciplines & Geometry

| Code (`Tipo`) | Discipline | Geometry Model | Default Offset | Sector Angle |
| :---: | :--- | :--- | :---: | :---: |
| `0` | **Shot Put** | Single Center Point (`P1`) | `1068 mm` | `34.92°` |
| `1` | **Discus Throw** | Single Center Point (`P1`) | `1250 mm` | `34.92°` |
| `2` | **Javelin Throw** | Single Center Point (`P1`) | `8000 mm` | `28.96°` |
| `3` | **Hammer Throw** | Single Center Point (`P1`) | `1068 mm` | `34.92°` |
| `4` | **Horizontal Jumps (Long / Triple)** | Two-Point Baseline (`P1`–`P2` orthogonal projection) | `0 mm` | — |

### How Official Marks Are Computed

1. **Planar Projection:** Each polar reading $(H_z, V_z, D_s)$ is projected onto the horizontal plane:
   $$D_h = |D_s \cdot \sin(V_z)|, \quad E = D_h \cos(H_z), \quad N = D_h \sin(H_z)$$
2. **Event Geometry:**
   - **Throws (`SingleCenterPoint`):** Computes the 2D Euclidean distance from the calibrated circle/arc center `P1` to the target, then subtracts the configured radius offset (e.g., `1068 mm`, `1250 mm`, `8000 mm`).
   - **Horizontal Jumps (`TwoPointBaseline`):** Constructs unit tangent and normal vectors from take-off board endpoints `P1` and `P2` and computes the orthogonal perpendicular distance from the target to the take-off line (minus any custom millimeter offset).
3. **World Athletics Rounding:** Net distances in meters are rounded **down** to the nearest whole centimeter (`Math.Floor((netMeters + 1e-9) * 100)`) and formatted as `XX,XX m`.

---

## Supported Total Station Protocols

Set **EDM Protocol** to **Auto-Detect** (`0`) or force a specific parser in the **Serial Port** tab:

| Protocol (`Seriale.Protocol`) | Instrument / Format | Frame Grammar & Units |
| :---: | :--- | :--- |
| `0` | **Auto-Detect** | Automatically classifies incoming frames across all supported grammars |
| `1` | **MATO MTS-602R+** | 21-digit packed block: `DDDDDDD` (mm) + `VVVVVVV` (`DDDMMSS`) + `HHHHHHH` (`DDDMMSS`), with $10^{-4}$ Gon fallback |
| `2` | **Sokkia / SET** | 23-digit continuous block: `DDDDDDD` (mm) + `VVVVVVVV` ($10^{-4}$ Gon) + `HHHHHHHH` ($10^{-4}$ Gon) |
| `3` | **Leica / Stonex / GeoMax GSI** | Space-delimited GSI-8 / GSI-16 word blocks (`21..` Hz, `22..` V, `31..` SD with unit scaling) |
| `4` | **Nikon Raw ASCII** | Delimited key-value tokens containing `HA`, `VA`, and `SD` markers |

---

## Getting Started

### Prerequisites

- **OS:** Windows (DPI-aware WinForms desktop application)
- **Runtime:** .NET Framework 4.0 or later (preinstalled on Windows 10/11)

### Run the Prebuilt Executable

Launch `EDMResults\EDMResults.exe` directly. Keep `Config.txt` and `EDMRESULTS.ico` in the same directory as the executable.

### Build from Source

The project uses only standard .NET Framework assemblies (`System.Windows.Forms`, `System.Drawing`, `System.IO.Ports`) with no external dependencies. You can compile it directly with the Windows `.NET Framework 4` C# compiler:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo /target:winexe /optimize+ `
  /out:"EDMResults\EDMResults.exe" `
  /win32icon:"EDMResults\EDMRESULTS.ico" `
  "EDMResults\*.cs"
```

---

## Competition Workflow & Shortcuts

1. **Connect Total Station (`Serial Port` tab):** Select your `COM` port, baud rate (`1200`–`115200`, default `9600`), data bits (`7`/`8`), parity, stop bits, and protocol, then click **Apply & Reconnect Serial Port**.
2. **Select Event & Calibrate (`Configuration` tab):**
   - Choose the competition discipline (standard World Athletics radius is applied automatically; adjust in `1 mm` steps if needed).
   - Click **Acquire P1...** (circle center for Throws, or take-off board end A for Jumps), sight the prism, and trigger a shot on the total station.
   - For Horizontal Jumps, repeat for **Acquire P2...** (take-off board end B, at least `5 cm` from `P1`).
   - Enable **Lock calibration and offset** before competition starts.
3. **Measure (`Live Measure` tab):** Trigger shots on the total station to see the official mark, diagnostic breakdown (`Gross | Offset | Net`), 2D Field Geometry Radar, and recent attempts.
4. **Export (`Measure History` tab):** Review all recorded attempts in `Misure.txt`, copy the list, or export a `;`-delimited UTF-8 `.csv` file.

### Keyboard Shortcuts

| Shortcut | Action |
| :---: | :--- |
| `F2` | Record a **FOUL** attempt in the ledger |
| `F5` | Simulate an EDM shot (auto-seeds sample MATO calibration anchors if uncalibrated) |
| `Ctrl+C` | Copy the current official mark to the clipboard (on the **Live Measure** tab) |

---

## Configuration & Persistence

All settings and measurements are stored locally alongside `EDMResults.exe`:

- **`Config.txt`** — UTF-8 `key=value` file automatically loaded on startup and saved on change/exit:
  - `Language` (`it`, `en`, or custom `.lang` code)
  - `Tipo` (`0`–`4` discipline code)
  - `Lock` (`true` / `false`)
  - `OffsetZero` (radius offset in millimeters)
  - `Punto1`, `Punto1.O`, `Punto1.V`, `Punto1.D` (calibrated Point 1 status, horizontal/vertical Gon, slope distance in meters)
  - `Punto2`, `Punto2.O`, `Punto2.V`, `Punto2.D` (calibrated Point 2 coordinates for jumps)
  - `Seriale.Port`, `Seriale.Speed`, `Seriale.Bits`, `Seriale.Parity`, `Seriale.StopBits`, `Seriale.Protocol`
- **`Misure.txt`** — Plain-text UTF-8 ledger storing timestamped attempts (`HH:mm  <mark>`) across restarts.

---

## Localization & Custom Languages

Built-in interface languages are **Italian (`it`)** and **English (`en`)**, switchable from the top header bar or the **Configuration** tab.

To add a new language without recompiling:

1. Create a `Languages` folder next to `EDMResults.exe`.
2. Add a UTF-8 `.lang` file (for example, `Languages\fr.lang`) with `key=value` entries and metadata headers:
   ```ini
   Meta.Code=fr
   Meta.Name=Français
   Meta.Badge=FR
   App.WindowTitle=EDM Results — Télémétrie Officielle d'Athlétisme
   Nav.LiveMeasure=Mesure en Direct
   ```
3. Restart the application; missing keys automatically fall back to English (`en`). See [Localization.cs](EDMResults/Localization.cs) for the full list of translation keys.

---

## Repository Structure

```text
├── assets/
│   ├── EDMRESULTS.jpg
│   └── EDM_Results.gif           # UI preview animation
├── EDMResults/
│   ├── Config.txt                # Default runtime configuration
│   ├── EDMRESULTS.ico            # Application icon
│   ├── EDMResults.exe            # Prebuilt Windows executable (v2.2.0)
│   ├── GeometryEngine.cs         # Polar math, 2D baseline projector, WA rounding
│   ├── Localization.cs           # Built-in IT/EN packs & external *.lang loader
│   ├── MainWindow.cs             # WinForms UI, radar canvas, dialogs, CSV export
│   ├── Program.cs                # DPI-aware application entry point & assembly info
│   ├── SerialTransceiver.cs      # Thread-safe serial controller & idle-gap framer
│   ├── SettingsRepository.cs     # Config.txt & Misure.txt persistence
│   └── TotalStationParser.cs     # MATO, Sokkia, Leica/Stonex GSI & Nikon parsers
├── Old Version/                  # Legacy Italian/English & MisuraPlus archives
├── LICENSE                       # MIT License
└── README.md
```

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

