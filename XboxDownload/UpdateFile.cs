using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XboxDownload
{
    class UpdateFile
    {
        private const string updateUrl = "https://github.com/skydevil88/XboxDownload/releases/download/v1/";
        public const string getXboxUrl = "http://13.215.187.105:5000";
        private const string exeFile = "XboxDownload.exe";
        public const string pdfFile = "ProductManual.pdf";
        public const string dataFile = "XboxGame.dat";
        private static readonly string[,] proxys = {
            { "proxy", "https://ghproxy.com/" },
            { "proxy", "https://gh.api.99988866.xyz/" },
            { "proxy", "https://github.91chi.fun/" },
            { "mirror", "https://cdn.githubjs.cf/" },
            { "mirror", "https://hub.fastgit.xyz/" },
            { "direct", "" }
        };

        public static void Start(bool autoupdate, Form1 parentForm)
        {
            Properties.Settings.Default.NextUpdate = DateTime.Now.AddDays(7).Ticks;
            Properties.Settings.Default.Save();

            //清理历史遗留文件
            if (Directory.Exists(Application.StartupPath + "\\Store"))
            {
                Directory.Delete(Application.StartupPath + "\\Store", true);
            }
            string[] files = new string[]
            {
                "Hosts",
                "Domain",
                "使用说明.docx",
                "IP.assets1.xboxlive.com.txt",
                "IP.origin-a.akamaihd.net.txt",
                "IP列表(assets1.xboxlive.cn).txt",
                "IP.uplaypc-s-ubisoft.cdn.ubi.com.txt"
            };
            foreach (string file in files)
            {
                if (File.Exists(Application.StartupPath + "\\" + file))
                {
                    File.Delete(Application.StartupPath + "\\" + file);
                }
            }

            string md5 = string.Empty;
            Task[] tasks = new Task[proxys.GetLongLength(0)];
            for (int i = 0; i <= tasks.Length - 1; i++)
            {
                string updateUrl;
                switch (proxys[i, 0])
                {
                    case "proxy":
                        updateUrl = proxys[i, 1] + UpdateFile.updateUrl;
                        break;
                    case "mirror":
                        updateUrl = proxys[i, 1] + Regex.Replace(UpdateFile.updateUrl, @"https?://[^/]+/", "");
                        break;
                    default:
                        updateUrl = UpdateFile.updateUrl;
                        break;
                }
                tasks[i] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(updateUrl + UpdateFile.exeFile + ".md5", "GET", null, null, true, false, true, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(md5) && Regex.IsMatch(socketPackage.Html, @"^[A-Z0-9]{32}$"))
                    {
                        md5 = socketPackage.Html;
                        Update(autoupdate, md5, updateUrl, parentForm);
                    }
                });
            }
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            if (string.IsNullOrEmpty(md5) && !autoupdate)
            {
                parentForm.Invoke(new Action(() =>
                {
                    MessageBox.Show("检查更新出错，请稍候再试。", Form1.appName + " - 软件更新", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    parentForm.tsmUpdate.Enabled = true;
                }));
            }
        }

        private static void Update(bool autoupdate, string md5, string updateUrl, Form1 parentForm)
        {
            if (!string.Equals(md5, GetPathHash(Application.ExecutablePath, "md5")))
            {
                bool isUpdate = false;
                parentForm.Invoke(new Action(() =>
                {
                    isUpdate = MessageBox.Show("已检测到新版本，是否立即更新？", Form1.appName + " - 软件更新", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes;
                    if (!isUpdate) parentForm.tsmUpdate.Enabled = true;
                }));
                if (!isUpdate) return;

                string filename = Path.GetFileName(Application.ExecutablePath);
                Task[] tasks = new Task[3];
                tasks[0] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(updateUrl + UpdateFile.exeFile, "GET", null, null, true, false, false, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(socketPackage.Err) && socketPackage.Buffer.Length > 0 && socketPackage.Headers.Contains(" 200 OK"))
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(Application.StartupPath + "\\" + filename + ".update", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                            {
                                fs.Write(socketPackage.Buffer, 0, socketPackage.Buffer.Length);
                                fs.Flush();
                                fs.Close();
                            }
                        }
                        catch { }
                    }
                });
                tasks[1] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(updateUrl + UpdateFile.pdfFile, "GET", null, null, true, false, false, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(socketPackage.Err) && socketPackage.Buffer.Length > 0 && socketPackage.Headers.Contains(" 200 OK"))
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(Application.StartupPath + "\\" + UpdateFile.pdfFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                            {
                                fs.Write(socketPackage.Buffer, 0, socketPackage.Buffer.Length);
                                fs.Flush();
                                fs.Close();
                            }
                        }
                        catch { }
                    }
                });
                tasks[2] = new Task(() =>
                {
                    UpdateXboxGameData(updateUrl);
                });
                Array.ForEach(tasks, x => x.Start());
                Task.WaitAll(tasks);

                FileInfo fi = new FileInfo(Application.StartupPath + "\\" + filename + ".update");
                if (fi.Exists)
                {
                    if (string.Equals(md5, GetPathHash(fi.FullName, "md5")))
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            if (Form1.bServiceFlag) parentForm.ButStart_Click(null, null);
                            parentForm.notifyIcon1.Visible = false;
                        }));
                        using (FileStream fs = File.Create(Application.StartupPath + "\\" + filename + ".md5"))
                        {
                            Byte[] bytes = new UTF8Encoding(true).GetBytes(md5);
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Flush();
                            fs.Close();
                        }
                        if (File.Exists(Application.StartupPath + "\\.update.cmd"))
                            File.Delete(Application.StartupPath + "\\.update.cmd");
                        using (FileStream fs = File.Create(Application.StartupPath + "\\" + ".update.cmd"))
                        {
                            Byte[] byteArray = new UTF8Encoding(true).GetBytes("cd /d \"" + Application.StartupPath + "\"\r\nchoice /t 3 /d y /n >nul\r\nmove \"" + filename + ".update\" \"" + filename + "\"\r\n\"" + filename + "\"\r\ndel /a/f/q .update.cmd");
                            fs.Write(byteArray, 0, byteArray.Length);
                            fs.Flush();
                            fs.Close();
                        }
                        File.SetAttributes(Application.StartupPath + "\\" + ".update.cmd", FileAttributes.Hidden);
                        using (Process p = new Process())
                        {
                            p.StartInfo.FileName = "cmd.exe";
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.Arguments = "/c \"" + Application.StartupPath + "\\.update.cmd\"";
                            p.Start();
                        }
                        Process.GetCurrentProcess().Kill();
                    }
                    else
                    {
                        fi.Delete();
                    }
                }
                parentForm.Invoke(new Action(() =>
                {
                    MessageBox.Show("下载文件出错，请稍候再试。", Form1.appName + " - 软件更新", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    parentForm.tsmUpdate.Enabled = true;
                }));
            }
            else
            {
                parentForm.Invoke(new Action(() =>
                {
                    if (!autoupdate) MessageBox.Show("软件已经是最新版本。", Form1.appName + " - 软件更新", MessageBoxButtons.OK, MessageBoxIcon.None);
                    parentForm.tsmUpdate.Enabled = true;
                }));
            }
        }

        public static void Download(string filename)
        {
            string md5 = string.Empty;
            Task[] tasks = new Task[proxys.GetLongLength(0)];
            for (int i = 0; i <= tasks.Length - 1; i++)
            {
                string updateUrl;
                switch (proxys[i, 0])
                {
                    case "proxy":
                        updateUrl = proxys[i, 1] + UpdateFile.updateUrl;
                        break;
                    case "fastgit":
                        updateUrl = proxys[i, 1] + Regex.Replace(UpdateFile.updateUrl, @"https?://[^/]+/", "");
                        break;
                    default:
                        updateUrl = UpdateFile.updateUrl;
                        break;
                }
                tasks[i] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(updateUrl + UpdateFile.exeFile + ".md5", "GET", null, null, true, false, true, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(md5) && Regex.IsMatch(socketPackage.Html, @"^[A-Z0-9]{32}$"))
                    {
                        md5 = socketPackage.Html;
                        Download(filename, updateUrl + filename);
                    }
                });
            }
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
        }

        private static void Download(string filename, string url)
        {
            SocketPackage socketPackage = ClassWeb.HttpRequest(url, "GET", null, null, true, false, false, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
            if (string.IsNullOrEmpty(socketPackage.Err) && socketPackage.Buffer.Length > 0 && socketPackage.Headers.Contains(" 200 OK"))
            {
                try
                {
                    using (FileStream fs = new FileStream(Application.StartupPath + "\\" + filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        fs.Write(socketPackage.Buffer, 0, socketPackage.Buffer.Length);
                        fs.Flush();
                        fs.Close();
                    }
                }
                catch { }
            }
        }

        public static void UpdateXboxGameData(string updateUrl)
        {
            SocketPackage socketPackage = ClassWeb.HttpRequest(updateUrl + UpdateFile.dataFile, "GET", null, null, true, false, false, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
            ConcurrentDictionary<String, XboxGameDownload.Products> dicXboxGame = null;
            if (string.IsNullOrEmpty(socketPackage.Err) && socketPackage.Buffer.Length > 0 && socketPackage.Headers.Contains(" 200 OK"))
            {
                try
                {
                    using (MemoryStream stream = new MemoryStream(socketPackage.Buffer))
                    {
                        BinaryFormatter bFormat = new BinaryFormatter();
                        dicXboxGame = (ConcurrentDictionary<String, XboxGameDownload.Products>)bFormat.Deserialize(stream);
                        stream.Close();
                    }
                }
                catch { }
            }
            if (dicXboxGame != null)
            {
                foreach (var item in dicXboxGame)
                {
                    if (XboxGameDownload.dicXboxGame.TryGetValue(item.Key, out XboxGameDownload.Products XboxGame))
                    {
                        if (item.Value.Version >= XboxGame.Version)
                        {
                            XboxGame.Version = item.Value.Version;
                            XboxGame.FileSize = item.Value.FileSize;
                            XboxGame.Url = item.Value.Url.Replace(".xboxlive.cn", ".xboxlive.com");
                        }
                    }
                    else
                    {
                        XboxGameDownload.dicXboxGame.TryAdd(item.Key, new XboxGameDownload.Products
                        {
                            Version = item.Value.Version,
                            FileSize = item.Value.FileSize,
                            Url = item.Value.Url.Replace(".xboxlive.cn", ".xboxlive.com")
                        });
                    }
                }
                try
                {
                    using (FileStream stream = new FileStream(Application.StartupPath + "\\" + UpdateFile.dataFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        BinaryFormatter bFormat = new BinaryFormatter();
                        bFormat.Serialize(stream, XboxGameDownload.dicXboxGame);
                        stream.Close();
                    }
                }
                catch { }
            }
        }

        public static string GetPathHash(string path, string encrypt = "md5")
        {
            string hash = string.Empty;
            switch (encrypt)
            {
                case "md5":
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(path))
                        {
                            hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                        }
                    }
                    break;
                case "sha256":
                    using (var sha256 = SHA256Managed.Create())
                    {
                        using (var stream = File.OpenRead(path))
                        {
                            hash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
                        }
                    }
                    break;
            }
            return hash;
        }
    }
}