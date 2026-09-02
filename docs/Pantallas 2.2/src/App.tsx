import React, { useRef, useState } from "react";

// ── Design tokens ─────────────────────────────────────────────────────────
const GOLD        = "#e8a820";
const GOLD_BORDER = "#d4960e";
const CARD_BG     = "#0d1a13";
const TEXT_WHITE  = "#ffffff";
const TEXT_GRAY   = "rgba(255,255,255,0.50)";
const TEXT_DIM    = "rgba(255,255,255,0.30)";
const BORDER_SUBTLE = "rgba(255,255,255,0.13)";

// Rarity palette (from Unity reference screens)
const RARITY: Record<string, { border: string; label: string; gradient?: string }> = {
  "Común":       { border: "#27c96a",               label: "#27c96a" },
  "Poco común":  { border: "rgba(180,200,195,0.55)", label: "rgba(180,200,195,0.75)" },
  "Rara":        { border: "#9b5cf6",               label: "#9b5cf6" },
  "Mítica":      { border: GOLD,                    label: GOLD, gradient: "linear-gradient(135deg,#e8a820,#f97316)" },
};

// ── Mock card data ────────────────────────────────────────────────────────
const CARDS = [
  { id:  1, name: "Luis Díaz",      ini: "LD",  rarity: "Mítica",      count: 1 },
  { id:  2, name: "Vinicius Jr.",   ini: "VJ",  rarity: "Rara",        count: 2 },
  { id:  3, name: "Haaland",        ini: "EH",  rarity: "Común",       count: 5 },
  { id:  4, name: "Mbappé",         ini: "KM",  rarity: "Poco común",  count: 3 },
  { id:  5, name: "Pedri",          ini: "PE",  rarity: "Rara",        count: 1 },
  { id:  6, name: "Rodri",          ini: "RO",  rarity: "Común",       count: 4 },
  { id:  7, name: "Lamine Yamal",   ini: "LY",  rarity: "Mítica",      count: 1 },
  { id:  8, name: "Bellingham",     ini: "JB",  rarity: "Rara",        count: 2 },
  { id:  9, name: "Salah",          ini: "MS",  rarity: "Poco común",  count: 6 },
  { id: 10, name: "De Bruyne",      ini: "KDB", rarity: "Rara",        count: 1 },
  { id: 11, name: "Musiala",        ini: "JM",  rarity: "Común",       count: 3 },
  { id: 12, name: "Osimhen",        ini: "VO",  rarity: "Poco común",  count: 2 },
];

const FILTERS = ["Álbum", "Recientes", "Rareza", "Cantidad", "Nación"];

// ── Shared: Stadium background ────────────────────────────────────────────
function StadiumBackground() {
  return (
    <>
      <div style={{
        position: "fixed", inset: 0, zIndex: 0,
        background: "linear-gradient(to bottom,#0d1520 0%,#101f18 35%,#0e2018 65%,#071510 100%)",
      }} />
      <div style={{
        position: "fixed", inset: 0, zIndex: 1, pointerEvents: "none",
        background: `
          radial-gradient(ellipse 260px 180px at -20px -20px,rgba(255,230,140,0.07) 0%,transparent 70%),
          radial-gradient(ellipse 260px 180px at calc(100% + 20px) -20px,rgba(255,230,140,0.07) 0%,transparent 70%)`,
      }} />
      <svg style={{
        position: "fixed", bottom: 0, left: "50%", transform: "translateX(-50%)",
        width: "100%", maxWidth: 390, height: 340, zIndex: 2,
        pointerEvents: "none", opacity: 0.055,
      }} viewBox="0 0 390 340" preserveAspectRatio="xMidYMax meet">
        <line x1="0"   y1="170" x2="390" y2="170" stroke="white" strokeWidth="1.5" />
        <circle cx="195" cy="170" r="54"   stroke="white" strokeWidth="1.5" fill="none" />
        <circle cx="195" cy="170" r="3"    fill="white" />
        <rect x="107" y="0"   width="176" height="88"  stroke="white" strokeWidth="1.5" fill="none" />
        <rect x="147" y="0"   width="96"  height="32"  stroke="white" strokeWidth="1.5" fill="none" />
        <circle cx="195" cy="68"  r="2.5" fill="white" />
        <rect x="107" y="252" width="176" height="88"  stroke="white" strokeWidth="1.5" fill="none" />
        <rect x="147" y="308" width="96"  height="32"  stroke="white" strokeWidth="1.5" fill="none" />
        <circle cx="195" cy="272" r="2.5" fill="white" />
        <rect x="8"   y="0"   width="374" height="340" stroke="white" strokeWidth="1.5" fill="none" />
      </svg>
      <div style={{
        position: "fixed", inset: 0, zIndex: 3, pointerEvents: "none",
        background: "radial-gradient(ellipse 85% 70% at 50% 40%,transparent 40%,rgba(0,0,0,0.55) 100%)",
      }} />
      {[14, 38, 72].map((pct) => (
        <div key={pct} style={{
          position: "fixed", top: 0, bottom: 0, left: `${pct}%`,
          width: 1, background: "rgba(255,255,255,0.03)",
          pointerEvents: "none", zIndex: 4,
        }} />
      ))}
    </>
  );
}

// ── Shared: Coin icon ─────────────────────────────────────────────────────
// ── Component: LogoMark ───────────────────────────────────────────────────
function LogoMark({ compact = false }: { compact?: boolean }) {
  const cardW = compact ? 36 : 44;
  const cardH = compact ? 49 : 60;
  const fontSize = compact ? 32 : 44;
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: compact ? 6 : 10 }}>
      {/* Stacked card icon */}
      <div style={{ position: "relative", width: cardW + 14, height: cardH + 4 }}>
        {/* Back card — rotated */}
        <div style={{
          position: "absolute", bottom: 0, left: 0,
          width: cardW, height: cardH, borderRadius: 7,
          background: "rgba(232,168,32,0.10)",
          border: "1px solid rgba(232,168,32,0.22)",
          transform: "rotate(-9deg)",
        }} />
        {/* Front card */}
        <div style={{
          position: "absolute", bottom: 0, right: 0,
          width: cardW, height: cardH, borderRadius: 7,
          background: "#0c1810",
          border: `1.5px solid ${GOLD}`,
          display: "flex", alignItems: "center", justifyContent: "center",
          boxShadow: `0 0 18px rgba(232,168,32,0.22)`,
        }}>
          <svg width={compact ? 18 : 22} height={compact ? 18 : 22} viewBox="0 0 24 24" fill="none"
            stroke={GOLD} strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="9" />
            <path d="M12 3c0 0-2 4-2 6s1 4 2 5 2-3 2-5-2-6-2-6z" />
            <path d="M3.5 9.5c2 0 4 1 5 3" />
            <path d="M20.5 9.5c-2 0-4 1-5 3" />
            <path d="M5 17c1-1 3-2 5-2" />
            <path d="M19 17c-1-1-3-2-5-2" />
          </svg>
        </div>
      </div>
      {/* Name */}
      <div style={{ textAlign: "center" }}>
        <div style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800,
          fontSize, letterSpacing: "0.14em", textTransform: "uppercase",
          color: TEXT_WHITE, lineHeight: 1,
        }}>FUTCARD</div>
        {!compact && (
          <div style={{
            fontSize: 11, color: "rgba(255,255,255,0.38)",
            letterSpacing: "0.22em", textTransform: "uppercase", marginTop: 4,
          }}>Trading Card Game</div>
        )}
      </div>
    </div>
  );
}

// ── Screen: Splash ────────────────────────────────────────────────────────
function SplashScreen() {
  return (
    <div style={{
      position: "fixed", inset: 0, zIndex: 100,
      display: "flex", flexDirection: "column",
      alignItems: "center", justifyContent: "center",
      gap: 48,
    }}>
      <LogoMark />
      <div className="spinner" />
    </div>
  );
}

// ── Screen: Login / Vincular cuenta ──────────────────────────────────────
function LoginScreen({ variant, onLogin, onSkip }: {
  variant: "link" | "nosession";
  onLogin: () => void;
  onSkip?: () => void;
}) {
  const isLink = variant === "link";

  const PROVIDER_BTN = {
    width: "100%", padding: "15px 20px",
    background: "rgba(255,255,255,0.04)",
    border: `1px solid rgba(255,255,255,0.14)`,
    borderRadius: 11,
    display: "flex", alignItems: "center", gap: 14,
    cursor: "pointer",
    transition: "background 0.15s",
  } as const;

  return (
    <div style={{
      position: "fixed", inset: 0, zIndex: 100,
      display: "flex", flexDirection: "column",
    }}>
      {/* Content — vertically centered with slight upward bias */}
      <div style={{
        flex: 1, display: "flex", flexDirection: "column",
        alignItems: "center", justifyContent: "center",
        padding: "0 28px", gap: 0,
      }}>
        {/* Logo */}
        <LogoMark compact />
        <div style={{ height: 40 }} />

        {/* Title + body */}
        <div style={{ textAlign: "center", marginBottom: 32 }}>
          <h1 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 26, letterSpacing: "0.10em", textTransform: "uppercase",
            color: TEXT_WHITE, margin: "0 0 10px",
          }}>
            {isLink ? "Guarda tu progreso" : "Bienvenido"}
          </h1>
          <p style={{
            fontSize: 13, color: TEXT_GRAY, margin: 0, lineHeight: 1.55,
            maxWidth: 280,
          }}>
            {isLink
              ? "Vincula tu cuenta para no perder tu colección si cambias de dispositivo."
              : "Inicia sesión para acceder a tu colección y conectarte con otros jugadores."}
          </p>
        </div>

        {/* Provider buttons */}
        <div style={{ width: "100%", display: "flex", flexDirection: "column", gap: 12 }}>
          {/* Google */}
          <button onClick={onLogin} style={PROVIDER_BTN}>
            {/* Google G mark */}
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
              <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
              <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
              <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
            </svg>
            <span style={{
              flex: 1, textAlign: "left",
              fontSize: 14, fontWeight: 500, color: TEXT_WHITE,
            }}>Continuar con Google</span>
          </button>

          {/* Email */}
          <button onClick={onLogin} style={PROVIDER_BTN}>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <rect x="2" y="4" width="20" height="16" rx="2"/>
              <polyline points="2,4 12,13 22,4"/>
            </svg>
            <span style={{
              flex: 1, textAlign: "left",
              fontSize: 14, fontWeight: 500, color: TEXT_WHITE,
            }}>Continuar con email</span>
          </button>
        </div>

        {/* Skip option — only for link variant */}
        {isLink && onSkip && (
          <button
            onClick={onSkip}
            style={{
              marginTop: 24, background: "none", border: "none", cursor: "pointer",
              padding: "8px 16px",
            }}
          >
            <span style={{ fontSize: 13, color: TEXT_DIM }}>Ahora no</span>
          </button>
        )}
      </div>

      {/* Safe area bottom spacer */}
      <div style={{ height: "max(20px, env(safe-area-inset-bottom))" }} />
    </div>
  );
}

function CoinIcon({ size = 14 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none">
      <circle cx="12" cy="12" r="10" fill={GOLD} />
      <circle cx="12" cy="12" r="7.5" fill="none" stroke="rgba(0,0,0,0.25)" strokeWidth="1" />
      <text x="12" y="16.5" textAnchor="middle" fontSize="9" fontWeight="700"
        fill="rgba(0,0,0,0.6)" fontFamily="sans-serif">$</text>
    </svg>
  );
}

// ── Shared: tab definitions ───────────────────────────────────────────────
const TABS = [
  {
    key: "inicio", label: "Inicio",
    icon: (a: boolean) => (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
        stroke={a ? GOLD : "rgba(255,255,255,0.45)"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M3 9.5L12 3l9 6.5V20a1 1 0 01-1 1H4a1 1 0 01-1-1V9.5z" /><path d="M9 21V12h6v9" />
      </svg>
    ),
  },
  {
    key: "cartas", label: "Mis cartas",
    icon: (a: boolean) => (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
        stroke={a ? GOLD : "rgba(255,255,255,0.45)"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <rect x="4" y="5" width="12" height="16" rx="2" /><rect x="7" y="3" width="12" height="16" rx="2" />
      </svg>
    ),
  },
  {
    key: "tienda", label: "Tienda",
    icon: (a: boolean) => (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
        stroke={a ? GOLD : "rgba(255,255,255,0.45)"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z" />
        <line x1="3" y1="6" x2="21" y2="6" />
        <path d="M16 10a4 4 0 01-8 0" />
      </svg>
    ),
  },
  {
    key: "comunidad", label: "Comunidad",
    icon: (a: boolean) => (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
        stroke={a ? GOLD : "rgba(255,255,255,0.45)"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="9" cy="8" r="3" /><circle cx="17" cy="10" r="2.5" />
        <path d="M2 20c0-3.3 3.1-6 7-6s7 2.7 7 6" /><path d="M17 14c2.2.5 4 2 4 4" />
      </svg>
    ),
  },
  {
    key: "perfil", label: "Perfil",
    icon: (a: boolean) => (
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
        stroke={a ? GOLD : "rgba(255,255,255,0.45)"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="8" r="4" /><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" />
      </svg>
    ),
  },
];

// ── Shared: Liquid-glass tab bar ──────────────────────────────────────────
function TabBar({ active, onSelect }: { active: string; onSelect: (k: string) => void }) {
  return (
    <nav style={{
      position: "fixed", bottom: 14, left: "50%", transform: "translateX(-50%)",
      width: "calc(100% - 32px)", maxWidth: 358,
      background: "rgba(14,32,22,0.58)",
      backdropFilter: "blur(26px)", WebkitBackdropFilter: "blur(26px)",
      borderRadius: 22,
      boxShadow: `
        inset 0 1px 0 rgba(255,255,255,0.22),
        inset 0 -1px 0 rgba(255,255,255,0.05),
        0 8px 32px rgba(0,0,0,0.50),
        0 2px 8px rgba(0,0,0,0.35)`,
      display: "flex", justifyContent: "space-around", alignItems: "center",
      padding: "10px 2px 14px", zIndex: 40,
    }}>
      <div style={{
        position: "absolute", top: 0, left: 18, right: 18, height: 1, borderRadius: 1,
        background: "linear-gradient(to right,transparent,rgba(255,255,255,0.35) 30%,rgba(255,255,255,0.35) 70%,transparent)",
        pointerEvents: "none",
      }} />
      {TABS.map((tab) => {
        const isActive = active === tab.key;
        return (
          <button key={tab.key} onClick={() => onSelect(tab.key)} style={{
            background: "none", border: "none", display: "flex", flexDirection: "column",
            alignItems: "center", cursor: "pointer", padding: 0, position: "relative", flex: 1,
          }}>
            {isActive && (
              <div style={{
                position: "absolute", inset: "-4px 4px -4px 4px", borderRadius: 14,
                background: "rgba(255,255,255,0.10)",
                boxShadow: "inset 0 1px 0 rgba(255,255,255,0.18),0 2px 8px rgba(0,0,0,0.25)",
                backdropFilter: "blur(8px)", WebkitBackdropFilter: "blur(8px)",
              }} />
            )}
            <div style={{
              position: "relative", display: "flex", flexDirection: "column",
              alignItems: "center", gap: 4, padding: "6px 8px 4px",
            }}>
              {tab.icon(isActive)}
              <span style={{
                fontSize: 10, fontWeight: isActive ? 600 : 400,
                color: isActive ? GOLD : "rgba(255,255,255,0.38)",
                letterSpacing: "0.03em", transition: "color 0.15s",
              }}>{tab.label}</span>
            </div>
          </button>
        );
      })}
    </nav>
  );
}

// ── Missions modal ───────────────────────────────────────────────────────
const MISSIONS_DATA = [
  { id: 1, name: "Abre 1 sobre",                       current: 0, total: 1 },
  { id: 2, name: "Consigue 1 carta rara o superior",   current: 0, total: 1 },
  { id: 3, name: "Vende 3 cartas duplicadas",          current: 1, total: 3 },
];

function MissionRow({ name, current, total }: { name: string; current: number; total: number }) {
  const done = current >= total;
  const pct = Math.min((current / total) * 100, 100);
  return (
    <div style={{
      background: "rgba(255,255,255,0.04)",
      border: `1px solid ${done ? GOLD_BORDER : BORDER_SUBTLE}`,
      borderRadius: 8,
      padding: "12px 14px",
      display: "flex", flexDirection: "column", gap: 8,
    }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <span style={{ fontSize: 13, fontWeight: done ? 600 : 400, color: done ? TEXT_WHITE : "rgba(255,255,255,0.80)", flex: 1, paddingRight: 12 }}>
          {name}
        </span>
        <span style={{ fontSize: 11, color: done ? GOLD : TEXT_GRAY, fontWeight: done ? 600 : 400, whiteSpace: "nowrap" }}>
          {done ? "✓ Listo" : `${current}/${total}`}
        </span>
      </div>
      <div style={{ height: 3, borderRadius: 999, background: "rgba(255,255,255,0.10)", overflow: "hidden" }}>
        <div style={{ height: "100%", width: `${pct}%`, borderRadius: 999, background: done ? GOLD : "rgba(232,168,32,0.55)", transition: "width 0.4s" }} />
      </div>
    </div>
  );
}

function MissionsModal({ onClose }: { onClose: () => void }) {
  const completed = MISSIONS_DATA.filter((m) => m.current >= m.total).length;
  const barPct = Math.min((completed / 4) * 100, 100);

  return (
    <>
      {/* Backdrop */}
      <div onClick={onClose} style={{
        position: "fixed", inset: 0, zIndex: 60,
        background: "rgba(0,0,0,0.68)",
        backdropFilter: "blur(3px)",
        WebkitBackdropFilter: "blur(3px)",
      }} />

      {/* Modal card — centered */}
      <div style={{
        position: "fixed",
        top: "50%", left: "50%",
        transform: "translate(-50%, -50%)",
        width: "calc(100% - 40px)",
        maxWidth: 360,
        maxHeight: "80dvh",
        background: "#0c1810",
        border: `1px solid ${BORDER_SUBTLE}`,
        borderRadius: 14,
        boxShadow: "0 24px 64px rgba(0,0,0,0.70), 0 4px 16px rgba(0,0,0,0.50)",
        zIndex: 61,
        display: "flex", flexDirection: "column",
        overflow: "hidden",
      }}>
        {/* Scrollable body */}
        <div style={{ overflowY: "auto", padding: "20px 20px 24px" }}>

          {/* Header */}
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 20 }}>
            <h2 style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 20,
              letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
            }}>Misiones diarias</h2>
            <button onClick={onClose} style={{ background: "none", border: "none", cursor: "pointer", padding: 2, marginLeft: 8 }}>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.40)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </div>

          {/* ── Milestone reward bar ── */}
          <div style={{ marginBottom: 18 }}>
            {/* Reward squares + connector lines above the bar */}
            <div style={{ position: "relative", height: 58, marginBottom: 0 }}>
              {/* M1 at 50% */}
              {[
                { pct: "50%", req: 2, transform: "translateX(-50%)" },
                { pct: "calc(100% - 2px)", req: 4, transform: "translateX(-100%)" },
              ].map(({ pct, req, transform }) => (
                <div key={req} style={{
                  position: "absolute", left: pct, top: 0,
                  transform,
                  display: "flex", flexDirection: "column", alignItems: "center", gap: 4,
                }}>
                  <div style={{
                    width: 38, height: 38,
                    border: `1.5px dashed ${GOLD_BORDER}`,
                    borderRadius: 7,
                    background: "rgba(232,168,32,0.06)",
                    display: "flex", alignItems: "center", justifyContent: "center",
                  }}>
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                      stroke={GOLD_BORDER} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                      <rect x="2" y="9" width="20" height="12" rx="2" />
                      <path d="M12 9v12M2 13h20" />
                      <path d="M8 9C8 6.5 9.8 5 12 5s4 1.5 4 4" />
                    </svg>
                  </div>
                  {/* Vertical connector to bar */}
                  <div style={{ width: 1, height: 8, background: `rgba(212,150,14,0.35)` }} />
                </div>
              ))}
            </div>

            {/* Progress track */}
            <div style={{ position: "relative", height: 6, borderRadius: 999, background: "rgba(255,255,255,0.09)" }}>
              {/* Fill */}
              <div style={{ position: "absolute", left: 0, top: 0, bottom: 0, width: `${barPct}%`, borderRadius: 999, background: GOLD, transition: "width 0.5s" }} />
              {/* M1 checkpoint dot at 50% */}
              <div style={{
                position: "absolute", left: "50%", top: "50%", transform: "translate(-50%,-50%)",
                width: 12, height: 12, borderRadius: "50%",
                background: completed >= 2 ? GOLD : "#0c1810",
                border: `2px solid ${completed >= 2 ? GOLD : "rgba(255,255,255,0.25)"}`,
                zIndex: 1,
              }} />
              {/* M2 checkpoint at right edge */}
              <div style={{
                position: "absolute", right: 0, top: "50%", transform: "translateY(-50%)",
                width: 12, height: 12, borderRadius: "50%",
                background: completed >= 4 ? GOLD : "#0c1810",
                border: `2px solid ${completed >= 4 ? GOLD : "rgba(255,255,255,0.25)"}`,
                zIndex: 1,
              }} />
            </div>

            {/* Labels under bar */}
            <div style={{ display: "flex", justifyContent: "space-between", marginTop: 6 }}>
              <span style={{ flex: 1 }} />
              <span style={{ fontSize: 10, color: TEXT_DIM, flex: 0, transform: "translateX(-50%)", whiteSpace: "nowrap" }}>2 misiones</span>
              <span style={{ flex: 1 }} />
              <span style={{ fontSize: 10, color: TEXT_DIM, whiteSpace: "nowrap" }}>4 misiones</span>
            </div>
          </div>

          {/* Stats row */}
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 18 }}>
            <span style={{ fontSize: 12, color: TEXT_GRAY }}>
              Completadas: <span style={{ color: TEXT_WHITE, fontWeight: 600 }}>{completed}</span>
            </span>
            <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none"
                stroke={TEXT_GRAY} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="9" /><polyline points="12,6 12,12 16,14" />
              </svg>
              <span style={{ fontSize: 12, color: TEXT_GRAY }}>Se reinicia en 05h 41min</span>
            </div>
          </div>

          {/* Mission list */}
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {MISSIONS_DATA.map((m) => (
              <MissionRow key={m.id} name={m.name} current={m.current} total={m.total} />
            ))}
          </div>

        </div>
      </div>
    </>
  );
}

