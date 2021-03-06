﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoalsAndCornersPredictions
{
    public class RExecutor
    {
        private static readonly log4net.ILog log
          = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public RExecutor(String workingDirectory)
        {
            log.Debug("Running process in directory: " + workingDirectory);
            //TODO: either use PATH env. or configurable full path
            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = GlobalData.Instance.RexecutableFullPath;
            //si.Arguments = "CMD BATCH " + GlobalData.Instance.ScriptFullPath;
            si.Arguments = @"CMD BATCH C:\Users\daddy\Documents\GitHub\GoalsAndCornersPredictions\GoalsAndCornersPredictions\script.R";
            si.WorkingDirectory = workingDirectory;
            si.UseShellExecute = true;
            si.CreateNoWindow = true;

            try
            {
                int maxRWaitTime = 1000;
                using (Process p = new Process())
                {
                    p.StartInfo = si;
                    p.Exited += processExited;
                    p.EnableRaisingEvents = true;

                    if (p.Start())
                    {
                        p.PriorityClass = ProcessPriorityClass.AboveNormal;
                    }

                    int waitedTime = 0;

                    while (p.HasExited == false || waitedTime < maxRWaitTime)
                    {
                        log.Debug("Waiting " + waitedTime + " seconds for R process to finish");

                        System.Threading.Thread.Sleep(5000);

                        waitedTime += 5;

                        var rProcesses = Process.GetProcesses().ToArray().ToList().Select(x => x.MainWindowTitle);

                        if (rProcesses.Any(y => y.Equals("C:\\Program Files\\R\\R-3.0.3\\bin\\x64\\R.exe")) == false)
                        {
                            log.Warn("Looks like R has crashed, exitting...");
                            break;
                        }
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                log.Error("Error executing process exception: " + e);
            }
            catch (Exception e)
            {
                log.Error("Error executing process exception: " + e);
            }
        }

        void processExited(object sender, EventArgs e)
        {
            log.Info("R has exitted");
        }
    };
}
