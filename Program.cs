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
            DoWork();
            //try
            //{
            //    DoWork();
            //}
            //catch
            //{
            //    System.Windows.Forms.MessageBox.Show("Something bad has happened to FTP Sync");
            //}
        }

        private static void DoWork()
        {
            
            Ftp.FtpGetSortedDirList();
            Loc.LoadLocalDats();
            Loc.SortLocalDats();
            Ftp.AddNewFtpDats();
            Ftp.UploadDatsInList();
            Ftp.DownloadDatsInList();
        }
    }
}