// ── Screen: Inicio ────────────────────────────────────────────────────────
const ENVELOPES = [
  { label: "Sobre A", featured: false },
  { label: "Sobre B", featured: true  },
  { label: "Sobre C", featured: false },
];

function HomeScreen() {
  const [showMissions, setShowMissions] = useState(false);
  return (
    <>
      {/* Top bar */}
      <div style={{
        display: "flex", alignItems: "center",
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 10px",
        position: "relative", flexShrink: 0,
      }}>
        <div style={{ display: "flex", gap: 8 }}>
          {[0, 1].map((i) => (
            <button key={i} style={{
              width: 36, height: 36,
              background: "rgba(255,255,255,0.06)", border: `1px solid ${BORDER_SUBTLE}`,
              borderRadius: 8, cursor: "pointer",
            }} />
          ))}
        </div>
        {/* Avatar — absolutely centered */}
        <div style={{
          position: "absolute", left: "50%", transform: "translateX(-50%)",
          display: "flex", flexDirection: "column", alignItems: "center", gap: 2,
          pointerEvents: "none",
        }}>
          <div style={{
            width: 44, height: 44, borderRadius: "50%",
            background: "rgba(255,255,255,0.07)", border: `1px solid ${BORDER_SUBTLE}`,
            display: "flex", alignItems: "center", justifyContent: "center", pointerEvents: "auto",
          }}>
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.45)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="8" r="4" /><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" />
            </svg>
          </div>
          <span style={{ fontSize: 12, fontWeight: 600, letterSpacing: "0.04em", lineHeight: 1, pointerEvents: "auto" }}>
            JUGADOR_01
          </span>
          <span style={{ fontSize: 10, color: TEXT_GRAY, letterSpacing: "0.03em", pointerEvents: "auto" }}>
            Nivel 7
          </span>
        </div>
        <div style={{ flex: 1 }} />
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{
            display: "flex", alignItems: "center", gap: 5,
            background: "rgba(0,0,0,0.4)", border: `1px solid ${GOLD_BORDER}`,
            borderRadius: 999, padding: "5px 10px 5px 7px",
          }}>
            <CoinIcon size={15} />
            <span style={{ fontSize: 12, fontWeight: 600, color: TEXT_WHITE, letterSpacing: "0.02em" }}>240</span>
          </div>
          <button style={{ background: "none", border: "none", cursor: "pointer", padding: 2, position: "relative" }}>
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.5)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <rect x="2" y="4" width="20" height="16" rx="2" /><polyline points="2,4 12,13 22,4" />
            </svg>
            <span style={{
              position: "absolute", top: 0, right: 0, width: 7, height: 7,
              borderRadius: "50%", background: GOLD, border: "1.5px solid #0d1520",
            }} />
          </button>
          <button style={{ background: "none", border: "none", cursor: "pointer", padding: 2 }}>
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.5)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <rect x="2" y="9" width="20" height="12" rx="2" />
              <path d="M12 9v12M2 13h20" />
              <path d="M8 9C8 6.5 9.8 5 12 5s4 1.5 4 4" />
              <path d="M9 5c0-1.7 1.3-3 3-3s3 1.3 3 3" />
            </svg>
          </button>
        </div>
      </div>

      {/* Main content */}
      <div style={{
        flex: 1, display: "flex", flexDirection: "column",
        padding: "4px 16px", paddingBottom: 108, gap: 32, overflowY: "auto",
      }}>
        {/* Sobres disponibles */}
        <section>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE, marginBottom: 14,
          }}>Sobres disponibles</h2>
          <div style={{ display: "flex", gap: 10, height: 220 }}>
            {ENVELOPES.map((env) => (
              <div key={env.label} style={{
                flex: 1, height: "100%", background: CARD_BG,
                border: `1.5px solid ${env.featured ? GOLD_BORDER : BORDER_SUBTLE}`,
                borderRadius: 8,
                display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "flex-end",
                padding: "0 0 12px", cursor: "pointer",
              }}>
                <span style={{
                  fontSize: 11, fontWeight: 400, letterSpacing: "0.05em", textTransform: "uppercase",
                  color: env.featured ? GOLD : TEXT_GRAY,
                }}>{env.label}</span>
              </div>
            ))}
          </div>
        </section>

        {/* Evento especial + Tienda */}
        <section>
          <div style={{ display: "flex", gap: 10 }}>
            {[
              { label: "Evento especial", path: <><circle cx="12" cy="12" r="9" /><polyline points="12,6 12,12 16,14" /></> },
              { label: "Tienda", path: <><path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z" /><line x1="3" y1="6" x2="21" y2="6" /><path d="M16 10a4 4 0 01-8 0" /></> },
            ].map((item) => (
              <button key={item.label} style={{
                flex: 1, background: CARD_BG, border: `1px solid ${BORDER_SUBTLE}`,
                borderRadius: 8, padding: "26px 10px",
                display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center",
                gap: 10, cursor: "pointer", color: TEXT_GRAY, fontSize: 13, fontWeight: 400,
              }}>
                <svg width="26" height="26" viewBox="0 0 24 24" fill="none"
                  stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                  {item.path}
                </svg>
                {item.label}
              </button>
            ))}
          </div>
        </section>

        {/* Misiones */}
        <div style={{ display: "flex", justifyContent: "flex-end", marginTop: -16 }}>
          <button onClick={() => setShowMissions(true)} style={{
            background: GOLD, border: "none", borderRadius: 999, padding: "13px 28px",
            display: "flex", alignItems: "center", gap: 8, cursor: "pointer",
            color: "#000", fontSize: 13, fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase",
          }}>
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none"
              stroke="#000" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M9 11l3 3L22 4" /><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" />
            </svg>
            Misiones
          </button>
        </div>

        {showMissions && <MissionsModal onClose={() => setShowMissions(false)} />}

        {/* Racha diaria */}
        <section style={{
          background: CARD_BG, border: `1px solid ${BORDER_SUBTLE}`,
          borderRadius: 8, padding: "16px 16px 18px",
        }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", marginBottom: 12 }}>
            <span style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 15,
              letterSpacing: "0.1em", textTransform: "uppercase", color: TEXT_WHITE,
            }}>Racha diaria</span>
            <span style={{ fontSize: 11, color: TEXT_GRAY }}>3 / 5 días</span>
          </div>
          <div style={{ height: 6, borderRadius: 999, background: "rgba(255,255,255,0.10)", overflow: "hidden" }}>
            <div style={{ height: "100%", width: "60%", borderRadius: 999, background: GOLD }} />
          </div>
          <div style={{ display: "flex", justifyContent: "space-between", marginTop: 10 }}>
            {[1, 2, 3, 4, 5].map((day) => {
              const done = day <= 3;
              return (
                <div key={day} style={{
                  width: 28, height: 28, borderRadius: 6,
                  background: done ? "rgba(232,168,32,0.18)" : "rgba(255,255,255,0.05)",
                  border: `1px solid ${done ? GOLD_BORDER : BORDER_SUBTLE}`,
                  display: "flex", alignItems: "center", justifyContent: "center",
                }}>
                  {done
                    ? <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                        stroke={GOLD} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="20,6 9,17 4,12" />
                      </svg>
                    : <span style={{ fontSize: 10, color: TEXT_DIM, fontWeight: 500 }}>{day}</span>
                  }
                </div>
              );
            })}
          </div>
        </section>
      </div>
    </>
  );
}

// ── Screen: Mis Cartas ────────────────────────────────────────────────────
function MyCardsScreen() {
  const [activeFilter, setActiveFilter] = useState("Rareza");
  const scrollRef = useRef<HTMLDivElement>(null);

  const scroll = (dir: "left" | "right") => {
    scrollRef.current?.scrollBy({ left: dir === "left" ? -120 : 120, behavior: "smooth" });
  };

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 0",
        flexShrink: 0,
      }}>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 26,
          letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
          margin: 0, marginBottom: 18,
        }}>Mis cartas</h1>

        {/* Filter bar */}
        <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 14 }}>
          <button onClick={() => scroll("left")} style={{
            background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0,
          }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.35)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="15,18 9,12 15,6" />
            </svg>
          </button>

          <div ref={scrollRef} style={{
            display: "flex", gap: 8, overflowX: "auto", flex: 1,
            scrollbarWidth: "none", msOverflowStyle: "none",
          }}>
            {FILTERS.map((f) => {
              const isActive = f === activeFilter;
              return (
                <button key={f} onClick={() => setActiveFilter(f)} style={{
                  flexShrink: 0,
                  background: isActive ? "rgba(232,168,32,0.10)" : "rgba(255,255,255,0.05)",
                  border: `1px solid ${isActive ? GOLD_BORDER : BORDER_SUBTLE}`,
                  borderRadius: 999,
                  padding: "6px 14px",
                  color: isActive ? GOLD : TEXT_GRAY,
                  fontSize: 12, fontWeight: isActive ? 600 : 400,
                  letterSpacing: "0.03em", cursor: "pointer", whiteSpace: "nowrap",
                }}>
                  {f}
                </button>
              );
            })}
          </div>

          <button onClick={() => scroll("right")} style={{
            background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0,
          }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.35)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="9,18 15,12 9,6" />
            </svg>
          </button>
        </div>

        {/* Counter + search */}
        <div style={{
          display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16,
        }}>
          <span style={{ fontSize: 13, color: TEXT_GRAY, letterSpacing: "0.02em" }}>1232 cartas</span>
          <button style={{ background: "none", border: "none", cursor: "pointer", padding: 2 }}>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.45)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="11" cy="11" r="7" /><line x1="16.5" y1="16.5" x2="21" y2="21" />
            </svg>
          </button>
        </div>
      </div>

      {/* Card grid */}
      <div className="card-grid-scroll" style={{
        flex: 1, overflowY: "auto",
        padding: "0 16px", paddingBottom: 108,
      }}>
        <div style={{
          display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12,
        }}>
          {CARDS.map((card) => {
            const r = RARITY[card.rarity];
            const isMythic = card.rarity === "Mítica";

            const inner = (
              <div style={{
                background: CARD_BG,
                borderRadius: isMythic ? 7 : 8,
                display: "flex", flexDirection: "column",
                alignItems: "center", justifyContent: "space-between",
                padding: "16px 10px 12px",
                height: "100%",
              }}>
                {/* Player initials avatar */}
                <div style={{
                  width: 52, height: 52, borderRadius: "50%",
                  background: "rgba(255,255,255,0.06)",
                  border: `1px solid ${r.border}`,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  marginBottom: 10,
                }}>
                  <span style={{
                    fontSize: 15, fontWeight: 700, color: r.label, letterSpacing: "0.04em",
                  }}>{card.ini}</span>
                </div>

                {/* Name */}
                <span style={{
                  fontSize: 12, fontWeight: 500, color: TEXT_WHITE,
                  textAlign: "center", lineHeight: 1.3,
                  overflow: "hidden", textOverflow: "ellipsis", maxWidth: "100%",
                }}>{card.name}</span>

                {/* Rarity + count */}
                <span style={{
                  marginTop: 8, fontSize: 10, color: r.label,
                  letterSpacing: "0.04em", fontWeight: 400,
                }}>
                  {card.rarity}{card.count > 1 ? ` ×${card.count}` : ""}
                </span>
              </div>
            );

            return isMythic ? (
              <div key={card.id} style={{
                background: r.gradient, padding: "1.5px", borderRadius: 9,
                aspectRatio: "3/4",
              }}>
                {inner}
              </div>
            ) : (
              <div key={card.id} style={{
                border: `1.5px solid ${r.border}`, borderRadius: 8,
                aspectRatio: "3/4",
              }}>
                {inner}
              </div>
            );
          })}
        </div>
      </div>
    </>
  );
}

// ── Screen: Tienda ────────────────────────────────────────────────────────
const SOBRES_SHOP = [
  { label: "Sobre A",      featured: false, price: 100 },
  { label: "Sobre B",      featured: true,  price: 300 },
  { label: "Sobre C",      featured: false, price: 600 },
];

const COIN_PACKS = [
  { label: "Inicio",   coins: 150,  bonus: null,  priceTag: "$0.99" },
  { label: "Estándar", coins: 400,  bonus: null,  priceTag: "$1.99" },
  { label: "Premium",  coins: 900,  bonus: 100,   priceTag: "$3.99" },
  { label: "Élite",    coins: 2000, bonus: 300,   priceTag: "$7.99" },
];

function CoinChip({ balance }: { balance: number }) {
  return (
    <div style={{
      display: "flex", alignItems: "center", gap: 5,
      background: "rgba(0,0,0,0.4)", border: `1px solid ${GOLD_BORDER}`,
      borderRadius: 999, padding: "5px 10px 5px 7px",
    }}>
      <CoinIcon size={15} />
      <span style={{ fontSize: 12, fontWeight: 600, color: TEXT_WHITE, letterSpacing: "0.02em" }}>{balance}</span>
    </div>
  );
}

