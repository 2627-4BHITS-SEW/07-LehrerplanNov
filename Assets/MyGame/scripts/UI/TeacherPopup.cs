using System;
using System.Collections;
using System.Collections.Generic;
using LehrerPlan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LehrerPlan.UI
{
    /// <summary>
    /// Popup-Fenster mit den Infos zu den Lehrkräften eines Raums.
    /// </summary>
    public class TeacherPopup : MonoBehaviour
    {
        static readonly Color PanelColor = UiFactory.Hex("#FFFFFF");
        static readonly Color HeaderColor = UiFactory.Hex("#1E3A5F");
        static readonly Color CardColor = UiFactory.Hex("#F1F4F8");
        static readonly Color TextColor = UiFactory.Hex("#263238");
        static readonly Color MutedColor = UiFactory.Hex("#607D8B");
        static readonly Color LinkButtonColor = UiFactory.Hex("#2E6DA4");

        Text _title;
        Text _subtitle;
        RectTransform _content;
        ScrollRect _scroll;
        Coroutine _popAnimation;

        public event Action Closed;

        public static TeacherPopup Create(Transform parent, Rect r)
        {
            // Schatten + Fenster
            Image shadow = UiFactory.CreateImage("TeacherPopup", parent, r, new Color(0, 0, 0, 0.18f));
            var popup = shadow.gameObject.AddComponent<TeacherPopup>();

            // Pivot in die Mitte, damit das Aufpoppen aus der Mitte heraus skaliert
            var rootRt = (RectTransform)shadow.transform;
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition += new Vector2(r.width / 2f, -r.height / 2f);

            Image panel = UiFactory.CreateImage("Fenster", shadow.transform, new Rect(-6, -6, r.width, r.height), PanelColor, true);

            // Kopfzeile
            Image header = UiFactory.CreateImage("Kopf", panel.transform, new Rect(0, 0, r.width, 96), HeaderColor);
            popup._title = UiFactory.CreateText("Titel", header.transform, new Rect(20, 12, r.width - 90, 44), "",
                32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            popup._subtitle = UiFactory.CreateText("Untertitel", header.transform, new Rect(20, 56, r.width - 90, 28), "",
                18, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleLeft);
            UiFactory.CreateButton("Schliessen", header.transform, new Rect(r.width - 64, 24, 44, 44), "X",
                UiFactory.Hex("#C0392B"), Color.white, 22, popup.Hide);

            // Scrollbereich für die Lehrer-Karten
            RectTransform scrollRt = UiFactory.CreateRect("Scroll", panel.transform, new Rect(16, 112, r.width - 32, r.height - 128));
            popup._scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            popup._scroll.horizontal = false;
            popup._scroll.movementType = ScrollRect.MovementType.Clamped;
            popup._scroll.scrollSensitivity = 30f;

            RectTransform viewport = UiFactory.CreateRect("Viewport", scrollRt);
            UiFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0); // unsichtbar, fängt aber das Mausrad

            RectTransform content = UiFactory.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            popup._scroll.viewport = viewport;
            popup._scroll.content = content;
            popup._content = content;

            shadow.gameObject.SetActive(false);
            return popup;
        }

        public bool IsOpen => gameObject.activeSelf;

        public void ShowLoading(string title)
        {
            Open(title, "Lade Daten von der HTL-Homepage …");
            AddMessageCard("Bitte warten …");
        }

        public void ShowMessage(string title, string subtitle, string message)
        {
            Open(title, subtitle);
            AddMessageCard(message);
        }

        public void ShowTeachers(string title, IReadOnlyList<TeacherInfo> teachers)
        {
            Open(title, teachers.Count == 1 ? "1 Lehrkraft" : teachers.Count + " Lehrkräfte");
            foreach (TeacherInfo teacher in teachers)
            {
                AddTeacherCard(teacher);
            }
            FinishLayout();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void Open(string title, string subtitle)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            ClearContent();

            bool wasOpen = gameObject.activeSelf;
            gameObject.SetActive(true);
            if (!wasOpen)
            {
                if (_popAnimation != null) StopCoroutine(_popAnimation);
                _popAnimation = StartCoroutine(PopIn());
            }
        }

        void ClearContent()
        {
            foreach (Transform child in _content)
            {
                // erst deaktivieren, damit das Layout den alten Inhalt sofort ignoriert
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        void AddMessageCard(string message)
        {
            RectTransform card = CreateCard("Hinweis");
            UiFactory.CreateText("Text", card, message, 20, MutedColor, TextAnchor.UpperLeft);
            FinishLayout();
        }

        void AddTeacherCard(TeacherInfo t)
        {
            RectTransform card = CreateCard("Lehrer_" + t.LastName);

            string info;
            if (t.DetailsLoaded)
            {
                info = $"<b><size=22>{t.Name}</size></b>\n" +
                       $"Raum: {OrDash(t.Room)}\n" +
                       $"Tel-DW: {OrDash(t.Phone)}\n" +
                       $"Sprechstunde: {OrDash(t.OfficeHours)}\n" +
                       $"E-Mail: {OrDash(t.Email)}";
            }
            else
            {
                info = $"<b><size=22>{t.Name}</size></b>\n" +
                       $"Raum: {OrDash(t.Room)}\n" +
                       $"<i>Details konnten nicht geladen werden ({t.DetailError})</i>";
            }

            UiFactory.CreateText("Info", card, info, 18, TextColor, TextAnchor.UpperLeft);

            string url = t.DetailUrl;
            Button link = UiFactory.CreateButton("Homepage", card, new Rect(0, 0, 200, 36), "Visitenkarte öffnen",
                LinkButtonColor, Color.white, 16, () => Application.OpenURL(url));
            var linkLayout = link.gameObject.AddComponent<LayoutElement>();
            linkLayout.preferredHeight = 36;
        }

        RectTransform CreateCard(string name)
        {
            Image card = UiFactory.CreateImage(name, _content, new Rect(0, 0, 100, 100), CardColor);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return (RectTransform)card.transform;
        }

        void FinishLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scroll.verticalNormalizedPosition = 1f;
        }

        IEnumerator PopIn()
        {
            const float duration = 0.15f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
                yield return null;
            }
            transform.localScale = Vector3.one;
            _popAnimation = null;
        }

        static string OrDash(string s)
        {
            return string.IsNullOrEmpty(s) ? "–" : s;
        }
    }
}
