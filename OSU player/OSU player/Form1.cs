﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using Un4seen.Bass;

namespace OSU_player
{

    public partial class Form1
    {
        public Form1()
        {
            InitializeComponent();
        }

        public Videofiles uni_Video = new Videofiles();
        public Audiofiles uni_Audio;
        public QQ uni_QQ = new QQ();
        // Thread DelayVideo = new Thread((delegate() { Thread.Sleep(10); }));
        int Nextmode = 3;
        List<ListViewItem> FullList = new List<ListViewItem>();
        BeatmapSet CurrentSet;
        Beatmap CurrentBeatmap;
        BeatmapSet TmpSet;
        Beatmap TmpBeatmap;
        float Allvolume = 1.0f;
        float Musicvolume = 0.8f;
        float Fxvolume = 0.6f;
        bool playvideo = true;
        bool playfx = true;
        bool playsb = true;

        #region 各种方法
        private void AskForExit(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            DialogResult close;
            close = MessageBox.Show("确认退出？", "提示", MessageBoxButtons.YesNo);
            if (close == DialogResult.Yes)
            {
                this.Dispose();
            }
            else
            {
                e.Cancel = true;
            }
            uni_QQ.Send2QQ(Core.uin, "");
            if (uni_Audio != null) { uni_Audio.Dispose(); };
        }
        private void printdetail()
        {
            ListDetail.Items.Clear();
            for (int i = (int)OSUfile.FileVersion; i < (int)OSUfile.OSUfilecount; i++)
            {
                ListViewItem tmpl = new ListViewItem(Enum.GetName(typeof(OSUfile), i).ToString());
                tmpl.SubItems.Add(TmpBeatmap.Rawdata[i]);
                ListDetail.Items.Add(tmpl);
            }
        }
        private void setbg()
        {
            printdetail();
            uni_Video.initbg(CurrentBeatmap.Background);
            uni_Video.Play(this.panel2);
        }
        private void Stop()
        {
            // AVsyncer.Enabled = false;
            uni_Audio.Stop();
            uni_Audio.Dispose();
            uni_Video.Stop();
            TrackBar1.Enabled = false;
            TrackBar1.Value = 0;
            StopButton.Enabled = false;
            PlayButton.Text = "播放";
            //  DelayVideo.Abort();
        }
        private void Play()
        {
            uni_Audio = new Audiofiles(CurrentBeatmap.Audio);
            uni_Audio.UpdateTimer.Tick += new EventHandler(AVsync);
            uni_Audio.Play(Allvolume * Musicvolume);
            if (CurrentBeatmap.haveVideo&& playvideo)
            {
                uni_Video.init(Path.Combine(CurrentBeatmap.location, CurrentBeatmap.Video));
                if (CurrentBeatmap.VideoOffset > 0)
                {
                    Thread.Sleep(CurrentBeatmap.VideoOffset / 1000);
                    uni_Video.Play(this.panel2);
                }
                else { uni_Video.Play(this.panel2); }
            }
            TrackBar1.Enabled = true;
            //    AVsyncer.Enabled = true;
            uni_QQ.Send2QQ(Core.uin, CurrentBeatmap.name);
            PlayButton.Text = "暂停";
            StopButton.Enabled = true;
        }
        private void Pause()
        {
            uni_Video.Pause();
            uni_Audio.Pause();
            uni_QQ.Send2QQ(Core.uin, "");
            PlayButton.Text = "播放";
        }
        private void Resume()
        {
            uni_Video.Pause();
            uni_Audio.Pause();
            //   AVsyncer.Enabled = true;
            PlayButton.Text = "暂停";
        }
        private void PlayNext()
        {
            Stop();
            int next;
            int now;
            if (PlayList.SelectedItems.Count == 0) { now = 0; }
            else { now = PlayList.SelectedItems[0].Index; };
            switch (Nextmode)
            {
                case 1: next = (now + 1) % PlayList.Items.Count;
                    break;
                case 2: next = now;
                    break;
                case 3: next = new Random().Next() % PlayList.Items.Count;
                    break;
                default: next = 0;
                    break;
            }
            PlayList.Select();
            if (PlayList.SelectedItems.Count != 0)
            {
                PlayList.SelectedItems[0].Selected = false;
            }
            PlayList.Items[next].Selected = true;
            PlayList.EnsureVisible(next);
            TmpSet = Core.allsets[Convert.ToInt32(PlayList.SelectedItems[0].SubItems[1].Text)];
            TmpBeatmap = TmpSet.Diffs[0];
            CurrentSet = TmpSet;
            CurrentBeatmap = TmpBeatmap;
            Play();
        }
        private void scanforset(string path)
        {
            string[] osufiles = Directory.GetFiles(path, "*.osu");
            if (osufiles.Length != 0)
            {
                BeatmapSet tmp = new BeatmapSet(path);
                //tmp.GetDetail();
                Core.allsets.Add(tmp);
                this.backgroundWorker1.ReportProgress(0, tmp.name);
            }
            else
            {
                string[] tmpfolder = Directory.GetDirectories(path);
                foreach (string subfolder in tmpfolder)
                {
                    scanforset(subfolder);
                }
            }
        }
        private void initset()
        {
            try
            {
                if (Directory.Exists(Path.Combine(Core.osupath, "Songs")))
                {
                    this.backgroundWorker1.RunWorkerAsync(Path.Combine(Core.osupath, "Songs"));
                }
            }
            catch (SystemException ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw (new FormatException("Failed to read song path", ex));
            }
        }
        #endregion
        private void Form1_Load(object sender, EventArgs e)
        {
            BassNet.Registration("sqh1994@163.com", "2X280331512622");
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            new Thread(new ThreadStart(Selfupdate.check_update)).Start();
            Core.Getpath();
            MessageBox.Show("将开始初始化");
            initset();
        }
        private void AVsync(object sender, EventArgs e)
        {
            if (!uni_Audio.isplaying)
            {
                PlayNext();
                return;
            }
            TrackBar1.Value = (int)Math.Round((uni_Audio.position / uni_Audio.durnation) * TrackBar1.Maximum);
            label1.Text = String.Format("{0}:{1:D2} / {2}:{3:D2}", (int)uni_Audio.position / 60,
                (int)uni_Audio.position % 60, (int)uni_Audio.durnation / 60,
                (int)uni_Audio.durnation % 60);
            if (uni_Audio.durnation == uni_Audio.position)
            {
                PlayNext();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            scanforset(e.Argument.ToString());
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState.ToString().Length > 0)
            {
                ListViewItem tmpl = new ListViewItem(e.UserState.ToString());
                PlayList.Items.Add(tmpl);
            }
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (ListViewItem item in PlayList.Items)
            {
                item.SubItems.Add(item.Index.ToString());
                FullList.Add(item);
            }
            MessageBox.Show(string.Format("初始化完毕，发现曲目{0}个", Core.allsets.Count));
            button3.Enabled = false;
            PlayList.Items[0].Selected = true;
        }


