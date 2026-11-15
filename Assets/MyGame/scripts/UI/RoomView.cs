using System;
using LehrerPlan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LehrerPlan.UI
{
    /// <summary>
    /// Ein anklickbarer Raum im Flurplan (Rahmen, Tür und Raumnummer).
    /// </summary>
    public class RoomView : MonoBehaviour
    {
        static readonly Color BorderColor = UiFactory.Hex("#37474F");
        static readonly Color DoorColor = UiFactory.Hex("#8D6E63");
        static readonly Color ColorWingB = UiFactory.Hex("#CFE3F7");
        static readonly Color ColorWingC = UiFactory.Hex("#FCE1C4");
        static readonly Color ColorSelected = UiFactory.Hex("#FFD54F");
        static readonly Color ColorEmpty = UiFactory.Hex("#E4E4E4");
        static readonly Color TextColor = UiFactory.Hex("#263238");

        public RoomSlot Slot { get; private set; }

        /// <summary>Raumnummer im aktuellen Stockwerk, z.B. "B312" (null = kein Raum).</summary>
        public string RoomId { get; private set; }

        public event Action<RoomView> Clicked;

        Image _fill;
        Button _button;
        Text _title;
        bool _selected;

        public static RoomView Create(Transform parent, RoomSlot slot)
        {
            // Rahmen (dunkles Rechteck), darin die eigentliche Fläche als Button
            Image border = UiFactory.CreateImage("Raum_" + slot.Id, parent, slot.Rect, BorderColor);
            var view = border.gameObject.AddComponent<RoomView>();
            view.Slot = slot;

            RectTransform fillRt = UiFactory.CreateRect("Flaeche", border.transform);
            UiFactory.Stretch(fillRt, 3f);
            view._fill = fillRt.gameObject.AddComponent<Image>();
            view._button = fillRt.gameObject.AddComponent<Button>();
            view._button.targetGraphic = view._fill;
            view._button.colors = UiFactory.DefaultColors();
            view._button.onClick.AddListener(() => view.Clicked?.Invoke(view));

            UiFactory.CreateImage("Tuer", border.transform, DoorRect(slot), DoorColor);

            float w = slot.Rect.width;
            view._title = UiFactory.CreateText("Raumnummer", border.transform, new Rect(6, 8, w - 12, 26), "",
                22, TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);

            view.SetRoom(null);
            return view;
        }

        /// <summary>Setzt die Raumnummer für das aktuelle Stockwerk.</summary>
        public void SetRoom(string roomId)
        {
            RoomId = roomId;
            _selected = false;
            _button.interactable = roomId != null;
            _title.text = roomId != null ? RoomNames.Pretty(roomId) : "";
            UpdateColor();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            UpdateColor();
        }

        void UpdateColor()
        {
            if (RoomId == null) _fill.color = ColorEmpty;
            else if (_selected) _fill.color = ColorSelected;
            else _fill.color = Slot.Wing == Wing.B ? ColorWingB : ColorWingC;
        }

        /// <summary>Kleiner brauner Balken auf der Seite zum Gang = Tür.</summary>
        static Rect DoorRect(RoomSlot slot)
        {
            float w = slot.Rect.width;
            float h = slot.Rect.height;
            const float len = 50f;
            const float thick = 6f;

            switch (slot.Door)
            {
                case DoorSide.Top: return new Rect((w - len) / 2f, 0, len, thick);
                case DoorSide.Bottom: return new Rect((w - len) / 2f, h - thick, len, thick);
                case DoorSide.Left: return new Rect(0, (h - len) / 2f, thick, len);
                default: return new Rect(w - thick, (h - len) / 2f, thick, len);
            }
        }
    }
}
