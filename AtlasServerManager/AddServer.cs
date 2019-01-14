﻿using System;
using System.IO;
using System.Windows.Forms;
using AtlasServerManager.Includes;

namespace AtlasServerManager
{
    public partial class AddServer : Form
    {
        public AtlasServerData ServerData;
        public bool Create = true;
        string ServerPath = string.Empty;
        Process_Affinity PA = new Process_Affinity();

        public AddServer(string ServerPath)
        {
            InitializeComponent();
            Text = "Create Server";
            Create = true;
            CopyServerData();
            Registry.SaveRegServer(ServerData, 0, true);
            ServerData = Registry.LoadRegServer("LastSaved");
            if (ServerData.Loaded) UpdateComponents(ServerData);
            else if (ServerPath != string.Empty)
            {
                directoryPathTextBox.Text = ServerPath;
                this.ServerPath = ServerPath;
            }
        }

        public AddServer(AtlasServerData ServerData, string ServerPath)
        {
            InitializeComponent();
            Create = false;
            UpdateComponents(ServerData);
        }

        private void UpdateComponents(AtlasServerData ServerData)
        {
            this.ServerData = ServerData;
            directoryPathTextBox.Text = ServerData.ServerPath;
            ServerPath = ServerData.ServerPath;
            AltSaveDirectoryTextBox.Text = ServerData.AltSaveDirectory;
            MaxPlayersNumericUpDown.Value = ServerData.MaxPlayers;
            QueryPortNumericUpDown.Value = ServerData.QueryPort;
            ServerPortNumericUpDown.Value = ServerData.ServerPort;
            AdminPassMaskedTextBox.Text = ServerData.Pass;
            checkBox1.Checked = ServerData.WildWipe;
            checkBox10.Checked = ServerData.PVP;
            checkBox11.Checked = ServerData.MapB;
            checkBox12.Checked = ServerData.Gamma;
            checkBox13.Checked = ServerData.Third;
            checkBox14.Checked = ServerData.Crosshair;
            checkBox15.Checked = ServerData.HitMarker;
            checkBox16.Checked = ServerData.Imprint;
            checkBox17.Checked = ServerData.FTD;
            BattleEyeCheck.Checked = ServerData.BattleEye;
            AdditionalArgsTextBox.Text = ServerData.CustomArgs;
            AdditionalAfterArgsTextBox.Text = ServerData.CustomAfterArgs;
            ServerXNumericUpDown.Value = ServerData.ServerX;
            ServerYNumericUpDown.Value = ServerData.ServerY;
            ReservedPlayersNumericUpDown.Value = ServerData.ReservedPlayers;
            ServerIPTextBox.Text = ServerData.ServerIp;
            ProcessPriotityCombo.SelectedIndex = ServerData.ProcessPriority;
            UPNPCheck.Checked = ServerData.Upnp;

            /*Process Affinity*/
            if (ServerData.ProcessAffinity == null || ServerData.ProcessAffinity.Length == 0)
                ServerData.ProcessAffinity = new bool[Environment.ProcessorCount];
            PA.UpdateCheckBoxs(ServerData.ProcessAffinity);

            /*Rcon*/            
            checkBox3.Checked = ServerData.Rcon;
            RconIP.Text = ServerData.RCONIP;
            RconNumericUpDown.Value = ServerData.RconPort;
            if (!Create)
            {
                Text = "Edit: ServerX: " + ServerData.ServerX + ", ServerY: " + ServerData.ServerX + ", Port: " + ServerData.ServerPort;
                AddServerButton.Text = "Save Settings";
            }
            //Translate.TranslateComponents(Controls, "ru");
        }

