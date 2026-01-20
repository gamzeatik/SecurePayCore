# SecurePay API

Güvenli para transferi işlemleri için geliştirilmiş RESTful API.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              SecurePay API                                  │
│                            (.NET 10 Web API)                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐    ┌─────────────────┐    ┌─────────────────────────┐    │
│   │ Controllers │───▶│   Interfaces    │◀───│     Repositories        │    │
│   │             │    │                 │    │   (Dapper + Oracle)     │    │
│   │ - Account   │    │ - IAccount      │    │   - AccountRepository   │    │
│   │             │    │   Repository    │    │                         │    │
│   └─────────────┘    └─────────────────┘    └───────────┬─────────────┘    │
│          │                                              │                   │
│          ▼                                              ▼                   │
│   ┌─────────────┐                            ┌─────────────────────────┐    │
│   │ Middleware  │                            │    Stored Procedures    │    │
│   │ - Exception │                            │    - SP_TRANSFER_MONEY  │    │
│   │   Handling  │                            │    - SP_CREATE_ACCOUNT  │    │
│   └─────────────┘                            │    - SP_UPDATE_ACCOUNT  │    │
│                                              │    - SP_DELETE_ACCOUNT  │    │
│   ┌─────────────┐    ┌─────────────────┐    │    - SP_UPDATE_BALANCE  │    │
│   │   Models    │    │      DTOs       │    └───────────┬─────────────┘    │
│   │ - Account   │    │ - TransferReq   │                │                   │
│   │ - Transaction│   │                 │                │                   │
│   │ - ApiResponse│   │                 │                │                   │
│   └─────────────┘    └─────────────────┘                │                   │
│                                                         │                   │
└─────────────────────────────────────────────────────────┼───────────────────┘
                                                          │
                                                          ▼
                                              ┌─────────────────────────┐
                                              │      Oracle XE 21c      │
                                              │      (Docker)           │
                                              │                         │
                                              │   ┌─────────────────┐   │
                                              │   │    ACCOUNTS     │   │
                                              │   │    Table        │   │
                                              │   └─────────────────┘   │
                                              │   ┌─────────────────┐   │
                                              │   │  TRANSACTIONS   │   │
                                              │   │    Table        │   │
                                              │   └─────────────────┘   │
                                              └─────────────────────────┘
```

### Teknoloji Stack

| Katman | Teknoloji |
|--------|-----------|
| **API Framework** | .NET 10 (ASP.NET Core Web API) |
| **ORM** | Dapper (Micro ORM) |
| **Database** | Oracle XE 21c |
| **Container** | Docker |
| **API Docs** | Swagger / OpenAPI |
| **Frontend** | HTML5 / CSS3 / Vanilla JS |

---

## Web UI

Proje, para transferi işlemleri için modern bir web arayüzü içerir.

**URL:** http://localhost:5022

### Özellikler

- **Glassmorphism Tasarım:** Modern, şeffaf cam efektli arayüz
- **3D Coin Animasyonu:** Başarılı transferlerde dönen TL madeni para efekti
- **Sparkle Efektleri:** Parıltı animasyonları
- **Responsive:** Mobil uyumlu tasarım

### Ekran Görüntüsü

```
<img width="1826" height="1155" alt="image" src="https://github.com/user-attachments/assets/ded74e89-0743-45fb-ab44-451d723212be" />
```

---

## Setup

### 1. Oracle XE Docker Kurulumu

```bash
# Oracle XE 21c container'ını çalıştır
docker run -d \
  --name oracle-xe \
  -p 1521:1521 \
  -p 5500:5500 \
  -e ORACLE_PASSWORD=YourPassword123 \
  -e ORACLE_CHARACTERSET=AL32UTF8 \
  -v oracle-data:/opt/oracle/oradata \
  gvenzl/oracle-xe:21-slim

# Container durumunu kontrol et
docker ps

# Container loglarını izle (başlatma tamamlanana kadar bekle)
docker logs -f oracle-xe
```

### 2. appsettings.json Yapılandırması

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "OracleDbConnection": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User Id=SYSTEM;Password=YourPassword123;"
  }
}
```

