# Stored Procedures – IssueReport
> Trích xuất từ PostgreSQL: **QUALITY_CONTROL_SYS_4_0**  
> Ngày: 2026-07-16 09:12  
> Tổng cộng: **14 Functions**

---

## 1. SP_GET_CHI_TIET_KIEM_TRA_NEW
> Lấy chi tiết kiểm tra (phiên bản mới) theo `idlist`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_CHI_TIET_KIEM_TRA_NEW"(p_idlist integer)
 RETURNS SETOF type_chi_tiet_kiem_tra_new
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    WITH DistinctHangMuc AS (
        SELECT DISTINCT hangmuccheck, ketquakt, Cavity AS cavity, comment, hinhthuc
          FROM "tb_chitietketqua_newver"
         WHERE idlistketquakiemtra = p_idlist
    )
    SELECT (ROW_NUMBER() OVER (ORDER BY hangmuccheck))::integer AS stt,
           hangmuccheck, ketquakt, cavity, comment, hinhthuc
      FROM DistinctHangMuc;
END;
$function$
```

---

## 2. SP_GET_CHI_TIET_KIEM_TRA
> Lấy chi tiết kiểm tra (bản cũ) theo `idlist`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_CHI_TIET_KIEM_TRA"(p_idlist integer)
 RETURNS SETOF type_chi_tiet_kiem_tra
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        id, nguoikiemtra, ngaykiemtra, hinhthuc, mayduc, partno, dieno, partname, rank, bemat, vitri,
        hinhanh, ketquakt, comment, cavity, anhloi, idlistketquakiemtra, idchitiet, ketqualimit, loi, hangmuccheck
    FROM "tb_chitietketqua_newver"
    WHERE idlistketquakiemtra = p_idlist;
END;
$function$
```

---

## 3. SP_GET_DANH_SACH_KIEM_TRA
> Lấy thông tin 1 record kiểm tra theo `id`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_DANH_SACH_KIEM_TRA"(p_idlist integer)
 RETURNS SETOF type_danh_sach_kiem_tra
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        id, giolaymau, ngayktra, nguoiktra, hinhthuc, ca, may, idchitietkqua, masp, tensp, dieno, ketquatonghop,
        comment, burr, rework_recheck, "NG" AS ng, do1, luuhoso, cavity, ktraop, commentop, ktramay, commentmay,
        ktratinhtrang, commenttinhtrang, ktrapacking, commentpacking, ktramau, commentmau, tinhtrang,
        thoigian, shift, ngay, nhuatai, hangmucchung, 
        "danhgiadiemthaydoiDIE" AS danhgiadiemthaydoidie, "diemthaydoiDIE" AS diemthaydoidie, "commentDIE" AS commentdie,
        "danhgiadiemthaydoiPM" AS danhgiadiemthaydoipm, "diemthaydoiPM" AS diemthaydoipm, "commentPM" AS commentpm, 
        "danhgiadiemthaydoiPRO" AS danhgiadiemthaydoipro, "diemthaydoiPRO" AS diemthaydoipro, "commentPRO" AS commentpro,
        "NGcungmau" AS ngcungmau, giaoca, rank, rankhistory, shotvaloaibd, loidie, ketqua, ketqualimit
    FROM "tb_listketquakiemtra"
    WHERE id = p_idlist;
END;
$function$
```

---

## 4. SP_GET_CHECK_SHEET
> Lấy thông tin Check Sheet theo mã sản phẩm

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_CHECK_SHEET"(p_masp character varying)
 RETURNS SETOF type_check_sheet
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        "Id" AS id, "Part_no" AS part_no, "Soquanly" AS soquanly, "Soversion" AS soversion,
        "Lydo_update" AS lydo_update, "Ghichu" AS ghichu, "Attfile_scan" AS attfile_scan,
        "Attfile_E" AS attfile_e, "Ngay_update" AS ngay_update, "Nguoi_update" AS nguoi_update,
        "isHistory" AS ishistory, tensp AS tensp, approved AS approved, ngayissue AS ngayissue,
        nguoicheckcode AS nguoicheckcode, nguoipheduyetcode AS nguoipheduyetcode, nguoilapcode AS nguoilapcode
    FROM "Check_Sheet"
    WHERE "Part_no" = p_masp;
END;
$function$
```

---

## 5. SP_GET_PART_MATERIAL_INFO
> Lấy thông tin vật liệu của part theo `masp` + `sokhuon`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_PART_MATERIAL_INFO"(p_masp character varying, p_sokhuon character varying)
 RETURNS SETOF type_part_material_info
 LANGUAGE plpgsql
