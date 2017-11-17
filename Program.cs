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
            catch (Exception e)
            {
                Tools.Report("Main loop error", e);
                Ftp.RenameFile(Ftp.ServerPath + "FtpBusy.txt", "/" + Ftp.DatDir + "/FtpReady.txt");
                Tools.WriteLog();
            }
        }

        private static void DoWork()
        {
           
                Tools.ReadIniFile();
                if (!Ftp.WaitForFtpReady())
                    throw new Exception("ftp never ready");

                if (!Ftp.RenameFile(Ftp.ServerPath + "FtpReady.txt", "/" + Ftp.DatDir + "/FtpBusy.txt"))
                    throw new Exception("rename from ready to busy");

                Ftp.FtpGetSortedDirList();
                Loc.LoadLocalDats();
                Loc.SortLocalDats();
                Ftp.AddNewFtpDats();
                Ftp.UploadDatsInList();
                Ftp.DownloadDatsInList();
                if(!Ftp.RenameFile(Ftp.ServerPath + "FtpBusy.txt", "/" + Ftp.DatDir + "/FtpReady.txt"))
                    throw new Exception("rename from busy to ready");

                if (Tools.ErrorOccured)
                    Tools.WriteLog();
          
        }
    }
}

