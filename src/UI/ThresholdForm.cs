using System;
using System.Drawing;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using System.Text.Json; 

namespace LiteMonitor
{
    public class ThresholdForm : Form
    {
        private Settings _cfg;          
        private Settings _sourceCfg;    
        private float _scale = 1.0f;

        // === 🎨 现代深色主题 (Modern Dark Theme) ===
        // 配色完全保持不变
        private readonly Color C_Background = Color.FromArgb(60, 60, 60);    
        private readonly Color C_Card       = Color.FromArgb(46, 46, 46);    
        private readonly Color C_Button_Bar = Color.FromArgb(50, 50, 50);    
        private readonly Color C_InputBack  = Color.FromArgb(55, 55, 55);    
        private readonly Color C_Separator  = Color.FromArgb(60, 60, 60);    
        
        private readonly Color C_TextMain   = Color.FromArgb(240, 240, 240); 
        private readonly Color C_TextSub    = Color.FromArgb(160, 160, 160); 
        private readonly Color C_TextTitle  = Color.FromArgb(255, 255, 255); 
        
        private readonly Color C_Warn       = Color.FromArgb(255, 180, 0);   
        private readonly Color C_Crit       = Color.FromArgb(255, 80, 80);   
        private readonly Color C_Action     = Color.FromArgb(0, 120, 215);   

        // 字体缓存
        private Font F_Title;   
        private Font F_Label;   
        private Font F_Value;   

        public ThresholdForm(Settings cfg)
        {
            _sourceCfg = cfg;

            var json = JsonSerializer.Serialize(cfg);
            _cfg = JsonSerializer.Deserialize<Settings>(json);
            
            // DPI 适配
            using (Graphics g = this.CreateGraphics())
            {
                _scale = g.DpiX / 96.0f;
            }

            // [修改] 字体微调：稍微改小一点点，更加精致紧凑
            F_Title = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold); // 原 11F
            F_Label = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular); // 原 9.5F
            F_Value = new Font("Consolas", 10F, FontStyle.Bold); // 原 10.5F

            // 窗体属性
            this.Text = "告警阈值设置 (Threshold Settings)";
            // [修改] 窗体高度：960 -> 720 (避免太长)
            this.Size = new Size(S(545), S(720)); 
            this.MinimumSize = new Size(S(545), S(720));
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = C_Background;
            this.ForeColor = C_TextMain;

