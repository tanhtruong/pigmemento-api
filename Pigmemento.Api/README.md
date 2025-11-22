

# 🧠 Pigmemento API  
Backend for the Pigmemento Educational Melanoma Recognition Trainer  
*ASP.NET Core Web API (C#) · PostgreSQL · EF Core · S3/R2 Storage · Optional Python ML Inference*

> **Disclaimer:**  
> Pigmemento is for **educational use only** — not intended for diagnosis or clinical decision‑making.

---

## 🚀 Overview

The Pigmemento API powers the mobile training app for melanoma recognition.  
It provides:

- Case delivery (dermoscopic images + metadata)  
- User authentication (JWT)  
- Model inference proxy (Python microservice *or* ONNX runtime)  
- Grad‑CAM image storage  
- Quiz attempt tracking & spaced‑repetition data  
- Admin import for bulk case loading  

This backend is designed to be **secure**, **fast**, and **easy to deploy** on Render, Fly.io, or Azure App Service.

---

## 🗂️ Project Structure

```
Pigmemento.Api/
  Controllers/
    CasesController.cs
    InferenceController.cs
    AuthController.cs
    AttemptsController.cs
  Data/
    AppDbContext.cs
  Models/
    Case.cs
    Patient.cs
    User.cs
    Attempt.cs
    TeachingPoint.cs
  Dtos/
    CaseDto.cs
    InferResponseDto.cs
    AuthDtos.cs
  Services/
    IInferenceClient.cs
    PythonInferenceClient.cs (optional)
    OnnxInferenceClient.cs (optional)
    StorageService.cs
  Auth/
    JwtTokenService.cs
  appsettings.json
  Program.cs
```

---

## 🔌 API Endpoints

### Health
```
GET /health
→ { "ok": true }
```

### Cases
```
GET /cases?limit=&difficulty=
GET /cases/{id}
```
- Listing does **not** expose truth labels  
- `/cases/{id}` includes label + teaching points (review screen)

### Inference
```
POST /infer (multipart/form-data)
```
Returns:
```json
{
  "probs": { "benign": 0.34, "malignant": 0.66 },
  "camPngUrl": "https://..."
}
```

### Auth
```
POST /register
POST /login
```
Returns JWT for the mobile client.

### Attempts
```
POST /answers
GET /me/progress   (future)
```

---

## 🧩 Tech Stack

### Core
- **ASP.NET Core 8 Web API**
- **Entity Framework Core**
- **PostgreSQL (Neon/Supabase)**

### Storage
- **Amazon S3** or **Cloudflare R2**  
Used for case images & Grad‑CAM overlays.

### ML Inference Options
#### **A) Python microservice** (recommended)
- Fastest iteration
- Rich PyTorch + Grad‑CAM ecosystem  
Configured in `appsettings.json`:
```json
"Inference": {
  "Mode": "Python",
  "BaseUrl": "https://ml.pigmemento.app"
}
```

#### **B) ONNX Runtime (C#)**
- Single deployment  
- Requires custom Grad‑CAM implementation

---

## ⚙️ Setup

### 1. Install dependencies
```bash
dotnet restore
```

### 2. Apply EF Core migrations
```bash
dotnet ef database update
```

### 3. Configure environment variables  
(Required for prod deployment)

```
ConnectionStrings__Postgres=<connection string>
JWT__Key=<your secret>
Storage__Bucket=pigmemento
Storage__Region=eu-central-1
Inference__Mode=Python
Inference__BaseUrl=https://ml.pigmemento.app
```

### 4. Run the API
```bash
dotnet run
```

---

## 🧪 Development

Swagger UI is enabled in Development:
```
https://localhost:5001/swagger
```

### Adding a migration
```bash
dotnet ef migrations add Init
```

### Seeding cases
Create a script or admin endpoint to import:
- Image URLs  
- Binary labels  
- Teaching points  
- Metadata (age, site, skin tone proxy)

Recommended sources: HAM10000, ISIC archive.

---

## 📦 Deployment

### Build
```bash
dotnet publish -c Release
```

### Run
```bash
dotnet Pigmemento.Api.dll
```

### Hosting options
- **Render** (recommended beginner‑friendly)
- **Fly.io** (good for small footprints)
- **Azure App Service**  
- **Docker → any VPS**

Ensure CORS allows:

```
https://pigmemento.app
https://www.pigmemento.app
http://localhost:19006   # Expo
```

---

## 🛡️ Security, GDPR & Safety

- No personal health identifiers (PHI) stored  
- Case images are fully anonymized; EXIF stripped  
- JWT with 8‑hour expiration  
- Strict CORS  
- Educational, **not diagnostic**  
- Sensitivity ≥0.92 at operating threshold; specificity reported

---

## 📊 ML Validation Targets

- **AUROC ≥ 0.90** on held‑out sites  
- **Sensitivity ≥ 0.92** at fixed threshold  
- Subgroup fairness: skin tone & site parity  
- Latency & UX metrics tracked from mobile app  

---

## 📄 License
Proprietary — internal development use only.