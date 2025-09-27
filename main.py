# main.py
from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Literal
from PIL import Image
import io, random

app = FastAPI(title="Pigmemento API", version="0.1.0")

# --- CORS: allow your domains + local dev (Expo / RN) ---
ALLOWED_ORIGINS = [
    "https://pigmemento.app",
    "https://www.pigmemento.app",
    "http://localhost:19006",   # Expo web preview
    "http://localhost:8081",    # Metro bundler
    "exp://",                   # Expo native scheme
]
app.add_middleware(
    CORSMiddleware,
    allow_origins=ALLOWED_ORIGINS,
    allow_credentials=True,
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["*"],
)

# --- Models ---
Label = Literal["benign", "malignant"]
Difficulty = Literal["easy", "med", "hard"]

class Patient(BaseModel):
    age: int
    site: str
    notes: str | None = None

class Case(BaseModel):
    id: str
    imageUrl: str
    patient: Patient
    label: Label
    difficulty: Difficulty

class InferResponse(BaseModel):
    probs: dict
    camPngUrl: str

# --- In-memory sample data ---
CASES: List[Case] = [
    Case(
        id="case_001",
        imageUrl="https://dummyimage.com/1024x1024/222/fff.png&text=Dermoscopy+1",
        patient=Patient(age=62, site="upper back"),
        label="malignant",
        difficulty="med",
    ),
    Case(
        id="case_002",
        imageUrl="https://dummyimage.com/1024x1024/333/fff.png&text=Dermoscopy+2",
        patient=Patient(age=34, site="forearm"),
        label="benign",
        difficulty="easy",
    ),
]

# --- Routes ---
@app.get("/health")
def health():
    return {"ok": True}

@app.get("/cases", response_model=List[Case])
def list_cases(limit: int = 20, difficulty: Difficulty | None = None):
    items = CASES
    if difficulty:
        items = [c for c in items if c.difficulty == difficulty]
    return items[:limit]

@app.get("/cases/{case_id}", response_model=Case)
def get_case(case_id: str):
    for c in CASES:
        if c.id == case_id:
            return c
    raise HTTPException(status_code=404, detail="Case not found")

@app.post("/infer", response_model=InferResponse)
async def infer(file: UploadFile = File(...)):
    # validate it's an image
    data = await file.read()
    try:
        _ = Image.open(io.BytesIO(data)).convert("RGB")
    except Exception:
        raise HTTPException(status_code=400, detail="Invalid image file")

    # TODO: replace with real model inference later
    p_m = round(random.uniform(0.05, 0.95), 3)
    return InferResponse(
        probs={"benign": round(1 - p_m, 3), "malignant": p_m},
        camPngUrl="https://dummyimage.com/600x600/cccccc/000000.png&text=Heatmap+Placeholder",
    )