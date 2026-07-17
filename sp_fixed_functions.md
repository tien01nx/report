# Functions đã sửa — IssueReport SP Migration

## 1. `sp_get_part_material_info`

**Lỗi gốc**: `column m.anhvetkhacnhua does not exist` + kiểu dữ liệu không khớp  
**Fix**: Dùng `NULL::bytea` thay `anhvetkhacnhua`, sửa return type `NhaSX` và `CHANGINGPOINT` thành `text`

```sql
DROP FUNCTION IF EXISTS sp_get_part_material_info(character varying, character varying);

CREATE OR REPLACE FUNCTION sp_get_part_material_info(
    p_masp    character varying,
    p_sokhuon character varying
)
RETURNS TABLE(
    "Part_no"            character varying,
    "Part_name"          character varying,
    "Die_no"             character varying,
    "Nhacungcap"         character varying,
    "NhaSX"              text,              -- text, không phải varchar
    "Tennhua"            character varying,
    "Loainhua"           character varying,
    "Mamau"              character varying,
    "Maunhua"            character varying,
    "Model"              character varying,
    "Certified"          character varying,
    "TaiSD_ACTUAL"       character varying,
    "Taisudung_phantram" character varying,
    anhvetkhacnhua       bytea,             -- NULL::bytea (cột này không tồn tại trong DB)
    "CHANGINGPOINT"      text               -- text, không phải varchar
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT
        P."Part_no",
        P."Part_name",
        P."Die_no",
        M."Nhacungcap",
        M."NhaSX",
        M."Tennhua",
        M."Loainhua",
        M."Mamau",
        M."Maunhua",
        P."Model",
        M."Certified",
        P."TaiSD_ACTUAL",
        M."Taisudung_phantram",
        NULL::bytea,          -- bảng Material không có cột anhvetkhacnhua, dùng NULL
        M.changingpoint
    FROM "tb_Part_master" P
    JOIN "Material" M ON P."Part_no" = M."Part_no"
    WHERE P."Part_no" = p_masp
      AND P."Die_no"  = p_sokhuon;
END;
$$;
```

---

## 2. `sp_insert_demension_data_supplier`

**Lỗi gốc 1**: `column reference "item" is ambiguous` (PostgreSQL 42702)  
**Lỗi gốc 2**: `column "item" is of type integer but expression is of type text` (PostgreSQL 42804)  
**Fix**: Dùng CTE với `RETURNING` qualify rõ tên bảng; giữ đúng kiểu `integer` cho cột `item`

```sql
DROP FUNCTION IF EXISTS sp_insert_demension_data_supplier(integer, character varying, character varying, integer);

CREATE OR REPLACE FUNCTION sp_insert_demension_data_supplier(
    p_idlist  integer,
    p_masp    character varying,
    p_sokhuon character varying,
    p_cavity  integer
)
RETURNS TABLE(
    item          integer,            -- integer, không phải text
    ktdn          double precision,
    dungsai       character varying,
    cavityno      character varying,
    "position"    character varying,
    tool          character varying,
    data1         double precision,
    judge         character varying,
    lower         double precision,
    upper         double precision,
    spectrslower  double precision,
    spectrsupper  double precision,
    trangbv       character varying,
    loaikichthuoc character varying,
    diffgh        double precision
)
LANGUAGE plpgsql AS $$
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
            CTD.items,                                                              -- integer, không cast ::text
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
        -- Qualify rõ tên bảng để tránh ambiguous
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
END;
$$;
```

---

## 3. `sp_insert_demension_data_supplier2`

**Lỗi**: Giống `supplier` ở trên, thêm cột `diffghfa`

```sql
DROP FUNCTION IF EXISTS sp_insert_demension_data_supplier2(integer, character varying, character varying, integer);

CREATE OR REPLACE FUNCTION sp_insert_demension_data_supplier2(
    p_idlist  integer,
    p_masp    character varying,
    p_sokhuon character varying,
    p_cavity  integer
)
RETURNS TABLE(
    item          integer,
    ktdn          double precision,
    dungsai       character varying,
    cavityno      character varying,
    "position"    character varying,
    tool          character varying,
    data1         double precision,
    judge         character varying,
    lower         double precision,
    upper         double precision,
    spectrslower  double precision,
    spectrsupper  double precision,
    trangbv       character varying,
    loaikichthuoc character varying,
    diffgh        double precision,
    diffghfa      double precision   -- thêm so với supplier1
)
LANGUAGE plpgsql AS $$
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
            CTD.vitri, CTD.dungcudo, CTD.ketquado, CTD.ketquadanhgia,
            CTD.gioihanduoidrw, CTD.gioihantrendrw,
            CTD.gioihanduoifa, CTD.gioihantrenfa,
            HMD.trangbanve, HMD.loaikt,
            CTD.diffgh,
            CTD.diffghfa            -- thêm cột này
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
END;
$$;
```

---

## Ghi chú — Schema bảng `tb_Report_DimensionDataForSupplier`

| Cột | Kiểu thực tế |
|---|---|
| `id` | integer (NOT NULL) |
| `item` | **integer** |
| `ktdn` | double precision |
| `dungsai` | character varying(50) |
| `cavityno` | character varying(50) |
| `position` | character varying(50) |
| `tool` | character varying(50) |
| `data1` | double precision |
| `judge` | character varying(50) |
| `lower` | double precision |
| `upper` | double precision |
| `spectRSlower` | double precision |
| `spectRSupper` | double precision |
| `trangbv` | character varying(50) |
| `loaikichthuoc` | character varying(50) |
| `diffgh` | double precision |
| `diffghfa` | double precision |
