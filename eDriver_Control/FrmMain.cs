using System.IO;
using System.IO.Ports;
using System.Data.SqlClient;
using System.Threading;
using System.Text;
using Microsoft.VisualBasic.Logging;
using System.Runtime.InteropServices;

namespace eDriver_Control
{
    public partial class FrmMain : Form
    {
        SerialPort m_spEDriver = new SerialPort();

        //---------------- Keyboard Hooking : USB Scanner

        private string m_strBarcodeScannerString = string.Empty;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static LowLevelKeyboardProc? callbackDelegate;

        const int WH_KEYBOARD_LL = 13;

        const int WM_KEYDOWN = 0x100;

        private IntPtr hook = IntPtr.Zero;

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(uint uCode, uint uMapType);

        private void SetHook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            callbackDelegate = new LowLevelKeyboardProc(HookProc);
            hook = SetWindowsHookEx(WH_KEYBOARD_LL, callbackDelegate, hInstance, 0);
        }

        private void UnHook()
        {
            if (callbackDelegate == null) return;

            UnhookWindowsHookEx(hook);

        }

        private IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Enter(Return)이면 입력
                if (vkCode == 0x0D)
                {
                    //key flush
                    this.Enabled = false;
                    Application.DoEvents();
                    this.Enabled = true;

                    setBarcode();
                    m_strBarcodeScannerString = string.Empty;
                }
                else
                {
                    int nonVirtualKey = MapVirtualKey((uint)vkCode, 2);
                    char mappedChar = Convert.ToChar(nonVirtualKey);

                    m_strBarcodeScannerString += mappedChar;
                }
            }

            return CallNextHookEx(hook, code, (int)wParam, lParam); // 키입력을 정상적으로 동작하게 합니다.
        }

        int m_iLastRYNo = -1;
        int m_iLastAction = -1;

        Thread? m_tMainThread;
        bool m_bMainThread = false;

        string m_strPort_Barcode = string.Empty;
        string m_strPort_eDriver = string.Empty;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void button_Bolt_CNT_PLUS_M3_Click(object sender, EventArgs e)
        {

        }

        private void button_Bolt_CNT_MINUS_M3_Click(object sender, EventArgs e)
        {

        }

        private void button_Bolt_CNT_PLUS_M4M5_Click(object sender, EventArgs e)
        {

        }

        private void button_Bolt_CNT_MINUS_M4M5_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button_WorkComplete.Visible = false;
            this.WindowState = FormWindowState.Maximized;

            button2.Text = "설정 수량";
            button4.Text = "체결 수량";
            button7.Text = "수량\r\n증가";
            button8.Text = "수량\r\n감소";

            m_strPort_Barcode = GetAppConfig("PortBarcode");
            if (m_strPort_Barcode == string.Empty)
            {
                MessageBox.Show("바코드 리더기의 포트가 설정되어 있지 않습니다.", "바코드 리더기 설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            m_strPort_eDriver = GetAppConfig("PortEDriver");
            if (m_strPort_eDriver == string.Empty)
            {
                MessageBox.Show("전동 드라이버의 포트가 설정되어 있지 않습니다.", "전동 드라이버 설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            m_bMainThread = true;
            m_tMainThread = new Thread(monitor);
            m_tMainThread.Start();
        }

        private void 아이템코드관리ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmItemCode dlg = new FrmItemCode(this);
            dlg.ShowDialog();
        }

        private void bARCODETESTToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        public SqlConnection? connectDB_TEAMDB()
        {
            SqlConnection sqlConnection_TEAMDB = new SqlConnection("data source=172.28.140.136;Initial Catalog=DB_INV;uid=sa;pwd=1qaz2wsx3edc");

            try
            {
                sqlConnection_TEAMDB.Open();
                return sqlConnection_TEAMDB;
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString(), "RTY Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                writeLog(e.ToString());
                return null;
            }
        }

        public void writeLog(string strContent)
        {
            string m_strLogPath = System.IO.Directory.GetCurrentDirectory() + "\\Log";

            DirectoryInfo di = new DirectoryInfo(m_strLogPath);
            if (di.Exists == false)
                di.Create();

            string logFileName = "";

            logFileName = DateTime.Now.ToString("yyyyMMdd") + ".log";

            string logline = string.Empty;

            logline = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "," + strContent + "\r\n";

            try
            {
                FileStream writeData = new FileStream(m_strLogPath + "\\" + logFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                writeData.Write(Encoding.UTF8.GetBytes(logline), 0, Encoding.UTF8.GetByteCount(logline));
                writeData.Close();
            }
            catch
            {
                return;
            }
        }

        private bool OpenEDriver()
        {
            m_spEDriver = new SerialPort();
            if (m_spEDriver.IsOpen == true)
            {
                m_spEDriver.Close();
                System.Threading.Thread.Sleep(10);
            }

            // Set the port's settings
            m_spEDriver.PortName = m_strPort_eDriver;
            m_spEDriver.BaudRate = 9600;
            m_spEDriver.DataBits = 8;
            m_spEDriver.Parity = Parity.None;
            m_spEDriver.StopBits = StopBits.One;
            m_spEDriver.DataReceived += new SerialDataReceivedEventHandler(ReceiveEDriver);

            try
            {
                // Open the port
                m_spEDriver.Open();
                m_spEDriver.ReadTimeout = 1000;
                return true;
            }
            catch (Exception ex)
            {
                writeLog(ex.ToString());
            }

            m_spEDriver.Dispose();
            return false;
        }

        private void ReceiveEDriver(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            byte[] byteReceive = new byte[1024];
            int read = sp.Read(byteReceive, 0, 1024);

            int iRY_No = -1;
            int iAction = -1;

            if (read > 0)
            {
                if ((byteReceive[0] == 0x52) && (byteReceive[0] == 0x59))   //RY
                {
                    iRY_No = Convert.ToInt32(byteReceive[3]);
                    iAction = Convert.ToInt32(byteReceive[5]);
                }
            }

            m_iLastAction = iAction;
            m_iLastRYNo = iRY_No;
        }

        private void setBarcode()
        {
            string str = string.Empty;

            for (int i = 0; i < m_strBarcodeScannerString.Length; i++)
            {
                if (m_strBarcodeScannerString[i] == '\0')
                    continue;

                str += m_strBarcodeScannerString[i];
            }
        }

        private void monitor()
        {
            while (true)
            {



                if (m_bMainThread == false)
                    return;

                Thread.Sleep(10);
            }
        }

        public static string GetAppConfig(string key)
        {
            if (File.Exists(".\\config.ini") == false)
                return string.Empty;

            IniFile ini = new IniFile();
            ini.Load(".\\config.ini");

            if (ini["appSettings"].ContainsKey(key) == false)
                return string.Empty;
            else
            {
                string strReturn = ini["appSettings"][key].ToString();
                if (strReturn == "")
                    return string.Empty;
                else
                    return strReturn;
            }

        }

        public static void SetAppConfig(string key, string value)
        {
            if (File.Exists(".\\config.ini") == false)
            {
                IniFile iniCreate = new IniFile();
                iniCreate.Add("appSettings");
                iniCreate.Save(".\\config.ini");
            }

            IniFile ini = new IniFile();
            ini.Load(".\\config.ini");

            ini["appSettings"][key] = value;
            ini.Save(".\\config.ini");

        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_tMainThread != null)
            {
                m_bMainThread = false;
                Thread.Sleep(100);
            }
        }
    }
}