function StoreScreen() {
  const [adCount] = useState(2);
  const adMax = 3;

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", justifyContent: "space-between",
        flexShrink: 0,
      }}>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 26,
          letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
        }}>Tienda</h1>
        <CoinChip balance={240} />
      </div>

      {/* Scrollable content */}
      <div style={{ flex: 1, overflowY: "auto", padding: "0 16px", paddingBottom: 108 }}>

        {/* ── COMPRAR SOBRES ── */}
        <section style={{ marginBottom: 28 }}>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
            margin: 0, marginBottom: 14,
          }}>Comprar sobres</h2>
          <div style={{ display: "flex", gap: 10 }}>
            {SOBRES_SHOP.map((env) => (
              <div key={env.label} style={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
                {/* Envelope card */}
                <div style={{
                  width: "100%", aspectRatio: "3/4",
                  background: CARD_BG,
                  border: `1.5px solid ${env.featured ? GOLD_BORDER : BORDER_SUBTLE}`,
                  borderRadius: 8,
                  display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "flex-end",
                  padding: "0 0 10px", cursor: "pointer",
                }}>
                  <span style={{ fontSize: 11, color: env.featured ? GOLD : TEXT_GRAY, letterSpacing: "0.05em", textTransform: "uppercase" }}>
                    {env.label}
                  </span>
                </div>
                {/* Price */}
                <button style={{
                  width: "100%",
                  background: env.featured ? "rgba(232,168,32,0.10)" : "rgba(255,255,255,0.05)",
                  border: `1px solid ${env.featured ? GOLD_BORDER : BORDER_SUBTLE}`,
                  borderRadius: 6, padding: "7px 4px",
                  display: "flex", alignItems: "center", justifyContent: "center", gap: 5,
                  cursor: "pointer",
                }}>
                  <CoinIcon size={13} />
                  <span style={{ fontSize: 12, fontWeight: 600, color: env.featured ? GOLD : TEXT_GRAY }}>{env.price}</span>
                </button>
              </div>
            ))}
          </div>
        </section>

        {/* ── VER ANUNCIO ── */}
        <section style={{ marginBottom: 28 }}>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
            margin: 0, marginBottom: 14,
          }}>Ver anuncio</h2>
          <button style={{
            width: "100%",
            background: "rgba(14,22,10,0.85)",
            border: `1.5px solid ${GOLD}`,
            borderRadius: 10,
            padding: "20px 20px",
            display: "flex", alignItems: "center", gap: 16,
            cursor: "pointer",
            boxShadow: `0 0 0 1px rgba(232,168,32,0.10)`,
            textAlign: "left",
          }}>
            {/* Play button */}
            <div style={{
              width: 52, height: 52, borderRadius: "50%", flexShrink: 0,
              background: GOLD,
              display: "flex", alignItems: "center", justifyContent: "center",
            }}>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="#000" stroke="none">
                <polygon points="5,3 19,12 5,21" />
              </svg>
            </div>
            <div style={{ flex: 1 }}>
              <p style={{ fontSize: 14, fontWeight: 600, color: TEXT_WHITE, margin: 0, marginBottom: 4, lineHeight: 1.3 }}>
                Ve un anuncio y gana 1 sobre
              </p>
              <p style={{ fontSize: 11, color: TEXT_GRAY, margin: 0 }}>Gratis · Sin costo</p>
            </div>
            {/* Daily counter */}
            <div style={{
              display: "flex", flexDirection: "column", alignItems: "center", gap: 2, flexShrink: 0,
            }}>
              <span style={{ fontSize: 18, fontWeight: 700, color: GOLD, lineHeight: 1 }}>{adCount}/{adMax}</span>
              <span style={{ fontSize: 9, color: TEXT_DIM, letterSpacing: "0.05em", textTransform: "uppercase" }}>hoy</span>
            </div>
          </button>
        </section>

        {/* ── COMPRAR MONEDAS ── */}
        <section style={{ marginBottom: 8 }}>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
            margin: 0, marginBottom: 14,
          }}>Comprar monedas</h2>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
            {COIN_PACKS.map((pack) => (
              <button key={pack.label} style={{
                background: CARD_BG,
                border: `1px solid ${BORDER_SUBTLE}`,
                borderRadius: 9,
                padding: "16px 12px 14px",
                display: "flex", flexDirection: "column", alignItems: "center", gap: 6,
                cursor: "pointer",
              }}>
                {/* Coin stack visual */}
                <div style={{ display: "flex", alignItems: "center", gap: 4, marginBottom: 2 }}>
                  <CoinIcon size={22} />
                  {pack.bonus && <CoinIcon size={16} />}
                </div>
                {/* Amount */}
                <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 1 }}>
                  <span style={{ fontSize: 16, fontWeight: 700, color: TEXT_WHITE, lineHeight: 1 }}>
                    {pack.coins.toLocaleString()}
                  </span>
                  {pack.bonus && (
                    <span style={{ fontSize: 10, color: GOLD, fontWeight: 500 }}>+{pack.bonus} bonus</span>
                  )}
                </div>
                {/* Price — placeholder from billing */}
                <span style={{
                  fontSize: 12, fontWeight: 600,
                  color: TEXT_GRAY,
                  background: "rgba(255,255,255,0.06)",
                  border: `1px solid ${BORDER_SUBTLE}`,
                  borderRadius: 4, padding: "3px 8px",
                  letterSpacing: "0.02em",
                }}>
                  {pack.priceTag}
                </span>
              </button>
            ))}
          </div>
        </section>

      </div>
    </>
  );
}

// ── Screen: Comunidad ─────────────────────────────────────────────────────
const COMMUNITY_ITEMS = [
  {
    key: "vitrinas",
    label: "Vitrinas públicas",
    badge: null,
    icon: (
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
        stroke="rgba(255,255,255,0.60)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="3" width="7" height="7" rx="1" />
        <rect x="14" y="3" width="7" height="7" rx="1" />
        <rect x="3" y="14" width="7" height="7" rx="1" />
        <rect x="14" y="14" width="7" height="7" rx="1" />
      </svg>
    ),
  },
  {
    key: "intercambio",
    label: "Intercambio",
    badge: 3,
    icon: (
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
        stroke="rgba(255,255,255,0.60)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M7 16H3l4-4" />
        <path d="M3 12h14a2 2 0 010 4H3" />
        <path d="M17 8h4l-4 4" />
        <path d="M21 12H7a2 2 0 010-4h14" />
      </svg>
    ),
  },
  {
    key: "mercado",
    label: "Mercado",
    badge: null,
    icon: (
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
        stroke="rgba(255,255,255,0.60)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M20.59 13.41l-7.17 7.17a2 2 0 01-2.83 0L2 12V2h10l8.59 8.59a2 2 0 010 2.82z" />
        <circle cx="7" cy="7" r="1.5" fill="rgba(255,255,255,0.60)" stroke="none" />
      </svg>
    ),
  },
  {
    key: "amigos",
    label: "Amigos",
    badge: 2,
    icon: (
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
        stroke="rgba(255,255,255,0.60)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="9" cy="7" r="3" />
        <path d="M3 21v-2a4 4 0 014-4h4a4 4 0 014 4v2" />
        <path d="M16 3.13a4 4 0 010 7.75" />
        <path d="M21 21v-2a4 4 0 00-3-3.85" />
      </svg>
    ),
  },
];

function CommunityScreen({ onNavigate }: { onNavigate: (sub: string) => void }) {
  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 0",
        flexShrink: 0,
      }}>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 26,
          letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
          margin: 0, marginBottom: 28,
        }}>Comunidad</h1>
      </div>

      {/* 2×2 grid */}
      <div style={{
        flex: 1, padding: "0 16px", paddingBottom: 108, overflowY: "auto",
        display: "flex", flexDirection: "column", gap: 12,
      }}>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
          {COMMUNITY_ITEMS.map((item) => (
            <button
              key={item.key}
              onClick={() => onNavigate(item.key)}
              style={{
                position: "relative",
                background: CARD_BG,
                border: `1px solid ${BORDER_SUBTLE}`,
                borderRadius: 10,
                padding: "28px 12px 22px",
                display: "flex", flexDirection: "column",
                alignItems: "center", justifyContent: "center",
                gap: 14, cursor: "pointer",
                aspectRatio: "1 / 1",
              }}
            >
              {item.badge !== null && (
                <div style={{
                  position: "absolute", top: 10, right: 10,
                  minWidth: 20, height: 20, borderRadius: 999,
                  background: "rgba(0,0,0,0.55)",
                  border: `1px solid ${GOLD_BORDER}`,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  padding: "0 5px",
                }}>
                  <span style={{ fontSize: 10, fontWeight: 700, color: GOLD, letterSpacing: "0.02em" }}>{item.badge}</span>
                </div>
              )}
              {item.icon}
              <span style={{ fontSize: 12, fontWeight: 500, color: TEXT_GRAY, letterSpacing: "0.03em", textAlign: "center", lineHeight: 1.35 }}>
                {item.label}
              </span>
            </button>
          ))}
        </div>
      </div>
    </>
  );
}

// ── Screen: Vitrinas públicas ─────────────────────────────────────────────

const VITRINAS_POPULARES = [
  { user: "ProPlayer_99", avatar: "PP", cards: ["Mítica", "Rara",        "Rara"],        likes: 234 },
  { user: "FutbolFan_22", avatar: "FF", cards: ["Rara",   "Poco común",  "Común"],       likes: 189 },
  { user: "CardMaster_X", avatar: "CM", cards: ["Mítica", "Mítica",      "Rara"],        likes: 512 },
  { user: "GoldenShot_7", avatar: "GS", cards: ["Rara",   "Rara",        "Poco común"],  likes: 97  },
];

const VITRINAS_AMIGOS = [
  { user: "MiAmigo_01",  avatar: "MA", cards: ["Común",  "Rara",  "Poco común"], likes: 45 },
  { user: "ElChampion",  avatar: "EC", cards: ["Mítica", "Rara",  "Común"],      likes: 78 },
];

type VitrineCard_ = { id: number; name: string; ini: string; rarity: string };
const VITRINE_DETAIL_CARDS: Record<string, VitrineCard_[]> = {
  "ProPlayer_99": [
    { id: 1, name: "Haaland",      ini: "EH",  rarity: "Mítica"     },
    { id: 2, name: "Mbappé",       ini: "KM",  rarity: "Rara"       },
    { id: 3, name: "De Bruyne",    ini: "KDB", rarity: "Rara"       },
    { id: 4, name: "Salah",        ini: "MS",  rarity: "Poco común" },
    { id: 5, name: "Pedri",        ini: "PE",  rarity: "Rara"       },
    { id: 6, name: "Rodri",        ini: "RO",  rarity: "Común"      },
  ],
  "FutbolFan_22": [
    { id: 1, name: "Vinicius Jr.", ini: "VJ",  rarity: "Rara"       },
    { id: 2, name: "Bellingham",   ini: "JB",  rarity: "Poco común" },
    { id: 3, name: "Musiala",      ini: "JM",  rarity: "Común"      },
    { id: 4, name: "Osimhen",      ini: "VO",  rarity: "Poco común" },
  ],
  "CardMaster_X": [
    { id: 1, name: "Luis Díaz",    ini: "LD",  rarity: "Mítica"     },
    { id: 2, name: "Lamine Yamal", ini: "LY",  rarity: "Mítica"     },
    { id: 3, name: "Pedri",        ini: "PE",  rarity: "Rara"       },
    { id: 4, name: "Haaland",      ini: "EH",  rarity: "Rara"       },
    { id: 5, name: "De Bruyne",    ini: "KDB", rarity: "Poco común" },
    { id: 6, name: "Bellingham",   ini: "JB",  rarity: "Común"      },
  ],
  "GoldenShot_7": [
    { id: 1, name: "Mbappé",       ini: "KM",  rarity: "Rara"       },
    { id: 2, name: "Salah",        ini: "MS",  rarity: "Rara"       },
    { id: 3, name: "Rodri",        ini: "RO",  rarity: "Poco común" },
    { id: 4, name: "Osimhen",      ini: "VO",  rarity: "Común"      },
  ],
  "MiAmigo_01": [
    { id: 1, name: "Musiala",      ini: "JM",  rarity: "Común"      },
    { id: 2, name: "Bellingham",   ini: "JB",  rarity: "Rara"       },
    { id: 3, name: "Osimhen",      ini: "VO",  rarity: "Poco común" },
  ],
  "ElChampion": [
    { id: 1, name: "Vinicius Jr.", ini: "VJ",  rarity: "Mítica"     },
    { id: 2, name: "Pedri",        ini: "PE",  rarity: "Rara"       },
    { id: 3, name: "Haaland",      ini: "EH",  rarity: "Común"      },
    { id: 4, name: "Rodri",        ini: "RO",  rarity: "Poco común" },
  ],
};

function MiniCard({ rarity }: { rarity: string }) {
  const rl = RARITY[rarity];
  const isMythic = rarity === "Mítica";
  const inner = (
    <div style={{ width: "100%", height: "100%", background: "#090f0c", borderRadius: isMythic ? 3 : 4 }} />
  );
  return isMythic ? (
    <div style={{ width: 28, aspectRatio: "3/4", background: rl.gradient, borderRadius: 5, padding: "1.5px", flexShrink: 0 }}>
      {inner}
    </div>
  ) : (
    <div style={{ width: 28, aspectRatio: "3/4", border: `1.5px solid ${rl.border}`, borderRadius: 5, flexShrink: 0 }}>
      {inner}
    </div>
  );
}

function VitrineCard({ user, avatar, cards, likes, onClick }: { user: string; avatar: string; cards: string[]; likes: number; onClick?: () => void }) {
  return (
    <div onClick={onClick} style={{
      background: CARD_BG,
      border: `1px solid ${BORDER_SUBTLE}`,
      borderRadius: 9,
      padding: "12px 12px 10px",
      display: "flex", flexDirection: "column", gap: 10,
      cursor: "pointer",
    }}>
      {/* User info */}
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <div style={{
          width: 30, height: 30, borderRadius: "50%", flexShrink: 0,
          background: "rgba(255,255,255,0.07)",
          border: `1px solid ${BORDER_SUBTLE}`,
          display: "flex", alignItems: "center", justifyContent: "center",
        }}>
          <span style={{ fontSize: 9, fontWeight: 700, color: TEXT_GRAY, letterSpacing: "0.04em" }}>{avatar}</span>
        </div>
        <span style={{
          fontSize: 11, fontWeight: 500, color: TEXT_WHITE,
          overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap",
        }}>{user}</span>
      </div>

      {/* Card previews */}
      <div style={{ display: "flex", gap: 5, justifyContent: "center" }}>
        {cards.map((r, i) => <MiniCard key={i} rarity={r} />)}
      </div>

      {/* Likes */}
      <div style={{ display: "flex", justifyContent: "flex-end", alignItems: "center", gap: 4 }}>
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none"
          stroke={TEXT_DIM} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
          <path d="M14 9V5a3 3 0 00-3-3l-4 9v11h11.28a2 2 0 002-1.7l1.38-9a2 2 0 00-2-2.3H14z" />
          <path d="M7 22H4a2 2 0 01-2-2v-7a2 2 0 012-2h3" />
        </svg>
        <span style={{ fontSize: 11, color: TEXT_DIM }}>{likes}</span>
      </div>
    </div>
  );
}

// ── Card zoom + holographic modal ────────────────────────────────────────

function CardZoomModal({ card, onClose }: { card: VitrineCard_; onClose: () => void }) {
  const rl = RARITY[card.rarity];
  const isMythic = card.rarity === "Mítica";
  const W = 224;
  const H = 314;

  return (
    <div
      onClick={onClose}
      style={{
        position: "fixed", inset: 0, zIndex: 90,
        background: "rgba(2,8,5,0.90)",
        backdropFilter: "blur(10px)", WebkitBackdropFilter: "blur(10px)",
        display: "flex", alignItems: "center", justifyContent: "center",
      }}
    >
      <div onClick={(e) => e.stopPropagation()} style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 16 }}>

        {/* Close button */}
        <div style={{ width: W, display: "flex", justifyContent: "flex-end" }}>
          <button onClick={onClose} style={{ background: "none", border: "none", cursor: "pointer", padding: 4 }}>
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.40)" strokeWidth="1.8" strokeLinecap="round">
              <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        {/* Card */}
        {isMythic ? (
          <div style={{
            width: W, height: H,
            background: rl.gradient, borderRadius: 14, padding: "2px",
            boxShadow: `0 0 48px rgba(232,168,32,0.35), 0 0 96px rgba(249,115,22,0.18)`,
          }}>
            <div style={{
              width: "100%", height: "100%",
              background: "#060d09", borderRadius: 13,
              position: "relative", overflow: "hidden",
              display: "flex", flexDirection: "column",
              alignItems: "center", justifyContent: "center", gap: 6,
            }}>
              {/* Holographic rainbow overlay */}
              <div className="holo-rainbow" style={{ position: "absolute", inset: 0, borderRadius: 13 }} />
              {/* Glare sweep */}
              <div className="holo-glare" />
              {/* Sparkle badge */}
              <svg style={{
                position: "absolute", top: 12, right: 12, zIndex: 3,
                filter: `drop-shadow(0 0 6px ${GOLD})`,
              }} width="16" height="16" viewBox="0 0 24 24" fill={GOLD}>
                <path d="M12 2l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 17l-6.2 4.3 2.4-7.4L2 9.4h7.6z" />
              </svg>
              {/* Content above overlays */}
              <span style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800, fontSize: 58,
                color: "rgba(255,255,255,0.14)", letterSpacing: "0.04em",
                position: "relative", zIndex: 2, lineHeight: 1,
              }}>{card.ini}</span>
              <span style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 11,
                color: GOLD, letterSpacing: "0.14em", textTransform: "uppercase",
                position: "relative", zIndex: 2,
              }}>{card.rarity}</span>
              <span style={{
                fontSize: 13, color: "rgba(255,255,255,0.55)",
                position: "relative", zIndex: 2, textAlign: "center", padding: "0 20px",
              }}>{card.name}</span>
            </div>
          </div>
        ) : (
          <div style={{
            width: W, height: H,
            border: `2px solid ${rl.border}`,
            borderRadius: 14, background: "#060d09",
            boxShadow: `0 0 32px ${rl.border}28`,
            display: "flex", flexDirection: "column",
            alignItems: "center", justifyContent: "center", gap: 6,
          }}>
            <span style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800, fontSize: 58,
              color: "rgba(255,255,255,0.14)", letterSpacing: "0.04em", lineHeight: 1,
            }}>{card.ini}</span>
            <span style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 11,
              color: rl.label, letterSpacing: "0.14em", textTransform: "uppercase",
            }}>{card.rarity}</span>
            <span style={{ fontSize: 13, color: "rgba(255,255,255,0.50)", textAlign: "center", padding: "0 20px" }}>
              {card.name}
            </span>
          </div>
        )}

        {/* Name below */}
        <span style={{ fontSize: 13, color: TEXT_DIM }}>Toca fuera para cerrar</span>
      </div>
    </div>
  );
}

// ── Vitrina detail screen ─────────────────────────────────────────────────

type VitrineEntry = { user: string; avatar: string; cards: string[]; likes: number };

