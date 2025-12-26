using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq; // ★ 需要引用 Linq
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class TaskbarPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;
        private List<Control> _customColorInputs = new List<Control>();

        public TaskbarPage()
        {
            this.BackColor = UIColors.MainBg;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            _container = new BufferedPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) }; 
            this.Controls.Add(_container);
        }

        public override void OnShow()
        {
            base.OnShow();
            if (Config == null || _isLoaded) return;
            
            _container.SuspendLayout();
            _container.Controls.Clear();

            CreateGeneralGroup(); 
            CreateColorGroup();   

            _container.ResumeLayout();
            _isLoaded = true;
        }

        private void CreateGeneralGroup()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.TaskbarSettings"));

            // 1. 总开关
            AddBool(group, "Menu.TaskbarShow", 
                () => Config.ShowTaskbar, 
                v => Config.ShowTaskbar = v,
                chk => chk.CheckedChanged += (s, e) => EnsureSafeVisibility(null, null, chk)
            );

            // 3. 样式 (Bold/Regular)
            AddComboIndex(group, "Menu.TaskbarStyle",
                new[] { LanguageManager.T("Menu.TaskbarStyleBold"), LanguageManager.T("Menu.TaskbarStyleRegular") },
                () => (Math.Abs(Config.TaskbarFontSize - 9f) < 0.1f && !Config.TaskbarFontBold) ? 1 : 0,
                idx => {
                    if (idx == 1) { Config.TaskbarFontSize = 9f; Config.TaskbarFontBold = false; }
                    else { Config.TaskbarFontSize = 10f; Config.TaskbarFontBold = true; }
                }
            );

            

             // 4. 单行显示
            AddBool(group, "Menu.TaskbarSingleLine", 
                () => Config.TaskbarSingleLine, 
                v => Config.TaskbarSingleLine = v
            );

            // 2. 鼠标穿透
            AddBool(group, "Menu.ClickThrough", () => Config.TaskbarClickThrough, v => Config.TaskbarClickThrough = v);
           

            // ★★★ 新增：选择显示器 ★★★
            // 获取所有屏幕列表
            var screens = Screen.AllScreens;
            // 构造显示名称： "1: \\.\DISPLAY1 [Main]"
            var screenNames = screens.Select((s, i) => 
                $"{i + 1}: {s.DeviceName.Replace(@"\\.\DISPLAY", "Display ")}{(s.Primary ? " [Main]" : "")}"
            ).ToList();
            
            // 插入 "自动 (主屏)" 选项
            screenNames.Insert(0, LanguageManager.T("Menu.Auto"));
            AddComboIndex(group, "Menu.TaskbarMonitor", screenNames.ToArray(), 
                () => {
                    // Getter: 根据保存的 DeviceName 找到对应 Index
                    if (string.IsNullOrEmpty(Config.TaskbarMonitorDevice)) return 0;
                    var idx = Array.FindIndex(screens, s => s.DeviceName == Config.TaskbarMonitorDevice);
                    return idx >= 0 ? idx + 1 : 0;
                },
                idx => {
                    // Setter: 保存选中的 DeviceName
                    if (idx == 0) Config.TaskbarMonitorDevice = ""; // 自动
                    else Config.TaskbarMonitorDevice = screens[idx - 1].DeviceName;
                }
            );

            // 5. 双击操作
            string[] actions = { 
                LanguageManager.T("Menu.ActionToggleVisible"),
                LanguageManager.T("Menu.ActionTaskMgr"), 
                LanguageManager.T("Menu.ActionSettings"),
                LanguageManager.T("Menu.ActionTrafficHistory")
            };
            AddComboIndex(group, "Menu.DoubleClickAction", actions,
                () => Config.TaskbarDoubleClickAction,
                idx => Config.TaskbarDoubleClickAction = idx
            );


            // 4. 对齐
            AddComboIndex(group, "Menu.TaskbarAlign",
                new[] { LanguageManager.T("Menu.TaskbarAlignRight"), LanguageManager.T("Menu.TaskbarAlignLeft") },
                () => Config.TaskbarAlignLeft ? 1 : 0,
                idx => Config.TaskbarAlignLeft = (idx == 1)
            );

            // ★★★ 新增：手动偏移量修正 (支持负数) ★★★
            // 提示：你可以在 zh.json 中添加 "Menu.TaskbarOffsetAdjust": "偏移量修正 (px)"
            AddNumberInt(group, "Menu.TaskbarOffset", "px", 
                () => Config.TaskbarManualOffset, 
                v => Config.TaskbarManualOffset = v
            );

            group.AddFullItem(new LiteNote(LanguageManager.T("Menu.TaskbarAlignTip"), 0));
            AddGroupToPage(group);
        }

        private void CreateColorGroup()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.TaskbarCustomColors"));
            _customColorInputs.Clear();

            AddBool(group, "Menu.TaskbarCustomColors", 
                () => Config.TaskbarCustomStyle, 
                v => Config.TaskbarCustomStyle = v,
                chk => chk.CheckedChanged += (s, e) => {
                    foreach(var c in _customColorInputs) c.Enabled = chk.Checked;
                }
            );

            var tbResult = new LiteUnderlineInput("#000000", "", "", 65, null, HorizontalAlignment.Center);
            tbResult.Inner.ReadOnly = true; 
            var btnPick = new LiteSortBtn("🖌"); 
            btnPick.Location = new Point(70, 1);

            btnPick.Click += (s, e) => {
                using (Form f = new Form { FormBorderStyle = FormBorderStyle.None, WindowState = FormWindowState.Maximized, TopMost = true, Cursor = Cursors.Cross })
                {
                    Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    f.BackgroundImage = bmp;
                    f.MouseClick += (ms, me) => {
                        Color c = bmp.GetPixel(me.X, me.Y);
                        string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                        tbResult.Inner.Text = hex;
                        f.Close();

                        string confirmMsg = string.Format("{0} {1}?", LanguageManager.T("Menu.ScreenColorPickerTip"), hex);
                        if (MessageBox.Show(confirmMsg, "LiteMonitor", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Config.TaskbarColorBg = hex;
                            foreach (var control in _customColorInputs)
                            {
                                if (control is LiteColorInput ci && ci.Input.Inner.Tag?.ToString() == "Menu.BackgroundColor")
                                {
                                    ci.HexValue = hex; 
                                    break;
                                }
                            }
                        }
                    };
                    f.ShowDialog();
                }
            };

            Panel toolCtrl = new Panel { Size = new Size(96, 26) };
            toolCtrl.Controls.Add(tbResult);
            toolCtrl.Controls.Add(btnPick);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ScreenColorPicker"), toolCtrl));

            group.AddFullItem(new LiteNote(LanguageManager.T("Menu.TaskbarCustomTip"), 0));

            void AddC(string key, Func<string> get, Action<string> set)
            {
                var input = AddColor(group, key, get, set, Config.TaskbarCustomStyle);
                _customColorInputs.Add(input);
                if (input is LiteColorInput lci)
                {
                    lci.Input.Inner.Tag = key;
                }
            }

            AddC("Menu.LabelColor",      () => Config.TaskbarColorLabel, v => Config.TaskbarColorLabel = v);
            AddC("Menu.ValueSafeColor",  () => Config.TaskbarColorSafe,  v => Config.TaskbarColorSafe = v);
            AddC("Menu.ValueWarnColor",  () => Config.TaskbarColorWarn,  v => Config.TaskbarColorWarn = v);
            AddC("Menu.ValueCritColor",  () => Config.TaskbarColorCrit,  v => Config.TaskbarColorCrit = v);
            AddC("Menu.BackgroundColor", () => Config.TaskbarColorBg,    v => Config.TaskbarColorBg = v);

            AddGroupToPage(group);
        }
        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }
    }
}