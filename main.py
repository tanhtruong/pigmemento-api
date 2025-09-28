from fastapi import FastAPI, UploadFile, File, HTTPException, Depends, status, Response
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, EmailStr, Field
from typing import List, Literal
from PIL import Image
import io, random, os

# ---------- SQLAlchemy (async) ----------
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession
from sqlalchemy.orm import sessionmaker, declarative_base
from sqlalchemy import Column, text, select
from sqlalchemy.types import Text, DateTime
from sqlalchemy.dialects.postgresql import UUID, CITEXT
from sqlalchemy.exc import IntegrityError

# Load .env only in local development (not in production on Render)
if os.getenv("RENDER") is None:  # Render sets RENDER=1 internally
    try:
        from dotenv import load_dotenv
        load_dotenv()
        print("[startup] .env loaded (local dev).")
    except ImportError:
        print("[startup] python-dotenv not installed, skipping .env load.")

# --- SQLAlchemy base & deferred engine/session (safe pattern) ---
Base = declarative_base()
engine = None
AsyncSessionLocal = None

def _normalize_db_url(url: str) -> str:
    """Ensure we use the async driver so SQLAlchemy doesn't try psycopg2."""
    if url.startswith("postgres://"):
        return "postgresql+asyncpg://" + url[len("postgres://"):]
    if url.startswith("postgresql://"):
        return "postgresql+asyncpg://" + url[len("postgresql://"):]
    return url

def init_db():
    global engine, AsyncSessionLocal
    if engine is None:
        url = os.getenv("DATABASE_URL")
        if not url:
            raise RuntimeError("DATABASE_URL is not set. Add it in Render → Environment tab.")
        url = _normalize_db_url(url)
        if not url.startswith("postgresql+asyncpg://"):
            raise RuntimeError(
                "DATABASE_URL must use the async driver. "
                "Example: postgresql+asyncpg://user:pass@host:5432/dbname"
            )
        engine = create_async_engine(
            url,
            echo=False,
            pool_pre_ping=True,
            pool_size=5,
            max_overflow=5,
        )
        AsyncSessionLocal = sessionmaker(engine, class_=AsyncSession, expire_on_commit=False)

async def get_session():
    if AsyncSessionLocal is None:
        init_db()
    async with AsyncSessionLocal() as session:
        yield session

# ---------- FastAPI app ----------
app = FastAPI(title="Pigmemento API", version="0.1.0")

# --- CORS: allow your domains + local dev (Expo / RN) ---
ALLOWED_ORIGINS = [
    "https://pigmemento.app",
    "https://www.pigmemento.app",
    "http://localhost:5173",
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

# ---------- Domain models (existing) ----------
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

# --- In-memory sample data (existing) ---
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

# ---------- Waitlist DB model + schema ----------
class WaitlistSubscriber(Base):
    __tablename__ = "waitlist_subscribers"
    id = Column(UUID(as_uuid=True), primary_key=True, server_default=text("gen_random_uuid()"))
    name = Column(Text, nullable=False)
    email = Column(CITEXT, nullable=False, unique=True)
    created_at = Column(DateTime(timezone=True), nullable=False, server_default=text("now()"))

class WaitlistCreate(BaseModel):
    name: str = Field(min_length=1, max_length=200)
    email: EmailStr

# ---------- App startup: create extensions + tables ----------
@app.on_event("startup")
async def on_startup():
    init_db()
    async with engine.begin() as conn:
        # If your DB role cannot create extensions, comment these two lines
        await conn.exec_driver_sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;")
        await conn.exec_driver_sql("CREATE EXTENSION IF NOT EXISTS citext;")
        await conn.run_sync(Base.metadata.create_all)

# ---------- Routes (existing) ----------
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

# ---------- Waitlist endpoints ----------
@app.post("/waitlist")
async def join_waitlist(
    payload: WaitlistCreate,
    response: Response,
    db: AsyncSession = Depends(get_session),
):
    name = payload.name.strip()
    email = payload.email  # EmailStr validates; CITEXT makes it case-insensitive

    # Check if already signed up
    existing = await db.execute(
        select(WaitlistSubscriber).where(WaitlistSubscriber.email == email)
    )
    if existing.scalar_one_or_none():
        response.status_code = status.HTTP_200_OK
        return {"ok": True, "message": "Already on the waitlist."}

    # Try to add new subscriber
    record = WaitlistSubscriber(name=name, email=email)
    db.add(record)
    try:
        await db.commit()
        response.status_code = status.HTTP_201_CREATED
        return {"ok": True, "message": "Added to waitlist!"}
    except IntegrityError:
        await db.rollback()
        response.status_code = status.HTTP_200_OK
        return {"ok": True, "message": "Already on the waitlist."}

@app.post("/waitlist/check")
async def check_waitlist(payload: WaitlistCreate, db: AsyncSession = Depends(get_session)):
    q = await db.execute(select(WaitlistSubscriber).where(WaitlistSubscriber.email == payload.email))
    exists = q.scalar_one_or_none() is not None
    return {"ok": True, "exists": exists}