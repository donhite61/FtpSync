using System.Collections.Generic;
using System.Net;
using System.IO;
using System;

namespace FTPSync
{
    public class FTP
    {
        public static string DatDir { get; set; }
        public static string ServerIp { get; set; }
        public static string UserName { get; set; }
        public static string PassWord { get; set; }
        public static SortedList<string, Dat> DatsList { get; set; }
        public static List<Dat> datsDownload { get; set; }

        internal static bool AddNewFtpDats()
        {
            Tools.Report("Adding  " + DatsList.Count + " FTP files to download list");
            if (FTP.DatsList.Count > 0)
            {
                foreach (var dat in FTP.DatsList) // all found local dats have been deleted from ftpdatlist
                {
                    FTP.datsDownload.Add(dat.Value);
                }
            }
            return true;
        }

        internal static bool CheckFtpReady()
        {
            string ftpPath = "ftp://" + ServerIp + DatDir + "/FTPReady.txt";
            var request = (FtpWebRequest)WebRequest.Create(ftpPath);
            request.Credentials = new NetworkCredential(UserName, PassWord);
            request.Method = WebRequestMethods.Ftp.GetFileSize;

            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                return true;
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    return false;
            }
            return false;
        }

        internal static void RenameFtpFile(string oldName, string newName)
        {

        }
        internal static void DoDownloads()
        {
            Tools.Report("Downloading " + datsDownload.Count + " files");
            using (WebClient ftpClient = new WebClient())
            {
                ftpClient.Credentials = new System.Net.NetworkCredential(UserName, PassWord);

                foreach (var dat in datsDownload)
                {
                    string ftpPath = "ftp://" + ServerIp + DatDir + "/" + dat.FtpNameTime + "_" + dat.OrderNum + ".dat";
                    string locDatpath = Loc.DatDir+"\\"+dat.OrderNum+".dat";
                    string tmpDatpath = Loc.DatDir + "\\" + dat.OrderNum + ".old";

                    if (File.Exists(locDatpath))
                    {
                        File.Move(locDatpath, tmpDatpath);
                    }
                    try
                    {
                        ftpClient.DownloadFile(ftpPath, locDatpath);
                        File.Delete(tmpDatpath);
                    }
                    catch
                    {
                        File.Move(tmpDatpath, locDatpath);
                    }
                }
            }
        }

        internal static void LoadFtpDats()
        {
            for(int i=0; i<10; i++)
            {
                if (CheckFtpReady())
                    break;

                System.Threading.Thread.Sleep(3000);
            }

            Tools.Report("Loading FTP file list ");
            string ftpPath = "ftp://" + ServerIp + DatDir+"/";

            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ftpPath);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            ftpRequest.Credentials = new NetworkCredential(UserName, PassWord);
            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();

            DatsList = new SortedList<string, Dat>();
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var splitTime = line.Split('_');
                    if (splitTime.Length <2)
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
                    if (FTP.DatsList.TryGetValue(dat.OrderNum, out matchingFtpDat))
                    {
                        DatsList.Remove(matchingFtpDat.OrderNum);
                        dat = FindOldDatandDelete(matchingFtpDat, dat);
                    }

                    FTP.DatsList.Add(dat.OrderNum, dat); // Add to list.
                }
            }
        }

        public static Dat FindOldDatandDelete(Dat matchingFtpDat, Dat dat)
        {
            int timediff = string.Compare(matchingFtpDat.FtpNameTime, dat.FtpNameTime);
            if (timediff < 0)
            {
                FTP.DeleteFtpDat(matchingFtpDat);
                return dat;
            }
            else
            {
                FTP.DeleteFtpDat(dat);
                return matchingFtpDat;
            }
        }

        public static void DeleteFtpDat(Dat dat)
        {
            string ftpPath = "ftp://" + FTP.ServerIp + FTP.DatDir + "/" + dat.FtpNameTime + "_" + dat.OrderNum + ".dat";
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpPath);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(FTP.UserName, FTP.PassWord);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                //return response.StatusDescription;
            }
        }
    }
}

