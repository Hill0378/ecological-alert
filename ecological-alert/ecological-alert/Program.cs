﻿using ESRI.ArcGIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ecological_alert
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!RuntimeManager.Bind(ProductCode.Engine))
            {
                if (!RuntimeManager.Bind(ProductCode.Desktop))
                {
                    MessageBox.Show("Unable to bind to ArcGIS runtime. Application will be shut down.");
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Mainform());
        }
    }
}
