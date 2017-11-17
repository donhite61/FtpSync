using System.Collections.Generic;
using System.Net;
using System.IO;
using System;

namespace FTPSync
{
    class Ftp
    {
        public static string DatDir { get; set; }
        public static string AchDir { get; set; }
        public static string ServerIp { get; set; }
        public static string ServerPath { get; set; }
        public static string UserName { get; set; }
        public static string PassWord { get; set; }
        public static bool FtpReady { get; set; }
        public static SortedList<string, Dat> DatsList { get; set; }
        public static List<Dat> DatsDownload { get; set; }
        public static List<Dat> DatsUpload { get; set; }

        internal static void FtpGetSortedDirList()
        {
            Tools.Log("Start FtpGetSortedDirList");
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

                        var dat = new Dat()
                        {
                            OrderNum = splitExt[0],
                            FtpNameTime = splitTime[0]
                        };
                        if (DatsList.TryGetValue(dat.OrderNum, out Dat matchingFtpDat)) //check for duplicate with diff name
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
            Tools.Log("Start FindOldDatandDelete, dup found " +
                matchingFtpDat.FtpNameTime + "_" + matchingFtpDat.OrderNum + ".dat" + " " +
                dat.FtpNameTime + "_" + dat.OrderNum + ".dat");
                
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
            Tools.Log("Start DownloadDatsInList");
            foreach (var dat in DatsDownload)
            {
                string locDatpath = Loc.DatDir + "\\" + dat.OrderNum + ".dat";
                FtpDownloadFile(locDatpath, dat.FtpNameTime + "_" + dat.OrderNum + ".dat");
            }
        }
        internal static bool AddNewFtpDats()
        {
            Tools.Log("Start AddNewFtpDats");
            if (DatsList.Count > 0)
            {
                foreach (var dat in Ftp.DatsList) // all found local dats have been deleted from locdatlist
                {
                    Tools.Log(dat.Key + "added to datsDownloadList");
                    DatsDownload.Add(dat.Value);
                }
            }
            return true;
        }
        internal static void UploadDatsInList()
        {
            Tools.Log("Start UploadDatsInList");
            foreach (var dat in DatsUpload)
            {
                Loc.AddDatSize(dat);

                FtpUploadFile(dat.locFileName, dat.FileModDate + "_" + dat.OrderNum + ".dat");
                if (dat.FtpNameTime != null)
                    FtpDeleteFile(dat.FtpNameTime + "_" + dat.OrderNum + ".dat");
            }
        }
        public static bool WaitForFtpReady()
        {
            Tools.Log("Start WaitForFtpReady");
            var ftpRdy = false;
            var ftpBsy = false;
            for (var i = 1; i < 50; i++)
            {
                ftpBsy = Ftp.DownloadString("FtpBusy.txt");
                ftpRdy = Ftp.DownloadString("FtpReady.txt");

                if (ftpRdy == true && ftpBsy == false)
                {
                    return true;
                }
                else if (ftpRdy == true && ftpBsy == true)
                {
                    Tools.Report("FtpSync WaitForReady found both 'ready' & 'busy'" , new Exception());
                    FtpDeleteFile("FtpBusy.txt");
                }
                else if (ftpRdy == false && ftpBsy == false)
                {
                    Tools.Report("FtpSync WaitForReady did not find 'ready' or 'busy'", new Exception());
                    Ftp.UploadString("FtpReady.txt");
                }
                else
                {
                    System.Threading.Thread.Sleep(3000);
                }
                if(i==48)
                {
                    Tools.Report("FtpSync WaitForReady timed out waiting, setting as 'ready'", new Exception());
                    Ftp.UploadString("FtpReady.txt");
                    FtpDeleteFile("FtpBusy.txt");
                }
            }
            Tools.Report("FtpSync error making ready, exited", new Exception());
            return false;
        }