        #region 菜单栏
        private void button3_Click(object sender, EventArgs e)
        {
            initset();
        }
        #region 文件
        private void 运行OSU_Click(object sender, EventArgs e)
        {
            Process.Start(Path.Combine(Core.osupath, "osu!.exe"));
        }
        private void 手动指定OSU目录_Click(object sender, EventArgs e)
        {

        }
        private void 重新导入osu_Click(object sender, EventArgs e)
        {

        }
        private void 重新导入collections_Click(object sender, EventArgs e)
        {

        }
        private void 打开曲目文件夹_Click(object sender, EventArgs e)
        {
            Process.Start(TmpSet.location);
        }
        private void 打开铺面文件_Click(object sender, EventArgs e)
        {
            Process.Start("notepad.exe", TmpBeatmap.path);

        }
        private void 打开SB文件_Click(object sender, EventArgs e)
        {
            Process.Start("notepad.exe", TmpSet.OsbPath);
        }
        private void 退出_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion
        #region 工具
        private void 重复歌曲扫描_Click(object sender, EventArgs e)
        {
            using (DelDulp dialog = new DelDulp())
            {
                dialog.Show();
            }
        }
        #endregion
        #region 选项
        private void 随机播放_Click(object sender, EventArgs e)
        {
            Nextmode = 3;
        }
        private void 顺序播放_Click(object sender, EventArgs e)
        {
            Nextmode = 1;
        }
        private void 单曲循环_Click(object sender, EventArgs e)
        {
            Nextmode = 2;
        }
        private void 音效_Click(object sender, EventArgs e)
        {
            playfx = 音效.Checked;
        }
        private void 视频开关_Click(object sender, EventArgs e)
        {
            playvideo = 视频开关.Checked;
        }
        private void QQ状态同步_Click(object sender, EventArgs e)
        {
            Core.syncQQ = QQ状态同步.Checked;
        }
        private void SB开关_Click(object sender, EventArgs e)
        {
            playsb = SB开关.Checked;
        }
        #endregion
        private void 关于_Click(object sender, EventArgs e)
        {
            using (About dialog = new About())
            {
                dialog.Show();
            }

        }

