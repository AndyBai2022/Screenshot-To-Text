using IronOcr;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
 



namespace screenCaptrue
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }



        void ScreenCapture()
        {
            DLL.PrScrn();
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            Hotkey.ProcessHotKey(m);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            DLL.PrScrn();
            if (Clipboard.ContainsImage()) { 
            pictureBox1.Image = Clipboard.GetImage();
                //Clipboard.GetImage().Save(@"D:\temp.png");
            
            }
            this.WindowState = FormWindowState.Normal;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //注册热键(窗体句柄,热键ID,辅助键,实键)   
            try
            {
                Hotkey.Regist(this.Handle, HotkeyModifiers.MOD_ALT, Keys.F1, ScreenCapture);
            }
            catch(Exception te)
            {
                //MessageBox.Show("Alt + A 热键被占用");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //注消热键(句柄,热键ID)   
            Hotkey.UnRegist(this.Handle, ScreenCapture);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //richTextBox1.Text = await ConvertTxtAsync().Text; //X

            progressBar1.Value = 0;
            var progress = new Progress<int>(percent =>
            {
                progressBar1.Value = percent;
                

            });


            //OcrResult ocrResult = await ConvertTxtAsync(progress);
            OcrResult ocrResult = await Task.Run(()=>ConvertTxtAsync(progress));
            //progressBar1.Value = 1000;
            richTextBox1.Text = ocrResult.Text;

        }

        //private async Task<OcrResult> ConvertTxtAsync()
        private async Task<OcrResult> ConvertTxtAsync(IProgress<int> progress)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("Please take screenshot first!");
                return null;
            }
            //draw progress bar
            for (int i = 1; i <= 100; i++)
            {
                Thread.Sleep(100);
                if (progress != null)
                    progress.Report(i);
            }



            var Ocr = new IronTesseract();
            Ocr.Language = OcrLanguage.ChineseSimplifiedBest;
            using (var Input = new OcrInput())
            {
                Input.AddImage(pictureBox1.Image);

                OcrResult convertedText =  await Ocr.ReadAsync(Input);
                return convertedText;

              }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //there should be no GUI component method
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //this is updated from doWwork.Its where GUI components are updated
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {//called when the heavy operation in bg is over.Can also acCept GUI components

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }

    public class DLL
    {
        [DllImport("PrScrn.dll", EntryPoint = "PrScrn")]

        public extern static int PrScrn();//与dll中一致   
    }



    public static class Hotkey
    {
        
        #region 系统api
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, HotkeyModifiers fsModifiers, Keys vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion

        /// <summary> 
        /// 注册快捷键 
        /// </summary> 
        /// <param name="hWnd">持有快捷键窗口的句柄</param> 
        /// <param name="fsModifiers">组合键</param> 
        /// <param name="vk">快捷键的虚拟键码</param> 
        /// <param name="callBack">回调函数</param> 
        public static void Regist(IntPtr hWnd, HotkeyModifiers fsModifiers, Keys vk, HotKeyCallBackHanlder callBack)
        {
            int id = keyid++;
            if (!RegisterHotKey(hWnd, id, fsModifiers, vk))
                throw new Exception("regist hotkey fail.");
            keymap[id] = callBack;
        }

        /// <summary> 
        /// 注销快捷键 
        /// </summary> 
        /// <param name="hWnd">持有快捷键窗口的句柄</param> 
        /// <param name="callBack">回调函数</param> 
        public static void UnRegist(IntPtr hWnd, HotKeyCallBackHanlder callBack)
        {
            foreach (KeyValuePair<int, HotKeyCallBackHanlder> var in keymap)
            {
                if (var.Value == callBack)
                    UnregisterHotKey(hWnd, var.Key);
            }
        }

        /// <summary> 
        /// 快捷键消息处理 
        /// </summary> 
        public static void ProcessHotKey(System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                HotKeyCallBackHanlder callback;
                if (keymap.TryGetValue(id, out callback))
                {
                    callback();
                }
            }
        }

        const int WM_HOTKEY = 0x312;
        static int keyid = 10;
        static Dictionary<int, HotKeyCallBackHanlder> keymap = new Dictionary<int, HotKeyCallBackHanlder>();

        public delegate void HotKeyCallBackHanlder();
    }

    public enum HotkeyModifiers
    {
        MOD_ALT = 0x1,
        MOD_CONTROL = 0x2,
        MOD_SHIFT = 0x4,
        MOD_WIN = 0x8
    }
}
