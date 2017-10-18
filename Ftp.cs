using System.Collections.Generic;
using System.Net;
using System.IO;
using System;

namespace FTPSync
{
    class Ftp
    {
        public static string DatDir { get; set; }
        public static string ServerPath { get; set; }
        public static string UserName { get; set; }
        public static string PassWord { get; set; }
        public static bool FtpReady { get; set; }
        public static SortedList<string, Dat> DatsList { get; set; }
        public static List<Dat> datsDownload { get; set; }
        public static List<Dat> datsUpload { get; set; }

        internal static void FtpGetSortedDirList()
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ServerPath);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            ftpRequest.Credentials = new NetworkCredential(UserName, PassWord);
            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();

            DatsList = new SortedList<string, Dat>();
            try
            {
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var splitTime = line.Split('_');
                        if (splitTime.Length < 2)
                            continue;

                        if (splitTime[0].Length != 14)
                            continue;

                        var splitExt = splitTime[1].Split('.');
                        if (splitTime.Length < 2)
                            continue;

                        if (splitExt[1] != "dat")
                            continue;

                        Dat dat = new Dat();
                        dat.OrderNum = splitExt[0];
                        dat.FtpNameTime = splitTime[0];

                        Dat matchingFtpDat = null;
                        if (DatsList.TryGetValue(dat.OrderNum, out matchingFtpDat))
                        {
                            DatsList.Remove(matchingFtpDat.OrderNum);
                            dat = FindOldDatandDelete(matchingFtpDat, dat);
                        }

                        DatsList.Add(dat.OrderNum, dat); // Add to list.
                    }
                }
            }
            catch (WebException e)
            {
                System.Windows.Forms.MessageBox.Show("FtpSync retrieve dir error" + "\n" + "\n" + e.ToString());
            }
        }
        public static Dat FindOldDatandDelete(Dat matchingFtpDat, Dat dat)
        {
            int timediff = string.Compare(matchingFtpDat.FtpNameTime, dat.FtpNameTime);
            if (timediff < 0)
            {
                FtpDeleteFile(matchingFtpDat.FtpNameTime + "_" + matchingFtpDat.OrderNum + ".dat");
                return dat;
            }
            else
            {
                FtpDeleteFile(dat.FtpNameTime+"_"+dat.OrderNum+".dat");
                return matchingFtpDat;
            }
        }
        internal static void DownloadDatsInList()
        {
            foreach (var dat in datsDownload)
            {
                string locDatpath = Loc.DatDir + "\\" + dat.OrderNum + ".dat";
                FtpDownloadFile(locDatpath, dat.FtpNameTime + "_" + dat.OrderNum + ".dat");
            }
        }
        internal static bool AddNewFtpDats()
        {
            if (DatsList.Count > 0)
            {
                foreach (var dat in DatsList) // all found local dats have been deleted from ftpdatlist
                {
                    datsDownload.Add(dat.Value);
                }
            }
            return true;
        }
        internal static void UploadDatsInList()
        {
            foreach (var dat in datsUpload)
            {
                Loc.AddDatSize(dat);

                FtpUploadFile(dat.locFileName, dat.FileModDate + "_" + dat.OrderNum + ".dat");
                if (dat.FtpNameTime != null)
                    FtpDeleteFile(dat.FtpNameTime + "_" + dat.OrderNum + ".dat");
            }
        }
        public static bool WaitForFtpReady()
        {
            int count = 0;
            while (!Ftp.DownloadString("FtpReady.txt")) // if ftp is busy
            {
                for (var i = 0; i < 21; i++)
                {
                    if (!Ftp.DownloadString("FtpBusy.txt"))
                        break;

                    System.Threading.Thread.Sleep(3000);
                }
                Ftp.UploadString("FtpReady.txt");
                if (count > 100)
                    Environment.Exit(0);
                count += 1;
            }
            return true;
        }

        public static bool FtpUploadFile(string locFilePath, string ftpFileName)
        {
            var client = new WebClient();
            client.Credentials = new NetworkCredential(UserName, PassWord);
            client.BaseAddress = ServerPath;
            
            try
            {
                client.UploadFile(ftpFileName, locFilePath); //since the baseaddress
            }
            catch (WebException e)
            {
                System.Windows.Forms.MessageBox.Show("FtpSync error uploading " + ftpFileName + "\n"+"\n"+ e.ToString());
                return false;
            }
            return true;
        }
        public static bool FtpDownloadFile(string locFilePath, string ftpFileName)
        {
            string tmpFilePath = "";
            if (File.Exists(locFilePath))
            {
                tmpFilePath = locFilePath.Substring(0, locFilePath.Length - 4) + ".tmp";
                File.Move(locFilePath, tmpFilePath);
            }

            var client = new WebClient();
            client.Credentials = new NetworkCredential(UserName, PassWord);
            try
            {
                client.DownloadFile(ServerPath + ftpFileName, locFilePath);
                if (tmpFilePath != "")
                    File.Delete(tmpFilePath);
            }
            catch (WebException e)
            {
                if(tmpFilePath != "")
                    File.Move(tmpFilePath, locFilePath);
                if(ftpFileName != "FtpReady.txt")
                    System.Windows.Forms.MessageBox.Show("FtpSync error downloading " + ftpFileName + "\n"+"\n" + e.ToString());
                return false;
            }
            return true;
        }
        public static bool UploadString(string ftpFileName) //used to check for "FtpReady.txt"
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential(UserName, PassWord);
            try
            {
                client.UploadString(ServerPath + ftpFileName, "");
                return true;
            }
            catch (WebException )
            {
                return false;
            }
        }
        public static bool DownloadString(string ftpFileName) //used to check for "FtpReady.txt"
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential(UserName, PassWord);
            try
            {
                client.DownloadString(ServerPath + ftpFileName);
                return true;
            }
            catch (WebException )
            {
                return false;
            }
        }
        public static bool RenameFile(string oldFName, string newFName)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ServerPath + "/"+ oldFName);
            ftpRequest.Method = WebRequestMethods.Ftp.Rename;
            ftpRequest.Proxy = null;
            ftpRequest.Credentials = new NetworkCredential(UserName, PassWord);

            ftpRequest.Method = WebRequestMethods.Ftp.Rename;
            ftpRequest.RenameTo = "/" + DatDir + "/" + newFName;
            try
            {
                ftpRequest.GetResponse();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool FtpDeleteFile(string ftpFileName)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ServerPath + ftpFileName);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(UserName, PassWord);

            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                    return true;
            }
            catch (WebException e)
            {
                System.Windows.Forms.MessageBox.Show("FtpSync error deleting " + ftpFileName + "\n" + "\n" + e.ToString());
                return false;
            }
        }
    }
}