function VitrineDetailScreen({ vitrine, onBack }: { vitrine: VitrineEntry; onBack: () => void }) {
  const [liked, setLiked] = useState(false);
  const [likeCount, setLikeCount] = useState(vitrine.likes);
  const [zoomedCard, setZoomedCard] = useState<VitrineCard_ | null>(null);

  const detailCards: VitrineCard_[] =
    VITRINE_DETAIL_CARDS[vitrine.user] ??
    vitrine.cards.map((r, i) => ({ id: i, name: "—", ini: "?", rarity: r }));

  function handleLike() {
    setLiked((prev) => {
      setLikeCount((c) => (prev ? c - 1 : c + 1));
      return !prev;
    });
  }

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", justifyContent: "space-between",
        flexShrink: 0,
      }}>
        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
          <div style={{
            width: 46, height: 46, borderRadius: "50%", flexShrink: 0,
            background: "rgba(255,255,255,0.07)",
            border: `1.5px solid ${BORDER_SUBTLE}`,
            display: "flex", alignItems: "center", justifyContent: "center",
          }}>
            <span style={{ fontSize: 13, fontWeight: 700, color: TEXT_GRAY, letterSpacing: "0.04em" }}>{vitrine.avatar}</span>
          </div>
          <div>
            <div style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
              fontSize: 20, letterSpacing: "0.07em", textTransform: "uppercase", color: TEXT_WHITE,
            }}>{vitrine.user}</div>
            <div style={{ fontSize: 11, color: TEXT_DIM, marginTop: 1 }}>Vitrina pública · {detailCards.length} cartas</div>
          </div>
        </div>
        <button onClick={onBack} style={{ background: "none", border: "none", cursor: "pointer", padding: 4 }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.40)" strokeWidth="1.8" strokeLinecap="round">
            <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
          </svg>
        </button>
      </div>

      {/* Separator */}
      <div style={{ height: 1, background: BORDER_SUBTLE, flexShrink: 0, margin: "0 16px" }} />

      {/* Card grid */}
      <div className="card-grid-scroll" style={{ flex: 1, overflowY: "auto", padding: "16px 16px", paddingBottom: 120 }}>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
          {detailCards.map((card) => {
            const rl = RARITY[card.rarity];
            const isMythic = card.rarity === "Mítica";
            return (
              <div
                key={card.id}
                onClick={() => setZoomedCard(card)}
                style={{ cursor: "pointer", aspectRatio: "3/4.2" }}
              >
                {isMythic ? (
                  <div style={{ width: "100%", height: "100%", background: rl.gradient, borderRadius: 9, padding: "1.5px" }}>
                    <div style={{
                      width: "100%", height: "100%", background: CARD_BG, borderRadius: 8,
                      display: "flex", flexDirection: "column", position: "relative",
                      alignItems: "center", justifyContent: "center", gap: 4,
                    }}>
                      {/* Holographic indicator */}
                      <svg style={{ position: "absolute", top: 6, right: 6, filter: `drop-shadow(0 0 4px ${GOLD})` }}
                        width="11" height="11" viewBox="0 0 24 24" fill={GOLD}>
                        <path d="M12 2l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 17l-6.2 4.3 2.4-7.4L2 9.4h7.6z" />
                      </svg>
                      <span style={{
                        fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800,
                        fontSize: 30, color: "rgba(255,255,255,0.18)", letterSpacing: "0.04em", lineHeight: 1,
                      }}>{card.ini}</span>
                      <span style={{
                        fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                        fontSize: 8, color: GOLD, letterSpacing: "0.12em", textTransform: "uppercase",
                      }}>{card.rarity}</span>
                      <span style={{ fontSize: 10, color: "rgba(255,255,255,0.38)", textAlign: "center", padding: "0 8px" }}>{card.name}</span>
                    </div>
                  </div>
                ) : (
                  <div style={{
                    width: "100%", height: "100%",
                    border: `1.5px solid ${rl.border}`, borderRadius: 9, background: CARD_BG,
                    display: "flex", flexDirection: "column",
                    alignItems: "center", justifyContent: "center", gap: 4,
                  }}>
                    <span style={{
                      fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800,
                      fontSize: 30, color: "rgba(255,255,255,0.18)", letterSpacing: "0.04em", lineHeight: 1,
                    }}>{card.ini}</span>
                    <span style={{
                      fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                      fontSize: 8, color: rl.label, letterSpacing: "0.12em", textTransform: "uppercase",
                    }}>{card.rarity}</span>
                    <span style={{ fontSize: 10, color: "rgba(255,255,255,0.38)", textAlign: "center", padding: "0 8px" }}>{card.name}</span>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Floating like pill */}
      <div style={{ position: "fixed", bottom: 104, right: 20, zIndex: 35 }}>
        <button
          onClick={handleLike}
          style={{
            display: "flex", alignItems: "center", gap: 8,
            background: liked ? GOLD : "rgba(10,24,16,0.86)",
            border: `1.5px solid ${liked ? GOLD : GOLD_BORDER}`,
            borderRadius: 999, padding: "10px 18px 10px 14px",
            cursor: "pointer",
            backdropFilter: "blur(16px)", WebkitBackdropFilter: "blur(16px)",
            boxShadow: liked
              ? `0 0 24px rgba(232,168,32,0.50), 0 4px 16px rgba(0,0,0,0.40)`
              : `0 4px 20px rgba(0,0,0,0.45)`,
            transition: "all 0.18s ease",
          }}
        >
          <svg width="18" height="18" viewBox="0 0 24 24"
            fill={liked ? "#0d1a13" : "none"}
            stroke={liked ? "#0d1a13" : GOLD}
            strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <path d="M14 9V5a3 3 0 00-3-3l-4 9v11h11.28a2 2 0 002-1.7l1.38-9a2 2 0 00-2-2.3H14z" />
            <path d="M7 22H4a2 2 0 01-2-2v-7a2 2 0 012-2h3" />
          </svg>
          <span style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 17, letterSpacing: "0.06em",
            color: liked ? "#0d1a13" : GOLD,
          }}>{likeCount}</span>
        </button>
      </div>

      {/* Fullscreen card zoom */}
      {zoomedCard && <CardZoomModal card={zoomedCard} onClose={() => setZoomedCard(null)} />}
    </>
  );
}

function VitrinesScreen({ onBack }: { onBack: () => void }) {
  const [query, setQuery] = useState("");
  const [selectedVitrine, setSelectedVitrine] = useState<VitrineEntry | null>(null);

  if (selectedVitrine) {
    return <VitrineDetailScreen vitrine={selectedVitrine} onBack={() => setSelectedVitrine(null)} />;
  }

  const filterVitrinas = (list: typeof VITRINAS_POPULARES) =>
    query.trim()
      ? list.filter((v) => v.user.toLowerCase().includes(query.toLowerCase()))
      : list;

  return (
    <>
      {/* Header with back */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", gap: 12,
        flexShrink: 0,
      }}>
        <button onClick={onBack} style={{ background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0 }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="15,18 9,12 15,6" />
          </svg>
        </button>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 24,
          letterSpacing: "0.10em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
        }}>Vitrinas públicas</h1>
      </div>

      {/* Search */}
      <div style={{ padding: "0 16px", marginBottom: 4, flexShrink: 0 }}>
        <div style={{
          display: "flex", alignItems: "center", gap: 8,
          background: "rgba(255,255,255,0.05)",
          border: `1px solid ${BORDER_SUBTLE}`,
          borderRadius: 8, padding: "10px 12px",
        }}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.30)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="11" cy="11" r="7" /><line x1="16.5" y1="16.5" x2="21" y2="21" />
          </svg>
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Busca por usuario o código de amigo…"
            style={{
              background: "none", border: "none", outline: "none",
              color: TEXT_WHITE, fontSize: 13, flex: 1,
              caretColor: GOLD,
            }}
          />
          {query && (
            <button onClick={() => setQuery("")} style={{ background: "none", border: "none", cursor: "pointer", padding: 0 }}>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.30)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          )}
        </div>
      </div>

      {/* Scroll content */}
      <div className="card-grid-scroll" style={{ flex: 1, overflowY: "auto", padding: "16px 16px", paddingBottom: 108 }}>

        {/* POPULARES */}
        <section style={{ marginBottom: 28 }}>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
            margin: 0, marginBottom: 12,
          }}>Populares</h2>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
            {filterVitrinas(VITRINAS_POPULARES).map((v) => (
              <VitrineCard key={v.user} {...v} onClick={() => setSelectedVitrine(v)} />
            ))}
            {filterVitrinas(VITRINAS_POPULARES).length === 0 && (
              <span style={{ fontSize: 13, color: TEXT_DIM, gridColumn: "span 2" }}>Sin resultados</span>
            )}
          </div>
        </section>

        {/* AMIGOS */}
        <section>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
            margin: 0, marginBottom: 12,
          }}>Amigos</h2>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
            {filterVitrinas(VITRINAS_AMIGOS).map((v) => (
              <VitrineCard key={v.user} {...v} onClick={() => setSelectedVitrine(v)} />
            ))}
            {filterVitrinas(VITRINAS_AMIGOS).length === 0 && (
              <span style={{ fontSize: 13, color: TEXT_DIM, gridColumn: "span 2" }}>Sin resultados</span>
            )}
          </div>
        </section>

      </div>
    </>
  );
}

// ── Screen: Perfil ───────────────────────────────────────────────────────

// Tactical formation: x/y = % within the pitch container (top=attack, bottom=defense)
const FORMATION_SLOTS = [
  // FWD (3)
  { id: "f1", pos: "DEL", rarity: "Rara",        x: "22%", y: "11%" },
  { id: "f2", pos: "DEL", rarity: null,           x: "50%", y: "11%" },
  { id: "f3", pos: "DEL", rarity: null,           x: "78%", y: "11%" },
  // MID (3)
  { id: "m1", pos: "MED", rarity: "Mítica",       x: "18%", y: "35%" },
  { id: "m2", pos: "MED", rarity: null,           x: "50%", y: "35%" },
  { id: "m3", pos: "MED", rarity: "Común",        x: "82%", y: "35%" },
  // DEF (4)
  { id: "d1", pos: "DEF", rarity: "Poco común",   x: "11%", y: "62%" },
  { id: "d2", pos: "DEF", rarity: null,           x: "36%", y: "62%" },
  { id: "d3", pos: "DEF", rarity: null,           x: "64%", y: "62%" },
  { id: "d4", pos: "DEF", rarity: "Rara",         x: "89%", y: "62%" },
  // GK (1)
  { id: "g1", pos: "POR", rarity: null,           x: "50%", y: "86%" },
];

const FEATURED_SLOTS: Array<{ rarity: string | null; ini: string | null; name: string | null }> = [
  { rarity: "Mítica",  ini: "LD",  name: "Luis Díaz" },
  { rarity: "Rara",    ini: "JB",  name: "Bellingham" },
  { rarity: null,      ini: null,  name: null },
];

// ── Screen: Intercambio ──────────────────────────────────────────────────

type TradeCard_ = { ini: string; rarity: string };
type Trade = {
  id: number;
  user: string;
  avatar: string;
  youGive: TradeCard_[];
  youReceive: TradeCard_[];
  time: string;
  unread?: boolean;
};

const TRADES_RECEIVED: Trade[] = [
  {
    id: 1, user: "MiAmigo_01", avatar: "MA", time: "hace 2 h", unread: true,
    youGive:    [{ ini: "EH", rarity: "Mítica" }, { ini: "KM", rarity: "Rara" }],
    youReceive: [{ ini: "LD", rarity: "Mítica" }],
  },
  {
    id: 2, user: "ElChampion", avatar: "EC", time: "hace 1 d", unread: true,
    youGive:    [{ ini: "VJ", rarity: "Rara" }],
    youReceive: [{ ini: "PE", rarity: "Rara" }, { ini: "RO", rarity: "Común" }],
  },
  {
    id: 3, user: "ProPlayer_99", avatar: "PP", time: "hace 3 d", unread: false,
    youGive:    [{ ini: "JB", rarity: "Poco común" }, { ini: "VO", rarity: "Común" }],
    youReceive: [{ ini: "MS", rarity: "Poco común" }],
  },
];

const TRADES_SENT: Trade[] = [
  {
    id: 4, user: "GoldenShot_7", avatar: "GS", time: "hace 5 h",
    youGive:    [{ ini: "LY", rarity: "Mítica" }],
    youReceive: [{ ini: "KM", rarity: "Rara" }, { ini: "JB", rarity: "Rara" }],
  },
];

function TradeCardPreview({ rarity }: { rarity: string }) {
  const rl = RARITY[rarity];
  const isMythic = rarity === "Mítica";
  return isMythic ? (
    <div style={{ width: 34, aspectRatio: "3/4", background: rl.gradient, borderRadius: 5, padding: "1.5px", flexShrink: 0 }}>
      <div style={{ width: "100%", height: "100%", background: "#090f0c", borderRadius: 4,
        display: "flex", alignItems: "center", justifyContent: "center" }}>
        <span style={{ fontSize: 8, fontWeight: 800, color: "rgba(255,255,255,0.18)", letterSpacing: "0.04em" }}>
          {rarity.slice(0, 2).toUpperCase()}
        </span>
      </div>
    </div>
  ) : (
    <div style={{ width: 34, aspectRatio: "3/4", border: `1.5px solid ${rl.border}`, borderRadius: 5,
      background: "#090f0c", flexShrink: 0,
      display: "flex", alignItems: "center", justifyContent: "center" }}>
      <span style={{ fontSize: 8, fontWeight: 800, color: rl.border, letterSpacing: "0.04em", opacity: 0.7 }}>
        {rarity.slice(0, 2).toUpperCase()}
      </span>
    </div>
  );
}

function SwapIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
      stroke="rgba(255,255,255,0.22)" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 4l4 4-4 4" />
      <path d="M3 8h18" />
      <path d="M7 20l-4-4 4-4" />
      <path d="M21 16H3" />
    </svg>
  );
}

function TradeOfferCard({ trade, mode, onAccept, onReject, onCancel }: {
  trade: Trade;
  mode: "received" | "sent";
  onAccept?: () => void;
  onReject?: () => void;
  onCancel?: () => void;
}) {
  return (
    <div style={{
      background: CARD_BG,
      border: `1px solid ${trade.unread && mode === "received" ? "rgba(232,168,32,0.28)" : BORDER_SUBTLE}`,
      borderRadius: 10,
      padding: "12px 12px 10px",
      display: "flex", flexDirection: "column", gap: 12,
      position: "relative",
    }}>
      {/* Unread dot */}
      {trade.unread && mode === "received" && (
        <div style={{
          position: "absolute", top: 12, right: 12,
          width: 7, height: 7, borderRadius: "50%",
          background: GOLD, boxShadow: `0 0 6px ${GOLD}`,
        }} />
      )}

      {/* Header */}
      <div style={{ display: "flex", alignItems: "center", gap: 9 }}>
        <div style={{
          width: 32, height: 32, borderRadius: "50%", flexShrink: 0,
          background: "rgba(255,255,255,0.06)",
          border: `1px solid ${BORDER_SUBTLE}`,
          display: "flex", alignItems: "center", justifyContent: "center",
        }}>
          <span style={{ fontSize: 9, fontWeight: 700, color: TEXT_GRAY, letterSpacing: "0.04em" }}>{trade.avatar}</span>
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <span style={{ fontSize: 13, fontWeight: 600, color: TEXT_WHITE, display: "block",
            overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
            {trade.user}
          </span>
        </div>
        <span style={{ fontSize: 10, color: TEXT_DIM, flexShrink: 0 }}>{trade.time}</span>
      </div>

      {/* Exchange row */}
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        {/* You give */}
        <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 5 }}>
          <span style={{ fontSize: 9, fontWeight: 600, color: TEXT_DIM, letterSpacing: "0.06em", textTransform: "uppercase" }}>
            Tú das
          </span>
          <div style={{ display: "flex", gap: 4 }}>
            {trade.youGive.map((c, i) => <TradeCardPreview key={i} rarity={c.rarity} />)}
          </div>
        </div>

        {/* Swap icon */}
        <div style={{ flexShrink: 0, paddingTop: 14 }}>
          <SwapIcon />
        </div>

        {/* You receive */}
        <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 5, alignItems: "flex-end" }}>
          <span style={{ fontSize: 9, fontWeight: 600, color: TEXT_DIM, letterSpacing: "0.06em", textTransform: "uppercase" }}>
            Tú recibes
          </span>
          <div style={{ display: "flex", gap: 4 }}>
            {trade.youReceive.map((c, i) => <TradeCardPreview key={i} rarity={c.rarity} />)}
          </div>
        </div>
      </div>

      {/* Action buttons */}
      <div style={{ display: "flex", gap: 8, marginTop: 2 }}>
        {mode === "received" ? (
          <>
            <button
              onClick={onAccept}
              style={{
                flex: 1, padding: "9px 0",
                background: GOLD, border: "none", borderRadius: 7,
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 13, letterSpacing: "0.08em", textTransform: "uppercase",
                color: "#0d1a13", cursor: "pointer",
              }}
            >Aceptar</button>
            <button
              onClick={onReject}
              style={{
                flex: 1, padding: "9px 0",
                background: "transparent",
                border: `1px solid ${BORDER_SUBTLE}`, borderRadius: 7,
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 13, letterSpacing: "0.08em", textTransform: "uppercase",
                color: TEXT_GRAY, cursor: "pointer",
              }}
            >Rechazar</button>
          </>
        ) : (
          <button
            onClick={onCancel}
            style={{
              flex: 1, padding: "9px 0",
              background: "transparent",
              border: `1px solid rgba(255,255,255,0.15)`, borderRadius: 7,
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
              fontSize: 13, letterSpacing: "0.08em", textTransform: "uppercase",
              color: TEXT_GRAY, cursor: "pointer",
            }}
          >Cancelar oferta</button>
        )}
      </div>
    </div>
  );
}

function TradeEmptyState({ mode }: { mode: "received" | "sent" }) {
  return (
    <div style={{
      display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center",
      padding: "48px 24px", gap: 14, textAlign: "center",
    }}>
      <svg width="40" height="40" viewBox="0 0 24 24" fill="none"
        stroke="rgba(255,255,255,0.15)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
        <path d="M17 4l4 4-4 4" />
        <path d="M3 8h18" />
        <path d="M7 20l-4-4 4-4" />
        <path d="M21 16H3" />
      </svg>
      <div>
        <p style={{ fontSize: 14, fontWeight: 600, color: "rgba(255,255,255,0.35)", margin: 0 }}>
          {mode === "received"
            ? "Aún no tienes intercambios."
            : "No has enviado ninguna oferta."}
        </p>
        <p style={{ fontSize: 12, color: TEXT_DIM, margin: "6px 0 0", lineHeight: 1.5 }}>
          {mode === "received"
            ? "Cuando un amigo te proponga un trato, aparecerá aquí."
            : "Propón un intercambio a un amigo y empieza a negociar."}
        </p>
      </div>
    </div>
  );
}

