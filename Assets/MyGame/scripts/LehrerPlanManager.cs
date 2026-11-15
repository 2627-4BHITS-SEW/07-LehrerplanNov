using System.Collections.Generic;
using System.Threading.Tasks;
using LehrerPlan.Data;
using LehrerPlan.UI;
using LehrerPlan.Web;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LehrerPlan
{
    /// <summary>
    /// Baut den 2D-Flurplan (Gang B und C) auf, lädt die Lehrerdaten von der HTL-Homepage
    /// und zeigt beim Klick auf einen Raum die Lehrkräfte in einem Popup an.
    /// Einfach auf ein leeres GameObject in der Szene legen.
    /// </summary>
    public class LehrerPlanManager : MonoBehaviour
    {
        [Tooltip("Mit welchem Stockwerk der Plan startet (2 oder 3).")]
        [SerializeField] int startFloor = 3;

        static readonly Color BackgroundColor = UiFactory.Hex("#E9EEF3");
        static readonly Color PaperColor = UiFactory.Hex("#FDFDFB");
        static readonly Color GridColor = new Color(0.62f, 0.78f, 0.92f, 0.35f);
        static readonly Color WallColor = UiFactory.Hex("#212121");
        static readonly Color CorridorColor = UiFactory.Hex("#D9D9D9");
        static readonly Color DarkText = UiFactory.Hex("#1E3A5F");
        static readonly Color MutedText = UiFactory.Hex("#607D8B");
        static readonly Color ButtonActive = UiFactory.Hex("#1E3A5F");
        static readonly Color ButtonInactive = UiFactory.Hex("#FFFFFF");

        readonly HtlTeacherService _service = new HtlTeacherService();
        readonly List<RoomView> _rooms = new List<RoomView>();
        readonly Dictionary<int, Button> _floorButtons = new Dictionary<int, Button>();

        Task _listTask;
        TeacherPopup _popup;
        Text _floorTitle;
        RoomView _selectedRoom;
        int _floor;
        int _clickVersion;

        void Awake()
        {
            EnsureEventSystem();
            BuildUi();
        }

        async void Start()
        {
            SetFloor(startFloor == 2 ? 2 : 3);
            await LoadTeacherListAsync();
        }

        // ---------------------------------------------------------
        // Daten laden
        // ---------------------------------------------------------

        async Task LoadTeacherListAsync()
        {
            _listTask = _service.LoadTeacherListAsync();
            await _listTask;
            if (this == null) return; // Play-Mode wurde inzwischen beendet

            Debug.Log($"{_service.TeacherCount} Lehrkräfte von der Homepage geladen.");
            RefreshDoorSigns();
        }

        async void OnRoomClicked(RoomView room)
        {
            if (room.RoomId == null) return;

            SelectRoom(room);
            int version = ++_clickVersion;
            string title = "Raum " + RoomNames.Pretty(room.RoomId);
            _popup.ShowLoading(title);

            await _listTask;
            IReadOnlyList<TeacherInfo> teachers = await _service.LoadRoomAsync(room.RoomId);

            // inzwischen anderer Raum angeklickt oder Play-Mode beendet?
            if (this == null || version != _clickVersion) return;

            if (teachers.Count == 0)
            {
                _popup.ShowMessage(title, _floor + ". Stock",
                    "Für diesen Raum ist auf der HTL-Homepage keine Lehrkraft eingetragen.");
            }
            else
            {
                _popup.ShowTeachers(title, teachers);
            }
        }

        // ---------------------------------------------------------
        // Stockwerk / Auswahl
        // ---------------------------------------------------------

        void SetFloor(int floor)
        {
            _floor = floor;
            _clickVersion++; // laufende Anfragen ignorieren
            _selectedRoom = null;
            _popup.Hide();

            foreach (RoomView room in _rooms)
            {
                room.SetRoom(room.Slot.RoomOn(floor));
            }
            RefreshDoorSigns();

            _floorTitle.text = $"{floor}. Stock  ·  Gang B und Gang C";

            foreach (KeyValuePair<int, Button> pair in _floorButtons)
            {
                bool active = pair.Key == floor;
                pair.Value.targetGraphic.color = active ? ButtonActive : ButtonInactive;
                pair.Value.GetComponentInChildren<Text>().color = active ? Color.white : DarkText;
            }
        }

        void RefreshDoorSigns()
        {
            foreach (RoomView room in _rooms)
            {
                room.SetDoorSign(_service.IsListLoaded ? _service.GetTeachers(room.RoomId) : null);
            }
        }

        void SelectRoom(RoomView room)
        {
            if (_selectedRoom != null) _selectedRoom.SetSelected(false);
            _selectedRoom = room;
            if (_selectedRoom != null) _selectedRoom.SetSelected(true);
        }

        void OnPopupClosed()
        {
            _clickVersion++;
            SelectRoom(null);
        }

        // ---------------------------------------------------------
        // UI aufbauen
        // ---------------------------------------------------------

        void BuildUi()
        {
            // Canvas (Screen Space Overlay, skaliert auf 1920x1080)
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RectTransform background = UiFactory.CreateRect("Hintergrund", canvasGo.transform);
            UiFactory.Stretch(background);
            background.gameObject.AddComponent<Image>().color = BackgroundColor;

            // fester 1920x1080 Bereich in der Mitte, alles andere ist relativ dazu
            RectTransform layout = UiFactory.CreateRect("Layout", canvasGo.transform);
            layout.anchorMin = layout.anchorMax = new Vector2(0.5f, 0.5f);
            layout.pivot = new Vector2(0.5f, 0.5f);
            layout.sizeDelta = new Vector2(1920, 1080);

            BuildHeader(layout);
            BuildPlan(layout, new Rect(30, 130, FloorPlanData.Width, FloorPlanData.Height));

            _popup = TeacherPopup.Create(layout, new Rect(1345, 130, 545, 880));
            _popup.Closed += OnPopupClosed;
        }

        void BuildHeader(RectTransform parent)
        {
            UiFactory.CreateText("Titel", parent, new Rect(30, 22, 1100, 52),
                "HTL Salzburg – Lehrerräume Trakt B / C", 38, DarkText, TextAnchor.MiddleLeft, FontStyle.Bold);
            _floorTitle = UiFactory.CreateText("Stockwerk", parent, new Rect(32, 76, 900, 34), "",
                22, MutedText, TextAnchor.MiddleLeft);

            float x = 1010;
            foreach (int floor in FloorPlanData.Floors)
            {
                int f = floor; // eigene Variable für das Lambda
                Button button = UiFactory.CreateButton("Stock_" + f, parent, new Rect(x, 40, 140, 54), f + ". Stock",
                    ButtonInactive, DarkText, 22, () => SetFloor(f));
                _floorButtons[f] = button;
                x += 150;
            }
        }

        void BuildPlan(RectTransform parent, Rect area)
        {
            Image paper = UiFactory.CreateImage("Flurplan", parent, area, PaperColor);
            Transform plan = paper.transform;

            // Karopapier wie in der Skizze
            const float grid = 40f;
            for (float gx = grid; gx < area.width; gx += grid)
                UiFactory.CreateImage("GridV", plan, new Rect(gx, 0, 1, area.height), GridColor);
            for (float gy = grid; gy < area.height; gy += grid)
                UiFactory.CreateImage("GridH", plan, new Rect(0, gy, area.width, 1), GridColor);

            // Gänge
            UiFactory.CreateImage("Gang_B", plan, FloorPlanData.CorridorB, CorridorColor);
            UiFactory.CreateImage("Gang_C", plan, FloorPlanData.CorridorC, CorridorColor);

            Rect b = FloorPlanData.CorridorB;
            UiFactory.CreateText("Label_Gang_B", plan, new Rect(b.x + 300, b.y, b.width - 300, b.height), "Gang B",
                56, new Color(0, 0, 0, 0.25f), TextAnchor.MiddleCenter, FontStyle.Bold);

            Rect c = FloorPlanData.CorridorC;
            Text labelC = UiFactory.CreateText("Label_Gang_C", plan, "Gang C", 56, new Color(0, 0, 0, 0.25f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.PlaceCentered(labelC.rectTransform, new Vector2(c.center.x, c.y + c.height * 0.6f), new Vector2(400, 100));
            labelC.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);

            // Wände
            foreach (Rect wall in FloorPlanData.Walls)
                UiFactory.CreateImage("Wand", plan, wall, WallColor);

            // Räume
            foreach (RoomSlot slot in FloorPlanData.Slots)
            {
                RoomView room = RoomView.Create(plan, slot);
                room.Clicked += OnRoomClicked;
                _rooms.Add(room);
            }

            UiFactory.CreateText("Hinweis", plan, new Rect(440, 560, 780, 160),
                "Klick auf einen Raum, um die Lehrkräfte anzuzeigen.\n" +
                "<size=18>Die Daten werden live von www.htl-salzburg.ac.at geladen.</size>",
                24, MutedText, TextAnchor.MiddleCenter);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
