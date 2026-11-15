# 07-LehrerPlanNov

Unity-Projekt (Unity 6000.3) für den Unterricht: Flurplan der HTL Salzburg (Gang B und Gang C, 2. und 3. Stock) in 2D.
Die Räume sind anklickbar, beim Klick poppen die Infos zu den Lehrkräften auf, die in dem Raum sitzen.
Die Daten kommen live von der Schulhomepage (`https://www.htl-salzburg.ac.at/lehrerinnen.html`) und werden mit Regex aus dem HTML gefiltert.

## Arbeitsauftrag

- Raumsituation im 2. oder 3. Stock im Bereich Gang C und B erheben → Skizze in `Docs/Skizze_Flurplan.png`
- Räume/Türen in Unity nachbilden
- Türschilder mit Infos von der HTL-Salzburg-Lehrer-Homepage befüllen

## Starten

1. Projekt im Unity Hub öffnen
2. Szene `Assets/MyGame/Scenes/MainScene.unity` öffnen
3. Play drücken (Internetverbindung nötig)

Der Plan wird zur Laufzeit per Code aufgebaut (Objekt `LehrerPlan` in der Szene mit dem Script `LehrerPlanManager`).
Oben rechts kann man zwischen 2. und 3. Stock wechseln.

## Aufbau

| Datei | Aufgabe |
|---|---|
| `Assets/MyGame/scripts/LehrerPlanManager.cs` | Baut die UI auf, Stockwerk wechseln, Klick auf Raum → Daten laden → Popup |
| `Assets/MyGame/scripts/Web/HtlTeacherService.cs` | Web Request (UnityWebRequest) + Regex für Lehrerliste und Detailseiten |
| `Assets/MyGame/scripts/Data/TeacherInfo.cs` | Datenklasse für eine Lehrkraft, Hilfen für Raumnamen |
| `Assets/MyGame/scripts/Data/FloorPlanData.cs` | Grundriss aus der Skizze: Gänge, Wände, Räume pro Stockwerk |
| `Assets/MyGame/scripts/UI/RoomView.cs` | Ein Raum mit Tür und Türschild, anklickbar |
| `Assets/MyGame/scripts/UI/TeacherPopup.cs` | Popup mit den Infos (Name, Raum, Tel-DW, Sprechstunde, E-Mail) |
| `Assets/MyGame/scripts/UI/UiFactory.cs` | Helfer zum Erzeugen von uGUI-Elementen per Code |

Neue Räume einfach in `FloorPlanData.Slots` eintragen (Position, Tür-Seite, Raumnummer im 2. und 3. Stock).

## Regex

Ein Eintrag in der Lehrerliste sieht so aus:

```html
<a class="value" href="/lehrerinnen-details/holzer-roland-prof-dipl-ing-fh-b-211.html"><span class="text">Holzer Roland, Prof. Dipl.-Ing.&#40;FH&#41;</span></a>
```

Die Raumnummer steckt am Ende vom Link (`b-211`). Für die Vorlage aus dem Unterricht (`wantedRoom = "C 205"`) passt z.B.:

```csharp
string slug = wantedRoom.Replace(" ", "-").ToLower();            // "c-205"
Regex linkRegex = new Regex("href=\"(/lehrerinnen-details/[^\"]*-" + slug + "\\.html)\"");
string relativeUrl = link.Groups[1].Value;
```

Im Projekt wird die ganze Liste auf einmal gelesen (`TeacherLinkRegex`) und die Raumnummer mit `RoomInUrlRegex` aus dem Link geholt.
Auf der Detailseite liest `DetailFieldRegex` die Felder `Raum`, `Telefon`, `SprStunde` und `Email` aus:

```html
<div class="field Raum">
<div class="label">Raum:</div>
<div class="value"> <span class="text">B 211</span></div>
```

Die E-Mail-Adresse ist auf der Homepage als HTML-Entities kodiert (`&#114;&#x6F;...`) und wird mit `WebUtility.HtmlDecode` lesbar gemacht.
