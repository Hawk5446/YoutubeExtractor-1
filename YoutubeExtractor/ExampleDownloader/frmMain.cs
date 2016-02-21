using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExtractor;
namespace ExampleDownloader
{
    public partial class frmMain : Form
    {
        Timer uiUpdater = new Timer();
        ListViewItemsUIList lviUI = new ListViewItemsUIList();
        string baseURL;
        MethodInvoker add;
        public frmMain()
        {
            InitializeComponent();
            uiUpdater.Interval = 200;
            uiUpdater.Tick += uiUpdater_Tick;
            uiUpdater.Start();
        }

        private void uiUpdater_Tick(object sender, EventArgs e)
        {
            lviUI.applyUpdates();
        }

        private void addItem(ListViewItem lvi)
        {
            lvStatus.Items.Add(lvi);
        }

        private void cmdProcess_Click(object sender, EventArgs e)
        {
            baseURL = this.txtURL.Text;
            lvStatus.Items.Clear();
            lviUI.Clear();
            System.Threading.Thread th = new System.Threading.Thread(processItems);
            th.Start();
        }

        private void processItems()
        {
            int index = 0;
            ListViewItem lvi;
            try
            {
                List<string> values = DownloadUrlResolver.ExtractLinks(baseURL);
                if (values.Count() == 0)
                {
                    values.Add(baseURL);
                }
                foreach (string s in values)
                {
                    index++;
                    lvi = new ListViewItem();
                    lviUI.addNew(lvi, index.ToString(), s, @"Pending... ");
                    add = delegate { this.addItem(lvi); };
                    this.lvStatus.Invoke(add);
                }
                index = 0;
                IEnumerable<VideoInfo> videoInfos;
                foreach (string s in values)
                {
                    try
                    {
                        lviUI.updateText(index, 2, "Preprocessing...");
                        videoInfos = DownloadUrlResolver.GetDownloadUrls(s, false);
                        lviUI.updateText(index, 2, "Download Starting...");
                        DownloadVideo(index,videoInfos);
                    }
                    catch (Exception e)
                    {
                        lviUI.updateText(index,2,"Error. " + e.Message);
                        System.Diagnostics.Debug.Print(e.ToString());
                    }
                    index++;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.ToString());
            }
        }
        private void DownloadVideo(int index, IEnumerable<VideoInfo> videoInfos)
        {

            /*
             * Select the first .mp4 video with 360p resolution
             */
            VideoInfo video = videoInfos
                .First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);

            /*
             * If the video has a decrypted signature, decipher it
             */
            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }

            /*
             * Create the video downloader.
             * The first argument is the video to download.
             * The second argument is the path to save the video file.
             */
            var videoDownloader = new VideoDownloader(video,
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RemoveIllegalPathCharacters(video.Title) + video.VideoExtension));
            // Register the ProgressChanged event and print the current progress
            videoDownloader.DownloadProgressChanged += (sender, args) => lviUI.updateText(index,2, ((int)args.ProgressPercentage).ToString() + "%");

            /*
             * Execute the video downloader.
             * For GUI applications note, that this method runs synchronously.
             */
            videoDownloader.Execute();
        }
        private static string RemoveIllegalPathCharacters(string path)
        {
            string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
            var r = new System.Text.RegularExpressions.Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
            return r.Replace(path, "");
        }
    }
    public class ListViewItemsUIList : List<ListViewItemUI>
    {
        public void updateText(int itemIndex, int textIndex, string text)
        {
            this[itemIndex].columns[textIndex] = text;
            this[itemIndex].changed = true;
        }
        public void addNew(ListViewItem lvi, params string[] columns)
        {
            this.Add(new ListViewItemUI(lvi, columns));
        }
        public void applyUpdates()
        {
            foreach(ListViewItemUI lviUI in this)
            {
                lviUI.applyUpdates();
            }
        }
    }
    public class ListViewItemUI
    {
        public ListViewItemUI(ListViewItem lvi, params string[] columns)
        {
            this.columns = new List<string>();
            this.columns.AddRange(columns);
            this.lvi = lvi;
            this.changed = true;
        }
        public bool changed { get; set; }
        public ListViewItem lvi { get; set; }
        public List<string> columns { get; set; }
        public void applyUpdates()
        {
            if (lvi.SubItems.Count != this.columns.Count)
            {
                this.lvi.SubItems.Clear(); //enforcement
                if (columns.Count >= 1)
                {
                    lvi.Text = columns[0];
                }
                for (int i = 1; i < columns.Count; i++)
                {
                    lvi.SubItems.Add(columns[i]);
                }
                this.changed = false;
            }
            if (changed)
            {
                if(columns.Count >= 1)
                {
                    lvi.Text = columns[0];
                }
                for(int i = 1; i < lvi.SubItems.Count && i < columns.Count(); i++)
                {
                    lvi.SubItems[i].Text = columns[i];
                }
            }
            this.changed = false;
        }
    }
}