AS $function$
 BEGIN
     RETURN QUERY
     SELECT
         pm."Part_no" AS part_no,
         pm."Part_name" AS part_name,
         pm."Die_no" AS die_no,
         'Samsung SDI Co., Ltd'::varchar AS nhacungcap,
         'Samsung SDI Vina'::varchar      AS nhasx,
         'ABS-120 Grade A'::varchar      AS tennhua,
         'ABS'::varchar                  AS loainhua,
         'BK-001'::varchar               AS mamau,
         'Black (Mau den)'::varchar      AS maunhua,
         pm."Model"                      AS model,
         'Certified (RoHS)'::varchar     AS certified,
         pm."TaiSD_ACTUAL"::varchar      AS taisd_actual,
         'img_gate_carving.png'::varchar AS anhvetkhacnhua,
         'Point A1 modified'::varchar    AS changingpoint,
         '15%'::varchar                  AS taisudung_phantram,
         'img_gate_carving2.png'::varchar AS anhvetkhacnhua2,
         pm."TaiSD_ACTUAL"::varchar      AS taisd_actual2
     FROM "tb_Part_master" pm
     WHERE pm."Part_no" = p_masp AND pm."Die_no" = p_sokhuon;
 END;
 $function$
```

---

## 6. SP_GET_PART_INFO
> Lấy toàn bộ thông tin part theo `masp`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_PART_INFO"(p_masp character varying)
 RETURNS SETOF type_part_info
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        "Id_part" AS id_part, "Model" AS model, "Part_name" AS part_name, "Part_no" AS part_no, 
        "Die_no" AS die_no, "Cavity" AS cavity, "Box_using" AS box_using, "Part_img1" AS part_img1, 
        "Part_img2" AS part_img2, "LastUpdate" AS lastupdate, "Pic" AS pic, grouppart, priority,
        "Part_number" AS part_number, "TaiSD_DRW" AS taisd_drw, "TaiSD_ACTUAL" AS taisd_actual, 
        "CutGateInfo" AS cutgateinfo, "CommentCutGate" AS commentcutgate,
        "Sample_barcode" AS sample_barcode, "Sample_location" AS sample_location,
        "InsideCutGate" AS insidecutgate, "CMTInsideCutGate" AS cmtinsidecutgate, 
        "CongKim" AS congkim, "CMTCongKim" AS cmtcongkim, "PhunTrucTiep" AS phuntructiep, 
        "CMTPhunTrucTiep" AS cmtphuntructiep, "Tu_dkd" AS tu_dkd, "VerNew" AS vernew, 
        nguoiupdateeng, ngayupdateeng
    FROM "tb_Part_master"
    WHERE "Part_no" = p_masp;
END;
$function$
```

---

## 7. SP_GET_DATASHEET_INFO
> Lấy thông tin Data Sheet theo `masp` + `sokhuon`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_DATASHEET_INFO"(p_masp character varying, p_sokhuon character varying)
 RETURNS SETOF type_datasheet_info
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        "Id" AS id, "Part_no" AS part_no, "Soquanly" AS soquanly, "Soversion" AS soversion,
        "Lydo_update" AS lydo_update, "Ghichu" AS ghichu, "Attfile_scan" AS attfile_scan,
        "Attfile_E" AS attfile_e, "Attfile" AS attfile, "Ngay_update" AS ngay_update,
        "Nguoi_update" AS nguoi_update, "isHistory" AS ishistory, "tensp" AS tensp,
        "aprroved" AS approved, "nguoilapcode" AS nguoilapcode, "nguoicheckcode" AS nguoicheckcode,
        "nguoipheduyetcode" AS nguoipheduyetcode, "ngayissue" AS ngayissue,
        "ngaycheck" AS ngaycheck, "ngaypheduyet" AS ngaypheduyet
    FROM "Data_Sheet"
    WHERE "Part_no" = p_masp AND "Ghichu" = p_sokhuon;
