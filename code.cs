using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

class Program
{
   [DllImport("user32.dll")]
   private static extern IntPtr GetForegroundWindow();
   [DllImport("user32.dll")]
   private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
   static void Main(string[] args)
   {
       string jsonFile = "time.json";
       var timeTracking = LoadJson(jsonFile);
       string lastExeName = null;
       DateTime lastSwitchTime = DateTime.Now;
       while (true)
       {
           string currentExeName = GetActiveWindowExe();
           if (currentExeName != lastExeName)
           {
               if (lastExeName != null)
               {
                   var elapsedTime = DateTime.Now - lastSwitchTime;
                   var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                   timeTracking.TryAdd(currentDate, new());
                   timeTracking[currentDate].TryAdd(lastExeName, new() { { "TimeSpent", "00.00.00" } });
                   var existingTime = timeTracking[currentDate][lastExeName]["TimeSpent"];
                   var totalTime = ParseTime(existingTime) + elapsedTime;
                   timeTracking[currentDate][lastExeName]["TimeSpent"] = FormatTimeSpan(totalTime);
                   SaveJson(jsonFile, timeTracking);
               }
               lastExeName = currentExeName;
               lastSwitchTime = DateTime.Now;
           }
           Thread.Sleep(1000);
       }
   }
   static string GetActiveWindowExe()
   {
       GetWindowThreadProcessId(GetForegroundWindow(), out int processId);
       try
       {
           return Path.GetFileName(Process.GetProcessById(processId).MainModule.FileName);
       }
       catch { return "Unknown"; }
   }
   static string FormatTimeSpan(TimeSpan timeSpan) =>
       string.Format("{0:00}.{1:00}.{2:00}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
   static TimeSpan ParseTime(string timeString)
   {
       var parts = timeString.Split('.');
       return new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
   }
   static Dictionary<string, Dictionary<string, Dictionary<string, string>>> LoadJson(string path)
   {
       if (!File.Exists(path)) return new();
       var json = File.ReadAllText(path);
       return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(json) ?? new();
   }
   static void SaveJson(string path, Dictionary<string, Dictionary<string, Dictionary<string, string>>> data) =>
       File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}
