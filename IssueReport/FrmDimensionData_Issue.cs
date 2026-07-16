using DevExpress.Xpo;
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
    public partial class FrmDimensionData_Issue : FrmBase
    {
        FrmListKetQuaDo frm;
        bool isDirect;
        XPCollection<tb_ChuKyDienTu> ChuKys;
        public FrmDimensionData_Issue(FrmListKetQuaDo frm, bool isDirect)
        {
            InitializeComponent();
            this.frm = frm;
            gridview = myGridView2;
            gridview.BestFitColumns();
            memoEdit1.Text = getKQTong();
            this.isDirect = isDirect;
            ChuKys = new XPCollection<tb_ChuKyDienTu>(UOW);
        }

        private void myGridView2_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            try
            {
                if (e.RowHandle > -1 && (e.Column == colgioihanduoifa || e.Column == colgioihantrenfa) && e.DisplayText.Equals("1000"))
                    e.DisplayText = string.Empty;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FrmDimensionData_Issue] {ex.Message}"); }

        }
        string getKQTong()
        {
            return xpCDimensionData.Cast<tb_Issue_Dimention_Data_Temp>()
                .Any(item => item.ketquadanhgia == "NG") ? "NG" : "OK";
        }

        protected override void gridview_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            base.gridview_CellValueChanged(sender, e);
            tb_Issue_Dimention_Data_Temp item = myGridView2.GetFocusedRow() as tb_Issue_Dimention_Data_Temp;
            if (item == null) return;
            if (item.ketquado >= item.gioihanduoi && item.ketquado <= item.gioihantren)
                item.ketquadanhgia = "OK";
            else item.ketquadanhgia = "NG";
            memoEdit1.Text = getKQTong();
            try
            {
                UOW.CommitChanges();
                //Lưu giá trị khi lưu hồ sơ vào bảng gốc dữ liệu đo
               // UOW.ExecuteNonQuery("Update tb_chitietkqdo set ketquadohoso=A.ketquado,ketquadanhgiahoso=A.ketquadanhgia from (Select id,ketquado,ketquadanhgia from tb_Issue_Dimention_Data_Temp) A where A.id=tb_chitietkqdo.id");
            }
            catch (Exception)
            {
                UOW.ReloadChangedObjects();
            }
        }

        private void memoEdit1_TextChanged(object sender, EventArgs e)
        {
            if (memoEdit1.Text == "OK") memoEdit1.BackColor = Color.Green;
            else memoEdit1.BackColor = Color.Red;
        }

        void ThemHoSo()
        {
            //Lưu giá trị khi lưu hồ sơ vào bảng gốc dữ liệu đo
            UOW.ExecuteNonQuery("Update \"tb_chitietkqdo\" set \"ketquadohoso\"=A.\"ketquado\",\"ketquadanhgiahoso\"=A.\"ketquadanhgia\" from (Select \"id\",\"ketquado\",\"ketquadanhgia\" from \"tb_Issue_Dimention_Data_Temp\") A where A.\"id\"=\"tb_chitietkqdo\".\"id\"");

            tb_Issue_Dimention_Data_Temp item = xpCDimensionData[0] as tb_Issue_Dimention_Data_Temp;
            DataSheet_Report report = new DataSheet_Report();
            report.Parameters["idList"].Value = item.idlistketquado;
            report.Parameters["masp"].Value = item.masp;
            report.Parameters["sokhuon"].Value = item.sokhuon;
            report.Parameters["cavity"].Value = item.cavity;
            report.Parameters["mayduc"].Value = Mayduc_textEdit1.Text;
            report.Parameters["G2Name"].Value = Ge_Name_textEdit1.Text;
            string timea = DateTime.Now.ToString("ddMMyyyyHHmmss_" + Mayduc_textEdit1.Text);
            timea = item.masp + "-" + item.sokhuon + "-" + timea;
            string filename = GetFileForImage(timea + ".xlsx");
            report.ExportToXlsx(filename);
            tb_Issue_Dimension itemSave = new tb_Issue_Dimension(UOW);
            itemSave.iddo = item.idlistketquado;
            itemSave.ngaysx = item.ngayktra;
            itemSave.casx = item.casx;
            itemSave.cavity = item.cavity;
            itemSave.attdatafile = timea + ".xlsx";
            itemSave.nguoiattach = FrmMAIN.userName;
            itemSave.ngayupdate = DateTime.Now;
            itemSave.masp = item.masp;
            itemSave.tensp = item.tensp;
            itemSave.sokhuon = item.sokhuon;
            itemSave.lydodo = item.hinhthuc;
            itemSave.ketquadanhgia = memoEdit1.Text;
            base.Save();
          
           
            UOW.ExecuteNonQuery(
                "update \"tb_listketquado\" set \"LuuHs\" = 'YES' where \"id\" = ?",
                new object[] { item.idlistketquado });
            this.frm.Reload();
            if (!isDirect)
                if (Dialog.ShowYesNoDialog("Đã issue thành công! Bạn có muốn mở file không?") == System.Windows.Forms.DialogResult.Yes)
                    OpenAfterExport(filename);
        }

        private void btluu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Ge_Name_textEdit1.Text))
            {
                Dialog.ShowErrorDialog("Chưa nhập mật khẩu G2 check?");
                return;
            }
            if (memoEdit1.Text.Equals("NG"))
            {
                if (Dialog.ShowYesNoDialog("Vẫn còn hạng mục NG bạn chắc chắn muôn Lưu vào hồ sơ không?") == System.Windows.Forms.DialogResult.No)
                    return;
                if (xpCDimensionData.Count > 0)
                {
                    try
                    {
                        ThemHoSo();
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        Dialog.ShowErrorDialog(ex.ToString());
                    }
                }
            }
            else if (memoEdit1.Text.Equals("OK"))
                try
                {
                    ThemHoSo();
                    this.Close();
                }
                catch (Exception ex)
                {
                    Dialog.ShowErrorDialog(ex.ToString());
                }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            string fileName = "";
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Exel 2007 (*.xlsx)|*.xlsx";
            if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                fileName = save.FileName;
            if (fileName != "")
                myGridView2.ExportToXlsx(fileName);
        }
        string getPIC(string password)
        {
            if (string.IsNullOrEmpty(password.Trim()))
            {
                Dialog.ShowErrorDialog("Chưa nhập mật khẩu G2 check");
                return string.Empty;
            }
            tb_ChuKyDienTu chuky = ChuKys.FirstOrDefault(s => s.matkhau == password);
            if (chuky != null)
                return chuky.ho + " " + chuky.ten;
            else
            {
                Dialog.ShowErrorDialog("Mật khẩu không đúng!");
                return string.Empty;
            }
        }
        private void Sign_buttonEdit1_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            Ge_Name_textEdit1.Text = getPIC(Sign_buttonEdit1.Text.Trim());
        }

        private void Sign_buttonEdit1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                Ge_Name_textEdit1.Text = getPIC(Sign_buttonEdit1.Text.Trim());
            }
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(getKQTong());

        }
    }
}
