using LiteMonitor.src.Core;
using LiteMonitor.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LiteMonitor
{
    /// <summary>
    /// -------- 渲染层：只负责画，不参与布局 --------
    /// </summary>
    public static class UIRenderer
    {
        private static readonly Dictionary<string, SolidBrush> _brushCache = new();

        private static SolidBrush GetBrush(string color, Theme t)
        {
            if (!_brushCache.TryGetValue(color, out var br))
            {
                br = new SolidBrush(ThemeManager.ParseColor(color));
                _brushCache[color] = br;
            }
            return br;
        }

        public static void ClearCache() => _brushCache.Clear();

        /// <summary>
        /// 绘制整个面板
        /// </summary>
        public static void Render(Graphics g, List<GroupLayoutInfo> groups, Theme t)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 背景高度
            int bgH = (groups.Count > 0)
                ? groups[^1].Bounds.Bottom + t.Layout.GroupBottom + t.Layout.Padding
                : t.Layout.Padding * 2;

            g.FillRectangle(
                GetBrush(t.Color.Background, t),
                new Rectangle(0, 0, t.Layout.Width, bgH));

            // ===== 绘制主标题 =====
            string title = LanguageManager.T("Title");
            if (string.IsNullOrEmpty(title) || title == "Title")
                title = "LiteMonitor";

            int titleH = TextRenderer.MeasureText(title, t.FontTitle).Height;

            var titleRect = new Rectangle(
                t.Layout.Padding,
                t.Layout.Padding,
                t.Layout.Width - t.Layout.Padding * 2,
                titleH + 4);

            TextRenderer.DrawText(
                g, title, t.FontTitle, titleRect,
                ThemeManager.ParseColor(t.Color.TextTitle),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            // ===== 绘制各组 =====
            foreach (var gr in groups)
                DrawGroup(g, gr, t);
        }

        private static void DrawGroup(Graphics g, GroupLayoutInfo gr, Theme t)
        {
            int gp = t.Layout.GroupPadding;
            int radius = t.Layout.GroupRadius;

            // 背景块
            var block = new Rectangle(gr.Bounds.X, gr.Bounds.Y, gr.Bounds.Width, gr.Bounds.Height);
            UIUtils.FillRoundRect(g, block, radius, ThemeManager.ParseColor(t.Color.GroupBackground));

            // 组标题
            string label = LanguageManager.T($"Groups.{gr.GroupName}");
            if (string.IsNullOrEmpty(label)) label = gr.GroupName;

            int titleH = t.FontGroup.Height;
            int titleY = block.Y - t.Layout.GroupTitleOffset - titleH;

            var rectTitle = new Rectangle(
                block.X + gp,
                Math.Max(0, titleY),
                block.Width - gp * 2,
                titleH);

            TextRenderer.DrawText(
                g, label, t.FontGroup, rectTitle,
                ThemeManager.ParseColor(t.Color.TextGroup),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            // ===== NET / DISK（双列）=====
            if (gr.GroupName.Equals("DISK", StringComparison.OrdinalIgnoreCase))
            {
                DrawTwoCols(g, gr, t, "DISK", "DISK.Read", "DISK.Write");
                return;
            }

            if (gr.GroupName.Equals("NET", StringComparison.OrdinalIgnoreCase))
            {
                DrawTwoCols(g, gr, t, "NET", "NET.Up", "NET.Down");
                return;
            }

            // 普通行
            foreach (var it in gr.Items)
                DrawMetricItem(g, it, t);
        }

        /// <summary>
        /// 单行指标（含进度条）
        /// </summary>
        private static void DrawMetricItem(Graphics g, MetricItem it, Theme t)
        {
            if (it.Bounds == Rectangle.Empty) return;

            var inner = new Rectangle(it.Bounds.X + 10, it.Bounds.Y, it.Bounds.Width - 20, it.Bounds.Height);
            int topH = (int)(inner.Height * 0.55);

            var topRect = new Rectangle(inner.X, inner.Y, inner.Width, topH);

            // label
            string label = LanguageManager.T($"Items.{it.Key}");
            if (label == $"Items.{it.Key}") label = it.Label;

            TextRenderer.DrawText(
                g, label, t.FontItem, topRect,
                ThemeManager.ParseColor(t.Color.TextPrimary),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            // value
            string text = UIUtils.FormatValue(it.Key, it.DisplayValue);
            Color valColor = UIUtils.GetColor(it.Key, it.DisplayValue, t);

            TextRenderer.DrawText(
                g, text, t.FontValue, topRect,
                valColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            // bar
            int barH = Math.Max(6, (int)(inner.Height * 0.25));
            int barY = inner.Bottom - barH - 3;

            var barRect = new Rectangle(inner.X, barY, inner.Width, barH);
            UIUtils.DrawBar(g, barRect, it.DisplayValue, it.Key, t);
        }

        /// <summary>
        /// NET / DISK 双列绘制
        /// </summary>
        private static void DrawTwoCols(Graphics g, GroupLayoutInfo gr, Theme t,
            string prefix, string leftKey, string rightKey)
        {
            var items = gr.Items;
            var left = items.FirstOrDefault(i => i.Key.Equals(leftKey, StringComparison.OrdinalIgnoreCase));
            var right = items.FirstOrDefault(i => i.Key.Equals(rightKey, StringComparison.OrdinalIgnoreCase));
            if (left == null && right == null) return;

            var baseRect = (left ?? right).Bounds;
            int rowH = baseRect.Height;
            int colW = baseRect.Width / 2;

            var rectLeft = new Rectangle(baseRect.X, baseRect.Y, colW, rowH);
            var rectRight = new Rectangle(rectLeft.Right, baseRect.Y, colW, rowH);

            double offsetY = rowH * 0.10;

            // labels
            DrawCenterLabel(g, rectLeft, offsetY, LanguageManager.T($"Items.{leftKey}"), t);
            DrawCenterLabel(g, rectRight, offsetY, LanguageManager.T($"Items.{rightKey}"), t);

            // values
            DrawCenterValue(g, rectLeft, offsetY, left, t);
            DrawCenterValue(g, rectRight, offsetY, right, t);
        }

        private static void DrawCenterLabel(Graphics g, Rectangle r, double offsetY, string text, Theme t)
        {
            var rect = new Rectangle(r.X, r.Y + (int)offsetY, r.Width, r.Height);
            TextRenderer.DrawText(
                g, text, t.FontItem, rect,
                ThemeManager.ParseColor(t.Color.TextPrimary),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
        }

        private static void DrawCenterValue(Graphics g, Rectangle r, double offsetY, MetricItem? item, Theme t)
        {
            if (item == null) return;

            var rect = new Rectangle(r.X, r.Y + (int)(offsetY * 2), r.Width, r.Height);

            string text = UIUtils.FormatValue(item.Key, item.Value);
            Color c = UIUtils.GetColor(item.Key, item.Value ?? 0, t);

            TextRenderer.DrawText(
                g, text, t.FontValue, rect,
                c,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
        }
    }
}