END;
$function$
```

---

## 8. SP_GET_DEMENSION_DATA_FULL
> Lấy toàn bộ dữ liệu đo kích thước từ bảng Temp, JOIN với bảng chuẩn

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_DEMENSION_DATA_FULL"(
    p_idlist integer,
    p_masp character varying,
    p_sokhuon character varying,
    p_cavity integer
)
 RETURNS SETOF type_demension_data_full
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_groupname varchar(50);
BEGIN
    SELECT grouppart INTO v_groupname FROM "tb_Part_master"
     WHERE "Part_no" = p_masp AND "Die_no" = p_sokhuon;

    RETURN QUERY
    SELECT ctd.items, hmd.trangbanve, hmd.ktdng,
           ('+' || hmd.sailechtren::text || '/-' || hmd.sailechduoi::text)::text AS sailech,
           ctd.gioihantren, ctd.gioihanduoi, ctd.cavity, ctd.vitri,
           ctd.dungcudo, ctd.ketquado, ctd.ketquadanhgia,
           ctd.gioihanduoifa, ctd.gioihantrenfa,
           hmdct.fanolower, hmdct.fanoupper, hmdct.changingpoint::text AS changingpoint,
           ctd.nguoiktra, ctd.casx, ctd.ngayktra,
           ctd.hinhthuc, ctd.gioihanduoidrw, ctd.gioihantrendrw,
           ctd.idlistketquado
      FROM "tb_Issue_Dimention_Data_Temp" ctd
      JOIN "tb_HangMucDoNewVer" hmd
        ON ctd.items = hmd.items AND ctd.vitri = hmd.vitri AND hmd.groupname = v_groupname
      JOIN "tb_HangMucDoChiTietNewVer" hmdct
        ON hmd.items = hmdct.items AND hmd.vitri = hmdct.vitri
       AND hmdct.groupname = v_groupname
       AND hmdct.masp = p_masp AND hmdct.sokhuon = p_sokhuon AND hmdct.cavity = p_cavity
     WHERE ctd.idlistketquado = p_idlist
     ORDER BY ctd.dungcudo, ctd.items, ctd.vitri;
END;
$function$
```

---

## 9. SP_GET_DEMENSION_DATA_FOR_DATASHEET
> Lấy dữ liệu kích thước để đổ vào DataSheet/FA report (có phân biệt FA vs non-FA)

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_DEMENSION_DATA_FOR_DATASHEET"(
    p_masp character varying,
    p_sokhuon character varying,
    p_cavity integer,
    p_hinhthuc character varying
)
 RETURNS SETOF type_demension_data_for_datasheet
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_groupname varchar(50);
BEGIN
    SELECT grouppart INTO v_groupname FROM "tb_Part_master"
     WHERE "Part_no" = p_masp AND "Die_no" = p_sokhuon;

    IF p_hinhthuc = N'FA' THEN
        -- FA: lấy tất cả hạng mục kể cả faonly
        RETURN QUERY
        SELECT hmd.items, hmd.trangbanve, hmd.ktdng,
               ('+' || hmd.sailechtren::text || '/-' || hmd.sailechduoi::text)::text AS sailech,
               hmdct.finalupper, hmdct.finallower, hmdct.cavity, hmdct.vitri,
               hmd.dungcudo, hmdct.gioihanduoifa, hmdct.gioihantrenfa,
               hmdct.fanolower, hmdct.fanoupper, hmdct.changingpoint::text AS changingpoint,
               hmd.gioihanduoidrw, hmd.gioihantrendrw
          FROM "tb_HangMucDoNewVer" hmd
          JOIN "tb_HangMucDoChiTietNewVer" hmdct
            ON hmd.items = hmdct.items AND hmd.vitri = hmdct.vitri
           AND hmd.groupname = v_groupname AND hmdct.groupname = v_groupname
           AND hmdct.masp = p_masp AND hmdct.sokhuon = p_sokhuon AND hmdct.cavity = p_cavity
         WHERE hmd.groupname = v_groupname
         ORDER BY hmd.dungcudo, hmd.items, hmd.vitri;
    ELSE
        -- Non-FA: chỉ lấy hạng mục faonly = false
        RETURN QUERY
        SELECT hmd.items, hmd.trangbanve, hmd.ktdng,
               ('+' || hmd.sailechtren::text || '/-' || hmd.sailechduoi::text)::text AS sailech,
               hmdct.finalupper, hmdct.finallower, hmdct.cavity, hmdct.vitri,
               hmd.dungcudo, hmdct.gioihanduoifa, hmdct.gioihantrenfa,
               hmdct.fanolower, hmdct.fanoupper, hmdct.changingpoint::text AS changingpoint,
               hmd.gioihanduoidrw, hmd.gioihantrendrw
          FROM "tb_HangMucDoNewVer" hmd
          JOIN "tb_HangMucDoChiTietNewVer" hmdct
            ON hmd.items = hmdct.items AND hmd.vitri = hmdct.vitri
           AND hmd.groupname = v_groupname AND hmdct.groupname = v_groupname
           AND hmdct.masp = p_masp AND hmdct.sokhuon = p_sokhuon AND hmdct.cavity = p_cavity
         WHERE hmd.groupname = v_groupname AND hmd.faonly = false
         ORDER BY hmd.dungcudo, hmd.items, hmd.vitri;
    END IF;