### 3. Projeyi Çalıştırma

```bash
# Projeyi klonla
git clone <repo-url>
cd SecurePayCore/SecurePay.Api

# Bağımlılıkları yükle
dotnet restore

# Uygulamayı çalıştır
dotnet run
```

### 4. API Erişimi

- **Swagger UI:** http://localhost:5022/swagger
- **API Base URL:** http://localhost:5022/api

---

## Database Schema

### ACCOUNTS Tablosu

```sql
CREATE TABLE ACCOUNTS (
    AccountId NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    CustomerName VARCHAR2(100) NOT NULL,
    AccountNumber VARCHAR2(20) NOT NULL UNIQUE,
    Balance NUMBER(18,2) DEFAULT 0,
    Currency VARCHAR2(3) DEFAULT 'TRY',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Index
CREATE INDEX IDX_ACCOUNTS_NUMBER ON ACCOUNTS(AccountNumber);
```

### TRANSACTIONS Tablosu

```sql
CREATE TABLE TRANSACTIONS (
    TransactionId NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    SenderAccountId NUMBER NOT NULL,
    ReceiverAccountId NUMBER NOT NULL,
    Amount NUMBER(18,2) NOT NULL,
    TransactionType VARCHAR2(20) DEFAULT 'Transfer',
    Status VARCHAR2(20) DEFAULT 'Pending',
    ReferenceNo VARCHAR2(50),
    TransactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_SENDER FOREIGN KEY (SenderAccountId) REFERENCES ACCOUNTS(AccountId),
    CONSTRAINT FK_RECEIVER FOREIGN KEY (ReceiverAccountId) REFERENCES ACCOUNTS(AccountId)
);

-- Index
CREATE INDEX IDX_TRANSACTIONS_SENDER ON TRANSACTIONS(SenderAccountId);
CREATE INDEX IDX_TRANSACTIONS_RECEIVER ON TRANSACTIONS(ReceiverAccountId);
```

---

## Stored Procedures

### SP_TRANSFER_MONEY

```sql
CREATE OR REPLACE PROCEDURE SP_TRANSFER_MONEY(
    p_sender_id IN NUMBER,
    p_receiver_id IN NUMBER,
    p_amount IN NUMBER,
    p_status OUT VARCHAR2
) AS
    v_sender_balance NUMBER;
    v_receiver_exists NUMBER;
    v_reference_no VARCHAR2(50);
BEGIN
    -- Gönderen hesabın bakiyesini kontrol et
    SELECT Balance INTO v_sender_balance
    FROM ACCOUNTS WHERE AccountId = p_sender_id;

    -- Alıcı hesabın varlığını kontrol et
    SELECT COUNT(*) INTO v_receiver_exists
    FROM ACCOUNTS WHERE AccountId = p_receiver_id;

    IF v_receiver_exists = 0 THEN
        p_status := 'HATA: Alici hesap bulunamadi';
        RETURN;
    END IF;

    IF v_sender_balance < p_amount THEN
        p_status := 'HATA: Yetersiz bakiye';
        RETURN;
    END IF;

    -- Reference number oluştur
    v_reference_no := 'TRX' || TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS') || p_sender_id;

    -- Transfer işlemi
    UPDATE ACCOUNTS SET Balance = Balance - p_amount WHERE AccountId = p_sender_id;
    UPDATE ACCOUNTS SET Balance = Balance + p_amount WHERE AccountId = p_receiver_id;

    -- Transaction kaydı
    INSERT INTO TRANSACTIONS (SenderAccountId, ReceiverAccountId, Amount, Status, ReferenceNo)
    VALUES (p_sender_id, p_receiver_id, p_amount, 'Completed', v_reference_no);

    COMMIT;
    p_status := 'BASARILI';
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_status := 'HATA: Gonderen hesap bulunamadi';
    WHEN OTHERS THEN
        ROLLBACK;
        p_status := 'HATA: ' || SQLERRM;
END;
/
```

### SP_CREATE_ACCOUNT

