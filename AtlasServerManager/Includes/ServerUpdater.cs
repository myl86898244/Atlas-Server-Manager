﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace AtlasServerManager.Includes
{
    class ServerUpdater
    {
        public static bool Working = true;
        public static Process UpdateProcess = null;
        private static bool UpdateError = false, FirstLaunch = true;
        private static string DownloadSizeString, UpdateErrorText;
        private static ulong DownloadSizeBytes = 0;

        public static string GenerateUpdateMessage(AtlasServerManager AtlasMgr, int SleepTime, string time = "Minutes")
        {
            return AtlasMgr.ServerUpdateMessage.Text.Replace("{time}", SleepTime.ToString() + " " + time);
        }

        public static void CheckForUpdates(object Data)
        {
            AtlasServerManager AtlasMgr = (AtlasServerManager)Data;
            if (AtlasMgr.checkAutoServerUpdate.Checked || AtlasMgr.ForcedUpdate) AtlasMgr.Log("[Atlas] Checking for updates, can take up to 30 seconds...");
            string CurrentVersion = "";
            int UpdateVersion = 0, CurrentVer = 0;
            while (Working)
            {
                if (AtlasMgr.checkAutoServerUpdate.Checked || AtlasMgr.ForcedUpdate)
                {
                    UpdateVersion = GetAtlasServerBuildID(AtlasMgr);
                    if (UpdateVersion != 0)
                    {
                        if (File.Exists(AtlasMgr.SteamPath + "AtlasLatestVersion.txt")) using (StreamReader r = new StreamReader(AtlasMgr.SteamPath + "AtlasLatestVersion.txt")) CurrentVersion = r.ReadLine();
                        int.TryParse(CurrentVersion, out CurrentVer);
                        bool ServerStillOpen = false;
                        if (CurrentVer != UpdateVersion)
                        {
                            AtlasMgr.Updating = true;
                            AtlasMgr.Log("[Atlas] BuildID " + UpdateVersion + " Released!");
                            AtlasMgr.Invoke((System.Windows.Forms.MethodInvoker)delegate ()
                            {
                                foreach (ArkServerListViewItem ASLVI in AtlasMgr.ServerList.Items)
                                    if (ASLVI.GetServerData().IsRunning()) ServerStillOpen = true;
                            });
                            if (ServerStillOpen)
                            {
                                int SleepTime = (int)AtlasMgr.numServerWarning.Value / 2;
                                AtlasMgr.Log("[Atlas] Update Broadcasting " + (int)AtlasMgr.numServerWarning.Value + " Minutes");
                                SourceRconTools.SendCommandToAll("broadcast " + GenerateUpdateMessage(AtlasMgr, (int)AtlasMgr.numServerWarning.Value));

                                Thread.Sleep(SleepTime * 60000);

                                AtlasMgr.Log("[Atlas] Update Broadcasting " + SleepTime + " Minutes");
                                SourceRconTools.SendCommandToAll("broadcast " + GenerateUpdateMessage(AtlasMgr, SleepTime));
                                SleepTime = (int)AtlasMgr.numServerWarning.Value / 4;
                                Thread.Sleep(SleepTime * 60000);

                                AtlasMgr.Log("[Atlas] Update Broadcasting " + SleepTime + " Minutes");
                                SourceRconTools.SendCommandToAll("broadcast " + GenerateUpdateMessage(AtlasMgr, SleepTime));
                                SleepTime = (int)AtlasMgr.numServerWarning.Value / 4;
                                Thread.Sleep((SleepTime * 60000) - 35000);

                                AtlasMgr.Log("[Atlas] Update Broadcasting 30 Seconds");
                                SourceRconTools.SendCommandToAll("broadcast " + GenerateUpdateMessage(AtlasMgr, 30, "Seconds"));
                                Thread.Sleep(30000);
                                AtlasMgr.Log("[Atlas] Update Saving World");
                                SourceRconTools.SendCommandToAll("broadcast " + AtlasMgr.ServerUpdatingMessage.Text);
                                Thread.Sleep(5000);
                                if (!SourceRconTools.SaveWorld())
                                {
                                    AtlasMgr.Log("[Atlas] Failed Saving World, Not Updating!");
                                    continue;
                                }
                            }

                            while (ServerStillOpen)
                            {
                                ServerStillOpen = false;
                                AtlasMgr.Invoke((System.Windows.Forms.MethodInvoker)delegate ()
                                {
                                    foreach (ArkServerListViewItem ASLVI in AtlasMgr.ServerList.Items)
                                        if (ASLVI.GetServerData().IsRunning()) ServerStillOpen = true;
                                });
                                Thread.Sleep(3000);
                                if (!ServerStillOpen) break;
                            }
                            AtlasMgr.Log("[Atlas] Current BuildID: " + CurrentVersion + ", Updating To BuildID: " + UpdateVersion);
                            UpdateAtlas(AtlasMgr, UpdateVersion.ToString());
                            if (UpdateError)
                            {
                                AtlasMgr.Log("[Atlas] Update Error, Retrying...");
                                UpdateError = false;
                                if (UpdateErrorText.Contains("606") || UpdateErrorText.Contains("602"))
                                {
                                    AtlasServerManager.GetInstance().Log("[Update] Attempting to fix update error!");
                                    try
                                    {
                                        Directory.Delete(AtlasServerManager.GetInstance().SteamPath + @"steamapps\", true);
                                        Directory.Delete(AtlasServerManager.GetInstance().SteamPath + @"steamapps\", true);
                                    }
                                    catch
                                    { }
                                }
                                continue;
                            }
                            if (!Working)
                            {
                                AtlasMgr.Updating = false;
                                break;
                            }
                            AtlasMgr.Log("[Atlas] Updated, Launching Servers if offline!");
                            FirstLaunch = true;
                            AtlasMgr.ForcedUpdate = AtlasMgr.Updating = false;
                            StartServers(AtlasMgr);
                        }
                        else
                        {
                            AtlasMgr.Updating = false;
                        }
                    }
                }
                else AtlasMgr.Updating = false;
                if (!Working)
                {
                    AtlasMgr.Updating = false;
                    break;
                }
                if (FirstLaunch)
                {
                    AtlasMgr.Log("[Atlas] No updates found, Booting servers if offline!");
                    FirstLaunch = false;
                    StartServers(AtlasMgr);
                }
                Thread.Sleep((int)(AtlasMgr.numServerUpdate.Value * 60000));
            }
        }

        private static int GetAtlasServerBuildID(AtlasServerManager AtlasMgr)
        {
            try
            {
                if (!Directory.Exists(AtlasMgr.SteamPath)) Directory.CreateDirectory(AtlasMgr.SteamPath);
                if (!File.Exists(AtlasMgr.SteamPath + "steamcmd.exe")) File.WriteAllBytes(AtlasMgr.SteamPath + "steamcmd.exe", Properties.Resources.steamcmd);
                UpdateProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo(AtlasMgr.SteamPath + "steamcmd.exe", "+@NoPromptForPassword 1 +@sSteamCmdForcePlatformType windows +login anonymous +app_info_update 1 +app_info_print 1006030 +app_info_print 1006030 +app_info_print 1006030 +quit")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        WorkingDirectory = AtlasMgr.SteamPath
                    }
                };
                UpdateProcess.Start();
                string output = UpdateProcess.StandardOutput.ReadToEnd();
                UpdateProcess.WaitForExit();
                int Version = 0;
                int BuildIndex = output.IndexOf("\"buildid\"		\"");
                if(BuildIndex != -1)
                {
                    BuildIndex += 12;
                    int EndIndex = output.IndexOf('"', BuildIndex) - BuildIndex;
                    if (EndIndex != -1) int.TryParse(output.Substring(BuildIndex, EndIndex), out Version);
                }
                return Version;
            }
            catch { AtlasMgr.Log("[Update] Failed Checking For Updates..."); }     
            return 0;
        }

        private static List<string> UpdatePaths = new List<string>();
        private static void UpdateAtlas(AtlasServerManager AtlasMgr, string UpdateVersion)
        {
            AtlasMgr.FirstDl = false;
            AtlasMgr.Invoke((System.Windows.Forms.MethodInvoker)delegate ()
            {
                foreach (ArkServerListViewItem ASLVI in AtlasMgr.ServerList.Items)
                {
                    string UpdatePath = ASLVI.GetServerData().ServerPath;
                    if (UpdatePath.StartsWith("./") || UpdatePath.StartsWith(@".\")) UpdatePath = UpdatePath.Replace("./", System.AppDomain.CurrentDomain.BaseDirectory).Replace(@".\", System.AppDomain.CurrentDomain.BaseDirectory).Replace("//", "/").Replace(@"\\", @"\");
                    
                    if (!Directory.Exists(Path.GetDirectoryName(UpdatePath))) Directory.CreateDirectory(Path.GetDirectoryName(UpdatePath));

                    if (UpdatePath.Contains(@"ShooterGame\Binaries\Win64")) UpdatePath = Regex.Split(UpdatePath, "\\ShooterGame")[0];

                    if (!UpdatePaths.Contains(UpdatePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '\\'))) UpdatePaths.Add(UpdatePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '\\'));
                }
            });
            
            foreach (string UpdatePath in UpdatePaths)
            {
                AtlasMgr.Log("[Ark] Updating Path: " + UpdatePath);
                UpdateProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo(AtlasMgr.SteamPath + "steamcmd.exe", "+@NoPromptForPassword 1 +@sSteamCmdForcePlatformType windows +login anonymous +force_install_dir \"" + UpdatePath + "\" +app_update 1006030 validate +quit")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        WorkingDirectory = AtlasMgr.SteamPath
                    }
                };
                UpdateProcess.OutputDataReceived += (s, e) => UpdateData(e.Data);
                UpdateProcess.Start();
                UpdateProcess.BeginOutputReadLine();
                UpdateProcess.WaitForExit();
            }
            if (Working && !UpdateError) using (StreamWriter w = new StreamWriter(AtlasMgr.SteamPath + "AtlasLatestVersion.txt")) w.WriteLine(UpdateVersion);
        }

        private static void StartServers(AtlasServerManager AtlasMgr)
        {
            AtlasMgr.Invoke((System.Windows.Forms.MethodInvoker)delegate ()
            {
                foreach (ArkServerListViewItem ASLVI in AtlasMgr.ServerList.Items)
                    if(!ASLVI.GetServerData().IsRunning()) ASLVI.GetServerData().StartServer();
            });
        }

        private static void UpdateData(string input)
        {
            if (input != null && input.Length > 1)
            {
                if (input.Contains("Error! App "))
                {
                    UpdateErrorText = input;
                    UpdateError = true;
                }
                if (input.Contains("progress: "))
                {
                    string PercentText = Regex.Split(input, "progress: ")[1];
                    PercentText = PercentText.Substring(0, PercentText.IndexOf(' '));
                    string[] splts = input.Split('(');
                    if (splts.Length == 3 && splts[2].Contains("/"))
                    {
                        splts = Regex.Split(splts[2].Replace(")", ""), " / ");
                        if (splts.Length == 2)
                        {
                            if (splts[0] != "0" && splts[1] != "0")
                            {
                                DownloadSizeBytes = ulong.Parse(splts[1]);
                                DownloadSizeString = FormatBytes((long)DownloadSizeBytes);
                                input = input.Replace(splts[0], FormatBytes(long.Parse(splts[0]))).Replace(splts[1], DownloadSizeString);
                            }
                            else if (splts[1] != "0")
                            {
                                DownloadSizeBytes = ulong.Parse(splts[1]);
                                DownloadSizeString = FormatBytes((long)DownloadSizeBytes);
                                input = input.Replace(splts[1], DownloadSizeString);
                            }
                        }
                    }
                }
                AtlasServerManager.GetInstance().Log("[Update]" + input);
            }
        }

        private static string FormatBytes(System.Int64 bytes)
        {

            if (bytes >= 0x40000000) return string.Format("{0:F2} GB", (double)(bytes >> 20) / 1024);
            if (bytes >= 0x100000) return string.Format("{0:F2} MB", (double)(bytes >> 10) / 1024);
            if (bytes >= 0x400) return string.Format("{0:F2} KB", (double)bytes / 1024);
            return string.Format("{0:F2} Bytes", bytes);
        }
    }
}