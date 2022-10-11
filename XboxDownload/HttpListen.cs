using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace XboxDownload
{
    class HttpListen
    {
        private readonly Form1 parentForm;
        private readonly ConcurrentDictionary<String, String> dicAppLocalUploadFile = new ConcurrentDictionary<String, String>();
        Socket socket = null;

        public HttpListen(Form1 parentForm)
        {
            this.parentForm = parentForm;
        }

        public void Listen()
        {
            int port = 80;
            IPEndPoint ipe = new IPEndPoint(Properties.Settings.Default.ListenIP == 0 ? IPAddress.Parse(Properties.Settings.Default.LocalIP) : IPAddress.Any, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(ipe);
                socket.Listen(100);
            }
            catch (SocketException ex)
            {
                parentForm.Invoke(new Action(() =>
                {
                    parentForm.pictureBox1.Image = Properties.Resources.Xbox3;
                    MessageBox.Show(String.Format("启用HTTP服务失败!\n错误信息: {0}\n\n解决方法：1、停用占用 {1} 端口的服务。2、监听IP选择(Any)", ex.Message, port), "启用HTTP服务失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                return;
            }
            while (Form1.bServiceFlag)
            {
                try
                {
                    Socket mySocket = socket.Accept();
                    ThreadPool.QueueUserWorkItem(TcpThread, mySocket);
                }
                catch { }
            }
        }

        private void TcpThread(object obj)
        {
            Socket mySocket = (Socket)obj;
            if (mySocket.Connected)
            {
                mySocket.SendTimeout = 30000;
                mySocket.ReceiveTimeout = 30000;
                while (Form1.bServiceFlag && mySocket.Connected && mySocket.Poll(3000000, SelectMode.SelectRead))
                {
                    Byte[] _receive = new Byte[4096];
                    int _num = mySocket.Receive(_receive, 0, _receive.Length, SocketFlags.None, out _);
                    string _buffer = Encoding.ASCII.GetString(_receive, 0, _num);
                    Match result = Regex.Match(_buffer, @"(?<method>GET|OPTIONS|HEAD) (?<path>[^\s]+)");
                    if (!result.Success)
                    {
                        mySocket.Close();
                        continue;
                    }
                    string _method = result.Groups["method"].Value;
                    string _filePath = Regex.Replace(result.Groups["path"].Value.Trim(), @"^https?://[^/]+", "");
                    result = Regex.Match(_buffer, @"Host:(.+)");
                    if (!result.Success)
                    {
                        mySocket.Close();
                        continue;
                    }

                    string _hosts = result.Groups[1].Value.Trim().ToLower();
                    string _tmpPath = Regex.Replace(_filePath, @"\?.+$", ""), _localPath = null;
                    if (Properties.Settings.Default.LocalUpload)
                    {
                        if (File.Exists(Properties.Settings.Default.LocalPath + _tmpPath))
                            _localPath = Properties.Settings.Default.LocalPath + _tmpPath.Replace("/", "\\");
                        else if (File.Exists(Properties.Settings.Default.LocalPath + "\\" + Path.GetFileName(_tmpPath)))
                            _localPath = Properties.Settings.Default.LocalPath + "\\" + Path.GetFileName(_tmpPath);
                        else if (dicAppLocalUploadFile.ContainsKey(_filePath) && File.Exists(Properties.Settings.Default.LocalPath + "\\" + dicAppLocalUploadFile[_filePath]))
                        {
                            _tmpPath = dicAppLocalUploadFile[_filePath];
                            _localPath = Properties.Settings.Default.LocalPath + "\\" + _tmpPath;
                        }
                    }
                    string _extension = Path.GetExtension(_tmpPath).ToLowerInvariant();
                    if (Properties.Settings.Default.LocalUpload && !string.IsNullOrEmpty(_localPath))
                    {
                        using (FileStream fs = new FileStream(_localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (BinaryReader br = new BinaryReader(fs))
                            {
                                string _contentRange = null, _status = "200 OK";
                                long _fileLength = br.BaseStream.Length, _startPosition = 0;
                                long _endPosition = _fileLength;
                                result = Regex.Match(_buffer, @"Range: bytes=(?<StartPosition>\d+)(-(?<EndPosition>\d+))?");
                                if (result.Success)
                                {
                                    _startPosition = long.Parse(result.Groups["StartPosition"].Value);
                                    if (_startPosition > br.BaseStream.Length) _startPosition = 0;
                                    if (!string.IsNullOrEmpty(result.Groups["EndPosition"].Value))
                                        _endPosition = long.Parse(result.Groups["EndPosition"].Value) + 1;
                                    _contentRange = "bytes " + _startPosition + "-" + (_endPosition - 1) + "/" + _fileLength;
                                    _status = "206 Partial Content";
                                }

                                StringBuilder sb = new StringBuilder();
                                sb.Append("HTTP/1.1 " + _status + "\r\n");
                                sb.Append("Content-Type: " + System.Web.MimeMapping.GetMimeMapping(_filePath) + "\r\n");
                                sb.Append("Content-Length: " + (_endPosition - _startPosition) + "\r\n");
                                if (_contentRange != null) sb.Append("Content-Range: " + _contentRange + "\r\n");
                                sb.Append("Accept-Ranges: bytes\r\n\r\n");

                                Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                mySocket.Send(_headers, 0, _headers.Length, SocketFlags.None, out _);

                                br.BaseStream.Position = _startPosition;
                                int _size = 4096;
                                while (Form1.bServiceFlag && mySocket.Connected)
                                {
                                    long _remaining = _endPosition - br.BaseStream.Position;
                                    if (Properties.Settings.Default.Truncation && _extension == ".xcp" && _remaining <= 1048576) //Xbox360主机本地上传防爆头
                                    {
                                        Thread.Sleep(1000);
                                        continue;
                                    }
                                    byte[] _response = new byte[_remaining <= _size ? _remaining : _size];
                                    br.Read(_response, 0, _response.Length);
                                    mySocket.Send(_response, 0, _response.Length, SocketFlags.None, out _);
                                    if (_remaining <= _size) break;
                                }
                            }
                        }
                        if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("本地上传", _localPath, ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString());
                    }
                    else
                    {
                        bool _redirect = false;
                        string _newHosts = null;
                        switch (_hosts)
                        {
                            case "assets1.xboxlive.com":
                            case "assets2.xboxlive.com":
                            case "dlassets.xboxlive.com":
                            case "dlassets2.xboxlive.com":
                            case "d1.xboxlive.com":
                            case "d2.xboxlive.com":
                                if (Properties.Settings.Default.Redirect)
                                {
                                    _redirect = true;
                                    _newHosts = Regex.Replace(_hosts, @"\.com$", ".cn");
                                }
                                if (dicFilePath.TryAdd(_filePath, null))
                                    ThreadPool.QueueUserWorkItem(delegate { UpdateGameUrl(_hosts, _filePath, _extension); });
                                break;
                            case "xvcf1.xboxlive.com":
                                if (Properties.Settings.Default.Redirect)
                                {
                                    _redirect = true;
                                    _newHosts = "assets1.xboxlive.cn";
                                }
                                if (dicFilePath.TryAdd(_filePath, null))
                                    ThreadPool.QueueUserWorkItem(delegate { UpdateGameUrl(_hosts, _filePath, _extension); });
                                break;
                            case "xvcf2.xboxlive.com":
                                if (Properties.Settings.Default.Redirect)
                                {
                                    _redirect = true;
                                    _newHosts = "assets2.xboxlive.cn";
                                }
                                if (dicFilePath.TryAdd(_filePath, null))
                                    ThreadPool.QueueUserWorkItem(delegate { UpdateGameUrl(_hosts, _filePath, _extension); });
                                break;
                            case "us.cdn.blizzard.com":
                            case "eu.cdn.blizzard.com":
                            case "kr.cdn.blizzard.com":
                            case "level3.blizzard.com":
                                if (Properties.Settings.Default.BattleStore && Properties.Settings.Default.BattleCDN)
                                {
                                    _redirect = true;
                                    _newHosts = "blzddist1-a.akamaihd.net";
                                }
                                break;
                            case "epicgames-download1.akamaized.net":
                            case "download.epicgames.com":
                            case "download2.epicgames.com":
                            case "download3.epicgames.com":
                            case "download4.epicgames.com":
                            case "fastly-download.epicgames.com":
                                if (Properties.Settings.Default.EpicStore && Properties.Settings.Default.EpicCDN)
                                {
                                    _redirect = true;
                                    _newHosts = "epicgames-download1-1251447533.file.myqcloud.com";
                                }
                                break;
                        }
                        if (_redirect)
                        {
                            string _url = "http://" + _newHosts + _filePath;
                            StringBuilder sb = new StringBuilder();
                            sb.Append("HTTP/1.1 301 Moved Permanently\r\n");
                            sb.Append("Content-Type: text/html\r\n");
                            sb.Append("Location: " + _url + "\r\n");
                            sb.Append("Content-Length: 0\r\n\r\n");
                            Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                            mySocket.Send(_headers, 0, _headers.Length, SocketFlags.None, out _);
                            if (Properties.Settings.Default.RecordLog)
                            {
                                int argb = 0;
                                switch (_hosts)
                                {
                                    case "assets1.xboxlive.com":
                                    case "assets2.xboxlive.com":
                                    case "dlassets.xboxlive.com":
                                    case "dlassets2.xboxlive.com":
                                    case "d1.xboxlive.com":
                                    case "d2.xboxlive.com":
                                    case "xvcf1.xboxlive.com":
                                    case "xvcf2.xboxlive.com":
                                        argb = 0x008000;
                                        break;
                                }
                                parentForm.SaveLog("HTTP 301", _url, ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString(), argb);
                            }
                        }
                        else
                        {
                            bool bFileNotFound = true;
                            string _url = "http://" + _hosts + _filePath;
                            if (_hosts == "dl.delivery.mp.microsoft.com" || _extension == ".phf" || _extension == ".json") //代理 Xbox|PS 下载索引
                            {
                                string ip = ClassDNS.DoH(_hosts);
                                if (!string.IsNullOrEmpty(ip))
                                {
                                    SocketPackage socketPackage = ClassWeb.HttpRequest(_url, "GET", null, null, true, false, false, null, null, null, null, null, null, null, null, 0, null, 30000, 30000, 1, ip);
                                    if (string.IsNullOrEmpty(socketPackage.Err))
                                    {
                                        bFileNotFound = false;
                                        string str = socketPackage.Headers;
                                        str = Regex.Replace(str, @"(Content-Encoding|Transfer-Encoding|Content-Length): .+\r\n", "");
                                        str = Regex.Replace(str, @"\r\n\r\n", "\r\nContent-Length: " + socketPackage.Buffer.Length + "\r\n\r\n");
                                        Byte[] _headers = Encoding.ASCII.GetBytes(str);
                                        mySocket.Send(_headers, 0, _headers.Length, SocketFlags.None, out _);
                                        mySocket.Send(socketPackage.Buffer, 0, socketPackage.Buffer.Length, SocketFlags.None, out _);
                                        if (Properties.Settings.Default.RecordLog)
                                        {
                                            Match m1 = Regex.Match(socketPackage.Headers, @"^HTTP[^\s]+\s([^\s]+)");
                                            if (m1.Success) parentForm.SaveLog("HTTP " + m1.Groups[1].Value, _url, ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString());
                                            if (_hosts.EndsWith(".prod.dl.playstation.net") && _extension == ".json") //分析PS4游戏下载地址
                                            {
                                                string html = Encoding.GetEncoding("utf-8").GetString(socketPackage.Buffer);
                                                if (Regex.IsMatch(html, @"^{.+}$"))
                                                {
                                                    JavaScriptSerializer js = new JavaScriptSerializer();
                                                    try
                                                    {
                                                        var json = js.Deserialize<PsGame.Game>(html);
                                                        if (json != null && json.Pieces != null && json.Pieces.Count >= 1)
                                                        {
                                                            StringBuilder sb = new StringBuilder();
                                                            sb.AppendLine("下载文件总数：" + json.NumberOfSplitFiles + "，容量：" + ClassMbr.ConvertBytes(Convert.ToUInt64(json.OriginalFileSize)) + "，下载地址：");
                                                            foreach (var pieces in json.Pieces)
                                                                sb.AppendLine(pieces.Url);
                                                            parentForm.SaveLog("下载地址", sb.ToString(), ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString(), 0x008000);
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (Properties.Settings.Default.LocalUpload && _hosts == "tlu.dl.delivery.mp.microsoft.com" && !dicAppLocalUploadFile.ContainsKey(_filePath)) //识别本地上传应用文件名
                            {
                                string ip = ClassDNS.DoH(_hosts);
                                if (!string.IsNullOrEmpty(ip))
                                {
                                    SocketPackage socketPackage = ClassWeb.HttpRequest(_url, "GET", null, null, true, false, false, null, null, new string[] { "Range: bytes=0-0" }, null, null, null, null, null, 0, null, 30000, 30000, 1, ip);
                                    Match m1 = Regex.Match(socketPackage.Headers, @"Content-Disposition: attachment; filename=(.+)");
                                    if (m1.Success)
                                    {
                                        string filename = m1.Groups[1].Value.Trim();
                                        dicAppLocalUploadFile.AddOrUpdate(_filePath, filename, (oldkey, oldvalue) => filename);
                                    }
                                }
                            }
                            else if (_hosts == "www.msftconnecttest.com" && _tmpPath.ToLower() == "/connecttest.txt") // 网络连接 (NCSI)，修复 Xbox、Windows 系统网络正常却显示离线
                            {
                                bFileNotFound = false;
                                Byte[] _response = Encoding.ASCII.GetBytes("Microsoft Connect Test");
                                StringBuilder sb = new StringBuilder();
                                sb.Append("HTTP/1.1 200 OK\r\n");
                                sb.Append("Content-Type: text/plain\r\n");
                                sb.Append("Content-Length: " + _response.Length + "\r\n\r\n");
                                Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                mySocket.Send(_headers, 0, _headers.Length, SocketFlags.None, out _);
                                mySocket.Send(_response, 0, _response.Length, SocketFlags.None, out _);
                                if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("HTTP 200", _url, ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString());
                            }
                            else if (_hosts == "ctest.cdn.nintendo.net" && _tmpPath.ToLower() == "/")
                            {
                                bFileNotFound = false;
                                if (Properties.Settings.Default.NSBrowser)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    sb.Append("HTTP/1.1 302 Moved Temporarily\r\n");
                                    sb.Append("Content-Type: text/html\r\n");
                                    sb.Append("Location: " + Properties.Settings.Default.NSHomepage + "\r\n");
                                    sb.Append("Content-Length: 0\r\n\r\n");
                                    Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                    mySocket.Send(_headers, 0, _headers.Length, SocketFlags.None, out _);
                                }
                                else
                                {
                                    Byte[] _response = Encoding.ASCII.GetBytes("ok");
                                    StringBuilder sb = new StringBuilder();
                                    sb.Append("HTTP/1.1 200 OK\r\n");
                                    sb.Append("Content-Type: text/plain\r\n");
                                    sb.Append("X-Organization: Nintendo\r\n");
                                    sb.Append("Content-Length: " + _response.Length + "\r\n\r\n");
                                    Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                    mySocket.Send(_headers, 0, _headers.Length, SocketFlags.None, out _);
                                    mySocket.Send(_response, 0, _response.Length, SocketFlags.None, out _);
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("HTTP 200", _url, ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString());
                                }
                            }
                            if (bFileNotFound)
                            {
                                Byte[] _response = Encoding.ASCII.GetBytes("File not found.");
                                StringBuilder sb = new StringBuilder();
                                sb.Append("HTTP/1.1 404 Not Found\r\n");
                                sb.Append("Content-Type: text/html\r\n");
                                sb.Append("Content-Length: " + _response.Length + "\r\n\r\n");
                                Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                mySocket.Send(_headers, 0, _headers.Length, SocketFlags.None, out _);
                                mySocket.Send(_response, 0, _response.Length, SocketFlags.None, out _);
                                if (Properties.Settings.Default.RecordLog)
                                {

                                    int argb = 0;
                                    switch (_hosts)
                                    {
                                        case "assets1.xboxlive.com":
                                        case "assets2.xboxlive.com":
                                        case "dlassets.xboxlive.com":
                                        case "dlassets2.xboxlive.com":
                                        case "d1.xboxlive.com":
                                        case "d2.xboxlive.com":
                                        case "xvcf1.xboxlive.com":
                                        case "xvcf2.xboxlive.com":
                                        case "assets1.xboxlive.cn":
                                        case "assets2.xboxlive.cn":
                                        case "dlassets.xboxlive.cn":
                                        case "dlassets2.xboxlive.cn":
                                        case "d1.xboxlive.cn":
                                        case "d2.xboxlive.cn":
                                            argb = 0x008000;
                                            if (dicFilePath.TryAdd(_filePath, null))
                                                ThreadPool.QueueUserWorkItem(delegate { UpdateGameUrl(_hosts, _filePath, _extension); });
                                            break;
                                        case "tlu.dl.delivery.mp.microsoft.com":
                                        case "download.xbox.com":
                                        case "download.xbox.com.edgesuite.net":
                                        case "xbox-ecn102.vo.msecnd.net":
                                        case "gst.prod.dl.playstation.net":
                                        case "gs2.ww.prod.dl.playstation.net":
                                        case "zeus.dl.playstation.net":
                                        case "ares.dl.playstation.net":
                                            argb = 0x008000;
                                            break;
                                    }
                                    parentForm.SaveLog("HTTP 404", _url, ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString(), argb);
                                }
                            }
                        }
                    }
                }
            }
            if (mySocket.Connected)
            {
                try
                {
                    mySocket.Shutdown(SocketShutdown.Both);
                }
                catch { }
            }
            mySocket.Close();
            mySocket.Dispose();
        }

        public void Close()
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
                socket = null;
            }
        }

        readonly ConcurrentDictionary<String, String> dicFilePath = new ConcurrentDictionary<String, String>();
        private void UpdateGameUrl(string _hosts, string _filePath, string _extension)
        {
            if (Regex.IsMatch(_extension, @"\.(phf|xsp)$")) return;

            Match result = Regex.Match(_filePath, @"/(?<ContentId>\w{8}-\w{4}-\w{4}-\w{4}-\w{12})/(?<Version>\d+\.\d+\.\d+\.\d+)\.\w{8}-\w{4}-\w{4}-\w{4}-\w{12}");
            if (result.Success)
            {
                string key = result.Groups["ContentId"].Value.ToLower();
                if (Regex.IsMatch(_filePath, @"_xs\.xvc$", RegexOptions.IgnoreCase))
                    key += "_xs";
                else if (!Regex.IsMatch(_filePath, @"\.msixvc$", RegexOptions.IgnoreCase))
                    key += "_x";
                Version version = new Version(result.Groups["Version"].Value);
                if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products XboxGame))
                {
                    if (XboxGame.Version >= version) return;
                }
                _hosts = _hosts.Replace(".xboxlive.cn", ".xboxlive.com");
                if (!DnsListen.dicHosts2.TryGetValue(_hosts, out IPAddress ip))
                {
                    if (IPAddress.TryParse(ClassDNS.DoH(_hosts), out ip))
                    {
                        DnsListen.dicHosts2.AddOrUpdate(_hosts, ip, (oldkey, oldvalue) => ip);
                    }
                }
                if (ip != null)
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest("http://" + _hosts + _filePath, "GET", null, null, true, false, false, null, null, new string[] { "Range: bytes=0-0" }, ClassWeb.useragent, null, null, null, null, 0, null, 30000, 30000, 1, ip.ToString());
                    Match m1 = Regex.Match(socketPackage.Headers, @"Content-Range: bytes 0-0/(?<FileSize>\d+)");
                    if (m1.Success)
                    {
                        ulong filesize = ulong.Parse(m1.Groups["FileSize"].Value);
                        XboxGame = new XboxGameDownload.Products
                        {
                            Version = version,
                            FileSize = filesize,
                            Url = "http://" + _hosts + _filePath
                        };
                        XboxGameDownload.dicXboxGame.AddOrUpdate(key, XboxGame, (oldkey, oldvalue) => XboxGame);
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
                        ClassWeb.HttpRequest(UpdateFile.getXboxUrl + "/Game/AddUrl?url=" + ClassWeb.UrlEncode(XboxGame.Url), "PUT", null, null, true, false, true, null, null, new String[] { "X-Organization: XboxDownload", "X-Author: Devil" }, null, null, null, null, null, 0, null);
                    }
                }
            }
        }
    }
}
