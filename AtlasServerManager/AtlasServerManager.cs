﻿using System;
using System.Windows.Forms;
using AtlasServerManager.Includes;
using System.IO;
using System.Diagnostics;

namespace AtlasServerManager
{
    public partial class AtlasServerManager : Form
    {
        public string SteamPath, ArkManagerPath, ServerPath = string.Empty, ASMTitle;
        public static AtlasServerManager GetInstance() { return instance; }
        public bool Updating = true, FirstDl = false, ForcedUpdate = false;
        private static AtlasServerManager instance;
        private delegate void RichTextBoxUpdateEventHandler(string txt);
        private InputDialog inputDialog;

        public AtlasServerManager()
        {
            InitializeComponent();
            instance = this;
            // 
            // ServerList
            // 
            ServerList = new ArkListView
            {
                AllowColumnReorder = true, BackColor = System.Drawing.SystemColors.Window,
                CheckBoxes = true, ContextMenuStrip = contextMenuStrip1, Dock = DockStyle.Fill,
                FullRowSelect = true, GridLines = true, Location = new System.Drawing.Point(4, 4),
                Margin = new Padding(4), MultiSelect = false, Name = "ServerList", RightToLeft = System.Windows.Forms.RightToLeft.No,
                Size = new System.Drawing.Size(668, 256), TabIndex = 0, UseCompatibleStateImageBehavior = false,
                View = View.Details
            };
            ServerList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            columnHeader3, columnHeader1, columnHeader6, columnHeader7, columnHeader4, columnHeader5});
            tabPage1.Controls.Add(ServerList);
            inputDialog = new InputDialog();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Translate.TranslateMenu(menuStrip1.Items, "fr");
            //Translate.TranslateComponents(Controls, "fr");
            //Translate.TranslateListView(ServerList.Columns, "fr");
            //Translate.FirstTranslate = true;
            ASMTitle = Text;
            ArkManagerPath = Path.GetDirectoryName(Application.ExecutablePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("/", @"\") + Path.DirectorySeparatorChar;
            SteamPath = Path.Combine(ArkManagerPath, @"Steam\");
            Registry.LoadRegConfig(this);
            Worker.Init(this, ServerList.Items.Count > 0);
            if (File.Exists(ArkManagerPath + "ShooterGameServer.exe")) ServerPath = ArkManagerPath + "ShooterGameServer.exe";
            else
            {
                string[] Files = Directory.GetFiles(ArkManagerPath, "*.exe", SearchOption.AllDirectories);
                foreach (string file in Files)
                {
                    if (!file.Contains("steamapps") && Path.GetFileNameWithoutExtension(file) == "ShooterGameServer")
                    {
                        ServerPath = file;
                        break;
                    }
                }
            }
            if (!checkAutoServerUpdate.Checked) Updating = false;
            SetupCallbacks();
        }

        private void AddServer()
        {
            AddServer AddSrv = new AddServer(ServerPath);
            if (AddSrv.ShowDialog() == DialogResult.OK)
            {
                ServerList.Items.Add(new ArkServerListViewItem(AddSrv.ServerData));
                Registry.SaveRegConfig(this);
                Log(AddSrv.ServerData.AltSaveDirectory + " Added!");
                Worker.ForceUpdaterRestart(this);
            }
            AddSrv.Dispose();
        }

        private void RemoveServer()
        {
            if (ServerList.FocusedItem != null && MessageBox.Show("Are you sure you want to delete ServerX:" + ((ArkServerListViewItem)ServerList.FocusedItem).GetServerData().ServerX + ", ServerY: " + ((ArkServerListViewItem)ServerList.FocusedItem).GetServerData().ServerY + ", Port: " + ((ArkServerListViewItem)ServerList.FocusedItem).GetServerData().ServerPort + "?\n Press 'Yes' To Delete!", "Delete ServerX:" + ((ArkServerListViewItem)ServerList.FocusedItem).GetServerData().ServerX + ", ServerY: " + ((ArkServerListViewItem)ServerList.FocusedItem).GetServerData().ServerY + ", Port: " + ((ArkServerListViewItem)ServerList.FocusedItem).GetServerData().ServerPort + "?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ArkServerListViewItem ASLVI = ((ArkServerListViewItem)ServerList.FocusedItem);
                Log(ASLVI.GetServerData().AltSaveDirectory + " Removed!");
                Registry.DeleteServer(ServerList.FocusedItem.Index);
                ServerList.Items.RemoveAt(ServerList.FocusedItem.Index);
                if (ServerList.Items.Count == 0) Worker.StopUpdating();
            }
        }

        private void EditServer()
        {
            if (ServerList.FocusedItem != null)
            {
                ArkServerListViewItem ASLVI = ((ArkServerListViewItem)ServerList.FocusedItem);
                AddServer AddSrv = new AddServer(ASLVI.GetServerData(), ServerPath);
                if (AddSrv.ShowDialog() == DialogResult.OK) ASLVI.SetServerData(AddSrv.ServerData);
                Log(ASLVI.GetServerData().AltSaveDirectory + " Edited!");
                AddSrv.Dispose();
            }
        }

        private void StartServer()
        {
            if (ServerList.FocusedItem != null)
            {
                ArkServerListViewItem ASLVI = (ArkServerListViewItem)ServerList.FocusedItem;
                ASLVI.GetServerData().StartServer();
                ASLVI.UpdateStatus();
                Registry.SaveRegConfig(this);
                Log(ASLVI.GetServerData().AltSaveDirectory + " Started!");
            }
        }

        private void StopServer()
        {
            if (ServerList.FocusedItem != null)
            {
                ArkServerListViewItem ASLVI = (ArkServerListViewItem)ServerList.FocusedItem;
                ASLVI.GetServerData().StopServer();
                ASLVI.UpdateStatus();
                Registry.SaveRegConfig(this);
                Log(ASLVI.GetServerData().AltSaveDirectory + " Stopped!");
            }
        }

        private void RconBroadcast(bool AllServers)
        {
            if (AllServers || ServerList.FocusedItem != null)
            {
                ArkServerListViewItem ASLVI = AllServers ? null : (ArkServerListViewItem)ServerList.FocusedItem;
                inputDialog.Text = "Broadcast to " + (AllServers ? "All" : ASLVI.GetServerData().AltSaveDirectory);
                inputDialog.SendButton.Text = "Broadcast";
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    if (AllServers) SourceRconTools.SendCommandToAll("broadcast " + inputDialog.InputText.Text);
                    else SourceRconTools.SendCommand("broadcast " + inputDialog.InputText.Text, ASLVI);
                    Log("Broadcasted!");
                }
            }
        }

        private void RconSaveWorld(bool AllServers)
        {
            if (AllServers || ServerList.FocusedItem != null)
            {
                if (AllServers) SourceRconTools.SendCommandToAll("saveworld");
                else
                {
                    ArkServerListViewItem ASLVI = (ArkServerListViewItem)ServerList.FocusedItem;
                    SourceRconTools.SendCommand("saveworld", ASLVI);
                }
                Log("Saved World!");
            }
        }

        private void RconCloseSaveWorld(bool AllServers)
        {
            if (AllServers || ServerList.FocusedItem != null)
            {
                if (AllServers) SourceRconTools.SendCommandToAll("DoExit");
                else
                {
                    ArkServerListViewItem ASLVI = (ArkServerListViewItem)ServerList.FocusedItem;
                    SourceRconTools.SendCommand("DoExit", ASLVI);
                }
                Log("Closed Saved World!");
            }
        }

        private void RconCustomCommand(bool AllServers)
        {
            if (AllServers || ServerList.FocusedItem != null)
            {
                ArkServerListViewItem ASLVI = AllServers ? null : (ArkServerListViewItem)ServerList.FocusedItem;
                inputDialog.Text = "Send Custom Command to " + (AllServers ? "All" : ASLVI.GetServerData().AltSaveDirectory);
                inputDialog.SendButton.Text = "Send Command";
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    if (AllServers) SourceRconTools.SendCommandToAll(inputDialog.InputText.Text);
                    else SourceRconTools.SendCommand(inputDialog.InputText.Text, ASLVI);
                    Log("Custom Command Executed: " + inputDialog.InputText.Text);
                }
            }
        }

        private void RconPlugin(bool AllServers, bool Load)
        {
            if (AllServers || ServerList.FocusedItem != null)
            {
                ArkServerListViewItem ASLVI = AllServers ? null : (ArkServerListViewItem)ServerList.FocusedItem;
                inputDialog.Text = (Load ? "Load" : "Unload") + " Plugin to " + (AllServers ? "All" : ASLVI.GetServerData().AltSaveDirectory);
                inputDialog.SendButton.Text = (Load ? "Load" : "Unload") + " Plugin";
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    if (AllServers) SourceRconTools.SendCommandToAll("plugins." + (Load ? "load " : "unload ") + inputDialog.InputText.Text);
                    else SourceRconTools.SendCommand("plugins." + (Load ? "load " : "unload ") + inputDialog.InputText.Text, ASLVI);
                    Log("Plugin " + (Load ? "Loaded" : "Unloaded") + ": " + inputDialog.InputText.Text);
                }
            }
        }

