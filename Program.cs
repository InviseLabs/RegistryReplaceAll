/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
* Registry Replace All by Invise Labs / Authors: Mike Lierman
* Copyright Invise Labs 2023.
* Open-source under Creative Commons Non-Commercial Use.
* For use in commercial and for-profit applications, please contact us.
* We can be reached at contact@inviselabs.com.
* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */


using Microsoft.SqlServer.Server;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Windows.Forms;

namespace RegistryReplaceAll
{
    internal class Program
    {
        static string aboutAndAuthorsLine = @"Registry Replace All by Invise Labs / Authors: Mike Lierman"; //- AUTHORS LINE - CHANGE IF ANYONE ELSE AT INVISE HELPS CONTRIBUTE
        static string copyrightLine = @"Copyright Invise Labs 2023. Open-source under Creative Commons Non-Commercial Use"; //- COPYRIGHT LINE

        static string findTerm = "";
        static string replaceTerm = "";
        static int workersWorking = 0;
        static int workersReady = 0;
        static int workersTotal = 1;
        static string currentPath = ""; //- Current path we are working on
        static string[] workersStatus;
        static bool getArgs = true; //- Whether to get inputs from user, such as findTerm, replaceTerm

        //- Variables related to possible arguments
        static bool logEnabled = false;
        static string logPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\log.txt";
        static bool acceptTerms = false;
        static bool silentRun = false;
        static bool confirm = false;


        //- Status Variables
        static int replaced = 0;
        static int processed = 0;
        static int errs= 0;

        static AutoResetEvent notify = new AutoResetEvent(false);

        //- Setup console switching for hidden or visible
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static void Main(string[] args)
        {
            Console.Title = "Registry Replace All by Invise Labs";

            if (args.Length >= 2) //- Minimum number of arguments 
            {
                if ((String.IsNullOrWhiteSpace(args[0]) || args[0].Length < 3)
                 || (String.IsNullOrWhiteSpace(args[1]) || args[1].Length < 3)) { TermsAndInput(true); } //- Minimum amount of characters for each term is 3

                findTerm = args[0];
                replaceTerm = args[1];
                getArgs = false;

                if (args.Length > 2)
                {
                    for (int i = 2; i < args.Length; i++)
                    {
                        string temp = args[i];

                        //- If arg is invalid
                        if (String.IsNullOrWhiteSpace(args[i]) && args[i].Length < 4)
                        { TermsAndInput(true); }


                        switch (args[i].ToLower())
                        {
                            case "accept":
                                acceptTerms = true;
                                break;

                            case "a":
                                acceptTerms |= true;
                                break;

                            case "silent":
                                silentRun = true;
                                break;

                            case "s":
                                silentRun = true;
                                break;

                            case "log":
                                logEnabled = true;
                                logPath = args[i];
                                break;

                            case "l":
                                logEnabled = true;
                                logPath = args[i];
                                break;

                            case "noconfirm":
                                confirm = true;
                                break;

                            case "nc":
                                confirm = true;
                                break;

                            default:
                                TermsAndInput(true);
                                break;
                        }
                    }
                }
            }
            else if (args.Length > 0)
            { TermsAndInput(true); }

            //- Silent Run implies accept terms, and does not show any console window
            if (silentRun)
            {

                //Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);
                //Application.Run(new Form1());
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
                Go();
            }
            else
            {

                /* Doesn't work. Was suppose to allow attaching console to WinForms app,
                 * but nothing appears when writing lines after attaching.
                if (!AttachConsole(-1))
                {  AllocConsole(); }*/

if (acceptTerms)
                {
                    ResetConsole();
                    GetSet();
                }
                else { TermsAndInput(); }
            }
        }

