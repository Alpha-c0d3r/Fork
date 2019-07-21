using System;

namespace nihilus.Logic.Model
{
    [Serializable]
    public class ServerJavaSettings
    {
        public int MaxRam { get; set; } = 2048;
        public int MinRam { get; set; } = 512;
        public string JavaPath { get; set; } = "java.exe";
        public string StartupParameters { get; set; } = "";
        
        public ServerJavaSettings(){}
    }
}