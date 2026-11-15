using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LehrerPlan.UI
{
    /// <summary>
    /// Kleine Helfer, um uGUI-Elemente per Code zu erzeugen.
    /// Positionen werden wie in der Skizze angegeben: x/y = links oben, y zeigt nach unten.
    /// </summary>
    public static class UiFactory
    {
        const int UiLayer = 5;

        static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform CreateRect(string name, Transform parent, Rect r)
        {
            RectTransform rt = CreateRect(name, parent);
            Place(rt, r);
            return rt;
        }

        /// <summary>Setzt ein Element relativ zur linken oberen Ecke des Parents.</summary>
        public static void Place(RectTransform rt, Rect r)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(r.x, -r.y);
            rt.sizeDelta = new Vector2(r.width, r.height);
        }

        /// <summary>Wie Place, aber mit Pivot in der Mitte (praktisch zum Drehen).</summary>
        public static void PlaceCentered(RectTransform rt, Vector2 center, Vector2 size)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(center.x, -center.y);
            rt.sizeDelta = size;
        }

        /// <summary>Füllt den Parent komplett aus (optional mit Rand).</summary>
        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image CreateImage(string name, Transform parent, Rect r, Color color, bool raycast = false)
        {
            RectTransform rt = CreateRect(name, parent, r);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Text, der den Parent ausfüllt.</summary>
        public static Text CreateText(string name, Transform parent, string content, int size, Color color,
            TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = CreateRect(name, parent);
            Stretch(rt);
            return SetupText(rt, content, size, color, anchor, style);
        }

        /// <summary>Text an einer bestimmten Position.</summary>
        public static Text CreateText(string name, Transform parent, Rect r, string content, int size, Color color,
            TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = CreateRect(name, parent, r);
            return SetupText(rt, content, size, color, anchor, style);
        }

        static Text SetupText(RectTransform rt, string content, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.text = content;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, Rect r, string label, Color background,
            Color textColor, int fontSize, UnityAction onClick)
        {
            Image img = CreateImage(name, parent, r, background, true);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.colors = DefaultColors();
            CreateText("Label", img.transform, label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>Farb-Tönung für Buttons: etwas dunkler beim Drüberfahren / Klicken.</summary>
        public static ColorBlock DefaultColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }
    }
}
