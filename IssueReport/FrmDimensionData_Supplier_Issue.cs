using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;
using QUALITY_CONTROL_SYSTEM.Presentation.Do;
using QualityControlSystem_Ver2._0.Base;
using QualityControlSystem_Ver2._0.QUALITY_CONTROL_SYS_4_0;
using QualityControlSystem_Ver2._0.Job;
using QualityControlSystem_Ver2._0.Utils;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QualityControlSystem_Ver2._0.IssueReport
{
    public partial class FrmDimensionData_Supplier_Issue : FrmBase
    {
        public FrmDimensionData_Supplier_Issue()
        {
            InitializeComponent();
            gridview = myGridView2;
            gridview.BestFitColumns();

            textEdit1.Text = getKQDo("1");
            textEdit2.Text = getKQDo("2");
            textEdit3.Text = getKQDo("3");
            textEdit4.Text = getKQDo("4");
        }
        string cavity1, cavity2, cavity3, cavity4;
        string getKQTong(string cavity)
        {
            int socavity = getSoCavity(FrmListKetQuaDo.masp1);
            if (Convert.ToInt32(cavity) > socavity) return string.Empty;

            int count = xpCDimensionData.Cast<tb_Report_DimensionDataForSupplier>()
                .Count(item => item.cavityno == cavity);

            if (count == 0 && Convert.ToInt32(cavity) <= socavity)
            {
                UOW.ExecuteSproc("SP_INSERT_DEMENSION_DATA_SUPPLIER", FrmListKetQuaDo.id1, FrmListKetQuaDo.masp1, FrmListKetQuaDo.sokhuon1, cavity);
                return "None";
            } 

            return cavity;
        }

        string getKQDo(string cavity)
        {
            int socavity = getSoCavity(FrmListKetQuaDo.masp1);
            if (int.Parse(cavity) > socavity) return string.Empty;

            bool hasData = false;
            foreach (tb_Report_DimensionDataForSupplier item in xpCDimensionData)
            {
                if (item.cavityno != cavity) continue;
                hasData = true;
                if (item.judge == "NG") return "NG";
            }
            return hasData ? "OK" : "None";
        }


        private void myGridView2_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            base.gridview_CellValueChanged(sender, e);
            tb_Report_DimensionDataForSupplier item = myGridView2.GetFocusedRow() as tb_Report_DimensionDataForSupplier;
            if (item == null) return;

            if ((item.data1 >= item.lower || (item.spectRSlower != 1000 && (item.data1 >= item.spectRSlower)))
                && (item.data1 <= item.lower || (item.spectRSupper != 1000 && (item.data1 <= item.spectRSupper))))
                item.judge = "OK";
            else item.judge = "NG";
            try
            {
                UOW.CommitChanges();

                textEdit1.Text = getKQDo("1");
                textEdit2.Text = getKQDo("2");
                textEdit3.Text = getKQDo("3");
                textEdit4.Text = getKQDo("4");
            }
            catch (Exception)
            {
                UOW.ReloadChangedObjects();
            }
        }
        string getECNHis(string masp)
        {
            tb_ECNDRW item = UOW.FindObject<tb_ECNDRW>(CriteriaOperator.Parse("masp=?", masp));
            if (item != null)
                return item.sobanvechildren.ToString();
            return "000";
        }

        int getSoCavity(string masp)
        {
            tb_Part_master item = UOW.FindObject<tb_Part_master>(CriteriaOperator.Parse("Part_no=?", masp));
            if (item != null)
                return item.Cavity;
            return 0;
        }

        /// <summary>
        /// Prepare cavity data, generate report filename parts, export report
        /// </summary>
        /// <param name="isSupplier">true = Supplier report, false = FA report</param>
        void ExportDimensionReport(bool isSupplier)
        {
            cavity1 = getKQTong("1");
            cavity2 = getKQTong("2");
            cavity3 = getKQTong("3");
            cavity4 = getKQTong("4");

            if (cavity1 == "None")
                UOW.ExecuteSproc("SP_DIMENSION_CHUANHOA", "1");
            if (cavity2 == "None")
                UOW.ExecuteSproc("SP_DIMENSION_CHUANHOA", "2");
            if (cavity3 == "None")
                UOW.ExecuteSproc("SP_DIMENSION_CHUANHOA", "3");
            if (cavity4 == "None")
                UOW.ExecuteSproc("SP_DIMENSION_CHUANHOA", "4");

            UOW.ExecuteSproc("SP_DIMENSION_CHUANHOA", FrmListKetQuaDo.masp1);

            // Create report based on type
            XtraReport report;
            if (isSupplier)
            {
                var supplierReport = new DimensionFormatForSupplier_Report();
                supplierReport.masp.Value = FrmListKetQuaDo.masp1;
                supplierReport.sokhuon.Value = FrmListKetQuaDo.sokhuon1;
                supplierReport.ngaydo.Value = FrmListKetQuaDo.ngayktra1.Date;
                supplierReport.cav1.Value = cavity1;
                supplierReport.cav2.Value = cavity2;
                supplierReport.cav3.Value = cavity3;
                supplierReport.cav4.Value = cavity4;
                report = supplierReport;
            }
            else
            {
                var faReport = new DimensionFormatForFA_Report();
                faReport.masp.Value = FrmListKetQuaDo.masp1;
                faReport.sokhuon.Value = FrmListKetQuaDo.sokhuon1;
                faReport.ngaydo.Value = FrmListKetQuaDo.ngayktra1.Date;
                faReport.cav1.Value = cavity1;
                faReport.cav2.Value = cavity2;
                faReport.cav3.Value = cavity3;
                faReport.cav4.Value = cavity4;
                report = faReport;
            }

            // Build filename
            string timea = FrmListKetQuaDo.ngayktra1.Date.ToString("yyyyMMdd");
            string his = getECNHis(FrmListKetQuaDo.masp1).PadLeft(3, '0');
            string codespp = textEdit6.Text;
            string solangui = textEdit7.Text.PadLeft(2, '0');
            string parta = FrmListKetQuaDo.masp1 + "-000-" + FrmListKetQuaDo.sokhuon1 + "-" + his + "-" + codespp + "-" + timea + "-" + solangui;

            string filename = FrmListKetQuaDo.SaveFileDialog("Type(*.xls,*.xlsx)|*.xls;*.xlsx", parta + ".xlsx");
            if (filename != string.Empty)
                report.ExportToXlsx(filename);
            this.Close();
            if (Dialog.ShowYesNoDialog("Đã issue thành công! Bạn có muốn mở file không?") == System.Windows.Forms.DialogResult.Yes)
                OpenAfterExport(filename);
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            ExportDimensionReport(isSupplier: true);
        }

        private void myGridView2_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            try
            {
                if (e.RowHandle > -1 && (e.DisplayText.Equals("1000") || ((e.Column == coldata2 || e.Column == coldata3) && e.DisplayText.Equals("0"))))
                    e.DisplayText = string.Empty;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FrmDimensionData_Supplier_Issue] {ex.Message}"); }
        }

        private void textEdit1_TextChanged(object sender, EventArgs e)
        {
            TextEdit textedit = sender as TextEdit;
            if (textedit.Text == "OK")
                textedit.BackColor = Color.Green;
            else if (textedit.Text == "NG")
                textedit.BackColor = Color.Red;
            else if (textedit.Text == string.Empty)
                textedit.BackColor = Color.Gray;
            else if (textedit.Text == "None")
                textedit.BackColor = Color.Yellow;
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            ExportDimensionReport(isSupplier: false);
        }
    }
}
