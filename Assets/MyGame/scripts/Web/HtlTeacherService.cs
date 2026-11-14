using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LehrerPlan.Data;
using UnityEngine.Networking;

namespace LehrerPlan.Web
{
    /// <summary>
    /// Holt die Lehrerliste von der HTL-Salzburg-Homepage
    /// und filtert die Daten mit Regex aus dem HTML.
    /// </summary>
    public class HtlTeacherService
    {
        public const string BaseUrl = "https://www.htl-salzburg.ac.at";
        public const string ListUrl = BaseUrl + "/lehrerinnen.html";

        // Ein Eintrag in der Lehrerliste sieht so aus:
        // <a class="value" href="/lehrerinnen-details/holzer-roland-prof-dipl-ing-fh-b-211.html"><span class="text">Holzer Roland, Prof. Dipl.-Ing.&#40;FH&#41;</span></a>
        // Gruppe "url"  = relativer Link zur Detailseite
        // Gruppe "name" = Name inkl. Titel
        static readonly Regex TeacherLinkRegex = new Regex(
            "<a class=\"value\" href=\"(?<url>/lehrerinnen-details/[^\"]+)\">\\s*<span class=\"text\">(?<name>[^<]+)</span>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Die Raumnummer steckt am Ende vom Link: ...-b-211.html -> Trakt "b", Nummer "211"
        // (manchmal ohne Bindestrich "a114.html" oder mit Zusatz "d-001-bibliothek.html")
        static readonly Regex RoomInUrlRegex = new Regex(
            "-(?<trakt>[a-z])-?(?<nr>\\d{3})[a-z]?(?:-[^/.]*)?\\.html$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly Dictionary<string, List<TeacherInfo>> _teachersByRoom = new Dictionary<string, List<TeacherInfo>>();

        public bool IsListLoaded { get; private set; }
        public int TeacherCount { get; private set; }

        // ---------------------------------------------------------
        // 1. Lehrerliste laden und nach Räumen gruppieren
        // ---------------------------------------------------------
        public async Task LoadTeacherListAsync()
        {
            string html = await DownloadAsync(ListUrl);

            _teachersByRoom.Clear();
            var seenUrls = new HashSet<string>();
            int count = 0;

            foreach (Match link in TeacherLinkRegex.Matches(html))
            {
                string relativeUrl = link.Groups["url"].Value;
                if (!seenUrls.Add(relativeUrl)) continue;
                count++;

                Match room = RoomInUrlRegex.Match(relativeUrl);
                if (!room.Success) continue; // Lehrkraft ohne Raum

                string roomId = room.Groups["trakt"].Value.ToUpperInvariant() + room.Groups["nr"].Value;

                var teacher = new TeacherInfo
                {
                    Name = Clean(link.Groups["name"].Value),
                    Room = RoomNames.Pretty(roomId),
                    DetailUrl = BaseUrl + relativeUrl
                };

                if (!_teachersByRoom.TryGetValue(roomId, out List<TeacherInfo> list))
                {
                    list = new List<TeacherInfo>();
                    _teachersByRoom[roomId] = list;
                }
                list.Add(teacher);
            }

            TeacherCount = count;
            IsListLoaded = true;
        }

        /// <summary>Alle Lehrkräfte eines Raums laut Lehrerliste (roomId z.B. "B312" oder "B 312").</summary>
        public IReadOnlyList<TeacherInfo> GetTeachers(string roomId)
        {
            if (!string.IsNullOrEmpty(roomId) &&
                _teachersByRoom.TryGetValue(RoomNames.Normalize(roomId), out List<TeacherInfo> list))
            {
                return list;
            }
            return Array.Empty<TeacherInfo>();
        }

        // ---------------------------------------------------------
        // Hilfsfunktionen
        // ---------------------------------------------------------

        /// <summary>Lädt eine Seite mit UnityWebRequest und liefert das HTML als string.</summary>
        static Task<string> DownloadAsync(string url)
        {
            var tcs = new TaskCompletionSource<string>();
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    tcs.SetResult(request.downloadHandler.text);
                }
                else
                {
                    tcs.SetException(new Exception($"{request.error} ({url})"));
                }
                request.Dispose();
            };

            return tcs.Task;
        }

        /// <summary>HTML-Entities auflösen (&amp;#40; -> "(") und Leerzeichen aufräumen.</summary>
        static string Clean(string raw)
        {
            string decoded = WebUtility.HtmlDecode(raw ?? "");
            return Regex.Replace(decoded, "\\s+", " ").Trim();
        }
    }
}