END;
$function$
```

---

## 10. SP_INSERT_DEMENSION_DATA_SUPPLIER
> Insert dữ liệu đo kích thước vào bảng Supplier từ bảng `tb_chitietkqdo`

```sql
CREATE OR REPLACE FUNCTION public."SP_INSERT_DEMENSION_DATA_SUPPLIER"(
    p_idlist integer,
    p_masp character varying,
    p_sokhuon character varying,
    p_cavity integer
)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_groupname varchar(50);
BEGIN
    SELECT grouppart INTO v_groupname FROM "tb_Part_master"
     WHERE "Part_no" = p_masp AND "Die_no" = p_sokhuon;

    INSERT INTO "tb_Report_DimensionDataForSupplier" (
        item, ktdn, dungsai, cavityno, position, tool,
        data1, judge, "lower", "upper", "spectRSlower", "spectRSupper",
        trangbv, loaikichthuoc, diffgh
    )
    SELECT ctd.items, hmd.ktdng,
           '+' || hmd.sailechtren::text || '/-' || hmd.sailechduoi::text,
           p_cavity::text, ctd.vitri, ctd.dungcudo,
           ctd.ketquado, ctd.ketquadanhgia,
           ctd.gioihanduoidrw, ctd.gioihantrendrw,
           ctd.gioihanduoifa, ctd.gioihantrenfa,
           hmd.trangbanve, hmd.loaikt, ctd.diffgh
      FROM "tb_chitietkqdo" ctd
      JOIN "tb_HangMucDoNewVer" hmd
        ON ctd.items = hmd.items AND ctd.vitri = hmd.vitri AND hmd.groupname = v_groupname
     WHERE ctd.idlistketquado = p_idlist
     ORDER BY ctd.dungcudo, ctd.items, ctd.vitri;
END;
$function$
```

---

## 11. SP_DIMENSION_CHUANHOA
> Chuẩn hóa dữ liệu bảng Supplier: reset giới hạn 1000 → NULL, xóa data cavity chỉ định

```sql
CREATE OR REPLACE FUNCTION public."SP_DIMENSION_CHUANHOA"(p_cavity character varying)
 RETURNS void
 LANGUAGE sql
AS $function$
UPDATE "tb_Report_DimensionDataForSupplier" SET "spectRSlower" = NULL WHERE "spectRSlower" = 1000;
UPDATE "tb_Report_DimensionDataForSupplier" SET "spectRSupper" = NULL WHERE "spectRSupper" = 1000;
UPDATE "tb_Report_DimensionDataForSupplier"
   SET cavityno = 'NONE', data1 = NULL, judge = NULL
 WHERE cavityno = p_cavity;
$function$
```

---

## 12. SP_GET_ECN_STATUS_INFO
> Lấy trạng thái ECN theo `masp` + `sokhuon`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_ECN_STATUS_INFO"(p_masp character varying, p_sokhuon character varying)
 RETURNS SETOF type_ecn_status_info
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT idstatus, ECNSTATUS_DATA.masp, ECNSTATUS_DATA.sokhuon,
           sobanveparentstatus, ngayupdatestatus, comment,
           nguoiupdatestatus, sobanveparentdrw
    FROM "tb_ECNStatus" ECNSTATUS_DATA
    WHERE ECNSTATUS_DATA.masp = p_masp AND ECNSTATUS_DATA.sokhuon = p_sokhuon;
END;
$function$
```

---

## 13. SP_GET_INFO_ECN_DRW
> Lấy thông tin ECN bản vẽ (DRW) theo `masp`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_INFO_ECN_DRW"(p_masp character varying)
 RETURNS SETOF type_info_ecn_drw
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        id, ECN_DRW.masp, tensp, soecn, attachment, noidungecn, suakhuon, fa, tvp, sobanvechildren,
        drwchildren, sobanveparent, drwparent, "P_3ddrw" AS p_3ddrw, artworddrw, ngayupdate, nguoiupdate, ishistory,
        sokhuon, history, ngayguiapply, ngaycapecn, dongweb, comment, ketquafa, ngaysuakhuon,
        ngaycapnhatgiayto, ecnlevel, situation
    FROM "tb_ECNDRW" ECN_DRW
    WHERE ECN_DRW.masp = p_masp;
