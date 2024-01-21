namespace GitDrive
{
    internal class Program
    {
        public static readonly string DefaultProgramPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "GitDrive");
        public static readonly string DefaultFilesPath = Path.Combine(DefaultProgramPath, "UserFiles");
        public static readonly string DefaultDataPath = Path.Combine(DefaultProgramPath, "DefaultData");

        private static void Main(string[] args)
        {
            DriveInit.InitDrive();

            foreach (var d in DriveInfo.GetDrives())
            {
                Console.WriteLine(d.Name);
                Console.WriteLine(d.DriveFormat);
                Console.WriteLine(d.DriveType);
            }

        }
    }
}