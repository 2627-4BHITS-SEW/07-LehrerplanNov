using UnityEngine;

namespace LehrerPlan.Data
{
    public enum Wing { B, C }

    /// <summary>Auf welcher Seite vom Raum die Tür zum Gang ist.</summary>
    public enum DoorSide { Top, Bottom, Left, Right }

    /// <summary>
    /// Ein Platz im Grundriss. Im 2. und 3. Stock liegen die Räume genau übereinander,
    /// darum hat ein Platz pro Stockwerk eine eigene Raumnummer (null = kein Lehrerraum erfasst).
    /// </summary>
    public class RoomSlot
    {
        public readonly string Id;
        public readonly Wing Wing;
        public readonly Rect Rect;      // Position im Plan (x/y = links oben, y nach unten)
        public readonly DoorSide Door;
        public readonly string RoomFloor2;
        public readonly string RoomFloor3;

        public RoomSlot(string id, Wing wing, Rect rect, DoorSide door, string roomFloor2, string roomFloor3)
        {
            Id = id;
            Wing = wing;
            Rect = rect;
            Door = door;
            RoomFloor2 = roomFloor2;
            RoomFloor3 = roomFloor3;
        }

        public string RoomOn(int floor)
        {
            return floor == 2 ? RoomFloor2 : RoomFloor3;
        }
    }

    /// <summary>
    /// Grundriss nach der Skizze: Gang B waagrecht oben, Gang C senkrecht links,
    /// beide treffen sich in der Ecke links oben. Alle Maße in Pixel (Plan = 1280 x 880).
    /// </summary>
    public static class FloorPlanData
    {
        public const float Width = 1280f;
        public const float Height = 880f;
        public const float WallThickness = 8f;

        public static readonly int[] Floors = { 2, 3 };

        // Gänge
        public static readonly Rect CorridorB = new Rect(220, 170, 1060, 160);
        public static readonly Rect CorridorC = new Rect(220, 170, 160, 710);

        // Wände (Linien aus der Skizze)
        public static readonly Rect[] Walls =
        {
            new Rect(216, 166, 1064, WallThickness), // Außenwand Gang B (oben)
            new Rect(216, 166, WallThickness, 714),  // Außenwand Gang C (links)
            new Rect(376, 326, 904, WallThickness),  // Innenwand Gang B
            new Rect(376, 326, WallThickness, 554),  // Innenwand Gang C
        };

        const float RoomW = 140f;
        const float RoomH = 140f;

        public static readonly RoomSlot[] Slots =
        {
            // Gang B, Außenseite (oberhalb der Außenwand) - laut Skizze nur 3. Stock
            new RoomSlot("B_out_1", Wing.B, new Rect(700, 26, RoomW, RoomH), DoorSide.Bottom, null, "B303"),
            new RoomSlot("B_out_2", Wing.B, new Rect(850, 26, RoomW, RoomH), DoorSide.Bottom, null, "B304"),
            new RoomSlot("B_out_3", Wing.B, new Rect(1000, 26, RoomW, RoomH), DoorSide.Bottom, null, "B305"),

            // Gang B, Innenseite (unterhalb der Innenwand) - 3. Stock oben, 2. Stock darunter
            new RoomSlot("B_in_1", Wing.B, new Rect(420, 334, RoomW, RoomH), DoorSide.Top, "B212", "B314"),
            new RoomSlot("B_in_2", Wing.B, new Rect(565, 334, RoomW, RoomH), DoorSide.Top, "B211", "B313"),
            new RoomSlot("B_in_3", Wing.B, new Rect(710, 334, RoomW, RoomH), DoorSide.Top, "B210", "B312"),
            new RoomSlot("B_in_4", Wing.B, new Rect(855, 334, RoomW, RoomH), DoorSide.Top, "B209", "B311"),
            new RoomSlot("B_in_5", Wing.B, new Rect(1000, 334, RoomW, RoomH), DoorSide.Top, "B208", "B310"),

            // Gang C, Außenseite (links der Außenwand)
            new RoomSlot("C_out_1", Wing.C, new Rect(66, 190, 150, 110), DoorSide.Right, "C214", "C313"),
            new RoomSlot("C_out_2", Wing.C, new Rect(66, 310, 150, 110), DoorSide.Right, "C213", "C312"),
            new RoomSlot("C_out_3", Wing.C, new Rect(66, 430, 150, 110), DoorSide.Right, "C212", null),
        };
    }
}
