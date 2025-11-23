using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.Common;

namespace LiteMonitor
{
    /// <summary>
    /// 横版渲染器（基于列结构绘制）
    /// 完全保留原版布局，不做任何功能添加。
    /// 修复内容：
    /// 1. Render 方法签名修复（支持 panelWidth）
    /// 2. value/颜色 使用 UIUtils 统一入口
    /// 3. 删除文件内重复工具函数
    /// </summary>
    public static class HorizontalRenderer
    {
        /// <summary>
        /// 修正版：支持 panelWidth，完全匹配 UIController 的调用方式
        /// </summary>
        public static void Render(Graphics g, Theme t, List<Column> cols, int panelWidth)
        {
            int panelHeight = (int)g.VisibleClipBounds.Height;

            // 背景按照原版保持不变
            using (var bg = new SolidBrush(ThemeManager.ParseColor(t.Color.Background)))
                g.FillRectangle(bg, new Rectangle(0, 0, panelWidth, panelHeight));

            foreach (var col in cols)
                DrawColumn(g, col, t);
        }

        private static void DrawColumn(Graphics g, Column col, Theme t)
        {
            if (col.Bounds == Rectangle.Empty)
                return;

            int half = col.Bounds.Height / 2;

            var rectTop = new Rectangle(col.Bounds.X, col.Bounds.Y, col.Bounds.Width, half);
            var rectBottom = new Rectangle(col.Bounds.X, col.Bounds.Y + half, col.Bounds.Width, half);

            if (col.Top != null)
                DrawItem(g, col.Top, rectTop, t);

            if (col.Bottom != null)
                DrawItem(g, col.Bottom, rectBottom, t);
        }

        private static void DrawItem(Graphics g, MetricItem it, Rectangle rc, Theme t)
        {
            // 原版写法：优先用 Short.xx
            string label = LanguageManager.T($"Short.{it.Key}");

            // === 使用 UIUtils 统一格式化 ===
            string value = UIUtils.FormatValue(it.Key, it.DisplayValue);
            value = UIUtils.FormatHorizontalValue(value);//去除网络和磁盘的“/s”，小数点智能显示

            int colW = rc.Width;
            int fontH = t.FontItem.Height;

            // --- 测量 label 宽度（保留原版行为）---
            int wLabel = TextRenderer.MeasureText(
                g, label, t.FontItem,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding
            ).Width;

            // 原版逻辑：给 label 增加 padding
            wLabel += fontH / 2;

            // label 宽不能超过列宽
            if (wLabel > colW - fontH)
                wLabel = colW - fontH;

            // 剩下所有宽度给 value
            int wValue = colW - wLabel;

            Rectangle rcLabel = new Rectangle(rc.X, rc.Y, wLabel, rc.Height);
            Rectangle rcValue = new Rectangle(rc.X + wLabel, rc.Y, wValue, rc.Height);

            // === label（左对齐）===
            TextRenderer.DrawText(
                g,
                label,
                t.FontItem,
                rcLabel,
                ThemeManager.ParseColor(t.Color.TextPrimary),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
            );

            // === value（右对齐）===
            Color valColor = UIUtils.GetColor(it.Key, it.DisplayValue, t);

            TextRenderer.DrawText(
                g,
                value,
                t.FontValue,
                rcValue,
                valColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
            );
        }
    }
}
