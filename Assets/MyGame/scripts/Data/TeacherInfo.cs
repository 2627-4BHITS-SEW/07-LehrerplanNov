using System;
using System.Text.RegularExpressions;

namespace LehrerPlan.Data
{
    /// <summary>
    /// Infos zu einer Lehrkraft, wie sie auf der HTL-Homepage stehen.
    /// Name und Link kommen aus der Lehrerliste, der Rest aus der Detailseite ("Visitenkarte").
    /// </summary>
    [Serializable]
    public class TeacherInfo
    {
        public string Name;          // z.B. "Holzer Roland, Prof. Dipl.-Ing.(FH)"
        public string Room;          // z.B. "B 211"
        public string Phone;         // Tel-DW (Durchwahl)
        public string OfficeHours;   // Sprechstunde
        public string Email;
        public string DetailUrl;     // absolute URL der Detailseite

        public bool DetailsLoaded;
        public string DetailError;

        /// <summary>Nachname = erstes Wort vom Namen ("Birgler-Schnell Selina, ..." -> "Birgler-Schnell").</summary>
        public string LastName
        {
            get
            {
                string[] parts = SplitName();
                return parts.Length > 0 ? parts[0] : Name;
            }
        }

        /// <summary>Kurzform fürs Türschild, z.B. "Holzer R."</summary>
        public string ShortName
        {
            get
            {
                string[] parts = SplitName();
                if (parts.Length == 0) return Name;
                if (parts.Length == 1) return parts[0];
                return parts[0] + " " + parts[1].Substring(0, 1) + ".";
            }
        }

        string[] SplitName()
        {
            return (Name ?? "").Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public override string ToString()
        {
            return $"{Name} ({Room})";
        }
    }

    /// <summary>Hilfsfunktionen für Raumbezeichnungen.</summary>
    public static class RoomNames
    {
        /// <summary>"B312" -> "B 312" (so wie auf der Homepage geschrieben)</summary>
        public static string Pretty(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || roomId.Length < 2) return roomId;
            return roomId.Substring(0, 1) + " " + roomId.Substring(1);
        }

        /// <summary>"B 312", "b-312" oder "B312" -> "B312" (interner Schlüssel)</summary>
        public static string Normalize(string room)
        {
            return Regex.Replace(room ?? "", "[^A-Za-z0-9]", "").ToUpperInvariant();
        }
    }
}
