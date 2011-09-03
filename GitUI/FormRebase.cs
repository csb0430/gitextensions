﻿using System;
using System.Drawing;
using System.Windows.Forms;
using GitCommands;
using ResourceManager.Translation;

namespace GitUI
{
    public partial class FormRebase : GitExtensionsForm
    {
        private readonly TranslationString _currentBranchText = new TranslationString("Current branch:");

        private readonly TranslationString _continueRebaseText = new TranslationString("Continue rebase");
        private readonly TranslationString _solveConflictsText = new TranslationString("Solve conflicts");

        private readonly TranslationString _solveConflictsText2 = new TranslationString(">Solve conflicts<");
        private readonly TranslationString _continueRebaseText2 = new TranslationString(">Continue rebase<");

        private readonly TranslationString _noBranchSelectedText = new TranslationString("Please select a branch");

        private readonly TranslationString _branchUpToDateText = 
            new TranslationString("Current branch a is up to date." + Environment.NewLine + "Nothing to rebase.");
        private readonly TranslationString _branchUpToDateCaption = new TranslationString("Rebase");

        private readonly string _defaultBranch;

        public FormRebase(string defaultBranch)
        {
            InitializeComponent();
            Translate();
            _defaultBranch = defaultBranch;
        }

        private void FormRebaseFormClosing(object sender, FormClosingEventArgs e)
        {
            SavePosition("rebase");
        }

        private void FormRebaseLoad(object sender, EventArgs e)
        {
            RestorePosition("rebase");

            var selectedHead = Settings.Module.GetSelectedBranch();
            Currentbranch.Text = _currentBranchText.Text + " " + selectedHead;

            Branches.DisplayMember = "Name";
            Branches.DataSource = Settings.Module.GetHeads(true, true);

            if (_defaultBranch != null)
                Branches.Text = _defaultBranch;

            Branches.Select();

            splitContainer2.SplitterDistance = Settings.Module.InTheMiddleOfRebase() ? 0 : 74;
            EnableButtons();

            // Honor the rebase.autosquash configuration.
            var autosquashSetting = Settings.Module.GetEffectiveSetting("rebase.autosquash");
            chkAutosquash.Checked = "true" == autosquashSetting.Trim().ToLower();
        }

        private void EnableButtons()
        {
            if (Settings.Module.InTheMiddleOfRebase())
            {
                if (Height < 200)
                    Height = 500;

                Branches.Enabled = false;
                Ok.Enabled = false;

                AddFiles.Enabled = true;
                Resolved.Enabled = !Settings.Module.InTheMiddleOfConflictedMerge();
                Mergetool.Enabled = Settings.Module.InTheMiddleOfConflictedMerge();
                Skip.Enabled = true;
                Abort.Enabled = true;
            }
            else
            {
                Branches.Enabled = true;
                Ok.Enabled = true;
                AddFiles.Enabled = false;
                Resolved.Enabled = false;
                Mergetool.Enabled = false;
                Skip.Enabled = false;
                Abort.Enabled = false;
            }

            SolveMergeconflicts.Visible = Settings.Module.InTheMiddleOfConflictedMerge();

            Resolved.Text = _continueRebaseText.Text;
            Mergetool.Text = _solveConflictsText.Text;
            ContinuePanel.BackColor = Color.Transparent;
            MergeToolPanel.BackColor = Color.Transparent;

            if (Settings.Module.InTheMiddleOfConflictedMerge())
            {
                AcceptButton = Mergetool;
                Mergetool.Focus();
                Mergetool.Text = _solveConflictsText2.Text;
                MergeToolPanel.BackColor = Color.Black;
            }
            else if (Settings.Module.InTheMiddleOfRebase())
            {
                AcceptButton = Resolved;
                Resolved.Focus();
                Resolved.Text = _continueRebaseText2.Text;
                ContinuePanel.BackColor = Color.Black;
            }
        }

        private void MergetoolClick(object sender, EventArgs e)
        {
            GitUICommands.Instance.StartResolveConflictsDialog();
            EnableButtons();
        }

        private void InteractiveRebaseClick(object sender, EventArgs e)
        {
            chkAutosquash.Enabled = chkInteractive.Checked;
        }

        private void AddFilesClick(object sender, EventArgs e)
        {
            GitUICommands.Instance.StartAddFilesDialog();
        }

        private void ResolvedClick(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            new FormProcess(GitCommandHelpers.ContinueRebaseCmd()).ShowDialog(this);

            if (!Settings.Module.InTheMiddleOfRebase())
                Close();

            EnableButtons();
            patchGrid1.Initialize();
            Cursor.Current = Cursors.Default;
        }

        private void SkipClick(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            new FormProcess(GitCommandHelpers.SkipRebaseCmd()).ShowDialog(this);

            if (!Settings.Module.InTheMiddleOfRebase())
                Close();

            EnableButtons();
            patchGrid1.Initialize();
            Cursor.Current = Cursors.Default;
        }

        private void AbortClick(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            new FormProcess(GitCommandHelpers.AbortRebaseCmd()).ShowDialog(this);

            if (!Settings.Module.InTheMiddleOfRebase())
                Close();

            EnableButtons();
            patchGrid1.Initialize();
            Cursor.Current = Cursors.Default;
        }

        private void OkClick(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            if (string.IsNullOrEmpty(Branches.Text))
            {
                MessageBox.Show(this, _noBranchSelectedText.Text);
                return;
            }

            var form = new FormProcess(GitCommandHelpers.RebaseCmd(Branches.Text, chkInteractive.Checked, chkPreserveMerges.Checked, chkAutosquash.Checked));
            form.ShowDialog(this);
            if (form.OutputString.ToString().Trim() == "Current branch a is up to date.")
                MessageBox.Show(this, _branchUpToDateText.Text, _branchUpToDateCaption.Text);

            if (!Settings.Module.InTheMiddleOfConflictedMerge() &&
                !Settings.Module.InTheMiddleOfRebase() &&
                !Settings.Module.InTheMiddleOfPatch())
                Close();

            EnableButtons();
            patchGrid1.Initialize();
            Cursor.Current = Cursors.Default;
        }

        private void SolveMergeconflictsClick(object sender, EventArgs e)
        {
            MergetoolClick(sender, e);
        }
    }
}
