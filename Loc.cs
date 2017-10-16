using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace FTPSync
{
    public class Loc
    {
        public static string DatDir { get; set; }
        public static List<Dat> DatList { get; set; }
        public static List<Dat> datsUpload { get; set; }

        internal static void DoUploads()
        {
            Tools.Report("Uploading "+Loc.datsUpload.Count+" files");
            using (WebClient ftpClient = new WebClient())
            {
                ftpClient.Credentials = new System.Net.NetworkCredential(FTP.UserName, FTP.PassWord);

                foreach (var dat in datsUpload)
                {
                    string ftpPath = "ftp://" + FTP.ServerIp + FTP.DatDir + "/" + dat.FileModDate + "_" + dat.OrderNum + ".dat";
                    string locpath = DatDir + "\\" + dat.OrderNum + ".dat";
                    ftpClient.UploadFile(ftpPath, locpath);
                }
            }

            Tools.Report("Deleting " + Loc.datsUpload.Count + " files");
            foreach (var dat in datsUpload)
            {
                if (dat.FtpNameTime != null)
                    FTP.DeleteFtpDat(dat);
            }
        }



        internal static void LoadLocalDats()
        {
            Tools.Report("loading local dats");
            Loc.DatList = new List<Dat>();
            string[] array2 = Directory.GetFiles(DatDir);
            foreach (string file in array2)
            {
                var lineList = Tools.ReadFileToList(file);
                var dat = new Dat();
                dat = ReadInfoFromFile(lineList, file);
                if (dat == null)
                    continue;

                Loc.DatList.Add(dat);
            }
        }

        internal static bool SortLocalDats()
        {
            Tools.Report("Sorting " + DatList.Count + " local files");
            if (FTP.DatsList.Count < 1 && Loc.DatList.Count < 1)
                return false;

            Loc.datsUpload = new List<Dat>();
            FTP.datsDownload = new List<Dat>();

            if (Loc.DatList.Count > 0)
            {
                foreach (var dat in Loc.DatList)
                {
                    Dat matchingFtpDat = null;
                    if (FTP.DatsList.TryGetValue(dat.OrderNum, out matchingFtpDat))
                    {
                        int timediff = string.Compare(matchingFtpDat.FtpNameTime, dat.FileModDate);
                        if (timediff < 0)
                        {
                            dat.FtpNameTime = matchingFtpDat.FtpNameTime;
                            Loc.datsUpload.Add(dat);
                        }
                        else if (timediff > 0)
                        {
                            dat.FtpNameTime = matchingFtpDat.FtpNameTime;
                            FTP.datsDownload.Add(dat);
                        }
                        FTP.DatsList.Remove(dat.OrderNum);
                        continue;
                    }
                    Loc.datsUpload.Add(dat); // if not found on ftp
                }
            }
            return true;
        }

        private static Dat AddDatSize(Dat dat) // if file is to be uploaded
        {
            Tools.Report("Adding size to " + datsUpload.Count + " local files");
            dat.Actsize = new System.IO.FileInfo(dat.locFileName).Length;
            if(dat.RecSize != dat.Actsize)
            {
                dat.RecSize = dat.Actsize;
                if (dat.RecSize != -1)
                {
                    var lineList = Tools.ReadFileToList(dat.locFileName);
                    lineList.RemoveAt(lineList.Count - 1);

                    File.WriteAllLines(dat.locFileName, lineList);
                }

                using (StreamWriter w = File.AppendText(dat.locFileName))
                {
                    w.WriteLine("FileSize=" + dat.RecSize);
                }
            }
            
            return dat;
        }

        private static Dat ReadInfoFromFile(List<string> lineList, string file)
        {
            //Tools.Report("Reading info from  " + DatList.Count + " local files");
            var dat = new Dat();
            bool fileSizeFound = false;
            foreach (var line in lineList)
            {
                var splitKey = line.Split('=');
                if (splitKey.Length < 2)
                    continue;
                switch (splitKey[0])
                {
                    case "Hite ID":
                        dat.OrderNum = splitKey[1];
                        break;
                    case "ModifiedTime":
                        dat.FileModDate = splitKey[1];
                        break;
                    case "FileSize":
                        fileSizeFound = true;
                        dat.RecSize = Convert.ToInt64(splitKey[1]);
                        break;
                }
            }

            if (dat.OrderNum == null || dat.FileModDate == null)
                return null;

            dat.locFileName = file;
            if (fileSizeFound == false)
                dat.RecSize = -1;

            return dat;
        }
    }
}