```sql
CREATE OR REPLACE PROCEDURE SP_CREATE_ACCOUNT(
    p_customer_name IN VARCHAR2,
    p_account_number IN VARCHAR2,
    p_balance IN NUMBER,
    p_currency IN VARCHAR2,
    p_account_id OUT NUMBER
) AS
BEGIN
    INSERT INTO ACCOUNTS (CustomerName, AccountNumber, Balance, Currency, CreatedAt)
    VALUES (p_customer_name, p_account_number, p_balance, p_currency, SYSDATE)
    RETURNING AccountId INTO p_account_id;
    COMMIT;
END;
/
```

### SP_UPDATE_ACCOUNT

```sql
CREATE OR REPLACE PROCEDURE SP_UPDATE_ACCOUNT(
    p_account_id IN NUMBER,
    p_customer_name IN VARCHAR2,
    p_account_number IN VARCHAR2,
    p_balance IN NUMBER,
    p_currency IN VARCHAR2,
    p_rows_affected OUT NUMBER
) AS
BEGIN
    UPDATE ACCOUNTS
    SET CustomerName = p_customer_name,
        AccountNumber = p_account_number,
        Balance = p_balance,
        Currency = p_currency
    WHERE AccountId = p_account_id;

    p_rows_affected := SQL%ROWCOUNT;
    COMMIT;
END;
/
```

### SP_DELETE_ACCOUNT

```sql
CREATE OR REPLACE PROCEDURE SP_DELETE_ACCOUNT(
    p_account_id IN NUMBER,
    p_rows_affected OUT NUMBER
) AS
BEGIN
    DELETE FROM ACCOUNTS WHERE AccountId = p_account_id;
    p_rows_affected := SQL%ROWCOUNT;
    COMMIT;
END;
/
```

### SP_UPDATE_BALANCE

```sql
CREATE OR REPLACE PROCEDURE SP_UPDATE_BALANCE(
    p_account_id IN NUMBER,
    p_new_balance IN NUMBER,
    p_rows_affected OUT NUMBER
) AS
BEGIN
    UPDATE ACCOUNTS
    SET Balance = p_new_balance
    WHERE AccountId = p_account_id;

    p_rows_affected := SQL%ROWCOUNT;
    COMMIT;
END;
/
```

---

## API Endpoints

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/account` | Tüm hesapları listele |
| GET | `/api/account/{id}` | ID ile hesap getir |
| GET | `/api/account/by-number/{accountNumber}` | Hesap numarası ile getir |
| POST | `/api/account` | Yeni hesap oluştur |
| PUT | `/api/account/{id}` | Hesap güncelle |
| DELETE | `/api/account/{id}` | Hesap sil |
| POST | `/api/account/transfer` | Para transferi |

### Örnek İstekler

**Para Transferi:**
```bash
curl -X POST http://localhost:5022/api/Account/transfer \
  -H "Content-Type: application/json" \
  -d '{
    "senderId": 1,
    "receiverId": 2,
    "amount": 500
  }'
```

**Yeni Hesap Oluşturma:**
```bash
curl -X POST http://localhost:5022/api/Account \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Ahmet Yilmaz",
    "accountNumber": "TR123456789",
    "balance": 10000,
    "currency": "TRY"
  }'
```

---

## API Response Format

Tüm endpoint'ler `ApiResponse<T>` formatında yanıt döner:

```json
{
  "success": true,
  "message": "İşlem başarılı",
  "data": { }
}
```

**Hata Durumu:**
```json
{
  "success": false,
  "message": "Yetersiz bakiye",
  "data": null
}
```

---

## Proje Yapısı

```
SecurePay.Api/
├── Controllers/
│   └── AccountController.cs
├── Interfaces/
│   └── IAccountRepository.cs
├── Repositories/
│   └── AccountRepository.cs
├── Models/
│   ├── Account.cs
│   ├── Transaction.cs
│   ├── ApiResponse.cs
│   └── DTOs/
│       └── TransferRequestDto.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── wwwroot/
│   └── index.html          # 3D Coin animasyonlu transfer arayüzü
├── Program.cs
├── appsettings.json
└── SecurePay.Api.csproj
```