END;
$function$
```

---

## 14. SP_GET_PIC_TOOL
> Lấy thông tin người đo + dụng cụ đo (tối đa 5 cặp) từ kết quả đo theo `idlist`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_PIC_TOOL"(p_idlist integer)
 RETURNS SETOF type_pic_tool
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_nguoi1 varchar(50); v_dungcu1 varchar(50);
    v_nguoi2 varchar(50); v_dungcu2 varchar(50);
    v_nguoi3 varchar(50); v_dungcu3 varchar(50);
    v_nguoi4 varchar(50); v_dungcu4 varchar(50);
    v_nguoi5 varchar(50); v_dungcu5 varchar(50);
BEGIN
    WITH pairs AS (
        SELECT DISTINCT nguoiktra, dungcudo,
               ROW_NUMBER() OVER (ORDER BY nguoiktra, dungcudo) AS rn
          FROM "tb_chitietkqdo"
         WHERE idlistketquado = p_idlist
    )
    SELECT
        max(CASE WHEN rn=1 THEN nguoiktra END),
        max(CASE WHEN rn=1 THEN dungcudo  END),
        max(CASE WHEN rn=2 THEN nguoiktra END),
        max(CASE WHEN rn=2 THEN dungcudo  END),
        max(CASE WHEN rn=3 THEN nguoiktra END),
        max(CASE WHEN rn=3 THEN dungcudo  END),
        max(CASE WHEN rn=4 THEN nguoiktra END),
        max(CASE WHEN rn=4 THEN dungcudo  END),
        max(CASE WHEN rn=5 THEN nguoiktra END),
        max(CASE WHEN rn=5 THEN dungcudo  END)
      INTO v_nguoi1,v_dungcu1, v_nguoi2,v_dungcu2, v_nguoi3,v_dungcu3,
           v_nguoi4,v_dungcu4, v_nguoi5,v_dungcu5
      FROM pairs
     WHERE rn <= 5;

    RETURN QUERY
    SELECT v_nguoi1, v_dungcu1, v_nguoi2, v_dungcu2, v_nguoi3, v_dungcu3,
           v_nguoi4, v_dungcu4, v_nguoi5, v_dungcu5;
END;
$function$
```

---

## Bảng tóm tắt

| # | Function | Parameters | Returns | Language |
|---|---|---|---|---|
| 1 | `SP_GET_CHI_TIET_KIEM_TRA_NEW` | `p_idlist int` | `SETOF type_chi_tiet_kiem_tra_new` | plpgsql |
| 2 | `SP_GET_CHI_TIET_KIEM_TRA` | `p_idlist int` | `SETOF type_chi_tiet_kiem_tra` | plpgsql |
| 3 | `SP_GET_DANH_SACH_KIEM_TRA` | `p_idlist int` | `SETOF type_danh_sach_kiem_tra` | plpgsql |
| 4 | `SP_GET_CHECK_SHEET` | `p_masp varchar` | `SETOF type_check_sheet` | plpgsql |
| 5 | `SP_GET_PART_MATERIAL_INFO` | `p_masp, p_sokhuon varchar` | `SETOF type_part_material_info` | plpgsql |
| 6 | `SP_GET_PART_INFO` | `p_masp varchar` | `SETOF type_part_info` | plpgsql |
| 7 | `SP_GET_DATASHEET_INFO` | `p_masp, p_sokhuon varchar` | `SETOF type_datasheet_info` | plpgsql |
| 8 | `SP_GET_DEMENSION_DATA_FULL` | `p_idlist int, p_masp, p_sokhuon varchar, p_cavity int` | `SETOF type_demension_data_full` | plpgsql |
| 9 | `SP_GET_DEMENSION_DATA_FOR_DATASHEET` | `p_masp, p_sokhuon varchar, p_cavity int, p_hinhthuc varchar` | `SETOF type_demension_data_for_datasheet` | plpgsql |
| 10 | `SP_INSERT_DEMENSION_DATA_SUPPLIER` | `p_idlist int, p_masp, p_sokhuon varchar, p_cavity int` | `void` | plpgsql |
| 11 | `SP_DIMENSION_CHUANHOA` | `p_cavity varchar` | `void` | sql |
| 12 | `SP_GET_ECN_STATUS_INFO` | `p_masp, p_sokhuon varchar` | `SETOF type_ecn_status_info` | plpgsql |
| 13 | `SP_GET_INFO_ECN_DRW` | `p_masp varchar` | `SETOF type_info_ecn_drw` | plpgsql |
| 14 | `SP_GET_PIC_TOOL` | `p_idlist int` | `SETOF type_pic_tool` | plpgsql |
