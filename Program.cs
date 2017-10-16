using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FTPSync
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                DoWork();
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Something bad has happened to FYP Sync");
            }
        }

        private static void DoWork()
        {
            Tools.LoadGVfromINIfile();
            FTP.LoadFtpDats();
            Loc.LoadLocalDats();
            Loc.SortLocalDats();
            FTP.AddNewFtpDats();
            Loc.DoUploads();
            FTP.DoDownloads();
            Console.WriteLine("FTPSync finished");
            Console.ReadLine();
        }
    }
}

