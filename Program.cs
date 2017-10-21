﻿using System;
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
        }

        private static void DoWork()
        {
            Tools.ReadIniFile();
            Ftp.WaitForFtpReady();
            Ftp.RenameFile(Ftp.ServerPath + "/FtpReady.txt", "/" + Ftp.DatDir + "/FtpBusy.txt");
            Ftp.FtpGetSortedDirList();
            Loc.LoadLocalDats();
            Loc.SortLocalDats();
            Ftp.AddNewFtpDats();
            Ftp.UploadDatsInList();
            Ftp.DownloadDatsInList();
            Ftp.RenameFile(Ftp.ServerPath + "/FtpBusy.txt", "/" + Ftp.DatDir + "/FtpReady.txt");
        }
    }
}