            BuildUI();
        }

        private int S(int pixel) => (int)(pixel * _scale);

        private void BuildUI()
        {
            // 1. 主滚动容器
            var mainScroll = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                // [修改] 边距：减少四周留白，底部留白从 100 减到 60
                Padding = new Padding(S(10), S(10), S(10), S(60)), 
                BackColor = C_Background
            };

            // 2. 添加功能卡片
            mainScroll.Controls.Add(CreateCard("最大频率与功耗 (Max Limits) -- 注意：仅在开启频率/功耗显示时设置", p => {
                AddHardwareHeaderRow(p); 
                
                AddMaxLimitRow(p, "最大频率 / Max Clock (MHz)", 
                    _cfg.RecordedMaxCpuClock, _cfg.RecordedMaxGpuClock,
                    v => _cfg.RecordedMaxCpuClock = v,
                    v => _cfg.RecordedMaxGpuClock = v);

                AddMaxLimitRow(p, "最大功耗 / Max Power (W)", 
                    _cfg.RecordedMaxCpuPower, _cfg.RecordedMaxGpuPower,
                    v => _cfg.RecordedMaxCpuPower = v,
                    v => _cfg.RecordedMaxGpuPower = v);
            
                AddDescriptionRow(p, "⚠️ 请填写硬件的实际最大值，不填将在高负载时动态学习并更新。");
            }));

            
            // 卡片 A
            mainScroll.Controls.Add(CreateCard("⚠️通用硬件 (General Hardware)", p => {
                AddHeaderRow(p); 
                AddConfigRow(p, "负载 / Load (%)", _cfg.Thresholds.Load);
                AddConfigRow(p, "温度 / Temp (°C)", _cfg.Thresholds.Temp);
            }));

            // 卡片 B
            mainScroll.Controls.Add(CreateCard("⚠️传输速率 (Transfer Speed)", p => {
                AddHeaderRow(p);
                AddConfigRow(p, "磁盘读写 / Disk IO (MB/s)", _cfg.Thresholds.DiskIOMB);
                AddConfigRow(p, "上传速率 / Net Up (MB/s)", _cfg.Thresholds.NetUpMB);
                AddConfigRow(p, "下载速率 / Net Down (MB/s)", _cfg.Thresholds.NetDownMB);
            }));

            // 卡片 C
            mainScroll.Controls.Add(CreateCard("⚠️每日流量 (Daily Data Usage)", p => {
                AddHeaderRow(p);
                AddConfigRow(p, "上传总量 / Upload (MB)", _cfg.Thresholds.DataUpMB);
                AddConfigRow(p, "下载总量 / Download (MB)", _cfg.Thresholds.DataDownMB);
            }));

           
            // 卡片 D
            mainScroll.Controls.Add(CreateCard("⚠️弹窗通知 (Popup Alert)", p => {
                AddSingleRow(p, "高温报警触发线 / High Temp Limit (°C)", _cfg.AlertTempThreshold, v => _cfg.AlertTempThreshold = v);
            }));

            this.Controls.Add(mainScroll);

            // 3. 底部按钮栏
            var bottomPanel = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = S(50), // [修改] 高度 66 -> 50
                BackColor = C_Button_Bar
            };
            bottomPanel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 1, BackColor = C_Separator });

            var btnSave = CreateButton("保存 (Save)", C_Action, true);
            // [修改] 按钮位置微调
            btnSave.Location = new Point(this.ClientSize.Width - S(230), S(8)); 
            
            btnSave.Click += (s, e) => { 
                _sourceCfg.Thresholds = _cfg.Thresholds;
                _sourceCfg.AlertTempThreshold = _cfg.AlertTempThreshold;
                _sourceCfg.RecordedMaxCpuClock = _cfg.RecordedMaxCpuClock;
                _sourceCfg.RecordedMaxGpuClock = _cfg.RecordedMaxGpuClock;
                _sourceCfg.RecordedMaxCpuPower = _cfg.RecordedMaxCpuPower;
                _sourceCfg.RecordedMaxGpuPower = _cfg.RecordedMaxGpuPower;

                _sourceCfg.Save(); 
                
                this.DialogResult = DialogResult.OK; 
                this.Close(); 
            };
            var btnCancel = CreateButton("取消 (Cancel)", Color.FromArgb(70, 70, 70), false);
            // [修改] 按钮位置微调
            btnCancel.Location = new Point(this.ClientSize.Width - S(115), S(8));
            btnCancel.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);
            this.Controls.Add(bottomPanel);
            bottomPanel.BringToFront();
        }

        // === UI 构建核心逻辑 ===

        private Panel CreateCard(string title, Action<TableLayoutPanel> contentBuilder)
        {
            var card = new Panel
            {
                Width = S(500), // [修改] 宽度略微减小适应 Padding
                AutoSize = true,
                BackColor = C_Card,
                Margin = new Padding(0, 0, 0, S(8)), // [修改] 卡片间距 15 -> 8
                Padding = new Padding(1) 
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = F_Title,
                ForeColor = C_TextTitle,
                Location = new Point(S(10), S(10)), // [修改] 标题内边距 15 -> 10
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            var table = new TableLayoutPanel
            {
                Location = new Point(S(10), S(35)), // [修改] 表格起始位置上移
                Width = S(480),
                Height = 0,     
                AutoSize = true,
                ColumnCount = 3,
                BackColor = Color.Transparent,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            contentBuilder(table);

            // [修改] 底部留白减少
            card.Height = table.Bottom + S(6); 
            
            var line = new Label { 
                BackColor = C_Separator, 
                Height = 1, 
                Width = S(495), 
                Location = new Point(S(10), S(32)) // [修改] 分割线上移
            };
            card.Controls.Add(line);

            card.Controls.Add(table);
            return card;
        }

        private void AddHeaderRow(TableLayoutPanel t)
        {
            t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(new Label(), 0, t.RowCount - 1);

            var lblWarn = new Label { Text = "注意 (Warn)", ForeColor = C_Warn, Font = new Font(F_Label, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.Left };
            t.Controls.Add(lblWarn, 1, t.RowCount - 1);

            var lblCrit = new Label { Text = "重视 (Crit)", ForeColor = C_Crit, Font = new Font(F_Label, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.Left };
            t.Controls.Add(lblCrit, 2, t.RowCount - 1);
        }

        private void AddHardwareHeaderRow(TableLayoutPanel t)
        {
            t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(new Label(), 0, t.RowCount - 1);

            var lblCpu = new Label { Text = "CPU (Max)", ForeColor = C_Action, Font = new Font(F_Label, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.Left };
            t.Controls.Add(lblCpu, 1, t.RowCount - 1);

            var lblGpu = new Label { Text = "GPU (Max)", ForeColor = C_Action, Font = new Font(F_Label, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.Left };
            t.Controls.Add(lblGpu, 2, t.RowCount - 1);
        }

        private void AddDescriptionRow(TableLayoutPanel t, string text)
        {
            t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int row = t.RowCount - 1;

            var lbl = new Label
            {
                Text = text,
                ForeColor = C_TextSub, 
                Font = new Font(F_Label.FontFamily, 8.5F, FontStyle.Regular), // [修改] 字体更小一点
                AutoSize = true,
                Margin = new Padding(0, S(2), 0, S(6)) // [修改] 间距大幅减小
            };
            t.Controls.Add(lbl, 0, row);
            t.SetColumnSpan(lbl, 3); 
        }

        private void AddConfigRow(TableLayoutPanel t, string name, ValueRange range)
        {
            t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int row = t.RowCount - 1;

            var lbl = new Label
            {
                Text = name,
                ForeColor = C_TextMain,
                Font = F_Label,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, S(5), 0, S(5)) // [修改] 垂直间距 10 -> 4
            };
            t.Controls.Add(lbl, 0, row);

            var numWarn = CreateModernNum(range.Warn, C_Warn);
            numWarn.ValueChanged += (s, e) => range.Warn = (double)numWarn.Value;
            t.Controls.Add(numWarn, 1, row);

            var numCrit = CreateModernNum(range.Crit, C_Crit);
            numCrit.ValueChanged += (s, e) => range.Crit = (double)numCrit.Value;
            t.Controls.Add(numCrit, 2, row);
        }

        private void AddMaxLimitRow(TableLayoutPanel t, string name, float cpuVal, float gpuVal, Action<float> setCpu, Action<float> setGpu)
        {
            t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int row = t.RowCount - 1;

            var lbl = new Label
            {
                Text = name,
                ForeColor = C_TextMain,
                Font = F_Label,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, S(5), 0, S(5)) // [修改] 垂直间距 10 -> 4
            };
            t.Controls.Add(lbl, 0, row);

            var numCpu = CreateModernNum(cpuVal, C_Action);
            numCpu.ValueChanged += (s, e) => setCpu((float)numCpu.Value);
            t.Controls.Add(numCpu, 1, row);

            var numGpu = CreateModernNum(gpuVal, C_Action);
            numGpu.ValueChanged += (s, e) => setGpu((float)numGpu.Value);
            t.Controls.Add(numGpu, 2, row);
        }

        private void AddSingleRow(TableLayoutPanel t, string name, int val, Action<int> setter)
        {
            t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int row = t.RowCount - 1;

            var lbl = new Label
            {
                Text = name,
                ForeColor = C_TextMain,
                Font = F_Label,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, S(4), 0, S(4)) // [修改] 垂直间距 10 -> 4
            };
            t.Controls.Add(lbl, 0, row);

            var num = CreateModernNum(val, C_Crit);
            num.ValueChanged += (s, e) => setter((int)num.Value);
            t.Controls.Add(num, 2, row);
        }

        private NumericUpDown CreateModernNum(double val, Color accent)
        {
            var num = new NumericUpDown
            {
                Width = S(100),
                BackColor = C_InputBack,
                ForeColor = accent, 
                BorderStyle = BorderStyle.FixedSingle, 
                Minimum = 0,
                Maximum = 999999,
                DecimalPlaces = 0,
                Value = (decimal)val,
                Font = F_Value,
                TextAlign = HorizontalAlignment.Center,
                Margin = new Padding(0, S(1), 0, S(1)) // [修改] 输入框间距 8 -> 1
            };
            return num;
        }

        private Button CreateButton(string text, Color bg, bool isPrimary)
        {
            return new Button
            {
                Text = text,
                Size = new Size(S(100), S(32)), // [修改] 按钮稍微变小
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei UI", 9F, isPrimary ? FontStyle.Bold : FontStyle.Regular), // [修改] 字体 10 -> 9
                FlatAppearance = { BorderSize = 0 } 
            };
        }
    }
}