        static void TermsAndInput(bool invalidArgs = false)
        {
            Console.WriteLine(aboutAndAuthorsLine);
            Console.WriteLine(copyrightLine);
            Console.WriteLine("For commercial & biz for-profit use please contact us for inexpensive usage options.");
            Console.WriteLine("You can also hire us to create an IT / tech tool for your unlimited use.");
            Console.WriteLine(@"Find more tools & projects @ inviselabs.com, see also github.com/inviselabs");
            Console.WriteLine("DISCLAIMER: This can and will probably hose your OS unless you know what you're doing!");
            Console.WriteLine("_________________________________________________________________________________________");
            Console.WriteLine("Available startup arguments: /find (/f) \"term or file path to search for\"");
            Console.WriteLine("/replaceall (/r) \"term or file path to replace with\" /log (/l) path for log file");
            Console.WriteLine("/accept (/a) accept license terms, but does not skip confirm step");
            Console.WriteLine("/silent (/s) No visible window, log only. Implies /accept and /noconfirm (CAREFUL!)");
            Console.WriteLine("/noconfirm (/nc) skip confirmation start modifying registry (CAREFUL!)");
            Console.WriteLine("");

            if (invalidArgs)
            {
                getArgs = true;
                Console.WriteLine("Specified arguments are either invalid, null, or empty. Try again or proceed with manual mode.");
                Console.WriteLine("");
            }

            if (!acceptTerms)
            {
                Console.WriteLine("Press any key to accept terms, including risk of data loss, and begin...");
                Console.ReadKey();
                ResetConsole();
            }

            if (getArgs)
            {
                Console.WriteLine("Enter required inputs...");
                Console.Write("Search the Windows registry for: ");

                //- Loop until user providers appropriate input
                while (String.IsNullOrWhiteSpace(findTerm = Console.ReadLine()))
                { Console.WriteLine(""); Console.Write("Enter Valid Search Term: "); }

                //- Loop until user providers appropriate input
                Console.WriteLine("");
                Console.Write("Replace ALL instances of that with: ");
                while (String.IsNullOrWhiteSpace(replaceTerm = Console.ReadLine()))
                { Console.WriteLine(""); Console.Write("Enter Valid Replace Term: "); }


            }

            if (!logEnabled)
            {
                Console.WriteLine("");
                Console.WriteLine("If enabled, log path would be:");
                Console.WriteLine(logPath);
                Console.Write("Enable Log? Y/N: ");
                string input = ""+Console.ReadLine();

                if (input.ToLower().Contains("yes") || input.ToLower().Contains("y"))
                { logEnabled = true; }
                else if (input.ToLower().Contains("no") || !input.ToLower().Contains("n"))
                { logEnabled = false; }
                else
                {
                    while ((input = Console.ReadLine()) != "skip")
                    {
                        Console.WriteLine(""); Console.Write("Enter Response Y/N: ");
                        input = Console.ReadLine();
                        Console.WriteLine($"You wrote \"{input}\"");

                        if (input.ToLower().Contains("yes") || input.ToLower().Contains("y"))
                        { logEnabled = true; break; }
                        else if (input.ToLower().Contains("no") || !input.ToLower().Contains("n"))
                        { logEnabled = false; break; }
                    }
                }
            }

            ResetConsole();
            GetSet();
        }

        static void GetSet()
        {
            workersReady = Environment.ProcessorCount;


            if (!confirm)
            {
                Console.WriteLine($"Find: \"{findTerm}\"");
                Console.WriteLine($"Replace all with: \"{replaceTerm}\"");
                Console.WriteLine("");
                Console.WriteLine("Is this correct? Last chance before hosing your OS. Press any key to begin...");
                Console.ReadKey();
                ResetConsole();
            }

            Console.WriteLine($"Find: \"{findTerm}\""); //- Line 4
            Console.WriteLine($"Replace all with: \"{replaceTerm}\""); //- Line 5
            Console.WriteLine($"Logging: " + logEnabled.ToString());//- Line 6
            Console.WriteLine(""); //- Line 7

            Go();
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        static void ResetConsole()
        {
            Console.Clear();
            Console.WriteLine(aboutAndAuthorsLine); //- Line 0
            Console.WriteLine(copyrightLine); //- Line 1
            Console.WriteLine("____________________________________"); //- Line 2
            Console.WriteLine(); //- Line 3
        }

        static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            int line = 10;
            Console.SetCursorPosition(0, 9); Console.Write($"Workers: {workersTotal} | Ready: {workersReady} | Working: {workersWorking} | Processed: {processed} | Errors: {errs}"); //- Line 9

            for (int i = 0; i < workersTotal; i++)
            {
                if (String.IsNullOrWhiteSpace(workersStatus[i])) { continue; } //- Instead of showing status pending, skip it and show only active and finished
                line = 10 + i; //- Starting at line 10 always
                Console.SetCursorPosition(0, line);
                ClearCurrentConsoleLine();
                Console.Write($"Worker {i + 1}/{workersTotal}: " + workersStatus[i]);
            }

            Console.WriteLine("");
            Console.SetCursorPosition(0, line + 1);
            ClearCurrentConsoleLine();
            Console.SetCursorPosition(0, line + 2);
            Console.Write("=========="); //- Last Line 10
            Console.WriteLine("");
        }

        static void Log(string text)
        {
            try
            {
                using (var file = File.Open(logPath, FileMode.OpenOrCreate))
                {
                    file.Seek(0, SeekOrigin.End);
                    using (var stream = new StreamWriter(file))
                        stream.WriteLine(text);
                }
            }
            catch { errs++; /* Error when writing to error log, that sucks. Oh well. */ }
        }

