using System;
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

        static readonly Color BackgroundColor = Color.white;
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

        /// <summary>Startet das Laden der Lehrerliste (oder versucht es nach einem Fehler erneut).</summary>
        Task EnsureTeacherListLoading()
        {
            if (_listTask == null || _listTask.IsFaulted || _listTask.IsCanceled)
            {
                _listTask = _service.LoadTeacherListAsync();
            }
            return _listTask;
        }

        async Task LoadTeacherListAsync()
        {
            SetStatus("Lade Lehrerliste von www.htl-salzburg.ac.at …");
            try
            {
                await EnsureTeacherListLoading();
                if (this == null) return; // Play-Mode wurde inzwischen beendet

                SetStatus($"{_service.TeacherCount} Lehrkräfte von der Homepage geladen. Klick auf einen Raum für Details.");
                RefreshDoorSigns();
            }
            catch (Exception e)
            {
                if (this == null) return;
                Debug.LogWarning("Lehrerliste konnte nicht geladen werden: " + e.Message);
                SetStatus("Lehrerliste konnte nicht geladen werden (" + e.Message + "). Klick auf einen Raum versucht es erneut.");
                RefreshDoorSigns();
            }
        }

        async void OnRoomClicked(RoomView room)
        {
            if (room.RoomId == null) return;

            SelectRoom(room);
            int version = ++_clickVersion;
            string title = "Raum " + RoomNames.Pretty(room.RoomId);
            _popup.ShowLoading(title);

            try
            {
                bool hadList = _service.IsListLoaded;
                await EnsureTeacherListLoading();
                if (!hadList && this != null)
                {
                    SetStatus($"{_service.TeacherCount} Lehrkräfte von der Homepage geladen.");
                    RefreshDoorSigns();
                }

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
            catch (Exception e)
            {
                if (this == null || version != _clickVersion) return;
                Debug.LogWarning(e);
                _popup.ShowMessage(title, "Fehler", "Die Daten konnten nicht geladen werden:\n" + e.Message +
                                                     "\n\nIst eine Internetverbindung vorhanden?");
                SetStatus("Lehrerliste konnte nicht geladen werden (" + e.Message + ").");
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
                if (_service.IsListLoaded) room.SetDoorSign(_service.GetTeachers(room.RoomId));
                else if (_listTask != null && _listTask.IsFaulted) room.SetDoorSignUnavailable();
                else room.SetDoorSign(null);
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

        void SetStatus(string message)
        {
            Debug.Log(message);
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
            Transform plan = UiFactory.CreateRect("Flurplan", parent, area);

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
