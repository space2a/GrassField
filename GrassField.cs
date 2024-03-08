using System;
using System.Collections.Generic;
using System.Threading;

namespace GrassField
{
    public class GrassFieldTaskManager
    {
        public List<GFProcess> TreeStructureProcesses { get; private set; }
        public List<GFProcess> FlatProcesses { get; private set; }

        public event EventHandler GrassFieldReload;

        public bool IncludeProcessMetrics { get; set; } = true;

        /// <summary>
        /// In MS
        /// </summary>
        public int RefreshInterval = 1000;

        private Thread _workingThread;
        private bool _AreMetricsAvailable = false;

        public void Start()
        {
            if (_workingThread != null) throw new System.Exception("This GrassField is already started.");
            _workingThread = new Thread(() => GFWork());
            _workingThread.Start();
        }

        public void Stop()
        {
            if (_workingThread == null) throw new System.Exception("This GrassField is already stopped.");
            _workingThread = null;
        }

        private void GFWork()
        {
            while (_workingThread != null)
            {
                DateTime start = DateTime.Now;

                FlatProcesses = WMI.GetProcesses(FlatProcesses, IncludeProcessMetrics);
                TreeStructureProcesses = OrganizeProcessesTreeStructure(FlatProcesses);

                GrassFieldReload?.Invoke(this, null);
                int wait = RefreshInterval - (int)(DateTime.Now - start).TotalMilliseconds;
                if (wait < 1) wait = 1;
                Thread.Sleep(wait);
                Console.WriteLine("Working after " + ((int)(DateTime.Now - start).TotalMilliseconds )+ "ms");
            }
        }

        private List<GFProcess> OrganizeProcessesTreeStructure(List<GFProcess> flatGFProcesses)
        {
            List<GFProcess> dest = new List<GFProcess>();
            foreach (var item in flatGFProcesses)
                dest.Add(item);

            DateTime dateTime = DateTime.Now;
            List<GFProcess> keysToRemove = new List<GFProcess>();
            for (int i = 0; i < dest.Count; i++)
            {
                int ind = dest.FindIndex(x => x.ProcessID == dest[i].PPID && x.ProcessName != "explorer.exe");

                if (ind != -1)
                {
                    dest[ind].SubProcesses.Add(dest[i]);
                    keysToRemove.Add(dest[i]);
                    continue;
                }
            }

            //Removing unwanted nodes
            for (int i = 0; i < keysToRemove.Count; i++)
                dest.Remove(keysToRemove[i]);

            keysToRemove.Clear();

            //Console.BackgroundColor = ConsoleColor.Blue;
            //Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS final");
            //Console.ResetColor();

            return dest;
        }

        private List<GFProcess> TreeStructureToFlat(List<GFProcess> processes)
        {
            if (processes == null) return null;
            List<GFProcess> FLAT = new List<GFProcess>();

            foreach (var process in processes)
            {
                FLAT.Add(process);
                FLAT.AddRange(TreeStructureToFlat(process.SubProcesses));
            }
            return FLAT;
        }

    }

   
}