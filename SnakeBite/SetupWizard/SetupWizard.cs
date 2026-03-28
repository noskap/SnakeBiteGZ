using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SnakeBite;

namespace SnakeBite.SetupWizard
{
    public partial class SetupWizard : Form
    {
        private IntroPage introPage = new IntroPage();
        private FindInstallPage findInstallPage = new FindInstallPage();
        private CreateBackupPage createBackupPage = new CreateBackupPage();
        private MergeDatPage mergeDatPage = new MergeDatPage();
        private int displayPage = 0;
        private bool setupComplete = false;
        private SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);

        public SetupWizard()
        {
            InitializeComponent();
            FormClosing += formSetupWizard_Closing;
        }

        private void formSetupWizard_Load(object sender, EventArgs e)
        {
            buttonSkip.Visible = false;
            contentPanel.Controls.Add(introPage);
        }

        private void formSetupWizard_Closing(object sender, FormClosingEventArgs e)
        {
            if ((string)Tag == "noclose" && !(displayPage == 5))
                e.Cancel = true;
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            switch (displayPage)
            {
                case -1:
                    buttonBack.Visible = false;
                    contentPanel.Controls.Clear();
                    contentPanel.Controls.Add(introPage);
                    displayPage = 0;
                    break;

                case 0:
                    // move to find installation
                    buttonBack.Visible = true;
                    buttonSkip.Visible = false;
                    contentPanel.Controls.Clear();
                    contentPanel.Controls.Add(findInstallPage);
                    displayPage = 1;
                    break;

                case 1:
                    Properties.Settings.Default.InstallPath = findInstallPage.InstallPath;
                    Properties.Settings.Default.Save();

                    manager = new SettingsManager(GamePaths.SnakeBiteSettings);
                    if (!manager.ValidInstallPath)
                    {
                        MessageBox.Show("Please select a valid installation directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (!BackupManager.GameFilesExist())
                    {
                        MessageBox.Show("Some game data appears to be missing. If you have just revalidated the game data, please wait for Steam to finish downloading the new files before continuing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    // show create backup page, without processing panel, enable skip
                    createBackupPage.panelProcessing.Visible = false;
                    contentPanel.Controls.Clear();
                    contentPanel.Controls.Add(createBackupPage);
                    buttonSkip.Visible = true;
                    buttonSkip.Text = "&Skip Backup";

                    displayPage = 2;
                    break;

                case 2:
                    manager = new SettingsManager(GamePaths.SnakeBiteSettings);
                    if (!(manager.IsVanillaG0sSize() || manager.IsVanillaG0sHash()) && (SettingsManager.IntendedGameVersion >= ModManager.GetMGSVersion())) // not the right 00/01 and there hasn't been a game update
                    {
                        var overWrite = MessageBox.Show(string.Format("Your existing game data contains unexpected filesizes, and is likely already modified or predates Game Version {0}." +
                            "\n\nIt is recommended that you do NOT store these files as backups, unless you are absolutely certain that they can reliably restore your game to a safe state!" +
                            "\n\nAre you sure you want to save these as backup data?", SettingsManager.IntendedGameVersion), "Unexpected data_00.g0s / data_01.g0s Filesizes", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (overWrite != DialogResult.Yes) return;
                    }

                    string overWriteMessage;

                    if (BackupManager.BackupExists())
                    {
                        if (SettingsManager.IntendedGameVersion < ModManager.GetMGSVersion()) //A recent update has occurred and the user should probably create new backups
                        {
                            overWriteMessage = (string.Format("Some backup data already exists. Since this version of SnakeBite is intended for Ground Zeroes Version {0} and is now Ground Zeroes Version {1}, it is recommended that you overwrite your old backup files with new data.", SettingsManager.IntendedGameVersion, ModManager.GetMGSVersion()) +
                                "\n\nContinue?");
                        }
                        else
                        {
                            overWriteMessage = "Some backup data already exists. Continuing will overwrite your existing backups." +
                            "\n\nAre you sure you want to continue?";
                        }

                        var overWrite = MessageBox.Show(overWriteMessage, "Overwrite Existing Files?", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (overWrite != DialogResult.Yes) return;
                    }

                    // create backup
                    buttonSkip.Visible = false;
                    buttonBack.Visible = false;
                    buttonNext.Enabled = false;
                    createBackupPage.panelProcessing.Visible = true;
                    Application.UseWaitCursor = true;

                    // do backup processing
                    BackgroundWorker backupProcessor = new BackgroundWorker();
                    backupProcessor.DoWork += new DoWorkEventHandler(BackupManager.backgroundWorker_CopyBackupFiles);
                    backupProcessor.WorkerReportsProgress = true;
                    backupProcessor.ProgressChanged += new ProgressChangedEventHandler(backupProcessor_ProgressChanged);
                    backupProcessor.RunWorkerAsync();

                    while (backupProcessor.IsBusy)
                    {
                        Application.DoEvents();
                        Thread.Sleep(10);
                    }

                    // GZ: Skip Merge Dat Page
                    contentPanel.Controls.Clear();
                    contentPanel.Controls.Add(mergeDatPage); // Reusing this page for "Done" state for now, or just to hold place
                    
                    setupComplete = true; // Mark as complete immediately
                    
                    mergeDatPage.panelProcessing.Visible = false;
                    Application.UseWaitCursor = false;

                    mergeDatPage.labelWelcome.Text = "Setup complete";
                    mergeDatPage.labelWelcomeText.Text = "SnakeBite is configured and ready to use.";

                    buttonNext.Text = "Do&ne";
                    buttonNext.Enabled = true;

                    displayPage = 4; // Go to "Done" state
                    break;

                case 3:
                    // GZ: Skipped logic
                    break;

                case 4:
                    displayPage = 5;
                    DialogResult = DialogResult.OK;
                    Close();
                    break;
            }
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            displayPage -= 2;
            buttonNext_Click(null, null);
        }

      private void buttonSkip_Click(object sender, EventArgs e)
      {
          var result = MessageBox.Show(
              "Skipping backup creation is not recommended.\n\n" +
              "SnakeBiteGZ uses backups to safely restore your game files if something goes wrong.\n\n" +
              "Are you sure you want to skip creating backups?",
              "Skip Backup Creation?",
              MessageBoxButtons.YesNo,
              MessageBoxIcon.Warning);

          if (result != DialogResult.Yes) return;

          // Skip backup and go straight to completion
          buttonSkip.Visible = false;
          buttonBack.Visible = false;
          buttonNext.Enabled = false;

          createBackupPage.panelProcessing.Visible = false;
          Application.UseWaitCursor = false;

          // Jump to done state
          contentPanel.Controls.Clear();
          contentPanel.Controls.Add(mergeDatPage);

          setupComplete = true;

          mergeDatPage.panelProcessing.Visible = false;
          mergeDatPage.labelWelcome.Text = "Setup complete";
          mergeDatPage.labelWelcomeText.Text =
              "SnakeBiteGZ is configured and ready to use.\n\n" +
              "Note: You skipped creating backups. It is recommended to create them later via Settings.";

          buttonNext.Text = "Do&ne";
          buttonNext.Enabled = true;

          displayPage = 4;
      }

        private void GoToMergeDatPage()
        {
             // GZ: This should probably not be called or should lead to Done
             displayPage = 2;
             buttonNext_Click(null, null); 
        }

        private void backupProcessor_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            createBackupPage.labelWorking.Text = "Backing up " + (string)e.UserState + ". Please Wait...";
        }

        private void mergeProcessor_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            mergeDatPage.labelWorking.Text = (string)e.UserState + ". Please Wait...";
        }

        private void mergeProcessor_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                setupComplete = false;
            else
                setupComplete = true;

        }
    }
}