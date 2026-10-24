# ScreenMover

**Fenster im Blick. Per Drag-and-drop auf einen anderen Bildschirm verschieben oder ausblenden.** Ein visueller Fenstermanager für Windows mit mehreren Bildschirmen.

[简体中文](README.md) · [English](README.en.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · [Español](README.es.md)

[Aktuelle Version](https://github.com/lbyh0116/ScreenMover/releases/latest) · [Problem melden](https://github.com/lbyh0116/ScreenMover/issues/new/choose)

## Herunterladen und starten

Aktuelle stabile Version: **v0.2.2 · Windows x64, portabel**.

1. [ScreenMover-v0.2.2-win-x64.zip](https://github.com/lbyh0116/ScreenMover/releases/download/v0.2.2/ScreenMover-v0.2.2-win-x64.zip) herunterladen.
2. Das gesamte Archiv entpacken und `ScreenMover.exe` starten. Alle mitgelieferten Dateien müssen zusammenbleiben.
3. Vor einem Upgrade die alte Version über das Symbol im Infobereich beenden. Einstellungen, Fensterreihenfolge und Einträge für ausgeblendete Fenster bleiben erhalten.

Die benötigten Laufzeitkomponenten sind enthalten; .NET muss nicht separat installiert werden. Das Paket ist nicht kommerziell codesigniert. Eine Microsoft-Store-Version ist noch nicht verfügbar. Die Oberfläche unterstützt vereinfachtes Chinesisch, Englisch, Deutsch, Französisch und Spanisch. Standardmäßig folgt sie der Windows-Anzeigesprache; für andere Sprachen wird Englisch verwendet. Die Sprache lässt sich in den Einstellungen ändern und wird nach einem Neustart übernommen.

## Funktionen

- **Visuelle Übersicht:** Bildschirm- und Fenstervorschauen als Galerie. Ist keine Aufnahme möglich, erscheint das Anwendungssymbol.
- **Verschieben per Drag-and-drop:** Fenster auf die Karte des Zielbildschirms ziehen. Mehrfachauswahl, gemeinsames Verschieben und Rückgängigmachen des letzten Verschiebens werden unterstützt.
- **Schnellzugriff im Infobereich:** kompakte Fensterliste neben den Bildschirmvorschauen; ein Klick außerhalb schließt das Panel.
- **Fenster ausblenden und zurückholen:** Auf unterstützten Windows-Builds wird ein Fenster auf einen eigenen, nach der Anwendung benannten virtuellen Desktop verschoben und nach Möglichkeit minimiert. Es lässt sich im Hauptfenster zurückholen. Verweigert die Anwendung das Minimieren, bleibt der bereits erfolgte Desktopwechsel bestehen. Manuelles Zurückverschieben wird automatisch erkannt.
- **Persönliche Einstellungen:** gespeicherte Fensterreihenfolge, heller und dunkler Modus, System- oder eigene Akzentfarbe, frei wählbare Tastenkombinationen und automatisches Speichern.
- **Updates in der App:** GitHub Releases prüfen, das passende Paket herunterladen und verifizieren, anschließend installieren und neu starten.

Vorschauen sind einzelne Bildschirmaufnahmen, keine laufende Aufzeichnung. Standardmäßig werden sie beim Start, beim erneuten Aktivieren, jede Minute und mit F5 aktualisiert. Beim Schließen des Hauptfensters läuft ScreenMover normalerweise im Infobereich weiter. Zum vollständigen Beenden das Kontextmenü des Symbols verwenden.

## Bedienung

| Aktion | Bedienung |
| --- | --- |
| Fenster verschieben | Auf eine andere Bildschirmkarte ziehen oder eine Verschieben-Schaltfläche nutzen |
| Mehrere Fenster / Bereich auswählen | Strg / Umschalt + Klick |
| Fenster aktivieren | Doppelklick auf die Fensterkarte |
| Suchen / alles auswählen | Strg+F / Strg+A |
| Vorschauen aktualisieren | F5 |
| Abbrechen / Schnellzugriff schließen | Esc; ein Klick auf eine freie Fläche hebt die Auswahl auf |
| Schnellzugriff öffnen / aktives Fenster ausblenden | Separate globale Tastenkombinationen in den Einstellungen festlegen |

Im Schnellzugriff blendet das Ablegen auf dem bisherigen Bildschirm das Fenster aus. Das Ablegen auf einem anderen Bildschirm verschiebt es dorthin. Eine Einblendung zeigt vor dem Loslassen, welche Aktion ausgeführt wird.

## Kompatibilität

- **Windows 11 x64:** Mica, sofern verfügbar. Das Ausblenden auf einem eigenen virtuellen Desktop ist derzeit nur auf den Builds **26100 / 26200** aktiviert.
- **Windows 10 22H2 x64:** grundlegender Kompatibilitätspfad mit Acryl, sofern unterstützt. Die Prüfung auf echten Windows-10-Geräten steht noch aus. Das Ausblenden auf einem eigenen virtuellen Desktop ist deaktiviert.
- **x86 / ARM64:** noch keine veröffentlichten Pakete.

Geschützte, minimierte, GPU-gerenderte oder nicht reagierende Fenster liefern möglicherweise keine Vorschau. Zum Verwalten von Anwendungen mit erhöhten Rechten kann ein Neustart als Administrator nötig sein. Unterschiedliche DPI-Werte, Monitorwechsel im Betrieb und verschiedene Grafikgeräte benötigen weitere Tests. [Details zur Kompatibilität](docs/COMPATIBILITY.md) (Chinesisch).

## Updates und lokale Daten

In den Einstellungen nach Updates suchen und anschließend herunterladen und installieren. Downloads werden mit SHA-256 geprüft. Schlägt der Dateiaustausch fehl, wird eine Wiederherstellung versucht; bei Stromausfall oder einem späteren Absturz der neuen Version ist keine automatische Wiederherstellung garantiert. Updateprüfungen werden manuell gestartet, nicht im Hintergrund. [Weitere Informationen](docs/UPDATES.md) (Chinesisch).

Lokale Daten liegen unter `%LOCALAPPDATA%\ScreenMover`. Bildschirmaufnahmen bleiben im Arbeitsspeicher und werden nicht hochgeladen. Aktionsprotokolle und Wiederherstellungseinträge können Fenstertitel enthalten. Updatesicherungen liegen im Unterordner `updates`. [Datenschutz](docs/PRIVACY.md) (Chinesisch).

## Aus dem Quellcode erstellen

Voraussetzungen: Windows x64, **.NET SDK 10.0.400** und Zugriff auf NuGet.

```powershell
git clone https://github.com/lbyh0116/ScreenMover.git
cd ScreenMover
.\build.ps1 -PureTest
```

Ausgabe: `dist\ScreenMover\ScreenMover.exe`. Der Befehl erstellt das Veröffentlichungspaket und führt Regressionstests ohne Oberfläche aus; Ihre Fenster werden dabei nicht verändert. [Roadmap](docs/ROADMAP.md) · [Manuelle Tests](docs/MANUAL_TEST.md) (Chinesisch).

## Lizenz und Sicherheit

Für den eigenen Programmcode wurde noch keine Open-Source-Lizenz gewählt. Öffentlich einsehbarer Quellcode bedeutet nicht, dass eine Open-Source-Lizenz erteilt wurde. Der Hilfscode für virtuelle Desktops stammt aus [MScholtes/VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) und behält seine [MIT-Lizenz](https://github.com/lbyh0116/ScreenMover/blob/main/ScreenMover/Helpers/VirtualDesktop-LICENSE.txt). Für weitere Abhängigkeiten gelten deren jeweilige Lizenzen.

Sicherheitslücken bitte [vertraulich melden](https://github.com/lbyh0116/ScreenMover/security/advisories/new), sonstige Probleme über GitHub Issues. [Aktuelle Versionshinweise](CHANGELOG.md) (Chinesisch und Englisch).
