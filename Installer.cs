using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using System.Net;

namespace AntigravityUkrainianInstaller
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        public const string CurrentVersion = "1.0.0";
        public const string RepoOwner = "RiasJ1Dar";
        public const string RepoName = "antigravity-ua";

        private string latestReleaseUrl = "https://github.com/" + RepoOwner + "/" + RepoName + "/releases/latest";
        private string latestReleaseTag = "";
        private bool updateAvailable = false;

        private Panel pnlUpdate;
        private Label lblUpdateText;
        private Button btnUpdateDownload;

        private TextBox txtPath;
        private Button btnBrowse;
        private Label lblAppStatus;
        private Label lblBackupStatus;
        private Label lblProcessStatus;
        private Button btnInstall;
        private Button btnRestore;
        private Button btnLaunch;
        private Button btnKillProcess;
        private ProgressBar progressBar;
        private Label lblProgress;
        private LinkLabel lnkFooter;

        public MainForm()
        {
            InitializeComponent();
            CheckStatuses();
            this.Shown += (s, e) => CheckUpdateOnStartup();
        }

        private void InitializeComponent()
        {
            this.Text = "Локалізація Antigravity 2.0 (Українська версія)";
            this.Size = new Size(580, 485);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(26, 115, 232)
            };

            Label lblTitle = new Label
            {
                Text = "Українізатор Google Antigravity 2.0",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Location = new Point(20, 14),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Автономний встановлювач українського інтерфейсу • Версія " + CurrentVersion,
                ForeColor = Color.FromArgb(232, 240, 254),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(22, 46),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);

            // Update Notification Banner (between Header and Path)
            pnlUpdate = new Panel
            {
                Location = new Point(20, 86),
                Size = new Size(525, 38),
                BackColor = Color.FromArgb(232, 240, 254),
                Visible = false
            };

            lblUpdateText = new Label
            {
                Text = "🚀 Доступна нова версія локалізації!",
                ForeColor = Color.FromArgb(26, 115, 232),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(12, 10),
                AutoSize = true
            };

            btnUpdateDownload = new Button
            {
                Text = "Завантажити оновлення ↗",
                Location = new Point(335, 5),
                Size = new Size(180, 28),
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUpdateDownload.FlatAppearance.BorderSize = 0;
            btnUpdateDownload.Click += BtnUpdateDownload_Click;

            pnlUpdate.Controls.Add(lblUpdateText);
            pnlUpdate.Controls.Add(btnUpdateDownload);
            this.Controls.Add(pnlUpdate);

            // Group: Installation Path
            GroupBox grpPath = new GroupBox
            {
                Text = "Каталог встановлення Antigravity",
                Location = new Point(20, 130),
                Size = new Size(525, 72),
                ForeColor = Color.FromArgb(32, 33, 36)
            };

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity");
            txtPath = new TextBox
            {
                Text = defaultPath,
                Location = new Point(15, 28),
                Size = new Size(405, 25),
                Font = new Font("Segoe UI", 9f)
            };
            txtPath.TextChanged += (s, e) => CheckStatuses();

            btnBrowse = new Button
            {
                Text = "Огляд...",
                Location = new Point(430, 26),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.System
            };
            btnBrowse.Click += BtnBrowse_Click;

            grpPath.Controls.Add(txtPath);
            grpPath.Controls.Add(btnBrowse);
            this.Controls.Add(grpPath);

            // Group: Status
            GroupBox grpStatus = new GroupBox
            {
                Text = "Поточний стан системи",
                Location = new Point(20, 210),
                Size = new Size(525, 100),
                ForeColor = Color.FromArgb(32, 33, 36)
            };

            lblAppStatus = new Label
            {
                Location = new Point(15, 23),
                Size = new Size(495, 20),
                Text = "Перевірка наявності Antigravity..."
            };

            lblProcessStatus = new Label
            {
                Location = new Point(15, 47),
                Size = new Size(360, 20),
                Text = "Перевірка процесів..."
            };

            btnKillProcess = new Button
            {
                Text = "Закрити процеси",
                Location = new Point(380, 44),
                Size = new Size(130, 25),
                BackColor = Color.FromArgb(254, 239, 239),
                ForeColor = Color.FromArgb(197, 34, 31),
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            btnKillProcess.FlatAppearance.BorderColor = Color.FromArgb(245, 194, 193);
            btnKillProcess.Click += BtnKillProcess_Click;

            lblBackupStatus = new Label
            {
                Location = new Point(15, 71),
                Size = new Size(495, 20),
                Text = "Перевірка резервної копії..."
            };

            grpStatus.Controls.Add(lblAppStatus);
            grpStatus.Controls.Add(lblProcessStatus);
            grpStatus.Controls.Add(btnKillProcess);
            grpStatus.Controls.Add(lblBackupStatus);
            this.Controls.Add(grpStatus);

            // Progress Bar & Status Text
            progressBar = new ProgressBar
            {
                Location = new Point(20, 318),
                Size = new Size(525, 10),
                Style = ProgressBarStyle.Blocks,
                Visible = false
            };
            this.Controls.Add(progressBar);

            lblProgress = new Label
            {
                Location = new Point(20, 332),
                Size = new Size(525, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(95, 99, 104),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                Text = "Готовий до встановлення"
            };
            this.Controls.Add(lblProgress);

            // Action Buttons
            btnInstall = new Button
            {
                Text = "✓ Встановити локалізацію",
                Location = new Point(20, 358),
                Size = new Size(210, 42),
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += BtnInstall_Click;

            btnRestore = new Button
            {
                Text = "⟲ Відновити оригінал (EN)",
                Location = new Point(240, 358),
                Size = new Size(180, 42),
                BackColor = Color.FromArgb(241, 243, 244),
                ForeColor = Color.FromArgb(60, 64, 67),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRestore.FlatAppearance.BorderColor = Color.FromArgb(218, 220, 224);
            btnRestore.Click += BtnRestore_Click;

            btnLaunch = new Button
            {
                Text = "▶ Запустити",
                Location = new Point(430, 358),
                Size = new Size(115, 42),
                BackColor = Color.FromArgb(52, 168, 83),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Click += BtnLaunch_Click;

            this.Controls.Add(btnInstall);
            this.Controls.Add(btnRestore);
            this.Controls.Add(btnLaunch);

            // Footer Link
            lnkFooter = new LinkLabel
            {
                Location = new Point(20, 412),
                Size = new Size(525, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(95, 99, 104),
                ActiveLinkColor = Color.FromArgb(26, 115, 232),
                Text = "Локалізація v" + CurrentVersion + " • GitHub: " + RepoOwner + "/" + RepoName
            };
            lnkFooter.LinkClicked += LnkFooter_LinkClicked;
            this.Controls.Add(lnkFooter);
        }

        private string GetAppAsarPath()
        {
            return Path.Combine(txtPath.Text.Trim(), "resources", "app.asar");
        }

        private string GetBackupAsarPath()
        {
            return Path.Combine(txtPath.Text.Trim(), "resources", "app.asar.bak");
        }

        private string GetExePath()
        {
            return Path.Combine(txtPath.Text.Trim(), "Antigravity.exe");
        }

        private bool IsAntigravityRunning()
        {
            Process[] p1 = Process.GetProcessesByName("Antigravity");
            Process[] p2 = Process.GetProcessesByName("language_server");
            return (p1.Length > 0 || p2.Length > 0);
        }

        private void KillAntigravityProcesses()
        {
            Process[] list = Process.GetProcesses();
            foreach (var p in list)
            {
                try
                {
                    string name = p.ProcessName.ToLower();
                    if (name == "antigravity" || name == "language_server")
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                }
                catch { }
            }
        }

        private void CheckStatuses()
        {
            string appAsar = GetAppAsarPath();
            string exePath = GetExePath();
            string bakPath = GetBackupAsarPath();

            if (File.Exists(appAsar) && File.Exists(exePath))
            {
                lblAppStatus.Text = "● Antigravity знайдено: " + txtPath.Text;
                lblAppStatus.ForeColor = Color.FromArgb(19, 115, 51);
                btnInstall.Enabled = true;
                btnLaunch.Enabled = true;
            }
            else
            {
                lblAppStatus.Text = "✕ Antigravity не знайдено за цим шляхом!";
                lblAppStatus.ForeColor = Color.FromArgb(217, 48, 37);
                btnInstall.Enabled = false;
                btnLaunch.Enabled = false;
            }

            if (IsAntigravityRunning())
            {
                lblProcessStatus.Text = "⚠ Antigravity або language_server працює в пам'яті.";
                lblProcessStatus.ForeColor = Color.FromArgb(179, 100, 0);
                btnKillProcess.Visible = true;
            }
            else
            {
                lblProcessStatus.Text = "● Процеси Antigravity зупинено (файл вільний для запису).";
                lblProcessStatus.ForeColor = Color.FromArgb(19, 115, 51);
                btnKillProcess.Visible = false;
            }

            if (File.Exists(bakPath))
            {
                FileInfo fi = new FileInfo(bakPath);
                lblBackupStatus.Text = string.Format("● Резервна копія оригіналу існує ({0:N0} байт)", fi.Length);
                lblBackupStatus.ForeColor = Color.FromArgb(19, 115, 51);
                btnRestore.Enabled = true;
            }
            else
            {
                lblBackupStatus.Text = "○ Резервну копію буде створено автоматично при першому встановленні.";
                lblBackupStatus.ForeColor = Color.FromArgb(95, 99, 104);
                btnRestore.Enabled = false;
            }
        }

        private void CheckUpdateOnStartup()
        {
            // Одноразова перевірка при запуску програми
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "Antigravity-UA-Setup/" + CurrentVersion);
                        string url = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";
                        string json = client.DownloadString(url);

                        string tag = ExtractJsonField(json, "tag_name");
                        string htmlUrl = ExtractJsonField(json, "html_url");

                        if (!string.IsNullOrEmpty(tag))
                        {
                            latestReleaseTag = tag;
                            if (!string.IsNullOrEmpty(htmlUrl))
                            {
                                latestReleaseUrl = htmlUrl;
                            }

                            string cleanLatest = tag.TrimStart('v', 'V');
                            bool isNewer = false;

                            Version vCur, vLat;
                            if (Version.TryParse(CurrentVersion, out vCur) && Version.TryParse(cleanLatest, out vLat))
                            {
                                isNewer = (vLat > vCur);
                            }
                            else
                            {
                                isNewer = string.Compare(cleanLatest, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0;
                            }

                            updateAvailable = isNewer;

                            this.BeginInvoke(new Action(() =>
                            {
                                if (isNewer)
                                {
                                    pnlUpdate.Visible = true;
                                    lblUpdateText.Text = "🚀 Доступна нова версія: " + tag + "!";
                                    lnkFooter.Text = "Доступне оновлення: " + tag + " • Натисніть для переходу на GitHub";
                                    lnkFooter.LinkColor = Color.FromArgb(26, 115, 232);

                                    // Повідомлення при запуску
                                    DialogResult res = MessageBox.Show(this,
                                        "Вийшла нова версія української локалізації (" + tag + ")!\n\nБажаєте відкрити сторінку завантаження оновлення зараз?",
                                        "Доступне оновлення локалізації", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                                    if (res == DialogResult.Yes)
                                    {
                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = latestReleaseUrl,
                                            UseShellExecute = true
                                        });
                                    }
                                }
                                else
                                {
                                    lnkFooter.Text = "✓ Найновіша версія (v" + CurrentVersion + ") • GitHub: " + RepoOwner + "/" + RepoName;
                                    lnkFooter.LinkColor = Color.FromArgb(19, 115, 51);
                                }
                            }));
                        }
                    }
                }
                catch
                {
                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            lnkFooter.Text = "Локалізація v" + CurrentVersion + " • GitHub: " + RepoOwner + "/" + RepoName;
                        }));
                    }
                    catch { }
                }
            });
        }

        private string ExtractJsonField(string json, string fieldName)
        {
            string pattern = "\"" + fieldName + "\":";
            int idx = json.IndexOf(pattern);
            if (idx == -1) return null;
            int start = json.IndexOf("\"", idx + pattern.Length);
            if (start == -1) return null;
            int end = json.IndexOf("\"", start + 1);
            if (end == -1) return null;
            return json.Substring(start + 1, end - start - 1).Trim();
        }

        private void BtnUpdateDownload_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = latestReleaseUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не вдалося відкрити посилання:\n" + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LnkFooter_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = updateAvailable ? latestReleaseUrl : ("https://github.com/" + RepoOwner + "/" + RepoName),
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Виберіть папку встановлення Antigravity:";
                dlg.SelectedPath = txtPath.Text;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = dlg.SelectedPath;
                    CheckStatuses();
                }
            }
        }

        private void BtnKillProcess_Click(object sender, EventArgs e)
        {
            KillAntigravityProcesses();
            Thread.Sleep(500);
            CheckStatuses();
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (updateAvailable)
            {
                DialogResult updChoice = MessageBox.Show(this,
                    "На GitHub доступна новіша версія українізатора (" + latestReleaseTag + ")!\n\nБажаєте відкрити сторінку завантаження найновішої версії?",
                    "Доступне оновлення", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);

                if (updChoice == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = latestReleaseUrl, UseShellExecute = true });
                    return;
                }
                else if (updChoice == DialogResult.Cancel)
                {
                    return;
                }
            }

            string appAsar = GetAppAsarPath();
            string bakAsar = GetBackupAsarPath();

            if (!File.Exists(appAsar))
            {
                MessageBox.Show(this, "Файл " + appAsar + " не знайдено!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (IsAntigravityRunning())
            {
                DialogResult res = MessageBox.Show(this, 
                    "Antigravity наразі працює. Для оновлення файлів програму необхідно закрити.\n\nЗакрити Antigravity зараз?", 
                    "Закриття програми", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (res == DialogResult.Yes)
                {
                    KillAntigravityProcesses();
                    Thread.Sleep(600);
                }
                else
                {
                    return;
                }
            }

            try
            {
                progressBar.Visible = true;
                progressBar.Value = 20;
                lblProgress.Text = "Створення резервної копії оригіналу...";
                lblProgress.Refresh();

                // 1. Create backup if it doesn't already exist
                if (!File.Exists(bakAsar))
                {
                    File.Copy(appAsar, bakAsar, false);
                }

                progressBar.Value = 50;
                lblProgress.Text = "Видобування українського пакету локалізації...";
                lblProgress.Refresh();

                // 2. Extract embedded resource
                Assembly asm = Assembly.GetExecutingAssembly();
                string resourceName = null;
                foreach (string name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("app_uk.asar", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = name;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(resourceName))
                {
                    throw new Exception("Вбудований ресурс app_uk.asar не знайдено в інсталяторі!");
                }

                string tempAsar = appAsar + ".tmp";
                using (Stream src = asm.GetManifestResourceStream(resourceName))
                using (FileStream dst = new FileStream(tempAsar, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[65536];
                    int bytesRead;
                    while ((bytesRead = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dst.Write(buffer, 0, bytesRead);
                    }
                }

                progressBar.Value = 85;
                lblProgress.Text = "Застосування файлів...";
                lblProgress.Refresh();

                // 3. Replace app.asar atomically
                if (File.Exists(appAsar))
                {
                    File.Delete(appAsar);
                }
                File.Move(tempAsar, appAsar);

                progressBar.Value = 100;
                lblProgress.Text = "Локалізацію успішно встановлено!";
                CheckStatuses();

                MessageBox.Show(this, 
                    "Українську локалізацію для Antigravity 2.0 успішно встановлено!\n\nТепер ви можете запустити програму.", 
                    "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                progressBar.Visible = false;
                lblProgress.Text = "Помилка встановлення: " + ex.Message;
                MessageBox.Show(this, "Помилка під час встановлення:\n" + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            string appAsar = GetAppAsarPath();
            string bakAsar = GetBackupAsarPath();

            if (!File.Exists(bakAsar))
            {
                MessageBox.Show(this, "Файл резервної копії не знайдено!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (IsAntigravityRunning())
            {
                DialogResult res = MessageBox.Show(this, 
                    "Antigravity наразі працює. Закрити програму для відновлення оригіналу?", 
                    "Підтвердження", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (res == DialogResult.Yes)
                {
                    KillAntigravityProcesses();
                    Thread.Sleep(600);
                }
                else
                {
                    return;
                }
            }

            try
            {
                progressBar.Visible = true;
                progressBar.Value = 50;
                lblProgress.Text = "Відновлення оригінального файлу...";
                lblProgress.Refresh();

                File.Copy(bakAsar, appAsar, true);

                progressBar.Value = 100;
                lblProgress.Text = "Оригінал успішно відновлено!";
                CheckStatuses();

                MessageBox.Show(this, 
                    "Оригінальну англійську версію Antigravity успішно відновлено!", 
                    "Відновлення завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                progressBar.Visible = false;
                lblProgress.Text = "Помилка відновлення: " + ex.Message;
                MessageBox.Show(this, "Помилка під час відновлення:\n" + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            string exePath = GetExePath();
            if (File.Exists(exePath))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath)
                    };
                    Process.Start(psi);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Не вдалося запустити Antigravity:\n" + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(this, "Файл Antigravity.exe не знайдено!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
