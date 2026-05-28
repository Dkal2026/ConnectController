using Windows.Gaming.Input;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

Application.Run(new AppWindow());
public class AppWindow : Form
{
    // --- ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ КЛАССА ---
    // Теперь их видят абсолютно все методы внутри этого класса
    bool wasConnect = false;
    Label discordStatus;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    // --- КОНСТРУКТОР ФОРМЫ ---
    public AppWindow()
    {
        this.Text = "Steam Gamepad Auto-Launcher";
        this.ClientSize = new Size(360, 200);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        this.BackColor = Color.FromArgb(28, 28, 30);
        this.ForeColor = Color.FromArgb(242, 242, 247);
        this.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        try { this.Icon = new Icon("icon.ico"); } catch { }

        // Блок инструкции
        Panel infoPanel = new Panel();
        infoPanel.Location = new Point(20, 20);
        infoPanel.Size = new Size(320, 80);
        infoPanel.BackColor = Color.FromArgb(44, 44, 46);
        Controls.Add(infoPanel);

        Label instruction = new Label();
        instruction.AutoSize = false;
        instruction.Dock = DockStyle.Fill;
        instruction.Padding = new Padding(12);
        instruction.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        instruction.ForeColor = Color.FromArgb(229, 229, 234);
        instruction.Text = "✨ Мониторинг активности запущен.\nПрограмма автоматически определяет игры и управляет Steam Big Picture.";
        infoPanel.Controls.Add(instruction);

        // Инициализируем наш статус (слово Label в начале ПИСАТЬ НЕ НАДО, так как переменная уже создана наверху!)
        discordStatus = new Label();
        discordStatus.Location = new Point(20, 130);
        discordStatus.Size = new Size(320, 30);
        discordStatus.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        discordStatus.ForeColor = Color.FromArgb(114, 137, 218); // Цвет Discord
        discordStatus.Text = "💤 Статус: Вне игры";
        Controls.Add(discordStatus);

        this.Load += (sender, e) =>
        {
            Thread backgroundThread = new Thread(CheckGamepadLoop);
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        };
    }
    void CheckGamepadLoop()
    {
        while (true)
        {
            IntPtr GetWindow = GetForegroundWindow();
            int activeProccessId;
            string activeProcessName = "";
            string activeProcessPath = "";
            GetWindowThreadProcessId(GetWindow, out activeProccessId);

            try
            {
                Process process = Process.GetProcessById(activeProccessId);
                activeProcessName = process.ProcessName;
                activeProcessPath = process.MainModule.FileName;
            }
            catch(Exception)
            {
                activeProcessName = "";
                activeProcessPath = "";
            }
            int gamepadCount = Gamepad.Gamepads.Count;
            bool isGameRunnig = false;

            if ((activeProcessPath.ToLower().Contains("games") || activeProcessPath.ToLower().Contains("steamapps")) && activeProcessName.ToLower() != "steam" && activeProcessName.ToLower() != "steamwebhelper" && activeProcessName.ToLower() != "gameoverlayui")
            {
                isGameRunnig = true;
            }

            if (gamepadCount > 0 && wasConnect == false && isGameRunnig == false)
            {
                wasConnect = true;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "steam://open/bigpicture",
                    UseShellExecute = true,
                }
                );


            }
            if (wasConnect == true && (gamepadCount == 0 && isGameRunnig == false))
            {
                wasConnect = false;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "steam://close/bigpicture",
                    UseShellExecute = true,
                }
                    );
            }
            if (this.IsHandleCreated)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    if (isGameRunnig && !string.IsNullOrEmpty(activeProcessName))
                    {
                        discordStatus.Text = $"🎮 Играет в: {activeProcessName}";
                        discordStatus.ForeColor = Color.FromArgb(67, 181, 129); // Зеленый статус игры
                    }
                    else
                    {
                        discordStatus.Text = "💤 Статус: Вне игры";
                        discordStatus.ForeColor = Color.FromArgb(114, 137, 218); // Фиолетовый статус покоя
                    }
                }));
            }
            Thread.Sleep(1000);
        }
    }

}