/*
  LezzetJet - Kayit Ol (Kullanici + Yonetici) veritabani yapisi
  Veritabani: master
  SSMS'te F5 ile calistirin.
  Mevcut veriyi silmez; tablo/kolon kontrolu ve ornek kayit sablonlari.
*/

USE [master];
GO

/* ---- admins (yonetici) ---- */
IF OBJECT_ID(N'dbo.admins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.admins
    (
        admin_id      BIGINT IDENTITY(1,1) NOT NULL,
        full_name     VARCHAR(120) NOT NULL,
        email         VARCHAR(150) NOT NULL,
        password_hash VARCHAR(255) NOT NULL,
        role          VARCHAR(30) NOT NULL
            CONSTRAINT DF_admins_role DEFAULT ('ADMIN'),
        is_active     BIT NOT NULL
            CONSTRAINT DF_admins_is_active DEFAULT (1),
        created_at    DATETIME2(0) NOT NULL
            CONSTRAINT DF_admins_created_at DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_admins PRIMARY KEY (admin_id),
        CONSTRAINT UQ_admins_email UNIQUE (email)
    );
END
GO

/* password_hash uzunlugu (hash icin 255 yeterli) */
IF COL_LENGTH('dbo.admins', 'password_hash') IS NOT NULL
BEGIN
    ALTER TABLE dbo.admins ALTER COLUMN password_hash VARCHAR(255) NOT NULL;
END
GO

IF COL_LENGTH('dbo.customers', 'password_hash') IS NOT NULL
BEGIN
    ALTER TABLE dbo.customers ALTER COLUMN password_hash VARCHAR(255) NOT NULL;
END
GO

/* ---- Manuel kayit ornekleri (uygulama disi test icin - duz sifre) ---- */
/*
INSERT INTO dbo.customers (full_name, email, phone, password_hash, is_beneficiary_verified, is_active, created_at)
VALUES (N'Yeni Kullanici', N'yeni@lezzetjet.com', N'5559998877', N'YeniKullanici1!', 0, 1, SYSDATETIME());

INSERT INTO dbo.admins (full_name, email, password_hash, role, is_active)
VALUES (N'Yeni Yonetici', N'yonetici@lezzetjet.com', N'Yonetici123!', N'ADMIN', 1);
*/

/* ---- Kayit kontrol sorgulari ---- */
SELECT COUNT(*) AS aktif_musteri FROM dbo.customers WHERE is_active = 1;
SELECT COUNT(*) AS aktif_yonetici FROM dbo.admins WHERE is_active = 1;
GO
