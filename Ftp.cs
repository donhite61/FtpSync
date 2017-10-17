using System.Collections.Generic;
using System.Net;
using System.IO;
using System;

namespace FTPSync
{
    class Ftp
    {
        public static string DatDir { get; set; }
        public static string ServerIp { get; set; }
        public static string UserName { get; set; }
        public static string PassWord { get; set; }
        public static bool FtpReady { get; set; }
        public static SortedList<string, Dat> DatsList { get; set; }
        public static List<Dat> datsDownload { get; set; }
        public static List<Dat> datsUpload { get; set; }

        internal static void FtpGetSortedDirList(string ftpPath = "")
        {
            if (!FtpReady) Tools.ReadIniFile();
            if (ftpPath == "")
                ftpPath = "ftp://"+ ServerIp +"/"+ DatDir +"/";

            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ftpPath);
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

        public static bool FtpUploadFile(string locFilePath, string ftpFileName, string ftpPath="")
        {
            if (!FtpReady) Tools.ReadIniFile();
            if (ftpPath == "")
                ftpPath = "ftp://" + ServerIp + "/" + DatDir + "/";

            var client = new WebClient();
            client.Credentials = new NetworkCredential(UserName, PassWord);
            client.BaseAddress = ftpPath;
            
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
        public static bool FtpDownloadFile(string locFilePath, string ftpFileName, string ftpPath = "")
        {
            if (!FtpReady) Tools.ReadIniFile();
            if (ftpPath == "")
                ftpPath = "ftp://" + ServerIp + "/" + DatDir + "/";

            string tmpFilePath = locFilePath.Substring(locFilePath.Length - 4) + ".tmp";
            if (File.Exists(locFilePath))
                File.Move(locFilePath, tmpFilePath);

            var client = new WebClient();
            client.Credentials = new NetworkCredential(UserName, PassWord);
            try
            {
                client.DownloadFile(ftpPath + ftpFileName, locFilePath); //since the baseaddress
                File.Delete(tmpFilePath);
            }
            catch (WebException e)
            {
                File.Move(tmpFilePath, locFilePath);
                System.Windows.Forms.MessageBox.Show("FtpSync error downloading " + ftpFileName + "\n"+"\n" + e.ToString());
                return false;
            }
            return true;
        }
        public static bool FtpDeleteFile(string ftpFileName, string ftpPath = "")
        {
            if (!FtpReady) Tools.ReadIniFile();
            if (ftpPath == "")
                ftpPath = "ftp://" + ServerIp + "/" + DatDir + "/";

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpPath + ftpFileName);
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
