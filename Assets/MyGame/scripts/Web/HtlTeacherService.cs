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
    /// Holt die Lehrerliste und die Detailseiten von der HTL-Salzburg-Homepage
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

        // Ein Feld auf der Detailseite:
        // <div class="field Raum">
        // <div class="label">Raum:</div>
        // <div class="value"> <span class="text">B 211</span></div>
        // Bei der E-Mail steht statt <span> ein <a class="email" ...>.
        static readonly Regex DetailFieldRegex = new Regex(
            "<div class=\"field (?<field>\\w+)\">\\s*<div class=\"label\">[^<]*</div>\\s*<div class=\"value\">\\s*(?:<span class=\"text\">|<a [^>]*>)(?<value>[^<]*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly Dictionary<string, List<TeacherInfo>> _teachersByRoom = new Dictionary<string, List<TeacherInfo>>();
        readonly Dictionary<TeacherInfo, Task> _detailTasks = new Dictionary<TeacherInfo, Task>();

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
        // 2. Detailseiten aller Lehrkräfte eines Raums laden
        // ---------------------------------------------------------
        public async Task<IReadOnlyList<TeacherInfo>> LoadRoomAsync(string roomId)
        {
            IReadOnlyList<TeacherInfo> teachers = GetTeachers(roomId);

            var tasks = new List<Task>();
            foreach (TeacherInfo teacher in teachers)
            {
                tasks.Add(LoadDetailsAsync(teacher));
            }
            await Task.WhenAll(tasks);

            return teachers;
        }

        /// <summary>Lädt die Detailseite einer Lehrkraft (nur einmal, danach aus dem Cache).</summary>
        public Task LoadDetailsAsync(TeacherInfo teacher)
        {
            if (!_detailTasks.TryGetValue(teacher, out Task task))
            {
                task = LoadDetailsInternalAsync(teacher);
                _detailTasks[teacher] = task;
            }
            return task;
        }

        async Task LoadDetailsInternalAsync(TeacherInfo teacher)
        {
            try
            {
                string html = await DownloadAsync(teacher.DetailUrl);

                foreach (Match field in DetailFieldRegex.Matches(html))
                {
                    string value = Clean(field.Groups["value"].Value);

                    switch (field.Groups["field"].Value)
                    {
                        case "Lehrername":
                            if (value.Length > 0) teacher.Name = value;
                            break;
                        case "Raum":
                            teacher.Room = value;
                            break;
                        case "Telefon":
                            teacher.Phone = value;
                            break;
                        case "SprStunde":
                            teacher.OfficeHours = value;
                            break;
                        case "Email":
                            teacher.Email = value;
                            break;
                    }
                }

                teacher.DetailsLoaded = true;
                teacher.DetailError = null;
            }
            catch (Exception e)
            {
                teacher.DetailError = e.Message;
                _detailTasks.Remove(teacher); // beim nächsten Klick nochmal versuchen
            }
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
