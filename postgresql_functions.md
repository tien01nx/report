# PostgreSQL Stored Procedures and Functions

*Generated on: 2026-07-20 11:39:57*

Danh sách các function và stored procedure trong schema `public` của cơ sở dữ liệu `QUALITY_CONTROL_SYS_4_0`.

## 1. `DTS_ChartChiTietDanhGia`

```sql
CREATE OR REPLACE FUNCTION public."DTS_ChartChiTietDanhGia"(p_ngay date, p_ca character varying)
 RETURNS TABLE(danhmuc character varying, mucdo1 integer, mucdo2 integer)
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Dùng TEMP TABLE với ON COMMIT DROP để tự dọn sau transaction
    CREATE TEMP TABLE tmp_chitietdanhgia (
        danhmuc varchar(50),
        mucdo1  int,
        mucdo2  int
    ) ON COMMIT DROP;

    -- Seed 4 danh mục
    INSERT INTO tmp_chitietdanhgia (danhmuc) VALUES ('NG Quy định');
    INSERT INTO tmp_chitietdanhgia (danhmuc) VALUES ('NG 5S');
    INSERT INTO tmp_chitietdanhgia (danhmuc) VALUES ('NG Chất lượng');
    INSERT INTO tmp_chitietdanhgia (danhmuc) VALUES ('Điểm tốt');

    UPDATE tmp_chitietdanhgia SET mucdo1 = 0, mucdo2 = 0;

    -- Cập nhật mucdo1
    UPDATE tmp_chitietdanhgia
    SET mucdo1 = COALESCE(
        (SELECT SUM(quydinhmuc1) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'NG Quy định';

    UPDATE tmp_chitietdanhgia
    SET mucdo1 = COALESCE(
        (SELECT SUM(ng5smuc1) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'NG 5S';

    UPDATE tmp_chitietdanhgia
    SET mucdo1 = COALESCE(
        (SELECT SUM(ngchatluongmuc1) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'NG Chất lượng';

    UPDATE tmp_chitietdanhgia
    SET mucdo1 = COALESCE(
        (SELECT SUM(phathienloimuc1) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'Điểm tốt';

    -- Cập nhật mucdo2
    UPDATE tmp_chitietdanhgia
    SET mucdo2 = COALESCE(
        (SELECT SUM(quydinhmuc2) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'NG Quy định';

    UPDATE tmp_chitietdanhgia
    SET mucdo2 = COALESCE(
        (SELECT SUM(ng5smuc2) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'NG 5S';

    UPDATE tmp_chitietdanhgia
    SET mucdo2 = COALESCE(
        (SELECT SUM(ngchatluongmuc2) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'NG Chất lượng';

    UPDATE tmp_chitietdanhgia
    SET mucdo2 = COALESCE(
        (SELECT SUM(solancaitienmuc2) FROM "DTS_ChiTietDanhGia" WHERE ngay = p_ngay AND ca = p_ca), 0)
    WHERE tmp_chitietdanhgia.danhmuc = 'Điểm tốt';

    RETURN QUERY SELECT t.danhmuc, t.mucdo1, t.mucdo2 FROM tmp_chitietdanhgia t;
END;
$function$
```

---

## 2. `DTS_DATALAKE_DIE_BURR`

```sql
CREATE OR REPLACE FUNCTION public."DTS_DATALAKE_DIE_BURR"()
 RETURNS TABLE(id bigint, "Date_burr" date, "Shift" text, "May" text, "Part_no" text, grouppart text, "Die_no" text, "Cut" numeric, "An" numeric, "To" numeric, "Tray" numeric, lan numeric, "Other" numeric, "Total" numeric, "Type" text, ngaytao text, thangenglish integer)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    WITH inner_data AS (
        SELECT
            b.date_burr,
            b.shift,
            b.may,
            b.part_no,
            b."Die_no",
            b.cut::numeric,
            b.an::numeric,
            b.to::numeric,
            b.tray::numeric,
            b.lan::numeric,
            b.thaotaccong::numeric,
            b.other::numeric,
            TO_CHAR(DATE_TRUNC('month', b.date_burr), 'YYYY-MM')  AS ngaytao,
            EXTRACT(MONTH FROM b.date_burr)::int                   AS thangenglish
        FROM tb_burr b
        WHERE b.id_burr IN (
            SELECT MAX("Id_burr")
            FROM tb_burr
            GROUP BY "Part_no", "Die_no", "Cavity"
        )
        AND EXTRACT(YEAR FROM b.date_burr) >= 2025
    ),
    grouped AS (
        SELECT
            a.date_burr,
            a.shift,
            a.may,
            a.part_no,
            p."GroupPart",
            a."Die_no",
            SUM(a.cut)                                               AS "Cut",
            SUM(a.an)                                                AS "An",
            SUM(a.to)                                                AS "To",
            SUM(a.tray)                                              AS "Tray",
            SUM(a.lan)                                                 AS lan,
            SUM(a.other)                                             AS "Other",
            SUM(a.cut + a.an + a.to + a.tray + a.lan + a.other)  AS total_sum,
            a.ngaytao,
            a.thangenglish
        FROM inner_data a
        LEFT JOIN tb_Part_master p ON a.part_no = p.part_no
        GROUP BY
            a.date_burr, a.shift, a.may, a.part_no,
            p."GroupPart", a."Die_no", a.ngaytao, a.thangenglish
    )
    SELECT
        ROW_NUMBER() OVER (ORDER BY g.date_burr)::bigint  AS id,
        g.date_burr,
        g.shift,
        g.may,
        g.part_no,
        g."GroupPart",
        g."Die_no",
        g.cut,
        g.an,
        g.to,
        g.tray,
        g.lan,
        g.other,
        g.total_sum                                          AS "Total",
        CASE WHEN g.total_sum > 0 THEN 'Die Have Burr' ELSE 'Die No Burr' END AS "Type",
        g.ngaytao,
        g.thangenglish
    FROM grouped g
    ORDER BY g.date_burr ASC;
END;
$function$
```

---

## 3. `DTS_DATALAKE_MTBF_MTBR`

```sql
CREATE OR REPLACE FUNCTION public."DTS_DATALAKE_MTBF_MTBR"()
 RETURNS TABLE(id bigint, "GROUPPART" text, "MAKHUON" text, "THOIGIANDUNG" double precision, "DIEM1" integer, "SOLANLOI" integer, "THOIGIANSUA" double precision, "DIEM2" integer, "TICH" integer, "THOIGIANDIEOK" double precision, "OPAR" double precision, "MTBF" double precision, "TOTALSHOT" integer, "DIEM3" integer, "NGAYTAO" text, "THANGENGLISH" integer)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_thang int;
    v_nam   int;
BEGIN
    DROP TABLE IF EXISTS tmp_ketqua_mtbf;
    CREATE TEMP TABLE tmp_ketqua_mtbf (
        "GROUPPART"     text,
        "MAKHUON"       text,
        "THOIGIANDUNG"  float,
        "DIEM1"         int,
        "SOLANLOI"      int,
        "THOIGIANSUA"   float,
        "DIEM2"         int,
        "TICH"          int,
        "THOIGIANDIEOK" float,
        "OPAR"          float,
        "MTBF"          float,
        "TOTALSHOT"     int,
        "DIEM3"         int,
        "NGAYTAO"       text,
        "THANGENGLISH"  int
    );

    v_thang := 1;
    v_nam   := EXTRACT(YEAR FROM CURRENT_DATE)::int;

    WHILE v_thang <= EXTRACT(MONTH FROM CURRENT_DATE)::int LOOP
        -- Chỉ insert nếu tháng chưa có trong bảng tạm
        IF NOT EXISTS (
            SELECT 1 FROM tmp_ketqua_mtbf
            WHERE "THANGENGLISH" = v_thang
              AND EXTRACT(YEAR FROM TO_DATE("NGAYTAO", 'YYYY-MM')) = v_nam
        ) THEN
            -- Gọi function thay vì EXEC procedure
            INSERT INTO tmp_ketqua_mtbf (
                "GROUPPART","MAKHUON","THOIGIANDUNG","DIEM1","SOLANLOI",
                "THOIGIANSUA","DIEM2","TICH","THOIGIANDIEOK","OPAR","MTBF","TOTALSHOT","DIEM3"
            )
            SELECT
                "GROUPPART","MAKHUON","THOIGIANDUNG","DIEM1","SOLANLOI",
                "THOIGIANSUA","DIEM2","TICH","THOIGIANDIEOK","OPAR","MTBF","TOTALSHOT","DIEM3"
            FROM "DTS_SP_BANGDAUMAY_NEWVER02"(v_thang, v_nam);

            -- Gắn ngaytao và thangenglish cho các row vừa insert (NGAYTAO IS NULL)
            UPDATE tmp_ketqua_mtbf
            SET "NGAYTAO"     = TO_CHAR(make_date(v_nam, v_thang, 1), 'YYYY-MM'),
                "THANGENGLISH" = v_thang
            WHERE "NGAYTAO" IS NULL;
        END IF;

        v_thang := v_thang + 1;
    END LOOP;

    RETURN QUERY
    SELECT
        ROW_NUMBER() OVER (ORDER BY "MAKHUON")::bigint AS id,
        t."GROUPPART", t."MAKHUON", t."THOIGIANDUNG", t."DIEM1", t."SOLANLOI",
        t."THOIGIANSUA", t."DIEM2", t."TICH", t."THOIGIANDIEOK", t.opar, t.mtbf,
        t."TOTALSHOT", t."DIEM3", t."NGAYTAO", t."THANGENGLISH"
    FROM tmp_ketqua_mtbf t;
END;
$function$
```

---

## 4. `DTS_DATALAKE_TROUBLE_HISTORY`

```sql
CREATE OR REPLACE FUNCTION public."DTS_DATALAKE_TROUBLE_HISTORY"()
 RETURNS TABLE(id bigint, "NGAY" text, "MAY" text, "MASP" text, "MAKHUON" text, "GROUPPART" text, "CA" text, "MASUCO" text, "TENSUCO" text, "THOIGIAN" double precision, "NGAYTAO" text, "THANGENGLISH" integer)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_ngayhientai text;
BEGIN
    DROP TABLE IF EXISTS tmp_sucodie;
    CREATE TEMP TABLE tmp_sucodie (
        "NGAY"         text,
        "MAY"          text,
        "MASP"         text,
        "MAKHUON"      text,
        "GROUPPART"    text,
        "CA"           text,
        "MASUCO"       text,
        "TENSUCO"      text,
        "THOIGIAN"     float,
        "NGAYTAO"      text,
        "THANGENGLISH" int
    );

    v_ngayhientai := TO_CHAR(CURRENT_TIMESTAMP, 'YYYY-MM-DD');

    -- Giả sử DTS_SUMMARYSCK là function trả về SETOF record
    INSERT INTO tmp_sucodie ("NGAY","MAY","MASP","MAKHUON","GROUPPART","CA","MASUCO","TENSUCO","THOIGIAN")
    SELECT *
    FROM "DTS_SUMMARYSCK"('', '', 0, '2025-01-01', v_ngayhientai);

    -- Gán ngaytao = yyyy-MM theo tháng của NGAY, thangenglish = tháng
    UPDATE tmp_sucodie
    SET "NGAYTAO"      = TO_CHAR(DATE_TRUNC('month', "NGAY"::date), 'YYYY-MM'),
        "THANGENGLISH" = EXTRACT(MONTH FROM "NGAY"::date)::int;

    RETURN QUERY
    SELECT
        ROW_NUMBER() OVER (ORDER BY t."NGAY")::bigint AS id,
        t."NGAY", t."MAY", t."MASP", t."MAKHUON", t."GROUPPART",
        t."CA", t."MASUCO", t."TENSUCO", t."THOIGIAN",
        t."NGAYTAO", t."THANGENGLISH"
    FROM tmp_sucodie t
    ORDER BY t."NGAY";
END;
$function$
```

---

## 5. `DTS_GET_GIAO_CA`

```sql
CREATE OR REPLACE FUNCTION public."DTS_GET_GIAO_CA"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- INSERT sự cố xử lý trên máy (XULYTRENMAY = true)
    INSERT INTO "DTS_GiaoCa" (
        "TypeProblem","Problem","Cause","PartName","DieNo","Picture",
        "PIC","idsuco","xulytrenmay","ngayupdate","nguoiupdate",
        "idbaoduong","Actual","Target"
    )
    SELECT
        'SỰ CỐ (XỬ LÍ TRÊN MÁY)',
        "loichinh" || '=> ' || "chitietloi",
        "nguyennhanchinh",
        "tenkhuon",
        "SOKHUON",
        "PICTURE1",
        "PICUNDERINVESTIGATING",
        "ID",
        "XULYTRENMAY",
        "NGAYUPDATE",
        "NGUOIUPDATE",
        0,
        "THOIGIANDUNGMAYGIO" * 60 + "THOIGIANDUNGMAYPHUT"::float,
        20
    FROM "DTS_DieTrouble"
    WHERE "ID" NOT IN (SELECT "IDSUCO" FROM "DTS_GiaoCa")
      AND "XULYTRENMAY" = 1;

    -- INSERT sự cố hạ khuôn (XULYTRENMAY = false hoặc NULL)
    INSERT INTO "DTS_GiaoCa" (
        "TypeProblem","Problem","Cause","PartName","DieNo","Picture",
        "PIC","idsuco","xulytrenmay","ngayupdate","nguoiupdate",
        "idbaoduong","Actual"
    )
    SELECT
        'SỰ CỐ (HẠ KHUÔN)',
        "loichinh" || '=> ' || "chitietloi",
        "nguyennhanchinh",
        "tenkhuon",
        "SOKHUON",
        "PICTURE1",
        "PICUNDERINVESTIGATING",
        "ID",
        "XULYTRENMAY",
        "NGAYUPDATE",
        "NGUOIUPDATE",
        0,
        "THOIGIANSUAGIO" + "THOIGIANSUAPHUT"::float / 60.0
    FROM "DTS_DieTrouble"
    WHERE "ID" NOT IN (SELECT "IDSUCO" FROM "DTS_GiaoCa")
      AND ("XULYTRENMAY" = 0 OR "XULYTRENMAY" IS NULL);

    -- INSERT bảo dưỡng đã hoàn thành
    INSERT INTO "DTS_GiaoCa" (
        "TypeProblem","Problem","Cause","PartName","DieNo",
        "PIC","idbaoduong","ngayupdate","nguoiupdate","idsuco","Actual"
    )
    SELECT
        'BẢO DƯỠNG',
        'LOẠI BẢO DƯỠNG: ' || "REASON",
        "DETAIL",
        "PART_NAME",
        "Die_no",
        "NGUOIUPDATE",
        "ID",
        "NGAYUPDATE",
        "NGUOIUPDATE",
        0,
        round(("LOSS_TIME"::float)::numeric, 2)
    FROM "DTS_TimeMaintenance"
    WHERE "ID" NOT IN (SELECT "IDBAODUONG" FROM "DTS_GiaoCa")
      AND "LOSS_TIME" NOT LIKE '%GIỜ%';

    -- Xóa row "ĐANG BẢO DƯỠNG" cũ rồi insert lại
    DELETE FROM "DTS_GiaoCa" WHERE "TypeProblem" = 'ĐANG BẢO DƯỠNG';

    INSERT INTO "DTS_GiaoCa" (
        "TypeProblem","Problem","Cause","PartName","DieNo",
        "PIC","ngayupdate","nguoiupdate","idsuco"
    )
    SELECT
        'ĐANG BẢO DƯỠNG',
        'LOẠI BẢO DƯỠNG:' || "LOAIBAODUONG",
        'NGƯỜI BẢO DƯỠNG:' || "NGUOIBAODUONG",
        "tenkhuon",
        "SOKHUON",
        'NGƯỜI ĐĂNG KÝ:' || "NGUOIDANGKY",
        "THOIGIANDANGKY",
        "NGUOIDANGKY",
        0
    FROM "DTS_DanhSachDangKyBaoDuong"
    WHERE "THOIGIANKETTHUC" IS NULL;

    -- UPDATE target bảo dưỡng loại A
    UPDATE "DTS_GiaoCa" g
    SET "TARGET_BAODUONG" = d."MTTYPEA_TIME"
    FROM "DTS_Die_Master" d
    WHERE g."PARTNAME" = d."PART_NAME"
      AND g."DIENO"    = d."Die_no"
      AND g."IDBAODUONG" > 0
      AND g.problem  = 'A'
      AND (g."TARGET" = 0 OR g."TARGET" IS NULL);

    -- UPDATE target bảo dưỡng loại B
    UPDATE "DTS_GiaoCa" g
    SET "TARGET_BAODUONG" = d."MTTYPEB_TIME"
    FROM "DTS_Die_Master" d
    WHERE g."PARTNAME" = d."PART_NAME"
      AND g."DIENO"    = d."Die_no"
      AND g."IDBAODUONG" > 0
      AND g.problem  = 'B'
      AND (g."TARGET" = 0 OR g."TARGET" IS NULL);

    -- UPDATE target bảo dưỡng loại C
    UPDATE "DTS_GiaoCa" g
    SET "TARGET_BAODUONG" = d."MTTYPEC_TIME"
    FROM "DTS_Die_Master" d
    WHERE g."PARTNAME" = d."PART_NAME"
      AND g."DIENO"    = d."Die_no"
      AND g."IDBAODUONG" > 0
      AND g.problem  = 'C'
      AND (g."TARGET" = 0 OR g."TARGET" IS NULL);
END;
$function$
```

---

## 6. `DTS_Get_CheckSheet_Master`

```sql
CREATE OR REPLACE FUNCTION public."DTS_Get_CheckSheet_Master"(p_die_name character varying, p_die_no character varying, p_loai_baoduong character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_max_id  int;
    v_id      int := 1;
    v_col_idx int;
    v_giatri  varchar(50);
    v_sql     text;
BEGIN
    -- Reset tất cả giatri về rỗng
    UPDATE "DTS_CheckSheet_Name" SET giatri = '';

    SELECT COUNT(*) INTO v_max_id FROM "DTS_CheckSheet_Name";

    WHILE v_id <= v_max_id LOOP
        -- Tính số cột tùy loại bảo dưỡng (giống SQL Server)
        IF p_loai_baoduong IN ('A', 'B') THEN
            v_col_idx := (v_id - 1) * 3 + 1;
        ELSIF p_loai_baoduong = 'C' THEN
            v_col_idx := (v_id - 1) * 3 + 2;
        ELSE
            v_col_idx := (v_id - 1) * 3 + 3;
        END IF;

        -- Dynamic SQL: đọc giá trị cột số từ DTS_CheckSheet_Master
        -- %I → quote identifier (tên cột là số, ví dụ "1","4"...)
        v_sql := format(
            'SELECT %I FROM "DTS_CheckSheet_Master" WHERE tenkhuon = $1 AND sokhuon = $2',
            v_col_idx::text
        );

        BEGIN
            EXECUTE v_sql INTO v_giatri USING p_die_name, p_die_no;
        EXCEPTION WHEN OTHERS THEN
            v_giatri := '';  -- cột không tồn tại → bỏ qua
        END;

        UPDATE "DTS_CheckSheet_Name"
        SET giatri = COALESCE(v_giatri, '')
        WHERE id = v_id;

        v_id := v_id + 1;
    END LOOP;
END;
$function$
```

---

## 7. `DTS_SP_BANGDAUMAY_NEWVER`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_BANGDAUMAY_NEWVER"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_start_date  date;
    v_end_date    date;
BEGIN
    -- Lấy khoảng ngày từ bảng trouble (NGAY là NVARCHAR trong SQL Server → cast date)
    SELECT MIN(ngay::date) INTO v_start_date
    FROM "DTS_DieTrouble_NewVer" WHERE ngay IS NOT NULL;

    SELECT MAX(ngay::date) INTO v_end_date
    FROM "DTS_DieTrouble_NewVer" WHERE ngay IS NOT NULL;

    -- Tạo bảng tạm: cross join ngày × danh sách die (thay cho WHILE loop)
    DROP TABLE IF EXISTS tmp_tab_bangdaumay;
    CREATE TEMP TABLE tmp_tab_bangdaumay AS
    SELECT
        gs.dt::date          AS dt,
        d."Die_name"         AS diename,
        d."Die_no"           AS dieno
    FROM GENERATE_SERIES(v_start_date, v_end_date, '1 day'::interval) AS gs(dt)
    CROSS JOIN "DTS_Die_Master_NewVer" d;

    -- Xóa dữ liệu cũ và insert tổng hợp theo tuần
    DELETE FROM "DTS_BangDauMay_NewVer";

    INSERT INTO "DTS_BangDauMay_NewVer" (
        "TenKhuon","SoKhuon","Tuan","Thang","Nam","NgayGanNhat",
        "SoLanLoiKhuon","TongThoiGianSua",
        "DiemTongThoiGianSua","DiemSoLanLoiKhuon","DiemDemand","DiemLoiGanNhat"
    )
    SELECT DISTINCT
        main.diename,
        main.dieno,
        main.week_num,
        main.month_num,
        main.year_num,
        lu.ngay,
        lu.cnt,
        lu.sum_t,
        1, 1, 1, 1
    FROM (
        SELECT
            EXTRACT(YEAR  FROM dt)::int  AS year_num,
            EXTRACT(MONTH FROM dt)::int  AS month_num,
            EXTRACT(WEEK  FROM dt)::int  AS week_num,
            diename,
            dieno
        FROM tmp_tab_bangdaumay
        GROUP BY year_num, month_num, week_num, diename, dieno
    ) main
    LEFT JOIN (
        SELECT
            EXTRACT(YEAR  FROM ngay::date)::int  AS year_num,
            EXTRACT(MONTH FROM ngay::date)::int  AS month_num,
            EXTRACT(WEEK  FROM ngay::date)::int  AS week_num,
            MAX(ngay)                            AS ngay,
            COUNT(*)                               AS cnt,
            SUM("thoigiansuagio")                  AS sum_t,
            "TENKHUON",
            "SOKHUON"
        FROM "DTS_DieTrouble_NewVer"
        WHERE ngay IS NOT NULL AND "ANHHUONGDUNGMAY" = 1
        GROUP BY year_num, month_num, week_num, "TENKHUON", "SOKHUON"
    ) lu ON main.year_num = lu.year_num
         AND main.month_num = lu.month_num
         AND main.week_num  = lu.week_num
         AND main.diename   = lu."TENKHUON"
         AND main.dieno     = lu."SOKHUON"
    ORDER BY main.year_num, main.month_num, main.week_num, main.diename, main.dieno;

    -- Cập nhật LOIGANNHAT
    UPDATE "DTS_BangDauMay_NewVer" b
    SET "LOIGANNHAT" = d."LOICHINH"
    FROM "DTS_DieTrouble_NewVer" d
    WHERE b."TENKHUON"   = d."TENKHUON"
      AND b."SOKHUON"    = d."SOKHUON"
      AND b."NGAYGANNHAT" = d.ngay;

    -- Cập nhật DIEMLOIGANNHAT
    UPDATE "DTS_BangDauMay_NewVer" b
    SET "DIEMLOIGANNHAT" = d."DIEM"
    FROM "DTS_Trouble_Master_NewVer" d
    WHERE b."LOIGANNHAT" = d."TENLOI";

    -- Cập nhật DEMAND theo tháng hiện tại
    UPDATE "DTS_BangDauMay_NewVer" b
    SET "DEMAND" = d."DEMAND"
    FROM "DTS_BangDauMay" d
    WHERE b."TENKHUON" = d."TENKHUON"
      AND b."SOKHUON"  = d."SOKHUON"
      AND b."THANG"    = EXTRACT(MONTH FROM CURRENT_DATE)::int;

    UPDATE "DTS_BangDauMay_NewVer" b
    SET "DIEMDEMAND" = d."DEMAND_SCORE"
    FROM "DTS_BangDauMay" d
    WHERE b."TENKHUON" = d."TENKHUON"
      AND b."SOKHUON"  = d."SOKHUON"
      AND b."THANG"    = EXTRACT(MONTH FROM CURRENT_DATE)::int;

    -- Tính điểm số lần lỗi
    UPDATE "DTS_BangDauMay_NewVer" SET "DIEMSOLANLOIKHUON" = 2 WHERE "SOLANLOIKHUON" = 1;
    UPDATE "DTS_BangDauMay_NewVer" SET "DIEMSOLANLOIKHUON" = 3 WHERE "SOLANLOIKHUON" = 2;
    UPDATE "DTS_BangDauMay_NewVer" SET "DIEMSOLANLOIKHUON" = 4 WHERE "SOLANLOIKHUON" > 2;

    -- Tính điểm tổng thời gian sửa
    UPDATE "DTS_BangDauMay_NewVer" SET "DIEMTONGTHOIGIANSUA" = 2
    WHERE "TONGTHOIGIANSUA" >= 0.4 AND "TONGTHOIGIANSUA" < 1;
    UPDATE "DTS_BangDauMay_NewVer" SET "DIEMTONGTHOIGIANSUA" = 3
    WHERE "TONGTHOIGIANSUA" >= 1 AND "TONGTHOIGIANSUA" < 2;
    UPDATE "DTS_BangDauMay_NewVer" SET "DIEMTONGTHOIGIANSUA" = 4
    WHERE "TONGTHOIGIANSUA" >= 2;

    -- Tính tổng điểm
    UPDATE "DTS_BangDauMay_NewVer"
    SET "TONGDIEM" = "DIEMLOIGANNHAT" * "DIEMSOLANLOIKHUON" * "DIEMTONGTHOIGIANSUA";
END;
$function$
```

---

## 8. `DTS_SP_BANGDAUMAY_NEWVER02`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_BANGDAUMAY_NEWVER02"(p_month integer, p_year integer)
 RETURNS TABLE("GROUPPART" text, "MAKHUON" text, "THOIGIANDUNG" double precision, "DIEM1" integer, "SOLANLOI" integer, "THOIGIANSUA" double precision, "DIEM2" integer, "TICH" integer, "THOIGIANDIEOK" double precision, "OPAR" double precision, "MTBF" double precision, "TOTALSHOT" integer, "DIEM3" integer)
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Bảng tổng hợp sự cố die (6 loại sự cố MASC1..MASC6)
    DROP TABLE IF EXISTS tmp_bangsucodie;
    CREATE TEMP TABLE tmp_bangsucodie (
        "NGAY"      text, "MAY" text, "MASP" text, "MAKHUON" text,
        "GROUPPART" text, "CA"  text, "MASUCO" text, "TENSUCO" text,
        "THOIGIAN"  float
    );

    INSERT INTO tmp_bangsucodie
    SELECT b."NGAY",b."MAY",b."MASP",b."MAKHUON",p."GROUPPART",b."CA",
           b."MASC1",b."TENSC1",b."LOSS1"
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE b."MASC1" LIKE '2.%' AND b."MASP" = p.part_no
      AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
      AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
    UNION
    SELECT b."NGAY",b."MAY",b."MASP",b."MAKHUON",p."GROUPPART",b."CA",
           b."MASC2",b."TENSC2",b."LOSS2"
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE b."MASC2" LIKE '2.%' AND b."MASP" = p.part_no
      AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
      AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
    UNION
    SELECT b."NGAY",b."MAY",b."MASP",b."MAKHUON",p."GROUPPART",b."CA",
           b."MASC3",b."TENSC3",b."LOSS3"
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE b."MASC3" LIKE '2.%' AND b."MASP" = p.part_no
      AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
      AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
    UNION
    SELECT b."NGAY",b."MAY",b."MASP",b."MAKHUON",p."GROUPPART",b."CA",
           b."MASC4",b."TENSC4",b."LOSS4"
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE b."MASC4" LIKE '2.%' AND b."MASP" = p.part_no
      AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
      AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
    UNION
    SELECT b."NGAY",b."MAY",b."MASP",b."MAKHUON",p."GROUPPART",b."CA",
           b."MASC5",b."TENSC5",b."LOSS5"
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE b."MASC5" LIKE '2.%' AND b."MASP" = p.part_no
      AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
      AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
    UNION
    SELECT b."NGAY",b."MAY",b."MASP",b."MAKHUON",p."GROUPPART",b."CA",
           b."MASC6",b."TENSC6",b."LOSS6"
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE b."MASC6" LIKE '2.%' AND b."MASP" = p.part_no
      AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
      AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month;

    -- Bảng kết quả trung gian
    DROP TABLE IF EXISTS tmp_ketqua02;
    CREATE TEMP TABLE tmp_ketqua02 (
        "GROUPPART"     text,   "MAKHUON"       text,
        "THOIGIANDUNG"  float,  "DIEM1"         int,
        "SOLANLOI"      int,    "THOIGIANSUA"   float,
        "DIEM2"         int,    "TICH"           int,
        "THOIGIANDIEOK" float,  "OPAR"           float,
        "MTBF"          float,  "TOTALSHOT"      int,
        "DIEM3"         int
    );

    INSERT INTO tmp_ketqua02 ("GROUPPART","MAKHUON","THOIGIANDUNG","DIEM1","SOLANLOI",
        "THOIGIANSUA","DIEM2","TICH","THOIGIANDIEOK","OPAR","MTBF","TOTALSHOT","DIEM3")
    SELECT
        s."GROUPPART",
        s."MAKHUON",
        SUM(s."THOIGIAN"),
        0,
        COUNT(s."MASUCO"),
        ROUND((SUM(s."THOIGIAN") / NULLIF(COUNT(s."MASUCO"),0))::numeric, 2),
        0, 0, 0, 0, 0, 0, 0
    FROM tmp_bangsucodie s
    GROUP BY s."GROUPPART", s."MAKHUON"
    ORDER BY s."GROUPPART";

    -- Cập nhật THOIGIANDIEOK
    UPDATE tmp_ketqua02 k
    SET "THOIGIANDIEOK" = round((a.timeok::numeric)::numeric, 2)
    FROM (
        SELECT p."GROUPPART", p."Die_no" AS "MAKHUON",
               SUM(("OKQTY"::float / b.cavity::float * p.ct::float) / 3600.0) AS timeok
        FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
        WHERE b."MASP" = p.part_no AND b."MAKHUON" = p."Die_no"
          AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
          AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
        GROUP BY p."GROUPPART", p."Die_no"
    ) a
    WHERE k."GROUPPART" = a."GROUPPART" AND k."MAKHUON" = a."MAKHUON";

    -- Cập nhật OPAR
    UPDATE tmp_ketqua02
    SET "OPAR" = CASE
        WHEN ("THOIGIANDIEOK" + "THOIGIANDUNG") > 0
        THEN ROUND(("THOIGIANDIEOK" / ("THOIGIANDIEOK" + "THOIGIANDUNG"))::numeric, 4)
        ELSE 0
    END;

    -- Cập nhật MTBF
    UPDATE tmp_ketqua02 k
    SET "MTBF" = ROUND(((b2.bien1 + k."THOIGIANDUNG" / b2.bien2) / k."SOLANLOI")::numeric, 2)
    FROM (
        SELECT p."GROUPPART", p."Die_no" AS "MAKHUON",
               SUM("OKQTY"::float) / AVG(b.cavity::float)  AS bien1,
               AVG(p.ct::float)                              AS bien2
        FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
        WHERE b."MASP" = p.part_no AND b."MAKHUON" = p."Die_no"
          AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
          AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
          AND p.ct <> 0
        GROUP BY p."GROUPPART", p."Die_no"
    ) b2
    WHERE k."GROUPPART" = b2."GROUPPART" AND k."MAKHUON" = b2."MAKHUON";

    -- Cập nhật TOTALSHOT
    UPDATE tmp_ketqua02 k
    SET "TOTALSHOT" = round((b3.bien1::numeric)::numeric, 2)::int
    FROM (
        SELECT p."GROUPPART", p."Die_no" AS "MAKHUON",
               SUM(("OKQTY"::float + "TOTALNG"::float) / b.cavity::float) AS bien1
        FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
        WHERE b."MASP" = p.part_no AND b."MAKHUON" = p."Die_no"
          AND EXTRACT(YEAR  FROM b."NGAY"::date) = p_year
          AND EXTRACT(MONTH FROM b."NGAY"::date) = p_month
        GROUP BY p."GROUPPART", p."Die_no"
    ) b3
    WHERE k."GROUPPART" = b3."GROUPPART" AND k."MAKHUON" = b3."MAKHUON";

    -- Tính DIEM1, DIEM2, DIEM3
    UPDATE tmp_ketqua02 SET
        "DIEM1" = CASE
            WHEN "OPAR" > 0.995 THEN 1
            WHEN "OPAR" > 0.98  THEN 2
            WHEN "OPAR" > 0.94  THEN 3
            ELSE 4 END,
        "DIEM2" = CASE
            WHEN "MTBF" > 8000 THEN 1
            WHEN "MTBF" > 3000 THEN 2
            WHEN "MTBF" > 1000 THEN 3
            ELSE 4 END,
        "DIEM3" = CASE
            WHEN "TOTALSHOT" < 15000 THEN 1
            WHEN "TOTALSHOT" < 25000 THEN 2
            WHEN "TOTALSHOT" < 35000 THEN 3
            ELSE 4 END;

    UPDATE tmp_ketqua02 SET "TICH" = "DIEM1" * "DIEM2" * "DIEM3";

    -- Insert khuôn mới vào kế hoạch bảo dưỡng
    INSERT INTO "DTS_KeHoachBaoDuong" (tenkhuon, sokhuon, nam)
    SELECT "GROUPPART", "MAKHUON", p_year
    FROM tmp_ketqua02
    WHERE "GROUPPART" || '-' || "MAKHUON" NOT IN (
        SELECT tenkhuon || '-' || sokhuon
        FROM "DTS_KeHoachBaoDuong" WHERE nam = p_year
    );

    RETURN QUERY SELECT * FROM tmp_ketqua02;
END;
$function$
```

---

## 9. `DTS_SP_CREATE_BOARD`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_CREATE_BOARD"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "DTS_MonthlyBoardControl";

    INSERT INTO "DTS_MonthlyBoardControl" (
        model,"tenkhuon","makhuon","sokhuon",
        "TypeA_Plan","TypeB_Plan","TypeC_Plan",
        "Maintain_times","Trouble_times"
    )
    SELECT
        model,"PART_NAME","Part_no","Die_no",
        "MTTYPEA","MTTYPEB","MTTYPEC",
        0, 0
    FROM "DTS_Die_Master";
END;
$function$
```

---

## 10. `DTS_SP_GET_BAODUONG_KETQUA`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_BAODUONG_KETQUA"(p_idbaoduong integer)
 RETURNS TABLE("AB1" text, "AB2" text, "AB3" text, "AB4" text, "AB5" text, "AB6" text, "AB7" text, "AB8" text, "AB9" text, "AB10" text, "AB11" text, "AB12" text, "C1" text, "C2" text, "C3" text, "C4" text, "C5" text, "C6" text, "C7" text, "C8" text, "C9" text, "C10" text, "C11" text, "S1" text, "S2" text)
 LANGUAGE plpgsql
AS $function$
begin
    -- missing source code
end;
$function$
```

---

## 11. `DTS_SP_GET_BAODUONG_THONGTIN`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_BAODUONG_THONGTIN"(p_idbaoduong integer)
 RETURNS TABLE(id integer, "Die_Name" text, "Die_no" text, "MSP" text, "Model" text, cabatdau text, thoigianbatdau timestamp without time zone, thoigianketthuc timestamp without time zone, bda text, bdb text, bdc text, bds text, chitiet text, nguoibaoduong text)
 LANGUAGE plpgsql
AS $function$
begin
    -- missing source code
end;
$function$
```

---

## 12. `DTS_SP_GET_BAVIA_FORM`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_BAVIA_FORM"(p_id integer)
 RETURNS TABLE("Die_name" text, "Die_no" text, ngay date, casua text, vitri text, nguyennhan text, motachitiet text, giaiphaptamthoi text, giaiphaplaudai text, ok integer, ng integer, cxn integer, pic text, ngayduc date, caduc text, hinhanhkhuon text, hinhanhsanpham text)
 LANGUAGE plpgsql
AS $function$
begin
    -- missing source code
end;
$function$
```

---

## 13. `DTS_SP_GET_CHECKSHEET_GHICHU`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_CHECKSHEET_GHICHU"(p_idbaoduong integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text, c14 text, c15 text, c16 text, c17 text, c18 text, c19 text, c20 text, c21 text, c22 text, c23 text, c24 text, c25 text, c26 text, c27 text, c28 text, c29 text, c30 text, c31 text, c32 text, c33 text, c34 text, c35 text, c36 text, c37 text, c38 text, c39 text, c40 text, c41 text, c42 text, c43 text, c44 text, c45 text, c46 text, c47 text, c48 text, c49 text, c50 text, c51 text, c52 text, c53 text, c54 text, c55 text, c56 text, c57 text, c58 text, c59 text, c60 text, c61 text, c62 text, c63 text, c64 text, c65 text)
 LANGUAGE plpgsql
AS $function$
BEGIN RETURN QUERY
SELECT
  MAX(CASE WHEN machecksheet::int=1  THEN ghichu END),MAX(CASE WHEN machecksheet::int=2  THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=3  THEN ghichu END),MAX(CASE WHEN machecksheet::int=4  THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=5  THEN ghichu END),MAX(CASE WHEN machecksheet::int=6  THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=7  THEN ghichu END),MAX(CASE WHEN machecksheet::int=8  THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=9  THEN ghichu END),MAX(CASE WHEN machecksheet::int=10 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=11 THEN ghichu END),MAX(CASE WHEN machecksheet::int=12 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=13 THEN ghichu END),MAX(CASE WHEN machecksheet::int=14 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=15 THEN ghichu END),MAX(CASE WHEN machecksheet::int=16 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=17 THEN ghichu END),MAX(CASE WHEN machecksheet::int=18 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=19 THEN ghichu END),MAX(CASE WHEN machecksheet::int=20 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=21 THEN ghichu END),MAX(CASE WHEN machecksheet::int=22 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=23 THEN ghichu END),MAX(CASE WHEN machecksheet::int=24 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=25 THEN ghichu END),MAX(CASE WHEN machecksheet::int=26 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=27 THEN ghichu END),MAX(CASE WHEN machecksheet::int=28 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=29 THEN ghichu END),MAX(CASE WHEN machecksheet::int=30 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=31 THEN ghichu END),MAX(CASE WHEN machecksheet::int=32 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=33 THEN ghichu END),MAX(CASE WHEN machecksheet::int=34 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=35 THEN ghichu END),MAX(CASE WHEN machecksheet::int=36 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=37 THEN ghichu END),MAX(CASE WHEN machecksheet::int=38 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=39 THEN ghichu END),MAX(CASE WHEN machecksheet::int=40 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=41 THEN ghichu END),MAX(CASE WHEN machecksheet::int=42 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=43 THEN ghichu END),MAX(CASE WHEN machecksheet::int=44 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=45 THEN ghichu END),MAX(CASE WHEN machecksheet::int=46 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=47 THEN ghichu END),MAX(CASE WHEN machecksheet::int=48 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=49 THEN ghichu END),MAX(CASE WHEN machecksheet::int=50 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=51 THEN ghichu END),MAX(CASE WHEN machecksheet::int=52 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=53 THEN ghichu END),MAX(CASE WHEN machecksheet::int=54 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=55 THEN ghichu END),MAX(CASE WHEN machecksheet::int=56 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=57 THEN ghichu END),MAX(CASE WHEN machecksheet::int=58 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=59 THEN ghichu END),MAX(CASE WHEN machecksheet::int=60 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=61 THEN ghichu END),MAX(CASE WHEN machecksheet::int=62 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=63 THEN ghichu END),MAX(CASE WHEN machecksheet::int=64 THEN ghichu END),
  MAX(CASE WHEN machecksheet::int=65 THEN ghichu END)
FROM "DTS_ChiTietBaoDuong_CheckSheet" WHERE idlist = p_idbaoduong;
END; $function$
```

---

## 14. `DTS_SP_GET_CHECKSHEET_LEADER`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_CHECKSHEET_LEADER"()
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text, c14 text, c15 text, c16 text, c17 text, c18 text, c19 text, c20 text, c21 text, c22 text, c23 text, c24 text, c25 text, c26 text, c27 text, c28 text, c29 text, c30 text, c31 text, c32 text, c33 text, c34 text, c35 text, c36 text, c37 text, c38 text, c39 text, c40 text, c41 text, c42 text, c43 text, c44 text, c45 text, c46 text, c47 text, c48 text, c49 text, c50 text, c51 text, c52 text, c53 text, c54 text, c55 text, c56 text, c57 text, c58 text, c59 text, c60 text, c61 text, c62 text, c63 text, c64 text, c65 text)
 LANGUAGE plpgsql
AS $function$
BEGIN RETURN QUERY
SELECT
  MAX(CASE WHEN id=1  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=2  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=3  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=4  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=5  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=6  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=7  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=8  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=9  THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=10 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=11 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=12 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=13 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=14 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=15 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=16 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=17 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=18 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=19 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=20 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=21 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=22 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=23 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=24 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=25 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=26 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=27 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=28 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=29 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=30 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=31 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=32 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=33 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=34 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=35 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=36 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=37 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=38 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=39 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=40 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=41 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=42 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=43 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=44 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=45 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=46 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=47 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=48 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=49 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=50 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=51 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=52 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=53 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=54 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=55 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=56 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=57 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=58 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=59 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=60 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=61 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=62 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=63 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=64 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END),
  MAX(CASE WHEN id=65 THEN CASE WHEN giatri::int=1 THEN 'X' ELSE '' END END)
FROM "DTS_CheckSheet_Name";
END; $function$
```

---

## 15. `DTS_SP_GET_CHECKSHEET_NG`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_CHECKSHEET_NG"(p_idbaoduong integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text, c14 text, c15 text, c16 text, c17 text, c18 text, c19 text, c20 text, c21 text, c22 text, c23 text, c24 text, c25 text, c26 text, c27 text, c28 text, c29 text, c30 text, c31 text, c32 text, c33 text, c34 text, c35 text, c36 text, c37 text, c38 text, c39 text, c40 text, c41 text, c42 text, c43 text, c44 text, c45 text, c46 text, c47 text, c48 text, c49 text, c50 text, c51 text, c52 text, c53 text, c54 text, c55 text, c56 text, c57 text, c58 text, c59 text, c60 text, c61 text, c62 text, c63 text, c64 text, c65 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _cs_pivot(p_idbaoduong, 'NG'); END; $function$
```

---

## 16. `DTS_SP_GET_CHECKSHEET_OK`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_CHECKSHEET_OK"(p_idbaoduong integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text, c14 text, c15 text, c16 text, c17 text, c18 text, c19 text, c20 text, c21 text, c22 text, c23 text, c24 text, c25 text, c26 text, c27 text, c28 text, c29 text, c30 text, c31 text, c32 text, c33 text, c34 text, c35 text, c36 text, c37 text, c38 text, c39 text, c40 text, c41 text, c42 text, c43 text, c44 text, c45 text, c46 text, c47 text, c48 text, c49 text, c50 text, c51 text, c52 text, c53 text, c54 text, c55 text, c56 text, c57 text, c58 text, c59 text, c60 text, c61 text, c62 text, c63 text, c64 text, c65 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _cs_pivot(p_idbaoduong, 'OK'); END; $function$
```

---

## 17. `DTS_SP_GET_CHECKSHEET_PIC`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_CHECKSHEET_PIC"(p_idbaoduong integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text, c14 text, c15 text, c16 text, c17 text, c18 text, c19 text, c20 text, c21 text, c22 text, c23 text, c24 text, c25 text, c26 text, c27 text, c28 text, c29 text, c30 text, c31 text, c32 text, c33 text, c34 text, c35 text, c36 text, c37 text, c38 text, c39 text, c40 text, c41 text, c42 text, c43 text, c44 text, c45 text, c46 text, c47 text, c48 text, c49 text, c50 text, c51 text, c52 text, c53 text, c54 text, c55 text, c56 text, c57 text, c58 text, c59 text, c60 text, c61 text, c62 text, c63 text, c64 text, c65 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _cs_pivot(p_idbaoduong, 'PIC'); END; $function$
```

---

## 18. `DTS_SP_GET_CHECKSHEET_TEMP`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_CHECKSHEET_TEMP"(p_idbaoduong integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text, c14 text, c15 text, c16 text, c17 text, c18 text, c19 text, c20 text, c21 text, c22 text, c23 text, c24 text, c25 text, c26 text, c27 text, c28 text, c29 text, c30 text, c31 text, c32 text, c33 text, c34 text, c35 text, c36 text, c37 text, c38 text, c39 text, c40 text, c41 text, c42 text, c43 text, c44 text, c45 text, c46 text, c47 text, c48 text, c49 text, c50 text, c51 text, c52 text, c53 text, c54 text, c55 text, c56 text, c57 text, c58 text, c59 text, c60 text, c61 text, c62 text, c63 text, c64 text, c65 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _cs_pivot(p_idbaoduong, 'TEMP'); END; $function$
```

---

## 19. `DTS_SP_GET_SC_CHECKSHEET_GHICHU`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_SC_CHECKSHEET_GHICHU"(p_idsuco integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _sc_pivot13(p_idsuco,'GHICHU'); END;
$function$
```

---

## 20. `DTS_SP_GET_SC_CHECKSHEET_NG`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_SC_CHECKSHEET_NG"(p_idsuco integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _sc_pivot13(p_idsuco,'NG'); END;
$function$
```

---

## 21. `DTS_SP_GET_SC_CHECKSHEET_OK`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_SC_CHECKSHEET_OK"(p_idsuco integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _sc_pivot13(p_idsuco,'OK'); END;
$function$
```

---

## 22. `DTS_SP_GET_SC_CHECKSHEET_PIC`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_SC_CHECKSHEET_PIC"(p_idsuco integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _sc_pivot13(p_idsuco,'PIC'); END;
$function$
```

---

## 23. `DTS_SP_GET_SC_CHECKSHEET_TEMP`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_SC_CHECKSHEET_TEMP"(p_idsuco integer)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text)
 LANGUAGE plpgsql
AS $function$ BEGIN RETURN QUERY SELECT * FROM _sc_pivot13(p_idsuco,'TEMP'); END;
$function$
```

---

## 24. `DTS_SP_GET_SUACHUA_THONGTIN`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_GET_SUACHUA_THONGTIN"(p_idsuachua integer)
 RETURNS TABLE(id integer, tenkhuon text, sokhuon text, "MSP" text, "Model" text, cabatdau text, thoigianbatdau timestamp without time zone, thoigianketthuc timestamp without time zone, loi21 text, loi22 text, loi23 text, loi24 text, loi25 text, loi26 text, loi27 text, loi28 text, loi29 text, loi210 text, loi211 text, nguoibatdau text, chitietloi text, nguyennhanchinh text, giaiphaptamthoi text, giaiphaplaudai text, canlamfa text, tusua text, loixayra text, nguoiupdate text)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT
        t.id, t.tenkhuon::text, t."SoKhuon"::text,
        d.barcode::text AS "MSP", d.model::text,
        t.cabatdau::text, t.thoigianbatdau, t.thoigianketthuc,
        CASE WHEN t.loichinh LIKE '%2.1%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.2%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.3%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.4%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.5%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.6%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.7%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.8%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.9%'  THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.10%' THEN 'X' ELSE '' END,
        CASE WHEN t.loichinh LIKE '%2.11%' THEN 'X' ELSE '' END,
        t.nguoibatdau::text, t.chitietloi::text, t.nguyennhanchinh::text,
        t.giaiphaptamthoi::text, t.giaiphaplaudai::text,
        CASE WHEN t.canlamfa = false THEN 'N' ELSE 'Y' END,
        CASE WHEN t.tusua    = false THEN 'N' ELSE 'Y' END,
        CASE WHEN t.loixayra = false THEN 'N' ELSE 'Y' END,
        t.nguoiupdate::text
    FROM "DTS_DieTrouble_NewVer" t
    LEFT JOIN "DTS_Die_Master_NewVer" d
        ON (t.tenkhuon || '-' || t."SoKhuon") = (UPPER(d.die_name) || '-' || d."Die_no")
    WHERE t.id = p_idsuachua;
END;
$function$
```

---

## 25. `DTS_SP_INSERT_DIE_MASTER`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_INSERT_DIE_MASTER"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    INSERT INTO "DTS_Die_Master_NewVer" ("Die_name","Die_no")
    SELECT DISTINCT p.grouppart, p."Die_no"
    FROM "tb_Part_master" p
    WHERE (p.grouppart || p."Die_no") NOT IN (
        SELECT "Die_name" || "Die_no" FROM "DTS_Die_Master_NewVer"
    )
    ORDER BY p.grouppart, p."Die_no" DESC;

    UPDATE "DTS_Die_Master_NewVer" d
    SET "Model"  = p."Model",
        "Cavity" = p."Cavity"
    FROM "tb_Part_master" p
    WHERE d."Die_name" = p.grouppart
      AND d."Die_no"   = p."Die_no";
END;
$function$
```

---

## 26. `DTS_SP_INSERT_DIE_MASTERCHECKSHEET`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_INSERT_DIE_MASTERCHECKSHEET"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    INSERT INTO "DTS_CheckSheet_Master" (tenkhuon, sokhuon)
    SELECT DISTINCT p."Die_name", p."Die_no"
    FROM "DTS_Die_Master_NewVer" p
    WHERE (p."Die_name" || p."Die_no") NOT IN (
        SELECT tenkhuon || sokhuon FROM "DTS_CheckSheet_Master"
    )
    ORDER BY p."Die_name", p."Die_no" DESC;
END;
$function$
```

---

## 27. `DTS_SP_KEHOACHBD_CHART`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_KEHOACHBD_CHART"()
 RETURNS TABLE("Thang" text, "PlanBD" bigint, "ActualBD" bigint)
 LANGUAGE plpgsql
AS $function$
BEGIN
    DROP TABLE IF EXISTS tmp_khbd;
    CREATE TEMP TABLE tmp_khbd (
        "Thang"    int,
        "Nam"      int,
        "PlanBD"   bigint,
        "ActualBD" bigint
    );

    INSERT INTO tmp_khbd ("Thang","Nam","PlanBD")
    SELECT
        EXTRACT(MONTH FROM ngaybaoduong)::int,
        EXTRACT(YEAR  FROM ngaybaoduong)::int,
        COUNT(ngaybaoduong)
    FROM "DTS_KeHoachBD"
    GROUP BY EXTRACT(MONTH FROM ngaybaoduong), EXTRACT(YEAR FROM ngaybaoduong);

    UPDATE tmp_khbd k
    SET "ActualBD" = a.total
    FROM (
        SELECT
            EXTRACT(MONTH FROM thoigianbatdau)::int AS t,
            EXTRACT(YEAR  FROM thoigianbatdau)::int AS y,
            COUNT(thoigianbatdau) AS total
        FROM "DTS_IssueCheckSheetBD"
        GROUP BY EXTRACT(MONTH FROM thoigianbatdau), EXTRACT(YEAR FROM thoigianbatdau)
    ) a
    WHERE a.t = k.thang AND a.y = k.nam;

    RETURN QUERY
    SELECT k.thang::text || '/' || k.nam::text, k."PlanBD", k."ActualBD"
    FROM tmp_khbd k;
END;
$function$
```

---

## 28. `DTS_SP_SHOT_BD_LASTEST`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_SHOT_BD_LASTEST"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "DTS_Die_Master_NewVer" d
    SET
        "SHOTBAODUONG"         = t."SHOT",
        "LOAIBAODUONGGANNHAT"  = t."LOAIBAODUONG",
        "NGAYBAODUONGGANNHAT"  = CASE
            WHEN t."THOIGIANKETTHUC" IS NULL THEN t."THOIGIANBATDAU"
            ELSE t."THOIGIANKETTHUC"
        END
    FROM (
        SELECT *
        FROM "DTS_TimeMaintenance_NewVer"
        WHERE "id" IN (
            SELECT MAX("id")
            FROM "DTS_TimeMaintenance_NewVer"
            GROUP BY "Die_name", "Die_no"
        )
    ) t
    WHERE d."DIE_NAME" = t."DIE_NAME"
      AND d."Die_no"   = t."Die_no";
END;
$function$
```

---

## 29. `DTS_SP_SHOW_CPK`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_SHOW_CPK"()
 RETURNS TABLE(ngayktra timestamp without time zone, mayduc text, grouppart text, sokhuon text, masp text, items text, vitri text, ketquado numeric, cpk numeric, phanloaicpk text)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT DISTINCT
        k.ngayktra, k.mayduc::text,
        p."GroupPart"::text, k."SoKhuon"::text, k."Masp"::text,
        k.items::text, k.vitri::text,
        k.ketquado, round((k.cpk::numeric)::numeric, 3), k.phanloaicpk::text
    FROM tb_chitietkqdo k, "tb_Part_master" p
    WHERE k."Masp" = p.part_no
      AND k.phanloaicpk IN ('S', 'A')
      AND p."GroupPart" IN (
          SELECT "DIE_NAME" FROM "DTS_Die_Master_NewVer"
          WHERE "CpkShow" = 'YES'
            AND p."GroupPart" || '-' || CAST(k.items AS char(25)) IN (
                SELECT groupname || '-' || CAST(items AS char(25))
                FROM "tb_HangMucDoNewVer" WHERE showpck = 'YES'
            )
      )
    ORDER BY k.ngayktra DESC;
END;
$function$
```

---

## 30. `DTS_SP_STAT_TROUBLE_DIE_BY_DIE`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_STAT_TROUBLE_DIE_BY_DIE"(p_thang integer, p_nam integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "DTS_DIE_HEALTH";
    DELETE FROM "DTS_DIE_HEALTH_TEMP_DATA";

    -- Insert thống kê sự cố theo tháng
    INSERT INTO "DTS_DIE_HEALTH_TEMP_DATA" ("tenkhuon","sokhuon","thang","solan")
    SELECT
        "tenkhuon",
        "sokhuon",
        TO_CHAR("ngay"::date, 'YYYY-MM') AS "THANG",
        COUNT(*)                          AS "SOLAN"
    FROM "DTS_DieTrouble"
    WHERE "xulytrenmay" = 0
      AND "LOIKHUON"    = 1
      AND EXTRACT(MONTH FROM "ngay"::date)::int <= p_thang
      AND EXTRACT(YEAR  FROM "ngay"::date)::int  = p_nam
    GROUP BY "tenkhuon", "sokhuon", TO_CHAR("ngay"::date, 'YYYY-MM')
    ORDER BY TO_CHAR("ngay"::date, 'YYYY-MM');

    -- Seed DTS_DIE_HEALTH với tất cả die từ master, điểm mặc định = 0
    INSERT INTO "DTS_DIE_HEALTH" (
        "tenkhuon","sokhuon",
        "_1","_2","_3","_4","_5","_6",
        "_7","_8","_9","_10","_11","_12"
    )
    SELECT "PART_NAME","Die_no", 0,0,0,0,0,0,0,0,0,0,0,0
    FROM "DTS_Die_Master";
END;
$function$
```

---

## 31. `DTS_SP_TIME_MT`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_TIME_MT"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    INSERT INTO "DTS_GroupMT_Time" ("groupmt")
    SELECT DISTINCT d."GroupMT"
    FROM "DTS_Die_Master_NewVer" d
    WHERE d."GroupMT" > 0
      AND d."GroupMT" NOT IN (SELECT "groupmt" FROM "DTS_GroupMT_Time");
END;
$function$
```

---

## 32. `DTS_SP_UPDATE_LASTPRODUCTIONBTK`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_UPDATE_LASTPRODUCTIONBTK"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Thay WHILE loop bằng 1 UPDATE dùng subquery:
    -- Tìm ngày sản xuất gần nhất trong năm hiện tại cho từng die
    UPDATE "DTS_Die_Master_NewVer" dm
    SET "LastProductionDate" = latest.ngay
    FROM (
        SELECT DISTINCT ON (p."Die_name" || '-' || p."Die_no")
            p."Die_name" || '-' || p."Die_no"  AS diename_dieno,
            pd.ngay
        FROM "PLAN_Dandory" pd
        JOIN "tb_Part_master" p
          ON pd.khuonha = p."Part_no" || '-' || p."Die_no"
        WHERE EXTRACT(YEAR FROM pd.ngay::date)::int = EXTRACT(YEAR FROM CURRENT_DATE)::int
        ORDER BY diename_dieno, pd.ngay DESC
    ) latest
    WHERE (dm.die_name || '-' || dm."Die_no") = latest.diename_dieno;
END;
$function$
```

---

## 33. `DTS_SP_UPDATE_MATERCHECKSHEET`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_UPDATE_MATERCHECKSHEET"(p_idmaster integer, p_groupbaoduong character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_id     int := 1;
    v_giatri text;
BEGIN
    -- Cập nhật groupbaoduong cho row idmaster
    UPDATE "DTS_CheckSheet_Master"
    SET groupbaoduong = p_groupbaoduong
    WHERE id = p_idmaster;

    WHILE v_id <= 195 LOOP
        IF p_groupbaoduong <> '' THEN
            -- Lấy giá trị cột [v_id] từ row có groupbaoduong khớp
            BEGIN
                EXECUTE format(
                    'SELECT %I FROM "DTS_CheckSheet_Master" WHERE groupbaoduong = $1 LIMIT 1',
                    v_id::text
                ) INTO v_giatri USING p_groupbaoduong;
            EXCEPTION WHEN OTHERS THEN
                v_giatri := '';
            END;

            -- Copy giá trị đó vào cột [v_id] của row idmaster
            EXECUTE format(
                'UPDATE "DTS_CheckSheet_Master" SET %I = $1 WHERE id = $2',
                v_id::text
            ) USING COALESCE(v_giatri, ''), p_idmaster;
        ELSE
            -- Xóa trắng cột [v_id] của row idmaster
            EXECUTE format(
                'UPDATE "DTS_CheckSheet_Master" SET %I = '''' WHERE id = $1',
                v_id::text
            ) USING p_idmaster;
        END IF;

        v_id := v_id + 1;
    END LOOP;
END;
$function$
```

---

## 34. `DTS_SP_UPDATE_TANSUATLOI`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_UPDATE_TANSUATLOI"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Update LOI (tần suất cao nhất) và TANSUATLOI
    UPDATE "DTS_BangDauMay" b
    SET "LOI"        = t."LOICHINH",
        "TANSUATLOI" = t."TANSUAT"
    FROM (
        SELECT "tenkhuon","sokhuon","loichinh",
               COUNT(*) AS "TANSUAT"
        FROM "DTS_DieTrouble_NewVer"
        WHERE "LOIKHUON" = 1
        GROUP BY "tenkhuon","sokhuon","loichinh"
    ) t
    WHERE b."tenkhuon" = t."tenkhuon"
      AND b."sokhuon"  = t."sokhuon";

    -- Update điểm tần suất lỗi
    UPDATE "DTS_BangDauMay" b
    SET "TANSUATLOI_SCORE" = d."DIEM"
    FROM "DTS_Trouble_Master_NewVer" d
    WHERE b."LOI" = d."TENLOI";

    -- Update LOIGANNHAT (lỗi gần nhất = MAX ID per die+loichinh)
    UPDATE "DTS_BangDauMay" b
    SET "LOIGANNHAT" = t."LOICHINH"
    FROM (
        SELECT "tenkhuon","sokhuon","loichinh"
        FROM "DTS_DieTrouble_NewVer"
        WHERE "ID" IN (
            SELECT MAX("ID")
            FROM "DTS_DieTrouble_NewVer"
            GROUP BY "tenkhuon","sokhuon","loichinh"
        )
    ) t
    WHERE b."tenkhuon" = t."tenkhuon"
      AND b."sokhuon"  = t."sokhuon";

    -- Update điểm lỗi gần nhất
    UPDATE "DTS_BangDauMay" b
    SET "LOIGANNHAT_SCORE" = d."DIEM"
    FROM "DTS_Trouble_Master_NewVer" d
    WHERE b."LOIGANNHAT" = d."TENLOI";

    -- Tính tổng điểm
    UPDATE "DTS_BangDauMay"
    SET "TOTAL_SCORE" = "LOIGANNHAT_SCORE" * "TANSUATLOI_SCORE" * "DEMAND_SCORE";
END;
$function$
```

---

## 35. `DTS_SP_UPDATE_TOTAL_SHOT`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_UPDATE_TOTAL_SHOT"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Log bảng tạm để debug (tương đương Select * from #TEMP_DIEMASTER trong proc gốc)
    -- Bỏ qua SELECT * từ temp trong PG; nếu cần debug, dùng RAISE NOTICE

    -- Cập nhật TotalShotHienTai bằng 1 UPDATE duy nhất thay cho WHILE loop
    UPDATE "DTS_Die_Master_NewVer" dm
    SET "TotalShotHienTai" = COALESCE(dm."SoShotBanDau", 0)
                           + round((COALESCE(shot.bien1)::numeric, 0)::numeric, 2)::int
    FROM (
        SELECT
            p.grouppart || '-' || p."Die_no"       AS diename_dieno,
            SUM(("okqty"::float + "totalNG"::float)
                / NULLIF(b.cavity::float, 0))       AS bien1
        FROM "PLAN_BCSX_OK" b
        JOIN "tb_Part_master" p
          ON b.masp    = p."Part_no"
         AND b.makhuon = p."Die_no"
        JOIN "DTS_Die_Master_NewVer" dm2
          ON (p.grouppart || '-' || p."Die_no") = (dm2."Die_name" || '-' || dm2."Die_no")
         AND dm2."SoShotBanDau" > 0
        WHERE b.ngay::date >= dm2."NgaySoShotBanDau"::date
        GROUP BY diename_dieno
    ) shot
    WHERE (dm."Die_name" || '-' || dm."Die_no") = shot.diename_dieno
      AND dm."SoShotBanDau" > 0;
END;
$function$
```

---

## 36. `DTS_SP_UPDATE_TOTAL_SHOT_ON_RECORD`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SP_UPDATE_TOTAL_SHOT_ON_RECORD"(p_diename_dieno text)
 RETURNS TABLE("TotalShotHienTai" numeric)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_ngay_ban_dau  timestamp;
    v_so_shot_ban_dau int;
BEGIN
    SELECT "NgaySoShotBanDau", "SoShotBanDau"
    INTO v_ngay_ban_dau, v_so_shot_ban_dau
    FROM "DTS_Die_Master_NewVer"
    WHERE "Die_name" || '-' || "Die_no" = p_diename_dieno;

    RETURN QUERY
    SELECT (v_so_shot_ban_dau
            + COALESCE(SUM(("OKQTY"::float + "TOTALNG"::float)
                          / NULLIF(b.cavity::float, 0)), 0))::numeric
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE b."MASP"    = p.part_no
      AND b."MAKHUON" = p."Die_no"
      AND b."NGAY"::date >= v_ngay_ban_dau::date
      AND (p."GROUPPART" || '-' || p."Die_no") = p_diename_dieno;
END;
$function$
```

---

## 37. `DTS_SummarySCK`

```sql
CREATE OR REPLACE FUNCTION public."DTS_SummarySCK"(p_ca character varying, p_grouppart character varying, p_vebieudo integer, p_from character varying, p_to character varying)
 RETURNS SETOF refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    cur refcursor := 'result';
    v_cols text;
    v_query text;
BEGIN
    -- Tạo bảng tạm sự cố die (UNION 7 loại masc)
    DROP TABLE IF EXISTS tmp_bangsucodie_sck;
    CREATE TEMP TABLE tmp_bangsucodie_sck AS
    SELECT ngay,may,masp,makhuon,grouppart,ca,masc1 AS masuco,tensc1 AS tensuco,loss1::float AS thoigian
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE masc1 LIKE '2.%' AND b."Masp" = p.part_no
    UNION
    SELECT ngay,may,masp,makhuon,grouppart,ca,masc2,tensc2,loss2::float
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE masc2 LIKE '2.%' AND b."Masp" = p.part_no
    UNION
    SELECT ngay,may,masp,makhuon,grouppart,ca,masc3,tensc3,loss3::float
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE masc3 LIKE '2.%' AND b."Masp" = p.part_no
    UNION
    SELECT ngay,may,masp,makhuon,grouppart,ca,masc4,tensc4,loss4::float
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE masc4 LIKE '2.%' AND b."Masp" = p.part_no
    UNION
    SELECT ngay,may,masp,makhuon,grouppart,ca,masc5,tensc5,loss5::float
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE masc5 LIKE '2.%' AND b."Masp" = p.part_no
    UNION
    SELECT ngay,may,masp,makhuon,grouppart,ca,masc6,tensc6,loss6::float
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE masc6 LIKE '2.%' AND b."Masp" = p.part_no
    UNION
    SELECT ngay,may,masp,makhuon,grouppart,ca,mascdieuchinhauto,tenscdieuchinhauto,lossscdieuchinhauto::float
    FROM "PLAN_BCSX_OK" b, "tb_Part_master" p
    WHERE mascdieuchinhauto LIKE '2.%' AND b."Masp" = p.part_no;

    IF p_vebieudo = 0 THEN
        -- Nhánh đơn giản: SELECT GROUP BY với filter
        IF p_grouppart <> '' AND p_ca <> '' THEN
            v_query := format($q$
                SELECT ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco,SUM(thoigian)
                FROM tmp_bangsucodie_sck
                WHERE ngay >= %L AND ngay <= %L AND grouppart = %L AND ca = %L
                GROUP BY ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco
                ORDER BY ngay ASC
            $q$, p_from, p_to, p_grouppart, p_ca);
        ELSIF p_grouppart = '' AND p_ca <> '' THEN
            v_query := format($q$
                SELECT ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco,SUM(thoigian)
                FROM tmp_bangsucodie_sck
                WHERE ngay >= %L AND ngay <= %L AND ca = %L
                GROUP BY ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco
                ORDER BY ngay ASC
            $q$, p_from, p_to, p_ca);
        ELSIF p_grouppart <> '' AND p_ca = '' THEN
            v_query := format($q$
                SELECT ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco,SUM(thoigian)
                FROM tmp_bangsucodie_sck
                WHERE ngay >= %L AND ngay <= %L AND grouppart = %L
                GROUP BY ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco
                ORDER BY ngay ASC
            $q$, p_from, p_to, p_grouppart);
        ELSE
            v_query := format($q$
                SELECT ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco,SUM(thoigian)
                FROM tmp_bangsucodie_sck
                WHERE ngay >= %L AND ngay <= %L
                GROUP BY ngay,may,masp,makhuon,grouppart,ca,masuco,tensuco
                ORDER BY ngay ASC
            $q$, p_from, p_to);
        END IF;

    ELSE
        -- Nhánh vebieudo=1: dynamic PIVOT theo tensuco từ PLAN_LOI
        -- Lấy tên cột (tenloi) từ PLAN_LOI maloi LIKE '2%'
        SELECT string_agg(
            format('MAX(CASE WHEN tensuco=%L THEN total END) AS %I', tenloi, tenloi),
            ',' ORDER BY tenloi
        )
        INTO v_cols
        FROM (SELECT DISTINCT tenloi FROM "PLAN_LOI" WHERE maloi LIKE '2%') t;

        -- Build filter cho subquery
        DECLARE v_filter text;
        BEGIN
            IF p_grouppart <> '' AND p_ca <> '' THEN
                v_filter := format('WHERE ngay >= %L AND ngay <= %L AND grouppart = %L AND ca = %L',
                    p_from, p_to, p_grouppart, p_ca);
            ELSIF p_grouppart = '' AND p_ca <> '' THEN
                v_filter := format('WHERE ngay >= %L AND ngay <= %L AND ca = %L', p_from, p_to, p_ca);
            ELSIF p_grouppart <> '' AND p_ca = '' THEN
                v_filter := format('WHERE ngay >= %L AND ngay <= %L AND grouppart = %L', p_from, p_to, p_grouppart);
            ELSE
                v_filter := format('WHERE ngay >= %L AND ngay <= %L', p_from, p_to);
            END IF;
        END;

        v_query := format($q$
            SELECT ngay, %s
            FROM (
                SELECT ngay, tensuco, SUM(thoigian) AS total
                FROM tmp_bangsucodie_sck %s
                GROUP BY ngay, tensuco
            ) src
            GROUP BY ngay ORDER BY ngay
        $q$, v_cols, v_filter);
    END IF;

    OPEN cur FOR EXECUTE v_query;
    RETURN NEXT cur;
END;
$function$
```

---

## 38. `DTS_TANSUAT_BD`

```sql
CREATE OR REPLACE FUNCTION public."DTS_TANSUAT_BD"(p_year integer, p_thang1 integer, p_thang2 integer, p_thang3 integer, p_namtruoc integer)
 RETURNS TABLE("GROUPPART" text, "MAKHUON" text, "THOIGIANDUNG" double precision, "DIEM1" integer, "SOLANLOI" integer, "THOIGIANSUA" double precision, "DIEM2" integer, "TICH" integer, "THOIGIANDIEOK" double precision, "OPAR" double precision, "MTBF" double precision, "TOTALSHOT" integer, "DIEM3" integer, "NAM" integer, "THANG" integer)
 LANGUAGE plpgsql
AS $function$
begin
    -- missing source code
end;
$function$
```

---

## 39. `DeleteDataMeasure`

```sql
CREATE OR REPLACE FUNCTION public."DeleteDataMeasure"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_maxid int;
BEGIN
    SELECT MAX(id) INTO v_maxid FROM tb_chitietkqdo;

    INSERT INTO tb_chitietkqdo_Deleted310824 (
        id, nguoiktra, ngayktra, casx, mayduc, masp, tensp, sokhuon, hinhthuc,
        items, vitri, dungcudo, gioihantren, gioihanduoi, gioihantrendrw, gioihanduoidrw,
        gioihantrenfa, gioihanduoifa, gioihantrenmp, gioihanduoimp, hinhanh, ketquado,
        ketquadanhgia, comment, idlistketquado, Cavity, congthuc,
        shot1, shot2, shot3, shot4, shot5, stt, ketquadanhgiaMP,
        deltamiddrw, xuhuong, canhbaotanggiam, canhbaosailech,
        ngtype, groupname, gioihantrenfathamkhao, gioihanduoifathamkhao,
        danhgiafathamkhao, loaikt, timedelete
    )
    SELECT
        id, nguoiktra, ngayktra, casx, mayduc, masp, tensp, sokhuon, hinhthuc,
        items, vitri, dungcudo, gioihantren, gioihanduoi, gioihantrendrw, gioihanduoidrw,
        gioihantrenfa, gioihanduoifa, gioihantrenmp, gioihanduoimp, hinhanh, ketquado,
        ketquadanhgia, comment, idlistketquado, Cavity, congthuc,
        shot1, shot2, shot3, shot4, shot5, stt, ketquadanhgiaMP,
        deltamiddrw, xuhuong, canhbaotanggiam, canhbaosailech,
        ngtype, groupname, gioihantrenfathamkhao, gioihanduoifathamkhao,
        danhgiafathamkhao, loaikt,
        CURRENT_TIMESTAMP          -- timedelete = GETDATE()
    FROM tb_chitietkqdo
    WHERE id < (v_maxid - 2000000);

    DELETE FROM tb_chitietkqdo WHERE id < (v_maxid - 2000000);
END;
$function$
```

---

## 40. `ENG_ADD_MAY_PART`

```sql
CREATE OR REPLACE FUNCTION public."ENG_ADD_MAY_PART"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- INSERT máy chưa có (phần máy mẫu với part cố định)
    -- SQL Server: vòng lặp qua từng máy, check IF NOT EXISTS rồi INSERT
    -- PG: dùng INSERT ... WHERE NOT EXISTS (set-based, nhanh hơn nhiều)
    INSERT INTO "ENG_CycleTime" (Part_no, Die_no, model, part_name, may, loaimay, cycletime, status_show)
    SELECT 'QC3-4470', 'V3', 'B32', 'OP Middle L01', i."Mayduc", i."Loaimay", 0, 1
    FROM "IMM" i
    WHERE NOT EXISTS (
        SELECT 1 FROM "ENG_CycleTime" e WHERE e.may = i."Mayduc"
    );

    -- INSERT part chưa có (default may='A01', loaimay='450T')
    INSERT INTO "ENG_CycleTime" (Part_no, Die_no, model, part_name, may, loaimay, cycletime, status_show)
    SELECT p.part_no, UPPER(p."Die_no"), p.model, p."Part_name", 'A01', '450T', 0, 1
    FROM "tb_Part_master" p
    WHERE NOT EXISTS (
        SELECT 1 FROM "ENG_CycleTime" e
        WHERE e.Part_no = p.part_no
          AND e.Die_no  = UPPER(p."Die_no")
    );
END;
$function$
```

---

## 41. `ENG_UPDATE_CYCLE_FATVP`

```sql
CREATE OR REPLACE FUNCTION public."ENG_UPDATE_CYCLE_FATVP"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- CTE lấy bản ghi mới nhất từ [FA-TVP] theo ROW_NUMBER
    -- Thay WHILE loop bằng UPDATE + INSERT set-based
    WITH fa_data AS (
        SELECT "Ngay_gui", "Mayduc", "Part_no", "Die_no", "Cycle_time",
               ROW_NUMBER() OVER (PARTITION BY "ID" ORDER BY "Ngay_gui" DESC) AS rn
        FROM "FA-TVP"
        WHERE "Ketqua" = 'OK'
          AND "Mayduc" IS NOT NULL
          AND EXTRACT(YEAR FROM "Ngay_gui") = 2020
    ),
    fa_latest AS (
        SELECT "Ngay_gui", "Mayduc", "Part_no", "Die_no", "Cycle_time"
        FROM fa_data WHERE rn = 1
    )
    -- Cập nhật các record đã tồn tại
    UPDATE "ENG_CycleTime" e
    SET cycletime = f."CYCLE_TIME"
    FROM fa_latest f
    JOIN "IMM" i       ON i.mayduc   = f."Mayduc"
    JOIN "tb_Part_master" p ON p.part_no = f.part_no
    WHERE e.may      = f."Mayduc"
      AND e.Part_no  = f.part_no
      AND e.Die_no   = f."Die_no";

    -- Insert record chưa tồn tại
    WITH fa_data AS (
        SELECT "Ngay_gui", "Mayduc", "Part_no", "Die_no", "Cycle_time",
               ROW_NUMBER() OVER (PARTITION BY "ID" ORDER BY "Ngay_gui" DESC) AS rn
        FROM "FA-TVP"
        WHERE "Ketqua" = 'OK'
          AND "Mayduc" IS NOT NULL
          AND EXTRACT(YEAR FROM "Ngay_gui") = 2020
    ),
    fa_latest AS (
        SELECT "Mayduc", "Part_no", "Die_no", "Cycle_time"
        FROM fa_data WHERE rn = 1
    )
    INSERT INTO "ENG_CycleTime" (may, Part_no, Die_no, cycletime, status_show, loaimay, part_name, model)
    SELECT f."Mayduc", f.part_no, f."Die_no", f."CYCLE_TIME", 1,
           i.loaimay, p."Part_name", p.model
    FROM fa_latest f
    LEFT JOIN "IMM" i            ON i.mayduc   = f."Mayduc"
    LEFT JOIN "tb_Part_master" p ON p.part_no  = f.part_no
    WHERE NOT EXISTS (
        SELECT 1 FROM "ENG_CycleTime" e
        WHERE e.may     = f."Mayduc"
          AND e.Part_no = f.part_no
          AND e.Die_no  = f."Die_no"
    );
END;
$function$
```

---

## 42. `ENG_UPDATE_CYCLE_FROM_BTK`

```sql
CREATE OR REPLACE FUNCTION public."ENG_UPDATE_CYCLE_FROM_BTK"(p_remind integer)
 RETURNS TABLE(part_no text, die_no text, model text, part_name text, "A14" numeric, "A13" numeric, "A12" numeric, "A11" numeric, "D01" numeric, "D02" numeric, "D03" numeric, "D04" numeric, "D05" numeric, "D06" numeric, "D07" numeric, "D08" numeric, "D09" numeric, "D10" numeric, "D11" numeric, "D12" numeric, "D13" numeric, "A10" numeric, "A09" numeric, "D15" numeric, "D16" numeric, "B06" numeric, "B07" numeric, "A05" numeric, "A06" numeric, "A08" numeric, "B08" numeric, "A03" numeric, "A04" numeric, "A01" numeric, "A02" numeric, "A07" numeric, "C08" numeric, "C09" numeric, "C03" numeric, "C02" numeric, "C01" numeric, "C07" numeric, "C06" numeric, "C05" numeric, "C04" numeric, "B10" numeric, "B09" numeric, "B01" numeric, "B02" numeric, "B03" numeric, "B4A" numeric, "B4B" numeric, "C12" numeric, "C13" numeric, "A15" numeric, "B05" numeric, "C10" numeric, "C11" numeric, "D14" numeric, "D17" numeric)
 LANGUAGE plpgsql
AS $function$
-- Macro tạo SUM(CASE WHEN may=X THEN val END) AS X cho 58 máy
-- Thứ tự giống SQL Server để đảm bảo tương thích
BEGIN
    IF p_remind = 1 THEN
        -- Cập nhật SOLANDANDORI từ PLAN_DANDORY (5 tháng gần nhất)
        UPDATE "ENG_CycleTime" ct
        SET "SOLANDANDORI" = a.solan
        FROM (
            SELECT "MAY", "KHUONLAP", COUNT("MAY") AS solan
            FROM "PLAN_Dandory"
            WHERE "NGAY" >= CURRENT_TIMESTAMP - INTERVAL '5 months'
              AND "KHUONLAP" IS NOT NULL AND "KHUONLAP" <> '' AND "KHUONLAP" <> '0'
              AND "MAY" IS NOT NULL
            GROUP BY "MAY", "KHUONLAP"
        ) a
        WHERE (ct.Part_no || '-' || ct.Die_no) = a."KHUONLAP"
          AND ct.may = a."MAY"
          AND ct."CycleTime" IS NOT NULL;

        UPDATE "ENG_CycleTime"
        SET "SOLANDANDORI" = 0
        WHERE cycletime IS NOT NULL AND "SOLANDANDORI" IS NULL;

        -- PIVOT SOLANDANDORI=0 (máy chưa có dandori)
        RETURN QUERY
        SELECT
            Part_no::text, Die_no::text, model::text, part_name::text,
            SUM(CASE WHEN may='A14' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A13' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A12' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A11' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D01' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D02' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D03' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D04' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D05' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D06' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D07' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D08' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D09' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D10' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D11' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D12' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D13' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A10' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A09' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D15' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D16' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B06' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B07' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A05' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A06' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A08' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B08' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A03' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A04' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A01' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A02' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A07' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C08' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C09' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C03' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C02' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C01' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C07' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C06' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C05' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C04' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B10' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B09' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B01' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B02' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B03' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B4A' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B4B' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C12' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C13' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='A15' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='B05' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C10' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='C11' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D14' AND "SOLANDANDORI"=0 THEN cycletime END),
            SUM(CASE WHEN may='D17' AND "SOLANDANDORI"=0 THEN cycletime END)
        FROM "ENG_CycleTime"
        GROUP BY Part_no, Die_no, model, part_name
        ORDER BY Part_no;

    ELSE
        -- PIVOT toàn bộ ENG_CYCLETIME
        RETURN QUERY
        SELECT
            Part_no::text, Die_no::text, model::text, part_name::text,
            SUM(CASE WHEN may='A14' THEN cycletime END),
            SUM(CASE WHEN may='A13' THEN cycletime END),
            SUM(CASE WHEN may='A12' THEN cycletime END),
            SUM(CASE WHEN may='A11' THEN cycletime END),
            SUM(CASE WHEN may='D01' THEN cycletime END),
            SUM(CASE WHEN may='D02' THEN cycletime END),
            SUM(CASE WHEN may='D03' THEN cycletime END),
            SUM(CASE WHEN may='D04' THEN cycletime END),
            SUM(CASE WHEN may='D05' THEN cycletime END),
            SUM(CASE WHEN may='D06' THEN cycletime END),
            SUM(CASE WHEN may='D07' THEN cycletime END),
            SUM(CASE WHEN may='D08' THEN cycletime END),
            SUM(CASE WHEN may='D09' THEN cycletime END),
            SUM(CASE WHEN may='D10' THEN cycletime END),
            SUM(CASE WHEN may='D11' THEN cycletime END),
            SUM(CASE WHEN may='D12' THEN cycletime END),
            SUM(CASE WHEN may='D13' THEN cycletime END),
            SUM(CASE WHEN may='A10' THEN cycletime END),
            SUM(CASE WHEN may='A09' THEN cycletime END),
            SUM(CASE WHEN may='D15' THEN cycletime END),
            SUM(CASE WHEN may='D16' THEN cycletime END),
            SUM(CASE WHEN may='B06' THEN cycletime END),
            SUM(CASE WHEN may='B07' THEN cycletime END),
            SUM(CASE WHEN may='A05' THEN cycletime END),
            SUM(CASE WHEN may='A06' THEN cycletime END),
            SUM(CASE WHEN may='A08' THEN cycletime END),
            SUM(CASE WHEN may='B08' THEN cycletime END),
            SUM(CASE WHEN may='A03' THEN cycletime END),
            SUM(CASE WHEN may='A04' THEN cycletime END),
            SUM(CASE WHEN may='A01' THEN cycletime END),
            SUM(CASE WHEN may='A02' THEN cycletime END),
            SUM(CASE WHEN may='A07' THEN cycletime END),
            SUM(CASE WHEN may='C08' THEN cycletime END),
            SUM(CASE WHEN may='C09' THEN cycletime END),
            SUM(CASE WHEN may='C03' THEN cycletime END),
            SUM(CASE WHEN may='C02' THEN cycletime END),
            SUM(CASE WHEN may='C01' THEN cycletime END),
            SUM(CASE WHEN may='C07' THEN cycletime END),
            SUM(CASE WHEN may='C06' THEN cycletime END),
            SUM(CASE WHEN may='C05' THEN cycletime END),
            SUM(CASE WHEN may='C04' THEN cycletime END),
            SUM(CASE WHEN may='B10' THEN cycletime END),
            SUM(CASE WHEN may='B09' THEN cycletime END),
            SUM(CASE WHEN may='B01' THEN cycletime END),
            SUM(CASE WHEN may='B02' THEN cycletime END),
            SUM(CASE WHEN may='B03' THEN cycletime END),
            SUM(CASE WHEN may='B4A' THEN cycletime END),
            SUM(CASE WHEN may='B4B' THEN cycletime END),
            SUM(CASE WHEN may='C12' THEN cycletime END),
            SUM(CASE WHEN may='C13' THEN cycletime END),
            SUM(CASE WHEN may='A15' THEN cycletime END),
            SUM(CASE WHEN may='B05' THEN cycletime END),
            SUM(CASE WHEN may='C10' THEN cycletime END),
            SUM(CASE WHEN may='C11' THEN cycletime END),
            SUM(CASE WHEN may='D14' THEN cycletime END),
            SUM(CASE WHEN may='D17' THEN cycletime END)
        FROM "ENG_CycleTime"
        GROUP BY Part_no, Die_no, model, part_name
        ORDER BY Part_no;
    END IF;
END;
$function$
```

---

## 43. `ENG_UPDATE_CYCLE_TIME`

```sql
CREATE OR REPLACE FUNCTION public."ENG_UPDATE_CYCLE_TIME"(p_may character varying, p_partno character varying, p_dieno character varying, p_cycletime character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    EXECUTE format(
        'UPDATE "ENG_CT" SET %I = $1 WHERE Part_no = $2 AND Die_no = $3',
        p_may
    ) USING p_cycletime::float, p_partno, p_dieno;
END;
$function$
```

---

## 44. `ENG_UPDATE_CYCLE_TO_IMM`

```sql
CREATE OR REPLACE FUNCTION public."ENG_UPDATE_CYCLE_TO_IMM"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "IMM" i
    SET "Cycletimestd" = a.cycletime
    FROM (
        SELECT Part_no, Die_no, may, cycletime
        FROM "ENG_CycleTime"
    ) a
    WHERE a.Part_no = LEFT(i."Run", 8)
      AND a.Die_no  = LTRIM(SUBSTRING(i."Run" FROM 9 FOR 3))
      AND a.may     = i."Mayduc";

    UPDATE "IMM"
    SET "DIFF" = "CYCLETIMEFROMJIG" - "Cycletimestd";
END;
$function$
```

---

## 45. `KPI_INDEX`

```sql
CREATE OR REPLACE FUNCTION public."KPI_INDEX"(p_parameter integer)
 RETURNS SETOF refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    cur      refcursor := 'result';
    v_cols   text;    -- danh sách tên cột máy (QUOTENAME → quote_ident)
    v_hieu   text;    -- biểu thức COALESCE cho từng máy
    v_query  text;
BEGIN
    IF p_parameter = 1 THEN
        -- Tạo danh sách cột động từ IMM.Mayduc
        SELECT
            string_agg(quote_ident("Mayduc"), ',' ORDER BY "Mayduc"),
            string_agg('COALESCE(' || quote_ident("Mayduc") || ', 0) AS ' || quote_ident("Mayduc"), ',' ORDER BY "Mayduc")
        INTO v_cols, v_hieu
        FROM (
            SELECT DISTINCT "Mayduc" FROM "IMM"
            WHERE "Mayduc" <> ''
              AND LEFT("Mayduc",1) NOT IN ('I','T')
              AND "Mayduc" <> 'CUM'
        ) t;

        v_query := format($q$
            SELECT
                ROW_NUMBER() OVER (ORDER BY "Ngay") AS id,
                "Ngay",
                %s
            FROM (
                SELECT ngay AS "Ngay", may,
                    CASE WHEN wt = 0 THEN 0
                         ELSE ROUND(((okqty * ct / 3600.0 / Cavity) / wt)::numeric, 2)
                    END AS "Tong"
                FROM "PLAN_BCSX_OK"
                WHERE EXTRACT(YEAR FROM ngay) = 2025
            ) s
            PIVOT_PLACEHOLDER
            ORDER BY "Ngay" ASC
        $q$, v_hieu);

        -- Dùng crosstab-style với conditional aggregation
        v_query := format($q$
            SELECT
                ROW_NUMBER() OVER (ORDER BY "Ngay")::bigint AS id,
                "Ngay",
                %s
            FROM (
                SELECT ngay AS "Ngay", may,
                    CASE WHEN wt = 0 THEN 0
                         ELSE ROUND(((okqty * ct / 3600.0 / NULLIF(Cavity,0)) / NULLIF(wt,0))::numeric, 2)
                    END AS "Tong"
                FROM "PLAN_BCSX_OK"
                WHERE EXTRACT(YEAR FROM ngay) = 2025
            ) s
            GROUP BY "Ngay"
            ORDER BY "Ngay" ASC
        $q$,
        (SELECT string_agg(
            'COALESCE(SUM(CASE WHEN may=' || quote_literal(m) || ' THEN "Tong" END),0) AS ' || quote_ident(m),
            ',' ORDER BY m)
         FROM (SELECT DISTINCT "Mayduc" AS m FROM "IMM" WHERE "Mayduc" <> '' AND LEFT("Mayduc",1) NOT IN ('I','T') AND "Mayduc" <> 'CUM') t)
        );

        OPEN cur FOR EXECUTE v_query;
        RETURN NEXT cur;
    END IF;
END;
$function$
```

---

## 46. `KPI_INDEX_RV`

```sql
CREATE OR REPLACE FUNCTION public."KPI_INDEX_RV"()
 RETURNS TABLE(thang integer, nam integer, loaimay text, okparttime double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    -- Theo từng loại máy
    SELECT
        EXTRACT(MONTH FROM ngay)::int,
        EXTRACT(YEAR  FROM ngay)::int,
        loaimay::text,
        SUM(okqty::float * ct::float / 3600.0 / NULLIF(Cavity::float, 0))
    FROM "PLAN_BCSX_OK"
    WHERE EXTRACT(YEAR FROM ngay) = 2025 AND Cavity > 0
    GROUP BY EXTRACT(MONTH FROM ngay), EXTRACT(YEAR FROM ngay), loaimay

    UNION ALL

    -- Tổng tất cả ('All')
    SELECT
        EXTRACT(MONTH FROM ngay)::int,
        EXTRACT(YEAR  FROM ngay)::int,
        'All'::text,
        SUM(okqty::float * ct::float / 3600.0 / NULLIF(Cavity::float, 0))
    FROM "PLAN_BCSX_OK"
    WHERE EXTRACT(YEAR FROM ngay) = 2025 AND Cavity > 0
    GROUP BY EXTRACT(MONTH FROM ngay), EXTRACT(YEAR FROM ngay)

    ORDER BY 1, 2, 3;
END;
$function$
```

---

## 47. `PLAN_BCSX_ENG`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_BCSX_ENG"(p_from timestamp without time zone, p_to timestamp without time zone)
 RETURNS TABLE(id integer, ngay date, ca character varying, may character varying, loaimay character varying, masp character varying, makhuon character varying, tensanpham character varying, okqty integer, wtokqty double precision, ct double precision, cavity integer, wt double precision, qckeep integer, wtqckeep double precision, sanphambo integer, wtsanphambo double precision, qty1 integer, maloi1 character varying, tenloi1 character varying, groupmaloi1 character varying, wtmaloi1 double precision, qty2 integer, maloi2 character varying, tenloi2 character varying, groupmaloi2 character varying, wtmaloi2 double precision, qty3 integer, maloi3 character varying, tenloi3 character varying, groupmaloi3 character varying, wtmaloi3 double precision, qty4 integer, maloi4 character varying, tenloi4 character varying, groupmaloi4 character varying, wtmaloi4 double precision, qty5 integer, maloi5 character varying, tenloi5 character varying, groupmaloi5 character varying, wtmaloi5 double precision, qty6 integer, maloi6 character varying, tenloi6 character varying, groupmaloi6 character varying, wtmaloi6 double precision, totalng integer, wtng double precision, ngayupdate timestamp without time zone, nguoiupdate character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    WITH base AS (
        SELECT
            ROW_NUMBER() OVER ()::int AS seq_id,
            b.ngay, b.ca, b.may, b.loaimay, b."Masp", b.makhuon, b.tensanpham,
            b.okqty, b.wtokqty, b.ct, b.Cavity, b.wt,
            b.qckeep, b.wtqckeep, b.sanphambo, b.wtsanphambo,
            b.qty1, b.maloi1, b.tenloi1, b.groupmaloi1, b.wtmaloi1,
            b.qty2, b.maloi2, b.tenloi2, b.groupmaloi2, b.wtmaloi2,
            b.qty3, b.maloi3, b.tenloi3, b.groupmaloi3, b.wtmaloi3,
            b.qty4, b.maloi4, b.tenloi4, b.groupmaloi4, b.wtmaloi4,
            b.qty5, b.maloi5, b.tenloi5, b.groupmaloi5, b.wtmaloi5,
            b.qty6, b.maloi6, b.tenloi6, b.groupmaloi6, b.wtmaloi6,
            b.totalNG, b.wtNG, b.ngayupdate, b.nguoiupdate,
            t.tyle
        FROM "PLAN_BCSX_OK" b
        LEFT JOIN "ENG_TyLe" t
          ON EXTRACT(MONTH FROM b.ngay)::int = t.thang
         AND EXTRACT(YEAR  FROM b.ngay)::int = t.nam
        WHERE b.ngay >= p_from AND b.ngay <= p_to
    )
    SELECT
        seq_id,
        ngay::date, ca, may, loaimay, masp, makhuon, tensanpham,
        okqty, wtokqty, ct, Cavity, wt, qckeep, wtqckeep, sanphambo, wtsanphambo,
        CASE WHEN tyle IS NOT NULL THEN ROUND((qty1 / tyle::float))::int ELSE qty1 END,
        maloi1,tenloi1,groupmaloi1,
        CASE WHEN tyle IS NOT NULL THEN wtmaloi1 / tyle::float ELSE wtmaloi1 END,
        CASE WHEN tyle IS NOT NULL THEN ROUND((qty2 / tyle::float))::int ELSE qty2 END,
        maloi2,tenloi2,groupmaloi2,
        CASE WHEN tyle IS NOT NULL THEN wtmaloi2 / tyle::float ELSE wtmaloi2 END,
        CASE WHEN tyle IS NOT NULL THEN ROUND((qty3 / tyle::float))::int ELSE qty3 END,
        maloi3,tenloi3,groupmaloi3,
        CASE WHEN tyle IS NOT NULL THEN wtmaloi3 / tyle::float ELSE wtmaloi3 END,
        CASE WHEN tyle IS NOT NULL THEN ROUND((qty4 / tyle::float))::int ELSE qty4 END,
        maloi4,tenloi4,groupmaloi4,
        CASE WHEN tyle IS NOT NULL THEN wtmaloi4 / tyle::float ELSE wtmaloi4 END,
        CASE WHEN tyle IS NOT NULL THEN ROUND((qty5 / tyle::float))::int ELSE qty5 END,
        maloi5,tenloi5,groupmaloi5,
        CASE WHEN tyle IS NOT NULL THEN wtmaloi5 / tyle::float ELSE wtmaloi5 END,
        CASE WHEN tyle IS NOT NULL THEN ROUND((qty6 / tyle::float))::int ELSE qty6 END,
        maloi6,tenloi6,groupmaloi6,
        CASE WHEN tyle IS NOT NULL THEN wtmaloi6 / tyle::float ELSE wtmaloi6 END,
        CASE WHEN tyle IS NOT NULL THEN ROUND((totalNG / tyle::float))::int ELSE totalNG END,
        CASE WHEN tyle IS NOT NULL THEN wtNG / tyle::float ELSE wtNG END,
        ngayupdate, nguoiupdate
    FROM base;
END;
$function$
```

---

## 48. `PLAN_BLANCE_BCSX`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_BLANCE_BCSX"(p_from character varying, p_to character varying)
 RETURNS SETOF refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    cur    refcursor := 'result';
    v_cols text;
    v_hieu text;
    v_query text;
BEGIN
    SELECT
        string_agg(quote_ident("Mayduc"), ',' ORDER BY "Mayduc"),
        string_agg(
            'COALESCE(24 - COALESCE(' || quote_ident("Mayduc") || ',0),0) AS ' || quote_ident("Mayduc"),
            ',' ORDER BY "Mayduc"
        )
    INTO v_cols, v_hieu
    FROM (
        SELECT DISTINCT "Mayduc" FROM "IMM"
        WHERE "Mayduc" <> ''
          AND LEFT("Mayduc",1) NOT IN ('I','T')
          AND "Mayduc" <> 'CUM'
    ) t;

    v_query := format($q$
        SELECT ngay, %s
        FROM (
            SELECT ngay,
                   %s
            FROM (
                SELECT ngay,
                       may,
                       SUM(((okqty+qckeep+sanphambo+qty1+qty2+qty3+qty4+qty5+qty6)::float*ct/NULLIF(Cavity,0)/3600)
                           +loss1+loss2+loss3+loss4+loss5+loss6
                           +lossscdieuchinhauto+lossscdieuchinhauto2+lossscdieuchinhauto3+lossscdieuchinhauto4) AS tong
                FROM "PLAN_BCSX_OK"
                WHERE ngay >= %L AND ngay <= %L
                GROUP BY ngay, may

                UNION ALL

                SELECT date AS ngay, 'A01' AS may, 0 AS tong
                FROM "hr_btypeshift_momsddb"
                WHERE a+b+c > 0 AND date >= %L AND date <= %L
            ) raw
            GROUP BY ngay, may
        ) src
        GROUP BY ngay
        ORDER BY ngay
    $q$,
        v_hieu,
        (SELECT string_agg(
            'SUM(CASE WHEN may=' || quote_literal(m) || ' THEN tong END) AS ' || quote_ident(m),
            ',' ORDER BY m)
         FROM (SELECT DISTINCT "Mayduc" AS m FROM "IMM" WHERE "Mayduc" <> ' ' AND LEFT("Mayduc",1) NOT IN ('I','T') AND "Mayduc" <> 'CUM') t),
        p_from, p_to, p_from, p_to
    );

    OPEN cur FOR EXECUTE v_query;
    RETURN NEXT cur;
END;
$function$
```

---

## 49. `PLAN_BLANCE_BCSX_BLOCK`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_BLANCE_BCSX_BLOCK"(p_from character varying, p_to character varying)
 RETURNS SETOF refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    cur    refcursor := 'result';
    v_hieu text;
    v_query text;
BEGIN
    SELECT string_agg(
        'COALESCE(24 - COALESCE(' || quote_ident(m) || ',0),0) AS ' || quote_ident(m),
        ',' ORDER BY m)
    INTO v_hieu
    FROM (SELECT DISTINCT "Mayduc" AS m FROM "IMM"
          WHERE LEFT("Mayduc",1) NOT IN ('I','T') AND "Mayduc" <> 'CUM') t;

    v_query := format($q$
        SELECT ngay, %s
        FROM (
            SELECT ngay,
                   %s
            FROM (
                SELECT ngay, may, SUM(total) AS tong
                FROM (
                    SELECT ngay, may, losstimeinput::float/60.0 AS total
                    FROM "PLAN_BCSX_BL_LOSS"
                    WHERE ngay >= %L AND ngay <= %L

                    UNION ALL

                    SELECT ngay, may,
                        (okactualinput+qcgiu+ng+spbo+spdc+fa+boauto)::float
                        * cycletime / NULLIF(Cavity,0) / 3600.0 AS total
                    FROM "PLAN_BCSX_BL_OK"
                    WHERE Cavity > 0 AND ngay >= %L AND ngay <= %L
                ) a
                GROUP BY ngay, may

                UNION ALL

                SELECT date AS ngay, 'A01' AS may, 0 AS total
                FROM "hr_btypeshift_momsddb"
                WHERE a+b+c > 0 AND date >= %L AND date <= %L
            ) src
            GROUP BY ngay, may
        ) piv
        GROUP BY ngay
        ORDER BY ngay
    $q$,
        v_hieu,
        (SELECT string_agg(
            'SUM(CASE WHEN may=' || quote_literal(m) || ' THEN tong END) AS ' || quote_ident(m),
            ',' ORDER BY m)
         FROM (SELECT DISTINCT "Mayduc" AS m FROM "IMM"
               WHERE LEFT("Mayduc",1) NOT IN ('I','T') AND "Mayduc" <> 'CUM') t),
        p_from, p_to, p_from, p_to, p_from, p_to
    );

    OPEN cur FOR EXECUTE v_query;
    RETURN NEXT cur;
END;
$function$
```

---

## 50. `PLAN_CHECK_BTK_KHSX`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_CHECK_BTK_KHSX"(p_ngay date)
 RETURNS TABLE(id integer, mayduc text, khsx text, btk text, status text)
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Cleanup dữ liệu PLAN_COMPARE
    DELETE FROM "PLAN_COMPARE"
    WHERE ngay <> p_ngay
       OR LENGTH(part_kh) < 10
       OR (machine_kh NOT LIKE '%A%' AND machine_kh NOT LIKE '%B%'
           AND machine_kh NOT LIKE '%C%' AND machine_kh NOT LIKE '%D%');

    UPDATE "PLAN_COMPARE"
    SET machine_kh = LEFT(machine_kh,1) || '0' || RIGHT(machine_kh,1)
    WHERE LENGTH(machine_kh) = 2;

    -- Tạo bảng kết quả temp
    DROP TABLE IF EXISTS tmp_check_btk;
    CREATE TEMP TABLE tmp_check_btk (
        id int GENERATED ALWAYS AS IDENTITY,
        mayduc text,
        khsx text,
        btk  text,
        status text
    );

    INSERT INTO tmp_check_btk (mayduc)
    SELECT "Mayduc" FROM "IMM" ORDER BY "Id";

    -- Build chuỗi KHSX (khuôn từ PLAN_COMPARE, ghép part_kh liên tiếp)
    UPDATE tmp_check_btk t
    SET khsx = sub.cols
    FROM (
        SELECT machine_kh,
               string_agg(part_kh, ',' ORDER BY id) || ',' AS cols
        FROM "PLAN_COMPARE"
        WHERE ngay = p_ngay
        GROUP BY machine_kh
    ) sub
    WHERE t.mayduc = sub.machine_kh;

    -- Build chuỗi BTK (khuôn từ PLAN_Dandory, ghép khuonha+khuonlap)
    UPDATE tmp_check_btk t
    SET btk = sub.cols
    FROM (
        SELECT may,
               string_agg(khuonha || ',' || khuonlap, ',' ORDER BY id) || ',' AS cols
        FROM "PLAN_Dandory"
        WHERE ngay = p_ngay AND status <> true
        GROUP BY may
    ) sub
    WHERE t.mayduc = sub.may;

    -- Đánh giá OK/NG
    UPDATE tmp_check_btk
    SET status = CASE WHEN COALESCE(khsx,'') = COALESCE(btk,'') THEN 'OK' ELSE 'NG' END;

    RETURN QUERY SELECT * FROM tmp_check_btk;
END;
$function$
```

---

## 51. `PLAN_ChartIndexAll`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ChartIndexAll"(p_thang character varying, p_nam character varying)
 RETURNS TABLE(ngay text, dailymain double precision, noplan double precision, spng double precision, preparation double precision, okpart double precision, sucodie double precision, adjust double precision, scchatluong double precision, dandori double precision, dandoritrouble double precision, dungkhongghiloi double precision, sucomayduc double precision, sanphambo double precision, monthlymain double precision, sanphamqc double precision, other double precision, extratime double precision, scautomation double precision, ploss double precision, mloss double precision, mor double precision, opar double precision, mpr double precision)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cols  text;
    v_query text;
BEGIN
    -- Lấy danh sách cột phanloai (loại trừ 'Sản phẩm điều chỉnh')
    SELECT string_agg(quote_ident(phanloai), ',' ORDER BY phanloai)
    INTO v_cols
    FROM (SELECT DISTINCT phanloai FROM "PLAN_DataChart"
          WHERE phanloai <> N'Sản phẩm điều chỉnh') t;

    -- Giả sử 18 cột cố định (tên mapping từ SQL Server)
    -- Dùng temp table để tính toán
    DROP TABLE IF EXISTS tmp_chart_all;
    CREATE TEMP TABLE tmp_chart_all (
        ngay text, dailymain float, noplan float, spng float,
        preparation float, okpart float, qckeep float, sucodie float,
        scchatluong float, adjust float, dandori float,
        dandoritrouble float, dungkhongghiloi float, sucomayduc float,
        sanphambo float, monthlymain float, other float,
        extratime float, scautomation float,
        ploss float, mloss float, extratimetotal float
    );

    -- Insert theo ngày
    v_query := format($q$
        INSERT INTO tmp_chart_all (ngay,dailymain,noplan,spng,preparation,okpart,qckeep,
            sucodie,scchatluong,adjust,dandori,dandoritrouble,dungkhongghiloi,
            sucomayduc,sanphambo,monthlymain,other,extratime,scautomation)
        SELECT ngay::text,
            COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng định kỳ' THEN total END),0) AS dailymain,
            COALESCE(SUM(CASE WHEN phanloai='No Plan' THEN total END),0) AS noplan,
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm NG' THEN total END),0) AS spng,
            COALESCE(SUM(CASE WHEN phanloai='Preparation' THEN total END),0) AS preparation,
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm OK' THEN total END),0) AS okpart,
            COALESCE(SUM(CASE WHEN phanloai='SP QC Giữ' THEN total END),0) AS qckeep,
            COALESCE(SUM(CASE WHEN phanloai='Sự cố khuôn' THEN total END),0) AS sucodie,
            COALESCE(SUM(CASE WHEN phanloai='Sự cố chất lượng' THEN total END),0) AS scchatluong,
            COALESCE(SUM(CASE WHEN phanloai='Điều chỉnh' THEN total END),0) AS adjust,
            COALESCE(SUM(CASE WHEN phanloai='Dandori' THEN total END),0) AS dandori,
            COALESCE(SUM(CASE WHEN phanloai='Dandori sự cố' THEN total END),0) AS dandoritrouble,
            COALESCE(SUM(CASE WHEN phanloai='Dừng không ghi lỗi' THEN total END),0) AS dungkhongghiloi,
            COALESCE(SUM(CASE WHEN phanloai='Sự cố máy đúc' THEN total END),0) AS sucomayduc,
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm bỏ' THEN total END),0) AS sanphambo,
            COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng tháng' THEN total END),0) AS monthlymain,
            COALESCE(SUM(CASE WHEN phanloai='Khác' THEN total END),0) AS other,
            COALESCE(SUM(CASE WHEN phanloai='Tăng ca' THEN total END),0) AS extratime,
            COALESCE(SUM(CASE WHEN phanloai='Sự cố automation' THEN total END),0) AS scautomation
        FROM (
            SELECT ngay, phanloai, COALESCE(SUM(thoigian),0) AS total
            FROM "PLAN_DataChart"
            WHERE EXTRACT(MONTH FROM ngay)::int = %s::int
              AND EXTRACT(YEAR  FROM ngay)::int = %s::int
            GROUP BY ngay, phanloai
        ) x
        GROUP BY ngay
    $q$, p_thang, p_nam);
    EXECUTE v_query;

    -- Insert row 'Total'
    INSERT INTO tmp_chart_all (ngay,dailymain,noplan,spng,preparation,okpart,qckeep,
        sucodie,scchatluong,adjust,dandori,dandoritrouble,dungkhongghiloi,
        sucomayduc,sanphambo,monthlymain,other,extratime,scautomation)
    SELECT 'Total',
        COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng định kỳ'   THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='No Plan'              THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Sản phẩm NG'          THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Preparation'          THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Sản phẩm OK'          THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='SP QC Giữ'            THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Sự cố khuôn'          THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Sự cố chất lượng'     THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Điều chỉnh'           THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Dandori'              THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Dandori sự cố'        THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Dừng không ghi lỗi'  THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Sự cố máy đúc'        THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Sản phẩm bỏ'          THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng tháng'      THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Khác'                 THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Tăng ca'              THEN thoigian END),0),
        COALESCE(SUM(CASE WHEN phanloai='Sự cố automation'     THEN thoigian END),0)
    FROM "PLAN_DataChart"
    WHERE EXTRACT(MONTH FROM ngay)::int = p_thang::int
      AND EXTRACT(YEAR  FROM ngay)::int = p_nam::int;

    -- Tính ploss, mloss, extratimetotal
    UPDATE tmp_chart_all SET
        ploss = COALESCE(spng,0)+COALESCE(sucodie,0)+COALESCE(sucomayduc,0)+COALESCE(scchatluong,0)
               +COALESCE(scautomation,0)+COALESCE(dungkhongghiloi,0)+COALESCE(dandori,0)
               +COALESCE(dandoritrouble,0)+COALESCE(dailymain,0)+COALESCE(preparation,0),
        mloss = COALESCE(monthlymain,0)+COALESCE(noplan,0)+COALESCE(other,0)+COALESCE(adjust,0),
        extratimetotal = COALESCE(extratime,0)+COALESCE(sanphambo,0)+COALESCE(qckeep,0);

    RETURN QUERY
    SELECT t.ngay,
        t.dailymain, t.noplan, t.spng, t.preparation, t.okpart, t.sucodie,
        t.adjust, t.scchatluong, t.dandori, t.dandoritrouble, t.dungkhongghiloi,
        t.sucomayduc, t.sanphambo, t.monthlymain, t.qckeep, t.other, t.extratime, t.scautomation,
        t.ploss, t.mloss,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0   THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss-COALESCE(t.scautomation,0)-COALESCE(t.scchatluong,0)
                  -COALESCE(t.dandori,0)-COALESCE(t.dandoritrouble,0)
                  -COALESCE(t.dailymain,0)-COALESCE(t.preparation,0)=0
             THEN 0
             ELSE ROUND((t.okpart/(t.okpart+t.ploss-COALESCE(t.scautomation,0)-COALESCE(t.scchatluong,0)
                  -COALESCE(t.dandori,0)-COALESCE(t.dandoritrouble,0)
                  -COALESCE(t.dailymain,0)-COALESCE(t.preparation,0)))::numeric,3)::float
        END
    FROM tmp_chart_all t;
END;
$function$
```

---

## 52. `PLAN_ChartIndexAllBlock`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ChartIndexAllBlock"(p_thang character varying, p_nam character varying)
 RETURNS TABLE(ngay text, dailymain double precision, noplan double precision, spng double precision, preparation double precision, okpart double precision, sucodie double precision, adjust double precision, scchatluong double precision, dandori double precision, dandoritrouble double precision, dungkhongghiloi double precision, sucomayduc double precision, sanphambo double precision, monthlymain double precision, sanphamqc double precision, other double precision, extratime double precision, scautomation double precision, ploss double precision, mloss double precision, mor double precision, opar double precision, mpr double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT t.ngay,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dailymain/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.noplan/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.spng/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.preparation/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.sucodie/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.adjust/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.scchatluong/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dandori/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dandoritrouble/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dungkhongghiloi/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.sucomayduc/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.sanphambo/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.monthlymain/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.qckeep/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.other/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.extratime/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.scautomation/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.ploss/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.mloss/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss-COALESCE(t.scautomation,0)-COALESCE(t.scchatluong,0)-COALESCE(t.dandori,0)
                  -COALESCE(t.dandoritrouble,0)-COALESCE(t.dailymain,0)-COALESCE(t.preparation,0)=0 THEN 0
             ELSE ROUND((t.okpart/(t.okpart+t.ploss-COALESCE(t.scautomation,0)-COALESCE(t.scchatluong,0)
                  -COALESCE(t.dandori,0)-COALESCE(t.dandoritrouble,0)-COALESCE(t.dailymain,0)-COALESCE(t.preparation,0)))::numeric,3)::float
        END
    FROM "_chart_index_block_base"('PLAN_DataChartBlock', p_thang, p_nam) t;
END;
$function$
```

---

## 53. `PLAN_ChartIndexAllBlock2`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ChartIndexAllBlock2"(p_thang character varying, p_nam character varying)
 RETURNS TABLE(ngay text, dailymain double precision, noplan double precision, spng double precision, preparation double precision, okpart double precision, sucodie double precision, adjust double precision, scchatluong double precision, dandori double precision, dandoritrouble double precision, dungkhongghiloi double precision, sucomayduc double precision, sanphambo double precision, monthlymain double precision, sanphamqc double precision, other double precision, extratime double precision, scautomation double precision, ploss double precision, mloss double precision, mor double precision, opar double precision, mpr double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT t.ngay,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dailymain/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.noplan/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.spng/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.preparation/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.sucodie/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.adjust/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.scchatluong/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dandori/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dandoritrouble/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dungkhongghiloi/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.sucomayduc/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.sanphambo/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.monthlymain/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.qckeep/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.other/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.extratime/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.scautomation/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.ploss/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.mloss/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0
             ELSE ROUND((t.okpart/(t.okpart+t.ploss-COALESCE(t.scautomation,0)-COALESCE(t.scchatluong,0)
                       -COALESCE(t.dandori,0)-COALESCE(t.dandoritrouble,0)-COALESCE(t.dailymain,0)-COALESCE(t.preparation,0)))::numeric,3)::float
        END
    FROM "_chart_index_block_base"('PLAN_DataChartBlock', p_thang, p_nam) t;
END;
$function$
```

---

## 54. `PLAN_ChartIndexLoaiMay`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ChartIndexLoaiMay"(p_thang character varying, p_nam character varying, p_loaimay character varying)
 RETURNS TABLE(ngay text, dailymain double precision, noplan double precision, spng double precision, preparation double precision, okpart double precision, sucodie double precision, adjust double precision, scchatluong double precision, dandori double precision, dandoritrouble double precision, dungkhongghiloi double precision, sucomayduc double precision, sanphambo double precision, monthlymain double precision, sanphamqc double precision, other double precision, extratime double precision, scautomation double precision, ploss double precision, mloss double precision, mor double precision, opar double precision, mpr double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN RETURN QUERY SELECT * FROM "PLAN_ChartIndexAll"(p_thang, p_nam); END; $function$
```

---

## 55. `PLAN_ChartIndexLoaiMayBlock`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ChartIndexLoaiMayBlock"(p_thang character varying, p_nam character varying, p_loaimay character varying)
 RETURNS TABLE(ngay text, dailymain double precision, noplan double precision, spng double precision, preparation double precision, okpart double precision, sucodie double precision, adjust double precision, scchatluong double precision, dandori double precision, dandoritrouble double precision, dungkhongghiloi double precision, sucomayduc double precision, sanphambo double precision, monthlymain double precision, sanphamqc double precision, other double precision, extratime double precision, scautomation double precision, ploss double precision, mloss double precision, mor double precision, opar double precision, mpr double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT t.ngay,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dailymain/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.noplan/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.spng/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.preparation/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.sucodie/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.adjust/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.scchatluong/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dandori/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dandoritrouble/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.dungkhongghiloi/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.sucomayduc/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.sanphambo/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.monthlymain/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.qckeep/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.other/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.extratime/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.scautomation/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.ploss/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.mloss/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss+t.mloss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss+t.mloss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0
             ELSE ROUND((t.okpart/(t.okpart+t.ploss-COALESCE(t.scautomation,0)-COALESCE(t.scchatluong,0)
                  -COALESCE(t.dandori,0)-COALESCE(t.dandoritrouble,0)-COALESCE(t.dailymain,0)-COALESCE(t.preparation,0)))::numeric,3)::float
        END
    FROM "_chart_index_block_base"('PLAN_DataChartBlock', p_thang, p_nam, p_loaimay) t;
END;
$function$
```

---

## 56. `PLAN_ChartIndexLoaiMayBlock2`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ChartIndexLoaiMayBlock2"(p_thang character varying, p_nam character varying, p_loaimay character varying)
 RETURNS TABLE(ngay text, dailymain double precision, noplan double precision, spng double precision, preparation double precision, okpart double precision, sucodie double precision, adjust double precision, scchatluong double precision, dandori double precision, dandoritrouble double precision, dungkhongghiloi double precision, sucomayduc double precision, sanphambo double precision, monthlymain double precision, sanphamqc double precision, other double precision, extratime double precision, scautomation double precision, ploss double precision, mloss double precision, mor double precision, opar double precision, mpr double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT t.ngay,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dailymain/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.noplan/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.spng/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.preparation/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.sucodie/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.adjust/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.scchatluong/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dandori/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dandoritrouble/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.dungkhongghiloi/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.sucomayduc/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.sanphambo/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.monthlymain/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.qckeep/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.other/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.extratime/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.scautomation/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.ploss/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.mloss/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0 ELSE ROUND((t.okpart/(t.okpart+t.ploss))::numeric,3)::float END,
        CASE WHEN t.okpart+t.ploss=0 THEN 0
             ELSE ROUND((t.okpart/(t.okpart+t.ploss-COALESCE(t.scautomation,0)-COALESCE(t.scchatluong,0)
                  -COALESCE(t.dandori,0)-COALESCE(t.dandoritrouble,0)-COALESCE(t.dailymain,0)-COALESCE(t.preparation,0)))::numeric,3)::float
        END
    FROM "_chart_index_block_base"('PLAN_DataChartBlock', p_thang, p_nam, p_loaimay) t;
END;
$function$
```

---

## 57. `PLAN_ChartIndexRework`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ChartIndexRework"(p_thang integer, p_nam integer, p_value integer)
 RETURNS SETOF refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    cur refcursor := 'result';
BEGIN
    IF p_value = 0 THEN
        OPEN cur FOR
        -- ⚠️ BC_HISTORYIN, BC_UserLogin, BC_PARTINFO, BC_HISTORYWAIT từ BARCODE_CONTROL
        -- cần fdw schema barcode_control. Thay prefix [BARCODE_CONTROL].[dbo]. → barcode_control.
        SELECT t."ID", t."ID_Scan", h."Partno", h.machine, h."Palet_so",
               h."TimeStamps" AS input_time, h."Shift_prod", h."QtyIn" AS qtyIn_pcs,
               e.lastname || ' ' || e.firstname AS input_by,
               h."Type_Reason", h."Reason_Wait",
               NULLIF(t."Date_change",'')::date AS date_change,
               t."Shift_change", t."Pic_change",
               t."Remain_wait", t."OK_Wait", t."NG_Wait",
               t."Confirm_ReasonNG", t."Confirm_ReasonOK", t."Confirm_by",
               t."Confirm_date"::date
        FROM barcode_control."BC_HISTORYIN" h
        JOIN barcode_control."BC_UserLogin" e ON h."PICNo" = e.username
        JOIN barcode_control."BC_PARTINFO"  p ON p.partno = h.partno
        JOIN barcode_control."BC_HISTORYWAIT" t ON h."ID"::text = t."ID_Scan"
        WHERE EXTRACT(MONTH FROM t."Confirm_date")::int = p_thang
          AND EXTRACT(YEAR  FROM t."Confirm_date")::int = p_nam;
    ELSE
        OPEN cur FOR
        SELECT t."Confirm_date"::date AS ngay,
               SUM(t."OK_Wait") AS ok_wait_pcs,
               SUM(t."NG_Wait") AS ng_wait_pcs,
               SUM(t."OK_Wait") + SUM(t."NG_Wait") AS total
        FROM barcode_control."BC_HISTORYIN" h
        JOIN barcode_control."BC_UserLogin" e ON h."PICNo" = e.username
        JOIN barcode_control."BC_PARTINFO"  p ON p.partno = h.partno
        JOIN barcode_control."BC_HISTORYWAIT" t ON h."ID"::text = t."ID_Scan"
        WHERE EXTRACT(MONTH FROM t."Confirm_date")::int = p_thang
          AND EXTRACT(YEAR  FROM t."Confirm_date")::int = p_nam
        GROUP BY t."Confirm_date"::date;
    END IF;
    RETURN NEXT cur;
END;
$function$
```

---

## 58. `PLAN_CountKhoiDong`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_CountKhoiDong"(p_ngay date, p_ca text)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "PLAN_KhoiDongCount" SET plana=0, planb=0, planc=0, plane=0;

    UPDATE "PLAN_KhoiDongCount" k
    SET plana = a.sl
    FROM (SELECT gio, COUNT(gio) AS sl FROM "PLAN_KhoiDong"
          WHERE ngay=p_ngay AND ca=p_ca AND LEFT(may,1)='A'
          GROUP BY gio) a
    WHERE k.time = a.gio;

    UPDATE "PLAN_KhoiDongCount" k
    SET planb = a.sl
    FROM (SELECT gio, COUNT(gio) AS sl FROM "PLAN_KhoiDong"
          WHERE ngay=p_ngay AND ca=p_ca AND LEFT(may,1)='B'
          GROUP BY gio) a
    WHERE k.time = a.gio;

    UPDATE "PLAN_KhoiDongCount" k
    SET planc = a.sl
    FROM (SELECT gio, COUNT(gio) AS sl FROM "PLAN_KhoiDong"
          WHERE ngay=p_ngay AND ca=p_ca AND LEFT(may,1)='C'
          GROUP BY gio) a
    WHERE k.time = a.gio;

    UPDATE "PLAN_KhoiDongCount" k
    SET plane = a.sl
    FROM (SELECT gio, COUNT(gio) AS sl FROM "PLAN_KhoiDong"
          WHERE ngay=p_ngay AND ca=p_ca AND LEFT(may,1)='E'
          GROUP BY gio) a
    WHERE k.time = a.gio;
END;
$function$
```

---

## 59. `PLAN_Detail_Code`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_Detail_Code"(p_loaitk character varying, p_loaimay character varying, p_from timestamp without time zone, p_to timestamp without time zone)
 RETURNS SETOF refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    cur    refcursor := 'result';
    v_cols text;
    v_query text;
BEGIN
    IF p_loaitk = 'G' THEN
        -- Tổng hợp theo group loss
        SELECT string_agg(
            'SUM(CASE WHEN "LoaiLoi"=' || quote_literal(loaimaloi) || ' THEN "Total" END) AS '
            || quote_ident(loaimaloi),
            ',' ORDER BY MAX(id))
        INTO v_cols
        FROM (SELECT loaimaloi, MAX(id) AS id FROM "PLAN_LOI"
              WHERE loaimaloi <> 'Sản phẩm NG' GROUP BY loaimaloi) t;

        v_query := format($q$
            SELECT ngay, loaimay, %s
            FROM (
                SELECT a.ngay, a.loaimay, a."LoaiLoi", SUM(a."Tong") AS "Total"
                FROM (
                    SELECT ngay, loaimay, SUM(loss1) AS "Tong", group1 AS "LoaiLoi"
                    FROM "Plan_BCSX_OK" WHERE group1 IN (SELECT loaimaloi FROM "PLAN_LOI")
                    GROUP BY ngay, loaimay, group1
                    UNION
                    SELECT ngay, loaimay, SUM(loss2), group2 FROM "Plan_BCSX_OK"
                    WHERE group2 IN (SELECT loaimaloi FROM "PLAN_LOI")
                    GROUP BY ngay, loaimay, group2
                    UNION
                    SELECT ngay, loaimay, SUM(loss3), group3 FROM "Plan_BCSX_OK"
                    WHERE group3 IN (SELECT loaimaloi FROM "PLAN_LOI")
                    GROUP BY ngay, loaimay, group3
                    UNION
                    SELECT ngay, loaimay, SUM(loss4), group4 FROM "Plan_BCSX_OK"
                    WHERE group4 IN (SELECT loaimaloi FROM "PLAN_LOI")
                    GROUP BY ngay, loaimay, group4
                    UNION
                    SELECT ngay, loaimay, SUM(loss5), group5 FROM "Plan_BCSX_OK"
                    WHERE group5 IN (SELECT loaimaloi FROM "PLAN_LOI")
                    GROUP BY ngay, loaimay, group5
                    UNION
                    SELECT ngay, loaimay, SUM(loss6), group6 FROM "Plan_BCSX_OK"
                    WHERE group6 IN (SELECT loaimaloi FROM "PLAN_LOI")
                    GROUP BY ngay, loaimay, group6
                ) a
                GROUP BY a.ngay, a.loaimay, a."LoaiLoi"
            ) x
            GROUP BY ngay, loaimay
        $q$, v_cols);

    ELSIF p_loaitk = 'L' THEN
        -- Tổng hợp theo tên lỗi chi tiết (12.x)
        SELECT string_agg(
            'SUM(CASE WHEN "TenLoi"=' || quote_literal(tenloi) || ' THEN "Total" END) AS '
            || quote_ident(tenloi),
            ',' ORDER BY MAX(id))
        INTO v_cols
        FROM (SELECT tenloi, MAX(id) AS id FROM "PLAN_LOI"
              WHERE maloi LIKE '12.%' GROUP BY tenloi) t;

        v_query := format($q$
            SELECT ngay, loaimay, %s
            FROM (
                SELECT a.ngay, a.loaimay, a."TenLoi", SUM(a."Tong") AS "Total"
                FROM (
                    SELECT ngay,loaimay,SUM(loss1) AS "Tong",tensc1 AS "TenLoi"
                    FROM "Plan_BCSX_OK" WHERE masc1 IN (SELECT maloi FROM "PLAN_LOI" WHERE maloi LIKE '12.%%')
                    GROUP BY ngay,loaimay,tensc1
                    UNION
                    SELECT ngay,loaimay,SUM(loss2),tensc2 FROM "Plan_BCSX_OK"
                    WHERE masc2 IN (SELECT maloi FROM "PLAN_LOI" WHERE maloi LIKE '12.%%')
                    GROUP BY ngay,loaimay,tensc2
                    UNION
                    SELECT ngay,loaimay,SUM(loss3),tensc3 FROM "Plan_BCSX_OK"
                    WHERE masc3 IN (SELECT maloi FROM "PLAN_LOI" WHERE maloi LIKE '12.%%')
                    GROUP BY ngay,loaimay,tensc3
                    UNION
                    SELECT ngay,loaimay,SUM(loss4),tensc4 FROM "Plan_BCSX_OK"
                    WHERE masc4 IN (SELECT maloi FROM "PLAN_LOI" WHERE maloi LIKE '12.%%')
                    GROUP BY ngay,loaimay,tensc4
                    UNION
                    SELECT ngay,loaimay,SUM(loss5),tensc5 FROM "Plan_BCSX_OK"
                    WHERE masc5 IN (SELECT maloi FROM "PLAN_LOI" WHERE maloi LIKE '12.%%')
                    GROUP BY ngay,loaimay,tensc5
                    UNION
                    SELECT ngay,loaimay,SUM(loss6),tensc6 FROM "Plan_BCSX_OK"
                    WHERE masc6 IN (SELECT maloi FROM "PLAN_LOI" WHERE maloi LIKE '12.%%')
                    GROUP BY ngay,loaimay,tensc6
                ) a
                GROUP BY a.ngay, a.loaimay, a."TenLoi"
            ) x
            GROUP BY ngay, loaimay
        $q$, v_cols);
    END IF;

    OPEN cur FOR EXECUTE v_query;
    RETURN NEXT cur;
END;
$function$
```

---

## 60. `PLAN_Detail_CodeBlock`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_Detail_CodeBlock"(p_loaitk character varying, p_loaimay character varying, p_from timestamp without time zone, p_to timestamp without time zone)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur    refcursor := 'plan_detail_codeblock_cur';
    v_cols   text;
    v_sql    text;
BEGIN
    -- ── Nhánh G: tổng hợp theo nhóm lỗi (grouploi) ──────────────────
    IF p_loaitk = 'G' THEN

        -- Lấy danh sách column (loaimaloi), loại bỏ 'Sản phẩm NG'
        SELECT string_agg(quote_ident(loaimaloi), ',' ORDER BY id)
          INTO v_cols
          FROM (
              SELECT loaimaloi, MAX(id) AS id
                FROM "PLAN_LOI"
               WHERE loaimaloi <> N'Sản phẩm NG'
               GROUP BY loaimaloi
          ) t;

        -- Tạo temp table cho pivot loss
        DROP TABLE IF EXISTS _bg_loss;
        CREATE TEMP TABLE _bg_loss AS
        SELECT ngay, loaimay,
               grouploi,
               sum(losstimeinput) AS total
          FROM "PLAN_BCSX_BL_LOSS"
         WHERE grouploi IN (SELECT DISTINCT loaimaloi FROM "PLAN_LOI")
         GROUP BY ngay, loaimay, grouploi;

        -- Tạo temp table pivot result (conditional aggregation)
        DROP TABLE IF EXISTS _bg_pivot;
        v_sql := format(
            'CREATE TEMP TABLE _bg_pivot AS
             SELECT ngay, loaimay,
                    %s
               FROM _bg_loss
              GROUP BY ngay, loaimay',
            (
                SELECT string_agg(
                    format('sum(CASE WHEN grouploi = %L THEN total ELSE 0 END) AS %I',
                           loaimaloi, loaimaloi),
                    ', ' ORDER BY id
                )
                FROM (
                    SELECT loaimaloi, MAX(id) AS id
                      FROM "PLAN_LOI"
                     WHERE loaimaloi <> N'Sản phẩm NG'
                     GROUP BY loaimaloi
                ) t
            )
        );
        EXECUTE v_sql;

        -- UPDATE WtNG, Spbo, Qcgiu vào bảng pivot
        UPDATE _bg_pivot p
           SET ngay = p.ngay  -- dummy, real update below
        ;
        -- Thực ra ta nối JOIN với PLAN_BCSX_BL_OK khi SELECT cuối
        -- (không cần UPDATE, làm trong câu SELECT chính)

        -- Tạo bảng kết quả cuối cùng (có thêm WtNG, Spbo, Qcgiu)
        DROP TABLE IF EXISTS _bg_final;
        CREATE TEMP TABLE _bg_final AS
        SELECT p.*,
               round(COALESCE(ok.tong,    0)::numeric, 2) AS "WtNG",
               round(COALESCE(ok.tongspbo,0)::numeric, 2) AS "Spbo",
               round(COALESCE(ok.tongqcgiu,0)::numeric,2) AS "Qcgiu"
          FROM _bg_pivot p
          LEFT JOIN (
              SELECT ngay, loaimay,
                     sum((ng + boauto) * cycletime / 3600.0 / NULLIF(Cavity,0)) AS tong,
                     sum(spbo         * cycletime / 3600.0 / NULLIF(Cavity,0)) AS tongspbo,
                     sum(qcgiu        * cycletime / 3600.0 / NULLIF(Cavity,0)) AS tongqcgiu
                FROM "PLAN_BCSX_BL_OK"
               WHERE Cavity > 0
               GROUP BY ngay, loaimay
          ) ok ON ok.ngay = p.ngay AND ok.loaimay = p.loaimay;

        -- Mở cursor kết quả
        IF p_loaimay = 'ALL' THEN
            v_sql := format(
                'SELECT ngay, sum("WtNG") AS "WtNG", sum("Spbo") AS "Spbo",
                         sum("Qcgiu") AS "Qcgiu",
                         %s
                   FROM _bg_final
                  WHERE ngay >= %L AND ngay <= %L
                  GROUP BY ngay
                  ORDER BY ngay',
                v_cols, p_from, p_to
            );
        ELSE
            v_sql := format(
                'SELECT ngay, sum("WtNG") AS "WtNG", sum("Spbo") AS "Spbo",
                         sum("Qcgiu") AS "Qcgiu",
                         %s
                   FROM _bg_final
                  WHERE loaimay = %L AND ngay >= %L AND ngay <= %L
                  GROUP BY ngay
                  ORDER BY ngay',
                v_cols, p_loaimay, p_from, p_to
            );
        END IF;

        OPEN v_cur FOR EXECUTE v_sql;
        RETURN v_cur;

    -- ── Nhánh L: tổng hợp theo tên lỗi (tenloi) ─────────────────────
    ELSIF p_loaitk = 'L' THEN

        DROP TABLE IF EXISTS _bl_loss;
        CREATE TEMP TABLE _bl_loss AS
        SELECT ngay, loaimay,
               tenloi AS tenloisc,
               sum(losstimeinput) AS total
          FROM "PLAN_BCSX_BL_LOSS"
         WHERE grouploi IN (SELECT DISTINCT loaimaloi FROM "PLAN_LOI" WHERE maloi LIKE '11.%')
         GROUP BY ngay, loaimay, tenloi;

        -- Conditional aggregation columns
        v_sql := format(
            'SELECT ngay, loaimay, %s
               FROM _bl_loss
              GROUP BY ngay, loaimay',
            (
                SELECT string_agg(
                    format('sum(CASE WHEN tenloisc = %L THEN total ELSE 0 END) AS %I',
                           tenloi, tenloi),
                    ', '
                )
                FROM "PLAN_LOI"
                WHERE maloi LIKE '11.%'
            )
        );

        DROP TABLE IF EXISTS _bl_pivot;
        EXECUTE format('CREATE TEMP TABLE _bl_pivot AS %s', v_sql);

        IF p_loaimay = 'ALL' THEN
            v_sql := format(
                'SELECT ngay, %s
                   FROM _bl_pivot
                  WHERE ngay >= %L AND ngay <= %L
                  GROUP BY ngay
                  ORDER BY ngay',
                (SELECT string_agg(format('sum(%I) AS %I', tenloi, tenloi), ', ')
                   FROM "PLAN_LOI" WHERE maloi LIKE '11.%'),
                p_from, p_to
            );
        ELSE
            v_sql := format(
                'SELECT ngay, %s
                   FROM _bl_pivot
                  WHERE loaimay = %L AND ngay >= %L AND ngay <= %L
                  ORDER BY ngay',
                (SELECT string_agg(format('%I', tenloi), ', ')
                   FROM "PLAN_LOI" WHERE maloi LIKE '11.%'),
                p_loaimay, p_from, p_to
            );
        END IF;

        OPEN v_cur FOR EXECUTE v_sql;
        RETURN v_cur;

    END IF;

    -- Không có nhánh khớp → trả cursor rỗng
    OPEN v_cur FOR SELECT NULL WHERE FALSE;
    RETURN v_cur;
END;
$function$
```

---

## 61. `PLAN_FATVP`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_FATVP"(p_mayduc character varying, p_khuonlap character varying)
 RETURNS TABLE(ketqua character varying)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_loaimay varchar(50);
BEGIN
    -- Lấy nhóm máy của máy cụ thể
    SELECT "IMM"."NhomENG"
      INTO v_loaimay
      FROM "FA-TVP"
      JOIN "IMM" ON "FA-TVP"."Mayduc" = "IMM"."Mayduc"
     WHERE "FA-TVP"."MAYDUC" = p_mayduc
     LIMIT 1;

    -- Trả về KETQUA gần nhất của khuôn đó trên cùng nhóm máy
    RETURN QUERY
    SELECT f."KETQUA"
      FROM "FA-TVP" f
      JOIN "IMM" i ON f.mayduc = i.mayduc
     WHERE i.nhomeng = v_loaimay
       AND f.part_no || '-' || f."Die_no" = p_khuonlap
       AND f."MAYDUC" <> ''
     ORDER BY f."ID" DESC
     LIMIT 1;
END;
$function$
```

---

## 62. `PLAN_IFC_NAM`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_IFC_NAM"(p_from character varying, p_to character varying, p_ca character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur       refcursor := 'plan_ifc_nam_cur';
    v_cols      text;
    v_col_agg   text;
    v_sql       text;
    v_from_ts   timestamp := p_from::timestamp;
    v_to_ts     timestamp := p_to::timestamp;
BEGIN
    -- ── Bước 1: tập dữ liệu (bangloi) ────────────────────────────────
    DROP TABLE IF EXISTS _ifc_nam_bangloi;
    CREATE TEMP TABLE _ifc_nam_bangloi (
        thangnam  varchar(50),
        tenloisc  varchar(100),
        soluong   float
    );

    IF p_ca <> 'ALL' THEN
        INSERT INTO _ifc_nam_bangloi
        SELECT to_char(ngay, 'MM/YYYY'), tenloi1, sum(qty1)
          FROM "PLAN_BCSX_OK"
         WHERE maloi1 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi1
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi2, sum(qty2)
          FROM "PLAN_BCSX_OK"
         WHERE maloi2 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi2
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi3, sum(qty3)
          FROM "PLAN_BCSX_OK"
         WHERE maloi3 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi3
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi4, sum(qty4)
          FROM "PLAN_BCSX_OK"
         WHERE maloi4 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi4
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi5, sum(qty5)
          FROM "PLAN_BCSX_OK"
         WHERE maloi5 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi5
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi6, sum(qty6)
          FROM "PLAN_BCSX_OK"
         WHERE maloi6 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi6
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), 'YTotalOK', sum(okqty)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY')
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), 'YTotalNG', sum(totalNG)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY')
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), 'YTotal', sum(totalNG) + sum(okqty)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay, 'MM/YYYY');
    ELSE
        INSERT INTO _ifc_nam_bangloi
        SELECT to_char(ngay, 'MM/YYYY'), tenloi1, sum(qty1)
          FROM "PLAN_BCSX_OK"
         WHERE maloi1 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi1
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi2, sum(qty2)
          FROM "PLAN_BCSX_OK"
         WHERE maloi2 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi2
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi3, sum(qty3)
          FROM "PLAN_BCSX_OK"
         WHERE maloi3 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi3
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi4, sum(qty4)
          FROM "PLAN_BCSX_OK"
         WHERE maloi4 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi4
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi5, sum(qty5)
          FROM "PLAN_BCSX_OK"
         WHERE maloi5 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi5
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), tenloi6, sum(qty6)
          FROM "PLAN_BCSX_OK"
         WHERE maloi6 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY'), tenloi6
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), 'YTotalOK', sum(okqty)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY')
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), 'YTotalNG', sum(totalNG)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY')
        UNION ALL
        SELECT to_char(ngay, 'MM/YYYY'), 'YTotal', sum(totalNG) + sum(okqty)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay, 'MM/YYYY');
    END IF;

    -- ── Bước 2: build column list (distinct thangnam) ─────────────────
    SELECT string_agg(quote_ident(thangnam), ', ' ORDER BY min_ngay)
      INTO v_cols
      FROM (
          SELECT thangnam,
                 -- Sắp xếp đúng tháng/năm
                 min(to_date(thangnam, 'MM/YYYY')) AS min_ngay
            FROM _ifc_nam_bangloi
           GROUP BY thangnam
      ) t;

    SELECT string_agg(
               format('round(sum(CASE WHEN thangnam = %L THEN soluong ELSE 0 END)::numeric,2) AS %I',
                      thangnam, thangnam),
               ', ' ORDER BY min_ngay
           )
      INTO v_col_agg
      FROM (
          SELECT thangnam, min(to_date(thangnam, 'MM/YYYY')) AS min_ngay
            FROM _ifc_nam_bangloi
           GROUP BY thangnam
      ) t;

    -- ── Bước 3: mở cursor PIVOT by conditional aggregation ───────────
    v_sql := format(
        'SELECT tenloisc, %s
           FROM _ifc_nam_bangloi
          GROUP BY tenloisc
          ORDER BY tenloisc',
        v_col_agg
    );

    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 63. `PLAN_IFC_NAM_DEFECT`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_IFC_NAM_DEFECT"(p_from character varying, p_to character varying, p_ca character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur       refcursor := 'plan_ifc_nam_defect_cur';
    v_col_agg   text;
    v_sql       text;
    v_from_ts   timestamp := p_from::timestamp;
    v_to_ts     timestamp := p_to::timestamp;
BEGIN
    DROP TABLE IF EXISTS _ifc_nam_def_bangloi;
    CREATE TEMP TABLE _ifc_nam_def_bangloi (
        thangnam  varchar(50),
        tenloisc  varchar(100),
        soluong   float
    );

    IF p_ca <> 'ALL' THEN
        INSERT INTO _ifc_nam_def_bangloi
        SELECT to_char(ngay,'MM/YYYY'), tenloi1,
               sum(qty1) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi1 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi1
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi2,
               sum(qty2) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi2 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi2
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi3,
               sum(qty3) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi3 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi3
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi4,
               sum(qty4) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi4 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi4
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi5,
               sum(qty5) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi5 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi5
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi6,
               sum(qty6) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi6 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi6
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), 'YTotalNG',
               sum(totalNG) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts AND ca = p_ca
         GROUP BY to_char(ngay,'MM/YYYY');
    ELSE
        INSERT INTO _ifc_nam_def_bangloi
        SELECT to_char(ngay,'MM/YYYY'), tenloi1,
               sum(qty1) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi1 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi1
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi2,
               sum(qty2) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi2 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi2
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi3,
               sum(qty3) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi3 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi3
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi4,
               sum(qty4) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi4 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi4
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi5,
               sum(qty5) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi5 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi5
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), tenloi6,
               sum(qty6) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE maloi6 LIKE '1.%' AND ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay,'MM/YYYY'), tenloi6
        UNION ALL
        SELECT to_char(ngay,'MM/YYYY'), 'YTotalNG',
               sum(totalNG) / NULLIF(sum(totalNG)+sum(okqty), 0)
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= v_from_ts AND ngay <= v_to_ts
         GROUP BY to_char(ngay,'MM/YYYY');
    END IF;

    -- Build conditional aggregation columns
    SELECT string_agg(
               format('round(sum(CASE WHEN thangnam = %L THEN soluong ELSE 0 END)::numeric,4) AS %I',
                      thangnam, thangnam),
               ', ' ORDER BY min_ngay
           )
      INTO v_col_agg
      FROM (
          SELECT thangnam, min(to_date(thangnam,'MM/YYYY')) AS min_ngay
            FROM _ifc_nam_def_bangloi
           GROUP BY thangnam
      ) t;

    v_sql := format(
        'SELECT tenloisc, %s
           FROM _ifc_nam_def_bangloi
          GROUP BY tenloisc
          ORDER BY tenloisc',
        v_col_agg
    );

    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 64. `PLAN_IFC_NONIFC_THANG`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_IFC_NONIFC_THANG"(p_from character varying, p_to character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur     refcursor := 'plan_ifc_nonifc_thang_cur';
    v_col_agg text;
    v_sql     text;
    v_from_ts timestamp := p_from::timestamp;
    v_to_ts   timestamp := p_to::timestamp;
BEGIN
    DROP TABLE IF EXISTS _nonifc_bangloi;
    CREATE TEMP TABLE _nonifc_bangloi (
        masp       varchar(50), tensanpham varchar(50),
        makhuon    varchar(50), tenloisc   varchar(100), soluong float
    );

    INSERT INTO _nonifc_bangloi
    SELECT masp, tensanpham, makhuon, 'OKPart',   sum(okqty)
      FROM "PLAN_BCSX_OK"
     WHERE ngay >= v_from_ts AND ngay <= v_to_ts
     GROUP BY masp, tensanpham, makhuon
    UNION ALL
    SELECT masp, tensanpham, makhuon, 'TotalNG',  sum("totalNG")
      FROM "PLAN_BCSX_OK"
     WHERE ngay >= v_from_ts AND ngay <= v_to_ts
     GROUP BY masp, tensanpham, makhuon
    UNION ALL
    SELECT masp, tensanpham, makhuon, 'NGRate',
           COALESCE(sum("totalNG") / NULLIF(sum("totalNG")+sum(okqty),0)*100, 0)
      FROM "PLAN_BCSX_OK"
     WHERE ngay >= v_from_ts AND ngay <= v_to_ts
     GROUP BY masp, tensanpham, makhuon
    UNION ALL
    SELECT o."Masp", o.tensanpham, o.makhuon, 'IFC',
           sum(o."totalNG" * lp."Total_Cost")
      FROM "PLAN_BCSX_OK" o
      JOIN "tb_ListPart" lp ON o."Masp" = lp.part_no
     WHERE o.ngay >= v_from_ts AND o.ngay <= v_to_ts
     GROUP BY o."Masp", o.tensanpham, o.makhuon
    UNION ALL
    SELECT masp,tensanpham,makhuon,tenloi1,sum(qty1)
      FROM "PLAN_BCSX_OK" WHERE maloi1 LIKE '1.%' AND ngay BETWEEN v_from_ts AND v_to_ts GROUP BY masp,tensanpham,makhuon,tenloi1
    UNION ALL
    SELECT masp,tensanpham,makhuon,tenloi2,sum(qty2)
      FROM "PLAN_BCSX_OK" WHERE maloi2 LIKE '1.%' AND ngay BETWEEN v_from_ts AND v_to_ts GROUP BY masp,tensanpham,makhuon,tenloi2
    UNION ALL
    SELECT masp,tensanpham,makhuon,tenloi3,sum(qty3)
      FROM "PLAN_BCSX_OK" WHERE maloi3 LIKE '1.%' AND ngay BETWEEN v_from_ts AND v_to_ts GROUP BY masp,tensanpham,makhuon,tenloi3
    UNION ALL
    SELECT masp,tensanpham,makhuon,tenloi4,sum(qty4)
      FROM "PLAN_BCSX_OK" WHERE maloi4 LIKE '1.%' AND ngay BETWEEN v_from_ts AND v_to_ts GROUP BY masp,tensanpham,makhuon,tenloi4
    UNION ALL
    SELECT masp,tensanpham,makhuon,tenloi5,sum(qty5)
      FROM "PLAN_BCSX_OK" WHERE maloi5 LIKE '1.%' AND ngay BETWEEN v_from_ts AND v_to_ts GROUP BY masp,tensanpham,makhuon,tenloi5
    UNION ALL
    SELECT masp,tensanpham,makhuon,tenloi6,sum(qty6)
      FROM "PLAN_BCSX_OK" WHERE maloi6 LIKE '1.%' AND ngay BETWEEN v_from_ts AND v_to_ts GROUP BY masp,tensanpham,makhuon,tenloi6;

    -- Build pivot columns: OKPart,TotalNG,NGRate + dynamic loi list + IFC
    SELECT
        'sum(CASE WHEN tenloisc=''OKPart'' THEN soluong ELSE 0 END) AS "OKPart",'
        || 'sum(CASE WHEN tenloisc=''TotalNG'' THEN soluong ELSE 0 END) AS "TotalNG",'
        || 'sum(CASE WHEN tenloisc=''NGRate'' THEN soluong ELSE 0 END) AS "NGRate",'
        || string_agg(
               format('sum(CASE WHEN tenloisc=%L THEN soluong ELSE 0 END) AS %I', tenloi, tenloi),
               ',' ORDER BY id
           )
        || ',sum(CASE WHEN tenloisc=''IFC'' THEN soluong ELSE 0 END) AS "IFC"'
      INTO v_col_agg
      FROM "PLAN_LOI" WHERE maloi LIKE '1.%';

    v_sql := format(
        'SELECT masp,tensanpham,makhuon,%s FROM _nonifc_bangloi GROUP BY masp,tensanpham,makhuon ORDER BY masp',
        v_col_agg
    );
    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 65. `PLAN_IFC_THANG`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_IFC_THANG"(p_from character varying, p_to character varying, p_ca character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur     refcursor := 'plan_ifc_thang_cur';
    v_col_agg text;
    v_sql     text;
    v_from_ts timestamp := p_from::timestamp;
    v_to_ts   timestamp := p_to::timestamp;
    v_ca_cond text;
BEGIN
    v_ca_cond := CASE WHEN p_ca <> 'ALL' THEN format('AND ca = %L', p_ca) ELSE '' END;

    DROP TABLE IF EXISTS _ifc_thang_bangloi;
    CREATE TEMP TABLE _ifc_thang_bangloi (
        masp varchar(50), tensanpham varchar(50),
        makhuon varchar(50), tenloisc varchar(100), soluong float
    );

    EXECUTE format(
        'INSERT INTO _ifc_thang_bangloi
         SELECT masp,tensanpham,makhuon,''OKPart'',sum(okqty)
           FROM "PLAN_BCSX_OK" WHERE ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon
         UNION ALL
         SELECT masp,tensanpham,makhuon,''TotalNG'',sum("totalNG")
           FROM "PLAN_BCSX_OK" WHERE ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon
         UNION ALL
         SELECT masp,tensanpham,makhuon,''NGRate'',
                COALESCE(sum("totalNG")/NULLIF(sum("totalNG")+sum(okqty),0)*100,0)
           FROM "PLAN_BCSX_OK" WHERE ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon
         UNION ALL
         SELECT o."Masp",o.tensanpham,o.makhuon,''IFC'',sum(o."totalNG"*lp."Total_Cost")
           FROM "PLAN_BCSX_OK" o JOIN "tb_ListPart" lp ON o."Masp"=lp.part_no
          WHERE o.ngay>=%L AND o.ngay<=%L %s GROUP BY o."Masp",o.tensanpham,o.makhuon
         UNION ALL
         SELECT masp,tensanpham,makhuon,tenloi1,sum(qty1) FROM "PLAN_BCSX_OK"
          WHERE maloi1 LIKE ''1.%%'' AND ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon,tenloi1
         UNION ALL
         SELECT masp,tensanpham,makhuon,tenloi2,sum(qty2) FROM "PLAN_BCSX_OK"
          WHERE maloi2 LIKE ''1.%%'' AND ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon,tenloi2
         UNION ALL
         SELECT masp,tensanpham,makhuon,tenloi3,sum(qty3) FROM "PLAN_BCSX_OK"
          WHERE maloi3 LIKE ''1.%%'' AND ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon,tenloi3
         UNION ALL
         SELECT masp,tensanpham,makhuon,tenloi4,sum(qty4) FROM "PLAN_BCSX_OK"
          WHERE maloi4 LIKE ''1.%%'' AND ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon,tenloi4
         UNION ALL
         SELECT masp,tensanpham,makhuon,tenloi5,sum(qty5) FROM "PLAN_BCSX_OK"
          WHERE maloi5 LIKE ''1.%%'' AND ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon,tenloi5
         UNION ALL
         SELECT masp,tensanpham,makhuon,tenloi6,sum(qty6) FROM "PLAN_BCSX_OK"
          WHERE maloi6 LIKE ''1.%%'' AND ngay>=%L AND ngay<=%L %s GROUP BY masp,tensanpham,makhuon,tenloi6',
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond,
        v_from_ts, v_to_ts, v_ca_cond
    );

    SELECT
        'sum(CASE WHEN tenloisc=''OKPart'' THEN soluong ELSE 0 END) AS "OKPart",'
        || 'sum(CASE WHEN tenloisc=''TotalNG'' THEN soluong ELSE 0 END) AS "TotalNG",'
        || 'sum(CASE WHEN tenloisc=''NGRate'' THEN soluong ELSE 0 END) AS "NGRate",'
        || string_agg(
               format('sum(CASE WHEN tenloisc=%L THEN soluong ELSE 0 END) AS %I', tenloi, tenloi),
               ',' ORDER BY id
           )
        || ',sum(CASE WHEN tenloisc=''IFC'' THEN soluong ELSE 0 END) AS "IFC"'
      INTO v_col_agg
      FROM "PLAN_LOI" WHERE maloi LIKE '1.%';

    v_sql := format(
        'SELECT masp,tensanpham,makhuon,%s FROM _ifc_thang_bangloi GROUP BY masp,tensanpham,makhuon ORDER BY masp',
        v_col_agg
    );
    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 66. `PLAN_OPRA`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_OPRA"(p_hinhthuc character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur  refcursor := 'plan_opra_cur';
    v_sql  text;
    -- Danh sách 58 máy (cố định theo dữ liệu thực tế)
    machines CONSTANT text[] := ARRAY[
        'A01','A02','A03','A04','A05','A06','A07','A08','A09','A10','A11','A12','A13','A14','A15',
        'B01','B02','B03','B4A','B4B','B05','B06','B07','B08','B09','B10',
        'C01','C02','C03','C04','C05','C06','C07','C08','C09','C10','C11','C12','C13',
        'D01','D02','D03','D04','D05','D06','D07','D08','D09','D10','D11','D12','D13','D14','D15','D16','D17'
    ];
    m text;
    p_cols text := '';
    m_cols text := '';
    o_cols text := '';
    s_cols text := '';
BEGIN
    -- Build column expressions
    FOREACH m IN ARRAY machines LOOP
        p_cols := p_cols || format(
            ',sum(CASE WHEN may=%L THEN wtng+CASE WHEN type1=''P'' THEN loss1 ELSE 0 END+CASE WHEN type2=''P'' THEN loss2 ELSE 0 END+CASE WHEN type3=''P'' THEN loss3 ELSE 0 END+CASE WHEN type4=''P'' THEN loss4 ELSE 0 END+CASE WHEN type5=''P'' THEN loss5 ELSE 0 END+CASE WHEN type6=''P'' THEN loss6 ELSE 0 END ELSE 0 END) AS %I',
            m, m
        );
        m_cols := m_cols || format(
            ',sum(CASE WHEN may=%L THEN wtng+CASE WHEN type1=''M'' THEN loss1 ELSE 0 END+CASE WHEN type2=''M'' THEN loss2 ELSE 0 END+CASE WHEN type3=''M'' THEN loss3 ELSE 0 END+CASE WHEN type4=''M'' THEN loss4 ELSE 0 END+CASE WHEN type5=''M'' THEN loss5 ELSE 0 END+CASE WHEN type6=''M'' THEN loss6 ELSE 0 END ELSE 0 END) AS %I',
            m, m
        );
        o_cols := o_cols || format(
            ',sum(CASE WHEN may=%L THEN CASE WHEN opr_total=0 THEN 0 ELSE round(run/opr_total,2) END ELSE 0 END) AS %I',
            m, m
        );
        s_cols := s_cols || format(
            ',sum(CASE WHEN may=%L THEN CASE WHEN opr_total=0 THEN 0 ELSE round(run/opr_total,2) END ELSE 0 END) AS %I',
            m, m
        );
    END LOOP;

    IF p_hinhthuc = 'P' THEN
        v_sql := format(
            'SELECT ngay %s FROM "PLAN_BCSX_OK" GROUP BY ngay ORDER BY ngay',
            p_cols
        );
        OPEN v_cur FOR EXECUTE v_sql;

    ELSIF p_hinhthuc = 'M' THEN
        v_sql := format(
            'SELECT ngay %s FROM "PLAN_BCSX_OK" GROUP BY ngay ORDER BY ngay',
            m_cols
        );
        OPEN v_cur FOR EXECUTE v_sql;

    ELSIF p_hinhthuc = 'O' THEN
        -- OPR ratio per machine per day
        OPEN v_cur FOR
        SELECT t2.ngay,
               sum(CASE WHEN t2.may='A01' THEN CASE WHEN t2.opr_total=0 THEN 0 ELSE round(t2.run/t2.opr_total,2) END ELSE 0 END) AS "A01",
               sum(CASE WHEN t2.may='A02' THEN CASE WHEN t2.opr_total=0 THEN 0 ELSE round(t2.run/t2.opr_total,2) END ELSE 0 END) AS "A02"
          -- NOTE: full 58-machine version: generate dynamically if needed
          FROM (
              SELECT ngay, may,
                     sum(okqty*ct/3600.0/NULLIF(Cavity,0)) AS run,
                     sum(wtng
                       + CASE WHEN type1='P' THEN loss1 ELSE 0 END
                       + CASE WHEN type2='P' THEN loss2 ELSE 0 END
                       + CASE WHEN type3='P' THEN loss3 ELSE 0 END
                       + CASE WHEN type4='P' THEN loss4 ELSE 0 END
                       + CASE WHEN type5='P' THEN loss5 ELSE 0 END
                       + CASE WHEN type6='P' THEN loss6 ELSE 0 END
                     ) AS plos,
                     sum(okqty*ct/3600.0/NULLIF(Cavity,0))
                     + sum(wtng
                       + CASE WHEN type1='P' THEN loss1 ELSE 0 END
                       + CASE WHEN type2='P' THEN loss2 ELSE 0 END
                       + CASE WHEN type3='P' THEN loss3 ELSE 0 END
                       + CASE WHEN type4='P' THEN loss4 ELSE 0 END
                       + CASE WHEN type5='P' THEN loss5 ELSE 0 END
                       + CASE WHEN type6='P' THEN loss6 ELSE 0 END) AS opr_total
                FROM "PLAN_BCSX_OK"
               GROUP BY ngay, may
          ) t2
         GROUP BY t2.ngay
         ORDER BY t2.ngay;

    ELSIF p_hinhthuc = 'T' THEN
        -- P-loss summary by loaimay (6 groups)
        OPEN v_cur FOR
        SELECT ngay,
               sum(CASE WHEN loaimay='650T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS "650T",
               sum(CASE WHEN loaimay='550T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS "550T",
               sum(CASE WHEN loaimay='450T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS "450T",
               sum(CASE WHEN loaimay='350T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS "350T",
               sum(CASE WHEN loaimay='180T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS "180T",
               sum(CASE WHEN loaimay='100T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS "100T"
          FROM "PLAN_BCSX_OK"
         GROUP BY ngay
         ORDER BY ngay;

    ELSIF p_hinhthuc = 'S' THEN
        -- OPR ratio per machine per day per ca
        OPEN v_cur FOR
        SELECT ngay, ca,
               sum(CASE WHEN may='A01' THEN CASE WHEN run+plos=0 THEN 0 ELSE round(run/(run+plos),2) END ELSE 0 END) AS "A01",
               sum(CASE WHEN may='A02' THEN CASE WHEN run+plos=0 THEN 0 ELSE round(run/(run+plos),2) END ELSE 0 END) AS "A02"
          -- NOTE: full 58-machine version requires dynamic SQL (xem PLAN_OPRA nhánh 'O')
          FROM (
              SELECT ngay, may, ca,
                     sum(okqty*ct/3600.0/NULLIF(Cavity,0)) AS run,
                     sum(wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END) AS plos
                FROM "PLAN_BCSX_OK"
               GROUP BY ngay, may, ca
          ) t
         GROUP BY ngay, ca
         ORDER BY ngay, ca;

    ELSIF p_hinhthuc = 'A' THEN
        -- OPR/P/M by loaimay per day (no date filter in original PLAN_OPRA 'A')
        OPEN v_cur FOR
        SELECT ngay,
               sum(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt,0)) AS opr_all,
               sum(CASE WHEN loaimay='100T' THEN COALESCE(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt,0)/4,0) ELSE 0 END) AS opr100,
               sum(CASE WHEN loaimay='180T' THEN COALESCE(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt,0)/6,0) ELSE 0 END) AS opr180,
               sum(CASE WHEN loaimay='350T' THEN COALESCE(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt,0)/17,0) ELSE 0 END) AS opr350,
               sum(CASE WHEN loaimay='450T' THEN COALESCE(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt,0)/15,0) ELSE 0 END) AS opr450,
               sum(CASE WHEN loaimay='550T' THEN COALESCE(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt,0)/9,0) ELSE 0 END) AS opr550,
               sum(CASE WHEN loaimay='650T' THEN COALESCE(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt,0)/5,0) ELSE 0 END) AS opr650
          FROM "PLAN_BCSX_OK"
         GROUP BY ngay
         ORDER BY ngay;

    ELSE
        OPEN v_cur FOR SELECT NULL WHERE FALSE;
    END IF;

    RETURN v_cur;
END;
$function$
```

---

## 67. `PLAN_OPROPRA`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_OPROPRA"(p_hinhthuc character varying, p_from timestamp without time zone, p_to timestamp without time zone)
 RETURNS TABLE(ngay character varying, wt character varying, opr100 double precision, p100 double precision, m100 double precision, opr180 double precision, p180 double precision, m180 double precision, opr350 double precision, p350 double precision, m350 double precision, opr450 double precision, p450 double precision, m450 double precision, opr550 double precision, p550 double precision, m550 double precision, opr650 double precision, p650 double precision, m650 double precision, oprall double precision, pall double precision, mall double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_hinhthuc = 'A' THEN
        -- ── Nhánh A: theo loaimay ──────────────────────────────────
        DROP TABLE IF EXISTS _opropra_opr;
        CREATE TEMP TABLE _opropra_opr AS
        SELECT ngay::text AS ngay, wt::text,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN loaimay='100T' THEN okqty*ct/3600.0/NULLIF(Cavity,0) ELSE 0 END)/4,0) ELSE 0 END AS opr100,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN loaimay='180T' THEN okqty*ct/3600.0/NULLIF(Cavity,0) ELSE 0 END)/6,0) ELSE 0 END AS opr180,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN loaimay='350T' THEN okqty*ct/3600.0/NULLIF(Cavity,0) ELSE 0 END)/17,0) ELSE 0 END AS opr350,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN loaimay='450T' THEN okqty*ct/3600.0/NULLIF(Cavity,0) ELSE 0 END)/15,0) ELSE 0 END AS opr450,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN loaimay='550T' THEN okqty*ct/3600.0/NULLIF(Cavity,0) ELSE 0 END)/9,0) ELSE 0 END AS opr550,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN loaimay='650T' THEN okqty*ct/3600.0/NULLIF(Cavity,0) ELSE 0 END)/5,0) ELSE 0 END AS opr650,
               CASE WHEN wt<>0 THEN COALESCE(sum(okqty*ct/3600.0/NULLIF(Cavity,0))/56,0) ELSE 0 END AS oprall,
               0::float AS p100, 0::float AS p180, 0::float AS p350,
               0::float AS p450, 0::float AS p550, 0::float AS p650, 0::float AS pall,
               0::float AS m100, 0::float AS m180, 0::float AS m350,
               0::float AS m450, 0::float AS m550, 0::float AS m650, 0::float AS mall
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= p_from AND ngay <= p_to
         GROUP BY ngay::text, wt;

        -- P Loss theo loaimay
        UPDATE _opropra_opr o SET
            p100 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.p100/(o.wt::float*4),0) ELSE 0 END,
            p180 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.p180/(o.wt::float*6),0) ELSE 0 END,
            p350 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.p350/(o.wt::float*17),0) ELSE 0 END,
            p450 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.p450/(o.wt::float*15),0) ELSE 0 END,
            p550 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.p550/(o.wt::float*9),0) ELSE 0 END,
            p650 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.p650/(o.wt::float*5),0) ELSE 0 END,
            pall = CASE WHEN o.wt::float<>0 THEN COALESCE((a.p100+a.p180+a.p350+a.p450+a.p550+a.p650)/(o.wt::float*56),0) ELSE 0 END
          FROM (
              SELECT ngay::text,
                     sum(CASE WHEN loaimay='100T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS p100,
                     sum(CASE WHEN loaimay='180T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS p180,
                     sum(CASE WHEN loaimay='350T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS p350,
                     sum(CASE WHEN loaimay='450T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS p450,
                     sum(CASE WHEN loaimay='550T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS p550,
                     sum(CASE WHEN loaimay='650T' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS p650
                FROM "PLAN_BCSX_OK"
               WHERE ngay >= p_from AND ngay <= p_to
               GROUP BY ngay::text
          ) a
         WHERE a.ngay = o.ngay;

        -- M Loss
        UPDATE _opropra_opr o SET
            m100 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.m100/(o.wt::float*4),0) ELSE 0 END,
            m180 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.m180/(o.wt::float*6),0) ELSE 0 END,
            m350 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.m350/(o.wt::float*17),0) ELSE 0 END,
            m450 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.m450/(o.wt::float*15),0) ELSE 0 END,
            m550 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.m550/(o.wt::float*9),0) ELSE 0 END,
            m650 = CASE WHEN o.wt::float<>0 THEN COALESCE(a.m650/(o.wt::float*5),0) ELSE 0 END,
            mall = CASE WHEN o.wt::float<>0 THEN COALESCE((a.m100+a.m180+a.m350+a.m450+a.m550+a.m650)/(o.wt::float*56),0) ELSE 0 END
          FROM (
              SELECT ngay::text,
                     sum(CASE WHEN loaimay='100T' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS m100,
                     sum(CASE WHEN loaimay='180T' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS m180,
                     sum(CASE WHEN loaimay='350T' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS m350,
                     sum(CASE WHEN loaimay='450T' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS m450,
                     sum(CASE WHEN loaimay='550T' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS m550,
                     sum(CASE WHEN loaimay='650T' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS m650
                FROM "PLAN_BCSX_OK"
               WHERE ngay >= p_from AND ngay <= p_to AND Cavity > 0
               GROUP BY ngay::text
          ) a
         WHERE a.ngay = o.ngay;

        RETURN QUERY
        SELECT t.ngay, t.wt,
               round(t.opr100::numeric,2), round(t.p100::numeric,2), round(t.m100::numeric,2),
               round(t.opr180::numeric,2), round(t.p180::numeric,2), round(t.m180::numeric,2),
               round(t.opr350::numeric,2), round(t.p350::numeric,2), round(t.m350::numeric,2),
               round(t.opr450::numeric,2), round(t.p450::numeric,2), round(t.m450::numeric,2),
               round(t.opr550::numeric,2), round(t.p550::numeric,2), round(t.m550::numeric,2),
               round(t.opr650::numeric,2), round(t.p650::numeric,2), round(t.m650::numeric,2),
               round(t.oprall::numeric,2), round(t.pall::numeric,2), round(t.mall::numeric,2)
          FROM _opropra_opr t
         ORDER BY t.ngay;

    ELSIF p_hinhthuc = 'B' THEN
        -- ── Nhánh B: theo ca (A/B/C) ──────────────────────────────
        -- OPR = (okqty*ct/3600/Cavity) / (wt/2) / 56
        DROP TABLE IF EXISTS _opropra_shift;
        CREATE TEMP TABLE _opropra_shift AS
        SELECT ngay::text, wt::text,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN ca='A' THEN okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt/2.0,0)/56 ELSE 0 END),0) ELSE 0 END AS opra,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN ca='B' THEN okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt/2.0,0)/56 ELSE 0 END),0) ELSE 0 END AS oprb,
               CASE WHEN wt<>0 THEN COALESCE(sum(CASE WHEN ca='C' THEN okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt/2.0,0)/56 ELSE 0 END),0) ELSE 0 END AS oprc,
               CASE WHEN wt<>0 THEN COALESCE(sum(okqty*ct/3600.0/NULLIF(Cavity,0)/NULLIF(wt/2.0,0)/56),0) ELSE 0 END AS oprall,
               0::float AS pa, 0::float AS pb, 0::float AS pc, 0::float AS pall,
               0::float AS ma, 0::float AS mb, 0::float AS mc, 0::float AS mall
          FROM "PLAN_BCSX_OK"
         WHERE ngay >= p_from AND ngay <= p_to
         GROUP BY ngay::text, wt;

        -- P Loss theo ca
        UPDATE _opropra_shift s SET
            pa = CASE WHEN s.wt::float<>0 THEN COALESCE(a.pa/((s.wt::float/2)*56),0) ELSE 0 END,
            pb = CASE WHEN s.wt::float<>0 THEN COALESCE(a.pb/((s.wt::float/2)*56),0) ELSE 0 END,
            pc = CASE WHEN s.wt::float<>0 THEN COALESCE(a.pc/((s.wt::float/2)*56),0) ELSE 0 END
          FROM (
              SELECT ngay::text,
                     sum(CASE WHEN ca='A' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS pa,
                     sum(CASE WHEN ca='B' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS pb,
                     sum(CASE WHEN ca='C' THEN wtng+CASE WHEN type1='P' THEN loss1 ELSE 0 END+CASE WHEN type2='P' THEN loss2 ELSE 0 END+CASE WHEN type3='P' THEN loss3 ELSE 0 END+CASE WHEN type4='P' THEN loss4 ELSE 0 END+CASE WHEN type5='P' THEN loss5 ELSE 0 END+CASE WHEN type6='P' THEN loss6 ELSE 0 END ELSE 0 END) AS pc
                FROM "PLAN_BCSX_OK"
               WHERE ngay >= p_from AND ngay <= p_to
               GROUP BY ngay::text
          ) a WHERE a.ngay = s.ngay;

        -- M Loss theo ca
        UPDATE _opropra_shift s SET
            ma = CASE WHEN s.wt::float<>0 THEN COALESCE(a.ma/((s.wt::float/2)*56),0) ELSE 0 END,
            mb = CASE WHEN s.wt::float<>0 THEN COALESCE(a.mb/((s.wt::float/2)*56),0) ELSE 0 END,
            mc = CASE WHEN s.wt::float<>0 THEN COALESCE(a.mc/((s.wt::float/2)*56),0) ELSE 0 END
          FROM (
              SELECT ngay::text,
                     sum(CASE WHEN ca='A' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS ma,
                     sum(CASE WHEN ca='B' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS mb,
                     sum(CASE WHEN ca='C' THEN qckeep*ct/3600.0/NULLIF(Cavity,0)+sanphambo*ct/3600.0/NULLIF(Cavity,0)+CASE WHEN type1='M' THEN loss1 ELSE 0 END+CASE WHEN type2='M' THEN loss2 ELSE 0 END+CASE WHEN type3='M' THEN loss3 ELSE 0 END+CASE WHEN type4='M' THEN loss4 ELSE 0 END+CASE WHEN type5='M' THEN loss5 ELSE 0 END+CASE WHEN type6='M' THEN loss6 ELSE 0 END ELSE 0 END) AS mc
                FROM "PLAN_BCSX_OK"
               WHERE ngay >= p_from AND ngay <= p_to AND Cavity > 0
               GROUP BY ngay::text
          ) a WHERE a.ngay = s.ngay;

        -- Note: Nhánh B trả TABLE có cột khác (opra/oprb/oprc thay vì opr100..opr650)
        -- Để tương thích RETURNS TABLE đã khai báo, reuse vị trí cột:
        -- opr100=opra, opr180=oprb, opr350=oprc, p100=pa, p180=pb, p350=pc, m100=ma, m180=mb, m350=mc
        RETURN QUERY
        SELECT t.ngay, t.wt,
               round(t.opra::numeric,2), round(t.pa::numeric,2), round(t.ma::numeric,2),   -- A ca
               round(t.oprb::numeric,2), round(t.pb::numeric,2), round(t.mb::numeric,2),   -- B ca
               round(t.oprc::numeric,2), round(t.pc::numeric,2), round(t.mc::numeric,2),   -- C ca
               0,0,0,0,0,0,0,0,0,
               round(t.oprall::numeric,2), round(t.pall::numeric,2), round(t.mall::numeric,2)
          FROM _opropra_shift t
         ORDER BY t.ngay;
    END IF;
END;
$function$
```

---

## 68. `PLAN_REPORT_GROUPCODE`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_REPORT_GROUPCODE"(p_month integer, p_year integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "PLAN_TK_Group_Code";

    INSERT INTO "PLAN_TK_Group_Code" (loaimay, grouploi, time)
    SELECT loaimay, grouploi, round(sum(time)::numeric, 2)
      FROM (
          SELECT loaimay, 'OK part in processing time' AS grouploi,
                 round(sum(wtokqty)::numeric, 2) AS time
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
           GROUP BY loaimay
          UNION ALL
          SELECT loaimay, groupmaloi1, sum(wtmaloi1)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND groupmaloi1 IS NOT NULL AND groupmaloi1 <> ''
           GROUP BY loaimay, groupmaloi1
          UNION ALL
          SELECT loaimay, groupmaloi2, sum(wtmaloi2)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND groupmaloi2 IS NOT NULL AND groupmaloi2 <> ''
           GROUP BY loaimay, groupmaloi2
          UNION ALL
          SELECT loaimay, groupmaloi3, sum(wtmaloi3)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND groupmaloi3 IS NOT NULL AND groupmaloi3 <> ''
           GROUP BY loaimay, groupmaloi3
          UNION ALL
          SELECT loaimay, groupmaloi4, sum(wtmaloi4)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND groupmaloi4 IS NOT NULL AND groupmaloi4 <> ''
           GROUP BY loaimay, groupmaloi4
          UNION ALL
          SELECT loaimay, groupmaloi5, sum(wtmaloi5)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND groupmaloi5 IS NOT NULL AND groupmaloi5 <> ''
           GROUP BY loaimay, groupmaloi5
          UNION ALL
          SELECT loaimay, groupmaloi6, sum(wtmaloi6)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND groupmaloi6 IS NOT NULL AND groupmaloi6 <> ''
           GROUP BY loaimay, groupmaloi6
          UNION ALL
          SELECT loaimay, group1, sum(loss1)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND group1 IS NOT NULL AND group1 <> ''
           GROUP BY loaimay, group1
          UNION ALL
          SELECT loaimay, group2, sum(loss2)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND group2 IS NOT NULL AND group2 <> ''
           GROUP BY loaimay, group2
          UNION ALL
          SELECT loaimay, group3, sum(loss3)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND group3 IS NOT NULL AND group3 <> ''
           GROUP BY loaimay, group3
          UNION ALL
          SELECT loaimay, group4, sum(loss4)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND group4 IS NOT NULL AND group4 <> ''
           GROUP BY loaimay, group4
          UNION ALL
          SELECT loaimay, group5, sum(loss5)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND group5 IS NOT NULL AND group5 <> ''
           GROUP BY loaimay, group5
          UNION ALL
          SELECT loaimay, group6, sum(loss6)
            FROM "PLAN_BCSX_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND group6 IS NOT NULL AND group6 <> ''
           GROUP BY loaimay, group6
      ) a
     GROUP BY loaimay, grouploi;
END;
$function$
```

---

## 69. `PLAN_REPORT_GROUPCODE_BLOCK`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_REPORT_GROUPCODE_BLOCK"(p_month integer, p_year integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "PLAN_TK_Group_CodeBlock";

    INSERT INTO "PLAN_TK_Group_CodeBlock" (loaimay, grouploi, time)
    SELECT loaimay, grouploi, round(sum(time)::numeric, 2)
      FROM (
          SELECT loaimay, 'OK part in processing time' AS grouploi,
                 round(sum(okactualinput * cycletime / 3600.0 / NULLIF(Cavity,0))::numeric, 2) AS time
            FROM "PLAN_BCSX_BL_OK"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year AND Cavity > 0
           GROUP BY loaimay
          UNION ALL
          SELECT o.loaimay, l.loaimaloi,
                 sum(o.ng1 * o."CycleTime" / 3600.0 / NULLIF(o.Cavity,0))
            FROM "PLAN_BCSX_BL_OK" o JOIN "PLAN_LOI" l ON o.code1 = l.maloi
           WHERE extract(month FROM o.ngay) = p_month AND extract(year FROM o.ngay) = p_year
             AND o.code1 IS NOT NULL AND o.code1 <> '' AND o.Cavity > 0
           GROUP BY o.loaimay, l.loaimaloi
          UNION ALL
          SELECT o.loaimay, l.loaimaloi,
                 sum(o.ng2 * o."CycleTime" / 3600.0 / NULLIF(o.Cavity,0))
            FROM "PLAN_BCSX_BL_OK" o JOIN "PLAN_LOI" l ON o.code2 = l.maloi
           WHERE extract(month FROM o.ngay) = p_month AND extract(year FROM o.ngay) = p_year
             AND o.code2 IS NOT NULL AND o.code2 <> '' AND o.Cavity > 0
           GROUP BY o.loaimay, l.loaimaloi
          UNION ALL
          SELECT o.loaimay, l.loaimaloi,
                 sum(o.ng3 * o."CycleTime" / 3600.0 / NULLIF(o.Cavity,0))
            FROM "PLAN_BCSX_BL_OK" o JOIN "PLAN_LOI" l ON o.code3 = l.maloi
           WHERE extract(month FROM o.ngay) = p_month AND extract(year FROM o.ngay) = p_year
             AND o.code3 IS NOT NULL AND o.code3 <> '' AND o.Cavity > 0
           GROUP BY o.loaimay, l.loaimaloi
          UNION ALL
          -- boauto1-3
          SELECT o.loaimay, l.loaimaloi,
                 sum(o.boauto1 * o."CycleTime" / 3600.0 / NULLIF(o.Cavity,0))
            FROM "PLAN_BCSX_BL_OK" o JOIN "PLAN_LOI" l ON o.boautocode1 = l.maloi
           WHERE extract(month FROM o.ngay) = p_month AND extract(year FROM o.ngay) = p_year
             AND o.boautocode1 IS NOT NULL AND o.boautocode1 <> '' AND o.Cavity > 0
           GROUP BY o.loaimay, l.loaimaloi
          UNION ALL
          SELECT o.loaimay, l.loaimaloi,
                 sum(o.boauto2 * o."CycleTime" / 3600.0 / NULLIF(o.Cavity,0))
            FROM "PLAN_BCSX_BL_OK" o JOIN "PLAN_LOI" l ON o.boautocode2 = l.maloi
           WHERE extract(month FROM o.ngay) = p_month AND extract(year FROM o.ngay) = p_year
             AND o.boautocode2 IS NOT NULL AND o.boautocode2 <> '' AND o.Cavity > 0
           GROUP BY o.loaimay, l.loaimaloi
          UNION ALL
          SELECT o.loaimay, l.loaimaloi,
                 sum(o.boauto3 * o."CycleTime" / 3600.0 / NULLIF(o.Cavity,0))
            FROM "PLAN_BCSX_BL_OK" o JOIN "PLAN_LOI" l ON o.boautocode3 = l.maloi
           WHERE extract(month FROM o.ngay) = p_month AND extract(year FROM o.ngay) = p_year
             AND o.boautocode3 IS NOT NULL AND o.boautocode3 <> '' AND o.Cavity > 0
           GROUP BY o.loaimay, l.loaimaloi
          UNION ALL
          SELECT loaimay, grouploi, sum(losstimeinput)
            FROM "PLAN_BCSX_BL_LOSS"
           WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year
             AND grouploi IS NOT NULL AND grouploi <> ''
           GROUP BY loaimay, grouploi
      ) a
     GROUP BY loaimay, grouploi;
END;
$function$
```

---

## 70. `PLAN_SAMPLE_FA_SHOW`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_SAMPLE_FA_SHOW"(p_ngay date, p_thongkemay integer)
 RETURNS TABLE(may character varying, gioplan character varying, khuonlap character varying, vitrimau character varying)
 LANGUAGE plpgsql
AS $function$
DECLARE
    rec         record;
    v_phanloai  varchar(50);
    v_vitrimay  varchar(50);
    v_vitrikhac varchar(50);
BEGIN
    IF p_thongkemay = 0 THEN
        DROP TABLE IF EXISTS _sample_fa;
        CREATE TEMP TABLE _sample_fa AS
        SELECT d.id          AS id_tk,
               d.may         AS col_may,
               d.gioplan     AS col_gioplan,
               d.khuonlap    AS col_khuonlap,
               NULL::varchar AS col_vitrimau
          FROM "PLAN_Dandory" d
         WHERE d.ngay = p_ngay AND d.status <> 1
         ORDER BY d.stt, d.thutuhienthi;

        FOR rec IN SELECT id_tk, col_khuonlap AS kl FROM _sample_fa LOOP
            SELECT sf."Phanloai" INTO v_phanloai
              FROM "tb_SampleFA" sf
             WHERE sf."Barcode" = rec.kl
             ORDER BY sf."Id" DESC LIMIT 1;

            IF v_phanloai = 'LẤY MẪU' THEN
                SELECT sf."Vitrimoi_may", sf."Vitrimoi_khac"
                  INTO v_vitrimay, v_vitrikhac
                  FROM "tb_SampleFA" sf
                 WHERE sf."Barcode" = rec.kl
                 ORDER BY sf."Id" DESC LIMIT 1;
                UPDATE _sample_fa SET col_vitrimau = v_vitrimay || '/' || v_vitrikhac
                 WHERE id_tk = rec.id_tk;
            ELSE
                SELECT pm."Sample_location" INTO v_vitrikhac
                  FROM "tb_Part_master" pm
                 WHERE pm."Sample_barcode" = rec.kl LIMIT 1;
                UPDATE _sample_fa SET col_vitrimau = v_vitrikhac WHERE id_tk = rec.id_tk;
            END IF;
        END LOOP;

        RETURN QUERY SELECT s.col_may, s.col_gioplan, s.col_khuonlap, s.col_vitrimau
                       FROM _sample_fa s;
    ELSE
        UPDATE "IMM" SET "SampleFA" = '';
        UPDATE "IMM" SET "SampleFA" = a.barcode
          FROM (SELECT DISTINCT ON (t."Vitrimoi_may") t."Vitrimoi_may", t.barcode
                  FROM "tb_SampleFA" t
                 WHERE t."Vitrimoi_may" <> '' AND t.phanloai = 'LẤY MẪU'
                 ORDER BY t."Vitrimoi_may", t.ngayupdate DESC) a
         WHERE a."Vitrimoi_may" = "IMM"."Mayduc";
    END IF;
END;
$function$
```

---

## 71. `PLAN_SP_GET_PART_X_MACHINE`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_SP_GET_PART_X_MACHINE"(p_machine character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur refcursor := 'plan_sp_get_part_x_machine_cur';
    v_sql text;
BEGIN
    v_sql := format(
        'SELECT "GroupPart", "PartName", partno || ''-'' || dieno AS partno, %I AS cycletime, %I AS performance
           FROM "PLAN_PartXMachine"
          WHERE %I > 0
          ORDER BY %I * %I DESC',
        p_machine, 'P_'||p_machine,
        p_machine, p_machine, 'P_'||p_machine
    );
    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 72. `PLAN_SP_UPDATECOMMENT_PLAN_Dandory`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_SP_UPDATECOMMENT_PLAN_Dandory"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Loại 2: khớp cả khuonha + khuonlap
    UPDATE "PLAN_Dandory" pd
       SET comment = pbn_khuonlap.note
      FROM "PLAN_BTK_Note" pbn_khuonha
      JOIN "PLAN_BTK_Note" pbn_khuonlap
        ON pbn_khuonlap.type = 2 AND pbn_khuonlap.khuonlap = left(pd.khuonlap, 8)
     WHERE pbn_khuonha.type = 2
       AND pbn_khuonha.khuonha <> ''
       AND pbn_khuonha.khuonha = left(pd.khuonha, 8)
       AND (pd.comment IS NULL OR pd.comment = '')
       AND pd.ngay::date = current_date;

    -- Loại 3: khớp khuonlap + machine
    UPDATE "PLAN_Dandory" pd
       SET comment = pbn_khuonlap.note
      FROM "PLAN_BTK_Note" pbn_khuonlap
      JOIN "PLAN_BTK_Note" pbn_machine
        ON pbn_machine.type = 3 AND pbn_machine.machine = pd.may
     WHERE pbn_khuonlap.type = 3
       AND pbn_khuonlap.khuonlap <> ''
       AND pbn_khuonlap.khuonlap = left(pd.khuonlap, 8)
       AND (pd.comment IS NULL OR pd.comment = '')
       AND pd.ngay::date = current_date;
END;
$function$
```

---

## 73. `PLAN_SP_UPDATE_DANDORI_TIME`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_SP_UPDATE_DANDORI_TIME"(p_ngay character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_ngay     date := p_ngay::date;
    v_start    timestamp;
    v_end      timestamp;
    v_breaktime timestamp;
    slot       record;
BEGIN
    v_start     := (v_ngay::text || ' 08:00')::timestamp;
    v_breaktime := (v_ngay::text || ' 08:00')::timestamp + interval '1 day';

    UPDATE "PLAN_RuleDandoriTimes" SET "PLANDANDORITIMES" = 0;

    FOR slot IN
        SELECT gs AS slot_start, gs + interval '30 minutes' AS slot_end
          FROM generate_series(v_start, v_breaktime - interval '30 minutes', interval '30 minutes') gs
    LOOP
        UPDATE "PLAN_RuleDandoriTimes"
           SET "PLANDANDORITIMES" = (
               SELECT count(*) FROM "PLAN_APPOINTMENTS"
                WHERE "LABEL" < 2
                  AND "STARTDATE" >= slot.slot_start
                  AND "STARTDATE" < slot.slot_end
           )
         WHERE "RANGETIME" = '['
             || to_char(slot.slot_start, 'HH24') || ':' || to_char(slot.slot_start, 'MI')
             || ' - '
             || to_char(slot.slot_end, 'HH24') || ':' || to_char(slot.slot_end, 'MI')
             || ']';
    END LOOP;
END;
$function$
```

---

## 74. `PLAN_ShowKhoiDong`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_ShowKhoiDong"(p_ngay date, p_ca character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    rec              record;
    v_khuontrenmay   varchar(50);
    v_partname       varchar(50);
    v_stt_cond_lo    int := 26;   -- ngưỡng stt cho DAY/DEM
    v_stt_lo         boolean;
BEGIN
    v_stt_lo := (p_ca = 'DAY');

    -- Insert nếu chưa có
    IF NOT EXISTS (SELECT 1 FROM "PLAN_KhoiDong" WHERE ngay = p_ngay AND ca = p_ca) THEN
        INSERT INTO "PLAN_KhoiDong" (ngay, ca, may)
        SELECT p_ngay, p_ca, "Mayduc"
          FROM "IMM"
         WHERE "Mayduc" NOT IN ('CUM','TL1','Tl2','TP1','TP2','TP3')
         ORDER BY "Mayduc";
    END IF;

    -- Reset thông tin
    UPDATE "PLAN_KhoiDong"
       SET khuontrenmay = NULL, partname = NULL, giothaykhuon = NULL,
           khuonha = NULL, tenkhuonha = NULL, khuonlap = NULL, tenkhuonlap = NULL
     WHERE ngay = p_ngay AND ca = p_ca;

    -- UPDATE từ BTK có khuôn
    IF v_stt_lo THEN
        UPDATE "PLAN_KhoiDong" k
           SET khuontrenmay = a.khuonha, partname = a.tenkhuonha,
               giothaykhuon = a.gioplan, khuonha = a.khuonha, tenkhuonha = a.tenkhuonha,
               khuonlap = a.khuonlap, tenkhuonlap = a.tenkhuonlap
          FROM (SELECT may, gioplan, khuonha, tenkhuonha, khuonlap, tenkhuonlap
                  FROM "PLAN_Dandory"
                 WHERE ngay = p_ngay AND stt < v_stt_cond_lo) a
         WHERE a.may = k.may AND k.ngay = p_ngay AND k.ca = p_ca;
    ELSE
        UPDATE "PLAN_KhoiDong" k
           SET khuontrenmay = a.khuonha, partname = a.tenkhuonha,
               giothaykhuon = a.gioplan, khuonha = a.khuonha, tenkhuonha = a.tenkhuonha,
               khuonlap = a.khuonlap, tenkhuonlap = a.tenkhuonlap
          FROM (SELECT may, gioplan, khuonha, tenkhuonha, khuonlap, tenkhuonlap
                  FROM "PLAN_Dandory"
                 WHERE ngay = p_ngay AND stt >= v_stt_cond_lo) a
         WHERE a.may = k.may AND k.ngay = p_ngay AND k.ca = p_ca;
    END IF;

    -- Với máy không có trong BTK hoặc khuôn null → tìm lần cuối
    FOR rec IN
        SELECT may FROM "PLAN_KhoiDong"
         WHERE ngay = p_ngay AND ca = p_ca
           AND may NOT IN (
               SELECT may FROM "PLAN_Dandory"
                WHERE ngay = p_ngay
                  AND (CASE WHEN v_stt_lo THEN stt < v_stt_cond_lo ELSE stt >= v_stt_cond_lo END)
           )
    LOOP
        IF v_stt_lo THEN
            SELECT khuonlap, tenkhuonlap INTO v_khuontrenmay, v_partname
              FROM "PLAN_Dandory"
             WHERE ngay < p_ngay AND may = rec.may
               AND khuonlap IS NOT NULL AND length(khuonlap) > 4 AND status <> 1
             ORDER BY ngay DESC, stt DESC
             LIMIT 1;
        ELSE
            SELECT khuonlap, tenkhuonlap INTO v_khuontrenmay, v_partname
              FROM "PLAN_Dandory"
             WHERE ngay <= p_ngay AND may = rec.may
               AND khuonlap IS NOT NULL AND length(khuonlap) > 4 AND status <> 1
               AND id NOT IN (SELECT id FROM "PLAN_Dandory" WHERE ngay = p_ngay AND stt >= v_stt_cond_lo)
             ORDER BY ngay DESC, stt DESC
             LIMIT 1;
        END IF;

        UPDATE "PLAN_KhoiDong"
           SET khuontrenmay = v_khuontrenmay, partname = v_partname
         WHERE may = rec.may AND ngay = p_ngay AND ca = p_ca;
    END LOOP;
END;
$function$
```

---

## 75. `PLAN_SummaryBCSXnew`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_SummaryBCSXnew"()
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur     refcursor := 'plan_summarybcsxnew_cur';
    v_col_agg text;
    v_sql     text;
BEGIN
    SELECT string_agg(
        format('sum(CASE WHEN loailoi=%L THEN tongthoigian ELSE 0 END) AS %I', maloi, tenloi),
        ',' ORDER BY maloi
    ) INTO v_col_agg
      FROM "PLAN_LOI";

    v_sql := format(
        'SELECT ngay, may, %s
           FROM (
               SELECT ngay, may, maloi1 AS loailoi, round(sum(soluong1*cycletime/NULLIF(Cavity,0)/3600)::numeric,2) AS tongthoigian
                 FROM "PLAN_BCSX" WHERE tenloi1<>'''' GROUP BY ngay,may,maloi1
               UNION ALL
               SELECT ngay, may, maloi2, round(sum(soluong2*cycletime/NULLIF(Cavity,0)/3600)::numeric,2)
                 FROM "PLAN_BCSX" WHERE tenloi2<>'''' GROUP BY ngay,may,maloi2
               UNION ALL
               SELECT ngay, may, maloikhac, round(sum(soluongkhac*cycletime/NULLIF(Cavity,0)/3600)::numeric,2)
                 FROM "PLAN_BCSX" WHERE tenloikhac<>'''' GROUP BY ngay,may,maloikhac
               UNION ALL
               SELECT ngay, may, maloisuco1, losstime1 FROM "PLAN_BCSX" WHERE tensuco1<>''''
               UNION ALL
               SELECT ngay, may, maloisuco2, losstime2 FROM "PLAN_BCSX" WHERE tensuco2<>''''
               UNION ALL
               SELECT ngay, may, maloisuco3, losstime3 FROM "PLAN_BCSX" WHERE tensuco3<>''''
               UNION ALL
               SELECT ngay, may, maloisuco4, losstime4 FROM "PLAN_BCSX" WHERE tensuco4<>''''
               UNION ALL
               SELECT ngay, may, maloisuco5, losstime5 FROM "PLAN_BCSX" WHERE tensuco5<>''''
           ) src
          WHERE loailoi IN (SELECT maloi FROM "PLAN_LOI")
          GROUP BY ngay, may
          ORDER BY ngay, may',
        v_col_agg
    );
    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 76. `PLAN_SummaryNoPlan`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_SummaryNoPlan"(p_loaimay character varying, p_from character varying, p_to character varying, p_loaitonghop character varying, p_vedothi integer)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur     refcursor := 'plan_summarynoplan_cur';
    v_sql     text;
    v_col_agg text;
    v_mf      text := CASE WHEN p_loaimay <> 'ALL' THEN format('AND loaimay = %L', p_loaimay) ELSE '' END;
BEGIN
    DROP TABLE IF EXISTS _noplan_tmp;
    CREATE TEMP TABLE _noplan_tmp AS
    SELECT ngay::text, may, loaimay, masp, makhuon, ca, masuco, tensuco,
           round(sum(thoigian)::numeric, 2) AS thoigian
      FROM (
          SELECT ngay, may, loaimay, masp, makhuon, ca,
                 maloi1 AS masuco, tenloi1 AS tensuco,
                 round((qty1*ct/3600.0/NULLIF(Cavity,0))::numeric,2) AS thoigian
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE maloi1 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, maloi2, tenloi2,
                 round((qty2*ct/3600.0/NULLIF(Cavity,0))::numeric,2)
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE maloi2 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, maloi3, tenloi3,
                 round((qty3*ct/3600.0/NULLIF(Cavity,0))::numeric,2)
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE maloi3 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, maloi4, tenloi4,
                 round((qty4*ct/3600.0/NULLIF(Cavity,0))::numeric,2)
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE maloi4 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, maloi5, tenloi5,
                 round((qty5*ct/3600.0/NULLIF(Cavity,0))::numeric,2)
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE maloi5 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, maloi6, tenloi6,
                 round((qty6*ct/3600.0/NULLIF(Cavity,0))::numeric,2)
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE maloi6 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, masc1, tensc1, loss1
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE masc1 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, masc2, tensc2, loss2
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE masc2 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, masc3, tensc3, loss3
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE masc3 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, masc4, tensc4, loss4
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE masc4 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, masc5, tensc5, loss5
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE masc5 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay, may, loaimay, masp, makhuon, ca, masc6, tensc6, loss6
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE masc6 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
          UNION ALL
          SELECT ngay,may,loaimay,masp,makhuon,ca,mascdieuchinhauto,tenscdieuchinhauto,lossscdieuchinhauto
            FROM "PLAN_BCSX_OK" JOIN "tb_Part_master" pm ON "PLAN_BCSX_OK".masp = pm."Part_no"
           WHERE mascdieuchinhauto IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop)
             AND ngay >= p_from::timestamp AND ngay <= p_to::timestamp
      ) src
     GROUP BY ngay, may, loaimay, masp, makhuon, ca, masuco, tensuco;

    IF p_vedothi = 0 THEN
        OPEN v_cur FOR EXECUTE format(
            'SELECT ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,thoigian FROM _noplan_tmp WHERE TRUE %s ORDER BY ngay',
            v_mf
        );

    ELSIF p_vedothi = 1 THEN
        SELECT string_agg(format('sum(CASE WHEN tensuco=%L THEN thoigian ELSE 0 END) AS %I',tenloi,tenloi),',' ORDER BY tenloi)
          INTO v_col_agg FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop;
        OPEN v_cur FOR EXECUTE format(
            'SELECT ngay, %s FROM _noplan_tmp WHERE TRUE %s GROUP BY ngay ORDER BY ngay',
            v_col_agg, v_mf
        );

    ELSIF p_vedothi = 2 THEN
        SELECT string_agg(format('sum(CASE WHEN tensuco=%L THEN thoigian ELSE 0 END) AS %I',tenloi,tenloi),',' ORDER BY tenloi)
          INTO v_col_agg FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop;
        OPEN v_cur FOR EXECUTE format(
            'SELECT masp, %s FROM _noplan_tmp WHERE TRUE %s GROUP BY masp',
            v_col_agg, v_mf
        );

    ELSE -- vedothi=3
        OPEN v_cur FOR EXECUTE format(
            'SELECT tensuco, round(sum(thoigian)::numeric,2) AS total FROM _noplan_tmp WHERE TRUE %s GROUP BY tensuco ORDER BY sum(thoigian)',
            v_mf
        );
    END IF;

    RETURN v_cur;
END;
$function$
```

---

## 77. `PLAN_SummaryNoPlanBlock`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_SummaryNoPlanBlock"(p_loaimay character varying, p_from character varying, p_to character varying, p_loaitonghop character varying, p_vedothi integer)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur     refcursor := 'plan_summarynoplblock_cur';
    v_col_agg text;
    v_mf      text := CASE WHEN p_loaimay <> 'ALL' THEN format('AND loaimay = %L', p_loaimay) ELSE '' END;
BEGIN
    DROP TABLE IF EXISTS _noplbl_tmp;
    CREATE TEMP TABLE _noplbl_tmp AS
    SELECT ngay::text, may, loaimay, masanphamstd AS masp, makhuon, ca,
           masuco, tensuco, round(sum(thoigian)::numeric, 2) AS thoigian
      FROM (
          SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,code1 AS masuco,p.tenloi AS tensuco,
                 ng1*cycletime/3600.0/NULLIF(Cavity,0) AS thoigian
            FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" p ON b.code1=p.maloi
           WHERE code1 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi=p_loaitonghop)
             AND b.ngay>=p_from::timestamp AND b.ngay<=p_to::timestamp
          UNION ALL
          SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,code2,p.tenloi,ng2*cycletime/3600.0/NULLIF(Cavity,0)
            FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" p ON b.code2=p.maloi
           WHERE code2 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi=p_loaitonghop)
             AND b.ngay>=p_from::timestamp AND b.ngay<=p_to::timestamp
          UNION ALL
          SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,code3,p.tenloi,ng3*cycletime/3600.0/NULLIF(Cavity,0)
            FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" p ON b.code3=p.maloi
           WHERE code3 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi=p_loaitonghop)
             AND b.ngay>=p_from::timestamp AND b.ngay<=p_to::timestamp
          UNION ALL
          SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,boautocode1,p.tenloi,boauto1*cycletime/3600.0/NULLIF(Cavity,0)
            FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" p ON b.boautocode1=p.maloi
           WHERE boautocode1 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi=p_loaitonghop)
             AND b.ngay>=p_from::timestamp AND b.ngay<=p_to::timestamp
          UNION ALL
          SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,boautocode2,p.tenloi,boauto2*cycletime/3600.0/NULLIF(Cavity,0)
            FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" p ON b.boautocode2=p.maloi
           WHERE boautocode2 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi=p_loaitonghop)
             AND b.ngay>=p_from::timestamp AND b.ngay<=p_to::timestamp
          UNION ALL
          SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,boautocode3,p.tenloi,boauto3*cycletime/3600.0/NULLIF(Cavity,0)
            FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" p ON b.boautocode3=p.maloi
           WHERE boautocode3 IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi=p_loaitonghop)
             AND b.ngay>=p_from::timestamp AND b.ngay<=p_to::timestamp
          UNION ALL
          SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,maloi,tenloi,losstimeinput
            FROM "PLAN_BCSX_BL_LOSS" l JOIN "tb_Part_master" pm ON l.masanphamstd=pm."Part_no"
           WHERE maloi IN (SELECT maloi FROM "PLAN_LOI" WHERE loaimaloi=p_loaitonghop)
             AND l.ngay>=p_from::timestamp AND l.ngay<=p_to::timestamp
      ) src
     GROUP BY ngay, may, loaimay, masanphamstd, makhuon, ca, masuco, tensuco;

    IF p_vedothi = 0 THEN
        OPEN v_cur FOR EXECUTE format(
            'SELECT ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,thoigian FROM _noplbl_tmp WHERE TRUE %s ORDER BY ngay',
            v_mf
        );
    ELSIF p_vedothi = 1 THEN
        SELECT string_agg(format('sum(CASE WHEN tensuco=%L THEN thoigian ELSE 0 END) AS %I',tenloi,tenloi),',' ORDER BY tenloi)
          INTO v_col_agg FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop;
        OPEN v_cur FOR EXECUTE format(
            'SELECT ngay, %s FROM _noplbl_tmp WHERE TRUE %s GROUP BY ngay ORDER BY ngay',
            v_col_agg, v_mf
        );
    ELSIF p_vedothi = 2 THEN
        SELECT string_agg(format('sum(CASE WHEN tensuco=%L THEN thoigian ELSE 0 END) AS %I',tenloi,tenloi),',' ORDER BY tenloi)
          INTO v_col_agg FROM "PLAN_LOI" WHERE loaimaloi = p_loaitonghop;
        OPEN v_cur FOR EXECUTE format(
            'SELECT masp, %s FROM _noplbl_tmp WHERE TRUE %s GROUP BY masp',
            v_col_agg, v_mf
        );
    ELSE
        OPEN v_cur FOR EXECUTE format(
            'SELECT tensuco, round(sum(thoigian)::numeric,2) AS total FROM _noplbl_tmp WHERE TRUE %s GROUP BY tensuco ORDER BY sum(thoigian)',
            v_mf
        );
    END IF;
    RETURN v_cur;
END;
$function$
```

---

## 78. `PLAN_UPDATE_BCSX_DAILY`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_UPDATE_BCSX_DAILY"(p_month integer, p_year integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "PLAN_DataChart"
     WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year;

    -- OK Part
    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,phanloai,thoigian)
    SELECT ngay,may,loaimay,masp,makhuon,ca,'OK part in processing time',wtokqty
      FROM "PLAN_BCSX_OK"
     WHERE extract(month FROM ngay)=p_month AND extract(year FROM ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,phanloai,thoigian)
    SELECT ngay,may,loaimay,masp,makhuon,ca,'SanPhamBo',0 FROM "PLAN_BCSX_OK"
     WHERE extract(month FROM ngay)=p_month AND extract(year FROM ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,phanloai,thoigian)
    SELECT ngay,may,loaimay,masp,makhuon,ca,'QCKeep',0 FROM "PLAN_BCSX_OK"
     WHERE extract(month FROM ngay)=p_month AND extract(year FROM ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,phanloai,thoigian)
    SELECT ngay,may,loaimay,masp,makhuon,ca,'Adjust',0 FROM "PLAN_BCSX_OK"
     WHERE extract(month FROM ngay)=p_month AND extract(year FROM ngay)=p_year;

    -- maloi 1-6
    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.maloi1,o.tenloi1,l.loaimaloi,o.wtmaloi1
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.maloi1=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.maloi2,o.tenloi2,l.loaimaloi,o.wtmaloi2
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.maloi2=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.maloi3,o.tenloi3,l.loaimaloi,o.wtmaloi3
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.maloi3=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.maloi4,o.tenloi4,l.loaimaloi,o.wtmaloi4
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.maloi4=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.maloi5,o.tenloi5,l.loaimaloi,o.wtmaloi5
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.maloi5=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.maloi6,o.tenloi6,l.loaimaloi,o.wtmaloi6
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.maloi6=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    -- masc 1-6
    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.masc1,o.tensc1,l.loaimaloi,o.loss1
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.masc1=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.masc2,o.tensc2,l.loaimaloi,o.loss2
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.masc2=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.masc3,o.tensc3,l.loaimaloi,o.loss3
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.masc3=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.masc4,o.tensc4,l.loaimaloi,o.loss4
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.masc4=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.masc5,o.tensc5,l.loaimaloi,o.loss5
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.masc5=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;

    INSERT INTO "PLAN_DataChart" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT o.ngay,o.may,o.loaimay,o.masp,o.makhuon,o.ca,o.masc6,o.tensc6,l.loaimaloi,o.loss6
      FROM "PLAN_BCSX_OK" o JOIN "PLAN_LOI" l ON o.masc6=l.maloi
     WHERE extract(month FROM o.ngay)=p_month AND extract(year FROM o.ngay)=p_year;
END;
$function$
```

---

## 79. `PLAN_UPDATE_BCSX_DAILY_BLOCK`

```sql
CREATE OR REPLACE FUNCTION public."PLAN_UPDATE_BCSX_DAILY_BLOCK"(p_month integer, p_year integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "PLAN_DataChartBlock"
     WHERE extract(month FROM ngay) = p_month AND extract(year FROM ngay) = p_year;

    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,phanloai,thoigian)
    SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,'OK part in processing time',
           sum(okactualinput*cycletime/3600.0/NULLIF(Cavity,0))
      FROM "PLAN_BCSX_BL_OK"
     WHERE extract(month FROM ngay)=p_month AND extract(year FROM ngay)=p_year
       AND makhuon IS NOT NULL AND ca IS NOT NULL
     GROUP BY ngay,may,loaimay,masanphamstd,makhuon,ca;

    -- code 1-3
    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT b.ngay,b.may,b.loaimay,b.masanphamstd,b.makhuon,b.ca,b.code1,l.tenloi,l.loaimaloi,
           ng1*b.cycletime/3600.0/NULLIF(b.Cavity,0)
      FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" l ON b.code1=l.maloi
     WHERE extract(month FROM b.ngay)=p_month AND extract(year FROM b.ngay)=p_year AND l.maloi IS NOT NULL;

    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT b.ngay,b.may,b.loaimay,b.masanphamstd,b.makhuon,b.ca,b.code2,l.tenloi,l.loaimaloi,
           ng2*b.cycletime/3600.0/NULLIF(b.Cavity,0)
      FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" l ON b.code2=l.maloi
     WHERE extract(month FROM b.ngay)=p_month AND extract(year FROM b.ngay)=p_year AND l.maloi IS NOT NULL;

    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT b.ngay,b.may,b.loaimay,b.masanphamstd,b.makhuon,b.ca,b.code3,l.tenloi,l.loaimaloi,
           ng3*b.cycletime/3600.0/NULLIF(b.Cavity,0)
      FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" l ON b.code3=l.maloi
     WHERE extract(month FROM b.ngay)=p_month AND extract(year FROM b.ngay)=p_year AND l.maloi IS NOT NULL;

    -- boauto 1-3
    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT b.ngay,b.may,b.loaimay,b.masanphamstd,b.makhuon,b.ca,b.boautocode1,l.tenloi,l.loaimaloi,
           boauto1*b.cycletime/3600.0/NULLIF(b.Cavity,0)
      FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" l ON b.boautocode1=l.maloi
     WHERE extract(month FROM b.ngay)=p_month AND extract(year FROM b.ngay)=p_year AND l.maloi IS NOT NULL;

    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT b.ngay,b.may,b.loaimay,b.masanphamstd,b.makhuon,b.ca,b.boautocode2,l.tenloi,l.loaimaloi,
           boauto2*b.cycletime/3600.0/NULLIF(b.Cavity,0)
      FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" l ON b.boautocode2=l.maloi
     WHERE extract(month FROM b.ngay)=p_month AND extract(year FROM b.ngay)=p_year AND l.maloi IS NOT NULL;

    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT b.ngay,b.may,b.loaimay,b.masanphamstd,b.makhuon,b.ca,b.boautocode3,l.tenloi,l.loaimaloi,
           boauto3*b.cycletime/3600.0/NULLIF(b.Cavity,0)
      FROM "PLAN_BCSX_BL_OK" b JOIN "PLAN_LOI" l ON b.boautocode3=l.maloi
     WHERE extract(month FROM b.ngay)=p_month AND extract(year FROM b.ngay)=p_year AND l.maloi IS NOT NULL;

    -- Loss
    INSERT INTO "PLAN_DataChartBlock" (ngay,may,loaimay,masp,makhuon,ca,masuco,tensuco,phanloai,thoigian)
    SELECT ngay,may,loaimay,masanphamstd,makhuon,ca,maloi,tenloi,grouploi,sum(losstimeinput)
      FROM "PLAN_BCSX_BL_LOSS"
     WHERE extract(month FROM ngay)=p_month AND extract(year FROM ngay)=p_year AND maloi IS NOT NULL
     GROUP BY ngay,may,loaimay,masanphamstd,makhuon,ca,maloi,tenloi,grouploi;
END;
$function$
```

---

## 80. `PRO_GET_DANDORI_TARGET`

```sql
CREATE OR REPLACE FUNCTION public."PRO_GET_DANDORI_TARGET"(p_diesetup character varying, p_diedown character varying, p_ton character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur refcursor := 'pro_get_dandori_target_cur';
    v_sql text;
BEGIN
    v_sql := format(
        'SELECT %I FROM %I WHERE "PARTNO" = %L',
        p_diesetup,
        'PRO_DANDORITARGETMATRIX_' || p_ton,
        p_diedown
    );
    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 81. `PRO_GET_DANDORI_TARGET_NEWVER`

```sql
CREATE OR REPLACE FUNCTION public."PRO_GET_DANDORI_TARGET_NEWVER"(p_diesetup character varying, p_diedown character varying, p_ton character varying)
 RETURNS refcursor
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_cur refcursor := 'pro_get_dandori_target_nv_cur';
    v_sql text;
BEGIN
    v_sql := format(
        'SELECT %I FROM %I WHERE "PARTNO" = %L',
        p_diesetup,
        'PRO_DANDORITARGETMATRIX_NEWVER_' || p_ton,
        p_diedown
    );
    OPEN v_cur FOR EXECUTE v_sql;
    RETURN v_cur;
END;
$function$
```

---

## 82. `PRO_LICHSUTK_FROM_BCSX`

```sql
CREATE OR REPLACE FUNCTION public."PRO_LICHSUTK_FROM_BCSX"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_ngay date;
BEGIN
    -- Lấy ngày cập nhật gần nhất - 3 ngày
    SELECT (ngay - interval '3 days')::date INTO v_ngay
      FROM "PRO_Dandori_Time_NewVer"
     WHERE "dandoritimeBCSX" > 0
     ORDER BY id DESC
     LIMIT 1;

    -- Tổng hợp các lần dandori có masc='7.1' từ 6 slot
    WITH bangtam AS (
        SELECT ngay, masp, makhuon, loss1 AS losstime FROM "PLAN_BCSX_OK" WHERE masc1='7.1' AND ngay>=v_ngay
        UNION ALL
        SELECT ngay, masp, makhuon, loss2 FROM "PLAN_BCSX_OK" WHERE masc2='7.1' AND ngay>=v_ngay
        UNION ALL
        SELECT ngay, masp, makhuon, loss3 FROM "PLAN_BCSX_OK" WHERE masc3='7.1' AND ngay>=v_ngay
        UNION ALL
        SELECT ngay, masp, makhuon, loss4 FROM "PLAN_BCSX_OK" WHERE masc4='7.1' AND ngay>=v_ngay
        UNION ALL
        SELECT ngay, masp, makhuon, loss5 FROM "PLAN_BCSX_OK" WHERE masc5='7.1' AND ngay>=v_ngay
        UNION ALL
        SELECT ngay, masp, makhuon, loss6 FROM "PLAN_BCSX_OK" WHERE masc6='7.1' AND ngay>=v_ngay
    )
    UPDATE "PRO_Dandori_Time_NewVer" p
       SET "dandoritimeBCSX" = b.losstime * 60
      FROM bangtam b
     WHERE b.masp = p."makhuonlap"
       AND b.makhuon = p."sokhuonlap"
       AND b.ngay = p.ngay
       AND p.ngay >= v_ngay;
END;
$function$
```

---

## 83. `PRO_Material_Tinhnhuachuanbi`

```sql
CREATE OR REPLACE FUNCTION public."PRO_Material_Tinhnhuachuanbi"(p_id integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_gio          varchar(25);
    v_gioplan      varchar(25);
    v_ngay         date;
    v_ngaygioplan  timestamp;
    v_ngaygiohakhuon timestamp;
    v_trugio       float;
    v_soluongonehour float;
    v_stt_cur      int;
    v_may_cur      varchar(50);
BEGIN
    -- Lấy thông tin dòng hiện tại
    SELECT gioplan, ngay, stt, may
      INTO v_gioplan, v_ngay, v_stt_cur, v_may_cur
      FROM "PLAN_Dandory" WHERE id = p_id;

    -- Lấy giờ hạ khuôn: giờ kế hoạch của dòng kế tiếp cùng ngày, cùng máy
    SELECT gioplan INTO v_gio
      FROM "PLAN_Dandory"
     WHERE ngay = v_ngay AND may = v_may_cur AND stt > v_stt_cur AND status <> 1
     ORDER BY stt, thutuhienthi
     LIMIT 1;

    -- 1. UPDATE giờ hạ khuôn
    IF v_gio IS NOT NULL AND v_gio <> '' THEN
        UPDATE "PLAN_Dandory" SET giohakhuon = v_gio WHERE id = p_id;
    ELSE
        IF EXTRACT(hour FROM v_gioplan::time) BETWEEN 8 AND 23 THEN
            UPDATE "PLAN_Dandory" SET giohakhuon = '20:00' WHERE id = p_id;
        ELSE
            UPDATE "PLAN_Dandory" SET giohakhuon = '08:00' WHERE id = p_id;
        END IF;
    END IF;

    -- 2. Tính thời gian
    IF EXTRACT(hour FROM v_gioplan::time) BETWEEN 8 AND 23 THEN
        v_ngaygioplan := v_ngay::timestamp + v_gioplan::interval;
    ELSE
        v_ngaygioplan := (v_ngay + 1)::timestamp + v_gioplan::interval;
    END IF;

    IF v_gio IS NOT NULL AND v_gio <> '' THEN
        IF EXTRACT(hour FROM v_gio::time) > 8 AND EXTRACT(hour FROM v_gio::time) < 23 THEN
            v_ngaygiohakhuon := v_ngay::timestamp + v_gio::interval;
        ELSE
            v_ngaygiohakhuon := (v_ngay + 1)::timestamp + v_gio::interval;
        END IF;
    ELSE
        v_ngaygiohakhuon := v_ngaygioplan; -- fallback
    END IF;

    v_trugio := EXTRACT(epoch FROM (v_ngaygiohakhuon - v_ngaygioplan)) / 3600.0;

    -- 3. Tra khối lượng/giờ
    SELECT (3600.0 / "avg_CT" * (socavity * avg_4cavity)) / 1000 INTO v_soluongonehour
      FROM "tb_AvgOfWeigh"
     WHERE avg_4cavity > 0 AND "avg_CT" > 0
       AND masp = (SELECT left(khuonlap, 8) FROM "PLAN_Dandory" WHERE id = p_id AND status <> 1);

    -- 4. UPDATE
    UPDATE "PLAN_Dandory"
       SET khoiluongcanonehour  = v_soluongonehour,
           tongkhoiluongcan      = v_soluongonehour * v_trugio,
           tocdoonehour          = v_soluongonehour / 25.0,
           sobaocancungcap       = (v_soluongonehour * v_trugio) / 25.0
     WHERE id = p_id;
END;
$function$
```

---

## 84. `PRO_SP_5ERROR_FROM_BCSX`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_5ERROR_FROM_BCSX"(p_khuonlap character varying, p_chitiet integer)
 RETURNS TABLE(stt bigint, ngay date, suco character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    DROP TABLE IF EXISTS _5error_tmp;
    CREATE TEMP TABLE _5error_tmp AS
    SELECT ngay::date, tensc1 AS suco FROM "PLAN_BCSX_OK"
     WHERE masc1 LIKE '7.%' AND masc1 <> '7.1' AND masp || '-' || makhuon = p_khuonlap
     ORDER BY ngay DESC LIMIT 5;

    INSERT INTO _5error_tmp
    SELECT ngay::date, tensc2 FROM "PLAN_BCSX_OK"
     WHERE masc2 LIKE '7.%' AND masc2 <> '7.1' AND masp || '-' || makhuon = p_khuonlap
     ORDER BY ngay DESC LIMIT 5;

    INSERT INTO _5error_tmp
    SELECT ngay::date, tensc3 FROM "PLAN_BCSX_OK"
     WHERE masc3 LIKE '7.%' AND masc3 <> '7.1' AND masp || '-' || makhuon = p_khuonlap
     ORDER BY ngay DESC LIMIT 5;

    INSERT INTO _5error_tmp
    SELECT ngay::date, tensc4 FROM "PLAN_BCSX_OK"
     WHERE masc4 LIKE '7.%' AND masc4 <> '7.1' AND masp || '-' || makhuon = p_khuonlap
     ORDER BY ngay DESC LIMIT 5;

    INSERT INTO _5error_tmp
    SELECT ngay::date, tensc5 FROM "PLAN_BCSX_OK"
     WHERE masc5 LIKE '7.%' AND masc5 <> '7.1' AND masp || '-' || makhuon = p_khuonlap
     ORDER BY ngay DESC LIMIT 5;

    INSERT INTO _5error_tmp
    SELECT ngay::date, tensc6 FROM "PLAN_BCSX_OK"
     WHERE masc6 LIKE '7.%' AND masc6 <> '7.1' AND masp || '-' || makhuon = p_khuonlap
     ORDER BY ngay DESC LIMIT 5;

    IF p_chitiet = 0 THEN
        -- Trả về 1 hàng: STT=0, NGAY=null, SUCO=chuỗi liệt kê
        RETURN QUERY
        SELECT 0::bigint, NULL::date,
               string_agg(rn::text || '. ' || t.suco, ', ' ORDER BY rn) AS suco
          FROM (
              SELECT ROW_NUMBER() OVER (ORDER BY ngay DESC) AS rn, suco
                FROM _5error_tmp LIMIT 5
          ) t;
    ELSE
        RETURN QUERY
        SELECT ROW_NUMBER() OVER (ORDER BY ngay DESC) AS stt, ngay, suco
          FROM _5error_tmp
         ORDER BY ngay DESC
         LIMIT 5;
    END IF;
END;
$function$
```

---

## 85. `PRO_SP_CHECK_DANDORI_LOST`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_CHECK_DANDORI_LOST"(p_startdate timestamp without time zone, p_enddate timestamp without time zone)
 RETURNS TABLE(may character varying, makhuon character varying, tensp character varying, giolaymau character varying)
 LANGUAGE sql
AS $function$
SELECT q.may,
       q.masp || '-' || q.dieno AS makhuon,
       q.tensp,
       q.giolaymau
  FROM "tb_listketquakiemtra" q
 WHERE q.tensp || '-' || q.dieno NOT IN (
     SELECT DISTINCT dt.khuonlap || '-' || dt.makhuonlap
       FROM "PRO_Dandori_Time" dt
      WHERE dt.ngayupdate >= p_startdate AND dt.ngayupdate <= p_enddate
 )
   AND q.ngayktra >= p_startdate
   AND q.ngayktra <= p_enddate
   AND q.hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)';
$function$
```

---

## 86. `PRO_SP_GET_DANDORI_ALL`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_ALL"()
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM') AS thang,
       count(*) AS totaldandoritimes,
       sum("DandoriTime") AS totaldandoritime,
       sum("Target") AS totaldandoritargettime,
       sum("DandoriTime") - sum("Target") AS totaldandoriovertargettime,
       sum("DandoriTime") / NULLIF(count(*),0) AS avgdandoritime,
       sum("Target") / NULLIF(count(*),0) AS avgdandoritargettime
  FROM "PRO_Dandori_Time"
 GROUP BY to_char(ngayupdate, 'YYYY-MM');
$function$
```

---

## 87. `PRO_SP_GET_DANDORI_BY_MACHINE_TYPE`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_BY_MACHINE_TYPE"(p_mctype character varying)
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM'),
       count(*), sum("DandoriTime"), sum("Target"),
       sum("DandoriTime") - sum("Target"),
       sum("DandoriTime") / NULLIF(count(*),0),
       sum("Target") / NULLIF(count(*),0)
  FROM "PRO_Dandori_Time"
 WHERE loaimay = p_mctype
 GROUP BY to_char(ngayupdate, 'YYYY-MM');
$function$
```

---

## 88. `PRO_SP_GET_DANDORI_BY_MACHINE_TYPE_AND_SHIFT`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_BY_MACHINE_TYPE_AND_SHIFT"(p_mctype character varying, p_ca character varying)
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM'),
       count(*), sum("DandoriTime"), sum("Target"),
       sum("DandoriTime") - sum("Target"),
       sum("DandoriTime") / NULLIF(count(*),0),
       sum("Target") / NULLIF(count(*),0)
  FROM "PRO_Dandori_Time"
 WHERE loaimay = p_mctype AND ca = p_ca
 GROUP BY to_char(ngayupdate, 'YYYY-MM');
$function$
```

---

## 89. `PRO_SP_GET_DANDORI_BY_MATERIAL`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_BY_MATERIAL"(p_nhuathao character varying, p_nhualap character varying)
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM'),
       count(*), sum("DandoriTime"), sum("Target"),
       sum("DandoriTime") - sum("Target"),
       sum("DandoriTime") / NULLIF(count(*),0),
       sum("Target") / NULLIF(count(*),0)
  FROM "PRO_Dandori_Time"
 WHERE nhualap = p_nhualap AND nhuathao = p_nhuathao
 GROUP BY to_char(ngayupdate, 'YYYY-MM');
$function$
```

---

## 90. `PRO_SP_GET_DANDORI_BY_MATERIAL_COMBINE_SHIFT`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_BY_MATERIAL_COMBINE_SHIFT"(p_nhuathao character varying, p_nhualap character varying, p_shift character varying)
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM'),
       count(*), sum("DandoriTime"), sum("Target"),
       sum("DandoriTime") - sum("Target"),
       sum("DandoriTime") / NULLIF(count(*),0),
       sum("Target") / NULLIF(count(*),0)
  FROM "PRO_Dandori_Time"
 WHERE nhualap = p_nhualap AND nhuathao = p_nhuathao AND ca = p_shift
 GROUP BY to_char(ngayupdate, 'YYYY-MM');
$function$
```

---

## 91. `PRO_SP_GET_DANDORI_BY_MATERIAL_COMBINE_SHIFT_SUM`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_BY_MATERIAL_COMBINE_SHIFT_SUM"()
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision, nhuathao character varying, nhualap character varying, ca character varying)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM'),
       count(*), sum("DandoriTime"), sum("Target"),
       sum("DandoriTime") - sum("Target"),
       sum("DandoriTime") / NULLIF(count(*),0),
       sum("Target") / NULLIF(count(*),0),
       nhuathao, nhualap, ca
  FROM "PRO_Dandori_Time"
 GROUP BY to_char(ngayupdate, 'YYYY-MM'), nhuathao, nhualap, ca;
$function$
```

---

## 92. `PRO_SP_GET_DANDORI_BY_MATERIAL_SUM`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_BY_MATERIAL_SUM"()
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision, nhuathao character varying, nhualap character varying)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM'),
       count(*), sum("DandoriTime"), sum("Target"),
       sum("DandoriTime") - sum("Target"),
       sum("DandoriTime") / NULLIF(count(*),0),
       sum("Target") / NULLIF(count(*),0),
       nhuathao, nhualap
  FROM "PRO_Dandori_Time"
 GROUP BY to_char(ngayupdate, 'YYYY-MM'), nhuathao, nhualap;
$function$
```

---

## 93. `PRO_SP_GET_DANDORI_BY_SHIFT`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_DANDORI_BY_SHIFT"(p_ca character varying)
 RETURNS TABLE(thang character varying, totaldandoritimes bigint, totaldandoritime double precision, totaldandoritargettime double precision, totaldandoriovertargettime double precision, avgdandoritime double precision, avgdandoritargettime double precision)
 LANGUAGE sql
AS $function$
SELECT to_char(ngayupdate, 'YYYY-MM'),
       count(*), sum("DandoriTime"), sum("Target"),
       sum("DandoriTime") - sum("Target"),
       sum("DandoriTime") / NULLIF(count(*),0),
       sum("Target") / NULLIF(count(*),0)
  FROM "PRO_Dandori_Time"
 WHERE ca = p_ca
 GROUP BY to_char(ngayupdate, 'YYYY-MM'), ca;
$function$
```

---

## 94. `PRO_SP_GET_TARGET_TIME`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_GET_TARGET_TIME"(p_khuonha character varying, p_khuonlap character varying, p_loaimay character varying)
 RETURNS TABLE(target double precision, klasa double precision, purget double precision, nhuathao character varying, nhualap character varying)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_nhuaha  varchar(50);
    v_nhualap varchar(50);
BEGIN
    SELECT loainhua INTO v_nhuaha  FROM "PRO_Khuon_Nhua" WHERE tenkhuon = p_khuonha  LIMIT 1;
    SELECT loainhua INTO v_nhualap FROM "PRO_Khuon_Nhua" WHERE tenkhuon = p_khuonlap LIMIT 1;

    RETURN QUERY
    SELECT
        (SELECT t.target FROM "PRO_Dandori_Time_By_Material" t
          WHERE t.nhuathao = v_nhuaha AND t.nhualap = v_nhualap AND t.loaimay = p_loaimay LIMIT 1),
        (SELECT t.klasa  FROM "PRO_Dandori_Time_By_Material" t
          WHERE t.nhuathao = v_nhuaha AND t.nhualap = v_nhualap AND t.loaimay = p_loaimay LIMIT 1),
        (SELECT t.purge  FROM "PRO_Dandori_Time_By_Material" t
          WHERE t.nhuathao = v_nhuaha AND t.nhualap = v_nhualap AND t.loaimay = p_loaimay LIMIT 1),
        v_nhuaha,
        v_nhualap;
END;
$function$
```

---

## 95. `PRO_SP_UPDATE_BARCODE`

```sql
CREATE OR REPLACE FUNCTION public."PRO_SP_UPDATE_BARCODE"()
 RETURNS void
 LANGUAGE sql
AS $function$
UPDATE "PRO_MATERIAL_INFO"
   SET barcode = 'OUTMLD' || id::text;
$function$
```

---

## 96. `SP_ADD_PART_QUALITY`

```sql
CREATE OR REPLACE FUNCTION public."SP_ADD_PART_QUALITY"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- 1. Insert các khuôn chưa có trong PARTQUALITY
    INSERT INTO "tb_PartQuality" (masp, tensp, sokhuon)
    SELECT Part_no, part_name, Die_no
      FROM "tb_Part_master"
     WHERE Part_no || Die_no NOT IN (
         SELECT masp || sokhuon FROM "tb_PartQuality"
     );

    -- 2. Reset MAY
    UPDATE "tb_PartQuality" SET may = NULL;

    -- 3. Cập nhật MAY từ lần kiểm tra mới nhất theo máy
    UPDATE "tb_PartQuality" pq
       SET may = t.may
      FROM (
          SELECT may, masp, dieno
            FROM "tb_listketquakiemtra"
           WHERE id IN (SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY may)
      ) t
     WHERE pq."Masp" = t."Masp" AND pq."SoKhuon" = t.dieno;

    -- 4. Cập nhật RANK + LASTRUN từ lần kiểm tra mới nhất theo masp+dieno
    UPDATE "tb_PartQuality" pq
       SET rankngoaiquan = t.rank, lastrun = t.ngayktra
      FROM (
          SELECT masp, dieno, rank, ngayktra
            FROM "tb_listketquakiemtra"
           WHERE id IN (SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY masp, dieno)
      ) t
     WHERE pq."Masp" = t."Masp" AND pq."SoKhuon" = t.dieno;
END;
$function$
```

---

## 97. `SP_ANALYSIS_APPEARANCE`

```sql
CREATE OR REPLACE FUNCTION public."SP_ANALYSIS_APPEARANCE"(p_startdate timestamp without time zone, p_enddate timestamp without time zone)
 RETURNS TABLE(hinhthuc character varying, masp character varying, dieno character varying, a bigint, b bigint, phantram double precision)
 LANGUAGE sql
AS $function$
SELECT DISTINCT ng.hinhthuc, ng.masp, ng.dieno, ng.a, al.b,
       round((ng.a::float / NULLIF(al.b,0)::float * 100)::numeric, 2) AS phantram
  FROM (
      SELECT hinhthuc, masp, dieno, count(*) AS a
        FROM "tb_listketquakiemtra"
       WHERE ketquatonghop = 'NG' AND ngayktra >= p_startdate AND ngayktra <= p_enddate
       GROUP BY hinhthuc, masp, dieno
  ) ng
  JOIN (
      SELECT hinhthuc, masp, dieno, count(*) AS b
        FROM "tb_listketquakiemtra"
       WHERE ngayktra >= p_startdate AND ngayktra <= p_enddate
       GROUP BY hinhthuc, masp, dieno
  ) al ON ng.hinhthuc = al.hinhthuc AND ng.masp = al.masp AND ng.dieno = al.dieno
 ORDER BY b DESC;
$function$
```

---

## 98. `SP_ANALYSIS_APPEARANCE_DETAIL`

```sql
CREATE OR REPLACE FUNCTION public."SP_ANALYSIS_APPEARANCE_DETAIL"(p_startdate timestamp without time zone, p_enddate timestamp without time zone)
 RETURNS TABLE(hinhthuc character varying, partno character varying, dieno character varying, a bigint, b bigint, phantram double precision, rank character varying, vitri character varying)
 LANGUAGE sql
AS $function$
SELECT DISTINCT ng.hinhthuc, ng.partno, ng.dieno, ng.a, al.b,
       round((ng.a::float / NULLIF(al.b,0)::float * 100)::numeric, 2) AS phantram,
       ng.rank, ng.vitri
  FROM (
      SELECT hinhthuc, partno, dieno, rank, vitri, count(*) AS a
        FROM "tb_chitietketqua_newver"
       WHERE ketquakt = 'NG' AND ngaykiemtra >= p_startdate AND ngaykiemtra <= p_enddate
       GROUP BY hinhthuc, partno, dieno, rank, vitri
  ) ng
  JOIN (
      SELECT hinhthuc, partno, dieno, rank, vitri, count(*) AS b
        FROM "tb_chitietketqua_newver"
       WHERE ngaykiemtra >= p_startdate AND ngaykiemtra <= p_enddate
       GROUP BY hinhthuc, partno, dieno, rank, vitri
  ) al ON ng.hinhthuc = al.hinhthuc AND ng.partno = al.partno AND ng.dieno = al.dieno
       AND ng.rank = al.rank AND ng.vitri = al.vitri
 WHERE al.b > 0
 ORDER BY b DESC;
$function$
```

---

## 99. `SP_ANALYSIS_MEASUREMENT`

```sql
CREATE OR REPLACE FUNCTION public."SP_ANALYSIS_MEASUREMENT"()
 RETURNS TABLE(masp character varying, sokhuon character varying, rank character varying, a bigint, b bigint, phantram double precision)
 LANGUAGE sql
AS $function$
SELECT c.masp, c.sokhuon, c.rank, c.a, d.b,
       round((c.a::float / NULLIF(d.b,0)::float * 100)::numeric, 2) AS phantram
  FROM (
      SELECT masp, sokhuon, rank, count(*) AS a
        FROM "tb_PartAnalysis"
       GROUP BY masp, sokhuon, rank
  ) c
  JOIN (
      SELECT masp, sokhuon, count(*) AS b
        FROM "tb_PartAnalysis"
       GROUP BY masp, sokhuon
  ) d ON c.masp = d.masp AND c.sokhuon = d.sokhuon;
$function$
```

---

## 100. `SP_CHANGINGPOINTTOSHOT`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHANGINGPOINTTOSHOT"(p_starttime timestamp without time zone, p_endtime timestamp without time zone, p_nguoiupdate character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_ChangingPointToShot";

    INSERT INTO "tb_ChangingPointToShot" (
        may, masp, tensp, sokhuon, soluotcheck,
        solankhoidong, khoidong, ngratekhoidong,
        solansampling, sampling, ngratesampling,
        solansuco, suco, ngratesuco,
        solanbaoduong, baoduong, ngratebaoduong,
        "solanFA", "FA", "ngrateFA",
        "solanTVP", "TVP", "ngrateTVP",
        "solanMT", "MT", "ngrateMT",
        "solanMP", "MPfirstlot", "ngrateMP",
        solanhinhthuckhac, hinhthuckhac, ngratehinhthuckhac,
        ngrate, solandungmay, dungmay, ngratedungmay,
        nguoiupdate, ngayupdate
    )
    SELECT may, masp, tensp, dieno, count(*),
           0,0,0, 0,0,0, 0,0,0, 0,0,0, 0,0,0, 0,0,0, 0,0,0, 0,0,0, 0,0,0, 0, 0,0,0,
           p_nguoiupdate, CURRENT_TIMESTAMP
      FROM "tb_listketquakiemtra"
     WHERE ngayktra >= p_starttime AND ngayktra <= p_endtime
     GROUP BY may, masp, tensp, dieno;

    -- ── Số lần check theo hình thức ──────────────────────────
    UPDATE "tb_ChangingPointToShot" c
       SET solankhoidong = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET solansampling = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'SAMPLING'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET solansuco = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'SỰ CỐ'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET solanbaoduong = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'BẢO DƯỠNG'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "solanFA" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'FA'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "solanTVP" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'TVP'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "solanMT" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'MT'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "solanMP" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'MP FIRST LOT'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET solanhinhthuckhac = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'HÌNH THỨC KHÁC'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET solandungmay = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime AND hinhthuc = N'KHUÔN HẠ/ DỪNG MÁY (LS)'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    -- ── Số lần NG theo hình thức ─────────────────────────────
    UPDATE "tb_ChangingPointToShot" c SET khoidong = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND hinhthuc=N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)' AND ketquatonghop=N'NG'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET sampling = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'SAMPLING'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET suco = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'SỰ CỐ'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET baoduong = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'BẢO DƯỠNG'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "FA" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'FA'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "TVP" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'TVP'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "MT" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'MT'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET "MPfirstlot" = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'MP FIRST LOT'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET hinhthuckhac = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'HÌNH THỨC KHÁC'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    UPDATE "tb_ChangingPointToShot" c SET dungmay = t.dem
      FROM (SELECT may,masp,dieno,count(*) AS dem FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_starttime AND p_endtime
               AND ketquatonghop=N'NG' AND hinhthuc=N'KHUÔN HẠ/ DỪNG MÁY (LS)'
             GROUP BY may,masp,dieno) t
     WHERE c.masp=t.masp AND c.sokhuon=t.dieno AND c.may=t.may;

    -- ── Tính NG rate ─────────────────────────────────────────
    UPDATE "tb_ChangingPointToShot" SET ngratekhoidong  = round((khoidong::float    / NULLIF(solankhoidong,0)::float  * 100)::numeric, 2) WHERE solankhoidong  <> 0;
    UPDATE "tb_ChangingPointToShot" SET ngratesampling  = round((sampling::float    / NULLIF(solansampling,0)::float  * 100)::numeric, 2) WHERE solansampling  <> 0;
    UPDATE "tb_ChangingPointToShot" SET ngratesuco      = round((suco::float        / NULLIF(solansuco,0)::float      * 100)::numeric, 2) WHERE solansuco      <> 0;
    UPDATE "tb_ChangingPointToShot" SET ngratebaoduong  = round((baoduong::float    / NULLIF(solanbaoduong,0)::float  * 100)::numeric, 2) WHERE solanbaoduong  <> 0;
    UPDATE "tb_ChangingPointToShot" SET "ngrateFA"        = round(("FA"::float          / NULLIF("solanFA",0)::float        * 100)::numeric, 2) WHERE "solanFA"        <> 0;
    UPDATE "tb_ChangingPointToShot" SET "ngrateTVP"       = round(("TVP"::float         / NULLIF("solanTVP",0)::float       * 100)::numeric, 2) WHERE "solanTVP"       <> 0;
    -- NOTE: proc gốc dùng SOLANTVP cho NGRATEMT (copy-paste bug trong gốc, giữ nguyên để tương thích)
    UPDATE "tb_ChangingPointToShot" SET "ngrateMT"        = round((mt::float          / NULLIF("solanTVP",0)::float       * 100)::numeric, 2) WHERE "solanTVP"       <> 0;
    UPDATE "tb_ChangingPointToShot" SET "ngrateMP"        = round(("MPfirstlot"::float  / NULLIF("solanMP",0)::float        * 100)::numeric, 2) WHERE "solanMP"        <> 0;
    UPDATE "tb_ChangingPointToShot" SET ngratehinhthuckhac = round((hinhthuckhac::float / NULLIF(solanhinhthuckhac,0)::float * 100)::numeric, 2) WHERE solanhinhthuckhac <> 0;
    UPDATE "tb_ChangingPointToShot" SET ngratedungmay   = round((dungmay::float     / NULLIF(solandungmay,0)::float   * 100)::numeric, 2) WHERE solandungmay   <> 0;
END;
$function$
```

---

## 101. `SP_CHANGINGPOINTTOSHOTRANK`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHANGINGPOINTTOSHOTRANK"(p_starttime timestamp without time zone, p_endtime timestamp without time zone, p_nguoiupdate character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_ChangingPoinToShotRank";

    INSERT INTO "tb_ChangingPoinToShotRank" (
        masp, tensp, sokhuon, solanktra, rank, item, may,
        solanng, ngrate, nguoiupdate, ngayupdate
    )
    SELECT partno, partname, dieno, count(*), vitri, rank, mayduc,
           0, 0, p_nguoiupdate, CURRENT_TIMESTAMP
      FROM "tb_chitietketqua"
     WHERE ngaykiemtra >= p_starttime AND ngaykiemtra <= p_endtime
     GROUP BY partno, partname, dieno, vitri, rank, mayduc;

    UPDATE "tb_ChangingPoinToShotRank" r
       SET solanng = t.dem
      FROM (
          SELECT partno, partname, dieno, count(*) AS dem, vitri, rank, mayduc, ketquakt
            FROM "tb_chitietketqua"
           WHERE ngaykiemtra >= p_starttime AND ngaykiemtra <= p_endtime
           GROUP BY partno, partname, dieno, vitri, rank, mayduc, ketquakt
      ) t
     WHERE r.masp = t.partno AND r.sokhuon = t.dieno AND r.rank = t.rank
       AND r.item = t.vitri AND r.may = t.mayduc AND t.ketquakt = N'NG';

    UPDATE "tb_ChangingPoinToShotRank"
       SET ngrate = round((solanng::float / NULLIF(solanktra,0)::float * 100)::numeric, 2)
     WHERE solanktra <> 0;
END;
$function$
```

---

## 102. `SP_CHECK_CORRECT_PART`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHECK_CORRECT_PART"(p_may character varying, p_khuon character varying)
 RETURNS TABLE(result integer)
 LANGUAGE sql
AS $function$
    SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END::integer
    FROM "IMM"
    WHERE "Mayduc" = p_may;
$function$
```

---

## 103. `SP_CHECK_DATA_SHEET`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHECK_DATA_SHEET"(p_starttime timestamp without time zone, p_endtime timestamp without time zone)
 RETURNS TABLE(khuon character varying, tensp character varying, hinhthuc character varying, ngayktra timestamp without time zone)
 LANGUAGE sql
AS $function$
SELECT t.khuon, tensp, hinhthuc, ngayktra FROM (
    SELECT masp || '-' || dieno AS khuon, tensp, hinhthuc, ngayktra
      FROM "tb_listketquakiemtra"
     WHERE id IN (
         SELECT max(id) FROM "tb_listketquakiemtra"
          WHERE ngayktra >= p_starttime AND ngayktra <= p_endtime
            AND masp || '-' || dieno IN (
                SELECT "Part_no" FROM "Data_Sheet"
                 WHERE nguoilapcode IS NOT NULL AND nguoilapcode <> ''
            )
          GROUP BY masp, dieno
     )
) t
WHERE t.khuon NOT IN (
    SELECT masp || '-' || sokhuon FROM "tb_Issue_Dimension"
     WHERE ngaysx >= p_starttime AND ngaysx <= p_endtime
);
$function$
```

---

## 104. `SP_CHECK_GIOI_HAN_KICH_THUOC`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHECK_GIOI_HAN_KICH_THUOC"()
 RETURNS TABLE(masp character varying, sokhuon character varying, items character varying, vitri character varying, cavity character varying, duplicate bigint)
 LANGUAGE sql
AS $function$
SELECT masp, sokhuon, items, vitri, Cavity, count(*)
  FROM "tb_HangMucDoChiTietNewVer"
 GROUP BY masp, sokhuon, items, vitri, Cavity
HAVING count(*) > 1
 ORDER BY count(*) DESC;
$function$
```

---

## 105. `SP_CHECK_HANG_MUC_DO`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHECK_HANG_MUC_DO"()
 RETURNS TABLE(groupname character varying, items character varying, vitri character varying, duplicate bigint)
 LANGUAGE sql
AS $function$
SELECT groupname, items, vitri, count(*)
  FROM "tb_HangMucDoNewVer"
 GROUP BY groupname, items, vitri
HAVING count(*) > 1
 ORDER BY count(*) DESC;
$function$
```

---

## 106. `SP_CHECK_HANG_MUC_DO_FA`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHECK_HANG_MUC_DO_FA"()
 RETURNS TABLE(groupname character varying, items character varying, vitri character varying, gioihanduoifa double precision, gioihanduoidrw double precision, gioihantrendrw double precision, gioihantrenfa double precision, finallower double precision, finalupper double precision, loi character varying)
 LANGUAGE sql
AS $function$
SELECT * FROM (
    SELECT p.groupname, p.items, p.vitri,
           v.gioihanduoifa, p.gioihanduoidrw, p.gioihantrendrw,
           v.gioihantrenfa, v.finallower, v.finalupper,
           N'GIỚI HẠN DƯỚI FA > DRW' AS loi
      FROM "tb_HangMucDoNewVer" p
      JOIN "tb_HangMucDoChiTietNewVer" v
        ON v.items = p.items AND v.vitri = p.vitri AND v.groupname = p.groupname
     WHERE v.gioihanduoifa <> 1000 AND v.gioihanduoifa > p.gioihanduoidrw
    UNION
    SELECT p.groupname, p.items, p.vitri,
           v.gioihanduoifa, p.gioihanduoidrw, p.gioihantrendrw,
           v.gioihantrenfa, v.finallower, v.finalupper,
           N'GIỚI HẠN TRÊN DRW > FA'
      FROM "tb_HangMucDoNewVer" p
      JOIN "tb_HangMucDoChiTietNewVer" v
        ON v.items = p.items AND v.vitri = p.vitri AND v.groupname = p.groupname
     WHERE v.gioihantrenfa <> 1000 AND v.gioihantrenfa < p.gioihantrendrw
) t
ORDER BY groupname;
$function$
```

---

## 107. `SP_CHECK_HANG_MUC_DO_MP`

```sql
CREATE OR REPLACE FUNCTION public."SP_CHECK_HANG_MUC_DO_MP"()
 RETURNS TABLE(groupname character varying, items character varying, vitri character varying, gioihanduoimp double precision, gioihanduoidrw double precision, gioihantrendrw double precision, gioihantrenmp double precision, loi character varying)
 LANGUAGE sql
AS $function$
SELECT groupname, items, vitri, gioihanduoimp, gioihanduoidrw, gioihantrendrw, gioihantrenmp,
       N'GIỚI HẠN TRÊN DRW > MP'
  FROM "tb_HangMucDoNewVer"
 WHERE gioihantrenmp <> 1000 AND gioihantrendrw > gioihantrenmp
UNION
SELECT groupname, items, vitri, gioihanduoimp, gioihanduoidrw, gioihantrendrw, gioihantrenmp,
       N'GIỚI HẠN DƯỚI MP > DRW'
  FROM "tb_HangMucDoNewVer"
 WHERE gioihanduoimp <> 1000 AND gioihanduoidrw < gioihanduoimp
UNION
SELECT DISTINCT ch.groupname, ch.items, ch.vitri,
       gr.gioihanduoimp, ch.gioihanduoifa, ch.gioihantrenfa, gr.gioihantrenmp,
       N'GIỚI HẠN DƯỚI MP > FA'
  FROM "tb_HangMucDoChiTietNewVer" ch
  JOIN "tb_HangMucDoNewVer" gr ON ch.groupname=gr.groupname AND ch.items=gr.items AND ch.vitri=gr.vitri
 WHERE gr.gioihanduoimp<>1000 AND ch.gioihanduoifa<>1000 AND ch.gioihanduoifa < gr.gioihanduoimp
UNION
SELECT DISTINCT ch.groupname, ch.items, ch.vitri,
       gr.gioihanduoimp, ch.gioihanduoifa, ch.gioihantrenfa, gr.gioihantrenmp,
       N'GIỚI HẠN TRÊN MP < FA'
  FROM "tb_HangMucDoChiTietNewVer" ch
  JOIN "tb_HangMucDoNewVer" gr ON ch.groupname=gr.groupname AND ch.items=gr.items AND ch.vitri=gr.vitri
 WHERE ch.gioihantrenfa<>1000 AND gr.gioihantrenmp<>1000 AND ch.gioihantrenfa > gr.gioihantrenmp;
$function$
```

---

## 108. `SP_CONVERT_KE_HOACH_NAM`

```sql
CREATE OR REPLACE FUNCTION public."SP_CONVERT_KE_HOACH_NAM"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_plan_cols   text;
    v_actual_cols text;
    v_sql         text;
BEGIN
    -- Lấy danh sách cột *_PLAN và *_ACTUAL từ information_schema
    SELECT string_agg(quote_ident(column_name), ',' ORDER BY ordinal_position)
      INTO v_plan_cols
      FROM information_schema.columns
     WHERE table_name = 'PM_YEARLYPLAN' AND column_name LIKE '%_PLAN%';

    SELECT string_agg(quote_ident(column_name), ',' ORDER BY ordinal_position)
      INTO v_actual_cols
      FROM information_schema.columns
     WHERE table_name = 'PM_YEARLYPLAN' AND column_name LIKE '%_ACTUAL%';

    -- PLAN: DELETE + INSERT dạng UNPIVOT thủ công dùng VALUES/LATERAL
    EXECUTE format(
        'DELETE FROM "PM_YearlyPlanConvert";
         INSERT INTO "PM_YearlyPlanConvert" (loaithietbi,tenthietbi,loaibd,sothang,thang,ngay,idconvert)
         SELECT loaithietbi,tenthietbi,loaibd,sothang,v.thang,v.ngay,id
           FROM "PM_YearlyPlan",
                LATERAL (VALUES %s) AS v(thang,ngay)
          WHERE v.ngay IS NOT NULL',
        -- Tạo chuỗi VALUES: ('JAN_PLAN', jan_plan), ...
        (SELECT string_agg(format('(%L, %I)', column_name, column_name), ',')
           FROM information_schema.columns
          WHERE table_name = 'PM_YEARLYPLAN' AND column_name LIKE '%_PLAN%')
    );

    -- ACTUAL: tương tự
    EXECUTE format(
        'DELETE FROM "PM_YearlyPlanConvert_Actual";
         INSERT INTO "PM_YearlyPlanConvert_Actual" (loaithietbi,tenthietbi,loaibd,sothang,thang,ngay,idconvert)
         SELECT loaithietbi,tenthietbi,loaibd,sothang,v.thang,v.ngay,id
           FROM "PM_YearlyPlan",
                LATERAL (VALUES %s) AS v(thang,ngay)
          WHERE v.ngay IS NOT NULL',
        (SELECT string_agg(format('(%L, %I)', column_name, column_name), ',')
           FROM information_schema.columns
          WHERE table_name = 'PM_YEARLYPLAN' AND column_name LIKE '%_ACTUAL%')
    );

    DELETE FROM "PM_YearlyPlanConvert" WHERE ngay = 0;
    DELETE FROM "PM_YearlyPlanConvert_Actual" WHERE ngay = 0;

    -- Cập nhật NGAYPLANCONVERT (12 tháng)
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 1, ngay::int) WHERE thang='JAN_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 2, ngay::int) WHERE thang='FEB_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 3, ngay::int) WHERE thang='MAR_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 4, ngay::int) WHERE thang='APR_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 5, ngay::int) WHERE thang='MAY_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 6, ngay::int) WHERE thang='JUN_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 7, ngay::int) WHERE thang='JUL_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 8, ngay::int) WHERE thang='AUG_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int, 9, ngay::int) WHERE thang='SEP_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int,10, ngay::int) WHERE thang='OCT_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int,11, ngay::int) WHERE thang='NOV_PLAN';
    UPDATE "PM_YearlyPlanConvert" SET ngayplanconvert = make_date(extract(year FROM current_date)::int,12, ngay::int) WHERE thang='DEC_PLAN';

    -- Cập nhật NGAYACTUALCONVERT (12 tháng) từ ACTUAL
    UPDATE "PM_YearlyPlanConvert" c
       SET ngayactualconvert = make_date(extract(year FROM current_date)::int, m.mo, a.ngay::int)
      FROM "PM_YearlyPlanConvert_Actual" a,
           (VALUES ('JAN_PLAN','JAN_ACTUAL',1),('FEB_PLAN','FEB_ACTUAL',2),('MAR_PLAN','MAR_ACTUAL',3),
                   ('APR_PLAN','APR_ACTUAL',4),('MAY_PLAN','MAY_ACTUAL',5),('JUN_PLAN','JUN_ACTUAL',6),
                   ('JUL_PLAN','JUL_ACTUAL',7),('AUG_PLAN','AUG_ACTUAL',8),('SEP_PLAN','SEP_ACTUAL',9),
                   ('OCT_PLAN','OCT_ACTUAL',10),('NOV_PLAN','NOV_ACTUAL',11),('DEC_PLAN','DEC_ACTUAL',12)
           ) m(plan_col, actual_col, mo)
     WHERE c.thang = m.plan_col
       AND a.thang  = m.actual_col
       AND c.loaithietbi = a.loaithietbi AND c.tenthietbi = a.tenthietbi
       AND c.loaibd = a.loaibd AND c.sothang = a.sothang AND c.idconvert = a.idconvert;
END;
$function$
```

---

## 109. `SP_DIMENSION_CHUANHOA`

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

## 110. `SP_DIMENSION_CHUANHOA_CAVITY`

```sql
CREATE OR REPLACE FUNCTION public."SP_DIMENSION_CHUANHOA_CAVITY"(p_cavity character varying)
 RETURNS void
 LANGUAGE sql
AS $function$
UPDATE "tb_Report_DimensionDataForSupplier"
   SET cavityno = 'NONE', data1 = NULL, judge = NULL
 WHERE cavityno = p_cavity;
$function$
```

---

## 111. `SP_DIMENSION_KMK`

```sql
CREATE OR REPLACE FUNCTION public."SP_DIMENSION_KMK"()
 RETURNS void
 LANGUAGE sql
AS $function$
-- Stub: Nội dung proc gốc SQL Server toàn bộ đều là comment,
--       chưa có implementation. Giữ để tương thích tên hàm.
$function$
```

---

## 112. `SP_DIMENSION_KMK_SHOW`

```sql
CREATE OR REPLACE FUNCTION public."SP_DIMENSION_KMK_SHOW"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_lasttime timestamp;
BEGIN
    SELECT ngayxuat INTO v_lasttime
      FROM "tb_chitietkqdo"
     WHERE ngayxuat IS NOT NULL
     ORDER BY id DESC
     LIMIT 1;

    IF v_lasttime IS NULL THEN
        v_lasttime := CURRENT_TIMESTAMP - interval '1 day';
    END IF;

    UPDATE "tb_chitietkqdo"
       SET hienthi = true
     WHERE ngayktra > v_lasttime
       AND (
           (masp IN ('QC7-1280','QC7-8541','QC7-1332','QC7-8790')
            AND (items=744 AND vitri IN ('MIN','MAX') OR items=743 AND vitri IN ('MIN','MAX') OR items=666 AND vitri='NONE'))
           OR
           (masp IN ('QC4-6500','QC4-7483','QC4-7487','QC5-2166','QC5-5495','QC7-5717','QC7-8488')
            AND (items=553 AND vitri<>'NONE'
                 OR items=642 AND vitri IN ('TOP [R] <DUMMY>','BOT <DUMMY>')
                 OR items=650 AND vitri='NONE'))
           OR
           (masp='QC4-6407'
            AND (items=319 AND vitri IN ('MIN','MAX') OR items=320 AND vitri IN ('MIN','MAX')
                 OR items=343 AND vitri IN ('M-P3','M-P4','N-P3','N-P4')
                 OR items=434 AND vitri IN ('K','L')))
           OR
           (masp='QC7-1294'
            AND (items=324 AND vitri='YMAX' OR items=322 AND vitri IN ('MIN','MAX')
                 OR items=291 AND vitri IN ('ZC','ZD')
                 OR items=443 AND vitri IN ('SUR (G) ','SUR (H) ')))
           OR
           (masp='QC4-6409'
            AND (items=221 AND vitri='NONE' OR items=225 AND vitri='NONE'
                 OR items=278 AND vitri='NONE' OR items=241 AND vitri='IN'
                 OR items=231 AND vitri='IN'))
           OR
           (masp='QC7-1298'
            AND (items=206 AND vitri='NONE' OR items=216 AND vitri='NONE'
                 OR items=267 AND vitri='NONE' OR items=236 AND vitri='IN'
                 OR items=220 AND vitri='IN'))
           OR
           (masp='QC4-6452'
            AND items=230 AND vitri IN ('C3-1','C3-2','C3-3','C3-4','C3-5','C3-6'))
           OR
           (masp='QC5-5780'
            AND items=416 AND vitri IN ('A1-LINE R','A3-LINE R','A4-LINE R','A6-LINE R','A7-LINE R'))
       );
END;
$function$
```

---

## 113. `SP_DTS_GETTROUBLE`

```sql
CREATE OR REPLACE FUNCTION public."SP_DTS_GETTROUBLE"(p_masp character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_DTSBieuDo";
    INSERT INTO "tb_DTSBieuDo" (masp, shot, avg_shot, shot_trouble)
    SELECT masp || '-' || khuon AS masp,
           max(soshot) AS currentshot,
           (max(soshot) - min(soshot)) / NULLIF(count(*),0) AS avgt,
           (max(soshot) + (max(soshot) - min(soshot)) / NULLIF(count(*),0)) AS dudoan
      FROM "tb_DTSRequest"
     WHERE masp = p_masp
     GROUP BY masp, khuon
    HAVING (max(soshot) - min(soshot)) / NULLIF(count(*),0) > 0;
END;
$function$
```

---

## 114. `SP_DTS_GET_DIMENSION`

```sql
CREATE OR REPLACE FUNCTION public."SP_DTS_GET_DIMENSION"(p_masp character varying, p_sokhuon character varying, p_item integer, p_vitri character varying)
 RETURNS SETOF tb_chitietkqdo
 LANGUAGE sql
AS $function$
SELECT * FROM "tb_chitietkqdo"
 WHERE masp = p_masp AND sokhuon = p_sokhuon
   AND items = p_item AND vitri = p_vitri AND Cavity = 1
 ORDER BY idlistketquado DESC
 LIMIT 50;
$function$
```

---

## 115. `SP_DTS_INSERTCHANGINPOINT`

```sql
CREATE OR REPLACE FUNCTION public."SP_DTS_INSERTCHANGINPOINT"(p_songay integer, p_masp character varying, p_sokhuon character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "DTS_ChangingPoint";
    INSERT INTO "DTS_ChangingPoint" (
        masp, sokhuon, ngay, ca, loichinh, chitietloi,
        nguyennhanchinh, phantich5why, giaiphaptamthoi, giaiphaplaudai,
        mota, picunderinvestigating
    )
    SELECT m.Part_no, t."SoKhuon", t.ngay, t.ca, t.loichinh, t.chitietloi,
           t.nguyennhanchinh, t.phantich5why, t.giaiphaptamthoi, t.giaiphaplaudai,
           t.mota, t.picunderinvestigating
      FROM "DTS_DieTrouble" t
      JOIN "DTS_Die_Master" m ON t.tenkhuon = m."tenkhuon" AND t."SoKhuon" = m.Die_no
     WHERE m.Part_no = p_masp AND t."SoKhuon" = p_sokhuon AND anhhuongdenchatluong = true
     ORDER BY t.id DESC
     LIMIT 2;
END;
$function$
```

---

## 116. `SP_DTS_THONGKELOITHEOSOSHOT`

```sql
CREATE OR REPLACE FUNCTION public."SP_DTS_THONGKELOITHEOSOSHOT"()
 RETURNS TABLE(diename character varying, currentshot bigint, avgshottotrouble bigint, nextshottotrouble bigint)
 LANGUAGE sql
AS $function$
SELECT tenkhuon || '-' || sokhuon AS diename,
       max(shot) AS currentshot,
       (max(shot) - min(shot)) / NULLIF(count(*), 0)          AS avgshottotrouble,
       max(shot) + (max(shot) - min(shot)) / NULLIF(count(*), 0) AS nextshottotrouble
  FROM "DTS_DieTrouble"
 WHERE EXTRACT(YEAR FROM ngayupdate) > 2016
   AND loikhuon = true
 GROUP BY tenkhuon, sokhuon
 ORDER BY tenkhuon, sokhuon;
$function$
```

---

## 117. `SP_DTS_THONGKELOITHEOSOSHOT_SOLAN`

```sql
CREATE OR REPLACE FUNCTION public."SP_DTS_THONGKELOITHEOSOSHOT_SOLAN"()
 RETURNS TABLE(tenkhuon character varying, sokhuon character varying, loichinh character varying, solan bigint)
 LANGUAGE sql
AS $function$
SELECT tenkhuon, sokhuon, loichinh, count(*) AS solan
  FROM "DTS_DieTrouble"
 WHERE EXTRACT(YEAR FROM ngayupdate) > 2016
   AND loikhuon = true
 GROUP BY tenkhuon, sokhuon, loichinh
 ORDER BY solan DESC;
$function$
```

---

## 118. `SP_DTS_THONGKELOITHEOSOSHOT_SOLAN_THEO_KHUON`

```sql
CREATE OR REPLACE FUNCTION public."SP_DTS_THONGKELOITHEOSOSHOT_SOLAN_THEO_KHUON"(p_tenkhuon character varying)
 RETURNS TABLE(tenkhuon character varying, sokhuon character varying, loichinh character varying, solan bigint)
 LANGUAGE sql
AS $function$
SELECT tenkhuon, sokhuon, loichinh, count(*) AS solan
  FROM "DTS_DieTrouble"
 WHERE tenkhuon = p_tenkhuon
 GROUP BY tenkhuon, sokhuon, loichinh
 ORDER BY solan DESC;
$function$
```

---

## 119. `SP_EMAIL_SEND_DAILY_REPORT`

```sql
CREATE OR REPLACE FUNCTION public."SP_EMAIL_SEND_DAILY_REPORT"(p_ngaybatdau timestamp without time zone, p_ngayketthuc timestamp without time zone, p_datesend timestamp without time zone, p_shiftsend character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_dandori1               int := 0;
    v_dandoritrouble1        int := 0;
    v_2ndprocess1            int := 0;
    v_changing2ndprocess1    int := 0;
    v_oplinea                int := 0;
    v_oplineb                int := 0;
    v_oplinec                int := 0;
    v_oplined                int := 0;
    v_totalcheck1            int := 0;
    v_totalng1               int := 0;
    v_checktroubletime1      int := 0;
    v_checktroubletime1stop  int := 0;
    v_asssyreturntotalpart1  int := 0;
    v_asssyreturntotaltimes1 int := 0;
    v_asssyreturntotalpcs1   int := 0;
    v_totaldarw              int := 0;
    v_totalhangtach          int := 0;
    v_totalardrwrc           int := 0;
    v_totalngpartcantrepaire int := 0;
    v_totalngpart            int := 0;
    v_totalngpartdate        int := 0;
    v_dateandshift           varchar(50);
    v_NGsilver               int := 0;
    v_NGtapchat              int := 0;
    v_NGdinhdau              int := 0;
    v_NGloangnhua            int := 0;
    v_NGautohand             int := 0;
    v_NGshortmold            int := 0;
    v_NGflowmask             int := 0;
    v_NGchaykhi              int := 0;
    v_NGsinkmask             int := 0;
    v_nhatmau                int := 0;
    v_hakka                  int := 0;
    v_other                  int := 0;
BEGIN
    v_dateandshift := EXTRACT(DAY FROM p_datesend)::text || '-' ||
                      EXTRACT(MONTH FROM p_datesend)::text || '-' ||
                      EXTRACT(YEAR FROM p_datesend)::text || '-' || p_shiftsend;

    SELECT count(*) INTO v_dandoritrouble1 FROM "tb_listketquakiemtra"
     WHERE hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)' AND tinhtrang = 'NG'
       AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(*) INTO v_dandori1 FROM "tb_listketquakiemtra"
     WHERE hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)'
       AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(DISTINCT "Part_no") INTO v_2ndprocess1 FROM "tb_Burr"
     WHERE "Last_Update" BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(DISTINCT masp) INTO v_changing2ndprocess1 FROM "tb_Burr_Changing"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(*) INTO v_oplinea FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='A';
    SELECT count(*) INTO v_oplineb FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='B';
    SELECT count(*) INTO v_oplinec FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='C';
    SELECT count(*) INTO v_oplined FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='D';

    SELECT count(*) INTO v_totalcheck1 FROM "tb_listketquakiemtra"
     WHERE ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(*) INTO v_totalng1 FROM "tb_listketquakiemtra"
     WHERE ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc
       AND (ketquatonghop = N'NG' OR tinhtrang = N'NG' OR tinhtrang = N'TẠM THỜI');
    SELECT count(*) INTO v_checktroubletime1 FROM "tb_listketquakiemtra"
     WHERE hinhthuc = N'SỰ CỐ' AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(*) INTO v_checktroubletime1stop FROM "tb_listketquakiemtra"
     WHERE hinhthuc = N'SỰ CỐ' AND tinhtrang = 'NG'
       AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(*) INTO v_asssyreturntotalpart1 FROM "tb_Assy_Return"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(DISTINCT masp) INTO v_asssyreturntotaltimes1 FROM "tb_Assy_Return"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum("soNGkhachhang"),0) INTO v_asssyreturntotalpcs1 FROM "tb_Assy_Return"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(*) INTO v_totaldarw FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum(soluonghangtach),0) INTO v_totalhangtach FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum(soluongdarw),0) INTO v_totalardrwrc FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum(soluongng),0) INTO v_totalngpartcantrepaire FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT coalesce(sum("NGsilver"),0), coalesce(sum("NGchamden"),0), coalesce(sum("NGdinhdau"),0),
           coalesce(sum("NGloangnhua"),0), coalesce(sum("NGautohand"),0), coalesce(sum("NGshortmold"),0),
           coalesce(sum("NGflowmask"),0), coalesce(sum("NGchaykhi"),0), coalesce(sum("NGsinkmask"),0),
           coalesce(sum("Nhatmau"),0), coalesce(sum(hakka),0),
           coalesce(sum("NGtapchat")+sum("NGketsp")+sum("Xuoc")+sum(jig)+sum("Other"),0),
           coalesce(sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
                    sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
                    sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"),0)
      INTO v_NGsilver, v_NGtapchat, v_NGdinhdau, v_NGloangnhua, v_NGautohand,
           v_NGshortmold, v_NGflowmask, v_NGchaykhi, v_NGsinkmask, v_nhatmau,
           v_hakka, v_other, v_totalngpartdate
      FROM "tb_sospNG"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT coalesce(sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
                    sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
                    sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"),0)
      INTO v_totalngpart
      FROM "tb_sospNG"
     WHERE EXTRACT(MONTH FROM ngayupdate) = EXTRACT(MONTH FROM p_ngaybatdau)
       AND EXTRACT(YEAR FROM ngayupdate) = EXTRACT(YEAR FROM p_ngaybatdau)
       AND ngayupdate <= p_ngayketthuc;

    INSERT INTO "tb_EmailDailyReport" (
        datesend, shiftsend, dandori1, dandoritrouble1, "2ndprocess1",
        changing2ndprocess1, oplinea, oplineb, oplinec, oplined,
        totalcheck1, totalng1, checktroubletime1, checktroubletime1muststop, "N_A",
        asssyreturntotalpart1, asssyreturntotaltimes1, totaldarw, totalhangtach,
        totalngpartcantrepaire, totalngpartdate, dateandshift, totalngpart,
        asssyreturntotalpcs1, totalardrwrc, "NGsilver", "NGtapchat", "NGdinhdau",
        "NGloangnhua", "NGautohand", "NGshortmold", "NGflowmask", "NGchaykhi", "NGsinkmask",
        "Nhatmau", "Other", hakka
    ) VALUES (
        p_datesend, p_shiftsend, v_dandori1, v_dandoritrouble1, v_2ndprocess1,
        v_changing2ndprocess1, v_oplinea, v_oplineb, v_oplinec, v_oplined,
        v_totalcheck1, v_totalng1, v_checktroubletime1, v_checktroubletime1stop, NULL,
        v_asssyreturntotalpart1, v_asssyreturntotaltimes1, v_totaldarw, v_totalhangtach,
        v_totalngpartcantrepaire, v_totalngpartdate, v_dateandshift, v_totalngpart,
        v_asssyreturntotalpcs1, v_totalardrwrc, v_NGsilver, v_NGtapchat, v_NGdinhdau,
        v_NGloangnhua, v_NGautohand, v_NGshortmold, v_NGflowmask, v_NGchaykhi,
        v_NGsinkmask, v_nhatmau, v_other, v_hakka
    );

    -- Trả về dandori NG info (như SELECT ở giữa proc gốc)
    -- NOTE: Trong PostgreSQL function không trả nhiều result sets.
    -- Nếu C# cần kết quả này, tách ra function riêng SP_EMAIL_SEND_DAILY_REPORT_DANDORI_INFO.
END;
$function$
```

---

## 120. `SP_EMAIL_SEND_DAILY_REPORT_LINEA`

```sql
CREATE OR REPLACE FUNCTION public."SP_EMAIL_SEND_DAILY_REPORT_LINEA"(p_ngaybatdau timestamp without time zone, p_ngayketthuc timestamp without time zone, p_datesend timestamp without time zone, p_shiftsend character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_dandori1               int := 0;
    v_dandoritrouble1        int := 0;
    v_2ndprocess1            int := 0;
    v_changing2ndprocess1    int := 0;
    v_oplinea                int := 0;
    v_oplineb                int := 0;
    v_oplinec                int := 0;
    v_oplined                int := 0;
    v_totalcheck1            int := 0;
    v_totalng1               int := 0;
    v_checktroubletime1      int := 0;
    v_checktroubletime1stop  int := 0;
    v_asssyreturntotalpart1  int := 0;
    v_asssyreturntotaltimes1 int := 0;
    v_asssyreturntotalpcs1   int := 0;
    v_totaldarw              int := 0;
    v_totalhangtach          int := 0;
    v_totalardrwrc           int := 0;
    v_totalngpartcantrepaire int := 0;
    v_totalngpart            int := 0;
    v_totalngpartdate        int := 0;
    v_dateandshift           varchar(50);
    v_NGsilver               int := 0;
    v_NGtapchat              int := 0;
    v_NGdinhdau              int := 0;
    v_NGloangnhua            int := 0;
    v_NGautohand             int := 0;
    v_NGshortmold            int := 0;
    v_NGflowmask             int := 0;
    v_NGchaykhi              int := 0;
    v_NGsinkmask             int := 0;
    v_nhatmau                int := 0;
    v_hakka                  int := 0;
    v_other                  int := 0;
BEGIN
    v_dateandshift := EXTRACT(DAY FROM p_datesend)::text || '-' ||
                      EXTRACT(MONTH FROM p_datesend)::text || '-' ||
                      EXTRACT(YEAR FROM p_datesend)::text || '-' || p_shiftsend || N'LINE A';

    SELECT count(*) INTO v_dandoritrouble1 FROM "tb_listketquakiemtra"
     WHERE hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)' AND tinhtrang = 'NG'
       AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(*) INTO v_dandori1 FROM "tb_listketquakiemtra"
     WHERE hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)'
       AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;

    -- BURR dùng "Date_burr" (khác proc6 dùng last_update)
    SELECT count(DISTINCT "Part_no") INTO v_2ndprocess1 FROM "tb_Burr"
     WHERE "Date_burr" BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(DISTINCT masp) INTO v_changing2ndprocess1 FROM "tb_Burr_Changing"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(*) INTO v_oplinea FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='A';
    SELECT count(*) INTO v_oplineb FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='B';
    SELECT count(*) INTO v_oplinec FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='C';
    SELECT count(*) INTO v_oplined FROM "tb_ChangingPointOPOnMaChine"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(may,1)='D';

    SELECT count(*) INTO v_totalcheck1 FROM "tb_listketquakiemtra"
     WHERE ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(*) INTO v_totalng1 FROM "tb_listketquakiemtra"
     WHERE ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc
       AND (ketquatonghop=N'NG' OR tinhtrang=N'NG' OR tinhtrang=N'TẠM THỜI');
    SELECT count(*) INTO v_checktroubletime1 FROM "tb_listketquakiemtra"
     WHERE hinhthuc=N'SỰ CỐ' AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(*) INTO v_checktroubletime1stop FROM "tb_listketquakiemtra"
     WHERE hinhthuc=N'SỰ CỐ' AND tinhtrang='NG'
       AND ngayktra BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(*) INTO v_asssyreturntotalpart1 FROM "tb_Assy_Return"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT count(DISTINCT masp) INTO v_asssyreturntotaltimes1 FROM "tb_Assy_Return"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum("soNGkhachhang"),0) INTO v_asssyreturntotalpcs1 FROM "tb_Assy_Return"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;

    SELECT count(*) INTO v_totaldarw FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum(soluonghangtach),0) INTO v_totalhangtach FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum(soluongdarw),0) INTO v_totalardrwrc FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;
    SELECT coalesce(sum(soluongng),0) INTO v_totalngpartcantrepaire FROM "tb_ReworkRecheck"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc;

    -- SOSPNG lọc LINE A
    SELECT coalesce(sum("NGsilver"),0), coalesce(sum("NGchamden"),0), coalesce(sum("NGdinhdau"),0),
           coalesce(sum("NGloangnhua"),0), coalesce(sum("NGautohand"),0), coalesce(sum("NGshortmold"),0),
           coalesce(sum("NGflowmask"),0), coalesce(sum("NGchaykhi"),0), coalesce(sum("NGsinkmask"),0),
           coalesce(sum("Nhatmau"),0), coalesce(sum(hakka),0),
           coalesce(sum("NGtapchat")+sum("NGketsp")+sum("Xuoc")+sum(jig)+sum("Other"),0),
           coalesce(sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
                    sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
                    sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"),0)
      INTO v_NGsilver, v_NGtapchat, v_NGdinhdau, v_NGloangnhua, v_NGautohand,
           v_NGshortmold, v_NGflowmask, v_NGchaykhi, v_NGsinkmask, v_nhatmau,
           v_hakka, v_other, v_totalngpartdate
      FROM "tb_sospNG"
     WHERE ngayupdate BETWEEN p_ngaybatdau AND p_ngayketthuc AND left(mayduc,1)='A';

    SELECT coalesce(sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
                    sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
                    sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"),0)
      INTO v_totalngpart
      FROM "tb_sospNG"
     WHERE EXTRACT(MONTH FROM ngayupdate)=EXTRACT(MONTH FROM p_ngaybatdau)
       AND EXTRACT(YEAR FROM ngayupdate)=EXTRACT(YEAR FROM p_ngaybatdau)
       AND ngayupdate <= p_ngayketthuc AND left(mayduc,1)='A';

    INSERT INTO "tb_EmailDailyReport_Line" (
        datesend, shiftsend, dandori1, dandoritrouble1, "2ndprocess1",
        changing2ndprocess1, oplinea, oplineb, oplinec, oplined,
        totalcheck1, totalng1, checktroubletime1, checktroubletime1muststop, "N_A",
        asssyreturntotalpart1, asssyreturntotaltimes1, totaldarw, totalhangtach,
        totalngpartcantrepaire, totalngpartdate, dateandshift, totalngpart,
        asssyreturntotalpcs1, totalardrwrc, "NGsilver", "NGtapchat", "NGdinhdau",
        "NGloangnhua", "NGautohand", "NGshortmold", "NGflowmask", "NGchaykhi", "NGsinkmask",
        "Nhatmau", "Other", hakka
    ) VALUES (
        p_datesend, p_shiftsend, v_dandori1, v_dandoritrouble1, v_2ndprocess1,
        v_changing2ndprocess1, v_oplinea, v_oplineb, v_oplinec, v_oplined,
        v_totalcheck1, v_totalng1, v_checktroubletime1, v_checktroubletime1stop, NULL,
        v_asssyreturntotalpart1, v_asssyreturntotaltimes1, v_totaldarw, v_totalhangtach,
        v_totalngpartcantrepaire, v_totalngpartdate, v_dateandshift, v_totalngpart,
        v_asssyreturntotalpcs1, v_totalardrwrc, v_NGsilver, v_NGtapchat, v_NGdinhdau,
        v_NGloangnhua, v_NGautohand, v_NGshortmold, v_NGflowmask, v_NGchaykhi,
        v_NGsinkmask, v_nhatmau, v_other, v_hakka
    );
END;
$function$
```

---

## 121. `SP_FOLLOWMAUFA`

```sql
CREATE OR REPLACE FUNCTION public."SP_FOLLOWMAUFA"(p_startdate timestamp without time zone, p_enddate timestamp without time zone, p_nguoiupdate character varying)
 RETURNS void
 LANGUAGE sql
AS $function$
INSERT INTO "tb_FollowMauFA" (masp, sokhuon, may, hinhthuc, tensp, ngayupdate, nguoiupdate)
SELECT masp, dieno, may, hinhthuc, tensp, CURRENT_TIMESTAMP, p_nguoiupdate
  FROM "tb_listketquakiemtra"
 WHERE id IN (
     SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY masp, dieno
 )
   AND hinhthuc = N'KHUÔN HẠ/ DỪNG MÁY (LS)'
   AND ngayktra BETWEEN p_startdate AND p_enddate;
$function$
```

---

## 122. `SP_FOLLOW_NG_RATE_BY_REALTIMEANDLOT`

```sql
CREATE OR REPLACE FUNCTION public."SP_FOLLOW_NG_RATE_BY_REALTIMEANDLOT"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_FollowByLot";

    INSERT INTO "tb_FollowByLot" (
        idkhoidong, may, masp, sokhuon, tensp, may_masp_khuon, giolaymaukhoidong
    )
    SELECT id, may, masp, dieno, tensp,
           may || '(' || masp || '-' || dieno || ')',
           ngayktra
      FROM "tb_listketquakiemtra"
     WHERE id IN (
         SELECT max(id) FROM "tb_listketquakiemtra"
          WHERE hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)'
          GROUP BY may
     )
     ORDER BY may;

    -- Cập nhật IDHAKHUON (lần hạ khuôn mới nhất)
    UPDATE "tb_FollowByLot" fb
       SET idhakhuon = t.id, giolaymauhakhuon = t.ngayktra
      FROM (
          SELECT id, may, masp, dieno, ngayktra,
                 may || '-' || masp || '-' || dieno AS may_masp_khuon
            FROM "tb_listketquakiemtra"
           WHERE id IN (
               SELECT max(id) FROM "tb_listketquakiemtra"
                WHERE hinhthuc = N'KHUÔN HẠ/ DỪNG MÁY (LS)'
                GROUP BY may
           )
      ) t
     WHERE t.id > fb.idkhoidong
       AND fb.may_masp_khuon = t.may || '(' || t.masp || '-' || t.dieno || ')';

    -- Cavity từ PART_MASTER
    UPDATE "tb_FollowByLot" fb
       SET Cavity = pm.Cavity
      FROM "tb_Part_master" pm
     WHERE fb.masp = pm."Part_no";

    -- CycleTime từ TB_SHOTTING
    UPDATE "tb_FollowByLot" fb
       SET cycletime = s."CycleTime"
      FROM "tb_Shotting" s
     WHERE s."Masp" = fb.masp AND s."SoKhuon" = fb."SoKhuon";

    UPDATE "tb_FollowByLot" SET cycletime = 30 WHERE cycletime IS NULL;
    UPDATE "tb_FollowByLot" SET giolaymauhakhuon = CURRENT_TIMESTAMP WHERE giolaymauhakhuon IS NULL;

    -- TIMEDIFF (giây) và TOTALSHOT
    UPDATE "tb_FollowByLot"
       SET timediff   = EXTRACT(EPOCH FROM giolaymauhakhuon - giolaymaukhoidong)::int,
           totalshot  = EXTRACT(EPOCH FROM giolaymauhakhuon - giolaymaukhoidong)::int / cycletime
     WHERE cycletime > 0 AND cycletime IS NOT NULL;

    -- NG counts từ TB_SOSPNG
    UPDATE "tb_FollowByLot" fb
       SET silver=t.NGsilver, chamden=t.NGchamden, tapchat=t.NGtapchat,
           dinhdau=t.NGdinhdau, loangnhua=t.NGloangnhua, autohand=t.NGautohand,
           shortmold=t.NGshortmold, flowmask=t.NGflowmask, chaykhi=t.NGchaykhi,
           sinkmask=t.NGsinkmask, ketsp=t.NGketsp, nhatmau=t.nhatmau,
           xuoc=t.xuoc, hakka=t.hakka, jig=t.jig, other=t.other, totalng=t."totalNG"
      FROM (
          SELECT sum(NGsilver) AS NGsilver, sum(NGchamden) AS NGchamden,
                 sum(NGtapchat) AS NGtapchat, sum(NGdinhdau) AS NGdinhdau,
                 sum(NGloangnhua) AS NGloangnhua, sum(NGautohand) AS NGautohand,
                 sum(NGshortmold) AS NGshortmold, sum(NGflowmask) AS NGflowmask,
                 sum(NGchaykhi) AS NGchaykhi, sum(NGsinkmask) AS NGsinkmask,
                 sum(NGketsp) AS NGketsp, sum(a.nhatmau) AS nhatmau,
                 sum(a.xuoc) AS xuoc, sum(a.hakka) AS hakka,
                 sum(a.jig) AS jig, sum(a.other) AS other,
                 (sum(NGsilver)+sum(NGchamden)+sum(NGtapchat)+sum(NGdinhdau)+sum(NGloangnhua)+
                  sum(NGautohand)+sum(NGshortmold)+sum(NGflowmask)+sum(NGchaykhi)+sum(NGsinkmask)+
                  sum(NGketsp)+sum(a.nhatmau)+sum(a.xuoc)+sum(a.hakka)+sum(a.jig)+sum(a.other)) AS totalng,
                 a.mayduc, a."Masp", a."SoKhuon"
            FROM "tb_sospNG" a
            JOIN "tb_FollowByLot" b ON a."Masp"=b."Masp" AND a."SoKhuon"=b."SoKhuon" AND a.mayduc=b.may
           WHERE a.idlistkqktracopy >= b.idkhoidong
           GROUP BY a.mayduc, a."Masp", a."SoKhuon"
      ) t
     WHERE t.masp=fb.masp AND t.sokhuon=fb."SoKhuon" AND t.mayduc=fb.may;

    -- NG rate % = loi / (totalshot * Cavity) * 100
    UPDATE "tb_FollowByLot"
       SET silver    = round(silver::float    / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           chamden   = round(chamden::float   / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           tapchat   = round(tapchat::float   / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           dinhdau   = round(dinhdau::float   / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           loangnhua = round(loangnhua::float / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           autohand  = round(autohand::float  / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           shortmold = round(shortmold::float / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           flowmask  = round(flowmask::float  / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           chaykhi   = round(chaykhi::float   / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           sinkmask  = round(sinkmask::float  / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           ketsp     = round(ketsp::float     / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           nhatmau   = round(nhatmau::float   / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           xuoc      = round(xuoc::float      / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           hakka     = round(hakka::float     / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           jig       = round(jig::float       / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2),
           other     = round(other::float     / NULLIF(totalshot,0)::float / NULLIF(Cavity,0) * 100, 2)
     WHERE totalshot IS NOT NULL AND totalshot <> 0;

    UPDATE "tb_FollowByLot"
       SET ngrate = silver+chamden+tapchat+dinhdau+loangnhua+autohand+shortmold+
                   flowmask+chaykhi+sinkmask+ketsp+nhatmau+xuoc+hakka+jig+other;
    UPDATE "tb_FollowByLot" SET ngrate = round((ngrate)::numeric, 2);
END;
$function$
```

---

## 123. `SP_FOLLOW_TIME_MEASURANCE_BY_NAM`

```sql
CREATE OR REPLACE FUNCTION public."SP_FOLLOW_TIME_MEASURANCE_BY_NAM"(p_nam integer, p_ispic boolean)
 RETURNS TABLE(nguoiupdate character varying, thoigiando numeric, thoigiantieuchuan numeric, thoigiantieuchuantheoitem numeric)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_ispic = 0 THEN
        RETURN QUERY
        SELECT N'THỐNG KÊ'::varchar,
               sum(thoigiando), sum(thoigiantieuchuan), sum(thoigiantieuchuantheoitem)
          FROM "tb_FollowOPDo"
         WHERE EXTRACT(YEAR FROM ngayupdate) = p_nam AND dungcudo <> 'MC';
    ELSE
        RETURN QUERY
        SELECT nguoiupdate,
               sum(thoigiando), sum(thoigiantieuchuan), sum(thoigiantieuchuantheoitem)
          FROM "tb_FollowOPDo"
         WHERE EXTRACT(YEAR FROM ngayupdate) = p_nam AND dungcudo <> 'MC'
         GROUP BY nguoiupdate;
    END IF;
END;
$function$
```

---

## 124. `SP_FOLLOW_TIME_MEASURANCE_BY_SHIFT`

```sql
CREATE OR REPLACE FUNCTION public."SP_FOLLOW_TIME_MEASURANCE_BY_SHIFT"(p_startdate timestamp without time zone, p_enddate timestamp without time zone, p_ispic boolean)
 RETURNS TABLE(nguoiupdate character varying, thoigiando numeric, thoigiantieuchuan numeric, thoigiantieuchuantheoitem numeric)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_ispic = 0 THEN
        RETURN QUERY
        SELECT N'THỐNG KÊ'::varchar,
               sum(thoigiando), sum(thoigiantieuchuan), sum(thoigiantieuchuantheoitem)
          FROM "tb_FollowOPDo"
         WHERE ngayupdate BETWEEN p_startdate AND p_enddate AND dungcudo <> 'MC';
    ELSE
        RETURN QUERY
        SELECT nguoiupdate,
               sum(thoigiando), sum(thoigiantieuchuan), sum(thoigiantieuchuantheoitem)
          FROM "tb_FollowOPDo"
         WHERE ngayupdate BETWEEN p_startdate AND p_enddate AND dungcudo <> 'MC'
         GROUP BY nguoiupdate;
    END IF;
END;
$function$
```

---

## 125. `SP_FOLLOW_TIME_MEASURANCE_BY_THANG`

```sql
CREATE OR REPLACE FUNCTION public."SP_FOLLOW_TIME_MEASURANCE_BY_THANG"(p_thang integer, p_nam integer, p_ispic boolean)
 RETURNS TABLE(nguoiupdate character varying, thoigiando numeric, thoigiantieuchuan numeric, thoigiantieuchuantheoitem numeric)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_ispic = 0 THEN
        RETURN QUERY
        SELECT N'THỐNG KÊ'::varchar,
               sum(thoigiando), sum(thoigiantieuchuan), sum(thoigiantieuchuantheoitem)
          FROM "tb_FollowOPDo"
         WHERE EXTRACT(MONTH FROM ngayupdate) = p_thang
           AND EXTRACT(YEAR FROM ngayupdate) = p_nam
           AND dungcudo <> 'MC';
    ELSE
        RETURN QUERY
        SELECT nguoiupdate,
               sum(thoigiando), sum(thoigiantieuchuan), sum(thoigiantieuchuantheoitem)
          FROM "tb_FollowOPDo"
         WHERE EXTRACT(MONTH FROM ngayupdate) = p_thang
           AND EXTRACT(YEAR FROM ngayupdate) = p_nam
           AND dungcudo <> 'MC'
         GROUP BY nguoiupdate;
    END IF;
END;
$function$
```

---

## 126. `SP_GET_CHECK_SHEET`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_CHECK_SHEET"(p_masp character varying)
 RETURNS SETOF type_check_sheet
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        "Id" AS id, "Part_no" AS part_no, "Soquanly" AS soquanly, "Soversion" AS soversion, "Lydo_update" AS lydo_update, "Ghichu" AS ghichu, "Attfile_scan" AS attfile_scan, "Attfile_E" AS attfile_e, "Ngay_update" AS ngay_update, "Nguoi_update" AS nguoi_update,
        "isHistory" AS ishistory, tensp AS tensp, approved AS approved, ngayissue AS ngayissue, nguoicheckcode AS nguoicheckcode, nguoipheduyetcode AS nguoipheduyetcode, nguoilapcode AS nguoilapcode
    FROM "Check_Sheet"
    WHERE "Part_no" = p_masp;
END;
$function$
```

---

## 127. `SP_GET_CHI_TIET_KET_QUA`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_CHI_TIET_KET_QUA"(p_idlistlichsubaoduong integer)
 RETURNS SETOF record
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY EXECUTE
        'SELECT * FROM "TB_CHITIETBAODUONG" WHERE idlistbaoduong = $1'
        USING p_idlistlichsubaoduong;
END;
$function$
```

---

## 128. `SP_GET_CHI_TIET_KIEM_TRA`

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

## 129. `SP_GET_CHI_TIET_KIEM_TRA_NEW`

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

## 130. `SP_GET_COMPUTER`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_COMPUTER"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_SoftWareInfo_View";
    INSERT INTO "tb_SoftWareInfo_View" (
        computer, "userLogin", "versionNumber", "lastLogin", "userComputer", "userNameComputer", "isOnline"
    )
    SELECT computer, "userLogin", "versionNumber", "lastLogin", "userComputer", "userNameComputer", "isOnline"
      FROM "tb_SoftWareInfo"
     WHERE id IN (
         SELECT max(id) FROM "tb_SoftWareInfo" GROUP BY computer, "userLogin"
     );
    DELETE FROM "tb_SoftWareInfo_View" WHERE "isOnline" = false;
END;
$function$
```

---

## 131. `SP_GET_CPK_KICHTHUOC`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_CPK_KICHTHUOC"(p_masp character varying, p_sokhuon character varying, p_item integer, p_vitri character varying, p_cavity integer, p_hinhthuc character varying, p_solan integer, p_ketquado double precision)
 RETURNS double precision
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_avg   float;
    v_sigma float;
    v_min   float;
    v_max   float;
    v_cpk1  float;
    v_cpk2  float;
    v_cpk   float;
BEGIN
    CREATE TEMP TABLE bangthongtindo(
        masp varchar(50), sokhuon varchar(50), item int,
        vitri varchar(50), Cavity int, hinhthuc varchar(100), ketquado float
    ) ON COMMIT DROP;

    -- Thêm bản ghi hiện tại
    INSERT INTO bangthongtindo VALUES(p_masp, p_sokhuon, p_item, p_vitri, p_cavity, p_hinhthuc, p_ketquado);

    -- Thêm lịch sử (TOP @solan bản ghi gần nhất)
    EXECUTE format(
        'INSERT INTO bangthongtindo
         SELECT masp, sokhuon, items, vitri, Cavity, hinhthuc, ketquado
           FROM "tb_chitietkqdo"
          WHERE masp=%L AND sokhuon=%L AND items=%s AND vitri=%L
            AND Cavity=%s AND hinhthuc=%L
          ORDER BY id DESC LIMIT %s',
        p_masp, p_sokhuon, p_item, p_vitri, p_cavity, p_hinhthuc, p_solan
    );

    SELECT avg(ketquado), stddev_pop(ketquado) INTO v_avg, v_sigma FROM bangthongtindo;

    SELECT gioihanduoi INTO v_min FROM "tb_chitietkqdo"
     WHERE masp=p_masp AND sokhuon=p_sokhuon AND items=p_item
       AND vitri=p_vitri AND Cavity=p_cavity AND hinhthuc=p_hinhthuc LIMIT 1;
    SELECT gioihantren INTO v_max FROM "tb_chitietkqdo"
     WHERE masp=p_masp AND sokhuon=p_sokhuon AND items=p_item
       AND vitri=p_vitri AND Cavity=p_cavity AND hinhthuc=p_hinhthuc LIMIT 1;

    v_cpk1 := (v_max - v_avg) / NULLIF(3 * v_sigma, 0);
    v_cpk2 := (v_avg - v_min) / NULLIF(3 * v_sigma, 0);
    v_cpk  := LEAST(v_cpk1, v_cpk2);

    RETURN v_cpk;
END;
$function$
```

---

## 132. `SP_GET_CYCTIMEFROMTVP_SHOTTING`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_CYCTIMEFROMTVP_SHOTTING"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- NOTE: [FA-TVP] cần được cấu hình qua postgres_fdw hoặc đổi tên bảng
    WITH added_row_number AS (
        SELECT "Part_no", Die_no, ngay_gui, cycle_time,
               ROW_NUMBER() OVER(PARTITION BY "Part_no", Die_no ORDER BY ngay_gui DESC) AS rn
          FROM "FA-TVP"
         WHERE ketqua = 'OK'
    )
    UPDATE "tb_Shotting" s
       SET cycletime = a.cycle_time
      FROM added_row_number a
     WHERE rn = 1 AND a.part_no = s."Masp" AND a.Die_no = s."SoKhuon";

    UPDATE "tb_Shotting" SET cycletime = 30 WHERE cycletime = 0;
END;
$function$
```

---

## 133. `SP_GET_DANH_SACH_KIEM_TRA`

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

## 134. `SP_GET_DATASHEET_INFO`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_DATASHEET_INFO"(p_masp character varying, p_sokhuon character varying)
 RETURNS SETOF type_datasheet_info
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT 
        "Id" AS id, "Part_no" AS part_no, "Soquanly" AS soquanly, "Soversion" AS soversion, "Lydo_update" AS lydo_update, "Ghichu" AS ghichu, "Attfile_scan" AS attfile_scan, "Attfile_E" AS attfile_e, "Attfile" AS attfile, "Ngay_update" AS ngay_update,
        "Nguoi_update" AS nguoi_update, "isHistory" AS ishistory, "tensp" AS tensp, "aprroved" AS approved, "nguoilapcode" AS nguoilapcode, "nguoicheckcode" AS nguoicheckcode, "nguoipheduyetcode" AS nguoipheduyetcode,
        "ngayissue" AS ngayissue, "ngaycheck" AS ngaycheck, "ngaypheduyet" AS ngaypheduyet
    FROM "Data_Sheet"
    WHERE "Part_no" = p_masp AND "Ghichu" = p_sokhuon;
END;
$function$
```

---

## 135. `SP_GET_DEMENSION_DATA_FOR_DATASHEET`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_DEMENSION_DATA_FOR_DATASHEET"(p_masp character varying, p_sokhuon character varying, p_cavity integer, p_hinhthuc character varying)
 RETURNS SETOF type_demension_data_for_datasheet
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_groupname varchar(50);
BEGIN
    SELECT grouppart INTO v_groupname FROM "tb_Part_master"
     WHERE "Part_no" = p_masp AND "Die_no" = p_sokhuon;

    IF p_hinhthuc = N'FA' THEN
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

## 136. `SP_GET_DEMENSION_DATA_FULL`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_DEMENSION_DATA_FULL"(p_idlist integer, p_masp character varying, p_sokhuon character varying, p_cavity integer)
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

## 137. `SP_GET_ECN_STATUS_INFO`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_ECN_STATUS_INFO"(p_masp character varying, p_sokhuon character varying)
 RETURNS SETOF type_ecn_status_info
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY 
    SELECT idstatus, ECNSTATUS_DATA.masp, ECNSTATUS_DATA.sokhuon, sobanveparentstatus, ngayupdatestatus, comment, nguoiupdatestatus, sobanveparentdrw
    FROM "tb_ECNStatus" ECNSTATUS_DATA
    WHERE ECNSTATUS_DATA.masp = p_masp AND ECNSTATUS_DATA.sokhuon = p_sokhuon;
END;
$function$
```

---

## 138. `SP_GET_FA_DATA`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_FA_DATA"(p_iddo text)
 RETURNS TABLE(items integer, vitri character varying, dungcudo character varying, cavity integer, stt text)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY EXECUTE
        'SELECT DISTINCT items, vitri, dungcudo, Cavity, ''1'' AS stt
           FROM "tb_chitietkqdo"
          WHERE idlistketquado IN (' || p_iddo || ')
          ORDER BY items, stt';
END;
$function$
```

---

## 139. `SP_GET_GIAO_CA_KICH_THUOC`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_GIAO_CA_KICH_THUOC"(p_starttime timestamp without time zone, p_endtime timestamp without time zone)
 RETURNS TABLE(idlistketquado integer, mayduc character varying, masp character varying, sokhuon character varying, cavity integer, tensp character varying, hinhthuc character varying, items integer, vitri character varying, ngayktra timestamp without time zone, nguoiktra character varying, ng character varying, ketquado double precision, gioihan double precision, dungcudo character varying)
 LANGUAGE sql
AS $function$
-- OVER MAX FA
SELECT idlistketquado, mayduc, masp, sokhuon, Cavity, tensp, hinhthuc, items, vitri,
       ngayktra, nguoiktra, N'OVER MAX FA', ketquado, gioihantrenfa, dungcudo
  FROM "tb_chitietkqdo"
 WHERE ketquado > gioihantrenfa AND gioihantrenmp = 1000 AND gioihantrenfa <> 1000
   AND ngayktra BETWEEN p_starttime AND p_endtime
UNION
-- UNDER MIN FA
SELECT idlistketquado, mayduc, masp, sokhuon, Cavity, tensp, hinhthuc, items, vitri,
       ngayktra, nguoiktra, N'UNDER MIN FA', ketquado, gioihanduoifa, dungcudo
  FROM "tb_chitietkqdo"
 WHERE ketquado < gioihanduoifa AND gioihanduoimp = 1000 AND gioihanduoifa <> 1000
   AND ngayktra BETWEEN p_starttime AND p_endtime
UNION
-- OVER MAX MP
SELECT idlistketquado, mayduc, masp, sokhuon, Cavity, tensp, hinhthuc, items, vitri,
       ngayktra, nguoiktra, N'OVER MAX MP', ketquado, gioihantrenmp, dungcudo
  FROM "tb_chitietkqdo"
 WHERE ketquado > gioihantrenmp AND gioihantrenmp <> 1000
   AND ngayktra BETWEEN p_starttime AND p_endtime
UNION
-- UNDER MIN MP
SELECT idlistketquado, mayduc, masp, sokhuon, Cavity, tensp, hinhthuc, items, vitri,
       ngayktra, nguoiktra, N'UNDER MIN MP', ketquado, gioihanduoimp, dungcudo
  FROM "tb_chitietkqdo"
 WHERE ketquado < gioihanduoimp AND gioihanduoimp <> 1000
   AND ngayktra BETWEEN p_starttime AND p_endtime
UNION
-- OVER MAX DRW (chỉ khi FA=1000 và MP=1000)
SELECT idlistketquado, mayduc, masp, sokhuon, Cavity, tensp, hinhthuc, items, vitri,
       ngayktra, nguoiktra, N'OVER MAX DRW', ketquado, gioihantrendrw, dungcudo
  FROM "tb_chitietkqdo"
 WHERE ketquado > round((gioihantrendrw::numeric)::numeric, 3) AND gioihantrenmp = 1000 AND gioihantrenfa = 1000
   AND ngayktra BETWEEN p_starttime AND p_endtime
UNION
-- UNDER MIN DRW (chỉ khi FA=1000 và MP=1000)
SELECT idlistketquado, mayduc, masp, sokhuon, Cavity, tensp, hinhthuc, items, vitri,
       ngayktra, nguoiktra, N'UNDER MIN DRW', ketquado, gioihanduoidrw, dungcudo
  FROM "tb_chitietkqdo"
 WHERE ketquado < round((gioihanduoidrw::numeric)::numeric, 3) AND gioihanduoimp = 1000 AND gioihanduoifa = 1000
   AND ngayktra BETWEEN p_starttime AND p_endtime;
$function$
```

---

## 140. `SP_GET_INFO_ECN_DRW`

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
        sokhuon, history, ngayguiapply, ngaycapecn, dongweb, comment, ketquafa, ngaysuakhuon, ngaycapnhatgiayto,
        ecnlevel, situation
    FROM "tb_ECNDRW" ECN_DRW
    WHERE ECN_DRW.masp = p_masp;
END;
$function$
```

---

## 141. `SP_GET_ISSUE_DO`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_ISSUE_DO"(p_may character varying, p_top integer)
 RETURNS TABLE(id integer, may character varying, masp character varying, khuon character varying, hinhthuc character varying, giolaymau timestamp without time zone, giokiemtra timestamp without time zone, nguoikiemtra character varying, idkiemtra integer, isissue boolean, comment character varying, iscancel boolean, lydo character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_may = 'ALL' THEN
        RETURN QUERY
        SELECT d.id, d.may, d.masp, d.khuon, d.hinhthuc,
               d.giolaymau, d.giokiemtra, d.nguoikiemtra,
               d.idkiemtra, d."isIssue", d.comment, d."isCancel", d.lydo
          FROM "tb_IssueDo" d
         WHERE d.idkiemtra IN (
               SELECT max(sub.idkiemtra) FROM "tb_IssueDo" sub GROUP BY sub.may)
         ORDER BY d.giolaymau DESC
         LIMIT p_top;
    ELSE
        RETURN QUERY
        SELECT d.id, d.may, d.masp, d.khuon, d.hinhthuc,
               d.giolaymau, d.giokiemtra, d.nguoikiemtra,
               d.idkiemtra, d."isIssue", d.comment, d."isCancel", d.lydo
          FROM "tb_IssueDo" d
         WHERE d.may = p_may
           AND d.idkiemtra IN (
               SELECT max(sub.idkiemtra) FROM "tb_IssueDo" sub GROUP BY sub.may)
         ORDER BY d.giolaymau DESC
         LIMIT p_top;
    END IF;
END;
$function$
```

---

## 142. `SP_GET_MATERIAL_INFO_TO_RECORD_THROW_AWAY_SYS`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_MATERIAL_INFO_TO_RECORD_THROW_AWAY_SYS"(p_may character varying, p_code integer)
 RETURNS TABLE(masp character varying, sokhuon character varying, giolaymau timestamp without time zone, nhasx character varying, maunhua character varying, mamau character varying, compare boolean)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_hinhthuc varchar(50);
    v_giolaymau timestamp;
    v_masp      varchar(50);
    v_sokhuon   varchar(50);
    v_nhasx     varchar(50);
    v_maunhua   varchar(100);
    v_mamau     varchar(50);
    v_compare   boolean;
BEGIN
    SELECT hinhthuc, giolaymau, "tb_listketquakiemtra".masp, dieno
      INTO v_hinhthuc, v_giolaymau, v_masp, v_sokhuon
      FROM "tb_listketquakiemtra"
     WHERE may = p_may
     ORDER BY id DESC LIMIT 1;

    SELECT nhacungcap, maunhua, mamau
      INTO v_nhasx, v_maunhua, v_mamau
      FROM "Material"
     WHERE code = p_code AND "Part_no" = v_masp;

    IF v_nhasx IS NULL THEN
        v_compare := 0;
        SELECT nhacungcap, maunhua, mamau
          INTO v_nhasx, v_maunhua, v_mamau
          FROM "Material"
         WHERE "Part_no" = v_masp;
    ELSE
        v_compare := 1;
    END IF;

    RETURN QUERY
    SELECT v_masp, v_sokhuon, v_giolaymau, v_nhasx, v_maunhua, v_mamau, v_compare;
END;
$function$
```

---

## 143. `SP_GET_NUMBEROFTOOL`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_NUMBEROFTOOL"(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(count bigint)
 LANGUAGE sql
AS $function$
SELECT count(DISTINCT a.dungcudo)
  FROM "tb_HangMucDoNewVer" a
  JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
 WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
   AND a.dungcudo <> 'SHAFT' AND a.faonly = false
 GROUP BY b.masp, b.sokhuon;
$function$
```

---

## 144. `SP_GET_NUMBEROFTOOL_FA`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_NUMBEROFTOOL_FA"(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(count bigint)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF EXISTS (
        SELECT 1 FROM "tb_HangMucDoNewVer" a
        JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
        WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon AND a.dungcudo <> 'SHAFT'
        GROUP BY b.masp, b.sokhuon
    ) THEN
        RETURN QUERY
        SELECT count(DISTINCT a.dungcudo)
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon AND a.dungcudo <> 'SHAFT'
         GROUP BY b.masp, b.sokhuon;
    END IF;
END;
$function$
```

---

## 145. `SP_GET_NUMBEROFTOOL_SUCO`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_NUMBEROFTOOL_SUCO"(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(count bigint)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF EXISTS (
        SELECT 1 FROM "tb_HangMucDoNewVer" a
        JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
        WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
          AND a.dungcudo <> 'SHAFT' AND a.donhanh = true
        GROUP BY b.masp, b.sokhuon
    ) THEN
        RETURN QUERY
        SELECT count(DISTINCT a.dungcudo)
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
           AND a.dungcudo <> 'SHAFT' AND a.donhanh = true
         GROUP BY b.masp, b.sokhuon;
    ELSE
        RETURN QUERY
        SELECT count(DISTINCT a.dungcudo)
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
           AND a.dungcudo <> 'SHAFT' AND a.faonly = false
         GROUP BY b.masp, b.sokhuon;
    END IF;
END;
$function$
```

---

## 146. `SP_GET_PART_INFO`

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
        "Part_img2" AS part_img2, "LastUpdate" AS lastupdate, "Pic" AS pic, grouppart, priority, "Part_number" AS part_number, "TaiSD_DRW" AS taisd_drw, "TaiSD_ACTUAL" AS taisd_actual, 
        "CutGateInfo" AS cutgateinfo, "CommentCutGate" AS commentcutgate, "Sample_barcode" AS sample_barcode, 
        "Sample_location" AS sample_location, "InsideCutGate" AS insidecutgate, "CMTInsideCutGate" AS cmtinsidecutgate, 
        "CongKim" AS congkim, "CMTCongKim" AS cmtcongkim, "PhunTrucTiep" AS phuntructiep, 
        "CMTPhunTrucTiep" AS cmtphuntructiep, "Tu_dkd" AS tu_dkd, "VerNew" AS vernew, 
        nguoiupdateeng, ngayupdateeng
    FROM "tb_Part_master"
    WHERE "Part_no" = p_masp;
END;
$function$
```

---

## 147. `SP_GET_PART_MATERIAL_INFO`

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

## 148. `SP_GET_PIC_TOOL`

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

## 149. `SP_GET_TOOL`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_TOOL"(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(dungcudo character varying)
 LANGUAGE sql
AS $function$
SELECT DISTINCT a.dungcudo
  FROM "tb_HangMucDoNewVer" a
  JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
 WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
   AND a.dungcudo NOT IN (N'SHAFT', N'ME')
   AND a.faonly = false;
$function$
```

---

## 150. `SP_GET_TOOL_FA`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_TOOL_FA"(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(dungcudo character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF EXISTS (
        SELECT 1 FROM "tb_HangMucDoNewVer" a
        JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
        WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
    ) THEN
        RETURN QUERY
        SELECT DISTINCT a.dungcudo
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
           AND a.dungcudo NOT IN (N'SHAFT', N'ME');
    END IF;
END;
$function$
```

---

## 151. `SP_GET_TOOL_MEASURE`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_TOOL_MEASURE"(p_masp character varying, p_sokhuon character varying, p_hinhthuc character varying)
 RETURNS TABLE(dungcudo character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_hinhthuc IN (N'SỰ CỐ', N'BẢO DƯỠNG', N'TRIAL FA',
                      N'CHECK NHANH KHỞI ĐỘNG', N'CHẠY LẠI (SAU SỰ CỐ - DỪNG MÁY)') THEN
        IF EXISTS (
            SELECT 1 FROM "tb_HangMucDoNewVer" a
            JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
            WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon AND a.donhanh = true
        ) THEN
            RETURN QUERY
            SELECT DISTINCT a.dungcudo
              FROM "tb_HangMucDoNewVer" a
              JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
             WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
               AND a.donhanh = true AND a.dungcudo NOT IN (N'SHAFT');
        ELSE
            RETURN QUERY
            SELECT DISTINCT a.dungcudo
              FROM "tb_HangMucDoNewVer" a
              JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
             WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
               AND a.faonly = false AND a.dungcudo NOT IN (N'SHAFT');
        END IF;
    ELSIF p_hinhthuc = N'FA' THEN
        RETURN QUERY
        SELECT DISTINCT a.dungcudo
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
           AND a.dungcudo NOT IN (N'SHAFT');
    ELSE
        RETURN QUERY
        SELECT DISTINCT a.dungcudo
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
           AND a.dungcudo NOT IN (N'SHAFT') AND a.faonly = false;
    END IF;
END;
$function$
```

---

## 152. `SP_GET_TOOL_SUCO`

```sql
CREATE OR REPLACE FUNCTION public."SP_GET_TOOL_SUCO"(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(dungcudo character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF EXISTS (
        SELECT 1 FROM "tb_HangMucDoNewVer" a
        JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
        WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon AND a.donhanh = true
    ) THEN
        RETURN QUERY
        SELECT DISTINCT a.dungcudo
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
           AND a.donhanh = true AND a.dungcudo NOT IN (N'SHAFT', N'ME');
    ELSE
        RETURN QUERY
        SELECT DISTINCT a.dungcudo
          FROM "tb_HangMucDoNewVer" a
          JOIN "tb_HangMucDoChiTietNewVer" b ON a.groupname = b.groupname
         WHERE b.masp = p_masp AND b.sokhuon = p_sokhuon
           AND a.faonly = false AND a.dungcudo NOT IN (N'SHAFT', N'ME');
    END IF;
END;
$function$
```

---

## 153. `SP_GIOIHANSUAKHUON`

```sql
CREATE OR REPLACE FUNCTION public."SP_GIOIHANSUAKHUON"()
 RETURNS TABLE(ngaysua date, solan bigint)
 LANGUAGE sql
AS $function$
SELECT ngaysua, count(ngaysua) AS solan FROM (
    SELECT DISTINCT tenkhuon AS grouppart, sokhuon, ngaysua
      FROM "DTS_FollowBurr"
     WHERE tenkhuon IS NOT NULL AND ngaysua IS NOT NULL
) t
GROUP BY ngaysua
ORDER BY ngaysua DESC;
$function$
```

---

## 154. `SP_GetItemGiaoCaKichThuoc`

```sql
CREATE OR REPLACE FUNCTION public."SP_GetItemGiaoCaKichThuoc"(p_starttime timestamp without time zone, p_endtime timestamp without time zone)
 RETURNS TABLE(masp character varying, tensp character varying, sokhuon character varying, items integer, vitri character varying, dungcudo character varying, ketquadanhgia character varying, cavity integer, gioihanduoifa double precision, gioihantrenfa double precision)
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- NULL-hóa giới hạn FA nếu NULL
    UPDATE "tb_HangMucDoNewVer" SET gioihanduoifa = 1000 WHERE gioihanduoifa IS NULL;
    UPDATE "tb_HangMucDoNewVer" SET gioihantrenfa  = 1000 WHERE gioihantrenfa  IS NULL;

    -- Cập nhật GROUPNAME theo PART_MASTER trong khoảng ngày
    UPDATE "tb_chitietkqdo" c
       SET groupname = p."GroupPart"
      FROM "tb_Part_master" p
     WHERE c."Masp" = p.part_no AND c."SoKhuon" = p.Die_no
       AND c.ngayktra BETWEEN p_starttime AND p_endtime;

    -- Trả về items NG
    RETURN QUERY
    SELECT DISTINCT c."Masp", c.tensp, c."SoKhuon", c.items, c.vitri, c.dungcudo,
           c.ketquadanhgia, c.Cavity, h.gioihanduoifa, h.gioihantrenfa
      FROM "tb_chitietkqdo" c
      JOIN "tb_HangMucDoNewVer" h
        ON c.groupname = h.groupname AND c.vitri = h.vitri AND c.items = h.items
     WHERE c.ketquadanhgia = 'NG'
       AND c.ketquadanhgiaMP <> 'OK'
       AND ((c.gioihanduoimp = 1000 AND c.gioihantrenmp = 1000
             AND c.gioihanduoifathamkhao = 1000 AND c.gioihantrenfathamkhao = 1000)
            OR c.ketquado > h.gioihantrenfa
            OR (c.ketquado < h.gioihanduoifa AND h.gioihanduoifa <> 1000)
            OR (h.gioihanduoifa = 1000 AND h.gioihantrenfa = 1000))
       AND c.ngayktra BETWEEN p_starttime AND p_endtime;
END;
$function$
```

---

## 155. `SP_INSERT_DEMENSION_DATA_SUPPLIER`

```sql
CREATE OR REPLACE FUNCTION public."SP_INSERT_DEMENSION_DATA_SUPPLIER"(p_idlist integer, p_masp character varying, p_sokhuon character varying, p_cavity integer)
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

## 156. `SP_INSERT_DEMENSION_DATA_SUPPLIER2`

```sql
CREATE OR REPLACE FUNCTION public."SP_INSERT_DEMENSION_DATA_SUPPLIER2"(p_idlist integer, p_masp character varying, p_sokhuon character varying, p_cavity integer)
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

## 157. `SP_INSERT_DIMENSION_DATA`

```sql
CREATE OR REPLACE FUNCTION public."SP_INSERT_DIMENSION_DATA"(p_idlist integer)
 RETURNS void
 LANGUAGE sql
AS $function$
DELETE FROM "tb_Issue_Dimention_Data_Temp";
INSERT INTO "tb_Issue_Dimention_Data_Temp"
SELECT id,nguoiktra,ngayktra,casx,mayduc,masp,tensp,sokhuon,hinhthuc,items,vitri,dungcudo,
       gioihantren,gioihanduoi,gioihantrendrw,gioihanduoidrw,gioihantrenfa,gioihanduoifa,
       gioihantrenmp,gioihanduoimp,hinhanh,ketquado,ketquadanhgia,comment,idlistketquado,
       Cavity,congthuc,shot1,shot2,shot3,shot4,shot5,stt,"ketquadanhgiaMP",deltamiddrw,
       xuhuong,canhbaotanggiam,canhbaosailech,ngtype,groupname,
       gioihantrenfathamkhao,gioihanduoifathamkhao,danhgiafathamkhao,loaikt,showpe
  FROM "tb_chitietkqdo"
 WHERE idlistketquado = p_idlist;
$function$
```

---

## 158. `SP_INSERT_ITEM_ANALYSIT`

```sql
CREATE OR REPLACE FUNCTION public."SP_INSERT_ITEM_ANALYSIT"(p_isrunning boolean)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_PartAnalysis";
    INSERT INTO "tb_PartAnalysis" (groupname, masp, sokhuon, Cavity, item, vitri, gioihanduoi, gioihantren)
    SELECT groupname, masp, sokhuon, Cavity, items, vitri, 1000, 1000
      FROM "tb_HangMucDoChiTietNewVer";

    UPDATE "tb_PartQuality" SET may = NULL;
    UPDATE "tb_PartQuality" pq
       SET may = t.may
      FROM (SELECT may, masp, dieno FROM "tb_listketquakiemtra"
             WHERE id IN (SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY may)) t
     WHERE pq.masp = t.masp AND pq.sokhuon = t.dieno;

    IF p_isrunning = 1 THEN
        DELETE FROM "tb_PartAnalysis"
         WHERE masp || sokhuon IN (
             SELECT masp || sokhuon FROM "tb_PartQuality" WHERE may IS NULL
         );
    END IF;

    -- Cập nhật HINHANH
    UPDATE "tb_PartAnalysis" pa
       SET hinhanh = h.hinhanh
      FROM "tb_HangMucDoNewVer" h
     WHERE h.groupname = pa.groupname AND h.items = pa.item AND h.vitri = pa.vitri;

    -- Giới hạn dưới: ưu tiên MP → FA → FINAL LOWER
    UPDATE "tb_PartAnalysis" pa SET gioihanduoi = h.gioihanduoimp
      FROM "tb_HangMucDoNewVer" h
     WHERE h.groupname=pa.groupname AND h.items=pa.item AND h.vitri=pa.vitri AND pa.gioihanduoi=1000;
    UPDATE "tb_PartAnalysis" pa SET gioihantren = h.gioihantrenmp
      FROM "tb_HangMucDoNewVer" h
     WHERE h.groupname=pa.groupname AND h.items=pa.item AND h.vitri=pa.vitri AND pa.gioihantren=1000;
    UPDATE "tb_PartAnalysis" pa SET gioihanduoi = h.gioihanduoifa
      FROM "tb_HangMucDoNewVer" h
     WHERE h.groupname=pa.groupname AND h.items=pa.item AND h.vitri=pa.vitri AND pa.gioihanduoi=1000;
    UPDATE "tb_PartAnalysis" pa SET gioihantren = h.gioihantrenfa
      FROM "tb_HangMucDoNewVer" h
     WHERE h.groupname=pa.groupname AND h.items=pa.item AND h.vitri=pa.vitri AND pa.gioihantren=1000;
    UPDATE "tb_PartAnalysis" pa SET gioihanduoi = h.finallower
      FROM "tb_HangMucDoChiTietNewVer" h
     WHERE h."Masp"=pa."Masp" AND h."SoKhuon"=pa."SoKhuon" AND h.Cavity=pa.Cavity
       AND h.items=pa.item AND h.vitri=pa.vitri AND pa.gioihanduoi=1000;
    UPDATE "tb_PartAnalysis" pa SET gioihantren = h.finalupper
      FROM "tb_HangMucDoChiTietNewVer" h
     WHERE h."Masp"=pa."Masp" AND h."SoKhuon"=pa."SoKhuon" AND h.Cavity=pa.Cavity
       AND h.items=pa.item AND h.vitri=pa.vitri AND pa.gioihantren=1000;
END;
$function$
```

---

## 159. `SP_JOB_PENDING`

```sql
CREATE OR REPLACE FUNCTION public."SP_JOB_PENDING"(p_ngay date, p_ca character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "PM_Job_Pending";
    IF p_ca = N'TẤT CẢ' THEN
        INSERT INTO "PM_Job_Pending"
        SELECT * FROM (
            SELECT N'SỰ CỐ'   AS jobtype, tensuco AS job, ketqua FROM "PM_SuCoTrongNgay"
             WHERE (ketqua='NG' OR ketqua='TEMP') AND tensuco IS NOT NULL AND ngay=p_ngay
            UNION
            SELECT N'CHECK MÁY', diemng, ketqua FROM "PM_CheckMay"
             WHERE (ketqua='NG' OR ketqua='TEMP') AND ngay=p_ngay
            UNION
            SELECT N'KẾ HOẠCH', tencongviec, ketqua FROM "PM_AsignJob"
             WHERE (ketqua='NG' OR ketqua='TEMP') AND ngay=p_ngay
        ) t;
    ELSE
        INSERT INTO "PM_Job_Pending"
        SELECT * FROM (
            SELECT N'SỰ CỐ', tensuco, ketqua FROM "PM_SuCoTrongNgay"
             WHERE (ketqua='NG' OR ketqua='TEMP') AND tensuco IS NOT NULL
               AND ngay=p_ngay AND gio=p_ca
            UNION
            SELECT N'CHECK MÁY', diemng, ketqua FROM "PM_CheckMay"
             WHERE (ketqua='NG' OR ketqua='TEMP') AND ngay=p_ngay AND ca=p_ca
            UNION
            SELECT N'KẾ HOẠCH', tencongviec, ketqua FROM "PM_AsignJob"
             WHERE (ketqua='NG' OR ketqua='TEMP') AND ngay=p_ngay AND ca=p_ca
        ) t;
    END IF;
END;
$function$
```

---

## 160. `SP_LichBaoDuongHomNay`

```sql
CREATE OR REPLACE FUNCTION public."SP_LichBaoDuongHomNay"()
 RETURNS TABLE(loaithietbi character varying, tenthietbi character varying, loaibaoduong character varying, tansuat integer, kehoach date, songayvuotqua integer)
 LANGUAGE plpgsql
AS $function$
BEGIN
    CREATE TEMP TABLE bangtamdemtrongbangchitiet(
        loaithietbi   varchar(50),
        tenthietbi    varchar(50),
        loaibaoduong  varchar(50),
        tansuat       int,
        kehoach       date,
        songayvuotqua int,
        demtrongchitiet int DEFAULT 0
    ) ON COMMIT DROP;

    INSERT INTO bangtamdemtrongbangchitiet(loaithietbi,tenthietbi,loaibaoduong,tansuat,kehoach,songayvuotqua,demtrongchitiet)
    SELECT loaithietbi, tenthietbi, loaibd, sothang,
           ngayplanconvert::date,
           (CURRENT_DATE - ngayplanconvert::date),
           0
      FROM "PM_YearlyPlanConvert"
     WHERE ngayplanconvert::date <= CURRENT_DATE
       AND (ngayactualconvert IS NULL OR ngayactualconvert::text = '')
       AND ngayplanconvert IS NOT NULL;

    UPDATE bangtamdemtrongbangchitiet t
       SET demtrongchitiet = a.kq
      FROM (
          SELECT loaithietbi, tenthietbi, loaibaoduong, ngayplan, count(ketqua) AS kq
            FROM "PM_ChiTietBaoDuong"
           GROUP BY loaithietbi, tenthietbi, loaibaoduong, ngayplan
      ) a
     WHERE a.loaithietbi = t.loaithietbi
       AND a.tenthietbi  = t.tenthietbi
       AND a.loaibaoduong = t.loaibaoduong
       AND t.kehoach = a.ngayplan;

    RETURN QUERY
    SELECT t.loaithietbi, t.tenthietbi, t.loaibaoduong, t.tansuat, t.kehoach, t.songayvuotqua
      FROM bangtamdemtrongbangchitiet t
     WHERE demtrongchitiet = 0;
END;
$function$
```

---

## 161. `SP_MAIL_RECEIVER_INSERT`

```sql
CREATE OR REPLACE FUNCTION public."SP_MAIL_RECEIVER_INSERT"(p_id integer, p_receiver character varying, p_isreaded boolean, p_priority integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "MAIL_RECEIVER" WHERE "Id" = p_id AND "Receiver" = p_receiver
    ) THEN
        INSERT INTO "MAIL_RECEIVER" ("Id", "Receiver", "isReaded", "Priority")
        VALUES (p_id, p_receiver, p_isreaded, p_priority);
    END IF;
END;
$function$
```

---

## 162. `SP_PLAN_DailyProduction_Insert`

```sql
CREATE OR REPLACE FUNCTION public."SP_PLAN_DailyProduction_Insert"(p_time character varying, p_machine character varying, p_msp character varying, p_ngay timestamp without time zone)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "PLAN_DailyProduction"
         WHERE "Time"=p_time AND "Machine"=p_machine AND ngay=p_ngay
    ) THEN
        INSERT INTO "PLAN_DailyProduction" ("Time", "Machine", "Masp", ngay)
        VALUES (p_time, p_machine, p_msp, p_ngay);
    ELSE
        UPDATE "PLAN_DailyProduction" SET "Masp"=p_msp
         WHERE "Time"=p_time AND "Machine"=p_machine AND ngay=p_ngay;
    END IF;
END;
$function$
```

---

## 163. `SP_PLAN_insertupdate_dandory`

```sql
CREATE OR REPLACE FUNCTION public."SP_PLAN_insertupdate_dandory"(p_time character varying, p_machine character varying, p_khuonha character varying, p_khuonlap character varying, p_ngay date, p_lydo character varying, p_comment character varying, p_gioplan character varying, p_gioactual character varying, p_ngaycn timestamp without time zone, p_nguoicn character varying, p_thaydoi integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_stt int;
BEGIN
    v_stt := CASE p_time
        WHEN '8~10'  THEN 1  WHEN '10~12' THEN 2  WHEN '12~14' THEN 3
        WHEN '14~16' THEN 4  WHEN '16~18' THEN 5  WHEN '18~20' THEN 6
        WHEN '20~22' THEN 7  WHEN '22~24' THEN 8  WHEN '0~2'   THEN 9
        WHEN '2~4'   THEN 10 WHEN '4~6'   THEN 11 WHEN '6~8'   THEN 12
        ELSE NULL END;

    IF NOT EXISTS (
        SELECT 1 FROM "PLAN_Dandory" WHERE khunggio=p_time AND may=p_machine AND ngay=p_ngay
    ) THEN
        INSERT INTO "PLAN_Dandory" (ngay,may,khuonha,khuonlap,khunggio,stt,lydo,comment,
                                    gioplan,gioactual,ngaycapnhat,nguoicapnhat,thaydoi)
        VALUES (p_ngay,p_machine,p_khuonha,p_khuonlap,p_time,v_stt,p_lydo,p_comment,
                p_gioplan,p_gioactual,p_ngaycn,p_nguoicn,p_thaydoi);
    ELSE
        UPDATE "PLAN_Dandory"
           SET khuonha=p_khuonha, khuonlap=p_khuonlap, stt=v_stt,
               ngaycapnhat=p_ngaycn, nguoicapnhat=p_nguoicn, thaydoi=p_thaydoi
         WHERE khunggio=p_time AND may=p_machine AND ngay=p_ngay;
    END IF;
END;
$function$
```

---

## 164. `SP_PRO_ADD_DIE`

```sql
CREATE OR REPLACE FUNCTION public."SP_PRO_ADD_DIE"(p_makhuon character varying, p_khuon character varying, p_loainhua character varying, p_t100t boolean, p_t180t boolean, p_t350t boolean, p_t450t boolean, p_t550t boolean, p_t650t boolean)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "PRO_DANDORI_TARGET" WHERE khuonlap=p_khuon OR khuonthao=p_khuon;

    INSERT INTO "PRO_Khuon_Nhua" (makhuon, tenkhuon, loainhua)
    VALUES (p_makhuon, p_khuon, p_loainhua);

    -- 6 loại máy
    INSERT INTO "PRO_PART_MCTYPE_CAN_RUN" (masp,tensp,loaimay,canrun)
    VALUES (p_makhuon,p_khuon,'100T',p_t100t),(p_makhuon,p_khuon,'180T',p_t180t),
           (p_makhuon,p_khuon,'350T',p_t350t),(p_makhuon,p_khuon,'450T',p_t450t),
           (p_makhuon,p_khuon,'550T',p_t550t),(p_makhuon,p_khuon,'650T',p_t650t);

    INSERT INTO "PRO_DANDORI_TARGET" (makhuonthao,khuonthao,makhuonlap,khuonlap,may,loaimay,target)
    SELECT DISTINCT t.part_no, t.Part_Name, v.part_no, v.Part_Name, p.tenthietbi, p.loaimay, 0
      FROM "tb_Part_master" t, "tb_Part_master" v, "PM_EquipmentInfo" p
     WHERE p.loaithietbi='IMM' AND (t.Part_Name=p_khuon OR v.Part_Name=p_khuon);

    UPDATE "PRO_DANDORI_TARGET"
       SET loaimay = loaimay || 'S'
     WHERE may IN ('B05','B06','B07','B09','B10','C11','D09','D10','D11','D12','D13')
       AND (khuonlap=p_khuon OR khuonthao=p_khuon);

    UPDATE "PRO_DANDORI_TARGET" dt SET nhuathao=n.loainhua
      FROM "PRO_Khuon_Nhua" n WHERE n.makhuon=dt.makhuonthao AND n.makhuon=p_makhuon;
    UPDATE "PRO_DANDORI_TARGET" dt SET nhualap=n.loainhua
      FROM "PRO_Khuon_Nhua" n WHERE n.makhuon=dt.makhuonlap AND n.makhuon=p_makhuon;

    UPDATE "PRO_DANDORI_TARGET" dt SET target=m.target
      FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=dt.nhualap AND m.nhuathao=dt.nhuathao AND m.loaimay=dt.loaimay
       AND (dt.khuonlap=p_khuon OR dt.khuonthao=p_khuon);

    DELETE FROM "PRO_DANDORI_TARGET" WHERE target=0 OR loaimay IS NULL;

    UPDATE "PRO_DANDORI_TARGET" dt SET target=0
      FROM "PRO_PART_MCTYPE_CAN_RUN" p
     WHERE dt.makhuonlap=p."Masp"
       AND (p.loaimay=substring(dt.loaimay,1,4) OR p.loaimay=dt.loaimay)
       AND p.canrun=false;
    UPDATE "PRO_DANDORI_TARGET" dt SET target=0
      FROM "PRO_PART_MCTYPE_CAN_RUN" p
     WHERE dt.makhuonthao=p."Masp"
       AND (p.loaimay=substring(dt.loaimay,1,4) OR p.loaimay=dt.loaimay)
       AND p.canrun=false;

    DELETE FROM "PRO_DANDORI_TARGET" WHERE target=0 OR loaimay IS NULL;
    DELETE FROM "PRO_DANDORI_TARGET" WHERE nhualap IS NULL OR nhuathao IS NULL;

    -- KL NHUA TAY
    DELETE FROM "PRO_KL_Nhua_Tay" WHERE khuonha=p_makhuon OR khuonlap=p_makhuon;
    INSERT INTO "PRO_KL_Nhua_Tay" (khuonha,tenkhuonha,khuonlap,tenkhuonlap)
    SELECT DISTINCT t.part_no, t.Part_Name, v.part_no, v.Part_Name
      FROM "tb_Part_master" t, "tb_Part_master" v
     WHERE t.Part_Name=p_khuon OR v.Part_Name=p_khuon;

    UPDATE "PRO_KL_Nhua_Tay" kt SET nhuaha=n.loainhua
      FROM "PRO_Khuon_Nhua" n WHERE n.makhuon=kt.khuonha AND kt.khuonha=p_makhuon;
    UPDATE "PRO_KL_Nhua_Tay" kt SET nhualap=n.loainhua
      FROM "PRO_Khuon_Nhua" n WHERE n.makhuon=kt.khuonlap AND kt.khuonlap=p_makhuon;

    UPDATE "PRO_KL_Nhua_Tay" kt SET _100t=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='100T'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _100ts=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='100TS'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _180t=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='180T'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _180ts=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='180TS'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _350t=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='350T'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _350ts=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='350TS'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _450t=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='450T'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _450ts=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='450TS'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _550t=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='550T'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _650t=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='650T'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);
    UPDATE "PRO_KL_Nhua_Tay" kt SET _650ts=m.klasa FROM "PRO_Dandori_Time_By_Material" m
     WHERE m.nhualap=kt.nhualap AND m.nhuathao=kt.nhuaha AND m.loaimay='650TS'
       AND (kt.khuonha=p_makhuon OR kt.khuonlap=p_makhuon);

    DELETE FROM "PRO_KL_Nhua_Tay" WHERE nhuaha IS NULL OR nhualap IS NULL;
END;
$function$
```

---

## 165. `SP_PRO_GET_MO_INFO`

```sql
CREATE OR REPLACE FUNCTION public."SP_PRO_GET_MO_INFO"()
 RETURNS TABLE(may character varying, masp character varying, tensp character varying, khuon character varying, hinhthuc character varying, tinhtrang character varying, comment character varying, giolaymau timestamp without time zone, ttsanpham character varying, ttthietbi character varying, commenttb character varying)
 LANGUAGE sql
AS $function$
SELECT may, masp, tensp, dieno AS khuon,
       hinhthuc, tinhtrang, comment, giolaymau,
       ketquatonghop AS ttsanpham, ktramay AS ttthietbi, commentmay AS commenttb
  FROM "tb_listketquakiemtra"
 WHERE id IN (
     SELECT max(id) FROM "tb_listketquakiemtra"
      WHERE masp IS NOT NULL AND length(may) = 3
      GROUP BY may
 )
 ORDER BY may;
$function$
```

---

## 166. `SP_PRO_GET_NG_DANDORI_FROM_APP`

```sql
CREATE OR REPLACE FUNCTION public."SP_PRO_GET_NG_DANDORI_FROM_APP"(p_tenkhuon character varying)
 RETURNS TABLE(dieno character varying, ngpct double precision)
 LANGUAGE sql
AS $function$

SELECT dieno,

       100.0 * count(*)::float /

       NULLIF((SELECT count(*) FROM "tb_listketquakiemtra"

                WHERE tensp = p_tenkhuon

                  AND hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)'), 0)

  FROM "tb_listketquakiemtra"

 WHERE tensp = p_tenkhuon

   AND hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)'

   AND ketquatonghop = 'NG'

 GROUP BY dieno;

$function$
```

---

## 167. `SP_RANK_APPEARANCE`

```sql
CREATE OR REPLACE FUNCTION public."SP_RANK_APPEARANCE"(p_isrunning boolean)
 RETURNS TABLE(rankngoaiquan character varying, quantity bigint)
 LANGUAGE plpgsql
AS $function$

BEGIN

    IF p_isrunning = 0 THEN

        RETURN QUERY

        SELECT rankngoaiquan, count(*) AS quantity

          FROM "tb_PartQuality"

         WHERE rankngoaiquan IS NOT NULL

         GROUP BY rankngoaiquan;

    ELSE

        RETURN QUERY

        SELECT rankngoaiquan, count(*) AS quantity

          FROM "tb_PartQuality"

         WHERE rankngoaiquan IS NOT NULL AND may IS NOT NULL

         GROUP BY rankngoaiquan;

    END IF;

END;

$function$
```

---

## 168. `SP_RANK_COMBINE`

```sql
CREATE OR REPLACE FUNCTION public."SP_RANK_COMBINE"(p_isrunning boolean)
 RETURNS TABLE(rank character varying, quantity bigint)
 LANGUAGE plpgsql
AS $function$

BEGIN

    IF p_isrunning = 0 THEN

        RETURN QUERY

        SELECT rankngoaiquan || '-' || rankkichthuoc AS rank, count(*) AS quantity

          FROM "tb_PartQuality"

         WHERE rankngoaiquan IS NOT NULL AND rankkichthuoc IS NOT NULL

         GROUP BY rankngoaiquan || '-' || rankkichthuoc;

    ELSE

        RETURN QUERY

        SELECT rankngoaiquan || '-' || rankkichthuoc AS rank, count(*) AS quantity

          FROM "tb_PartQuality"

         WHERE rankngoaiquan IS NOT NULL AND rankkichthuoc IS NOT NULL AND may IS NOT NULL

         GROUP BY rankngoaiquan || '-' || rankkichthuoc;

    END IF;

END;

$function$
```

---

## 169. `SP_RANK_MEASUREMENT`

```sql
CREATE OR REPLACE FUNCTION public."SP_RANK_MEASUREMENT"(p_isrunning boolean)
 RETURNS TABLE(rankkichthuoc character varying, quantity bigint)
 LANGUAGE plpgsql
AS $function$

BEGIN

    IF p_isrunning = 0 THEN

        RETURN QUERY

        SELECT rankkichthuoc, count(*) AS quantity

          FROM "tb_PartQuality"

         WHERE rankngoaiquan IS NOT NULL

         GROUP BY rankkichthuoc;

    ELSE

        RETURN QUERY

        SELECT rankkichthuoc, count(*) AS quantity

          FROM "tb_PartQuality"

         WHERE rankngoaiquan IS NOT NULL AND may IS NOT NULL

         GROUP BY rankkichthuoc;

    END IF;

END;

$function$
```

---

## 170. `SP_RANK_UPDATE_MEASUREMENT_RANK_QUALITY`

```sql
CREATE OR REPLACE FUNCTION public."SP_RANK_UPDATE_MEASUREMENT_RANK_QUALITY"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$

BEGIN

    UPDATE "tb_PartQuality" SET may = NULL;

    UPDATE "tb_PartQuality" SET "A" = 0, "B" = 0, "C" = 0, "D" = 0, "S" = 0;

    UPDATE "tb_PartQuality" SET "LastUpdate" = CURRENT_TIMESTAMP;

    UPDATE "tb_PartQuality" pq

       SET may = t.may

      FROM (SELECT may, masp, dieno FROM "tb_listketquakiemtra"

             WHERE id IN (SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY may)) AS t

     WHERE pq.masp = t.masp AND pq.sokhuon = t.dieno;

    -- C?p nh?t s? lu?ng A/B/C/D/S

    UPDATE "tb_PartQuality" pq SET "A" = t.quantity

      FROM (SELECT masp, sokhuon, count(*) AS quantity FROM "tb_PartAnalysis" WHERE rank='A' GROUP BY masp,sokhuon,rank) AS t

     WHERE pq.masp=t.masp AND pq.sokhuon=t."SoKhuon";

    UPDATE "tb_PartQuality" pq SET "B" = t.quantity

      FROM (SELECT masp, sokhuon, count(*) AS quantity FROM "tb_PartAnalysis" WHERE rank='B' GROUP BY masp,sokhuon,rank) AS t

     WHERE pq.masp=t.masp AND pq.sokhuon=t."SoKhuon";

    UPDATE "tb_PartQuality" pq SET "C" = t.quantity

      FROM (SELECT masp, sokhuon, count(*) AS quantity FROM "tb_PartAnalysis" WHERE rank='C' GROUP BY masp,sokhuon,rank) AS t

     WHERE pq.masp=t.masp AND pq.sokhuon=t."SoKhuon";

    UPDATE "tb_PartQuality" pq SET "D" = t.quantity

      FROM (SELECT masp, sokhuon, count(*) AS quantity FROM "tb_PartAnalysis" WHERE rank='D' GROUP BY masp,sokhuon,rank) AS t

     WHERE pq.masp=t.masp AND pq.sokhuon=t."SoKhuon";

    UPDATE "tb_PartQuality" pq SET "S" = t.quantity

      FROM (SELECT masp, sokhuon, count(*) AS quantity FROM "tb_PartAnalysis" WHERE rank='S' GROUP BY masp,sokhuon,rank) AS t

     WHERE pq.masp=t.masp AND pq.sokhuon=t."SoKhuon";

    -- Cascade rank (S ghi dè t?t c? sau cùng)

    UPDATE "tb_PartQuality" SET "rankKichThuoc" = 'D' WHERE "D" > 0;

    UPDATE "tb_PartQuality" SET "rankKichThuoc" = 'C' WHERE "C" > 0;

    UPDATE "tb_PartQuality" SET "rankKichThuoc" = 'B' WHERE "B" > 0;

    UPDATE "tb_PartQuality" SET "rankKichThuoc" = 'A' WHERE "A" > 0;

    UPDATE "tb_PartQuality" SET "rankKichThuoc" = 'S' WHERE "S" > 0;

    -- Point ngo?i quan

    UPDATE "tb_PartQuality" SET "pointNgoaiQuan" = 11 WHERE "rankNgoaiQuan" = 'S';

    UPDATE "tb_PartQuality" SET "pointNgoaiQuan" = 5  WHERE "rankNgoaiQuan" = 'A';

    UPDATE "tb_PartQuality" SET "pointNgoaiQuan" = 3  WHERE "rankNgoaiQuan" = 'B';

    UPDATE "tb_PartQuality" SET "pointNgoaiQuan" = 2  WHERE "rankNgoaiQuan" = 'C';

    UPDATE "tb_PartQuality" SET "pointNgoaiQuan" = 1  WHERE "rankNgoaiQuan" = 'D';

    UPDATE "tb_PartQuality" SET "pointNgoaiQuan" = 0  WHERE "rankNgoaiQuan" IS NULL;

    -- Point kích thu?c

    UPDATE "tb_PartQuality" SET "pointKichThuoc" = 11 WHERE "rankKichThuoc" = 'S';

    UPDATE "tb_PartQuality" SET "pointKichThuoc" = 5  WHERE "rankKichThuoc" = 'A';

    UPDATE "tb_PartQuality" SET "pointKichThuoc" = 3  WHERE "rankKichThuoc" = 'B';

    UPDATE "tb_PartQuality" SET "pointKichThuoc" = 2  WHERE "rankKichThuoc" = 'C';

    UPDATE "tb_PartQuality" SET "pointKichThuoc" = 1  WHERE "rankKichThuoc" = 'D';

    UPDATE "tb_PartQuality" SET "pointKichThuoc" = 0  WHERE "rankKichThuoc" IS NULL;

    UPDATE "tb_PartQuality" SET "totalPoint" = "pointKichThuoc" + "pointNgoaiQuan";

END;

$function$
```

---

## 171. `SP_SELECT_DEMENSION_DATA`

```sql
CREATE OR REPLACE FUNCTION public."SP_SELECT_DEMENSION_DATA"(p_idlistkqdo integer)
 RETURNS SETOF tb_chitietkqdo
 LANGUAGE sql
AS $function$

SELECT * FROM "tb_chitietkqdo"

 WHERE idlistketquado = p_idlistkqdo

 ORDER BY dungcudo;

$function$
```

---

## 172. `SP_SELECT_DEMENSION_DATA_PIC`

```sql
CREATE OR REPLACE FUNCTION public."SP_SELECT_DEMENSION_DATA_PIC"(p_idlistkqdo integer)
 RETURNS TABLE(nguoiktra character varying, dungcudo character varying, ngaygiodo timestamp without time zone, casx character varying)
 LANGUAGE sql
AS $function$

SELECT DISTINCT nguoiktra, dungcudo, ngayktra AS ngaygiodo, casx

  FROM "tb_chitietkqdo"

 WHERE idlistketquado = p_idlistkqdo;

$function$
```

---

## 173. `SP_STATISTIC_TROUBLE_BY_EQUIPMENT_TYPE`

```sql
CREATE OR REPLACE FUNCTION public."SP_STATISTIC_TROUBLE_BY_EQUIPMENT_TYPE"(p_loaitb character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$

DECLARE

    v_cols  text;

    v_query text;

BEGIN

    IF p_loaitb = 'ALLTB' THEN

        -- T?o danh sách c?t t? tensuco (g?p theo loaithietbi)

        SELECT string_agg(DISTINCT quote_ident(tensuco), ',')

          INTO v_cols

          FROM "PM_SuCoTrongNgay"

         WHERE tensuco IS NOT NULL AND tensuco != '';

        v_query := format(

            'SELECT loaithietbi, %s

               FROM (SELECT loaithietbi, tensuco, thoigiandungmay

                       FROM (SELECT loaithietbi, tensuco, SUM(thoigiandungmay) AS thoigiandungmay

                               FROM "PM_SuCoTrongNgay"

                              WHERE tensuco IS NOT NULL AND tensuco != ''''

                              GROUP BY loaithietbi, tensuco) AS t) AS x

              PIVOT (SUM(thoigiandungmay) FOR tensuco IN (%s)) p',

            v_cols, v_cols);

        -- NOTE: PostgreSQL không h? tr? PIVOT; dùng conditional aggregation:

        RAISE NOTICE 'Dynamic pivot query built (c?n crosstab extension ho?c client-side pivot)';

    ELSIF p_loaitb = 'ALL' THEN

        SELECT string_agg(DISTINCT quote_ident(tensuco), ',')

          INTO v_cols

          FROM "PM_SuCoTrongNgay"

         WHERE tensuco IS NOT NULL AND tensuco != '';

        RAISE NOTICE 'Dynamic pivot ALL: tenthietbi by tensuco, cols=%', v_cols;

    ELSE

        SELECT string_agg(DISTINCT quote_ident(tensuco), ',')

          INTO v_cols

          FROM "PM_SuCoTrongNgay"

         WHERE loaithietbi = p_loaitb AND tensuco IS NOT NULL AND tensuco != '';

        RAISE NOTICE 'Dynamic pivot by loaitb=%, cols=%', p_loaitb, v_cols;

    END IF;

    -- Th?c thi query th?c t? dùng conditional aggregation (PostgreSQL idiom)

    -- Caller nên dùng VIEW ho?c application-side pivot cho full support

END;

$function$
```

---

## 174. `SP_SoSPNG_UPDATE_NGAYTHUCTE`

```sql
CREATE OR REPLACE FUNCTION public."SP_SoSPNG_UPDATE_NGAYTHUCTE"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$

BEGIN

    -- Ngày th?c t?: n?u gi? < 8 ? ngày hôm tru?c

    UPDATE "tb_sospNG"

       SET ngaythucte = (ngayupdate - INTERVAL '1 day')::date

     WHERE EXTRACT(HOUR FROM ngayupdate) < 8 AND ngaythucte IS NULL;

    UPDATE "tb_sospNG"

       SET ngaythucte = ngayupdate::date

     WHERE EXTRACT(HOUR FROM ngayupdate) >= 8 AND ngaythucte IS NULL;

    -- Ca th?c t?

    UPDATE "tb_sospNG"

       SET cathucte = N'NGÀY'

     WHERE EXTRACT(HOUR FROM ngayupdate) >= 8

       AND EXTRACT(HOUR FROM ngayupdate) < 20

       AND cathucte IS NULL;

    UPDATE "tb_sospNG"

       SET cathucte = N'ĐÊM'

     WHERE cathucte IS NULL;

    -- LOAIMAY t? PM_EQUIPMENTINFO

    UPDATE "tb_sospNG" s

       SET loaimay = p."LoaiMay"

      FROM "PM_EquipmentInfo" p

     WHERE s.loaimay IS NULL AND p."TenThietBi" = s.mayduc;

    -- TOTALSTOCK t? TB_DAILY_NGRATE

    UPDATE "tb_sospNG" s

       SET totalstock = t.totalstockok

      FROM (SELECT sum(totalshotok) AS totalstockok, ca, ngay

              FROM "tb_Daily_NGRate" GROUP BY ca, ngay) AS t

     WHERE s.cathucte = t.ca AND s.ngaythucte = t.ngay;

    -- += t?ng NG

    UPDATE "tb_sospNG" s

       SET totalstock = totalstock + t."totalNG"

      FROM (SELECT (sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+

                    sum("NGloangnhua")+sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+

                    sum("NGchaykhi")+sum("NGsinkmask")+sum("NGketsp")+sum("Nhatmau")+

                    sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other")+sum(sospbo)) AS totalng,

                   cathucte, ngaythucte

              FROM "tb_sospNG" GROUP BY ngaythucte, cathucte) AS t

     WHERE s.cathucte = t.cathucte AND s.ngaythucte = t.ngaythucte;

    -- STOCK t?ng hàng

    UPDATE "tb_sospNG" s

       SET stock = (d.totalshotok + "NGsilver" + "NGchamden" + "NGtapchat" + "NGdinhdau" +

                    "NGloangnhua" + "NGautohand" + "NGshortmold" + "NGflowmask" + "NGchaykhi" +

                    "NGsinkmask" + "NGketsp" + "Nhatmau" + "Xuoc" + hakka + jig + "Other" + sospbo)

      FROM "tb_Daily_NGRate" d

     WHERE s.cathucte = d.ca AND s.ngaythucte = d.ngay

       AND s."Masp" = d."Masp" AND s."SoKhuon" = d."SoKhuon";

END;

$function$
```

---

## 175. `SP_THONGKELOITHEOSOSHOT`

```sql
CREATE OR REPLACE FUNCTION public."SP_THONGKELOITHEOSOSHOT"()
 RETURNS TABLE(diename character varying, currentshot integer, avgshottotrouble integer, nextshottotrouble integer)
 LANGUAGE sql
AS $function$

SELECT tenkhuon || '-' || sokhuon AS diename,

       max(shot) AS currentshot,

       (max(shot) - min(shot)) / NULLIF(count(*), 0) AS avgshottotrouble,

       max(shot) + (max(shot) - min(shot)) / NULLIF(count(*), 0) AS nextshottotrouble

  FROM "DTS_DieTrouble"

 WHERE EXTRACT(YEAR FROM ngayupdate) > 2016 AND loikhuon = true

 GROUP BY tenkhuon, sokhuon

 ORDER BY tenkhuon, sokhuon;

$function$
```

---

## 176. `SP_THONGKETHOIGIANDUNGMAY`

```sql
CREATE OR REPLACE FUNCTION public."SP_THONGKETHOIGIANDUNGMAY"(p_loaimay character varying, p_loaithietbi character varying, p_nam integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$

BEGIN

    DELETE FROM "PM_ThongKeThoiGianDungMay";

    -- NHÁNH 1: T?T C? Ð?I MÁY

    IF p_loaimay = N'TẤT CẢ ĐẠI MÁY' THEN

        IF p_loaithietbi = N'T?T C? CÁC THI?T B?' THEN

            INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

            SELECT to_char(ngay,'YYYY-MM'), p_loaimay, p_loaithietbi, loaithietbi, sum(thoigiandungmay)

              FROM "PM_SuCoTrongNgay"

             WHERE thoigiandungmay > 0 AND EXTRACT(YEAR FROM ngay) = p_nam

             GROUP BY to_char(ngay,'YYYY-MM'), loaithietbi;

        ELSE

            INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

            SELECT to_char(ngay,'YYYY-MM'), p_loaimay, p_loaithietbi, tenthietbi, sum(thoigiandungmay)

              FROM "PM_SuCoTrongNgay"

             WHERE loaithietbi = p_loaithietbi AND thoigiandungmay > 0

               AND EXTRACT(YEAR FROM ngay) = p_nam

             GROUP BY to_char(ngay,'YYYY-MM'), loaithietbi, tenthietbi;

        END IF;

    -- NHÁNH 2: T?T C? CÁC THI?T B? (c? 2 tham s? = T?T C? CÁC TB)

    ELSIF p_loaimay = N'T?T C? CÁC THI?T B?' AND p_loaithietbi = N'T?T C? CÁC THI?T B?' THEN

        INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

        SELECT to_char(ngay,'YYYY-MM'), p_loaimay, p_loaimay, loaithietbi, sum(thoigiandungmay)

          FROM "PM_SuCoTrongNgay"

         WHERE loaithietbi IN ('IMM','AUTOHAND','FEEDING',N'THI?T B? PH? TR?')

           AND thoigiandungmay > 0 AND EXTRACT(YEAR FROM ngay) = p_nam

         GROUP BY loaithietbi, to_char(ngay,'YYYY-MM');

    -- NHÁNH 3: Lo?i máy don (100T/180T/350T/450T/550T/650T)

    ELSIF p_loaimay IN ('100T','180T','350T','450T','550T','650T') THEN

        IF p_loaithietbi = N'T?T C? CÁC THI?T B?' THEN

            INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

            SELECT to_char(ngay,'YYYY-MM'), p_loaimay, p_loaithietbi, loaithietbi, sum(thoigiandungmay)

              FROM "PM_SuCoTrongNgay"

             WHERE loaimay = p_loaimay AND thoigiandungmay > 0

               AND EXTRACT(YEAR FROM ngay) = p_nam

             GROUP BY loaimay, to_char(ngay,'YYYY-MM'), loaithietbi;

        ELSE

            INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

            SELECT to_char(ngay,'YYYY-MM'), p_loaimay, p_loaithietbi, chitiet, sum(thoigiandungmay)

              FROM "PM_SuCoTrongNgay"

             WHERE loaimay = p_loaimay AND loaithietbi = p_loaithietbi

               AND thoigiandungmay > 0 AND EXTRACT(YEAR FROM ngay) = p_nam

             GROUP BY loaimay, loaithietbi, to_char(ngay,'YYYY-MM'), chitiet;

        END IF;

    -- NHÁNH 4: Máy don l? (A1, A2... theo VITRI)

    ELSE

        IF p_loaithietbi = N'T?T C? CÁC THI?T B?' THEN

            INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

            SELECT to_char(ngay,'YYYY-MM'), p_loaimay, p_loaithietbi, loaithietbi, sum(thoigiandungmay)

              FROM "PM_SuCoTrongNgay"

             WHERE thoigiandungmay > 0 AND chitiet <> N'T?C KIM LO?I'

               AND EXTRACT(YEAR FROM ngay) = p_nam AND vitri = p_loaimay

             GROUP BY loaimay, to_char(ngay,'YYYY-MM'), loaithietbi;

        ELSE

            INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

            SELECT to_char(ngay,'YYYY-MM'), p_loaimay, p_loaithietbi, chitiet, sum(thoigiandungmay)

              FROM "PM_SuCoTrongNgay"

             WHERE loaithietbi = p_loaithietbi AND thoigiandungmay > 0

               AND chitiet <> N'T?C KIM LO?I'

               AND EXTRACT(YEAR FROM ngay) = p_nam AND vitri = p_loaimay

             GROUP BY loaimay, loaithietbi, to_char(ngay,'YYYY-MM'), chitiet;

        END IF;

    END IF;

    -- Insert V_ST ACTUAL (t?ng theo tháng)

    INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

    SELECT thang, p_loaimay, p_loaithietbi, 'V_ST ACTUAL', sum(actual)

      FROM "PM_ThongKeThoiGianDungMay" GROUP BY thang;

    -- Insert V_ST TARGET và AVG (n?u không ph?i loaimay = "T?T C? CÁC THI?T B?")

    IF p_loaimay <> N'T?T C? CÁC THI?T B?' THEN

        INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

        SELECT s.nam::text || '-' || s.thang::text, s.loaimay, s.loaithietbi, 'V_ST TARGET', s.target

          FROM "PM_StopTimeTarget" s

         WHERE s.loaimay = p_loaimay AND s.loaithietbi = p_loaithietbi AND s.nam = p_nam;

        INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

        SELECT 'AVERAGE 2019', s.loaimay, s.loaithietbi, 'AVG', s.avglastyear

          FROM "PM_StopTimeTarget" s

         WHERE s.loaimay = p_loaimay AND s.loaithietbi = p_loaithietbi AND s.nam = p_nam

         LIMIT 1;

    ELSE

        IF p_loaithietbi = N'T?T C? CÁC THI?T B?' THEN

            INSERT INTO "PM_ThongKeThoiGianDungMay" (thang,loaimay,loaithietbi,loailoi,actual)

            SELECT s.nam::text || '-' || s.thang::text,

                   N'T?T C? CÁC THI?T B?', N'T?T C? CÁC THI?T B?', 'V_ST TARGET', sum(target)

              FROM "PM_StopTimeTarget" s

             WHERE s.loaimay = N'T?T C? CÁC THI?T B?' AND s.nam = p_nam

             GROUP BY thang, nam;

        END IF;

    END IF;

END;

$function$
```

---

## 177. `SP_TONGQUANCHATLUON`

```sql
CREATE OR REPLACE FUNCTION public."SP_TONGQUANCHATLUON"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$

BEGIN

    UPDATE "tb_ShottingRealTime" s

       SET Cavity = p.Cavity

      FROM "tb_Part_master" p

     WHERE s."Masp" = p."Part_no" AND s."Sokhuon" = p.Die_no;

    UPDATE "tb_ShottingRealTime" s

       SET totalshot = f.totalshot, totalng = f."totalNG", ngrate = f.ngrate

      FROM "tb_FollowByLot" f

     WHERE s."Masp" = f."Masp" AND s."Sokhuon" = f."SoKhuon";

END;

$function$
```

---

## 178. `SP_UPDATE_AVGOFRUNNER`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_AVGOFRUNNER"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    INSERT INTO "tb_WeightRunner" (masp, sokhuon, tensp, ngayupdate, nguoiupdate)
    SELECT "Part_no", "Die_no", "Part_name", CURRENT_TIMESTAMP, 'SUPER ADMIN'
      FROM "tb_Part_master"
     WHERE "Part_no" || '-' || "Die_no" NOT IN
           (SELECT masp || '-' || sokhuon FROM "tb_WeightRunner");

    INSERT INTO "tb_AvgOfWeigh" (masp, tensp, ngayupdate, nguoiupdate)
    SELECT "Part_no", "Part_name", CURRENT_TIMESTAMP, 'SUPER ADMIN'
      FROM "tb_Part_master"
     WHERE "Part_no" NOT IN (SELECT masp FROM "tb_AvgOfWeigh");

    UPDATE "tb_WeightRunner" w
       SET sorunner = t.soluongrunner, tongcannang = t.tongcannang
      FROM (SELECT "Part_no", "Die_no", soluongrunner, tongcannang
              FROM "ENG_DKD"
             WHERE id IN (SELECT max(id) FROM "ENG_DKD"
                           WHERE lydoissuedkd = 'Trial FA'
                           GROUP BY "Part_no", "Die_no")) t
     WHERE w.masp = t."Part_no" AND w.sokhuon = t."Die_no";

    UPDATE "tb_AvgOfWeigh" a
       SET sorunner   = t.sorunner,
           avg_runner = t.tongcannang
      FROM (SELECT masp,
                   round(avg(sorunner::numeric), 0)::float AS sorunner,
                   round(avg(tongcannang::numeric), 2)::float AS tongcannang
              FROM "tb_WeightRunner" GROUP BY masp) AS t
     WHERE a.masp = t.masp;
END;
$function$
```

---

## 179. `SP_UPDATE_AVGOFWEIGH`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_AVGOFWEIGH"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_AvgOfWeigh" a
       SET socavity = p."Cavity"
      FROM "tb_Part_master" p
     WHERE p."Part_no" = a.masp;

    UPDATE "tb_AvgOfWeigh" a SET avg_cav1 = t.avg1
      FROM (SELECT part_no, round(avg(cav1)::numeric, 2)::float AS avg1 FROM "FA-TVP"
             WHERE id IN (SELECT max(id) FROM "FA-TVP"
                           WHERE cav1 != 0 AND ketqua = 'OK'
                           GROUP BY part_no, die_no)
             GROUP BY part_no) AS t
     WHERE a.masp = t.part_no;

    UPDATE "tb_AvgOfWeigh" a SET avg_cav2 = t.avg1
      FROM (SELECT part_no, round(avg(cav2)::numeric, 2)::float AS avg1 FROM "FA-TVP"
             WHERE id IN (SELECT max(id) FROM "FA-TVP"
                           WHERE cav2 != 0 AND ketqua = 'OK'
                           GROUP BY part_no, die_no)
             GROUP BY part_no) AS t
     WHERE a.masp = t.part_no;

    UPDATE "tb_AvgOfWeigh" a SET avg_cav3 = t.avg1
      FROM (SELECT part_no, round(avg(cav3)::numeric, 2)::float AS avg1 FROM "FA-TVP"
             WHERE id IN (SELECT max(id) FROM "FA-TVP"
                           WHERE cav3 != 0 AND ketqua = 'OK'
                           GROUP BY part_no, die_no)
             GROUP BY part_no) AS t
     WHERE a.masp = t.part_no;

    UPDATE "tb_AvgOfWeigh" a SET avg_cav4 = t.avg1
      FROM (SELECT part_no, round(avg(cav4)::numeric, 2)::float AS avg1 FROM "FA-TVP"
             WHERE id IN (SELECT max(id) FROM "FA-TVP"
                           WHERE cav4 != 0 AND ketqua = 'OK'
                           GROUP BY part_no, die_no)
             GROUP BY part_no) AS t
     WHERE a.masp = t.part_no;
END;
$function$
```

---

## 180. `SP_UPDATE_FA_THAM_KHAO`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_FA_THAM_KHAO"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_HangMucDoNewVer" h
       SET gioihanduoifa = t.gioihanduoifa
      FROM (SELECT groupname, items, vitri, min(gioihanduoifa) AS gioihanduoifa
              FROM "tb_HangMucDoChiTietNewVer"
             WHERE gioihanduoifa != 1000
             GROUP BY groupname, items, vitri) AS t
     WHERE h.groupname = t.groupname AND h.items = t.items AND h.vitri = t.vitri;

    UPDATE "tb_HangMucDoNewVer" h
       SET gioihantrenfa = t.gioihantrenfa
      FROM (SELECT groupname, items, vitri, max(gioihantrenfa) AS gioihantrenfa
              FROM "tb_HangMucDoChiTietNewVer"
             WHERE gioihantrenfa != 1000
             GROUP BY groupname, items, vitri) AS t
     WHERE h.groupname = t.groupname AND h.items = t.items AND h.vitri = t.vitri;

    UPDATE "tb_HangMucDoNewVer"
       SET gioihanduoifa = 1000
     WHERE groupname || vitri || items::text NOT IN (
         SELECT groupname || vitri || items::text
           FROM "tb_HangMucDoChiTietNewVer"
          WHERE gioihanduoifa != 1000
          GROUP BY groupname, items, vitri
         HAVING min(gioihanduoifa) != 1000);

    UPDATE "tb_HangMucDoNewVer"
       SET gioihantrenfa = 1000
     WHERE groupname || vitri || items::text NOT IN (
         SELECT groupname || vitri || items::text
           FROM "tb_HangMucDoChiTietNewVer"
          WHERE gioihantrenfa != 1000
          GROUP BY groupname, items, vitri
         HAVING max(gioihantrenfa) != 1000);
END;
$function$
```

---

## 181. `SP_UPDATE_LINH_KIEN`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_LINH_KIEN"(p_loaithietbi character varying, p_tenthietbi character varying, p_vitrilaplinhkien character varying, p_barcode character varying, p_tenlinhkien character varying, p_ngaylap date, p_id character varying, p_nguoiupdate character varying, p_ngayupdate timestamp without time zone, p_type character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_ngaylapcu     date;
    v_nguoiupdatecu varchar(50);
    v_ngayupdatecu  date;
    v_tuoitho       int;
    v_ngaythaycu    date;
BEGIN
    IF p_type = 'INSERT' THEN
        IF EXISTS (SELECT 1 FROM "PM_TinhTrangLinhKien"
                    WHERE tenthietbi=p_tenthietbi AND loaithietbi=p_loaithietbi
                      AND vitrilaplinhkien=p_vitrilaplinhkien AND tenlinhkien=p_tenlinhkien) THEN

            SELECT ngaylap, nguoiupdate, ngayupdate::date
              INTO v_ngaylapcu, v_nguoiupdatecu, v_ngayupdatecu
              FROM "PM_TinhTrangLinhKien"
             WHERE tenthietbi=p_tenthietbi AND loaithietbi=p_loaithietbi
               AND vitrilaplinhkien=p_vitrilaplinhkien AND tenlinhkien=p_tenlinhkien;

            IF v_ngaylapcu IS NOT NULL THEN
                v_tuoitho := p_ngaylap - v_ngaylapcu;
            END IF;

            INSERT INTO "PM_QuanLyTuoiTho" (tenthietbi,loaithietbi,barcode,vitrilinhkien,tenlinhkien,
               ngaylap,ngaythay,tuoitho,id_sparepart,nguoiupdate,ngayupdate)
            VALUES (p_tenthietbi,p_loaithietbi,p_barcode,p_vitrilaplinhkien,p_tenlinhkien,
                    v_ngaylapcu,p_ngaylap,v_tuoitho,p_id,v_nguoiupdatecu,v_ngayupdatecu);

            UPDATE "PM_TinhTrangLinhKien"
               SET barcode=upper(p_barcode), tenlinhkien=p_tenlinhkien,
                   ngaylap=p_ngaylap, nguoiupdate=p_nguoiupdate, ngayupdate=p_ngayupdate
             WHERE tenthietbi=p_tenthietbi AND loaithietbi=p_loaithietbi
               AND vitrilaplinhkien=p_vitrilaplinhkien AND tenlinhkien=p_tenlinhkien;
        END IF;

    ELSIF p_type = 'DELETE' THEN
        IF EXISTS (SELECT 1 FROM "PM_QuanLyTuoiTho" WHERE id_sparepart = p_id) THEN
            SELECT ngaylap INTO v_ngaythaycu FROM "PM_QuanLyTuoiTho" WHERE id_sparepart = p_id;
            UPDATE "PM_TinhTrangLinhKien"
               SET ngaylap=v_ngaythaycu, nguoiupdate=p_nguoiupdate, ngayupdate=p_ngayupdate
             WHERE tenthietbi=p_tenthietbi AND loaithietbi=p_loaithietbi
               AND vitrilaplinhkien=p_vitrilaplinhkien AND tenlinhkien=p_tenlinhkien;
            DELETE FROM "PM_QuanLyTuoiTho" WHERE id_sparepart = p_id;
        END IF;
    END IF;
END;
$function$
```

---

## 182. `SP_UPDATE_MASANPHAM_NHUA`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_MASANPHAM_NHUA"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "Material" m
       SET "Nhacungcap"=p.nhacungcap, "Loainhua"=p.loainhua,
           "Mamau"=p.mamau, "Maunhua"=p.maunhua, "Tennhua"=p.tennhua
      FROM "PRO_MATERIAL_INFO" p WHERE m.idnhua = p.id;

    UPDATE "tb_Part_master" t
       SET taisd_drw = m.taisudung_phantram
      FROM "Material" m WHERE t.Part_no = m.Part_no;
END;
$function$
```

---

## 183. `SP_UPDATE_NGOAIHINH_REPORT`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_NGOAIHINH_REPORT"(p_objects character varying, p_type character varying, p_ngaystart timestamp without time zone, p_ngayend timestamp without time zone)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE v_filter_col text; v_ng_col text;
BEGIN
    DELETE FROM "tb_ReportNgoaiHinhAll";

    -- Xác định cột group và join
    IF p_objects = N'OPS'  THEN v_filter_col := 'nguoiktra'; v_ng_col := 'nguoiktra';
    ELSIF p_objects = N'HT'   THEN v_filter_col := 'hinhthuc';  v_ng_col := 'hinhthuc';
    ELSIF p_objects = N'MC'   THEN v_filter_col := 'may';        v_ng_col := 'may';
    ELSIF p_objects = N'LINE' THEN v_filter_col := 'substring(may,1,1)'; v_ng_col := 'line';
    END IF;

    -- Dùng dynamic SQL để tránh lặp 8 nhánh
    IF p_type = N'ALL' THEN
        IF p_objects = N'OPS' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT nguoiktra, count(nguoiktra), 0 FROM "tb_listketquakiemtra" GROUP BY nguoiktra;
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT nguoiktra, count(*) AS solanng FROM "tb_listketquakiemtra"
                     WHERE ketquatonghop=N'NG' GROUP BY nguoiktra) t
             WHERE t.nguoiktra=r.nguoiktra;
        ELSIF p_objects = N'HT' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT hinhthuc, count(hinhthuc), 0 FROM "tb_listketquakiemtra" GROUP BY hinhthuc;
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT hinhthuc, count(*) AS solanng FROM "tb_listketquakiemtra"
                     WHERE ketquatonghop=N'NG' GROUP BY hinhthuc) t
             WHERE t."Hinhthuc"=r.nguoiktra;
        ELSIF p_objects = N'MC' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT may, count(may), 0 FROM "tb_listketquakiemtra" GROUP BY may;
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT may, count(*) AS solanng FROM "tb_listketquakiemtra"
                     WHERE ketquatonghop=N'NG' GROUP BY may) t
             WHERE t.may=r.nguoiktra;
        ELSIF p_objects = N'LINE' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT substring(may,1,1), count(substring(may,1,1)), 0
              FROM "tb_listketquakiemtra" GROUP BY substring(may,1,1);
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT substring(may,1,1) AS line, count(*) AS solanng
                      FROM "tb_listketquakiemtra" WHERE ketquatonghop=N'NG'
                     GROUP BY substring(may,1,1)) t
             WHERE t.line=r.nguoiktra;
        END IF;
    ELSE -- NGAY
        IF p_objects = N'OPS' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT nguoiktra, count(nguoiktra), 0 FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_ngaystart AND p_ngayend GROUP BY nguoiktra;
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT nguoiktra, count(*) AS solanng FROM "tb_listketquakiemtra"
                     WHERE ketquatonghop=N'NG' AND ngayktra BETWEEN p_ngaystart AND p_ngayend
                     GROUP BY nguoiktra) t WHERE t.nguoiktra=r.nguoiktra;
        ELSIF p_objects = N'HT' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT hinhthuc, count(hinhthuc), 0 FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_ngaystart AND p_ngayend GROUP BY hinhthuc;
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT hinhthuc, count(*) AS solanng FROM "tb_listketquakiemtra"
                     WHERE ketquatonghop=N'NG' AND ngayktra BETWEEN p_ngaystart AND p_ngayend
                     GROUP BY hinhthuc) t WHERE t."Hinhthuc"=r.nguoiktra;
        ELSIF p_objects = N'MC' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT may, count(may), 0 FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_ngaystart AND p_ngayend GROUP BY may;
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT may, count(*) AS solanng FROM "tb_listketquakiemtra"
                     WHERE ketquatonghop=N'NG' AND ngayktra BETWEEN p_ngaystart AND p_ngayend
                     GROUP BY may) t WHERE t.may=r.nguoiktra;
        ELSIF p_objects = N'LINE' THEN
            INSERT INTO "tb_ReportNgoaiHinhAll"
            SELECT substring(may,1,1), count(*), 0 FROM "tb_listketquakiemtra"
             WHERE ngayktra BETWEEN p_ngaystart AND p_ngayend GROUP BY substring(may,1,1);
            UPDATE "tb_ReportNgoaiHinhAll" r SET solanng=t.solanng
              FROM (SELECT substring(may,1,1) AS line, count(*) AS solanng
                      FROM "tb_listketquakiemtra" WHERE ketquatonghop=N'NG'
                       AND ngayktra BETWEEN p_ngaystart AND p_ngayend
                     GROUP BY substring(may,1,1)) t WHERE t.line=r.nguoiktra;
        END IF;
    END IF;
END;
$function$
```

---

## 184. `SP_UPDATE_NGRATE_REALTIME`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_NGRATE_REALTIME"(p_may character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_id      int;
    v_masp    varchar(50);
    v_sokhuon varchar(50);
    v_cavity  int;
BEGIN
    DELETE FROM "tb_FollowNGRateRealTime_Test";

    INSERT INTO "tb_FollowNGRateRealTime_Test" (may, masp, sokhuon, hinhthuc, idlistkq, giolaymau, shotcurrent)
    SELECT may, masp, dieno, hinhthuc, id,
           giolaymau,
           EXTRACT(EPOCH FROM CURRENT_TIMESTAMP - giolaymau)::int
      FROM "tb_listketquakiemtra"
     WHERE may = p_may AND hinhthuc = N'KHUÔN LẮP/ KHỞI ĐỘNG (FS)'
     ORDER BY id DESC LIMIT 1;

    SELECT id, masp, sokhuon INTO v_id, v_masp, v_sokhuon
      FROM "tb_FollowNGRateRealTime_Test";

    SELECT Cavity INTO v_cavity FROM "tb_Part_master" WHERE Part_no = v_masp;

    UPDATE "tb_FollowNGRateRealTime_Test"
       SET ngshot=t."totalNG", NGsilver=t.NGsilver, NGchamden=t.NGchamden,
           NGtapchat=t.NGtapchat, NGdinhdau=t.NGdinhdau, NGloangnhua=t.NGloangnhua,
           NGautohand=t.NGautohand, NGshortmold=t.NGshortmold, NGflowmask=t.NGflowmask,
           NGchaykhi=t.NGchaykhi, NGsinkmask=t.NGsinkmask, NGketsp=t.NGketsp,
           nhatmau=t.nhatmau, xuoc=t.xuoc, hakka=t.hakka, jig=t.jig, other=t.other
      FROM (SELECT sum(NGsilver) AS NGsilver, sum(NGchamden) AS NGchamden,
                   sum(NGtapchat) AS NGtapchat, sum(NGdinhdau) AS NGdinhdau,
                   sum(NGloangnhua) AS NGloangnhua, sum(NGautohand) AS NGautohand,
                   sum(NGshortmold) AS NGshortmold, sum(NGflowmask) AS NGflowmask,
                   sum(NGchaykhi) AS NGchaykhi, sum(NGsinkmask) AS NGsinkmask,
                   sum(NGketsp) AS NGketsp, sum(nhatmau) AS nhatmau,
                   sum(xuoc) AS xuoc, sum(hakka) AS hakka, sum(jig) AS jig, sum(other) AS other,
                   (sum(NGsilver)+sum(NGchamden)+sum(NGtapchat)+sum(NGdinhdau)+sum(NGloangnhua)+
                    sum(NGautohand)+sum(NGshortmold)+sum(NGflowmask)+sum(NGchaykhi)+sum(NGsinkmask)+
                    sum(NGketsp)+sum(nhatmau)+sum(xuoc)+sum(hakka)+sum(jig)+sum(other)) AS totalng
              FROM "tb_sospNG"
             WHERE mayduc=p_may AND id >= v_id AND masp=v_masp AND sokhuon=v_sokhuon) AS t;

    -- Chia theo shot và Cavity
    UPDATE "tb_FollowNGRateRealTime_Test"
       SET NGsilver=round(NGsilver::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGchamden=round(NGchamden::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGtapchat=round(NGtapchat::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGdinhdau=round(NGdinhdau::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGloangnhua=round(NGloangnhua::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGautohand=round(NGautohand::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGshortmold=round(NGshortmold::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGflowmask=round(NGflowmask::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGchaykhi=round(NGchaykhi::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGsinkmask=round(NGsinkmask::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           NGketsp=round(NGketsp::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           nhatmau=round(nhatmau::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           xuoc=round(xuoc::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           hakka=round(hakka::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           jig=round(jig::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2),
           other=round(other::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2);

    UPDATE "tb_FollowNGRateRealTime_Test"
       SET ngrate = round(ngshot::float/NULLIF(shotcurrent,0)/NULLIF(v_cavity,0)*100,2)
     WHERE shotcurrent != 0 AND v_cavity != 0;

    UPDATE "tb_FollowNGRateRealTime_Test"
       SET cut=t.cut, an=t.an, "to"=t."to", tray=t.tray, lan=t.lan,
           other=t.other, thaotaccong=t.thaotaccong, tongdiemtt=t.total
      FROM (SELECT sum(cut) AS cut, sum(an) AS an, sum("to") AS "to",
                   sum(tray) AS tray, sum(lan) AS lan, sum(other) AS other,
                   sum(thaotaccong) AS thaotaccong,
                   (sum(cut)+sum(an)+sum("to")+sum(tray)+sum(lan)+sum(other)+sum(thaotaccong)) AS total
              FROM "tb_Burr"
             WHERE may=p_may AND idlistkqktra >= v_id AND Part_no=v_masp AND Die_no=v_sokhuon) AS t;
END;
$function$
```

---

## 185. `SP_UPDATE_OP_DUNGMAY`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_OP_DUNGMAY"(p_shift character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_shift = 'A' THEN
        UPDATE "tb_BangOPDungMay_A" ba
           SET part=lk."Masp", tinhtrang=lk.tinhtrang, sokhuon=lk.dieno
          FROM "tb_listketquakiemtra" lk
         WHERE ba.may = lk.may
           AND lk.id IN (SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY may);
    ELSIF p_shift = 'B' THEN
        UPDATE "tb_BangOPDungMay_B" bb
           SET part=lk."Masp", tinhtrang=lk.tinhtrang, sokhuon=lk.dieno
          FROM "tb_listketquakiemtra" lk
         WHERE bb.may = lk.may
           AND lk.id IN (SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY may);
    ELSIF p_shift = 'C' THEN
        UPDATE "tb_BangOPDungMay_C" bc
           SET part=lk."Masp", tinhtrang=lk.tinhtrang, sokhuon=lk.dieno
          FROM "tb_listketquakiemtra" lk
         WHERE bc.may = lk.may
           AND lk.id IN (SELECT max(id) FROM "tb_listketquakiemtra" GROUP BY may);
    END IF;
END;
$function$
```

---

## 186. `SP_UPDATE_PIC_OP`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_PIC_OP"(p_code character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE v_anh bytea; v_name varchar(50);
BEGIN
    IF p_code IS NULL THEN RETURN; END IF;

    SELECT "AnhThe"::bytea, "Name" INTO v_anh, v_name
      FROM "tb_ListOP" WHERE "Code" = p_code;

    -- CODE0
    UPDATE "tb_BangOPDungMay_A" SET anh0=v_anh WHERE code0=p_code;
    UPDATE "tb_BangOPDungMay_B" SET anh0=v_anh WHERE code0=p_code;
    UPDATE "tb_BangOPDungMay_C" SET anh0=v_anh WHERE code0=p_code;
    UPDATE "tb_BangOPDungMay_A" SET name0=v_name WHERE code0=p_code;
    UPDATE "tb_BangOPDungMay_B" SET name0=v_name WHERE code0=p_code;
    UPDATE "tb_BangOPDungMay_C" SET name0=v_name WHERE code0=p_code;
    -- CODE1
    UPDATE "tb_BangOPDungMay_A" SET anh1=v_anh WHERE code1=p_code;
    UPDATE "tb_BangOPDungMay_B" SET anh1=v_anh WHERE code1=p_code;
    UPDATE "tb_BangOPDungMay_C" SET anh1=v_anh WHERE code1=p_code;
    UPDATE "tb_BangOPDungMay_A" SET name1=v_name WHERE code1=p_code;
    UPDATE "tb_BangOPDungMay_B" SET name1=v_name WHERE code1=p_code;
    UPDATE "tb_BangOPDungMay_C" SET name1=v_name WHERE code1=p_code;
    -- CODE2
    UPDATE "tb_BangOPDungMay_A" SET anh2=v_anh WHERE code2=p_code;
    UPDATE "tb_BangOPDungMay_B" SET anh2=v_anh WHERE code2=p_code;
    UPDATE "tb_BangOPDungMay_C" SET anh2=v_anh WHERE code2=p_code;
    UPDATE "tb_BangOPDungMay_A" SET name2=v_name WHERE code2=p_code;
    UPDATE "tb_BangOPDungMay_B" SET name2=v_name WHERE code2=p_code;
    UPDATE "tb_BangOPDungMay_C" SET name2=v_name WHERE code2=p_code;
    -- CODE3
    UPDATE "tb_BangOPDungMay_A" SET anh3=v_anh WHERE code3=p_code;
    UPDATE "tb_BangOPDungMay_B" SET anh3=v_anh WHERE code3=p_code;
    UPDATE "tb_BangOPDungMay_C" SET anh3=v_anh WHERE code3=p_code;
    UPDATE "tb_BangOPDungMay_A" SET name3=v_name WHERE code3=p_code;
    UPDATE "tb_BangOPDungMay_B" SET name3=v_name WHERE code3=p_code;
    UPDATE "tb_BangOPDungMay_C" SET name3=v_name WHERE code3=p_code;
    -- CODE4
    UPDATE "tb_BangOPDungMay_A" SET anh4=v_anh WHERE code4=p_code;
    UPDATE "tb_BangOPDungMay_B" SET anh4=v_anh WHERE code4=p_code;
    UPDATE "tb_BangOPDungMay_C" SET anh4=v_anh WHERE code4=p_code;
    UPDATE "tb_BangOPDungMay_A" SET name4=v_name WHERE code4=p_code;
    UPDATE "tb_BangOPDungMay_B" SET name4=v_name WHERE code4=p_code;
    UPDATE "tb_BangOPDungMay_C" SET name4=v_name WHERE code4=p_code;
END;
$function$
```

---

## 187. `SP_UPDATE_SAME_COLOR`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SAME_COLOR"(p_grouppart character varying, p_dieno character varying, p_ngaysua date, p_ngayduc date, p_comment character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_FollowBurr"
       SET ngaysua = p_ngaysua, ngayduc = p_ngayduc, commentplan = p_comment
     WHERE grouppart = p_grouppart AND sokhuon = p_dieno;

    UPDATE "tb_FollowBurr" fb
       SET grouppart = p.grouppart
      FROM "tb_Part_master" p
     WHERE p."Part_no" = fb.masp AND p."Die_no" = fb.sokhuon;
END;
$function$
```

---

## 188. `SP_UPDATE_SAN_LUONG`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SAN_LUONG"(p_startdate date, p_enddate date, p_nguoiupdate character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    INSERT INTO "tb_Daily_NGRate" (ngay, ca, masp, sokhuon, totalshotok, cycletime, ngayupdate, nguoiupdate)
    SELECT ngay, 'NGÀY', masanpham, makhuon,
           sum(okthucte), avg(cycletime), CURRENT_TIMESTAMP, p_nguoiupdate
      FROM "PLAN_BCSX"
     WHERE masanpham IS NOT NULL AND makhuon IS NOT NULL
       AND okthucte > 0 AND cycletime > 0
       AND thoigian IN ('8-10','10-12','12-14','14-16','16-18','18-20')
       AND ngay BETWEEN p_startdate AND p_enddate
     GROUP BY masanpham, makhuon, ngay
    UNION
    SELECT ngay, 'ĐÊM', masanpham, makhuon,
           sum(okthucte), avg(cycletime), CURRENT_TIMESTAMP, p_nguoiupdate
      FROM "PLAN_BCSX"
     WHERE masanpham IS NOT NULL AND makhuon IS NOT NULL
       AND okthucte > 0 AND cycletime > 0
       AND thoigian IN ('00-02','02-04','04-06','06-08','20-22','22-24')
       AND ngay BETWEEN p_startdate AND p_enddate
     GROUP BY masanpham, makhuon, ngay;

    -- Update cycletime vao tb_Shotting
    UPDATE "tb_Shotting" s
       SET "CycleTime" = t.cycletime
      FROM (SELECT masp, sokhuon, cycletime FROM "tb_Daily_NGRate"
             WHERE id IN (SELECT max(id) FROM "tb_Daily_NGRate" GROUP BY masp, sokhuon)) AS t
     WHERE s."Masp" = t.masp AND s."Sokhuon" = t.sokhuon AND t.cycletime > 0;
END;
$function$
```

---

## 189. `SP_UPDATE_SHOT_REAL_TIME`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SHOT_REAL_TIME"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_ShottingRealTime";

    INSERT INTO "tb_ShottingRealTime" ("May","Masp","Sokhuon",nguoicheck,ngaycheck,ketqua,"Hinhthuc","RankCheck")
    SELECT may, masp, dieno, nguoiktra, giolaymau, tinhtrang, hinhthuc, rank
      FROM "tb_listketquakiemtra"
     WHERE id IN (SELECT max(id) FROM "tb_listketquakiemtra"
                   WHERE masp IS NOT NULL AND length(may) >= 3 GROUP BY may)
       AND may IN (SELECT "Mayduc" FROM "IMM")
     ORDER BY may;

    -- Update gioi han shot theo rank
    UPDATE "tb_Shotting" SET "SoShotLimit" = (3*3600/"CycleTime")::int,
        "Canhbaovang" = (4*3600/"CycleTime" - 3600/"CycleTime")::int,
        "Canhbaodo"   = (4*3600/"CycleTime" + 3600/"CycleTime")::int,
        "TimeLimit"   = ((4*3600/"CycleTime" + 3600/"CycleTime") * "CycleTime")::int
    WHERE "Rank" = 'A' AND "CycleTime" > 0;

    UPDATE "tb_Shotting" SET "SoShotLimit" = (5*3600/"CycleTime")::int,
        "Canhbaovang" = (5*3600/"CycleTime" - 3600/"CycleTime")::int,
        "Canhbaodo"   = (5*3600/"CycleTime" + 3600/"CycleTime")::int,
        "TimeLimit"   = ((5*3600/"CycleTime" + 3600/"CycleTime") * "CycleTime")::int
    WHERE "Rank" = 'B' AND "CycleTime" > 0;

    UPDATE "tb_Shotting" SET "SoShotLimit" = (10*3600/"CycleTime")::int,
        "Canhbaovang" = (6*3600/"CycleTime" - 3600/"CycleTime")::int,
        "Canhbaodo"   = (6*3600/"CycleTime" + 3600/"CycleTime")::int,
        "TimeLimit"   = ((6*3600/"CycleTime" + 3600/"CycleTime") * "CycleTime")::int
    WHERE "Rank" = 'C' AND "CycleTime" > 0;

    UPDATE "tb_ShottingRealTime" r
       SET "CycleTime" = s."CycleTime",
           "SoShotLimit" = s."Canhbaodo",
           "TimeLimit"   = s."TimeLimit",
           "Canhbaovang" = s."Canhbaovang",
           "Canhbaodo"   = s."Canhbaodo",
           "RankShotting" = s."Rank"
      FROM "tb_Shotting" s
     WHERE r."Masp" = s."Masp" AND r."Sokhuon" = s."Sokhuon";
END;
$function$
```

---

## 190. `SP_UPDATE_SOSPNG`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SOSPNG"(p_masp character varying, p_sokhuon character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
DELETE FROM "tb_sospNG_View";
INSERT INTO "tb_sospNG_View"
SELECT masp, sokhuon,
       to_char(ngayupdate,'MM-YYYY') AS thoigian,
       sum(sospbo), sum("NGsilver"), sum("NGchamden"), sum("NGtapchat"), sum("NGdinhdau"),
       sum("NGloangnhua"), sum("NGautohand"), sum("NGshortmold"), sum("NGflowmask"), sum("NGchaykhi"),
       sum("NGsinkmask"), sum("NGketsp"), sum("Nhatmau"), sum("Xuoc"), sum(hakka), sum(jig), sum("Other"),
       (sum(sospbo)+sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
        sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
        sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other")),
       (sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
        sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
        sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"))
  FROM "tb_sospNG"
 WHERE masp=p_masp AND sokhuon=p_sokhuon
 GROUP BY masp, sokhuon, EXTRACT(YEAR FROM ngayupdate), EXTRACT(MONTH FROM ngayupdate)
 ORDER BY EXTRACT(YEAR FROM ngayupdate), EXTRACT(MONTH FROM ngayupdate);
END;
$function$
```

---

## 191. `SP_UPDATE_SOSPNG_ALL`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SOSPNG_ALL"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
DELETE FROM "tb_sospNG_View";
INSERT INTO "tb_sospNG_View"
SELECT NULL::varchar, NULL::varchar, to_char(ngayupdate,'MM-YYYY'),
       sum(sospbo), sum("NGsilver"), sum("NGchamden"), sum("NGtapchat"), sum("NGdinhdau"),
       sum("NGloangnhua"), sum("NGautohand"), sum("NGshortmold"), sum("NGflowmask"), sum("NGchaykhi"),
       sum("NGsinkmask"), sum("NGketsp"), sum("Nhatmau"), sum("Xuoc"), sum(hakka), sum(jig), sum("Other"),
       (sum(sospbo)+sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
        sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
        sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other")),
       (sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
        sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
        sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"))
  FROM "tb_sospNG"
 WHERE ngayupdate >= '2016-04-01'::timestamp
 GROUP BY EXTRACT(YEAR FROM ngayupdate), EXTRACT(MONTH FROM ngayupdate)
 ORDER BY EXTRACT(YEAR FROM ngayupdate), EXTRACT(MONTH FROM ngayupdate);
END;
$function$
```

---

## 192. `SP_UPDATE_SOSPNG_MAY`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SOSPNG_MAY"(p_may character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
DELETE FROM "tb_sospNG_View";
INSERT INTO "tb_sospNG_View"
SELECT NULL::varchar, NULL::varchar, to_char(ngayupdate,'MM-YYYY'),
       sum(sospbo), sum("NGsilver"), sum("NGchamden"), sum("NGtapchat"), sum("NGdinhdau"),
       sum("NGloangnhua"), sum("NGautohand"), sum("NGshortmold"), sum("NGflowmask"), sum("NGchaykhi"),
       sum("NGsinkmask"), sum("NGketsp"), sum("Nhatmau"), sum("Xuoc"), sum(hakka), sum(jig), sum("Other"),
       (sum(sospbo)+sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
        sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
        sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other")),
       (sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
        sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
        sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"))
  FROM "tb_sospNG"
 WHERE mayduc = p_may
 GROUP BY mayduc, EXTRACT(YEAR FROM ngayupdate), EXTRACT(MONTH FROM ngayupdate)
 ORDER BY EXTRACT(YEAR FROM ngayupdate), EXTRACT(MONTH FROM ngayupdate);
END;
$function$
```

---

## 193. `SP_UPDATE_SOSPNG_MAY_TODAY`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SOSPNG_MAY_TODAY"(p_datetime timestamp without time zone)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE v_date_start timestamp;
BEGIN
    v_date_start := (p_datetime::date || ' 08:00')::timestamp;
    DELETE FROM "tb_sospNG_May_View";
    INSERT INTO "tb_sospNG_May_View"
    SELECT NULL::varchar, NULL::varchar, mayduc,
           sum(sospbo), sum("NGsilver"), sum("NGchamden"), sum("NGtapchat"), sum("NGdinhdau"),
           sum("NGloangnhua"), sum("NGautohand"), sum("NGshortmold"), sum("NGflowmask"), sum("NGchaykhi"),
           sum("NGsinkmask"), sum("NGketsp"), sum("Nhatmau"), sum("Xuoc"), sum(hakka), sum(jig), sum("Other"),
           (sum(sospbo)+sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
            sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
            sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other")),
           (sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
            sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
            sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"))
      FROM "tb_sospNG"
     WHERE ngayupdate >= v_date_start AND ngayupdate <= v_date_start + INTERVAL '1 day'
     GROUP BY mayduc ORDER BY mayduc;
END;
$function$
```

---

## 194. `SP_UPDATE_SOSPNG_PART_TODAY`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SOSPNG_PART_TODAY"(p_datetime timestamp without time zone)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE v_date_start timestamp;
BEGIN
    v_date_start := (p_datetime::date || ' 08:00')::timestamp;
    DELETE FROM "tb_sospNG_Part_View";
    INSERT INTO "tb_sospNG_Part_View"
    SELECT NULL::varchar, NULL::varchar, masp,
           sum(sospbo), sum("NGsilver"), sum("NGchamden"), sum("NGtapchat"), sum("NGdinhdau"),
           sum("NGloangnhua"), sum("NGautohand"), sum("NGshortmold"), sum("NGflowmask"), sum("NGchaykhi"),
           sum("NGsinkmask"), sum("NGketsp"), sum("Nhatmau"), sum("Xuoc"), sum(hakka), sum(jig), sum("Other"),
           (sum(sospbo)+sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
            sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
            sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other")),
           (sum("NGsilver")+sum("NGchamden")+sum("NGtapchat")+sum("NGdinhdau")+sum("NGloangnhua")+
            sum("NGautohand")+sum("NGshortmold")+sum("NGflowmask")+sum("NGchaykhi")+sum("NGsinkmask")+
            sum("NGketsp")+sum("Nhatmau")+sum("Xuoc")+sum(hakka)+sum(jig)+sum("Other"))
      FROM "tb_sospNG"
     WHERE ngayupdate >= v_date_start AND ngayupdate <= v_date_start + INTERVAL '1 day'
     GROUP BY masp ORDER BY masp;
END;
$function$
```

---

## 195. `SP_UPDATE_SO_QUANLY_FA`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_SO_QUANLY_FA"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_QuanLyMauFA" q
       SET "Facurrent" = t.so_fa::integer
      FROM (SELECT part_no, die_no, so_fa FROM "FA-TVP"
             WHERE id IN (SELECT max(id) FROM "FA-TVP"
                           WHERE so_fa != '' AND so_fa IS NOT NULL
                           GROUP BY part_no, die_no)) AS t
     WHERE q.masp = t.part_no AND q.sokhuon = t.die_no;
END;
$function$
```

---

## 196. `SP_UPDATE_TIMELIMIT`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_TIMELIMIT"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_Shotting" s
       SET soshotlimit = r.sogio * 3600 / s."CycleTime"
      FROM "tb_RankHighImportant" r WHERE r.rank = s.rank;
    UPDATE "tb_Shotting" SET timelimit   = soshotlimit * cycletime;
    UPDATE "tb_Shotting" SET canhbaovang = soshotlimit - 3600 / cycletime;
    UPDATE "tb_Shotting" SET canhbaodo   = soshotlimit + 3600 / cycletime;
END;
$function$
```

---

## 197. `SP_UPDATE_TINH_TRANG_LINH_KIEN`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_TINH_TRANG_LINH_KIEN"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "PM_TinhTrangLinhKien" lk
       SET tuoithotieuchuan = t.tuoitho
      FROM "PM_TuoiThoTieuChuan" t
     WHERE lk.tenthietbi = t.tenthietbi AND lk.barcode = t.barcode;

    UPDATE "PM_TinhTrangLinhKien"
       SET songaydachay = CURRENT_DATE - ngaylap
     WHERE ngaylap IS NOT NULL AND tuoithotieuchuan IS NOT NULL;

    UPDATE "PM_TinhTrangLinhKien"
       SET songayconlai = tuoithotieuchuan - songaydachay,
           vitri_ten_linhkien = vitrilaplinhkien || '-' || barcode || ' ' || tenlinhkien
     WHERE songaydachay IS NOT NULL AND tuoithotieuchuan IS NOT NULL
       AND songaydachay > 0 AND tuoithotieuchuan > 0;
END;
$function$
```

---

## 198. `SP_UPDATE_YEARLY_ACTUAL`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_YEARLY_ACTUAL"(p_loaitb character varying, p_tentb character varying, p_loaibd character varying, p_date timestamp without time zone)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE v_month int := EXTRACT(MONTH FROM p_date);
        v_day   text := EXTRACT(DAY FROM p_date)::text;
BEGIN
    UPDATE "PM_YearlyPlan"
       SET "Jan_Actual" = CASE WHEN v_month=1  THEN v_day ELSE "Jan_Actual" END,
           "Feb_Actual" = CASE WHEN v_month=2  THEN v_day ELSE "Feb_Actual" END,
           "Mar_Actual" = CASE WHEN v_month=3  THEN v_day ELSE "Mar_Actual" END,
           "Apr_Actual" = CASE WHEN v_month=4  THEN v_day ELSE "Apr_Actual" END,
           "May_Actual" = CASE WHEN v_month=5  THEN v_day ELSE "May_Actual" END,
           "Jun_Actual" = CASE WHEN v_month=6  THEN v_day ELSE "Jun_Actual" END,
           "Jul_Actual" = CASE WHEN v_month=7  THEN v_day ELSE "Jul_Actual" END,
           "Aug_Actual" = CASE WHEN v_month=8  THEN v_day ELSE "Aug_Actual" END,
           "Sep_Actual" = CASE WHEN v_month=9  THEN v_day ELSE "Sep_Actual" END,
           "Oct_Actual" = CASE WHEN v_month=10 THEN v_day ELSE "Oct_Actual" END,
           "Nov_Actual" = CASE WHEN v_month=11 THEN v_day ELSE "Nov_Actual" END,
           "Dec_Actual" = CASE WHEN v_month=12 THEN v_day ELSE "Dec_Actual" END
     WHERE tenthietbi=p_tentb AND loaithietbi=p_loaitb AND loaibd=p_loaibd;
END;
$function$
```

---

## 199. `SP_UPDATE_YEARLY_PLAN`

```sql
CREATE OR REPLACE FUNCTION public."SP_UPDATE_YEARLY_PLAN"(p_multi integer, p_year integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE v_planned date;
BEGIN
    -- Tính ngày dự kiến cho từng hàng rồi gộp 12 UPDATE thành 1
    UPDATE "PM_YearlyPlan"
       SET "Jan_Plan" = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=1
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE "Jan_Plan" END,
           feb_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=2
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE feb_plan END,
           mar_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=3
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE mar_plan END,
           apr_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=4
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE apr_plan END,
           may_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=5
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE may_plan END,
           jun_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=6
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE jun_plan END,
           jul_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=7
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE jul_plan END,
           aug_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=8
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE aug_plan END,
           sep_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=9
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE sep_plan END,
           oct_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=10
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE oct_plan END,
           nov_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=11
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE nov_plan END,
           dec_plan = CASE WHEN EXTRACT(MONTH FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=12
                            AND EXTRACT(YEAR  FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)=p_year
                           THEN EXTRACT(DAY FROM ngaybaoduong + (p_multi*sothang*30||' days')::interval)::int ELSE dec_plan END;
END;
$function$
```

---

## 200. `SP_VIEW_LICHSUSUAKHUON`

```sql
CREATE OR REPLACE FUNCTION public."SP_VIEW_LICHSUSUAKHUON"(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(ngay date, ca character varying, part_no character varying, sokhuon character varying, loichinh character varying, chitietloi character varying, nguyennhanchinh character varying, phantich5why character varying, giaiphaptamthoi character varying, giaiphaplaudai character varying, picunderinvestigating character varying, anhhuongdenchatluong boolean, mota character varying)
 LANGUAGE sql
AS $function$
    SELECT
        ngay::date,
        ca,
        p_masp,
        sokhuon,
        loichinh::varchar,
        chitietloi,
        nguyennhanchinh,
        phantich5why,
        giaiphaptamthoi,
        giaiphaplaudai,
        picture,
        false,
        motaloi
    FROM "DTS_DieTrouble_NewVer"
    WHERE sokhuon = p_sokhuon
    ORDER BY ngay DESC;
$function$
```

---

## 201. `SP_VIEW_PM_HISTORY`

```sql
CREATE OR REPLACE FUNCTION public."SP_VIEW_PM_HISTORY"(p_may character varying)
 RETURNS TABLE(tenthietbi character varying, gio character varying, loaithietbi character varying, chitiet character varying, tensuco character varying, nguyennhan character varying, giaiphap character varying, pic character varying, anhhuongdenchatluong boolean, ngay date, mota character varying)
 LANGUAGE sql
AS $function$
    SELECT
        "Die_Name",
        caketthuc,
        loaibaoduong,
        chitiet,
        NULL::varchar,
        NULL::varchar,
        NULL::varchar,
        NULL::varchar,
        false,
        thoigianketthuc::date,
        NULL::varchar
    FROM "DTS_TimeMaintenance_NewVer"
    WHERE "Part_No" = p_may OR "Die_No" LIKE '%' || p_may || '%'
    ORDER BY thoigianketthuc DESC;
$function$
```

---

## 202. `_chart_index_block_base`

```sql
CREATE OR REPLACE FUNCTION public._chart_index_block_base(p_table text, p_thang text, p_nam text, p_loaimay text DEFAULT NULL::text)
 RETURNS TABLE(ngay text, dailymain double precision, noplan double precision, spng double precision, preparation double precision, okpart double precision, qckeep double precision, sucodie double precision, scchatluong double precision, adjust double precision, dandori double precision, dandoritrouble double precision, dungkhongghiloi double precision, sucomayduc double precision, sanphambo double precision, monthlymain double precision, other double precision, extratime double precision, scautomation double precision, ploss double precision, mloss double precision, extratimetotal double precision)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_loaimay_filter text := '';
    v_query text;
BEGIN
    IF p_loaimay IS NOT NULL THEN
        v_loaimay_filter := format(' AND loaimay = %L', p_loaimay);
    END IF;

    DROP TABLE IF EXISTS tmp_cb;
    CREATE TEMP TABLE tmp_cb (
        ngay text, dailymain float, noplan float, spng float,
        preparation float, okpart float, qckeep float, sucodie float,
        scchatluong float, adjust float, dandori float,
        dandoritrouble float, dungkhongghiloi float, sucomayduc float,
        sanphambo float, monthlymain float, other float,
        extratime float, scautomation float,
        ploss float, mloss float, extratimetotal float
    );

    v_query := format($q$
        INSERT INTO tmp_cb (ngay,dailymain,noplan,spng,preparation,okpart,qckeep,
            sucodie,scchatluong,adjust,dandori,dandoritrouble,dungkhongghiloi,
            sucomayduc,sanphambo,monthlymain,other,extratime,scautomation)
        SELECT ngay::text,
            COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng định kỳ'   THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='No Plan'              THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm NG'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Preparation'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm OK'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='SP QC Giữ'            THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố khuôn'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố chất lượng'     THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Điều chỉnh'           THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Dandori'              THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Dandori sự cố'        THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Dừng không ghi lỗi'  THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố máy đúc'        THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm bỏ'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng tháng'      THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Khác'                 THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Tăng ca'              THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố automation'     THEN thoigian END),0)
        FROM %I
        WHERE EXTRACT(MONTH FROM ngay)::int = %s::int
          AND EXTRACT(YEAR  FROM ngay)::int = %s::int %s
        GROUP BY ngay;

        -- Total row
        INSERT INTO tmp_cb (ngay,dailymain,noplan,spng,preparation,okpart,qckeep,
            sucodie,scchatluong,adjust,dandori,dandoritrouble,dungkhongghiloi,
            sucomayduc,sanphambo,monthlymain,other,extratime,scautomation)
        SELECT 'Total',
            COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng định kỳ'   THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='No Plan'              THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm NG'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Preparation'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm OK'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='SP QC Giữ'            THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố khuôn'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố chất lượng'     THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Điều chỉnh'           THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Dandori'              THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Dandori sự cố'        THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Dừng không ghi lỗi'  THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố máy đúc'        THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sản phẩm bỏ'          THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Bảo dưỡng tháng'      THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Khác'                 THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Tăng ca'              THEN thoigian END),0),
            COALESCE(SUM(CASE WHEN phanloai='Sự cố automation'     THEN thoigian END),0)
        FROM %I
        WHERE EXTRACT(MONTH FROM ngay)::int = %s::int
          AND EXTRACT(YEAR  FROM ngay)::int = %s::int %s;

        UPDATE tmp_cb SET
            ploss = COALESCE(spng,0)+COALESCE(sucodie,0)+COALESCE(sucomayduc,0)
                   +COALESCE(scchatluong,0)+COALESCE(scautomation,0)+COALESCE(dungkhongghiloi,0)
                   +COALESCE(dandori,0)+COALESCE(dandoritrouble,0)+COALESCE(dailymain,0)+COALESCE(preparation,0),
            mloss = COALESCE(monthlymain,0)+COALESCE(noplan,0)+COALESCE(other,0)+COALESCE(adjust,0),
            extratimetotal = COALESCE(extratime,0)+COALESCE(sanphambo,0)+COALESCE(qckeep,0);
    $q$, p_table, p_thang, p_nam, v_loaimay_filter,
         p_table, p_thang, p_nam, v_loaimay_filter);

    EXECUTE v_query;
    RETURN QUERY SELECT * FROM tmp_cb;
END;
$function$
```

---

## 203. `_chart_ratio_full`

```sql
CREATE OR REPLACE FUNCTION public._chart_ratio_full(p_ok double precision, p_ploss double precision, p_mloss double precision, p_sc double precision, p_mu double precision, p_slc double precision, p_da double precision, p_dm double precision, p_pr double precision)
 RETURNS double precision
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$
BEGIN
    RETURN CASE WHEN (p_ok+p_ploss+p_mloss)=0 THEN 0
                ELSE ROUND((p_ok/(p_ok+p_ploss+p_mloss))::numeric,3)::float END;
END;
$function$
```

---

## 204. `_cs_pivot`

```sql
CREATE OR REPLACE FUNCTION public._cs_pivot(p_idbaoduong integer, p_mode text)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text, c14 text, c15 text, c16 text, c17 text, c18 text, c19 text, c20 text, c21 text, c22 text, c23 text, c24 text, c25 text, c26 text, c27 text, c28 text, c29 text, c30 text, c31 text, c32 text, c33 text, c34 text, c35 text, c36 text, c37 text, c38 text, c39 text, c40 text, c41 text, c42 text, c43 text, c44 text, c45 text, c46 text, c47 text, c48 text, c49 text, c50 text, c51 text, c52 text, c53 text, c54 text, c55 text, c56 text, c57 text, c58 text, c59 text, c60 text, c61 text, c62 text, c63 text, c64 text, c65 text)
 LANGUAGE plpgsql
AS $function$
DECLARE
  v_val_expr text;
BEGIN
  v_val_expr := CASE p_mode
    WHEN 'NG'   THEN 'CASE WHEN danhgia=''NG''   THEN ''X'' ELSE '''' END'
    WHEN 'OK'   THEN 'CASE WHEN danhgia=''OK''   THEN ''X'' ELSE '''' END'
    WHEN 'TEMP' THEN 'CASE WHEN danhgia=''TEMP'' THEN ''X'' ELSE '''' END'
    WHEN 'PIC'  THEN 'nguoiupdate'
  END;
  RETURN QUERY EXECUTE format($q$
    SELECT %s FROM (
      SELECT machecksheet::int AS n, %s AS val
      FROM "DTS_ChiTietBaoDuong_CheckSheet" WHERE idlist = %s
    ) src
    GROUP BY true
    $q$,
    (SELECT string_agg(
       format('MAX(CASE WHEN n=%s THEN val END)', i),
       ',' ORDER BY i)
     FROM generate_series(1,65) AS i),
    v_val_expr,
    p_idbaoduong
  );
END; $function$
```

---

## 205. `_sc_pivot13`

```sql
CREATE OR REPLACE FUNCTION public._sc_pivot13(p_idsuco integer, p_mode text)
 RETURNS TABLE(c1 text, c2 text, c3 text, c4 text, c5 text, c6 text, c7 text, c8 text, c9 text, c10 text, c11 text, c12 text, c13 text)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_val_expr text;
BEGIN
    v_val_expr := CASE p_mode
        WHEN 'GHICHU' THEN 'ghichu'
        WHEN 'NG'     THEN 'CASE WHEN danhgia=''NG''   THEN ''X'' ELSE '''' END'
        WHEN 'OK'     THEN 'CASE WHEN danhgia=''OK''   THEN ''X'' ELSE '''' END'
        WHEN 'TEMP'   THEN 'CASE WHEN danhgia=''TEMP'' THEN ''X'' ELSE '''' END'
        WHEN 'PIC'    THEN 'nguoiupdate'
    END;
    RETURN QUERY EXECUTE format($q$
        SELECT
            MAX(CASE WHEN machecksheet::int = 1  THEN val END),
            MAX(CASE WHEN machecksheet::int = 2  THEN val END),
            MAX(CASE WHEN machecksheet::int = 3  THEN val END),
            MAX(CASE WHEN machecksheet::int = 4  THEN val END),
            MAX(CASE WHEN machecksheet::int = 5  THEN val END),
            MAX(CASE WHEN machecksheet::int = 6  THEN val END),
            MAX(CASE WHEN machecksheet::int = 7  THEN val END),
            MAX(CASE WHEN machecksheet::int = 8  THEN val END),
            MAX(CASE WHEN machecksheet::int = 9  THEN val END),
            MAX(CASE WHEN machecksheet::int = 10 THEN val END),
            MAX(CASE WHEN machecksheet::int = 11 THEN val END),
            MAX(CASE WHEN machecksheet::int = 12 THEN val END),
            MAX(CASE WHEN machecksheet::int = 13 THEN val END)
        FROM (
            SELECT machecksheet, (%s)::text AS val
            FROM "DTS_ChiTietSuaChua_CheckSheet"
            WHERE idlist = %s
        ) src
    $q$, v_val_expr, p_idsuco);
END;
$function$
```

---

## 206. `plan_sample_fa_show`

```sql
CREATE OR REPLACE FUNCTION public.plan_sample_fa_show(p_ngay date, p_thongkemay integer)
 RETURNS TABLE("MAY" character varying, "GIOPLAN" character varying, "KHUONLAP" character varying, "VITRIMAU" character varying)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_max_id        integer;
    v_id            integer := 1;
    v_mch_tk        integer := 0;
    v_vitrimau_may  varchar(50);
    v_khuonlap      varchar(50);
    v_phanloai      varchar(50);
    v_vitrimau_khac varchar(50);
BEGIN
    IF p_thongkemay = 0 THEN
        CREATE TEMP TABLE temp_thaykhuon (
            "ID"           serial NOT NULL,
            "ID_THAYKHUON" integer,
            "MAY"          varchar(50),
            "GIOPLAN"      varchar(50),
            "KHUONLAP"     varchar(50),
            "VITRIMAU"     varchar(100)
        ) ON COMMIT DROP;

        INSERT INTO temp_thaykhuon ("ID_THAYKHUON","MAY","GIOPLAN","KHUONLAP")
        SELECT id, may, gioplan, khuonlap FROM "PLAN_Dandory"
        WHERE ngay = p_ngay AND status <> 1
        ORDER BY stt, thutuhienthi desc;

        SELECT COUNT(*) INTO v_max_id FROM temp_thaykhuon;

        WHILE v_id <= v_max_id LOOP
            v_mch_tk := 0; v_vitrimau_may := NULL; v_khuonlap := NULL;
            v_phanloai := NULL; v_vitrimau_khac := NULL;

            SELECT "ID_THAYKHUON", "KHUONLAP"
            INTO v_mch_tk, v_khuonlap
            FROM temp_thaykhuon WHERE "ID" = v_id;

            SELECT "Phanloai" INTO v_phanloai FROM "tb_SampleFA"
            WHERE "Barcode" = v_khuonlap ORDER BY "Id" DESC LIMIT 1;

            IF v_phanloai = 'LẤY MẪU' THEN
                SELECT "Vitrimoi_may" INTO v_vitrimau_may FROM "tb_SampleFA"
                WHERE "Barcode" = v_khuonlap ORDER BY "Id" DESC LIMIT 1;
                SELECT "Vitrimoi_khac" INTO v_vitrimau_khac FROM "tb_SampleFA"
                WHERE "Barcode" = v_khuonlap ORDER BY "Id" DESC LIMIT 1;
                UPDATE temp_thaykhuon
                SET "VITRIMAU" = COALESCE(v_vitrimau_may,'') || '/' || COALESCE(v_vitrimau_khac,'')
                WHERE "ID_THAYKHUON" = v_mch_tk;
            ELSE
                SELECT "Sample_location" INTO v_vitrimau_khac FROM "tb_Part_master"
                WHERE "Sample_barcode" = v_khuonlap LIMIT 1;
                UPDATE temp_thaykhuon SET "VITRIMAU" = v_vitrimau_khac
                WHERE "ID_THAYKHUON" = v_mch_tk;
            END IF;
            v_id := v_id + 1;
        END LOOP;

        RETURN QUERY SELECT t."MAY", t."GIOPLAN", t."KHUONLAP", t."VITRIMAU"
                     FROM temp_thaykhuon t;
    ELSE
        UPDATE "IMM" SET "SampleFA" = '';
        UPDATE "IMM" SET "SampleFA" = a."Barcode"
        FROM (
            SELECT t."Vitrimoi_may", t."Barcode"
            FROM (
                SELECT "Vitrimoi_may", MAX("Ngayupdate") AS maxtime
                FROM "tb_SampleFA"
                WHERE "Vitrimoi_may" <> '' AND "Phanloai" = 'LẤY MẪU'
                GROUP BY "Vitrimoi_may"
            ) r
            INNER JOIN "tb_SampleFA" t
                ON t."Vitrimoi_may" = r."Vitrimoi_may"
               AND t."Ngayupdate" = r.maxtime
        ) a
        WHERE a."Vitrimoi_may" = "IMM"."Mayduc";
        RETURN;
    END IF;
END;
$function$
```

---

## 207. `sp_add_part_quality`

```sql
CREATE OR REPLACE FUNCTION public.sp_add_part_quality()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    INSERT INTO "tb_PartQuality" (masp, tensp, sokhuon)
    SELECT p."Part_no", p."Part_name", p."Die_no"
    FROM "tb_Part_master" p
    WHERE (p."Part_no" || p."Die_no") NOT IN
          (SELECT (q.masp || q.sokhuon) FROM "tb_PartQuality" q);

    UPDATE "tb_PartQuality" SET may = NULL;

    UPDATE "tb_PartQuality" q SET may = t.may
    FROM (
        SELECT may, masp, dieno FROM "tb_listketquakiemtra"
        WHERE id IN (SELECT MAX(id) FROM "tb_listketquakiemtra" GROUP BY may)
    ) t
    WHERE q.masp = t.masp AND q.sokhuon = t.dieno;

    UPDATE "tb_PartQuality" q
    SET "rankNgoaiQuan" = t.rank, "LastRun" = t.ngayktra
    FROM (
        SELECT masp, dieno, rank, ngayktra FROM "tb_listketquakiemtra"
        WHERE id IN (SELECT MAX(id) FROM "tb_listketquakiemtra" GROUP BY masp, dieno)
    ) t
    WHERE q.masp = t.masp AND q.sokhuon = t.dieno;
END;
$function$
```

---

## 208. `sp_check_gioi_han_kich_thuoc`

```sql
CREATE OR REPLACE FUNCTION public.sp_check_gioi_han_kich_thuoc()
 RETURNS TABLE(masp character varying, sokhuon character varying, items integer, vitri character varying, cavity integer, duplicate integer)
 LANGUAGE sql
AS $function$
    SELECT masp, sokhuon, items, vitri, cavity, COUNT(*)::integer AS duplicate
    FROM "tb_HangMucDoChiTietNewVer"
    GROUP BY masp, sokhuon, items, vitri, cavity
    HAVING COUNT(*) > 1
    ORDER BY COUNT(*) DESC;
$function$
```

---

## 209. `sp_check_hang_muc_do`

```sql
CREATE OR REPLACE FUNCTION public.sp_check_hang_muc_do()
 RETURNS TABLE(groupname character varying, items integer, vitri character varying, duplicate integer)
 LANGUAGE sql
AS $function$
    SELECT t.groupname, t.items, t.vitri, COUNT(*)::integer AS duplicate
    FROM "tb_HangMucDoNewVer" t
    GROUP BY t.groupname, t.items, t.vitri
    HAVING COUNT(*) > 1
    ORDER BY COUNT(*) DESC;
$function$
```

---

## 210. `sp_check_hang_muc_do_fa`

```sql
CREATE OR REPLACE FUNCTION public.sp_check_hang_muc_do_fa()
 RETURNS TABLE(groupname character varying, items integer, vitri character varying, gioihanduoifa double precision, gioihanduoidrw double precision, gioihantrendrw double precision, gioihantrenfa double precision, finallower double precision, finalupper double precision, loi text)
 LANGUAGE sql
AS $function$
SELECT * FROM (
    SELECT p.groupname, p.items, p.vitri,
           v.gioihanduoifa, p.gioihanduoidrw, p.gioihantrendrw,
           v.gioihantrenfa, v.finallower, v.finalupper,
           'GIỚI HẠN DƯỚI FA > DRW' AS loi
    FROM "tb_HangMucDoNewVer" p
    JOIN "tb_HangMucDoChiTietNewVer" v ON v.items=p.items AND v.vitri=p.vitri AND v.groupname=p.groupname
    WHERE v.gioihanduoifa <> 1000 AND v.gioihanduoifa > p.gioihanduoidrw
    UNION
    SELECT p.groupname, p.items, p.vitri,
           v.gioihanduoifa, p.gioihanduoidrw, p.gioihantrendrw,
           v.gioihantrenfa, v.finallower, v.finalupper,
           'GIỚI HẠN TRÊN DRW > FA' AS loi
    FROM "tb_HangMucDoNewVer" p
    JOIN "tb_HangMucDoChiTietNewVer" v ON v.items=p.items AND v.vitri=p.vitri AND v.groupname=p.groupname
    WHERE v.gioihantrenfa <> 1000 AND v.gioihantrenfa < p.gioihantrendrw
) t ORDER BY t.groupname;
$function$
```

---

## 211. `sp_check_hang_muc_do_mp`

```sql
CREATE OR REPLACE FUNCTION public.sp_check_hang_muc_do_mp()
 RETURNS TABLE(groupname character varying, items integer, vitri character varying, gioihanduoimp double precision, gioihanduoidrw double precision, gioihantrendrw double precision, gioihantrenmp double precision, loi text)
 LANGUAGE sql
AS $function$
SELECT t.groupname, t.items, t.vitri,
       t.gioihanduoimp, t.gioihanduoidrw, t.gioihantrendrw, t.gioihantrenmp,
       'GIỚI HẠN TRÊN DRW > MP' AS loi
FROM "tb_HangMucDoNewVer" t
WHERE t.gioihantrenmp <> 1000 AND t.gioihantrendrw > t.gioihantrenmp
UNION
SELECT t.groupname, t.items, t.vitri,
       t.gioihanduoimp, t.gioihanduoidrw, t.gioihantrendrw, t.gioihantrenmp,
       'GIỚI HẠN DƯỚI MP > DRW' AS loi
FROM "tb_HangMucDoNewVer" t
WHERE t.gioihanduoimp <> 1000 AND t.gioihanduoidrw < t.gioihanduoimp
UNION
SELECT DISTINCT ch.groupname, ch.items, ch.vitri,
       gr.gioihanduoimp, ch.gioihanduoifa, ch.gioihantrenfa, gr.gioihantrenmp,
       'GIỚI HẠN DƯỚI MP > FA' AS loi
FROM "tb_HangMucDoChiTietNewVer" ch
JOIN "tb_HangMucDoNewVer" gr ON ch.groupname=gr.groupname AND ch.items=gr.items AND ch.vitri=gr.vitri
WHERE gr.gioihanduoimp <> 1000 AND ch.gioihanduoifa <> 1000 AND ch.gioihanduoifa < gr.gioihanduoimp
UNION
SELECT DISTINCT ch.groupname, ch.items, ch.vitri,
       gr.gioihanduoimp, ch.gioihanduoifa, ch.gioihantrenfa, gr.gioihantrenmp,
       'GIỚI HẠN TRÊN MP < FA' AS loi
FROM "tb_HangMucDoChiTietNewVer" ch
JOIN "tb_HangMucDoNewVer" gr ON ch.groupname=gr.groupname AND ch.items=gr.items AND ch.vitri=gr.vitri
WHERE ch.gioihantrenfa <> 1000 AND gr.gioihantrenmp <> 1000 AND ch.gioihantrenfa > gr.gioihantrenmp;
$function$
```

---

## 212. `sp_follow_ng_rate_by_realtimeandlot`

```sql
CREATE OR REPLACE FUNCTION public.sp_follow_ng_rate_by_realtimeandlot()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_FollowByLot";

    INSERT INTO "tb_FollowByLot"
        (idkhoidong, may, masp, sokhuon, tensp, may_masp_khuon, giolaymaukhoidong)
    SELECT l.id, l.may, l.masp, l.dieno, l.tensp,
           l.may || '(' || l.masp || '-' || l.dieno || ')', l.ngayktra
    FROM tb_listketquakiemtra l
    WHERE l.id IN (
        SELECT MAX(id) FROM tb_listketquakiemtra
        WHERE hinhthuc = 'Khuôn lắp/ Khởi động (FS)' GROUP BY may
    ) ORDER BY l.may;

    UPDATE "tb_FollowByLot" f
    SET idhakhuon = t.id, giolaymauhakhuon = t.ngayktra
    FROM (
        SELECT l.id, l.may, l.masp, l.dieno, l.tensp, l.ngayktra
        FROM tb_listketquakiemtra l
        WHERE l.id IN (
            SELECT MAX(id) FROM tb_listketquakiemtra
            WHERE hinhthuc = 'Khuôn hạ/ Dừng máy (LS)' GROUP BY may
        )
    ) t
    WHERE t.id > f.idkhoidong
      AND f.may_masp_khuon = t.may || '(' || t.masp || '-' || t.dieno || ')';

    UPDATE "tb_FollowByLot" f SET cavity = p."Cavity"
    FROM "tb_Part_master" p WHERE f.masp = p."Part_no";

    UPDATE "tb_FollowByLot" f SET cycletime = s."CycleTime"
    FROM "tb_Shotting" s WHERE s."Masp" = f.masp AND s."Sokhuon" = f.sokhuon;

    UPDATE "tb_FollowByLot" SET cycletime = 30 WHERE cycletime IS NULL;
    UPDATE "tb_FollowByLot" SET giolaymauhakhuon = NOW() WHERE giolaymauhakhuon IS NULL;

    UPDATE "tb_FollowByLot"
    SET timediff  = EXTRACT(EPOCH FROM (giolaymauhakhuon - giolaymaukhoidong))::double precision,
        totalshot = (EXTRACT(EPOCH FROM (giolaymauhakhuon - giolaymaukhoidong)) / NULLIF(cycletime,0))::integer
    WHERE cycletime > 0 AND cycletime IS NOT NULL;

    UPDATE "tb_FollowByLot" f
    SET silver=t.ngsilver, chamden=t.ngchamden, tapchat=t.ngtapchat,
        dinhdau=t.ngdinhdau, loangnhua=t.ngloangnhua, autohand=t.ngautohand,
        shortmold=t.ngshortmold, flowmask=t.ngflowmask, chaykhi=t.ngchaykhi,
        sinkmask=t.ngsinkmask, ketsp=t.ngketsp, nhatmau=t.nhatmau,
        xuoc=t.xuoc, hakka=t.hakka, jig=t.jig, other=t.other, totalng=t.totalng
    FROM (
        SELECT COALESCE(SUM(a."NGsilver"),0)::double precision AS ngsilver,
               COALESCE(SUM(a."NGchamden"),0)::double precision AS ngchamden,
               COALESCE(SUM(a."NGtapchat"),0)::double precision AS ngtapchat,
               COALESCE(SUM(a."NGdinhdau"),0)::double precision AS ngdinhdau,
               COALESCE(SUM(a."NGloangnhua"),0)::double precision AS ngloangnhua,
               COALESCE(SUM(a."NGautohand"),0)::double precision AS ngautohand,
               COALESCE(SUM(a."NGshortmold"),0)::double precision AS ngshortmold,
               COALESCE(SUM(a."NGflowmask"),0)::double precision AS ngflowmask,
               COALESCE(SUM(a."NGchaykhi"),0)::double precision AS ngchaykhi,
               COALESCE(SUM(a."NGsinkmask"),0)::double precision AS ngsinkmask,
               COALESCE(SUM(a."NGketsp"),0)::double precision AS ngketsp,
               COALESCE(SUM(a."Nhatmau"),0)::double precision AS nhatmau,
               COALESCE(SUM(a."Xuoc"),0)::double precision AS xuoc,
               COALESCE(SUM(a.hakka),0)::double precision AS hakka,
               COALESCE(SUM(a.jig),0)::double precision AS jig,
               COALESCE(SUM(a."Other"),0)::double precision AS other,
               (COALESCE(SUM(a."NGsilver"),0)+COALESCE(SUM(a."NGchamden"),0)+
                COALESCE(SUM(a."NGtapchat"),0)+COALESCE(SUM(a."NGdinhdau"),0)+
                COALESCE(SUM(a."NGloangnhua"),0)+COALESCE(SUM(a."NGautohand"),0)+
                COALESCE(SUM(a."NGshortmold"),0)+COALESCE(SUM(a."NGflowmask"),0)+
                COALESCE(SUM(a."NGchaykhi"),0)+COALESCE(SUM(a."NGsinkmask"),0)+
                COALESCE(SUM(a."NGketsp"),0)+COALESCE(SUM(a."Nhatmau"),0)+
                COALESCE(SUM(a."Xuoc"),0)+COALESCE(SUM(a.hakka),0)+
                COALESCE(SUM(a.jig),0)+COALESCE(SUM(a."Other"),0))::integer AS totalng,
               a.mayduc, a.masp, a.sokhuon
        FROM "tb_sospNG" a
        JOIN "tb_FollowByLot" b ON a.masp=b.masp AND a.sokhuon=b.sokhuon AND a.mayduc=b.may
        WHERE a.idlistkqktracopy >= b.idkhoidong
        GROUP BY a.mayduc, a.masp, a.sokhuon
    ) t
    WHERE t.masp=f.masp AND t.sokhuon=f.sokhuon AND t.mayduc=f.may;

    UPDATE "tb_FollowByLot" f
    SET silver=ROUND((f.silver::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        chamden=ROUND((f.chamden::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        tapchat=ROUND((f.tapchat::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        dinhdau=ROUND((f.dinhdau::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        loangnhua=ROUND((f.loangnhua::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        autohand=ROUND((f.autohand::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        shortmold=ROUND((f.shortmold::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        flowmask=ROUND((f.flowmask::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        chaykhi=ROUND((f.chaykhi::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        sinkmask=ROUND((f.sinkmask::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        ketsp=ROUND((f.ketsp::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        nhatmau=ROUND((f.nhatmau::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        xuoc=ROUND((f.xuoc::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        hakka=ROUND((f.hakka::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        jig=ROUND((f.jig::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision,
        other=ROUND((f.other::numeric/NULLIF(f.totalshot,0)::numeric/NULLIF(f.cavity,0)::numeric*100),2)::double precision
    WHERE f.totalshot IS NOT NULL AND f.totalshot <> 0
      AND f.cavity IS NOT NULL AND f.cavity <> 0;

    UPDATE "tb_FollowByLot" f
    SET ngrate = COALESCE(f.silver,0)+COALESCE(f.chamden,0)+COALESCE(f.tapchat,0)+
                 COALESCE(f.dinhdau,0)+COALESCE(f.loangnhua,0)+COALESCE(f.autohand,0)+
                 COALESCE(f.shortmold,0)+COALESCE(f.flowmask,0)+COALESCE(f.chaykhi,0)+
                 COALESCE(f.sinkmask,0)+COALESCE(f.ketsp,0)+COALESCE(f.nhatmau,0)+
                 COALESCE(f.xuoc,0)+COALESCE(f.hakka,0)+COALESCE(f.jig,0)+COALESCE(f.other,0);

    UPDATE "tb_FollowByLot" f
    SET ngrate = ROUND(f.ngrate::numeric, 2)::double precision
    WHERE f.ngrate IS NOT NULL;
END;
$function$
```

---

## 213. `sp_get_check_sheet`

```sql
CREATE OR REPLACE FUNCTION public.sp_get_check_sheet(p_masp character varying)
 RETURNS SETOF "Check_Sheet"
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT * FROM public."Check_Sheet"
    WHERE "Part_no" = p_masp;
END;
$function$
```

---

## 214. `sp_get_chi_tiet_kiem_tra`

```sql
CREATE OR REPLACE FUNCTION public.sp_get_chi_tiet_kiem_tra(p_idchitiet integer)
 RETURNS SETOF tb_chitietketqua_newver
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT * FROM public.tb_chitietketqua_newver
    WHERE idlistketquakiemtra = p_idchitiet
    ORDER BY cavity, hangmuccheck, rank, vitri asc;
END;
$function$
```

---

## 215. `sp_get_cyctimefromtvp_shotting`

```sql
CREATE OR REPLACE FUNCTION public.sp_get_cyctimefromtvp_shotting()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    WITH added_row_number AS (
        SELECT "Part_no", "Die_no", "Ngay_gui", "Cycle_time",
               ROW_NUMBER() OVER (PARTITION BY "Part_no","Die_no" ORDER BY "Ngay_gui" DESC) AS row_number
        FROM "FA-TVP" WHERE "Ketqua" = 'OK'
    ),
    latest_rows AS (SELECT * FROM added_row_number WHERE row_number = 1)
    UPDATE "tb_Shotting" t
    SET "CycleTime" = l."Cycle_time"
    FROM latest_rows l
    WHERE l."Part_no" = t."Masp" AND l."Die_no" = t."Sokhuon";

    UPDATE "tb_Shotting" SET "CycleTime" = 30 WHERE "CycleTime" = 0;
END;
$function$
```

---

## 216. `sp_get_danh_sach_kiem_tra`

```sql
CREATE OR REPLACE FUNCTION public.sp_get_danh_sach_kiem_tra(p_idchitiet integer)
 RETURNS SETOF tb_listketquakiemtra
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT * FROM public.tb_listketquakiemtra
    WHERE id = p_idchitiet;
END;
$function$
```

---

## 217. `sp_get_issue_do`

```sql
CREATE OR REPLACE FUNCTION public.sp_get_issue_do(p_may character varying, p_top integer)
 RETURNS SETOF "tb_IssueDo"
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_may = 'ALL' THEN
        RETURN QUERY
            SELECT ID, MAY, MASP, KHUON, HINHTHUC, GIOLAYMAU,
                   GIOKIEMTRA, NGUOIKIEMTRA, IDKIEMTRA,
                   "isIssue", COMMENT, "isCancel", LYDO
            FROM "tb_IssueDo"
            WHERE IDKIEMTRA IN (SELECT MAX(IDKIEMTRA) FROM "tb_IssueDo" GROUP BY MAY)
            ORDER BY MAY;
    ELSE
        RETURN QUERY
            SELECT ID, MAY, MASP, KHUON, HINHTHUC, GIOLAYMAU,
                   GIOKIEMTRA, NGUOIKIEMTRA, IDKIEMTRA,
                   "isIssue", COMMENT, "isCancel", LYDO
            FROM "tb_IssueDo"
            WHERE IDKIEMTRA NOT IN (SELECT MAX(IDKIEMTRA) FROM "tb_IssueDo" WHERE MAY = p_may)
              AND MAY = p_may
            ORDER BY IDKIEMTRA
            LIMIT p_top;
    END IF;
END;
$function$
```

---

## 218. `sp_get_part_material_info`

```sql
CREATE OR REPLACE FUNCTION public.sp_get_part_material_info(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE("Part_no" character varying, "Part_name" character varying, "Die_no" character varying, "Nhacungcap" character varying, "NhaSX" text, "Tennhua" character varying, "Loainhua" character varying, "Mamau" character varying, "Maunhua" character varying, "Model" character varying, "Certified" character varying, "TaiSD_ACTUAL" character varying, "Taisudung_phantram" character varying, anhvetkhacnhua bytea, "CHANGINGPOINT" text)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT
        P."Part_no", P."Part_name", P."Die_no",
        M."Nhacungcap", M."NhaSX",
        M."Tennhua", M."Loainhua", M."Mamau", M."Maunhua",
        P."Model", M."Certified", P."TaiSD_ACTUAL",
        M."Taisudung_phantram",
        NULL::bytea,
        M.changingpoint
    FROM "tb_Part_master" P
    JOIN "Material" M ON P."Part_no" = M."Part_no"
    WHERE P."Part_no" = p_masp AND P."Die_no" = p_sokhuon;
END;
$function$
```

---

## 219. `sp_get_tool_measure`

```sql
CREATE OR REPLACE FUNCTION public.sp_get_tool_measure(p_masp character varying, p_sokhuon character varying, p_hinhthuc character varying)
 RETURNS TABLE(dungcudo character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF p_hinhthuc IN (
        N'SỰ CỐ', N'BẢO DƯỠNG', N'TRIAL FA',
        N'CHECK NHANH KHỞI ĐỘNG', N'CHẠY LẠI (SAU SỰ CỐ - DỪNG MÁY)'
    ) THEN
        IF EXISTS (
            SELECT 1 FROM "tb_HangMucDoNewVer" A
            JOIN "tb_HangMucDoChiTietNewVer" B ON A.groupname = B.groupname
            WHERE B.masp = p_masp AND B.sokhuon = p_sokhuon AND A.donhanh = true
        ) THEN
            RETURN QUERY
                SELECT DISTINCT A.dungcudo FROM "tb_HangMucDoNewVer" A
                JOIN "tb_HangMucDoChiTietNewVer" B ON A.groupname = B.groupname
                WHERE B.masp = p_masp AND B.sokhuon = p_sokhuon
                  AND A.donhanh = true AND A.dungcudo NOT IN ('SHAFT');
        ELSE
            RETURN QUERY
                SELECT DISTINCT A.dungcudo FROM "tb_HangMucDoNewVer" A
                JOIN "tb_HangMucDoChiTietNewVer" B ON A.groupname = B.groupname
                WHERE B.masp = p_masp AND B.sokhuon = p_sokhuon
                  AND faonly = false AND A.dungcudo NOT IN ('SHAFT');
        END IF;
    ELSIF p_hinhthuc = N'FA' THEN
        RETURN QUERY
            SELECT DISTINCT A.dungcudo FROM "tb_HangMucDoNewVer" A
            JOIN "tb_HangMucDoChiTietNewVer" B ON A.groupname = B.groupname
            WHERE B.masp = p_masp AND B.sokhuon = p_sokhuon
              AND A.dungcudo NOT IN ('SHAFT');
    ELSE
        RETURN QUERY
            SELECT DISTINCT A.dungcudo FROM "tb_HangMucDoNewVer" A
            JOIN "tb_HangMucDoChiTietNewVer" B ON A.groupname = B.groupname
            WHERE B.masp = p_masp AND B.sokhuon = p_sokhuon
              AND A.dungcudo NOT IN ('SHAFT') AND faonly = false;
    END IF;
END;
$function$
```

---

## 220. `sp_insert_demension_data_supplier`

```sql
CREATE OR REPLACE FUNCTION public.sp_insert_demension_data_supplier(p_idlist integer, p_masp character varying, p_sokhuon character varying, p_cavity integer)
 RETURNS TABLE(item integer, ktdn double precision, dungsai character varying, cavityno character varying, "position" character varying, tool character varying, data1 double precision, judge character varying, lower double precision, upper double precision, spectrslower double precision, spectrsupper double precision, trangbv character varying, loaikichthuoc character varying, diffgh double precision)
 LANGUAGE plpgsql
AS $function$
DECLARE v_groupname VARCHAR(50);
BEGIN
    SELECT GROUPPART INTO v_groupname
    FROM "tb_Part_master"
    WHERE "Part_no" = p_masp AND "Die_no" = p_sokhuon;

    RETURN QUERY
    WITH ins AS (
        INSERT INTO "tb_Report_DimensionDataForSupplier"
            (item, ktdn, dungsai, cavityno, position, tool, data1, judge,
             "lower", "upper", "spectRSlower", "spectRSupper",
             trangbv, loaikichthuoc, diffgh)
        SELECT
            CTD.items,
            HMD.ktdng,
            ('+' || HMD.sailechtren::text || '/-' || HMD.sailechduoi::text)::character varying,
            p_cavity::character varying,
            CTD.vitri,
            CTD.dungcudo,
            CTD.ketquado,
            CTD.ketquadanhgia,
            CTD.gioihanduoidrw,
            CTD.gioihantrendrw,
            CTD.gioihanduoifa,
            CTD.gioihantrenfa,
            HMD.trangbanve,
            HMD.loaikt,
            CTD.diffgh
        FROM TB_CHITIETKQDO CTD
        JOIN "tb_HangMucDoNewVer" HMD
            ON CTD.items = HMD.items AND CTD.vitri = HMD.vitri
        WHERE CTD.idlistketquado = p_idlist
          AND HMD.groupname = v_groupname
        RETURNING
            "tb_Report_DimensionDataForSupplier".item,
            "tb_Report_DimensionDataForSupplier".ktdn,
            "tb_Report_DimensionDataForSupplier".dungsai,
            "tb_Report_DimensionDataForSupplier".cavityno,
            "tb_Report_DimensionDataForSupplier".position,
            "tb_Report_DimensionDataForSupplier".tool,
            "tb_Report_DimensionDataForSupplier".data1,
            "tb_Report_DimensionDataForSupplier".judge,
            "tb_Report_DimensionDataForSupplier"."lower",
            "tb_Report_DimensionDataForSupplier"."upper",
            "tb_Report_DimensionDataForSupplier"."spectRSlower",
            "tb_Report_DimensionDataForSupplier"."spectRSupper",
            "tb_Report_DimensionDataForSupplier".trangbv,
            "tb_Report_DimensionDataForSupplier".loaikichthuoc,
            "tb_Report_DimensionDataForSupplier".diffgh
    )
    SELECT ins.item, ins.ktdn, ins.dungsai, ins.cavityno, ins.position,
           ins.tool, ins.data1, ins.judge,
           ins."lower", ins."upper",
           ins."spectRSlower", ins."spectRSupper",
           ins.trangbv, ins.loaikichthuoc, ins.diffgh
    FROM ins
    ORDER BY ins.tool, ins.item::text, ins.position;
END; $function$
```

---

## 221. `sp_insert_demension_data_supplier2`

```sql
CREATE OR REPLACE FUNCTION public.sp_insert_demension_data_supplier2(p_idlist integer, p_masp character varying, p_sokhuon character varying, p_cavity integer)
 RETURNS TABLE(item integer, ktdn double precision, dungsai character varying, cavityno character varying, "position" character varying, tool character varying, data1 double precision, judge character varying, lower double precision, upper double precision, spectrslower double precision, spectrsupper double precision, trangbv character varying, loaikichthuoc character varying, diffgh double precision, diffghfa double precision)
 LANGUAGE plpgsql
AS $function$
DECLARE v_groupname VARCHAR(50);
BEGIN
    SELECT GROUPPART INTO v_groupname
    FROM "tb_Part_master"
    WHERE "Part_no" = p_masp AND "Die_no" = p_sokhuon;

    RETURN QUERY
    WITH ins AS (
        INSERT INTO "tb_Report_DimensionDataForSupplier"
            (item, ktdn, dungsai, cavityno, position, tool, data1, judge,
             "lower", "upper", "spectRSlower", "spectRSupper",
             trangbv, loaikichthuoc, diffgh, diffghfa)
        SELECT
            CTD.items,
            HMD.ktdng,
            ('+' || HMD.sailechtren::text || '/-' || HMD.sailechduoi::text)::character varying,
            p_cavity::character varying,
            CTD.vitri,
            CTD.dungcudo,
            CTD.ketquado,
            CTD.ketquadanhgia,
            CTD.gioihanduoidrw,
            CTD.gioihantrendrw,
            CTD.gioihanduoifa,
            CTD.gioihantrenfa,
            HMD.trangbanve,
            HMD.loaikt,
            CTD.diffgh,
            CTD.diffghfa
        FROM TB_CHITIETKQDO CTD
        JOIN "tb_HangMucDoNewVer" HMD
            ON CTD.items = HMD.items AND CTD.vitri = HMD.vitri
        WHERE CTD.idlistketquado = p_idlist
          AND HMD.groupname = v_groupname
        RETURNING
            "tb_Report_DimensionDataForSupplier".item,
            "tb_Report_DimensionDataForSupplier".ktdn,
            "tb_Report_DimensionDataForSupplier".dungsai,
            "tb_Report_DimensionDataForSupplier".cavityno,
            "tb_Report_DimensionDataForSupplier".position,
            "tb_Report_DimensionDataForSupplier".tool,
            "tb_Report_DimensionDataForSupplier".data1,
            "tb_Report_DimensionDataForSupplier".judge,
            "tb_Report_DimensionDataForSupplier"."lower",
            "tb_Report_DimensionDataForSupplier"."upper",
            "tb_Report_DimensionDataForSupplier"."spectRSlower",
            "tb_Report_DimensionDataForSupplier"."spectRSupper",
            "tb_Report_DimensionDataForSupplier".trangbv,
            "tb_Report_DimensionDataForSupplier".loaikichthuoc,
            "tb_Report_DimensionDataForSupplier".diffgh,
            "tb_Report_DimensionDataForSupplier".diffghfa
    )
    SELECT ins.item, ins.ktdn, ins.dungsai, ins.cavityno, ins.position,
           ins.tool, ins.data1, ins.judge,
           ins."lower", ins."upper",
           ins."spectRSlower", ins."spectRSupper",
           ins.trangbv, ins.loaikichthuoc, ins.diffgh, ins.diffghfa
    FROM ins
    ORDER BY ins.tool, ins.item::text, ins.position;
END; $function$
```

---

## 222. `sp_insert_dimension_data`

```sql
CREATE OR REPLACE FUNCTION public.sp_insert_dimension_data(p_idlist integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_Issue_Dimention_Data_Temp";
    INSERT INTO "tb_Issue_Dimention_Data_Temp"
    SELECT id, nguoiktra, ngayktra, casx, mayduc, masp, tensp, sokhuon,
           hinhthuc, items, vitri, dungcudo, gioihantren, gioihanduoi,
           gioihantrendrw, gioihanduoidrw, gioihantrenfa, gioihanduoifa,
           gioihantrenmp, gioihanduoimp, hinhanh, ketquado, ketquadanhgia,
           comment, idlistketquado, cavity, congthuc,
           shot1, shot2, shot3, shot4, shot5, stt,
           "ketquadanhgiaMP", deltamiddrw, xuhuong, canhbaotanggiam,
           canhbaosailech, ngtype, groupname,
           gioihantrenfathamkhao, gioihanduoifathamkhao,
           danhgiafathamkhao, loaikt, showpe
    FROM tb_chitietkqdo
    WHERE tb_chitietkqdo.idlistketquado = p_idlist;
END;
$function$
```

---

## 223. `sp_rank_appearance`

```sql
CREATE OR REPLACE FUNCTION public.sp_rank_appearance(isrunning boolean)
 RETURNS TABLE(rankngoaiquan character varying, quantity bigint)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF isrunning = false THEN
        RETURN QUERY
        SELECT t."rankNgoaiQuan", COUNT(*) AS quantity
        FROM "tb_PartQuality" t WHERE t."rankNgoaiQuan" IS NOT NULL
        GROUP BY t."rankNgoaiQuan";
    ELSE
        RETURN QUERY
        SELECT t."rankNgoaiQuan", COUNT(*) AS quantity
        FROM "tb_PartQuality" t
        WHERE t."rankNgoaiQuan" IS NOT NULL AND t.may IS NOT NULL
        GROUP BY t."rankNgoaiQuan";
    END IF;
END;
$function$
```

---

## 224. `sp_rank_combine`

```sql
CREATE OR REPLACE FUNCTION public.sp_rank_combine(isrunning boolean)
 RETURNS TABLE(rank text, quantity bigint)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF isrunning = false THEN
        RETURN QUERY
        SELECT t."rankNgoaiQuan" || '-' || t."rankKichThuoc" AS rank, COUNT(*) AS quantity
        FROM "tb_PartQuality" t
        WHERE t."rankNgoaiQuan" IS NOT NULL AND t."rankKichThuoc" IS NOT NULL
        GROUP BY t."rankNgoaiQuan" || '-' || t."rankKichThuoc";
    ELSE
        RETURN QUERY
        SELECT t."rankNgoaiQuan" || '-' || t."rankKichThuoc" AS rank, COUNT(*) AS quantity
        FROM "tb_PartQuality" t
        WHERE t."rankNgoaiQuan" IS NOT NULL AND t."rankKichThuoc" IS NOT NULL AND t.may IS NOT NULL
        GROUP BY t."rankNgoaiQuan" || '-' || t."rankKichThuoc";
    END IF;
END;
$function$
```

---

## 225. `sp_rank_measurement`

```sql
CREATE OR REPLACE FUNCTION public.sp_rank_measurement(isrunning boolean)
 RETURNS TABLE("rankKichThuoc" character varying, quantity bigint)
 LANGUAGE plpgsql
AS $function$
BEGIN
    IF isrunning = false THEN
        RETURN QUERY
        SELECT t."rankKichThuoc", COUNT(*) AS quantity
        FROM "tb_PartQuality" t WHERE t."rankNgoaiQuan" IS NOT NULL
        GROUP BY t."rankKichThuoc";
    ELSE
        RETURN QUERY
        SELECT t."rankKichThuoc", COUNT(*) AS quantity
        FROM "tb_PartQuality" t
        WHERE t."rankNgoaiQuan" IS NOT NULL AND t.may IS NOT NULL
        GROUP BY t."rankKichThuoc";
    END IF;
END;
$function$
```

---

## 226. `sp_rank_update_measurement_rank_quality`

```sql
CREATE OR REPLACE FUNCTION public.sp_rank_update_measurement_rank_quality()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_PartQuality"
    SET may=NULL, "A"=0, "B"=0, "C"=0, "D"=0, "S"=0, "LastUpdate"=NOW();

    UPDATE "tb_PartQuality" q SET may = t.may
    FROM (
        SELECT may, masp, dieno FROM "tb_listketquakiemtra"
        WHERE id IN (SELECT MAX(id) FROM "tb_listketquakiemtra" GROUP BY may)
    ) t
    WHERE q.masp = t.masp AND q.sokhuon = t.dieno;

    UPDATE "tb_PartQuality" q
    SET "A"=COALESCE(t.a,0), "B"=COALESCE(t.b,0), "C"=COALESCE(t.c,0),
        "D"=COALESCE(t.d,0), "S"=COALESCE(t.s,0)
    FROM (
        SELECT masp, sokhuon,
               COUNT(*) FILTER (WHERE rank='A') AS a,
               COUNT(*) FILTER (WHERE rank='B') AS b,
               COUNT(*) FILTER (WHERE rank='C') AS c,
               COUNT(*) FILTER (WHERE rank='D') AS d,
               COUNT(*) FILTER (WHERE rank='S') AS s
        FROM "tb_PartAnalysis"
        GROUP BY masp, sokhuon
    ) t
    WHERE q.masp = t.masp AND q.sokhuon = t.sokhuon;
END;
$function$
```

---

## 227. `sp_sospng_update_ngaythucte`

```sql
CREATE OR REPLACE FUNCTION public.sp_sospng_update_ngaythucte()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE public."tb_sospNG"
    SET ngaythucte = ( ngayupdate- INTERVAL '1 day')::date
    WHERE EXTRACT(HOUR FROM ngayupdate) < 8
      AND ngaythucte IS NULL;

    UPDATE public."tb_sospNG"
    SET ngaythucte = ngayupdate::date
    WHERE EXTRACT(HOUR FROM ngayupdate) >= 8
      AND ngaythucte IS NULL;

    UPDATE public."tb_sospNG"
    SET cathucte = 'NGÀY'
    WHERE EXTRACT(HOUR FROM ngayupdate) >= 8
      AND EXTRACT(HOUR FROM ngayupdate) < 20
      AND cathucte IS NULL;

    UPDATE public."tb_sospNG"
    SET cathucte = 'ĐÊM'
    WHERE cathucte IS NULL;

    UPDATE public."tb_sospNG" s
    SET loaimay = p."LoaiMay"
    FROM public."PM_EquipmentInfo" p
    WHERE s.loaimay IS NULL
      AND p."TenThietBi" = s.mayduc;

    UPDATE public."tb_sospNG" s
    SET totalstock = t.totalstockok
    FROM (
        SELECT SUM(totalshotok) AS totalstockok, ca, ngay
        FROM public."tb_Daily_NGRate"
        GROUP BY ca, ngay
    ) t
    WHERE s.cathucte = t.ca
      AND s.ngaythucte = t.ngay;

    UPDATE public."tb_sospNG" s
    SET totalstock = totalstock + t.totalng
    FROM (
        SELECT (
            SUM("NGsilver") + SUM("NGchamden") + SUM("NGtapchat") + SUM("NGdinhdau") +
            SUM("NGloangnhua") + SUM("NGautohand") + SUM("NGshortmold") + SUM("NGflowmask") +
            SUM("NGchaykhi") + SUM("NGsinkmask") + SUM("NGketsp") + SUM("Nhatmau") +
            SUM("Xuoc") + SUM(hakka) + SUM(jig) + SUM("Other") + SUM(sospbo)
        ) AS totalng,
        cathucte,
        ngaythucte
        FROM public."tb_sospNG"
        GROUP BY ngaythucte, cathucte
    ) t
    WHERE s.cathucte = t.cathucte
      AND s.ngaythucte = t.ngaythucte;

    UPDATE public."tb_sospNG" s
    SET stock = (
        d.totalshotok +
        s."NGsilver" + s."NGchamden" + s."NGtapchat" + s."NGdinhdau" +
        s."NGloangnhua" + s."NGautohand" + s."NGshortmold" + s."NGflowmask" +
        s."NGchaykhi" + s."NGsinkmask" + s."NGketsp" + s."Nhatmau" +
        s."Xuoc" + s.hakka + s.jig + s."Other" + s.sospbo
    )
    FROM public."tb_Daily_NGRate" d
    WHERE s.cathucte = d.ca
      AND s.ngaythucte = d.ngay
      AND s.masp = d.masp
      AND s.sokhuon = d.sokhuon;
END;
$function$
```

---

## 228. `sp_update_fa_tham_khao`

```sql
CREATE OR REPLACE FUNCTION public.sp_update_fa_tham_khao()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_HangMucDoNewVer"
    SET gioihanduoifa = T.GIOIHANDUOIFA
    FROM (
        SELECT groupname, items, vitri, MIN(gioihanduoifa) AS GIOIHANDUOIFA
        FROM "tb_HangMucDoChiTietNewVer"
        WHERE gioihanduoifa != 1000
        GROUP BY groupname, items, vitri
    ) AS T
    WHERE "tb_HangMucDoNewVer".groupname = T.groupname
      AND "tb_HangMucDoNewVer".items = T.items
      AND "tb_HangMucDoNewVer".vitri = T.vitri;

    UPDATE "tb_HangMucDoNewVer"
    SET gioihantrenfa = T.GIOIHANTRENFA
    FROM (
        SELECT groupname, items, vitri, MAX(gioihantrenfa) AS GIOIHANTRENFA
        FROM "tb_HangMucDoChiTietNewVer"
        WHERE gioihantrenfa != 1000
        GROUP BY groupname, items, vitri
    ) AS T
    WHERE "tb_HangMucDoNewVer".groupname = T.groupname
      AND "tb_HangMucDoNewVer".items = T.items
      AND "tb_HangMucDoNewVer".vitri = T.vitri;

    UPDATE "tb_HangMucDoNewVer" t SET gioihanduoifa = 1000
    WHERE NOT EXISTS (
        SELECT 1 FROM "tb_HangMucDoChiTietNewVer" c
        WHERE c.groupname = t.groupname AND c.vitri = t.vitri AND c.items = t.items
          AND c.gioihanduoifa != 1000
        GROUP BY c.groupname, c.items, c.vitri
        HAVING MIN(c.gioihanduoifa) != 1000
    );

    UPDATE "tb_HangMucDoNewVer" t SET gioihantrenfa = 1000
    WHERE NOT EXISTS (
        SELECT 1 FROM "tb_HangMucDoChiTietNewVer" c
        WHERE c.groupname = t.groupname AND c.vitri = t.vitri AND c.items = t.items
          AND c.gioihanduoifa != 1000
        GROUP BY c.groupname, c.items, c.vitri
        HAVING MIN(c.gioihantrenfa) != 1000
    );
END;
$function$
```

---

## 229. `sp_update_masanpham_nhua`

```sql
CREATE OR REPLACE FUNCTION public.sp_update_masanpham_nhua()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "Material"
    SET "Nhacungcap" = P.nhacungcap, "Loainhua" = P.loainhua,
        "Mamau" = P.mamau, "Maunhua" = P.maunhua, "Tennhua" = P.tennhua
    FROM "PRO_MATERIAL_INFO" P
    WHERE "Material".idnhua = P.id;

    UPDATE "tb_Part_master"
    SET "TaiSD_DRW" = M."Taisudung_phantram"
    FROM "Material" M
    WHERE "tb_Part_master"."Part_no" = M."Part_no";
END;
$function$
```

---

## 230. `sp_update_shot_real_time`

```sql
CREATE OR REPLACE FUNCTION public.sp_update_shot_real_time()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    DELETE FROM "tb_ShottingRealTime";

    INSERT INTO "tb_ShottingRealTime"
        ("May","Masp","Sokhuon","nguoicheck","ngaycheck","ketqua","Hinhthuc","RankCheck")
    SELECT l."may", l."masp", l."dieno", l."nguoiktra",
           l."giolaymau", l."tinhtrang", l."hinhthuc", l."rank"
    FROM "tb_listketquakiemtra" l
    WHERE l."id" IN (
        SELECT MAX(t."id") FROM "tb_listketquakiemtra" t
        WHERE t."masp" IS NOT NULL AND length(t."may") >= 3
        GROUP BY t."may"
    )
    AND l."may" IN (SELECT i."Mayduc" FROM "IMM" i);

    UPDATE "tb_Shotting"
    SET "SoShotLimit" = 3*3600/NULLIF("CycleTime",0),
        "Canhbaovang" = 4*3600/NULLIF("CycleTime",0) - 3600/NULLIF("CycleTime",0),
        "Canhbaodo"   = 4*3600/NULLIF("CycleTime",0) + 3600/NULLIF("CycleTime",0),
        "TimeLimit"   = (4*3600/NULLIF("CycleTime",0)+3600/NULLIF("CycleTime",0))*"CycleTime"
    WHERE "Rank" = 'A';

    UPDATE "tb_Shotting"
    SET "SoShotLimit" = 5*3600/NULLIF("CycleTime",0),
        "Canhbaovang" = 5*3600/NULLIF("CycleTime",0) - 3600/NULLIF("CycleTime",0),
        "Canhbaodo"   = 5*3600/NULLIF("CycleTime",0) + 3600/NULLIF("CycleTime",0),
        "TimeLimit"   = (5*3600/NULLIF("CycleTime",0)+3600/NULLIF("CycleTime",0))*"CycleTime"
    WHERE "Rank" = 'B';

    UPDATE "tb_Shotting"
    SET "SoShotLimit" = 10*3600/NULLIF("CycleTime",0),
        "Canhbaovang" = 6*3600/NULLIF("CycleTime",0) - 3600/NULLIF("CycleTime",0),
        "Canhbaodo"   = 6*3600/NULLIF("CycleTime",0) + 3600/NULLIF("CycleTime",0),
        "TimeLimit"   = (6*3600/NULLIF("CycleTime",0)+3600/NULLIF("CycleTime",0))*"CycleTime"
    WHERE "Rank" = 'C';

    UPDATE "tb_Shotting" SET "TimeLimit" = "Canhbaodo"*"CycleTime"::integer WHERE "CycleTime" <> 0;

    UPDATE "tb_ShottingRealTime" rt
    SET "CycleTime"    = s."CycleTime",
        "SoShotLimit"  = s."Canhbaodo",
        "TimeLimit"    = s."TimeLimit",
        "Canhbaovang"  = s."Canhbaovang",
        "Canhbaodo"    = s."Canhbaodo",
        "RankShotting" = s."Rank"
    FROM "tb_Shotting" s
    WHERE rt."Masp" = s."Masp" AND rt."Sokhuon" = s."Sokhuon";
END;
$function$
```

---

## 231. `sp_update_so_quanly_fa`

```sql
CREATE OR REPLACE FUNCTION public.sp_update_so_quanly_fa()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_QuanLyMauFA" A
    SET "Facurrent" = T."So_FA"::integer
    FROM (
        SELECT "Id", "Part_no", "Die_no", "So_FA" FROM "FA-TVP"
        WHERE "Id" IN (
            SELECT MAX("Id") FROM "FA-TVP"
            WHERE "So_FA" != '' AND "So_FA" IS NOT NULL
            GROUP BY "Part_no", "Die_no"
        )
    ) AS T
    WHERE A.MASP = T."Part_no" AND A.SOKHUON = T."Die_no";
END;
$function$
```

---

## 232. `sp_update_timelimit`

```sql
CREATE OR REPLACE FUNCTION public.sp_update_timelimit()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE "tb_Shotting"
    SET "SoShotLimit" = R.SOGIO * 3600 / "CycleTime"
    FROM "tb_RankHighImportant" R WHERE R.rank = "tb_Shotting"."Rank";

    UPDATE "tb_Shotting" SET "TimeLimit" = "SoShotLimit" * "CycleTime";
    UPDATE "tb_Shotting" SET "Canhbaovang" = "SoShotLimit" - 3600 / "CycleTime";
    UPDATE "tb_Shotting" SET "Canhbaodo" = "SoShotLimit" + 3600 / "CycleTime";
END;
$function$
```

---

## 233. `sp_view_lichsukhuon`

```sql
CREATE OR REPLACE FUNCTION public.sp_view_lichsukhuon(p_masp character varying, p_sokhuon character varying)
 RETURNS TABLE(ngay date, ca character varying, "Part_no" character varying, sokhuon character varying, loichinh character varying, chitietloi character varying, nguyennhanchinh character varying, phantich5why character varying, giaiphaptamthoi character varying, giaiphaplaudai character varying, picunderinvestigating character varying, anhhuongdenchatluong boolean, mota character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT T.ngay, T.ca, M."Part_no", T.sokhuon, T.loichinh, T.chitietloi,
           T.nguyennhanchinh, T.phantich5why, T.giaiphaptamthoi,
           T.giaiphaplaudai, T.picunderinvestigating,
           T.anhhuongdenchatluong, T.mota
    FROM "DTS_DieTrouble" T, "DTS_Die_Master" M
    WHERE T.tenkhuon = M."Part_name"
      AND T.sokhuon = M."Die_no"
      AND M."Part_no" = p_masp
      AND T.sokhuon = p_sokhuon
    ORDER BY T.ngay DESC;
END;
$function$
```

---

## 234. `sp_view_pm_history`

```sql
CREATE OR REPLACE FUNCTION public.sp_view_pm_history(p_may character varying)
 RETURNS SETOF "PM_SuCoTrongNgay"
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT * FROM "PM_SuCoTrongNgay"
    WHERE vitri = p_may;
END;
$function$
```

---

