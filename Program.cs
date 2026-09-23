using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeVault
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public sealed class MainForm : Form
    {
        // ---- palette ----
        private static readonly Color BgColor = ColorTranslator.FromHtml("#f6f5f2");
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color BorderColor = ColorTranslator.FromHtml("#d8d3c6");
        private static readonly Color TextColor = ColorTranslator.FromHtml("#1c1b19");
        private static readonly Color MutedColor = ColorTranslator.FromHtml("#6f6a5e");
        private static readonly Color AccentColor = ColorTranslator.FromHtml("#c9622a");
        private static readonly Color GoodColor = ColorTranslator.FromHtml("#3d8b5f");
        private static readonly Color WarnColor = ColorTranslator.FromHtml("#c98a1f");
        private static readonly Color BadColor = ColorTranslator.FromHtml("#c23b3b");

        private readonly Font _monoFont = new("Consolas", 10f);
        private readonly Font _monoFontSmall = new("Consolas", 8.5f);
        private readonly Font _bigCodeFont = new("Consolas", 30f, FontStyle.Bold);
        private readonly Font _headingFont = new("Segoe UI", 12f, FontStyle.Bold);
        private readonly Font _hintFont = new("Segoe UI", 8f);

        // ---- settings view controls ----
        private Panel _settingsPanel = null!;
        private TextBox _secretBox = null!;
        private ComboBox _digitsBox = null!;
        private ComboBox _periodBox = null!;
        private Label _errorLabel = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Button _clearButton = null!;

        // ---- main view controls ----
        private Panel _mainPanel = null!;
        private Label _codeLabel = null!;
        private Label _countdownLabel = null!;
        private Label _copiedLabel = null!;
        private Button _gearButton = null!;

        private System.Windows.Forms.Timer _timer = null!;

        private byte[]? _secretBytes;
        private int _digits = 6;
        private int _period = 30;
        private bool _hasSavedSecret;

        public MainForm()
        {
            Text = "Code Vault";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgColor;
            Font = new Font("Segoe UI", 9f);

            try
            {
                Icon = new Icon("CodeVault.ico");
            }
            catch
            {
                // fine to run without a custom icon if it's missing
            }

            BuildSettingsPanel();
            BuildMainPanel();

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (_, _) => Tick();

            Load += (_, _) => LoadOnStartup();
        }

        // ---------------- settings view ----------------

        private void BuildSettingsPanel()
        {
            _settingsPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(380, 420),
                BackColor = BgColor,
            };

            var eyebrow = new Label
            {
                Text = "RFC 6238",
                Font = _monoFontSmall,
                ForeColor = MutedColor,
                AutoSize = true,
                Location = new Point(22, 18),
            };

            var heading = new Label
            {
                Text = "Code Vault Setup",
                Font = _headingFont,
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(22, 36),
            };

            var secretLabel = new Label
            {
                Text = "SECRET KEY (BASE32)",
                Font = _monoFontSmall,
                ForeColor = MutedColor,
                AutoSize = true,
                Location = new Point(22, 74),
            };

            _secretBox = new TextBox
            {
                Location = new Point(22, 92),
                Size = new Size(336, 26),
                Font = _monoFont,
                BorderStyle = BorderStyle.FixedSingle,
            };

            var digitsLabel = new Label
            {
                Text = "DIGITS",
                Font = _monoFontSmall,
                ForeColor = MutedColor,
                AutoSize = true,
                Location = new Point(22, 130),
            };

            _digitsBox = new ComboBox
            {
                Location = new Point(22, 148),
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = _monoFont,
            };
            _digitsBox.Items.AddRange(new object[] { "6", "8" });
            _digitsBox.SelectedIndex = 0;

            var periodLabel = new Label
            {
                Text = "PERIOD (S)",
                Font = _monoFontSmall,
                ForeColor = MutedColor,
                AutoSize = true,
                Location = new Point(198, 130),
            };

            _periodBox = new ComboBox
            {
                Location = new Point(198, 148),
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = _monoFont,
            };
            _periodBox.Items.AddRange(new object[] { "30", "60" });
            _periodBox.SelectedIndex = 0;

            _errorLabel = new Label
            {
                Text = "",
                Font = _monoFontSmall,
                ForeColor = BadColor,
                AutoSize = false,
                Location = new Point(22, 182),
                Size = new Size(336, 16),
            };

            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(22, 206),
                Size = new Size(336, 34),
                BackColor = AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += (_, _) => OnSave();

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(22, 246),
                Size = new Size(336, 30),
                BackColor = BgColor,
                ForeColor = MutedColor,
                FlatStyle = FlatStyle.Flat,
                Visible = false,
            };
            _cancelButton.FlatAppearance.BorderColor = BorderColor;
            _cancelButton.Click += (_, _) => OnCancel();

            _clearButton = new Button
            {
                Text = "Clear saved secret",
                Location = new Point(22, 282),
                Size = new Size(336, 30),
                BackColor = BgColor,
                ForeColor = BadColor,
                FlatStyle = FlatStyle.Flat,
                Visible = false,
            };
            _clearButton.FlatAppearance.BorderColor = BadColor;
            _clearButton.Click += (_, _) => OnClear();

            var hint = new Label
            {
                Text = "The secret is encrypted with Windows DPAPI and saved next to this " +
                       "app as CodeVault.dat. It can only be decrypted by your Windows " +
                       "account on this machine — copying the file elsewhere, or another " +
                       "account reading it, yields nothing usable.",
                Font = _hintFont,
                ForeColor = MutedColor,
                AutoSize = false,
                Location = new Point(22, 326),
                Size = new Size(336, 80),
            };

            _settingsPanel.Controls.AddRange(new Control[]
            {
                eyebrow, heading, secretLabel, _secretBox,
                digitsLabel, _digitsBox, periodLabel, _periodBox,
                _errorLabel, _saveButton, _cancelButton, _clearButton, hint
            });

            Controls.Add(_settingsPanel);
        }

        // ---------------- main (code) view ----------------

        private void BuildMainPanel()
        {
            _mainPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(340, 240),
                BackColor = BgColor,
                Visible = false,
            };

            _gearButton = new Button
            {
                Text = "⚙",
                Font = new Font("Segoe UI", 11f),
                Location = new Point(292, 14),
                Size = new Size(30, 30),
                BackColor = SurfaceColor,
                ForeColor = MutedColor,
                FlatStyle = FlatStyle.Flat,
            };
            _gearButton.FlatAppearance.BorderColor = BorderColor;
            _gearButton.Click += (_, _) => ShowSettings();

            var eyebrow = new Label
            {
                Text = "CODE VAULT",
                Font = _monoFontSmall,
                ForeColor = MutedColor,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 46),
                Size = new Size(340, 16),
            };

            _codeLabel = new Label
            {
                Text = "------",
                Font = _bigCodeFont,
                ForeColor = TextColor,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 78),
                Size = new Size(340, 50),
                Cursor = Cursors.Hand,
            };
            _codeLabel.Click += (_, _) => OnCopy();

            _countdownLabel = new Label
            {
                Text = "30s",
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = GoodColor,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 134),
                Size = new Size(340, 20),
            };

            _copiedLabel = new Label
            {
                Text = "",
                Font = _monoFontSmall,
                ForeColor = GoodColor,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 160),
                Size = new Size(340, 16),
            };

            var tip = new Label
            {
                Text = "Click the code to copy",
                Font = _hintFont,
                ForeColor = MutedColor,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 196),
                Size = new Size(340, 16),
            };

            _mainPanel.Controls.AddRange(new Control[]
            {
                _gearButton, eyebrow, _codeLabel, _countdownLabel, _copiedLabel, tip
            });

            Controls.Add(_mainPanel);
        }

        // ---------------- behavior ----------------

        private void LoadOnStartup()
        {
            try
            {
                VaultConfig? cfg = ConfigStore.Load();
                if (cfg != null)
                {
                    _secretBytes = Totp.Base32Decode(cfg.Secret);
                    _digits = cfg.Digits;
                    _period = cfg.Period;
                    _hasSavedSecret = true;
                    ShowMain();
                    return;
                }
            }
            catch
            {
                // fall through to settings if the saved file is unreadable
            }

            _hasSavedSecret = false;
            ShowSettings();
        }

        private void ShowSettings()
        {
            _timer.Stop();

            _cancelButton.Visible = _hasSavedSecret;
            _clearButton.Visible = _hasSavedSecret;
            _errorLabel.Text = "";

            ClientSize = new Size(380, _hasSavedSecret ? 420 : 380);
            _mainPanel.Visible = false;
            _settingsPanel.Visible = true;
            _secretBox.Focus();
        }

        private void ShowMain()
        {
            ClientSize = new Size(340, 240);
            _settingsPanel.Visible = false;
            _mainPanel.Visible = true;

            Tick();
            _timer.Start();
        }

        private void Tick()
        {
            var now = DateTimeOffset.UtcNow;
            int remaining = Totp.SecondsRemaining(_period, now);

            _countdownLabel.Text = remaining + "s";
            _countdownLabel.ForeColor = remaining <= 5 ? BadColor : remaining <= 10 ? WarnColor : GoodColor;

            if (_secretBytes == null)
                return;

            try
            {
                string code = Totp.Generate(_secretBytes, _digits, _period, now);
                int mid = (code.Length + 1) / 2;
                _codeLabel.Text = code[..mid] + " " + code[mid..];
                _codeLabel.Tag = code;
            }
            catch
            {
                _codeLabel.Text = "ERROR";
            }
        }

        private void OnSave()
        {
            _errorLabel.Text = "";
            string raw = _secretBox.Text.Trim();

            if (string.IsNullOrEmpty(raw))
            {
                _errorLabel.Text = "Enter a secret key first.";
                return;
            }

            byte[] parsed;
            try
            {
                parsed = Totp.Base32Decode(raw);
            }
            catch (Exception ex)
            {
                _errorLabel.Text = ex.Message;
                return;
            }

            int digits = int.Parse((string)_digitsBox.SelectedItem!);
            int period = int.Parse((string)_periodBox.SelectedItem!);

            try
            {
                ConfigStore.Save(raw, digits, period);
            }
            catch (Exception ex)
            {
                _errorLabel.Text = "Could not save CodeVault.dat: " + ex.Message;
                return;
            }

            _secretBytes = parsed;
            _digits = digits;
            _period = period;
            _hasSavedSecret = true;
            _secretBox.Text = "";

            ShowMain();
        }

        private void OnCancel()
        {
            if (!_hasSavedSecret)
                return;

            _secretBox.Text = "";
            ShowMain();
        }

        private void OnClear()
        {
            ConfigStore.Delete();
            _secretBytes = null;
            _hasSavedSecret = false;
            _secretBox.Text = "";
            ShowSettings();
        }

        private void OnCopy()
        {
            if (_codeLabel.Tag is not string raw)
                return;

            try
            {
                Clipboard.SetText(raw);
                _copiedLabel.Text = "Copied to clipboard";
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (_, _) => { _copiedLabel.Text = ""; t.Stop(); t.Dispose(); };
                t.Start();
            }
            catch
            {
                _copiedLabel.Text = "Copy failed, select manually.";
            }
        }
    }
}
