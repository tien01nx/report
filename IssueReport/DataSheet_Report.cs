using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace QualityControlSystem_Ver2._0.Job
{
    public partial class DataSheet_Report : DevExpress.XtraReports.UI.XtraReport
    {
        public DataSheet_Report()
        {
            InitializeComponent();
        }

        private void pageFooterBand1_BeforePrint(object sender, CancelEventArgs e)
        {
        }
    }
}
