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
    bool isRunning = true;
    Label discordStatus;
    NotifyIcon trayIcon;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    // --- КОНСТРУКТОР ФОРМЫ ---
    public AppWindow()
    {
        // 1. Настройки окна (Увеличили высоту до 240, чтобы статус не обрезался)
        this.Text = "Steam Gamepad Auto-Launcher";
        this.ClientSize = new Size(360, 240);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        this.BackColor = Color.FromArgb(28, 28, 30);
        this.ForeColor = Color.FromArgb(242, 242, 247);
        this.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        ContextMenuStrip trayMenu = new ContextMenuStrip();

        // Добавляем пункт "Развернуть" и пишем для него метод показа
        trayMenu.Items.Add("Развернуть", null, (sender, e) =>
        {
            this.Show();                          // Показываем окно в системе
            this.WindowState = FormWindowState.Normal; // Разворачиваем в нормальный размер
            this.Activate();                      // Выводим на передний план
        });

        // Добавляем пункт "Выход" и пишем метод полного закрытия
        trayMenu.Items.Add("Выход", null, (sender, e) =>
        {
            trayIcon.Visible = false; // Выключаем иконку, чтобы она не зависла в трее
            Application.Exit();       // Полностью закрываем программу
        });

        // 2. Инициализируем саму иконку в трее
        trayIcon = new NotifyIcon();
        trayIcon.Text = "Steam Gamepad Auto-Launcher"; // Подсказка при наведении мыши
        trayIcon.ContextMenuStrip = trayMenu;          // Привязываем наше созданное меню

        // Извлекаем иконку из самого EXE-файла проекта
        try
        {
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch { }

        trayIcon.Visible = true; // Включаем отображение в трее

        // 3. Пишем метод показа окна по ДВОЙНОМУ КЛИКУ на иконку
        trayIcon.DoubleClick += (sender, e) =>
        {
            this.Show();                          // Показываем окно
            this.WindowState = FormWindowState.Normal; // Разворачиваем
            this.Activate();                      // Выводим вперед
        };

        this.Resize += (sender, e) =>
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide(); // Если окно свернули — полностью прячем его с экрана и панели задач
            }
        };

        // Метод-перехватчик крестика (FormClosing)
        this.FormClosing += (sender, e) =>
        {
            // Если программу закрывает сам пользователь (нажал на крестик)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Отменяем стандартное уничтожение окна
                this.Hide();     // Просто прячем форму с экрана

                // Показываем всплывающее уведомление Windows над часами на 2 секунды
                trayIcon.ShowBalloonTip(2000, "Авто-лаунчер", "Программа свернута в трей и продолжает работу.", ToolTipIcon.Info);
            }
        };

        try
        {
            // Берём иконку, которую вшили в сам EXE-файл проекта
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            // Резервный вариант, если что-то пошло не так
        }

        // 2. Блок инструкции
        Panel infoPanel = new Panel();
        infoPanel.Location = new Point(20, 20);
        infoPanel.Size = new Size(320, 85);
        infoPanel.BackColor = Color.FromArgb(44, 44, 46);
        Controls.Add(infoPanel);

        Label instruction = new Label();
        instruction.AutoSize = false;
        instruction.Dock = DockStyle.Fill;
        instruction.Padding = new Padding(12);
        instruction.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        instruction.ForeColor = Color.FromArgb(229, 229, 234); // Ярко-белый текст для читаемости на темном
        instruction.Text = "» Мониторинг активности запущен.\nПрограмма автоматически определяет игры и управляет Steam Big Picture.";
        infoPanel.Controls.Add(instruction); // Важно: добавляем текст ИМЕННО внутрь панели

        // 3. Инициализация глобального статуса Discord
        discordStatus = new Label();
        discordStatus.Location = new Point(20, 140); // Сместили чуть ниже панели
        discordStatus.Size = new Size(320, 30);
        discordStatus.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        discordStatus.ForeColor = Color.FromArgb(114, 137, 218); // Фирменный цвет Discord
        discordStatus.Text = "[ СТАТУС ]: Вне игры";
        Controls.Add(discordStatus); // Важно: добавляем статус напрямую на форму

        // 4. Запуск потока
        this.Load += (sender, e) =>
        {
            Thread backgroundThread = new Thread(CheckGamepadLoop);
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        isRunning = false;
        base.OnFormClosing(e);
    }

    void CheckGamepadLoop()
    {
        while (isRunning)
        {
            IntPtr GetWindow = GetForegroundWindow();
            int activeProcessId;
            string activeProcessName = "";
            string activeProcessPath = "";
            GetWindowThreadProcessId(GetWindow, out activeProcessId);

            try
            {
                using (Process process = Process.GetProcessById(activeProcessId))
                {
                    activeProcessName = process.ProcessName;
                    activeProcessPath = process.MainModule?.FileName ?? "";
                }
            }
            catch (Exception)
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
                        discordStatus.Text = $"[ ИГРАЕТ В ]: {activeProcessName.ToUpper()}";
                        discordStatus.ForeColor = Color.FromArgb(67, 181, 129); // Зеленый
                    }
                    else
                    {
                        discordStatus.Text = "[ СТАТУС ]: Вне игры";
                        discordStatus.ForeColor = Color.FromArgb(114, 137, 218); // Фиолетовый
                    }
                }));
            }

            Thread.Sleep(1000);
        }
    }
}