function TradeScreen({ onBack }: { onBack: () => void }) {
  const [activeTab, setActiveTab] = useState<"received" | "sent">("received");
  const [received, setReceived] = useState(TRADES_RECEIVED);
  const [sent, setSent]         = useState(TRADES_SENT);

  const unreadCount = received.filter((t) => t.unread).length;

  function markRead(id: number) {
    setReceived((prev) => prev.map((t) => t.id === id ? { ...t, unread: false } : t));
  }
  function removeTrade(id: number) {
    setReceived((prev) => prev.filter((t) => t.id !== id));
  }
  function removeSent(id: number) {
    setSent((prev) => prev.filter((t) => t.id !== id));
  }

  const list = activeTab === "received" ? received : sent;

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", gap: 12, flexShrink: 0,
      }}>
        <button onClick={onBack} style={{ background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0 }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="15,18 9,12 15,6" />
          </svg>
        </button>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 24,
          letterSpacing: "0.10em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
        }}>Intercambio</h1>
      </div>

      {/* Tab chips */}
      <div style={{ padding: "0 16px", display: "flex", gap: 8, flexShrink: 0, marginBottom: 16 }}>
        {(["received", "sent"] as const).map((tab) => {
          const active = activeTab === tab;
          const label  = tab === "received" ? "Recibidas" : "Enviadas";
          const badge  = tab === "received" && unreadCount > 0 ? unreadCount : null;
          return (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              style={{
                display: "flex", alignItems: "center", gap: 7,
                padding: "8px 16px",
                borderRadius: 999,
                background: active ? GOLD : "transparent",
                border: `1.5px solid ${active ? GOLD : BORDER_SUBTLE}`,
                cursor: "pointer", transition: "all 0.15s ease",
              }}
            >
              <span style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 13, letterSpacing: "0.08em", textTransform: "uppercase",
                color: active ? "#0d1a13" : TEXT_GRAY,
              }}>{label}</span>
              {badge !== null && (
                <span style={{
                  minWidth: 18, height: 18, borderRadius: 999,
                  background: active ? "#0d1a13" : "rgba(0,0,0,0.55)",
                  border: active ? "none" : `1px solid ${GOLD_BORDER}`,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  padding: "0 4px",
                  fontSize: 10, fontWeight: 700,
                  color: active ? GOLD : GOLD,
                }}>{badge}</span>
              )}
            </button>
          );
        })}
      </div>

      {/* Trade list */}
      <div className="card-grid-scroll" style={{ flex: 1, overflowY: "auto", padding: "0 16px", paddingBottom: 120 }}>
        {list.length === 0 ? (
          <TradeEmptyState mode={activeTab} />
        ) : (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {list.map((trade) => (
              <TradeOfferCard
                key={trade.id}
                trade={trade}
                mode={activeTab}
                onAccept={() => { markRead(trade.id); removeTrade(trade.id); }}
                onReject={() => removeTrade(trade.id)}
                onCancel={() => removeSent(trade.id)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Floating CTA */}
      <div style={{ position: "fixed", bottom: 104, right: 20, zIndex: 35 }}>
        <button style={{
          display: "flex", alignItems: "center", gap: 8,
          padding: "11px 20px",
          background: GOLD, border: "none", borderRadius: 999,
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
          fontSize: 15, letterSpacing: "0.07em", textTransform: "uppercase",
          color: "#0d1a13", cursor: "pointer",
          boxShadow: `0 4px 20px rgba(232,168,32,0.40), 0 2px 8px rgba(0,0,0,0.35)`,
        }}>
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none"
            stroke="#0d1a13" strokeWidth="2.2" strokeLinecap="round">
            <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Nuevo intercambio
        </button>
      </div>
    </>
  );
}

// ── Screen: Vender duplicados ─────────────────────────────────────────────

const SELL_VALUE: Record<string, number> = {
  "Común": 10,
  "Poco común": 25,
  "Rara": 75,
  "Mítica": 200,
};

const DAILY_MAX = 10;
const DAILY_USED_INIT = 3;

function SellScreen({ onBack }: { onBack: () => void }) {
  const duplicates = CARDS.filter((c) => c.count >= 2);
  const [selected, setSelected]       = useState<Set<number>>(new Set());
  const [cards, setCards]             = useState(duplicates);
  const [dailyUsed, setDailyUsed]     = useState(DAILY_USED_INIT);
  const [showConfirm, setShowConfirm] = useState(false);

  const remaining  = DAILY_MAX - dailyUsed;
  const limitFull  = remaining <= 0;
  const totalCoins = [...selected].reduce((sum, id) => {
    const card = cards.find((c) => c.id === id);
    return sum + (card ? SELL_VALUE[card.rarity] ?? 0 : 0);
  }, 0);
  const selectedCount = selected.size;

  function toggleCard(id: number) {
    if (limitFull) return;
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else {
        if (next.size >= remaining) return prev;
        next.add(id);
      }
      return next;
    });
  }

  function confirmSell() {
    const soldCount = selected.size;
    setCards((prev) => prev.filter((c) => !selected.has(c.id)));
    setDailyUsed((d) => d + soldCount);
    setSelected(new Set());
    setShowConfirm(false);
  }

  const dailyPct = Math.min((dailyUsed / DAILY_MAX) * 100, 100);

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", gap: 12, flexShrink: 0,
      }}>
        <button onClick={onBack} style={{ background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0 }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="15,18 9,12 15,6" />
          </svg>
        </button>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 24,
          letterSpacing: "0.10em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
        }}>Vender duplicados</h1>
      </div>

      {/* Daily limit bar */}
      <div style={{
        margin: "0 16px 16px",
        background: CARD_BG, border: `1px solid ${limitFull ? "rgba(232,168,32,0.30)" : BORDER_SUBTLE}`,
        borderRadius: 8, padding: "12px 14px 14px", flexShrink: 0,
      }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", marginBottom: 9 }}>
          <span style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 13,
            letterSpacing: "0.10em", textTransform: "uppercase",
            color: limitFull ? GOLD : TEXT_WHITE,
          }}>Ventas hoy</span>
          <span style={{ fontSize: 12, color: limitFull ? GOLD : TEXT_GRAY }}>
            {dailyUsed} / {DAILY_MAX}
          </span>
        </div>
        <div style={{ height: 5, borderRadius: 999, background: "rgba(255,255,255,0.09)", overflow: "hidden" }}>
          <div style={{
            height: "100%", borderRadius: 999,
            width: `${dailyPct}%`,
            background: limitFull ? GOLD : `linear-gradient(to right, rgba(232,168,32,0.60), ${GOLD})`,
            transition: "width 0.4s ease",
          }} />
        </div>
        {limitFull && (
          <p style={{ fontSize: 11, color: TEXT_GRAY, margin: "8px 0 0", lineHeight: 1.45 }}>
            Alcanzaste el límite de ventas de hoy. Vuelve mañana para seguir vendiendo.
          </p>
        )}
        {!limitFull && (
          <p style={{ fontSize: 11, color: TEXT_DIM, margin: "7px 0 0" }}>
            Puedes vender {remaining} carta{remaining !== 1 ? "s" : ""} más hoy.
          </p>
        )}
      </div>

      {/* Content */}
      {cards.length === 0 ? (
        /* Empty state */
        <div style={{
          flex: 1, display: "flex", flexDirection: "column",
          alignItems: "center", justifyContent: "center",
          padding: "40px 32px", gap: 14, textAlign: "center",
        }}>
          <svg width="44" height="44" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.14)" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round">
            <path d="M20.59 13.41l-7.17 7.17a2 2 0 01-2.83 0L2 12V2h10l8.59 8.59a2 2 0 010 2.82z" />
            <circle cx="7" cy="7" r="1.5" fill="rgba(255,255,255,0.14)" stroke="none" />
          </svg>
          <div>
            <p style={{ fontSize: 14, fontWeight: 600, color: "rgba(255,255,255,0.30)", margin: 0 }}>
              No tienes cartas duplicadas por ahora.
            </p>
            <p style={{ fontSize: 12, color: TEXT_DIM, margin: "6px 0 0", lineHeight: 1.5 }}>
              Abre más sobres para conseguir duplicados y obtener monedas extra.
            </p>
          </div>
        </div>
      ) : (
        /* Card grid */
        <div className="card-grid-scroll" style={{
          flex: 1, overflowY: "auto",
          padding: "0 16px",
          paddingBottom: selectedCount > 0 ? 160 : 120,
        }}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
            {cards.map((card) => {
              const rl = RARITY[card.rarity];
              const isMythic  = card.rarity === "Mítica";
              const isSelected = selected.has(card.id);
              const sellVal   = SELL_VALUE[card.rarity] ?? 0;
              const disabled  = limitFull || (!isSelected && selected.size >= remaining);

              const borderColor = isSelected ? GOLD : rl.border;

              return (
                <div
                  key={card.id}
                  onClick={() => !disabled && toggleCard(card.id)}
                  style={{
                    aspectRatio: "3/4.2",
                    cursor: disabled ? "default" : "pointer",
                    opacity: disabled && !isSelected ? 0.42 : 1,
                    transition: "opacity 0.15s",
                  }}
                >
                  {isMythic && !isSelected ? (
                    /* Gradient border wrapper for unselected Mítica */
                    <div style={{
                      width: "100%", height: "100%",
                      background: rl.gradient, borderRadius: 9, padding: "1.5px",
                    }}>
                      <div style={{
                        width: "100%", height: "100%",
                        background: CARD_BG, borderRadius: 8,
                        position: "relative",
                        display: "flex", flexDirection: "column",
                        alignItems: "center", justifyContent: "center", gap: 4,
                      }}>
                        {/* Count badge */}
                        <div style={{
                          position: "absolute", top: 6, left: 6,
                          minWidth: 18, height: 18, borderRadius: 4,
                          background: "rgba(0,0,0,0.55)", display: "flex",
                          alignItems: "center", justifyContent: "center", padding: "0 4px",
                        }}>
                          <span style={{ fontSize: 9, fontWeight: 700, color: GOLD }}>×{card.count}</span>
                        </div>
                        {/* Coin overlay */}
                        <div style={{
                          position: "absolute", bottom: 6, right: 6,
                          display: "flex", alignItems: "center", gap: 3,
                          background: "rgba(0,0,0,0.60)", borderRadius: 4, padding: "2px 5px",
                        }}>
                          <CoinIcon size={10} />
                          <span style={{ fontSize: 9, fontWeight: 700, color: GOLD }}>{sellVal}</span>
                        </div>
                        <span style={{
                          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800,
                          fontSize: 28, color: "rgba(255,255,255,0.18)", lineHeight: 1,
                        }}>{card.ini}</span>
                        <span style={{ fontSize: 9, color: GOLD, fontWeight: 700, letterSpacing: "0.10em", textTransform: "uppercase" }}>
                          {card.rarity}
                        </span>
                        <span style={{ fontSize: 10, color: "rgba(255,255,255,0.38)", textAlign: "center", padding: "0 8px" }}>
                          {card.name}
                        </span>
                      </div>
                    </div>
                  ) : (
                    /* Solid border (selected state or non-Mítica) */
                    <div style={{
                      width: "100%", height: "100%",
                      border: `${isSelected ? "2px" : "1.5px"} solid ${borderColor}`,
                      borderRadius: 9,
                      background: isSelected ? "rgba(232,168,32,0.06)" : CARD_BG,
                      position: "relative",
                      display: "flex", flexDirection: "column",
                      alignItems: "center", justifyContent: "center", gap: 4,
                      transition: "border-color 0.15s, background 0.15s",
                    }}>
                      {/* Checkmark when selected */}
                      {isSelected && (
                        <div style={{
                          position: "absolute", top: 6, left: 6,
                          width: 18, height: 18, borderRadius: 4,
                          background: GOLD, display: "flex",
                          alignItems: "center", justifyContent: "center",
                        }}>
                          <svg width="11" height="11" viewBox="0 0 24 24" fill="none"
                            stroke="#0d1a13" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                            <polyline points="20,6 9,17 4,12" />
                          </svg>
                        </div>
                      )}
                      {/* Count badge when not selected */}
                      {!isSelected && (
                        <div style={{
                          position: "absolute", top: 6, left: 6,
                          minWidth: 18, height: 18, borderRadius: 4,
                          background: "rgba(0,0,0,0.55)", display: "flex",
                          alignItems: "center", justifyContent: "center", padding: "0 4px",
                        }}>
                          <span style={{ fontSize: 9, fontWeight: 700, color: rl.label }}>×{card.count}</span>
                        </div>
                      )}
                      {/* Coin overlay */}
                      <div style={{
                        position: "absolute", bottom: 6, right: 6,
                        display: "flex", alignItems: "center", gap: 3,
                        background: isSelected ? "rgba(0,0,0,0.45)" : "rgba(0,0,0,0.60)",
                        borderRadius: 4, padding: "2px 5px",
                      }}>
                        <CoinIcon size={10} />
                        <span style={{ fontSize: 9, fontWeight: 700, color: GOLD }}>{sellVal}</span>
                      </div>
                      <span style={{
                        fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800,
                        fontSize: 28, color: "rgba(255,255,255,0.18)", lineHeight: 1,
                      }}>{card.ini}</span>
                      <span style={{
                        fontSize: 9, fontWeight: 700, letterSpacing: "0.10em", textTransform: "uppercase",
                        color: isSelected ? GOLD : rl.label,
                      }}>
                        {card.rarity}
                      </span>
                      <span style={{ fontSize: 10, color: "rgba(255,255,255,0.38)", textAlign: "center", padding: "0 8px" }}>
                        {card.name}
                      </span>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Fixed bottom sell bar */}
      {selectedCount > 0 && (
        <div style={{
          position: "fixed", bottom: 104, left: "50%", transform: "translateX(-50%)",
          width: "calc(100% - 32px)", maxWidth: 358, zIndex: 35,
          background: "rgba(10,24,16,0.90)",
          backdropFilter: "blur(20px)", WebkitBackdropFilter: "blur(20px)",
          border: `1px solid rgba(232,168,32,0.25)`,
          borderRadius: 12,
          padding: "12px 14px",
          display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12,
          boxShadow: "0 8px 32px rgba(0,0,0,0.55)",
        }}>
          {/* Total */}
          <div style={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <span style={{ fontSize: 10, color: TEXT_DIM, letterSpacing: "0.04em" }}>Total a recibir</span>
            <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
              <CoinIcon size={16} />
              <span style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 22, color: GOLD, letterSpacing: "0.04em",
              }}>{totalCoins}</span>
            </div>
          </div>
          {/* Button */}
          <button
            onClick={() => setShowConfirm(true)}
            style={{
              padding: "11px 18px",
              background: GOLD, border: "none", borderRadius: 9,
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
              fontSize: 14, letterSpacing: "0.08em", textTransform: "uppercase",
              color: "#0d1a13", cursor: "pointer",
              boxShadow: `0 0 16px rgba(232,168,32,0.35)`,
              whiteSpace: "nowrap",
            }}
          >
            Vender ({selectedCount} {selectedCount === 1 ? "carta" : "cartas"})
          </button>
        </div>
      )}

      {/* Confirmation modal */}
      {showConfirm && (
        <>
          <div onClick={() => setShowConfirm(false)} style={{
            position: "fixed", inset: 0, zIndex: 70,
            background: "rgba(0,0,0,0.72)",
            backdropFilter: "blur(4px)", WebkitBackdropFilter: "blur(4px)",
          }} />
          <div style={{
            position: "fixed",
            top: "50%", left: "50%", transform: "translate(-50%,-50%)",
            width: "calc(100% - 40px)", maxWidth: 320,
            background: "#0c1810",
            border: `1px solid ${BORDER_SUBTLE}`,
            borderRadius: 14,
            boxShadow: "0 24px 64px rgba(0,0,0,0.70)",
            zIndex: 71,
            padding: "24px 20px 20px",
            display: "flex", flexDirection: "column", gap: 14,
          }}>
            {/* Icon */}
            <div style={{ display: "flex", justifyContent: "center" }}>
              <div style={{
                width: 44, height: 44, borderRadius: 999,
                background: "rgba(232,168,32,0.12)",
                border: `1px solid rgba(232,168,32,0.25)`,
                display: "flex", alignItems: "center", justifyContent: "center",
              }}>
                <CoinIcon size={22} />
              </div>
            </div>
            {/* Text */}
            <div style={{ textAlign: "center" }}>
              <h3 style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 20, letterSpacing: "0.08em", textTransform: "uppercase",
                color: TEXT_WHITE, margin: 0, marginBottom: 8,
              }}>Confirmar venta</h3>
              <p style={{ fontSize: 13, color: TEXT_GRAY, margin: 0, lineHeight: 1.5 }}>
                Vas a vender <strong style={{ color: TEXT_WHITE }}>{selectedCount} {selectedCount === 1 ? "carta" : "cartas"}</strong> por{" "}
                <strong style={{ color: GOLD }}>{totalCoins} monedas</strong>.
              </p>
              <p style={{ fontSize: 11, color: TEXT_DIM, margin: "8px 0 0", lineHeight: 1.4 }}>
                Esta acción no se puede deshacer.
              </p>
            </div>
            {/* Buttons */}
            <div style={{ display: "flex", flexDirection: "column", gap: 8, marginTop: 4 }}>
              <button
                onClick={confirmSell}
                style={{
                  width: "100%", padding: "12px 0",
                  background: GOLD, border: "none", borderRadius: 8,
                  fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                  fontSize: 15, letterSpacing: "0.08em", textTransform: "uppercase",
                  color: "#0d1a13", cursor: "pointer",
                }}
              >Confirmar</button>
              <button
                onClick={() => setShowConfirm(false)}
                style={{
                  width: "100%", padding: "11px 0",
                  background: "transparent",
                  border: `1px solid ${BORDER_SUBTLE}`, borderRadius: 8,
                  fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                  fontSize: 15, letterSpacing: "0.08em", textTransform: "uppercase",
                  color: TEXT_GRAY, cursor: "pointer",
                }}
              >Cancelar</button>
            </div>
          </div>
        </>
      )}
    </>
  );
}

// ── Screen: Mercado ───────────────────────────────────────────────────────

type MarketListing = {
  id: number; cardName: string; ini: string; rarity: string;
  seller: string; sellerAvatar: string; price: number; postedAt: string;
};
type MyListing = { id: number; cardName: string; ini: string; rarity: string; price: number; listedAt: string };

const MARKET_LISTINGS_INIT: MarketListing[] = [
  { id: 1,  cardName: "Musiala",       ini: "JM",  rarity: "Común",      seller: "ProPlayer_99", sellerAvatar: "PP", price: 25,  postedAt: "hace 5 min"  },
  { id: 2,  cardName: "Rodri",         ini: "RO",  rarity: "Común",      seller: "ElChampion",   sellerAvatar: "EC", price: 30,  postedAt: "hace 3 h"    },
  { id: 3,  cardName: "Haaland",       ini: "EH",  rarity: "Común",      seller: "GoldenShot_7", sellerAvatar: "GS", price: 45,  postedAt: "hace 5 h"    },
  { id: 4,  cardName: "Salah",         ini: "MS",  rarity: "Poco común",  seller: "CardMaster_X", sellerAvatar: "CM", price: 70,  postedAt: "hace 8 h"    },
  { id: 5,  cardName: "Mbappé",        ini: "KM",  rarity: "Poco común",  seller: "FutbolFan_22", sellerAvatar: "FF", price: 80,  postedAt: "hace 12 min" },
  { id: 6,  cardName: "Pedri",         ini: "PE",  rarity: "Rara",       seller: "FutbolFan_22", sellerAvatar: "FF", price: 180, postedAt: "hace 5 h"    },
  { id: 7,  cardName: "Bellingham",    ini: "JB",  rarity: "Rara",       seller: "MiAmigo_01",   sellerAvatar: "MA", price: 195, postedAt: "hace 2 h"    },
  { id: 8,  cardName: "Vinicius Jr.",  ini: "VJ",  rarity: "Rara",       seller: "CardMaster_X", sellerAvatar: "CM", price: 220, postedAt: "hace 23 min" },
  { id: 9,  cardName: "Luis Díaz",     ini: "LD",  rarity: "Mítica",     seller: "ProPlayer_99", sellerAvatar: "PP", price: 650, postedAt: "hace 1 h"    },
  { id: 10, cardName: "Lamine Yamal",  ini: "LY",  rarity: "Mítica",     seller: "GoldenShot_7", sellerAvatar: "GS", price: 750, postedAt: "hace 4 h"    },
];

const MY_LISTINGS_INIT: MyListing[] = [
  { id: 101, cardName: "De Bruyne", ini: "KDB", rarity: "Rara",       price: 250, listedAt: "hace 2 h" },
  { id: 102, cardName: "Osimhen",   ini: "VO",  rarity: "Poco común",  price: 65,  listedAt: "hace 5 h" },
];

const MARKET_FILTERS = ["Todas", "Común", "Poco común", "Rara", "Mítica"];

// Price-tag sticker component
function PriceTag({ price, size = "md" }: { price: number; size?: "sm" | "md" }) {
  const sm = size === "sm";
  return (
    <div style={{
      display: "inline-flex", alignItems: "center", gap: sm ? 3 : 5,
      background: "rgba(232,168,32,0.09)",
      border: `1px solid rgba(232,168,32,0.30)`,
      borderRadius: sm ? "0 5px 5px 5px" : "0 7px 7px 7px",
      padding: sm ? "3px 7px 3px 5px" : "5px 10px 5px 7px",
      boxShadow: "inset 0 1px 0 rgba(255,255,255,0.04), 1px 1px 0 rgba(0,0,0,0.18)",
    }}>
      {/* Tag hole */}
      <div style={{
        width: sm ? 4 : 5, height: sm ? 4 : 5, borderRadius: "50%", flexShrink: 0,
        border: `1.5px solid rgba(232,168,32,0.48)`,
      }} />
      <CoinIcon size={sm ? 10 : 13} />
      <span style={{
        fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
        fontSize: sm ? 13 : 16, color: GOLD, letterSpacing: "0.03em",
      }}>{price}</span>
    </div>
  );
}

// Card listing (COMPRAR tab)
function MarketListingCard({ listing, onBuy }: { listing: MarketListing; onBuy: () => void }) {
  const rl = RARITY[listing.rarity];
  const isMythic = listing.rarity === "Mítica";

  const inner = (
    <div style={{
      width: "100%",
      background: CARD_BG, borderRadius: isMythic ? 8 : 9,
      display: "flex", flexDirection: "column", overflow: "hidden",
    }}>
      {/* Seller row */}
      <div style={{
        display: "flex", alignItems: "center", gap: 6, padding: "7px 9px 6px",
        borderBottom: `1px solid ${BORDER_SUBTLE}`,
      }}>
        <div style={{
          width: 20, height: 20, borderRadius: "50%", flexShrink: 0,
          background: "rgba(255,255,255,0.06)", border: `1px solid ${BORDER_SUBTLE}`,
          display: "flex", alignItems: "center", justifyContent: "center",
        }}>
          <span style={{ fontSize: 7, fontWeight: 700, color: TEXT_GRAY }}>{listing.sellerAvatar}</span>
        </div>
        <span style={{
          fontSize: 10, color: TEXT_GRAY, flex: 1,
          overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap",
        }}>{listing.seller}</span>
        <span style={{ fontSize: 9, color: TEXT_DIM, flexShrink: 0 }}>{listing.postedAt}</span>
      </div>

      {/* Card preview */}
      <div style={{
        flex: 1, display: "flex", flexDirection: "column",
        alignItems: "center", justifyContent: "center",
        padding: "14px 8px 10px", gap: 4,
      }}>
        <span style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800,
          fontSize: 34, color: "rgba(255,255,255,0.18)", lineHeight: 1,
        }}>{listing.ini}</span>
        <span style={{ fontSize: 10, color: "rgba(255,255,255,0.40)", textAlign: "center" }}>{listing.cardName}</span>
        <span style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
          fontSize: 9, letterSpacing: "0.10em", textTransform: "uppercase",
          color: isMythic ? GOLD : rl.label,
        }}>{listing.rarity}</span>
      </div>

      {/* Price + buy */}
      <div style={{
        padding: "8px 9px 10px", borderTop: `1px solid ${BORDER_SUBTLE}`,
        display: "flex", flexDirection: "column", gap: 7,
      }}>
        <PriceTag price={listing.price} size="sm" />
        <button
          onClick={onBuy}
          style={{
            width: "100%", padding: "8px 0",
            background: GOLD, border: "none", borderRadius: 6,
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 12, letterSpacing: "0.08em", textTransform: "uppercase",
            color: "#0d1a13", cursor: "pointer",
          }}
        >Comprar</button>
      </div>
    </div>
  );

  return isMythic ? (
    <div style={{ background: rl.gradient, borderRadius: 10, padding: "1.5px" }}>{inner}</div>
  ) : (
    <div style={{ border: `1.5px solid ${rl.border}`, borderRadius: 10, overflow: "hidden" }}>{inner}</div>
  );
}

