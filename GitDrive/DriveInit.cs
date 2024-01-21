using Microsoft.Win32;
using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;

namespace GitDrive
{
    internal class DriveInit
    {
        public static DriveInfo Drive { get; set; }
        public static char DriveLetter { get; set; }

        private static void CreateFolders(string[] paths)
        {
            foreach(var p in paths) if (!Directory.Exists(p)) Directory.CreateDirectory(p);
        }
        public static void InitDrive()
        {
            CreateFolders([Program.DefaultFilesPath, Program.DefaultDataPath]);

            char installedDrive = GetMappedDrive();

            if (installedDrive != char.MinValue)
            {
                DriveLetter = installedDrive;
                Drive = new DriveInfo(DriveLetter + ":\\");

                return;
            }

            char selectedDrive = GetAvaliableLabel();

            
            Subst.MapDrive(selectedDrive, Program.DefaultDataPath);
            Subst.SetDriveLabel(selectedDrive.ToString(), "GitDrive");
            Subst.SetDriveIcon(selectedDrive.ToString(), "C:\\Users\\Mrgaton\\OneDrive\\Programas\\Programas de CSharp\\GitDrive\\git_logo.ico");
        }
        private static char GetMappedDrive()
        {
            foreach(var drive in DriveInfo.GetDrives())
            {
                if (string.Equals(Subst.GetDriveMapping(drive.Name), Program.DefaultDataPath,StringComparison.InvariantCultureIgnoreCase)) return drive.Name.Split(':')[0][0];
            }

            return char.MinValue;
        }

        private static char[] validDrivesLetters = Enumerable.Range('A', 'Z' - 'A' + 1).Select(i => (char)i).ToArray();

        private static char GetAvaliableLabel()
        {
            var discs = DriveInfo.GetDrives().Select(d => d.Name.Split(':')[0].First());

            return validDrivesLetters.First(l => !discs.Contains(l));
        }

        private static class Subst
        {
            //Thx to: https://stackoverflow.com/questions/3753758/creating-virtual-hard-drive
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool DefineDosDevice(int flags, string devname, string path);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern int QueryDosDevice(string devname, StringBuilder buffer, int bufSize);

            private static string devName(char letter) => new string(char.ToUpper(letter), 1) + ":";

            public static void MapDrive(char letter, string path)
            {
                if (!DefineDosDevice(0, devName(letter), path)) throw new Win32Exception();
            }

            public static void UnmapDrive(char letter)
            {
                if (!DefineDosDevice(2, devName(letter), null)) throw new Win32Exception();
            }
            public static void SetDriveLabel(string driveLetter, string label)
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\" + driveLetter))
                {
                    if (key == null) return;
                    
                        key.SetValue("_LabelFromReg", label, RegistryValueKind.String);
                    
                }
            }
            public static void SetDriveIcon(string driveLetter, string iconPath)
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\" + driveLetter + @"\DefaultIcon"))
                {
                    if (key == null) return;
                    
                        key.SetValue("", iconPath, RegistryValueKind.String);
                    
                }
            }
            public static string GetDriveMapping(char letter) => GetDriveMapping(letter + ":");
            public static string GetDriveMapping(string name)
            {
                var sb = new StringBuilder(4096);
                if (QueryDosDevice(name.TrimEnd('\\'), sb, sb.Capacity) == 0)
                {
                    if (Marshal.GetLastWin32Error() == 2) return "";
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                return sb.ToString().Substring(4);
            }
        }
    }
}