        public static bool FtpUploadFile(string locFilePath, string ftpFileName)
        {
            Tools.Log("Start FtpUploadFile of "+ ftpFileName);
            var client = new WebClient()
            {
                Credentials = new NetworkCredential(UserName, PassWord),
                BaseAddress = ServerPath
            };
            try
            {
                client.UploadFile(ftpFileName, locFilePath);
            }
            catch (WebException)
            {
                System.Threading.Thread.Sleep(2000);
                try
                {
                    client.UploadFile(ftpFileName, locFilePath);
                }
                catch (WebException e)
                {
                    Tools.Report("FtpSync error uploading " + ftpFileName, e);
                    return false;
                }
            }
            return true;
        }
        public static bool FtpDownloadFile(string locFilePath, string ftpFileName)
        {
            Tools.Log("Start FtpDownloadFile of " + ftpFileName);
            string tmpFilePath = "";
            if (File.Exists(locFilePath)) // make backup of local copy in case of download fail
            {
                tmpFilePath = locFilePath.Substring(0, locFilePath.Length - 4) + ".tmp";
                if (File.Exists(tmpFilePath))
                    File.Delete(tmpFilePath);

                File.Move(locFilePath, tmpFilePath);
            }

            var client = new WebClient()
            {
                Credentials = new NetworkCredential(UserName, PassWord)
            };
            try
            {
                client.DownloadFile(ServerPath + ftpFileName, locFilePath);
                if (File.Exists(tmpFilePath))
                    File.Delete(tmpFilePath);
            }
            catch (WebException)
            {
                System.Threading.Thread.Sleep(2000);
                try
                {
                    client.DownloadFile(ServerPath + ftpFileName, locFilePath);
                    if (File.Exists(tmpFilePath))
                        File.Delete(tmpFilePath);
                }
                catch (WebException e)
                {
                    if (File.Exists(tmpFilePath))
                        File.Move(tmpFilePath, locFilePath); // restore original file

                    Tools.Report("FtpSync error downloading " + ftpFileName, e);
                    return false;
                }
            }
            return true;
        }
        public static bool RenameFile(string oldFName, string newFName)
        {
            Tools.Log("Start RenameFile from " + oldFName+" to "+newFName);
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(oldFName);
            ftpRequest.Method = WebRequestMethods.Ftp.Rename;
            ftpRequest.Proxy = null;
            ftpRequest.Credentials = new NetworkCredential(UserName, PassWord);
            ftpRequest.Method = WebRequestMethods.Ftp.Rename;
            ftpRequest.RenameTo =  newFName;
            try
            {
                ftpRequest.GetResponse() ;
            }
            catch (WebException)
            {
                System.Threading.Thread.Sleep(2000);
                try
                {
                    ftpRequest.GetResponse();
                }
                catch (WebException e)
                {
                    Tools.Report("FtpSync error renaming " + oldFName +" to "+ newFName, e);
                    return false;
                }
            }
            return true;
        }
        public static bool FtpDeleteFile(string ftpFileName)
        {
            Tools.Log("Start FtpDeleteFile of " + ftpFileName);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ServerPath + ftpFileName);
            var time = request.Timeout;
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(UserName, PassWord);

            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            }
            catch (WebException)
            {
                System.Threading.Thread.Sleep(2000);
                try
                {
                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                }
                catch (WebException e)
                {
                    Tools.Report("FtpSync error deleting " + ftpFileName, e);
                    return false;
                }
            }
            return true;
        }

        public static bool DownloadString(string ftpFileName) //used to check for "FtpReady.txt" and "FtpBusy.txt"
        {
            Tools.Log("Start DownloadString of " + ftpFileName);
            WebClient client = new WebClient()
            {
                Credentials = new NetworkCredential(UserName, PassWord)
            };
            try
            {
                client.DownloadString(ServerPath + ftpFileName);
                return true;
            }
            catch (WebException)
            {
                System.Threading.Thread.Sleep(2000);
                try
                {
                    client.DownloadString(ServerPath + ftpFileName);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            }
        }

        public static bool UploadString(string ftpFileName) //used to set "FtpReady" and "FtpBusy.txt"
        {
            Tools.Log("Start UploadString of " + ftpFileName);
            WebClient client = new WebClient()
            {
                Credentials = new NetworkCredential(UserName, PassWord)
            };
            try
            {
                client.UploadString(ServerPath + ftpFileName, "");
            }
            catch (WebException)
            {
                System.Threading.Thread.Sleep(1000);
                try
                {
                    client.UploadString(ServerPath + ftpFileName, "");
                }
                catch (WebException e)
                {
                    Tools.Report("FtpSync error uploading " + ftpFileName, e);
                    return false;
                }
            }
            return true;
        }
    }
}