// Active listing row (MIS VENTAS tab)
function ActiveListingRow({ listing, onEdit, onWithdraw }: {
  listing: MyListing;
  onEdit: (id: number, currentPrice: number) => void;
  onWithdraw: (id: number) => void;
}) {
  const rl = RARITY[listing.rarity];
  const isMythic = listing.rarity === "Mítica";
  const MINI_W = 44;

  return (
    <div style={{
      background: CARD_BG, border: `1px solid ${BORDER_SUBTLE}`,
      borderRadius: 10, padding: "12px",
      display: "flex", flexDirection: "column", gap: 10,
    }}>
      <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
        {/* Mini card */}
        {isMythic ? (
          <div style={{ width: MINI_W, aspectRatio: "3/4.2", background: rl.gradient, borderRadius: 7, padding: "1.5px", flexShrink: 0 }}>
            <div style={{
              width: "100%", height: "100%", background: CARD_BG, borderRadius: 6,
              display: "flex", alignItems: "center", justifyContent: "center",
            }}>
              <span style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800, fontSize: 14, color: "rgba(255,255,255,0.20)" }}>{listing.ini}</span>
            </div>
          </div>
        ) : (
          <div style={{
            width: MINI_W, aspectRatio: "3/4.2", border: `1.5px solid ${rl.border}`,
            borderRadius: 7, background: CARD_BG, flexShrink: 0,
            display: "flex", alignItems: "center", justifyContent: "center",
          }}>
            <span style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800, fontSize: 14, color: "rgba(255,255,255,0.20)" }}>{listing.ini}</span>
          </div>
        )}
        {/* Info */}
        <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 4 }}>
          <span style={{ fontSize: 13, fontWeight: 600, color: TEXT_WHITE }}>{listing.cardName}</span>
          <span style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 10, letterSpacing: "0.10em", textTransform: "uppercase",
            color: isMythic ? GOLD : rl.label,
          }}>{listing.rarity}</span>
          <PriceTag price={listing.price} size="sm" />
          <span style={{ fontSize: 10, color: TEXT_DIM }}>Publicado {listing.listedAt}</span>
        </div>
      </div>
      {/* Actions */}
      <div style={{ display: "flex", gap: 8 }}>
        <button
          onClick={() => onEdit(listing.id, listing.price)}
          style={{
            flex: 1, padding: "8px 0",
            background: "transparent", border: `1px solid ${BORDER_SUBTLE}`,
            borderRadius: 7,
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 12, letterSpacing: "0.06em", textTransform: "uppercase",
            color: TEXT_GRAY, cursor: "pointer",
          }}
        >Editar precio</button>
        <button
          onClick={() => onWithdraw(listing.id)}
          style={{
            flex: 1, padding: "8px 0",
            background: "transparent", border: `1px solid rgba(220,50,50,0.22)`,
            borderRadius: 7,
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 12, letterSpacing: "0.06em", textTransform: "uppercase",
            color: "rgba(220,80,80,0.80)", cursor: "pointer",
          }}
        >Retirar</button>
      </div>
    </div>
  );
}

// Publish / Edit price modal
function PriceModal({
  title, cardName, rarity, ini, initialPrice = "", onConfirm, onClose,
}: {
  title: string; cardName: string; rarity: string; ini: string;
  initialPrice?: string;
  onConfirm: (price: number) => void;
  onClose: () => void;
}) {
  const [priceInput, setPriceInput] = useState(initialPrice);
  const rl = RARITY[rarity];
  const isMythic = rarity === "Mítica";
  const parsed = parseInt(priceInput, 10);
  const valid = !isNaN(parsed) && parsed > 0;

  return (
    <>
      <div onClick={onClose} style={{
        position: "fixed", inset: 0, zIndex: 70,
        background: "rgba(0,0,0,0.72)", backdropFilter: "blur(4px)", WebkitBackdropFilter: "blur(4px)",
      }} />
      <div style={{
        position: "fixed", top: "50%", left: "50%", transform: "translate(-50%,-50%)",
        width: "calc(100% - 40px)", maxWidth: 300,
        background: "#0c1810", border: `1px solid ${BORDER_SUBTLE}`,
        borderRadius: 14, zIndex: 71, padding: "22px 18px 18px",
        display: "flex", flexDirection: "column", gap: 16,
        boxShadow: "0 24px 64px rgba(0,0,0,0.70)",
      }}>
        <h3 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
          fontSize: 19, letterSpacing: "0.08em", textTransform: "uppercase",
          color: TEXT_WHITE, margin: 0,
        }}>{title}</h3>

        {/* Card preview row */}
        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
          {isMythic ? (
            <div style={{ width: 40, aspectRatio: "3/4.2", background: rl.gradient, borderRadius: 6, padding: "1.5px", flexShrink: 0 }}>
              <div style={{ width: "100%", height: "100%", background: CARD_BG, borderRadius: 5, display: "flex", alignItems: "center", justifyContent: "center" }}>
                <span style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800, fontSize: 13, color: "rgba(255,255,255,0.20)" }}>{ini}</span>
              </div>
            </div>
          ) : (
            <div style={{ width: 40, aspectRatio: "3/4.2", border: `1.5px solid ${rl.border}`, borderRadius: 6, background: CARD_BG, flexShrink: 0, display: "flex", alignItems: "center", justifyContent: "center" }}>
              <span style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800, fontSize: 13, color: "rgba(255,255,255,0.20)" }}>{ini}</span>
            </div>
          )}
          <div>
            <div style={{ fontSize: 13, fontWeight: 600, color: TEXT_WHITE }}>{cardName}</div>
            <div style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 10, letterSpacing: "0.10em", textTransform: "uppercase", color: isMythic ? GOLD : rl.label, marginTop: 2 }}>{rarity}</div>
          </div>
        </div>

        {/* Price input */}
        <div>
          <label style={{ fontSize: 11, color: TEXT_DIM, display: "block", marginBottom: 6, letterSpacing: "0.04em" }}>
            Precio en monedas
          </label>
          <div style={{
            display: "flex", alignItems: "center", gap: 8,
            background: "rgba(255,255,255,0.05)", border: `1px solid ${BORDER_SUBTLE}`,
            borderRadius: 8, padding: "10px 12px",
          }}>
            <CoinIcon size={16} />
            <input
              type="number"
              value={priceInput}
              onChange={(e) => setPriceInput(e.target.value)}
              placeholder="0"
              min={1}
              style={{
                background: "none", border: "none", outline: "none",
                color: TEXT_WHITE, fontSize: 16, fontWeight: 600, flex: 1,
                caretColor: GOLD,
              }}
            />
          </div>
        </div>

        {/* Buttons */}
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          <button
            onClick={() => valid && onConfirm(parsed)}
            style={{
              width: "100%", padding: "12px 0",
              background: valid ? GOLD : "rgba(232,168,32,0.25)",
              border: "none", borderRadius: 8,
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
              fontSize: 14, letterSpacing: "0.08em", textTransform: "uppercase",
              color: valid ? "#0d1a13" : "rgba(232,168,32,0.50)", cursor: valid ? "pointer" : "default",
              transition: "all 0.15s",
            }}
          >Confirmar</button>
          <button
            onClick={onClose}
            style={{
              width: "100%", padding: "11px 0",
              background: "transparent", border: `1px solid ${BORDER_SUBTLE}`, borderRadius: 8,
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
              fontSize: 14, letterSpacing: "0.08em", textTransform: "uppercase",
              color: TEXT_GRAY, cursor: "pointer",
            }}
          >Cancelar</button>
        </div>
      </div>
    </>
  );
}

function MarketScreen({ onBack }: { onBack: () => void }) {
  const [activeTab, setActiveTab]     = useState<"buy" | "sell">("buy");
  const [rarityFilter, setRarityFilter] = useState("Todas");
  const [listings, setListings]       = useState(MARKET_LISTINGS_INIT);
  const [myListings, setMyListings]   = useState(MY_LISTINGS_INIT);
  const [publishCard, setPublishCard] = useState<typeof CARDS[0] | null>(null);
  const [editingId, setEditingId]     = useState<number | null>(null);
  const [myCoins, setMyCoins]         = useState(1240);

  const myDuplicates = CARDS.filter((c) => c.count >= 2);

  const filteredListings =
    rarityFilter === "Todas" ? listings : listings.filter((l) => l.rarity === rarityFilter);

  function handleBuy(listingId: number) {
    const listing = listings.find((l) => l.id === listingId);
    if (!listing) return;
    setListings((prev) => prev.filter((l) => l.id !== listingId));
    setMyCoins((c) => c - listing.price);
  }

  function handlePublish(price: number) {
    if (!publishCard) return;
    const newListing: MyListing = {
      id: Date.now(), cardName: publishCard.name, ini: publishCard.ini,
      rarity: publishCard.rarity, price, listedAt: "ahora mismo",
    };
    setMyListings((prev) => [newListing, ...prev]);
    setPublishCard(null);
  }

  function handleEditPrice(newPrice: number) {
    setMyListings((prev) => prev.map((l) => l.id === editingId ? { ...l, price: newPrice } : l));
    setEditingId(null);
  }

  function handleWithdraw(id: number) {
    setMyListings((prev) => prev.filter((l) => l.id !== id));
  }

  const editingListing = myListings.find((l) => l.id === editingId);

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", gap: 12, flexShrink: 0,
      }}>
        <button onClick={onBack} style={{ background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0 }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="15,18 9,12 15,6" />
          </svg>
        </button>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 24,
          letterSpacing: "0.10em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0, flex: 1,
        }}>Mercado</h1>
        {/* Coin balance */}
        <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
          <CoinIcon size={16} />
          <span style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 16, color: GOLD }}>{myCoins.toLocaleString()}</span>
        </div>
      </div>

      {/* Tab chips */}
      <div style={{ padding: "0 16px", display: "flex", gap: 8, flexShrink: 0, marginBottom: 14 }}>
        {(["buy", "sell"] as const).map((tab) => {
          const active = activeTab === tab;
          const label  = tab === "buy" ? "Comprar" : "Mis ventas";
          return (
            <button key={tab} onClick={() => setActiveTab(tab)} style={{
              padding: "8px 18px", borderRadius: 999,
              background: active ? GOLD : "transparent",
              border: `1.5px solid ${active ? GOLD : BORDER_SUBTLE}`,
              cursor: "pointer", transition: "all 0.15s",
            }}>
              <span style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 13, letterSpacing: "0.08em", textTransform: "uppercase",
                color: active ? "#0d1a13" : TEXT_GRAY,
              }}>{label}</span>
            </button>
          );
        })}
      </div>

      {/* ── COMPRAR tab ────────────────────────────────── */}
      {activeTab === "buy" && (
        <>
          {/* Rarity filter chips */}
          <div style={{
            overflowX: "auto", display: "flex", gap: 7,
            padding: "0 16px", marginBottom: 14, flexShrink: 0,
            scrollbarWidth: "none",
          }}>
            {MARKET_FILTERS.map((f) => {
              const active = rarityFilter === f;
              const rl     = f !== "Todas" ? RARITY[f] : null;
              return (
                <button key={f} onClick={() => setRarityFilter(f)} style={{
                  flexShrink: 0, padding: "6px 13px", borderRadius: 999,
                  background: active ? (rl?.gradient ?? GOLD) : "transparent",
                  border: `1.5px solid ${active ? (rl?.border ?? GOLD) : BORDER_SUBTLE}`,
                  cursor: "pointer",
                }}>
                  <span style={{
                    fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                    fontSize: 12, letterSpacing: "0.07em", textTransform: "uppercase",
                    color: active ? (f === "Mítica" ? "#0d1a13" : TEXT_WHITE) : TEXT_GRAY,
                  }}>{f}</span>
                </button>
              );
            })}
          </div>

          {/* Listings grid */}
          <div className="card-grid-scroll" style={{ flex: 1, overflowY: "auto", padding: "0 16px", paddingBottom: 108 }}>
            {filteredListings.length === 0 ? (
              <div style={{
                display: "flex", flexDirection: "column", alignItems: "center",
                justifyContent: "center", padding: "48px 24px", gap: 12, textAlign: "center",
              }}>
                <svg width="36" height="36" viewBox="0 0 24 24" fill="none"
                  stroke="rgba(255,255,255,0.14)" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M20.59 13.41l-7.17 7.17a2 2 0 01-2.83 0L2 12V2h10l8.59 8.59a2 2 0 010 2.82z"/>
                  <circle cx="7" cy="7" r="1.5" fill="rgba(255,255,255,0.14)" stroke="none"/>
                </svg>
                <div>
                  <p style={{ fontSize: 14, fontWeight: 600, color: "rgba(255,255,255,0.30)", margin: 0 }}>
                    Nadie está vendiendo cartas de esta rareza ahora mismo.
                  </p>
                  <p style={{ fontSize: 12, color: TEXT_DIM, margin: "6px 0 0", lineHeight: 1.5 }}>
                    Prueba otra rareza o vuelve más tarde — el mercado se actualiza constantemente.
                  </p>
                </div>
              </div>
            ) : (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
                {filteredListings.map((l) => (
                  <MarketListingCard key={l.id} listing={l} onBuy={() => handleBuy(l.id)} />
                ))}
              </div>
            )}
          </div>
        </>
      )}

      {/* ── MIS VENTAS tab ─────────────────────────────── */}
      {activeTab === "sell" && (
        <div className="card-grid-scroll" style={{ flex: 1, overflowY: "auto", padding: "0 16px", paddingBottom: 108 }}>

          {/* Tus duplicados */}
          <section style={{ marginBottom: 28 }}>
            <h2 style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 15,
              letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
              margin: "0 0 12px",
            }}>Tus duplicados</h2>

            {myDuplicates.length === 0 ? (
              <p style={{ fontSize: 13, color: TEXT_DIM, lineHeight: 1.5 }}>
                No tienes duplicados disponibles. Abre más sobres para conseguirlos.
              </p>
            ) : (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
                {myDuplicates.map((card) => {
                  const rl = RARITY[card.rarity];
                  const isMythic = card.rarity === "Mítica";
                  const inner = (
                    <div style={{
                      width: "100%", height: "100%", background: CARD_BG,
                      borderRadius: isMythic ? 8 : 9,
                      display: "flex", flexDirection: "column",
                      alignItems: "center", justifyContent: "center",
                      padding: "10px 8px 8px", gap: 3, position: "relative",
                    }}>
                      <div style={{
                        position: "absolute", top: 5, left: 5,
                        background: "rgba(0,0,0,0.55)", borderRadius: 4,
                        padding: "2px 5px",
                      }}>
                        <span style={{ fontSize: 9, fontWeight: 700, color: isMythic ? GOLD : rl.label }}>×{card.count}</span>
                      </div>
                      <span style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 800, fontSize: 28, color: "rgba(255,255,255,0.18)", lineHeight: 1 }}>{card.ini}</span>
                      <span style={{ fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 8, color: isMythic ? GOLD : rl.label, letterSpacing: "0.10em", textTransform: "uppercase" }}>{card.rarity}</span>
                      <span style={{ fontSize: 10, color: "rgba(255,255,255,0.35)", textAlign: "center" }}>{card.name}</span>
                      <button
                        onClick={() => setPublishCard(card)}
                        style={{
                          marginTop: 6, width: "100%", padding: "7px 0",
                          background: "rgba(232,168,32,0.12)",
                          border: `1px solid rgba(232,168,32,0.30)`,
                          borderRadius: 6,
                          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                          fontSize: 11, letterSpacing: "0.07em", textTransform: "uppercase",
                          color: GOLD, cursor: "pointer",
                        }}
                      >Publicar</button>
                    </div>
                  );
                  return (
                    <div key={card.id} style={{ aspectRatio: "3/4.5" }}>
                      {isMythic ? (
                        <div style={{ width: "100%", height: "100%", background: rl.gradient, borderRadius: 10, padding: "1.5px" }}>{inner}</div>
                      ) : (
                        <div style={{ width: "100%", height: "100%", border: `1.5px solid ${rl.border}`, borderRadius: 10, overflow: "hidden" }}>{inner}</div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </section>

          {/* Tus listados activos */}
          <section>
            <h2 style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 15,
              letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
              margin: "0 0 12px",
            }}>Listados activos</h2>

            {myListings.length === 0 ? (
              <p style={{ fontSize: 13, color: TEXT_DIM, lineHeight: 1.5 }}>
                No tienes cartas publicadas. Usa "Publicar" en tus duplicados para empezar a vender.
              </p>
            ) : (
              <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                {myListings.map((l) => (
                  <ActiveListingRow
                    key={l.id}
                    listing={l}
                    onEdit={(id, price) => { setEditingId(id); }}
                    onWithdraw={handleWithdraw}
                  />
                ))}
              </div>
            )}
          </section>
        </div>
      )}

      {/* Publish modal */}
      {publishCard && (
        <PriceModal
          title="Fijar precio"
          cardName={publishCard.name}
          rarity={publishCard.rarity}
          ini={publishCard.ini}
          onConfirm={handlePublish}
          onClose={() => setPublishCard(null)}
        />
      )}

      {/* Edit price modal */}
      {editingListing && (
        <PriceModal
          title="Editar precio"
          cardName={editingListing.cardName}
          rarity={editingListing.rarity}
          ini={editingListing.ini}
          initialPrice={String(editingListing.price)}
          onConfirm={handleEditPrice}
          onClose={() => setEditingId(null)}
        />
      )}
    </>
  );
}