        private void SetupCallbacks()
        {
            FormClosing += (e, a) =>
            {
                Worker.DestroyAll();
                Registry.SaveRegConfig(this);
            };

            checkAutoServerUpdate.CheckedChanged += (e, a) =>
            {
                if (!checkAutoServerUpdate.Checked)
                    Updating = false;
            };

            ServerList.MouseDoubleClick += (e, a) => EditServer();
            ServerList.SelectedIndexChanged += (e, a) =>
            {
                if (ServerList.FocusedItem != null)
                {
                    ArkServerListViewItem ASLVI = (ArkServerListViewItem)ServerList.FocusedItem;
                    StartButton.Enabled = true;
                    if (ASLVI.GetServerData().IsRunning())
                    {
                        StartButton.BackColor = System.Drawing.Color.Red;
                        StartButton.ForeColor = System.Drawing.Color.White;
                        StartButton.Text = "Stop";
                    }
                    else
                    {
                        StartButton.BackColor = System.Drawing.Color.Green;
                        StartButton.ForeColor = System.Drawing.Color.White;
                        StartButton.Text = "Start";
                    }
                }
                else
                {
                    StartButton.BackColor = System.Drawing.Color.DarkGray;
                    StartButton.ForeColor = System.Drawing.Color.DimGray;
                    StartButton.Enabled = false;
                }
            };

            StartButton.Click += (e, a) =>
            {
                if (ServerList.FocusedItem != null)
                {
                    ArkServerListViewItem ASLVI = (ArkServerListViewItem)ServerList.FocusedItem;
                    if (StartButton.Text == "Start") ASLVI.GetServerData().StartServer();
                    else ASLVI.GetServerData().StopServer();
                    ASLVI.UpdateStatus();
                }
            };

            addToolStripMenuItem.Click += (e, a) => AddServer();
            removeToolStripMenuItem.Click += (e, a) => RemoveServer();
            editSettingsToolStripMenuItem.Click += (e, a) => EditServer();

            addToolStripMenuItem1.Click += (e, a) => AddServer();
            removeToolStripMenuItem1.Click += (e, a) => RemoveServer();
            editSettingsToolStripMenuItem1.Click += (e, a) => EditServer();

            startToolStripMenuItem.Click += (e, a) => StartServer();
            stopToolStripMenuItem.Click += (e, a) => StopServer();

            startToolStripMenuItem1.Click += (e, a) => StartServer();
            stopToolStripMenuItem1.Click += (e, a) => StopServer();

            button1.Click += (e, a) =>
            {
                if (Updating)
                {
                    MessageBox.Show("Already Updating", "Update in progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (File.Exists(SteamPath + "AtlasLatestVersion.txt")) File.Delete(SteamPath + "AtlasLatestVersion.txt");
                Log("[Atlas] Forcing Update");
                ForcedUpdate = Updating = true;
                Worker.ForceUpdaterRestart(this);
            };

            ClearConfigButton.Click += (e, a) =>
            {
                if(MessageBox.Show("Are you sure you want to erase all your configurations?", "Configuration Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Registry.ClearAll();
                    ServerList.Clear();
                }
            };

            broadcastToolStripMenuItem.Click += (e, a) => RconBroadcast(false);
            saveWorldToolStripMenuItem.Click += (e, a) => RconSaveWorld(false);
            closeSaveWorldToolStripMenuItem.Click += (e, a) => RconCloseSaveWorld(false);
            customCommandToolStripMenuItem.Click += (e, a) => RconCustomCommand(false);
            loadPluginToolStripMenuItem.Click += (e, a) => RconPlugin(false, true);
            unloadPluginToolStripMenuItem.Click += (e, a) => RconPlugin(false, false);

            broadcastToolStripMenuItem1.Click += (e, a) => RconBroadcast(true);
            saveWorldToolStripMenuItem1.Click += (e, a) => RconSaveWorld(true);
            closeSaveWorldToolStripMenuItem1.Click += (e, a) => RconCloseSaveWorld(true);
            customCommandToolStripMenuItem1.Click += (e, a) => RconCustomCommand(true);
            loadPluginToolStripMenuItem1.Click += (e, a) => RconPlugin(true, true);
            unloadPluginToolStripMenuItem1.Click += (e, a) => RconPlugin(true, false);


            broadcastToolStripMenuItem2.Click += (e, a) => RconBroadcast(false);
            saveWorldToolStripMenuItem2.Click += (e, a) => RconSaveWorld(false);
            closeSaveWorldToolStripMenuItem2.Click += (e, a) => RconCloseSaveWorld(false);
            customCommandToolStripMenuItem2.Click += (e, a) => RconCustomCommand(false);
            loadPluginToolStripMenuItem2.Click += (e, a) => RconPlugin(false, true);
            unloadPluginToolStripMenuItem2.Click += (e, a) => RconPlugin(false, false);

            broadcastToolStripMenuItem3.Click += (e, a) => RconBroadcast(true);
            saveWorldToolStripMenuItem3.Click += (e, a) => RconSaveWorld(true);
            closeSaveWorldToolStripMenuItem3.Click += (e, a) => RconCloseSaveWorld(true);
            customCommandToolStripMenuItem3.Click += (e, a) => RconCustomCommand(true);
            loadPluginToolStripMenuItem3.Click += (e, a) => RconPlugin(true, true);
            unloadPluginToolStripMenuItem3.Click += (e, a) => RconPlugin(true, false);
        }

        bool bFirst = true;
        public void Log(string txt)
        {
            if (richTextBox1.InvokeRequired)
            {
                if (richTextBox1 == null) return;
                richTextBox1.Invoke(new RichTextBoxUpdateEventHandler(Log), new object[] { txt });
            }
            else
            {
                if (txt == null || txt == string.Empty || txt.Length < 8) return;
                if (txt.Contains("downloading") || txt.Contains("validat") || txt.Contains("committing") || txt.Contains("preallocating"))
                {
                    if(!FirstDl)
                    {
                        FirstDl = true;
                        richTextBox1.AppendText(string.Format("\n[{0}] {1}", DateTime.Now.ToString("hh:mm"), txt));
                        richTextBox1.ScrollToCaret();
                    }
                    else
                    {
                        string[] lines = richTextBox1.Lines;
                        lines[lines.Length - 1] = string.Format("[{0}] {1}", DateTime.Now.ToString("hh:mm"), txt);
                        richTextBox1.Lines = lines;
                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    }
                }
                else
                {
                    richTextBox1.AppendText(string.Format("{0}[{1}] {2}", (bFirst ? "" : "\n"), DateTime.Now.ToString("hh:mm"), txt));
                    richTextBox1.ScrollToCaret();
                    bFirst = false;
                }
            }
        }
    }
}