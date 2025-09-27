# main.py
from fastapi import FastAPI, UploadFile, File, HTTPException, Depends, status
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, EmailStr, Field
from typing import List, Literal
from PIL import Image
import io, random, os

# ---------- NEW: SQLAlchemy async setup ----------
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession
from sqlalchemy.orm import sessionmaker, declarative_base
from sqlalchemy import Column, text, select
from sqlalchemy.types import Text, DateTime
from sqlalchemy.dialects.postgresql import UUID, CITEXT
from sqlalchemy.exc import IntegrityError

DATABASE_URL = os.getenv("DATABASE_URL")
if not DATABASE_URL:
    raise RuntimeError("DATABASE_URL is not set")

engine = create_async_engine(DATABASE_URL, echo=False, pool_pre_ping=True)
AsyncSessionLocal = sessionmaker(engine, class_=AsyncSession, expire_on_commit=False)
Base = declarative_base()

async def get_session():
    async with AsyncSessionLocal() as session:
        yield session

# ---------- FastAPI app ----------
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

# --- Models (your existing) ---
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

# --- In-memory sample data (your existing) ---
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

# ---------- NEW: Waitlist DB model + schema ----------
class WaitlistSubscriber(Base):
    __tablename__ = "waitlist_subscribers"
    id = Column(UUID(as_uuid=True), primary_key=True, server_default=text("gen_random_uuid()"))
    name = Column(Text, nullable=False)
    email = Column(CITEXT, nullable=False, unique=True)
    created_at = Column(DateTime(timezone=True), nullable=False, server_default=text("now()"))

class WaitlistCreate(BaseModel):
    name: str = Field(min_length=1, max_length=200)
    email: EmailStr

# ---------- Startup: create tables if missing (safe for dev) ----------
@app.on_event("startup")
async def on_startup():
    async with engine.begin() as conn:
        # Only creates if not exists; for prod, switch to Alembic migrations
        await conn.run_sync(Base.metadata.create_all)

# --- Routes (your existing) ---
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
    data = await file.read()
    try:
        _ = Image.open(io.BytesIO(data)).convert("RGB")
    except Exception:
        raise HTTPException(status_code=400, detail="Invalid image file")

    p_m = round(random.uniform(0.05, 0.95), 3)
    return InferResponse(
        probs={"benign": round(1 - p_m, 3), "malignant": p_m},
        camPngUrl="https://dummyimage.com/600x600/cccccc/000000.png&text=Heatmap+Placeholder",
    )

# ---------- NEW: Waitlist endpoints ----------
@app.post("/waitlist", status_code=status.HTTP_201_CREATED)
async def join_waitlist(payload: WaitlistCreate, db: AsyncSession = Depends(get_session)):
    name = payload.name.strip()
    email = payload.email

    # Friendly fast-path (unique index remains source of truth)
    existing = await db.execute(select(WaitlistSubscriber).where(WaitlistSubscriber.email == email))
    if existing.scalar_one_or_none():
        # Return 200 to let your UI treat "already on list" as success
        return {"ok": True, "message": "Already on the waitlist."}

    record = WaitlistSubscriber(name=name, email=email)
    db.add(record)
    try:
        await db.commit()
    except IntegrityError:
        await db.rollback()
        return {"ok": True, "message": "Already on the waitlist."}

    return {"ok": True, "message": "Added to waitlist!"}

@app.post("/waitlist/check")
async def check_waitlist(payload: WaitlistCreate, db: AsyncSession = Depends(get_session)):
    """Idempotent: returns whether an email is already registered (for UX)."""
    q = await db.execute(select(WaitlistSubscriber).where(WaitlistSubscriber.email == payload.email))
    exists = q.scalar_one_or_none() is not None
    return {"ok": True, "exists": exists}