// ── Screen: Amigos ───────────────────────────────────────────────────────

const MY_FRIEND_CODE = "FCX-2847";
const MY_POWER = 5430;

const FRIEND_REQUESTS_INIT = [
  { id: 1, user: "NuevoJugador_01", avatar: "NJ" },
  { id: 2, user: "FutbolFan_77",    avatar: "F7" },
];

type Friend = {
  id: number; user: string; avatar: string;
  level: number; cards: number; albumPct: number; power: number;
};

const FRIENDS_LIST_INIT: Friend[] = [
  { id: 1, user: "GoldenShot_7", avatar: "GS", level: 24, cards: 445, albumPct: 89, power: 9120 },
  { id: 2, user: "ElChampion",   avatar: "EC", level: 18, cards: 312, albumPct: 71, power: 6840 },
  { id: 3, user: "MiAmigo_01",   avatar: "MA", level: 12, cards: 187, albumPct: 52, power: 4250 },
  { id: 4, user: "FutbolFan_22", avatar: "FF", level: 9,  cards: 98,  albumPct: 28, power: 2180 },
];

function LightningIcon({ size = 12, color = GOLD }: { size?: number; color?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill={color} stroke="none">
      <polygon points="13,2 3,14 12,14 11,22 21,10 12,10" />
    </svg>
  );
}

function FriendRow({ friend, onTrade }: { friend: Friend; onTrade: () => void }) {
  return (
    <div style={{
      background: CARD_BG, border: `1px solid ${BORDER_SUBTLE}`,
      borderRadius: 10, padding: "12px",
      display: "flex", flexDirection: "column", gap: 10,
    }}>
      {/* Top row: avatar + name + level + power */}
      <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
        <div style={{
          width: 40, height: 40, borderRadius: "50%", flexShrink: 0,
          background: "rgba(255,255,255,0.07)",
          border: `1.5px solid ${BORDER_SUBTLE}`,
          display: "flex", alignItems: "center", justifyContent: "center",
        }}>
          <span style={{ fontSize: 11, fontWeight: 700, color: TEXT_GRAY, letterSpacing: "0.04em" }}>{friend.avatar}</span>
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontSize: 13, fontWeight: 600, color: TEXT_WHITE, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
            {friend.user}
          </div>
          <div style={{ fontSize: 11, color: TEXT_DIM, marginTop: 1 }}>Nivel {friend.level}</div>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 4, flexShrink: 0 }}>
          <LightningIcon size={12} />
          <span style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 14, color: GOLD, letterSpacing: "0.03em",
          }}>{friend.power.toLocaleString()}</span>
        </div>
      </div>

      {/* Stats + album bar */}
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <span style={{ fontSize: 11, color: TEXT_DIM, flexShrink: 0 }}>{friend.cards} cartas</span>
        <span style={{ color: TEXT_DIM, fontSize: 10 }}>·</span>
        <span style={{ fontSize: 11, color: TEXT_DIM, flexShrink: 0 }}>Álbum</span>
        <div style={{ flex: 1, height: 4, borderRadius: 999, background: "rgba(255,255,255,0.09)", overflow: "hidden" }}>
          <div style={{ height: "100%", width: `${friend.albumPct}%`, borderRadius: 999, background: `linear-gradient(to right, rgba(232,168,32,0.55), ${GOLD})` }} />
        </div>
        <span style={{ fontSize: 11, color: TEXT_GRAY, flexShrink: 0 }}>{friend.albumPct}%</span>
      </div>

      {/* Action buttons */}
      <div style={{ display: "flex", gap: 8 }}>
        <button style={{
          flex: 1, padding: "8px 0",
          background: "transparent", border: `1px solid ${BORDER_SUBTLE}`, borderRadius: 7,
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
          fontSize: 12, letterSpacing: "0.06em", textTransform: "uppercase",
          color: TEXT_GRAY, cursor: "pointer",
        }}>Comparar</button>
        <button
          onClick={onTrade}
          style={{
            flex: 1, padding: "8px 0",
            background: "transparent", border: `1px solid ${BORDER_SUBTLE}`, borderRadius: 7,
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
            fontSize: 12, letterSpacing: "0.06em", textTransform: "uppercase",
            color: TEXT_GRAY, cursor: "pointer",
          }}
        >Intercambiar</button>
      </div>
    </div>
  );
}

