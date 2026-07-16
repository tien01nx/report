using QualityControlSystem_Ver2._0.Base;
using QualityControlSystem_Ver2._0.QUALITY_CONTROL_SYS_4_0;
using QualityControlSystem_Ver2._0.Utils;
using System;
using System.Windows.Forms;

namespace QualityControlSystem_Ver2._0.Document
{
    public partial class FrmCheckSheetData : FrmBase
    {
        public FrmCheckSheetData()
        {
            InitializeComponent();
            gridview = myGridView2;
            colattdatafile.ColumnEdit = colattsoftfile.ColumnEdit = colatthardfile.ColumnEdit = reposUploadFile;
            PhanTrang(xpCCheckData, labelControl1);
        }

        public override void OnSetPermission()
        {
            base.OnSetPermission();
            PrintCaption = "Danh sách check sheet ngoại hình";
            Printer = myGridView1;
        }

        protected override void OnNew()
        {
            //base.OnNew();
        }

        protected override void OnReload()
        {
            base.OnReload();
            xpCCheckData.Reload();
            PhanTrang(xpCCheckData, labelControl1);
        }


        private void simpleButton2_Click(object sender, EventArgs e)
        {
            clicklui(labelControl1);
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            clicktien(labelControl1);
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            pageSize = (int)spinEdit1.Value;
            PhanTrang(xpCCheckData, labelControl1);
        }
    }
}
