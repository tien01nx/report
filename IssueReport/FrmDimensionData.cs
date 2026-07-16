using QualityControlSystem_Ver2._0.Base;
using QualityControlSystem_Ver2._0.QUALITY_CONTROL_SYS_4_0;
using QualityControlSystem_Ver2._0.Utils;
using System;
using System.Windows.Forms;

namespace QualityControlSystem_Ver2._0.Document
{
    public partial class FrmDimensionData : FrmBase
    {
        public FrmDimensionData()
        {
            InitializeComponent();
            gridview = myGridView2;
            //Set permission
            colattdatafile.ColumnEdit = colattsoftfile.ColumnEdit = colatthardfile.ColumnEdit = reposUploadFile;
            PhanTrang(xpCDimensionData, labelControl1);
        }

        tb_Issue_Dimension itemcurrent;

        public override void OnSetPermission()
        {
            Printer = myGridView1;
            gridview = myGridView2;
            PrintCaption = "DATA LIST";
            base.OnSetPermission();
        }
        protected override void OnReload()
        {
            base.OnReload();
            xpCDimensionData.Reload();
            PhanTrang(xpCDimensionData, labelControl1);
        }
        protected override void gridview_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            base.gridview_FocusedRowChanged(sender, e);
            itemcurrent = item as tb_Issue_Dimension;
        }
    
        protected override void OnNew()
        {
            tb_Issue_Dimension item = new tb_Issue_Dimension(UOW);
            item.iddo = 0;
            item.nguoiattach = FrmMAIN.userName;
            item.ngayupdate = DateTime.Now;
            item.ngaysx = DateTime.Now;
            base.Save();
            xpCDimensionData.Reload();
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            clicktien(labelControl1);
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            clicklui(labelControl1);
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            pageSize =(int) spinEdit1.Value;
            PhanTrang(xpCDimensionData, labelControl1);
        }
    }
}
