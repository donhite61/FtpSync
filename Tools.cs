using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace FTPSync
{
    static class Tools
    {
        public static void Report(string msg)
        {
            Console.WriteLine(msg);
        }

        public static List<string> ReadFileToList(string filePath)
        {
            var aReadFile = File.ReadAllLines(filePath);
            var lineList = new List<string>(aReadFile);
            return lineList;
        }

        public static void LoadGVfromINIfile()
        {
            Report("Loading GV");
            Loc.DatDir = "";
            FTP.DatDir = "";
            FTP.ServerIp = "";
            FTP.UserName = "";
            FTP.ServerIp = "";

            string section = "";
            string scriptpath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string iniPath = scriptpath + "\\Hite Order Tracking Config.ini";

            var lineList = ReadFileToList(iniPath);

            foreach (string line in lineList)
            {
                int endBrack = line.IndexOf("]");
                if (endBrack != -1)
                    section = line.Substring(1, endBrack - 1);

                if (section == "General")
                {
                    var split = line.Split('=');
                    if (split[0] == "PathWorkingData")
                        Loc.DatDir = split[1];
                }

                if (section == "WebFTP")
                {
                    var split = line.Split('=');
                    if (split[0] == "FTPServerIp")
                        FTP.ServerIp = split[1];
                    if (split[0] == "FTPUserName")
                        FTP.UserName = split[1];
                    if (split[0] == "FTPPassWord")
                        FTP.PassWord = split[1];
                    if (split[0] == "FTPDatDir")
                        FTP.DatDir = split[1].Substring(12);
                }
            }

            if (Loc.DatDir == "" ||
                FTP.DatDir == "" ||
                FTP.ServerIp == "" ||
                FTP.UserName == "" ||
                FTP.ServerIp == "")
            {
                Report("Error reading INI");
            }
            //ShowGV();
        }
    }
}

