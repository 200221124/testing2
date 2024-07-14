using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;

namespace iWebITKiosk
{
    public partial class MainForm : Form
    {
        // Importação de funções da user32.dll para manipulação de hotkeys e hooks de teclado
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public MainForm()
        {
            InitializeComponent();
            SetupKioskMode();
            InitializeAsync();
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        // Configurar o modo quiosque (tela cheia, sem bordas, sempre no topo)
        private void SetupKioskMode()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.KeyPreview = true;
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);
        }

        private async void InitializeAsync()
        {
            try
            {
                var webViewEnvironment = await CoreWebView2Environment.CreateAsync(null, null);
                await webView2.EnsureCoreWebView2Async(webViewEnvironment);
                LoadWebPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Carregar a página da web a partir do ficheiro config.json
        private void LoadWebPage()
        {
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                var url = JObject.Parse(json)["url"]?.ToString();

                if (webView2 != null && webView2.CoreWebView2 != null)
                {
                    if (!string.IsNullOrEmpty(url))
                    {
                        webView2.CoreWebView2.Navigate(url);
                    }
                    else
                    {
                        MessageBox.Show("Invalid URL in config.json", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("WebView2 control or CoreWebView2 is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("config.json file not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Debug.Assert(webView2 != null, "WebView2 control is not initialized.");
            if (webView2 != null)
            {
                webView2.Dock = DockStyle.Fill;
                RegisterHotKeys();
            }
        }

        // Impedir que o utilizador feche a janela
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }

        // Suprimir as combinações Alt+F4 e Alt+Tab
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.F4) || keyData == (Keys.Alt | Keys.Tab))
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Registrar hotkeys para "desativar" a tecla Windows
        private void RegisterHotKeys()
        {
            RegisterHotKey(this.Handle, 1, 0x0000, (int)Keys.LWin);
            RegisterHotKey(this.Handle, 2, 0x0000, (int)Keys.RWin);
        }

        // Desregistrar hotkeys
        private void UnregisterHotKeys()
        {
            UnregisterHotKey(this.Handle, 1);
            UnregisterHotKey(this.Handle, 2);
        }

        // Interceptar mensagens de hotkeys para suprimir a tecla Windows
        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            UnregisterHotKeys();
            UnhookWindowsHookEx(_hookID);
        }

        // Configurar hook para interceptar teclas
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
                else
                {
                    throw new InvalidOperationException("Failed to get current module.");
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == (int)Keys.LWin || vkCode == (int)Keys.RWin || (Control.ModifierKeys == Keys.Alt && vkCode == (int)Keys.Tab))
                {
                    // Suprimir tecla Windows e Alt+Tab
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // Importação de função da kernel32.dll para obter o handle do módulo
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