        #endregion
        #region 第一排
        private void Button2_Click(object sender, EventArgs e)
        {

        }
        private void Button1_Click(object sender, EventArgs e)
        {
            using (Form2 dialog = new Form2())
            {
                dialog.Show();
            }
            LabelQQ.Text = "当前同步QQ：" + Core.uin.ToString();
        }
        private void TrackBar3_Scroll(object sender, EventArgs e)
        {
            Fxvolume = (float)TrackBar3.Value / (float)TrackBar3.Maximum;
        }
        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            Musicvolume = (float)trackBar4.Value / (float)trackBar4.Maximum;
            if (uni_Audio != null) { uni_Audio.Volume = Allvolume * Musicvolume; }

        }
        #endregion
        #region 第二排
        private void PlayList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PlayList.SelectedItems.Count == 0) { return; }
            DiffList.Items.Clear();
            TmpSet = Core.allsets[Convert.ToInt32(PlayList.SelectedItems[0].SubItems[1].Text)];
            if (!TmpSet.detailed)
            {
                TmpSet.GetDetail();
            }
            foreach (var s in TmpSet.diffstr)
            {
                DiffList.Items.Add(s);
            }
            if (!File.Exists(TmpSet.OsbPath))
            { 打开SB文件.Enabled = false; }
            else
            { 打开SB文件.Enabled = true; }
            DiffList.SelectedIndex = 0;
            //因为改变selectedindex会触发listbox的进程，所以以下省略了
        }
        private void DiffList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PlayList.SelectedIndices.Count == 0)
            {
                return;
            }
            TmpBeatmap = TmpSet.Diffs[DiffList.SelectedIndex];
            if (!StopButton.Enabled)
            {
                CurrentSet = TmpSet;
                CurrentBeatmap = TmpBeatmap;
                uni_Video.Stop();
                setbg();
            }
            else if (uni_Audio.isplaying)
            {
                printdetail();
            }
            else
            {
                Stop();
                setbg();
            }
        }
        private void PlayList_DoubleClick(object sender, EventArgs e)
        {
            TmpSet = Core.allsets[Convert.ToInt32(PlayList.SelectedItems[0].SubItems[1].Text)];
            TmpBeatmap = TmpSet.Diffs[0];
            CurrentSet = TmpSet;
            CurrentBeatmap = TmpBeatmap;
            Stop();
            setbg();
            Play();
        }
        private void DiffList_DoubleClick(object sender, EventArgs e)
        {
            TmpSet = Core.allsets[Convert.ToInt32(PlayList.SelectedItems[0].SubItems[1].Text)];
            TmpBeatmap = TmpSet.Diffs[DiffList.SelectedIndex];
            CurrentSet = TmpSet;
            CurrentBeatmap = TmpBeatmap;
            Stop();
            setbg();
            Play();
        }
        private void StopButton_Click(object sender, EventArgs e)
        {
            Stop();
        }
        private void NextButton_Click(object sender, EventArgs e)
        {
            PlayNext();
        }
        private void TrackBar1_Scroll(object sender, EventArgs e)
        {
            uni_Audio.Seek(TrackBar1.Value * uni_Audio.durnation / TrackBar1.Maximum);
            uni_Video.seek(TrackBar1.Value * uni_Audio.durnation / TrackBar1.Maximum + CurrentBeatmap.VideoOffset / 1000);
        }
        private void TrackBar2_Scroll(object sender, EventArgs e)
        {
            Allvolume = (float)TrackBar2.Value / (float)TrackBar2.Maximum;
            if (uni_Audio != null) { uni_Audio.Volume = Allvolume * Musicvolume; }

        }
        private void SearchButton_Click(object sender, EventArgs e)
        {
            Stop();
            PlayList.Items.Clear();
            if (TextBox1.Text == "")
            {
                foreach (ListViewItem item in FullList)
                {
                    PlayList.Items.Add(item);
                }
            }
            else
            {
                foreach (ListViewItem item in FullList)
                {
                    if (item.Text.ToLower().Contains(TextBox1.Text.ToLower()))
                    {
                        PlayList.Items.Add(item);
                    }
                }
            }
            if (PlayList.Items.Count != 0) { PlayList.Items[0].Selected = true; }

        }
        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (StopButton.Enabled == false)
            {
                Play();
            }
            else
            {
                if (PlayButton.Text == "播放")
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }

        }
        #endregion
    }
}