        static void Go()
        {
            //- String to store output text
            string output = "";

            workersStatus = new string[workersReady]; //- Define status strings, one for each worker, showing what the worker is working on

            //- If not a silent run, then set timer to update worker statuses
            if (!silentRun)
            {
                //- Update the status every 
                Timer t = new Timer();
                t.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
                t.Interval = 500;
                t.Start();

                Console.WriteLine("=========="); //- Line 8
                Console.WriteLine($"Workers: {workersTotal} | Ready: {workersReady} | Working: {workersWorking}"); //- Line 9
                Console.WriteLine($"");
                Console.WriteLine("=========="); //- Last Line 10
                Thread.Sleep(1000);
            }

            //- Create list of keys to iterate through, in the future can be specified through arguments to enable/disable
            List<RegistryKey> list = new List<RegistryKey>();
            list.Add(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default));
            list.Add(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default));
            list.Add(RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default));
            list.Add(RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Default));
            list.Add(RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default));
            list.Add(RegistryKey.OpenBaseKey(RegistryHive.DynData, RegistryView.Default));
            list.Add(RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Default));

            //- Iterate through keys in list, creating worker threads for each
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    RegistryKey k = list[i];

                    //- Wait here if not enough workers
                    while (workersReady == 0)
                    {
                        if (!silentRun) { Console.SetCursorPosition(0, 10 + i); ClearCurrentConsoleLine(); Console.Write($"Waiting for a worker to become available for {k.Name}"); }
                        notify.WaitOne();
                    }

                    //- Wait 400ms to not overload system, then start next worker thread
                    Thread.Sleep(400);
                    if (!silentRun) { Console.SetCursorPosition(0, 10 + i); ClearCurrentConsoleLine(); Console.Write($"Worker {i + 1}/{workersTotal}: Starting {k.Name}"); }
                    workersReady--;
                    workersTotal++;
                    workersWorking++;
                    ReplaceAllInKey(k, i);
                }
                catch (Exception ex) { Log(ex.ToString()); errs++; }
            }

            output = $"Completed in seconds. Processed: {processed}, Replaced: {replaced}";
            if (logEnabled) { Log(output); }

            if (!silentRun) { Console.WriteLine($"{output}"); }

            Console.ReadKey();
        }

        static void ReplaceAllInKey(RegistryKey baseKey, int workNum)
        {
            try
            {
                //- Define output stream variable
                string output = "";

                //- Create a stack to store the keys to be visited
                Stack<RegistryKey> stack = new Stack<RegistryKey>();

                //- Push the base key to the stack
                stack.Push(baseKey);
                processed++;

                // Loop until the stack is empty
                while (stack.Count > 0)
                {
                    try
                    {
                        //- Pop the top key from the stack
                        RegistryKey key = stack.Pop();

                        //- Update worker status
                        workersStatus[workNum] = key.ToString();
                        processed++;

                        //- Get the names of all subkeys under the current key
                        if (key.SubKeyCount > 0)
                        {
                            string[] subKeyNames = key.GetSubKeyNames();

                            //- Loop through each subkey name
                            if (subKeyNames != null && subKeyNames.Length > 0)
                            {
                                for (int i = 0; i < subKeyNames.Length; i++)
                                {
                                    try
                                    {
                                        //- Define sub key name and check if it's null
                                        string subName = subKeyNames[i].ToLower();
                                        if (String.IsNullOrWhiteSpace(subName)) { continue; }

                                        //- Update worker status
                                        workersStatus[workNum] = key.ToString()+"\\"+subName;
                                        processed++;

                                        //- Check if the subkey name contains the find string
                                        if (subName.ToLower().Contains(findTerm))
                                        {
                                            string newSubKeyName = "null";
                                            try
                                            {
                                                //- Replace the subkey name with the replace string
                                                newSubKeyName = Regex.Replace(subName, findTerm, replaceTerm, RegexOptions.IgnoreCase);

                                                //- Copy the subkey and its values to the new name
                                                key.CreateSubKey(newSubKeyName, true);
                                                CopyKey(key, subName, newSubKeyName);

                                                //- Delete the old subkey
                                                key.DeleteSubKeyTree(subName, true);

                                                replaced++;
                                                output = $"Replaced {key.ToString()}\\{subName} with {newSubKeyName}";
                                                if (logEnabled) { Log(output); }
                                                if (!silentRun) { Console.WriteLine(output); }
                                            }
                                            catch
                                            {
                                                errs++;
                                                output = $"Failed {key.ToString()}\\{subName} with {newSubKeyName}";
                                                if (logEnabled) { Log(output); }
                                                if (!silentRun) { Console.WriteLine(output); }
                                            }
                                        }

                                        //- Open the subkey
                                        RegistryKey subKey = key.OpenSubKey(subName, true);

                                        //- Update worker status
                                        workersStatus[workNum] = subKey.ToString();

                                        //- Push the subkey to the stack
                                        if (subKey != null && subKey.SubKeyCount > 0)
                                        { stack.Push(subKey); }
                                    }
                                    catch (Exception ex) { errs++; Log(ex.ToString()); }
                                }
                            }
                        }

                        // Get the names and values of all values under the current key
                        string[] valueNames = key.GetValueNames();
                        object[] values = new object[valueNames.Length];
                        for (int i = 0; i < valueNames.Length; i++)
                        {
                            try { values[i] = key.GetValue(valueNames[i]); }
                            catch (Exception ex) { errs++; Log(ex.ToString()); }
                        }

                        // Loop through each value name and value
                        for (int i = 0; i < valueNames.Length; i++)
                        {
                            try
                            {
                                string valueName = valueNames[i];
                                object value = values[i];
                                processed++;

                                // Check if the value name contains the find string
                                if (valueName.ToLower().Contains(findTerm))
                                {
                                    string newValueName = "null";
                                    try
                                    {
                                        // Replace the value name with the replace string
                                        newValueName = Regex.Replace(valueName, findTerm, replaceTerm, RegexOptions.IgnoreCase);

                                        // Set the value with the new name
                                        key.SetValue(newValueName, value);

                                        // Delete the old value
                                        key.DeleteValue(valueName, true);

                                        replaced++;
                                        output = $"Replaced {key.ToString()}\\{valueName} with {newValueName}";
                                        if (logEnabled) { Log(output); }
                                        if (!silentRun) { Console.WriteLine(output); }
                                    }
                                    catch
                                    {
                                        errs++;
                                        output = $"Failed {key.ToString()}\\{valueName} with {newValueName}";
                                        if (logEnabled) { Log(output); }
                                        if (!silentRun) { Console.WriteLine(output); }
                                    }
                                }

                                // Check if the value is a string and contains the find string
                                if (value is string && value.ToString().ToLower().Contains(findTerm))
                                {
                                    string newValue = "null";
                                    try
                                    {
                                        // Replace the value with the replace string
                                        newValue = Regex.Replace(value.ToString(), findTerm, replaceTerm, RegexOptions.IgnoreCase);

                                        // Set the value with the new value
                                        key.SetValue(valueName, newValue);

                                        replaced++;
                                        output = $"Replaced {key.ToString()}\\{valueName} >> Val: {valueName}, New: {newValue}";
                                        if (logEnabled) { Log(output); }
                                        if (!silentRun) { Console.WriteLine(output); }
                                    }
                                    catch
                                    {
                                        output = $"Failed {key.ToString()}\\{valueName} >> Val: {valueName}, New: {newValue}";
                                        if (logEnabled) { Log(output); }
                                        if (!silentRun) { Console.WriteLine(output); }
                                    }
                                }
                            }
                            catch (Exception ex) { errs++; Log(ex.ToString()); }
                        }
                    }
                    catch (Exception ex) { errs++; Log(ex.ToString()); }
                }

                //- Notify that this worker has finished
                workersStatus[workNum] = baseKey.ToString() + " Finished";
                workersReady++;
                notify.Set();

                output = $"{baseKey.ToString()} finished";
                if (logEnabled) { Log(output); }
            }
            catch (Exception ex) { errs++; Log(ex.ToString()); }
        }

        // A helper method that copies a subkey and its values to a new name
        static void CopyKey(RegistryKey parentKey, string sourceSubKeyName, string destSubKeyName)
        {
            try
            {
                // Open the source and destination subkeys
                RegistryKey sourceSubKey = parentKey.OpenSubKey(sourceSubKeyName, true);
                RegistryKey destSubKey = parentKey.OpenSubKey(destSubKeyName, true);

                // Get the names and values of all values under the source subkey
                string[] valueNames = sourceSubKey.GetValueNames();
                object[] values = new object[valueNames.Length];
                for (int i = 0; i < valueNames.Length; i++)
                {
                    try { values[i] = sourceSubKey.GetValue(valueNames[i]); }
                    catch (Exception ex) { errs++; Log(ex.ToString()); }
                }

                // Loop through each value name and value
                for (int i = 0; i < valueNames.Length; i++)
                {
                    try
                    {
                        string valueName = valueNames[i];
                        object value = values[i];

                        // Set the value with the same name under the destination subkey
                        destSubKey.SetValue(valueName, value);
                    }
                    catch (Exception ex) { errs++; Log(ex.ToString()); }
                }
            }
            catch (Exception ex) { errs++; Log(ex.ToString()); }
        }
    }
}