        private void CopyServerData()
        {
            if(directoryPathTextBox.Text == string.Empty)
            {
                directoryPathTextBox.Text = @".\AtlasServerData";
            }
            System.Diagnostics.Process OldProcess = ServerData?.ServerProcess;
            ServerData = new AtlasServerData()
            {
                ServerPath = directoryPathTextBox.Text,
                AltSaveDirectory = AltSaveDirectoryTextBox.Text,
                MaxPlayers = (int)MaxPlayersNumericUpDown.Value,
                QueryPort = (int)QueryPortNumericUpDown.Value,
                ServerPort = (int)ServerPortNumericUpDown.Value,
                Pass = AdminPassMaskedTextBox.Text,
                WildWipe = checkBox1.Checked,
                PVP = checkBox10.Checked,
                MapB = checkBox11.Checked,
                Gamma = checkBox12.Checked,
                Third = checkBox13.Checked,
                Crosshair = checkBox14.Checked,
                HitMarker = checkBox15.Checked,
                Imprint = checkBox16.Checked,
                FTD = checkBox17.Checked,
                BattleEye = BattleEyeCheck.Checked,
                CustomArgs = AdditionalArgsTextBox.Text,
                CustomAfterArgs = AdditionalAfterArgsTextBox.Text,
                /*Process Affinity*/
                ProcessAffinity = PA.ProcessAffinity,
                /*Rcon*/
                Rcon = checkBox3.Checked,
                RCONIP = RconIP.Text,
                RconPort = (int)RconNumericUpDown.Value,
                AutoStart = true,
                ServerX = (int)ServerXNumericUpDown.Value,
                ServerY = (int)ServerYNumericUpDown.Value,
                ReservedPlayers = (int)ReservedPlayersNumericUpDown.Value,
                ServerIp = ServerIPTextBox.Text,
                ProcessPriority = ProcessPriotityCombo.SelectedIndex,

                Upnp = UPNPCheck.Checked,
                ServerProcess = OldProcess,
                PID = OldProcess != null ? OldProcess.Id : 0
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string Error = string.Empty;
            if (Create)
            {
                foreach (AtlasServerData Asd in AtlasServerManager.GetInstance().ServerList.GetServerList())
                {
                    if (Asd.ServerPort == (int)ServerPortNumericUpDown.Value || Asd.ServerPort == (int)QueryPortNumericUpDown.Value || Asd.ServerPort == (int)RconNumericUpDown.Value)
                    {
                        Error = "Server Port " + Asd.ServerPort + " is already in use by " + Asd.AltSaveDirectory;
                        break;
                    }
                    if (Asd.QueryPort == (int)QueryPortNumericUpDown.Value || Asd.QueryPort == (int)ServerPortNumericUpDown.Value || Asd.QueryPort == (int)RconNumericUpDown.Value)
                    {
                        Error = "Query Port " + Asd.QueryPort + " is already in use by " + Asd.AltSaveDirectory;
                        break;
                    }
                    if (Asd.RconPort == (int)RconNumericUpDown.Value || Asd.RconPort == (int)QueryPortNumericUpDown.Value || Asd.RconPort == (int)ServerPortNumericUpDown.Value)
                    {
                        Error = "Rcon Port " + Asd.RconPort + " is already in use by " + Asd.AltSaveDirectory;
                        break;
                    }
                }
            }

            if (AltSaveDirectoryTextBox.Text == string.Empty)
            {
                Error = "Please set a Alt Save Directory!";
            }

            if (Error != string.Empty) MessageBox.Show(Error);
            else
            {
                CopyServerData();
                Registry.SaveRegServer(ServerData, 0, true, true);
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox3.Checked && MessageBox.Show("Disabling this will disable auto updates, Are you sure you want to disable this?", "Warning !!!", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No) checkBox3.Checked = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ServerData = Registry.LoadRegServer("Default");
            UpdateComponents(ServerData);
            Registry.SaveRegServer(ServerData, 0, true, true);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (ServerPath != string.Empty) ServerPathBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            if (ServerPathBrowserDialog.ShowDialog() == DialogResult.OK && Directory.Exists(ServerPathBrowserDialog.SelectedPath)) directoryPathTextBox.Text = ServerPathBrowserDialog.SelectedPath;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if(PA.ShowDialog() == DialogResult.OK)
            {
                ServerData.ProcessAffinity = PA.ProcessAffinity;
            }
        }
    }
}