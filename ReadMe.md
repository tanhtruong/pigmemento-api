# 🧠 Pigmemento – Educational Melanoma Recognition Trainer

> **Disclaimer:** Educational use only — **not for diagnosis**.  
> Pigmemento helps clinicians train visual recognition of melanoma using real dermoscopic cases, guided tips, and model-based attention maps.

---

## 🎯 Purpose

Pigmemento is a **mobile educational app** that helps general practitioners and dermatologists **train pattern recognition** for melanoma vs. benign lesions.  
It provides structured, interactive learning based on real dermoscopy cases, **AI explainability**, and **expert teaching points** — without making diagnostic claims.

---

## 🧩 Tech Stack

| Layer            | Technology                                                             |
| ---------------- | ---------------------------------------------------------------------- |
| **Frontend**     | React Native + Expo (TypeScript)                                       |
| **Backend**      | ASP.NET Core Web API (.NET 8), Entity Framework Core                   |
| **Database**     | PostgreSQL (Neon / Supabase)                                           |
| **Storage**      | S3 or Cloudflare R2 (case images, Grad-CAM overlays)                   |
| **Auth**         | JWT (ASP.NET Core)                                                     |
| **ML Inference** | Python microservice (PyTorch) or ONNX Runtime in .NET                  |
| **Hosting**      | Cloudflare Pages (landing) + Render / Fly.io / Azure App Service (API) |

---

## 🚦 Key Features

- 🧩 **Case Quiz** – classify lesions (benign vs malignant) from image and short clinical info
- 🔍 **Guided Review** – show model probabilities, Grad-CAM overlay, and expert teaching points
- ⏱️ **Timed Drills** – rapid challenges to improve pattern recognition speed
- 🔁 **Spaced Repetition** – resurface prior mistakes for retention
- 📊 **Fairness Metrics** – track model and user performance by skin tone or body site

---

## ⚖️ Compliance & Safety

- **Educational only** — no diagnostic output or claims.
- **GDPR-compliant**: anonymized data, stripped metadata, no PHI.
- **Transparency:** show AI heatmaps (Grad-CAM) and rationale.
- **Performance tracking:** model sensitivity/specificity reported at fixed thresholds.
- Clear disclaimer in app and store copy:
  > “Educational use only — not for diagnosis.”

---

## 🧠 Learning Design

- **Curriculum:** based on ABCDE and 7-point checklist principles.
- **Modes:**
  - Case Quiz (user guesses diagnosis)
  - Guided Review (AI feedback + teaching text)
  - Timed Drills (speed & retention)
- **Expert rationale:** each case includes dermatologist teaching notes.

---

## 📊 Model Objectives

- Metric: **AUROC ≥ 0.90** on held-out test sites
- Sensitivity: **≥ 0.92** at chosen threshold
- Specificity: reported alongside sensitivity
- Explainability: Grad-CAM overlays visualized in feedback
- Subgroup parity: assess across **skin tone**, **site**, **age**

---

## ☁️ Deployment Overview

- **Landing page:** Cloudflare Pages → `pigmemento.app`
- **Backend API:** Render / Fly.io / Azure → `api.pigmemento.app`
- **Inference microservice (if Python):** separate endpoint `ml.pigmemento.app`
- **Database:** Neon or Supabase (PostgreSQL)
- **Image storage:** Cloudflare R2 or AWS S3

All endpoints secured with HTTPS and JWT-based authentication.

---

## 🤝 Partnerships & Data Sources

- Public datasets for prototyping: **ISIC Archive**, **HAM10000**, **Derm7pt**.
- Dermatologist collaborators provide teaching points and review cases.
- Long-term goal: include institutionally vetted, diverse image datasets.

---

## 🧪 Validation Goals

- ✅ AUROC ≥ 0.90 (held-out dataset)
- ✅ Sensitivity ≥ 0.92 (training threshold)
- ✅ Balanced performance across subgroups
- ✅ Strong usability (SUS score, response time metrics)

---

## 🧭 Roadmap

1. MVP with Case Quiz + Guided Review
2. Add Timed Drills & Spaced Repetition
3. Integrate Grad-CAM explainability in feedback
4. Add user accounts and progress tracking
5. Fairness dashboard and institutional pilot

---

## 📜 License

MIT License © 2025 — Pigmemento  
Educational and research use only. Not intended for clinical diagnosis or patient management.
