using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Xml;
using System.Collections;

namespace vboxservice
{
    public partial class Service1 : ServiceBase
    {
        private string vboxdir;
        private const string vboxreg = "SOFTWARE\\VBoxService";
//        StreamWriter logfile = new StreamWriter("c:\\vboxservice.log", true);
        StreamWriter logfile = null;
        private Process vboxsvc = null;
        
        public struct VboxProc
        {
            public Process process;
            public string uuid;
            public string shutdownmethod;
            public string name;
        };

        private List<VboxProc> vboxprocs = new List<VboxProc>();
        
        public Service1()
        {
            InitializeComponent();
        }

        private void vboxsvcstart()
        {
            //Process vboxsvc = new Process();
            if (vboxsvc == null || vboxsvc.HasExited)
            {
                vboxsvc = new Process();
                vboxsvc.StartInfo.FileName = vboxdir + "VBoxSVC.exe";
                //vboxsvc.StartInfo.Arguments = "--automate";
                vboxsvc.StartInfo.CreateNoWindow = true;
                vboxsvc.StartInfo.UseShellExecute = false;
                vboxsvc.Start();
                log("Starting: VBoxSVC.exe");
            }
        }

        private void killrunningvboxsvc()
        {
            foreach (Process proc in Process.GetProcesses())
            {
                if (proc.ProcessName.ToLower().Contains("vboxsvc"))
                {
                    proc.WaitForExit(10000);
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                        proc.WaitForExit();
                    }
                }
            }
        }


        private void log(string logstr)
        {
            DateTime dt = DateTime.Now;

            if (logfile != null)
            {
                logfile.WriteLine(dt.ToString("u") + " " + logstr);
                logfile.Flush();
            }
        }

        private void vboxtostart()
        {
            List<String> vm_config_files = new List<String>();

            Process vboxmanage = new Process();
            vboxmanage.StartInfo.FileName = vboxdir + "VBoxManage.exe";
            //vboxmanage.StartInfo.Arguments = "controlvm " + vboxproc.UUID + " " + vboxproc.Shutdown;
            vboxmanage.StartInfo.CreateNoWindow = true;
            vboxmanage.StartInfo.UseShellExecute = false;
            vboxmanage.StartInfo.RedirectStandardOutput = true;
            vboxmanage.StartInfo.RedirectStandardError = true;


            vboxmanage.StartInfo.Arguments = "list vms";
            vboxmanage.Start();
            log("Starting: VBoxManage.exe");

            while (!vboxmanage.StandardOutput.EndOfStream)
            {
                String[] line = vboxmanage.StandardOutput.ReadLine().Split(new char[] { ':' }, 2);
                if (line.Length == 2 && line[0].Equals("Config file"))
                {
                    vm_config_files.Add(line[1].TrimStart());
                }
            }

            vboxmanage.WaitForExit();
            // VBoxSVC.exe has to exit so we can actually read the xml file do to locking.
            if (vboxsvc != null)
            {
                vboxsvc.WaitForExit();
            }
            else
            {
                killrunningvboxsvc();
            }

            foreach (string config_file in vm_config_files)
            {
                Dictionary<string, string> config = new Dictionary<string, string>();

                XmlTextReader config_xml = new XmlTextReader(config_file);

                while (config_xml.Read())
                {
                    config_xml.MoveToElement();
                    if (config_xml.Name.Equals("ExtraDataItem"))
                    {
                        string name, value;
                        name = config_xml.GetAttribute("name");
                        value = config_xml.GetAttribute("value");

                        if (name != null & value != null)
                        {
                            config[name] = value;
                        }
                    }
                    if (config_xml.Name.Equals("Machine") && config_xml.NodeType == XmlNodeType.Element)
                    {
                        string name, uuid;
                        name = config_xml.GetAttribute("name");
                        uuid = config_xml.GetAttribute("uuid");

                        config["machine_name"] = name;
                        config["machine_uuid"] = uuid.Trim(new char[] { '{' , '}' });
                    }
                }
                config_xml.Close();

                if (config.ContainsKey("SERVICE/AutoStart") && config["SERVICE/AutoStart"].ToLower().Equals("on"))
                {
                    VboxProc vboxproc = new VboxProc();
                    vboxproc.process = new Process();
                    vboxproc.uuid = config["machine_uuid"];
                    vboxproc.shutdownmethod = config.ContainsKey("SERVICE/ShutdownMethod") ? config["SERVICE/ShutdownMethod"] : "savestate";
                    vboxproc.name = config["machine_name"];
                    vboxprocs.Add(vboxproc);
                }

            }
        }


        protected override void OnStart(string[] args)
        {
            /*while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
            }*/
            string logfilename = null;
            logfilename = (string) Registry.LocalMachine.OpenSubKey(vboxreg).GetValue("Logfile", null);
            if (logfilename != null)
            {
                logfile = new StreamWriter(logfilename, true);
            }

            log("Service Starting");
            
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Sun\\xVM Virtualbox");

            if (regkey == null)
            {
                Environment.Exit(1);
            }

            vboxdir = (string)regkey.GetValue("InstallDir");

            //vboxsvcstart();

            vboxtostart();

            //vboxsvcstart();
            
            foreach (VboxProc vboxproc in vboxprocs)
            {
                vboxproc.process.StartInfo.FileName = vboxdir + "VBoxHeadless.exe";
                vboxproc.process.StartInfo.Arguments = "-s " + vboxproc.uuid + " -v config";

                vboxproc.process.StartInfo.CreateNoWindow = true;
                vboxproc.process.StartInfo.UseShellExecute = false;
                vboxproc.process.StartInfo.RedirectStandardOutput = true;
                vboxproc.process.StartInfo.RedirectStandardError = true;

                vboxproc.process.Start();
                log("Starting Machine: " + vboxproc.name);
            }
        }

        protected override void OnStop()
        {
            foreach (VboxProc vboxproc in vboxprocs)
            {
                while (!vboxproc.process.HasExited)
                {
                    Process vboxmanage = new Process();
                    vboxmanage.StartInfo.FileName = vboxdir + "VBoxManage.exe";
                    vboxmanage.StartInfo.Arguments = "controlvm " + vboxproc.uuid + " " + vboxproc.shutdownmethod;
                    vboxmanage.StartInfo.CreateNoWindow = true;
                    vboxmanage.StartInfo.UseShellExecute = false;
                    vboxmanage.StartInfo.RedirectStandardOutput = true;
                    vboxmanage.StartInfo.RedirectStandardError = true;
                    
                    vboxmanage.Start();
                    log("Stopping Machine: " + vboxproc.name + " - " + vboxproc.shutdownmethod);
                    vboxmanage.WaitForExit();

                    vboxproc.process.WaitForExit(30000);
                }
                log(vboxproc.process.StandardOutput.ReadToEnd());
            }
            if (vboxsvc != null)
            {
                vboxsvc.Kill();
                vboxsvc.WaitForExit();
            } else {
                killrunningvboxsvc();
            }
            log("Service Stopped");
            if (logfile != null)
            {
                logfile.Close();
            }
        }
    }
}
