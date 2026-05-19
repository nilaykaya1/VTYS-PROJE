/*
  ============================================================
  LezzetJet - PUANLAMA VE YORUM TABLOSU
  Tablo adi: dbo.restaurant_reviews
  Veritabani: master  (appsettings.json ile ayni olmali)
  ============================================================
  SSMS: Bu dosyayi acin -> F5 (Calistir)
  Sonra: master -> Tablolar -> dbo.restaurant_reviews
  ============================================================
*/

USE [master];
GO

/* Tablo var mi kontrol */
IF OBJECT_ID(N'dbo.restaurant_reviews', N'U') IS NOT NULL
    PRINT N'restaurant_reviews tablosu zaten var.';
ELSE
    PRINT N'restaurant_reviews tablosu olusturuluyor...';
GO

/* ========== TABLO ========== */
IF OBJECT_ID(N'dbo.restaurant_reviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.restaurant_reviews
    (
        review_id       BIGINT IDENTITY(1,1) NOT NULL,
        restaurant_id   BIGINT NOT NULL,
        customer_id     BIGINT NOT NULL,
        rating          TINYINT NOT NULL,          /* 1 ile 5 arasi puan */
        comment         VARCHAR(1000) NOT NULL,  /* yorum metni */
        is_active       BIT NOT NULL
            CONSTRAINT DF_restaurant_reviews_is_active DEFAULT (1),
        created_at      DATETIME2(0) NOT NULL
            CONSTRAINT DF_restaurant_reviews_created_at DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_restaurant_reviews PRIMARY KEY (review_id),
        CONSTRAINT CK_restaurant_reviews_rating CHECK (rating BETWEEN 1 AND 5),
        CONSTRAINT UQ_restaurant_reviews_customer_restaurant
            UNIQUE (restaurant_id, customer_id),
        CONSTRAINT FK_restaurant_reviews_restaurant
            FOREIGN KEY (restaurant_id) REFERENCES dbo.restaurants (restaurant_id),
        CONSTRAINT FK_restaurant_reviews_customer
            FOREIGN KEY (customer_id) REFERENCES dbo.customers (customer_id)
    );

    CREATE INDEX IX_restaurant_reviews_restaurant
        ON dbo.restaurant_reviews (restaurant_id, is_active);

    CREATE INDEX IX_restaurant_reviews_created
        ON dbo.restaurant_reviews (created_at DESC);

    PRINT N'Tablo olusturuldu: dbo.restaurant_reviews';
END
GO

/* ========== TRIGGER: Restoran ortalama puani ========== */
IF OBJECT_ID(N'dbo.trg_restaurant_reviews_update_rating', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_restaurant_reviews_update_rating;
GO

CREATE TRIGGER dbo.trg_restaurant_reviews_update_rating
ON dbo.restaurant_reviews
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH affected AS (
        SELECT restaurant_id FROM inserted
        UNION
        SELECT restaurant_id FROM deleted
    )
    UPDATE r
    SET r.rating = ISNULL((
        SELECT CAST(ROUND(AVG(CAST(rv.rating AS DECIMAL(3,1))), 1) AS DECIMAL(2,1))
        FROM dbo.restaurant_reviews rv
        WHERE rv.restaurant_id = r.restaurant_id AND rv.is_active = 1
    ), 0)
    FROM dbo.restaurants r
    INNER JOIN affected a ON a.restaurant_id = r.restaurant_id;
END
GO

/* ========== ORNEK YORUM (istege bagli) ========== */
DECLARE @cid BIGINT = (SELECT TOP 1 customer_id FROM dbo.customers WHERE is_active = 1 ORDER BY customer_id);
DECLARE @rid BIGINT = (SELECT TOP 1 restaurant_id FROM dbo.restaurants WHERE is_active = 1 ORDER BY restaurant_id);

IF @cid IS NOT NULL AND @rid IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM dbo.restaurant_reviews
       WHERE restaurant_id = @rid AND customer_id = @cid
   )
BEGIN
    INSERT INTO dbo.restaurant_reviews (restaurant_id, customer_id, rating, comment, is_active)
    VALUES (@rid, @cid, 5, N'Cok guzel yemekler, tavsiye ederim.', 1);
    PRINT N'Ornek yorum eklendi.';
END
GO

/* ========== KONTROL - Bu sonucu gormelisiniz ========== */
SELECT
    TABLE_SCHEMA AS sema,
    TABLE_NAME AS tablo_adi
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'restaurant_reviews';
GO

SELECT
    review_id AS yorum_no,
    restaurant_id AS restoran_no,
    customer_id AS musteri_no,
    rating AS puan,
    comment AS yorum,
    is_active AS aktif,
    created_at AS tarih
FROM dbo.restaurant_reviews
ORDER BY created_at DESC;
GO