function FriendsScreen({ onBack, onNavigateTo }: {
  onBack: () => void;
  onNavigateTo: (sub: string) => void;
}) {
  const [requests, setRequests] = useState(FRIEND_REQUESTS_INIT);
  const [friends, setFriends]   = useState(FRIENDS_LIST_INIT);
  const [addCode, setAddCode]   = useState("");
  const [copied, setCopied]     = useState(false);
  const [addFeedback, setAddFeedback] = useState<"success" | "error" | null>(null);

  const pendingCount = requests.length;

  const rankingList = [
    { user: "Tú", avatar: "YO", power: MY_POWER, isMe: true },
    ...friends.map((f) => ({ user: f.user, avatar: f.avatar, power: f.power, isMe: false })),
  ].sort((a, b) => b.power - a.power);

  function handleCopy() {
    navigator.clipboard?.writeText(MY_FRIEND_CODE).catch(() => {});
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  function handleAccept(id: number) {
    const req = requests.find((r) => r.id === id);
    if (req) {
      const newFriend: Friend = { id: Date.now(), user: req.user, avatar: req.avatar, level: 1, cards: 12, albumPct: 3, power: 320 };
      setFriends((prev) => [...prev, newFriend]);
    }
    setRequests((prev) => prev.filter((r) => r.id !== id));
  }

  function handleReject(id: number) {
    setRequests((prev) => prev.filter((r) => r.id !== id));
  }

  function handleRemoveFriend(id: number) {
    setFriends((prev) => prev.filter((f) => f.id !== id));
  }

  function handleAdd() {
    const trimmed = addCode.trim();
    if (trimmed.length >= 6) {
      setAddFeedback("success");
      setAddCode("");
      setTimeout(() => setAddFeedback(null), 2500);
    } else {
      setAddFeedback("error");
      setTimeout(() => setAddFeedback(null), 2000);
    }
  }

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", gap: 12, flexShrink: 0,
      }}>
        <button onClick={onBack} style={{ background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0 }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="15,18 9,12 15,6" />
          </svg>
        </button>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 24,
          letterSpacing: "0.10em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
        }}>Amigos</h1>
      </div>

      {/* Scroll content */}
      <div className="card-grid-scroll" style={{ flex: 1, overflowY: "auto", padding: "0 16px", paddingBottom: 108 }}>

        {/* Friend code section */}
        <div style={{
          background: CARD_BG, border: `1px solid ${BORDER_SUBTLE}`,
          borderRadius: 10, padding: "12px 14px",
          display: "flex", flexDirection: "column", gap: 10, marginBottom: 20,
        }}>
          {/* Own code + copy */}
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <div>
              <div style={{ fontSize: 10, color: TEXT_DIM, marginBottom: 3, letterSpacing: "0.04em" }}>Tu código de amigo</div>
              <span style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 20, color: TEXT_WHITE, letterSpacing: "0.14em",
              }}>{MY_FRIEND_CODE}</span>
            </div>
            <button
              onClick={handleCopy}
              style={{
                display: "flex", alignItems: "center", gap: 6,
                padding: "8px 12px", borderRadius: 7,
                background: copied ? "rgba(232,168,32,0.14)" : "rgba(255,255,255,0.05)",
                border: `1px solid ${copied ? "rgba(232,168,32,0.35)" : BORDER_SUBTLE}`,
                cursor: "pointer", transition: "all 0.15s",
              }}
            >
              {copied ? (
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                  stroke={GOLD} strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="20,6 9,17 4,12" />
                </svg>
              ) : (
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                  stroke="rgba(255,255,255,0.45)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                  <rect x="9" y="9" width="13" height="13" rx="2" /><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" />
                </svg>
              )}
              <span style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 12, letterSpacing: "0.07em", textTransform: "uppercase",
                color: copied ? GOLD : TEXT_GRAY,
              }}>{copied ? "¡Copiado!" : "Copiar"}</span>
            </button>
          </div>

          {/* Add friend input */}
          <div style={{ display: "flex", gap: 8 }}>
            <div style={{
              flex: 1, display: "flex", alignItems: "center", gap: 8,
              background: "rgba(255,255,255,0.05)",
              border: `1px solid ${addFeedback === "error" ? "rgba(220,80,80,0.50)" : addFeedback === "success" ? "rgba(39,201,106,0.45)" : BORDER_SUBTLE}`,
              borderRadius: 8, padding: "9px 12px",
              transition: "border-color 0.2s",
            }}>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.28)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="11" cy="11" r="7" /><line x1="16.5" y1="16.5" x2="21" y2="21" />
              </svg>
              <input
                value={addCode}
                onChange={(e) => setAddCode(e.target.value)}
                placeholder={addFeedback === "success" ? "¡Solicitud enviada!" : "Código de amigo…"}
                style={{
                  background: "none", border: "none", outline: "none",
                  color: addFeedback === "success" ? "rgba(39,201,106,0.90)" : TEXT_WHITE,
                  fontSize: 13, flex: 1, caretColor: GOLD,
                }}
              />
            </div>
            <button
              onClick={handleAdd}
              style={{
                padding: "9px 16px", flexShrink: 0,
                background: GOLD, border: "none", borderRadius: 8,
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                fontSize: 13, letterSpacing: "0.07em", textTransform: "uppercase",
                color: "#0d1a13", cursor: "pointer",
              }}
            >Agregar</button>
          </div>
          {addFeedback === "error" && (
            <p style={{ fontSize: 11, color: "rgba(220,80,80,0.80)", margin: 0 }}>
              Introduce un código válido (mín. 6 caracteres).
            </p>
          )}
        </div>

        {/* SOLICITUDES */}
        {pendingCount > 0 && (
          <section style={{ marginBottom: 24 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 12 }}>
              <h2 style={{
                fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 15,
                letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
              }}>Solicitudes</h2>
              <div style={{
                minWidth: 20, height: 20, borderRadius: 999, padding: "0 5px",
                background: "rgba(0,0,0,0.55)", border: `1px solid ${GOLD_BORDER}`,
                display: "flex", alignItems: "center", justifyContent: "center",
              }}>
                <span style={{ fontSize: 10, fontWeight: 700, color: GOLD }}>{pendingCount}</span>
              </div>
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {requests.map((req) => (
                <div key={req.id} style={{
                  background: CARD_BG, border: `1px solid ${BORDER_SUBTLE}`,
                  borderRadius: 10, padding: "10px 12px",
                  display: "flex", alignItems: "center", gap: 10,
                }}>
                  <div style={{
                    width: 36, height: 36, borderRadius: "50%", flexShrink: 0,
                    background: "rgba(255,255,255,0.07)", border: `1px solid ${BORDER_SUBTLE}`,
                    display: "flex", alignItems: "center", justifyContent: "center",
                  }}>
                    <span style={{ fontSize: 10, fontWeight: 700, color: TEXT_GRAY }}>{req.avatar}</span>
                  </div>
                  <span style={{ flex: 1, fontSize: 13, fontWeight: 500, color: TEXT_WHITE,
                    overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                    {req.user}
                  </span>
                  <div style={{ display: "flex", gap: 7, flexShrink: 0 }}>
                    <button
                      onClick={() => handleAccept(req.id)}
                      style={{
                        padding: "7px 14px",
                        background: GOLD, border: "none", borderRadius: 7,
                        fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                        fontSize: 12, letterSpacing: "0.06em", textTransform: "uppercase",
                        color: "#0d1a13", cursor: "pointer",
                      }}
                    >Aceptar</button>
                    <button
                      onClick={() => handleReject(req.id)}
                      style={{
                        padding: "7px 14px",
                        background: "transparent", border: `1px solid ${BORDER_SUBTLE}`, borderRadius: 7,
                        fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                        fontSize: 12, letterSpacing: "0.06em", textTransform: "uppercase",
                        color: TEXT_GRAY, cursor: "pointer",
                      }}
                    >Rechazar</button>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}

        {/* MIS AMIGOS */}
        <section style={{ marginBottom: 24 }}>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 15,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
            margin: "0 0 12px",
          }}>Mis amigos</h2>

          {friends.length === 0 ? (
            <div style={{
              display: "flex", flexDirection: "column", alignItems: "center",
              padding: "40px 24px", gap: 14, textAlign: "center",
            }}>
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.13)" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="9" cy="7" r="3" /><path d="M3 21v-2a4 4 0 014-4h4a4 4 0 014 4v2" />
                <path d="M16 3.13a4 4 0 010 7.75" /><path d="M21 21v-2a4 4 0 00-3-3.85" />
              </svg>
              <div>
                <p style={{ fontSize: 14, fontWeight: 600, color: "rgba(255,255,255,0.28)", margin: 0 }}>
                  Agrega a tu primer amigo para comparar colecciones.
                </p>
                <p style={{ fontSize: 12, color: TEXT_DIM, margin: "6px 0 0", lineHeight: 1.5 }}>
                  Comparte tu código o busca el de un amigo para conectar.
                </p>
              </div>
            </div>
          ) : (
            <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
              {friends.map((f) => (
                <FriendRow key={f.id} friend={f} onTrade={() => onNavigateTo("intercambio")} />
              ))}
            </div>
          )}
        </section>

        {/* RANKING */}
        {friends.length > 0 && (
          <section style={{ marginBottom: 8 }}>
            <h2 style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 15,
              letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
              margin: "0 0 12px",
            }}>Ranking de amigos</h2>
            <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
              {rankingList.map((entry, i) => (
                <div key={entry.user} style={{
                  display: "flex", alignItems: "center", gap: 10,
                  padding: "10px 12px",
                  background: entry.isMe ? "rgba(232,168,32,0.06)" : CARD_BG,
                  border: `1px solid ${entry.isMe ? "rgba(232,168,32,0.38)" : BORDER_SUBTLE}`,
                  borderRadius: 8,
                }}>
                  {/* Position */}
                  <span style={{
                    fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                    fontSize: 15, width: 20, textAlign: "center", flexShrink: 0,
                    color: i === 0 ? GOLD : i === 1 ? "rgba(200,200,200,0.70)" : TEXT_DIM,
                  }}>{i + 1}</span>

                  {/* Avatar */}
                  <div style={{
                    width: 32, height: 32, borderRadius: "50%", flexShrink: 0,
                    background: entry.isMe ? "rgba(232,168,32,0.14)" : "rgba(255,255,255,0.07)",
                    border: `1px solid ${entry.isMe ? "rgba(232,168,32,0.35)" : BORDER_SUBTLE}`,
                    display: "flex", alignItems: "center", justifyContent: "center",
                  }}>
                    <span style={{ fontSize: 9, fontWeight: 700, color: entry.isMe ? GOLD : TEXT_GRAY }}>{entry.avatar}</span>
                  </div>

                  {/* Name */}
                  <span style={{
                    flex: 1, fontSize: 13,
                    fontWeight: entry.isMe ? 700 : 500,
                    color: entry.isMe ? TEXT_WHITE : TEXT_GRAY,
                    overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap",
                  }}>{entry.user}</span>

                  {/* Power */}
                  <div style={{ display: "flex", alignItems: "center", gap: 5, flexShrink: 0 }}>
                    <LightningIcon size={12} color={entry.isMe ? GOLD : "rgba(255,255,255,0.30)"} />
                    <span style={{
                      fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
                      fontSize: 14, color: entry.isMe ? GOLD : TEXT_GRAY,
                    }}>{entry.power.toLocaleString()}</span>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}
      </div>
    </>
  );
}

function rarityBorder(rarity: string | null): string {
  if (!rarity) return BORDER_SUBTLE;
  return RARITY[rarity]?.border ?? BORDER_SUBTLE;
}

// Rectangular player slot for the tactical formation view
const SLOT_W = 50;
const SLOT_H = 68;

function PlayerSlot({ pos, rarity, x, y }: { pos: string; rarity: string | null; x: string; y: string }) {
  const empty = rarity === null;
  const isMythic = rarity === "Mítica";
  const rl = rarity ? RARITY[rarity] : null;
  const borderColor = rarityBorder(rarity);

  const inner = (
    <div style={{
      width: SLOT_W, height: SLOT_H,
      background: CARD_BG,
      borderRadius: isMythic ? 6 : 7,
      display: "flex", flexDirection: "column",
      alignItems: "center", justifyContent: "center",
      position: "relative",
      gap: 6,
    }}>
      {/* Position chip — top-left corner */}
      <div style={{
        position: "absolute", top: 4, left: 4,
        background: "rgba(6,14,10,0.90)",
        border: `1px solid ${empty ? BORDER_SUBTLE : borderColor}`,
        borderRadius: 999,
        padding: "1px 5px",
        fontSize: 7, fontWeight: 700,
        color: rl?.label ?? TEXT_DIM,
        letterSpacing: "0.06em",
        lineHeight: 1.4,
        zIndex: 1,
      }}>{pos}</div>

      {/* Inner indicator dot when occupied */}
      {!empty && (
        <div style={{
          width: 16, height: 16, borderRadius: "50%",
          background: "rgba(255,255,255,0.07)",
          border: `1px solid ${borderColor}`,
          marginTop: 8,
        }} />
      )}
    </div>
  );

  return (
    <div style={{
      position: "absolute", left: x, top: y,
      transform: "translate(-50%, -50%)",
      opacity: empty ? 0.42 : 1,
      zIndex: 2,
    }}>
      {isMythic ? (
        <div style={{
          width: SLOT_W + 3, height: SLOT_H + 3,
          background: rl!.gradient,
          borderRadius: 8,
          padding: "1.5px",
          display: "flex", alignItems: "center", justifyContent: "center",
        }}>{inner}</div>
      ) : (
        <div style={{
          width: SLOT_W + 3, height: SLOT_H + 3,
          border: `1.5px solid ${borderColor}`,
          borderRadius: 8,
          display: "flex", alignItems: "center", justifyContent: "center",
        }}>{inner}</div>
      )}
    </div>
  );
}

// ── Component: Toggle switch ──────────────────────────────────────────────
function Toggle({ value, onChange }: { value: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      onClick={() => onChange(!value)}
      style={{
        width: 46, height: 26, borderRadius: 999, padding: 0, flexShrink: 0,
        background: value ? GOLD : "rgba(255,255,255,0.08)",
        border: `1.5px solid ${value ? GOLD : BORDER_SUBTLE}`,
        cursor: "pointer", position: "relative",
        transition: "background 0.18s ease, border-color 0.18s ease",
      }}
    >
      <div style={{
        width: 18, height: 18, borderRadius: "50%",
        background: value ? "#0d1a13" : "rgba(255,255,255,0.65)",
        position: "absolute",
        top: "50%", transform: "translateY(-50%)",
        left: value ? "calc(100% - 22px)" : 4,
        transition: "left 0.18s ease",
        boxShadow: "0 1px 3px rgba(0,0,0,0.35)",
      }} />
    </button>
  );
}

// ── Screen: Ajustes ───────────────────────────────────────────────────────
function SettingsScreen({ onBack, onSignOut, onLinkAccount }: {
  onBack: () => void;
  onSignOut: () => void;
  onLinkAccount: () => void;
}) {
  const [music, setMusic]         = useState(true);
  const [notifs, setNotifs]       = useState(true);

  const ROW = {
    display: "flex", alignItems: "center", justifyContent: "space-between",
    padding: "16px 0",
  } as const;

  const DIVIDER = (
    <div style={{ height: 1, background: BORDER_SUBTLE }} />
  );

  return (
    <>
      {/* Header */}
      <div style={{
        padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 14px",
        display: "flex", alignItems: "center", gap: 12, flexShrink: 0,
      }}>
        <button onClick={onBack} style={{ background: "none", border: "none", cursor: "pointer", padding: "4px 2px", flexShrink: 0 }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="rgba(255,255,255,0.55)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="15,18 9,12 15,6" />
          </svg>
        </button>
        <h1 style={{
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 24,
          letterSpacing: "0.10em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
        }}>Ajustes</h1>
      </div>

      {/* Settings list */}
      <div style={{ flex: 1, padding: "6px 16px", paddingBottom: 108 }}>

        {/* Section card */}
        <div style={{
          background: CARD_BG, border: `1px solid ${BORDER_SUBTLE}`,
          borderRadius: 10, padding: "0 14px",
        }}>
          {/* Música */}
          <div style={ROW}>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
                stroke={music ? TEXT_WHITE : "rgba(255,255,255,0.40)"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <path d="M9 18V5l12-2v13"/>
                <circle cx="6" cy="18" r="3"/><circle cx="18" cy="16" r="3"/>
              </svg>
              <span style={{ fontSize: 15, color: music ? TEXT_WHITE : TEXT_GRAY }}>Música</span>
            </div>
            <Toggle value={music} onChange={setMusic} />
          </div>

          {DIVIDER}

          {/* Notificaciones */}
          <div style={ROW}>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
                stroke={notifs ? TEXT_WHITE : "rgba(255,255,255,0.40)"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
                <path d="M13.73 21a2 2 0 01-3.46 0"/>
              </svg>
              <span style={{ fontSize: 15, color: notifs ? TEXT_WHITE : TEXT_GRAY }}>Notificaciones</span>
            </div>
            <Toggle value={notifs} onChange={setNotifs} />
          </div>

          {DIVIDER}

          {/* Términos y privacidad */}
          <button style={{
            ...ROW, width: "100%", background: "none", border: "none", cursor: "pointer",
          }}>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.40)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/>
                <polyline points="14,2 14,8 20,8"/>
                <line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/>
                <polyline points="10,9 9,9 8,9"/>
              </svg>
              <span style={{ fontSize: 15, color: TEXT_GRAY }}>Términos y privacidad</span>
            </div>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.25)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="9,18 15,12 9,6" />
            </svg>
          </button>

          {DIVIDER}

          {/* Vincular cuenta */}
          <button onClick={onLinkAccount} style={{
            ...ROW, width: "100%", background: "none", border: "none", cursor: "pointer",
          }}>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.40)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <path d="M10 13a5 5 0 007.54.54l3-3a5 5 0 00-7.07-7.07l-1.72 1.71"/>
                <path d="M14 11a5 5 0 00-7.54-.54l-3 3a5 5 0 007.07 7.07l1.71-1.71"/>
              </svg>
              <span style={{ fontSize: 15, color: TEXT_GRAY }}>Vincular cuenta</span>
            </div>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.25)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="9,18 15,12 9,6" />
            </svg>
          </button>
        </div>

        {/* Cerrar sesión */}
        <button onClick={onSignOut} style={{
          marginTop: 32, width: "100%", padding: "14px 0",
          background: "transparent",
          border: "1px solid rgba(215,65,65,0.32)",
          borderRadius: 9,
          fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700,
          fontSize: 15, letterSpacing: "0.08em", textTransform: "uppercase",
          color: "rgba(225,75,75,0.88)",
          cursor: "pointer",
        }}>
          Cerrar sesión
        </button>

        {/* App version */}
        <p style={{ textAlign: "center", fontSize: 11, color: TEXT_DIM, marginTop: 24 }}>
          Versión 0.1.0 · Build 47
        </p>
      </div>
    </>
  );
}

function ProfileScreen({ onSignOut, onLinkAccount }: { onSignOut: () => void; onLinkAccount: () => void }) {
  const [copied, setCopied] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const friendCode = "4872-1093";

  const handleCopy = () => {
    navigator.clipboard?.writeText(friendCode).catch(() => {});
    setCopied(true);
    setTimeout(() => setCopied(false), 1800);
  };

  const assigned = FORMATION_SLOTS.filter((s) => s.rarity !== null).length;

  if (showSettings) {
    return (
      <SettingsScreen
        onBack={() => setShowSettings(false)}
        onSignOut={onSignOut}
        onLinkAccount={onLinkAccount}
      />
    );
  }

  return (
    <div style={{ flex: 1, display: "flex", flexDirection: "column", overflow: "hidden" }}>

      {/* Scrollable body */}
      <div className="profile-scroll" style={{
        flex: 1, overflowY: "auto", paddingBottom: 108,
      }}>

        {/* ── Header: settings + avatar + name + code ── */}
        <div style={{
          padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 28px",
          display: "flex", flexDirection: "column", alignItems: "center",
          position: "relative",
        }}>

          {/* Settings gear — top right */}
          <button onClick={() => setShowSettings(true)} style={{
            position: "absolute",
            top: "max(56px,calc(env(safe-area-inset-top) + 14px))",
            right: 16,
            background: "none", border: "none", cursor: "pointer", padding: 4,
          }}>
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
              stroke="rgba(255,255,255,0.45)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="3" />
              <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z" />
            </svg>
          </button>

          {/* Avatar with edit badge */}
          <div style={{ position: "relative", marginBottom: 16 }}>
            <div style={{
              width: 80, height: 80, borderRadius: "50%",
              background: "rgba(255,255,255,0.07)",
              border: `2px solid ${GOLD_BORDER}`,
              display: "flex", alignItems: "center", justifyContent: "center",
            }}>
              <svg width="36" height="36" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.4)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="8" r="4" />
                <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" />
              </svg>
            </div>
            {/* Edit badge on avatar */}
            <button style={{
              position: "absolute", bottom: 0, right: 0,
              width: 24, height: 24, borderRadius: "50%",
              background: CARD_BG,
              border: `1.5px solid ${GOLD_BORDER}`,
              display: "flex", alignItems: "center", justifyContent: "center",
              cursor: "pointer", padding: 0,
            }}>
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none"
                stroke={GOLD} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
              </svg>
            </button>
          </div>

          {/* Username + edit */}
          <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 8 }}>
            <span style={{ fontSize: 20, fontWeight: 700, letterSpacing: "0.04em", color: TEXT_WHITE }}>
              JUGADOR_01
            </span>
            <button style={{ background: "none", border: "none", cursor: "pointer", padding: 2 }}>
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none"
                stroke="rgba(255,255,255,0.35)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
              </svg>
            </button>
          </div>

          {/* Friend code + copy */}
          <button onClick={handleCopy} style={{
            background: "none", border: "none", cursor: "pointer", padding: "4px 0",
            display: "flex", alignItems: "center", gap: 6,
          }}>
            <span style={{ fontSize: 12, color: TEXT_GRAY, letterSpacing: "0.02em" }}>
              Código de amigo: <span style={{ color: "rgba(255,255,255,0.70)", fontWeight: 500 }}>{friendCode}</span>
            </span>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
              stroke={copied ? GOLD : "rgba(255,255,255,0.35)"}
              strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"
              style={{ transition: "stroke 0.2s" }}>
              <rect x="9" y="9" width="13" height="13" rx="2" />
              <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" />
            </svg>
            {copied && (
              <span style={{ fontSize: 10, color: GOLD, letterSpacing: "0.04em" }}>¡Copiado!</span>
            )}
          </button>
        </div>

        {/* ── Mi 11 ideal — tactical pitch ── */}
        <section style={{ padding: "0 16px", marginBottom: 32 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", marginBottom: 14 }}>
            <h2 style={{
              fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
              letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE, margin: 0,
            }}>Mi 11 ideal</h2>
            <span style={{ fontSize: 11, color: TEXT_GRAY }}>{assigned} / 11 espacios</span>
          </div>

          {/* Pitch container */}
          <div style={{
            position: "relative", width: "100%", height: 340,
            borderRadius: 10,
            overflow: "hidden",
            background: "rgba(8,18,12,0.70)",
            border: `1px solid ${BORDER_SUBTLE}`,
          }}>
            {/* Pitch lines SVG — same tonal language as the home screen background markings */}
            <svg style={{ position: "absolute", inset: 0, width: "100%", height: "100%", opacity: 0.10 }}
              viewBox="0 0 358 340" preserveAspectRatio="xMidYMid meet">
              {/* Boundary */}
              <rect x="4" y="4" width="350" height="332" stroke="white" strokeWidth="1.2" fill="none" rx="2" />
              {/* Halfway line */}
              <line x1="4" y1="170" x2="354" y2="170" stroke="white" strokeWidth="1.2" />
              {/* Centre circle */}
              <circle cx="179" cy="170" r="46" stroke="white" strokeWidth="1.2" fill="none" />
              <circle cx="179" cy="170" r="2.5" fill="white" />
              {/* Top penalty area */}
              <rect x="89" y="4" width="180" height="72" stroke="white" strokeWidth="1.2" fill="none" />
              {/* Top goal area */}
              <rect x="129" y="4" width="100" height="25" stroke="white" strokeWidth="1.2" fill="none" />
              {/* Top penalty spot */}
              <circle cx="179" cy="58" r="2" fill="white" />
              {/* Bottom penalty area */}
              <rect x="89" y="264" width="180" height="72" stroke="white" strokeWidth="1.2" fill="none" />
              {/* Bottom goal area */}
              <rect x="129" y="311" width="100" height="25" stroke="white" strokeWidth="1.2" fill="none" />
              {/* Bottom penalty spot */}
              <circle cx="179" cy="282" r="2" fill="white" />
              {/* Corner arcs */}
              {[
                "M4,20 A16,16,0,0,1,20,4",
                "M338,4 A16,16,0,0,1,354,20",
                "M4,320 A16,16,0,0,0,20,336",
                "M354,320 A16,16,0,0,1,338,336",
              ].map((d, i) => <path key={i} d={d} stroke="white" strokeWidth="1.2" fill="none" />)}
            </svg>

            {/* Player slots — absolutely positioned on pitch */}
            {FORMATION_SLOTS.map((slot) => (
              <PlayerSlot key={slot.id} pos={slot.pos} rarity={slot.rarity} x={slot.x} y={slot.y} />
            ))}
          </div>
        </section>

        {/* ── Cartas destacadas ── */}
        <section style={{ padding: "0 16px", marginBottom: 20 }}>
          <h2 style={{
            fontFamily: "'Barlow Condensed',sans-serif", fontWeight: 700, fontSize: 17,
            letterSpacing: "0.12em", textTransform: "uppercase", color: TEXT_WHITE,
            margin: 0, marginBottom: 16,
          }}>Cartas destacadas</h2>

          <div style={{ display: "flex", gap: 10 }}>
            {FEATURED_SLOTS.map((slot, i) => {
              const borderColor = rarityBorder(slot.rarity);
              const isMythic = slot.rarity === "Mítica";
              const rl = slot.rarity ? RARITY[slot.rarity] : null;

              const inner = (
                <div style={{
                  background: CARD_BG,
                  borderRadius: isMythic ? 7 : 8,
                  width: "100%", height: "100%",
                  display: "flex", flexDirection: "column",
                  alignItems: "center", justifyContent: "space-between",
                  padding: "16px 8px 12px",
                }}>
                  <div style={{
                    width: 40, height: 40, borderRadius: "50%",
                    background: "rgba(255,255,255,0.06)",
                    border: `1px solid ${borderColor}`,
                    display: "flex", alignItems: "center", justifyContent: "center",
                  }}>
                    {slot.ini && (
                      <span style={{ fontSize: 12, fontWeight: 700, color: rl?.label ?? TEXT_DIM }}>
                        {slot.ini}
                      </span>
                    )}
                  </div>
                  <span style={{
                    fontSize: 10, fontWeight: 400, color: slot.name ? TEXT_GRAY : TEXT_DIM,
                    textAlign: "center", lineHeight: 1.3,
                  }}>
                    {slot.name ?? "Vacío"}
                  </span>
                </div>
              );

              return isMythic ? (
                <div key={i} style={{
                  flex: 1, aspectRatio: "3/4",
                  background: RARITY["Mítica"].gradient,
                  padding: "1.5px", borderRadius: 9,
                }}>
                  {inner}
                </div>
              ) : (
                <div key={i} style={{
                  flex: 1, aspectRatio: "3/4",
                  border: `1.5px solid ${borderColor}`,
                  borderRadius: 8,
                }}>
                  {inner}
                </div>
              );
            })}
          </div>
        </section>
      </div>
    </div>
  );
}

// ── Root ──────────────────────────────────────────────────────────────────
type AppScreen = "splash" | "main" | "login-link" | "login-nosession";

export default function App() {
  const [appScreen, setAppScreen]     = useState<AppScreen>("splash");
  const [activeTab, setActiveTab]     = useState("inicio");
  const [communitySubscreen, setCommunitySubscreen] = useState<string | null>(null);

  React.useEffect(() => {
    if (appScreen === "splash") {
      const t = setTimeout(() => setAppScreen("main"), 2200);
      return () => clearTimeout(t);
    }
  }, [appScreen]);

  function handleTabSelect(tab: string) {
    setActiveTab(tab);
    if (tab !== "comunidad") setCommunitySubscreen(null);
  }

  /* ── Overlay screens (no tab bar) ── */
  if (appScreen === "splash") {
    return (
      <div style={{ minHeight: "100dvh", fontFamily: "'DM Sans',sans-serif", color: TEXT_WHITE, position: "relative" }}>
        <StadiumBackground />
        <SplashScreen />
      </div>
    );
  }

  if (appScreen === "login-link" || appScreen === "login-nosession") {
    return (
      <div style={{ minHeight: "100dvh", fontFamily: "'DM Sans',sans-serif", color: TEXT_WHITE, position: "relative" }}>
        <StadiumBackground />
        <LoginScreen
          variant={appScreen === "login-link" ? "link" : "nosession"}
          onLogin={() => setAppScreen("main")}
          onSkip={appScreen === "login-link" ? () => setAppScreen("main") : undefined}
        />
      </div>
    );
  }

  return (
    <div style={{
      minHeight: "100dvh", display: "flex", justifyContent: "center",
      fontFamily: "'DM Sans',sans-serif", color: TEXT_WHITE, position: "relative",
    }}>
      <StadiumBackground />

      <div style={{
        width: "100%", maxWidth: 390, height: "100dvh",
        display: "flex", flexDirection: "column",
        position: "relative", zIndex: 10,
      }}>
        {activeTab === "inicio"    && <HomeScreen />}
        {activeTab === "cartas"    && <MyCardsScreen />}
        {activeTab === "tienda"    && <StoreScreen />}
        {activeTab === "comunidad" && communitySubscreen === "vitrinas" && (
          <VitrinesScreen onBack={() => setCommunitySubscreen(null)} />
        )}
        {activeTab === "comunidad" && communitySubscreen === "intercambio" && (
          <TradeScreen onBack={() => setCommunitySubscreen(null)} />
        )}
        {activeTab === "comunidad" && communitySubscreen === "vender" && (
          <SellScreen onBack={() => setCommunitySubscreen(null)} />
        )}
        {activeTab === "comunidad" && communitySubscreen === "mercado" && (
          <MarketScreen onBack={() => setCommunitySubscreen(null)} />
        )}
        {activeTab === "comunidad" && communitySubscreen === "amigos" && (
          <FriendsScreen
            onBack={() => setCommunitySubscreen(null)}
            onNavigateTo={(sub) => setCommunitySubscreen(sub)}
          />
        )}
        {activeTab === "comunidad" && communitySubscreen === null && (
          <CommunityScreen onNavigate={(sub) => setCommunitySubscreen(sub)} />
        )}
        {activeTab === "perfil"    && (
          <ProfileScreen
            onSignOut={() => setAppScreen("login-nosession")}
            onLinkAccount={() => setAppScreen("login-link")}
          />
        )}

        <TabBar active={activeTab} onSelect={handleTabSelect} />
      </div>
    </div>
  );
}
