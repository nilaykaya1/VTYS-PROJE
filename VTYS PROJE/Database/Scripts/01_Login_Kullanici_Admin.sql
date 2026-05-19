/*
  LezzetJet - Kullanici ve Admin Giris Tablolari / Ornek Hesaplar
  Veritabani: master (mevcut projenizle ayni)
  SSMS'te bu dosyayi acip F5 ile calistirin.
  Mevcut tablolara DOKUNMAZ; sadece admins tablosunu ekler ve ornek kayitlar ekler.
*/

USE [master];
GO

/* ============================================================
   1) ADMIN TABLOSU (Yonetici girisi)
   ============================================================ */
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

    CREATE INDEX IX_admins_active ON dbo.admins (is_active);
END
GO

/* ============================================================
   2) ORNEK ADMIN HESAPLARI
   Sifreler duz metin (proje su an customers ile ayni mantikta karsilastiriyor)
   Admin:     admin@lezzetjet.com     / Admin123!
   Super:     superadmin@lezzetjet.com / SuperAdmin123!
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.admins WHERE email = 'admin@lezzetjet.com')
BEGIN
    INSERT INTO dbo.admins (full_name, email, password_hash, role, is_active)
    VALUES (N'LezzetJet Admin', N'admin@lezzetjet.com', N'Admin123!', N'ADMIN', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.admins WHERE email = 'superadmin@lezzetjet.com')
BEGIN
    INSERT INTO dbo.admins (full_name, email, password_hash, role, is_active)
    VALUES (N'Sistem Yoneticisi', N'superadmin@lezzetjet.com', N'SuperAdmin123!', N'SUPER_ADMIN', 1);
END
GO

/* ============================================================
   3) ORNEK MUSTERI (KULLANICI) GIRIS HESAPLARI
   customers tablosu zaten var; sadece yoksa ekler.
   Kullanici:  kullanici@lezzetjet.com  / Kullanici123!
   Kullanici2: demo@lezzetjet.com       / Demo123!
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.customers WHERE email = 'kullanici@lezzetjet.com')
BEGIN
    INSERT INTO dbo.customers
        (full_name, email, phone, password_hash, is_beneficiary_verified, is_active, created_at)
    VALUES
        (N'Demo Kullanici', N'kullanici@lezzetjet.com', N'5551112233', N'Kullanici123!', 0, 1, SYSDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.customers WHERE email = 'demo@lezzetjet.com')
BEGIN
    INSERT INTO dbo.customers
        (full_name, email, phone, password_hash, is_beneficiary_verified, is_active, created_at)
    VALUES
        (N'Demo Musteri', N'demo@lezzetjet.com', N'5554445566', N'Demo123!', 0, 1, SYSDATETIME());
END
GO

/* ============================================================
   4) KONTROL SORGULARI
   ============================================================ */
SELECT 'admins' AS tablo, admin_id, full_name, email, role, is_active FROM dbo.admins;
SELECT 'customers (giris icin)' AS tablo, customer_id, full_name, email, is_active
FROM dbo.customers
WHERE email IN ('kullanici@lezzetjet.com', 'demo@lezzetjet.com');
GO
