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
      padding: "8px 6px 10px", zIndex: 40,
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

// ── Screen: Inicio ────────────────────────────────────────────────────────
const ENVELOPES = [
  { label: "Sobre A", featured: false },
  { label: "Sobre B", featured: true  },
  { label: "Sobre C", featured: false },
];

function HomeScreen() {
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
        padding: "4px 16px", paddingBottom: 96, gap: 32, overflowY: "auto",
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
          <button style={{
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
        padding: "0 16px", paddingBottom: 96,
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
    key: "vender",
    label: "Vender duplicados",
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

function CommunityScreen() {
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
        flex: 1, padding: "0 16px", paddingBottom: 96, overflowY: "auto",
        display: "flex", flexDirection: "column", gap: 12,
      }}>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
          {COMMUNITY_ITEMS.map((item) => (
            <button
              key={item.key}
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
              {/* Badge */}
              {item.badge !== null && (
                <div style={{
                  position: "absolute", top: 10, right: 10,
                  minWidth: 20, height: 20, borderRadius: 999,
                  background: "rgba(0,0,0,0.55)",
                  border: `1px solid ${GOLD_BORDER}`,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  padding: "0 5px",
                }}>
                  <span style={{
                    fontSize: 10, fontWeight: 700, color: GOLD, letterSpacing: "0.02em",
                  }}>{item.badge}</span>
                </div>
              )}

              {item.icon}

              <span style={{
                fontSize: 12, fontWeight: 500, color: TEXT_GRAY,
                letterSpacing: "0.03em", textAlign: "center", lineHeight: 1.35,
              }}>
                {item.label}
              </span>
            </button>
          ))}
        </div>
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

function ProfileScreen() {
  const [copied, setCopied] = useState(false);
  const friendCode = "4872-1093";

  const handleCopy = () => {
    navigator.clipboard?.writeText(friendCode).catch(() => {});
    setCopied(true);
    setTimeout(() => setCopied(false), 1800);
  };

  const assigned = FORMATION_SLOTS.filter((s) => s.rarity !== null).length;

  return (
    <div style={{ flex: 1, display: "flex", flexDirection: "column", overflow: "hidden" }}>

      {/* Scrollable body */}
      <div className="profile-scroll" style={{
        flex: 1, overflowY: "auto", paddingBottom: 96,
      }}>

        {/* ── Header: settings + avatar + name + code ── */}
        <div style={{
          padding: "max(56px,calc(env(safe-area-inset-top) + 14px)) 16px 28px",
          display: "flex", flexDirection: "column", alignItems: "center",
          position: "relative",
        }}>

          {/* Settings gear — top right */}
          <button style={{
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
export default function App() {
  const [activeTab, setActiveTab] = useState("inicio");

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
        {activeTab === "comunidad" && <CommunityScreen />}
        {activeTab === "perfil"    && <ProfileScreen />}

        <TabBar active={activeTab} onSelect={setActiveTab} />
      </div>
    </div>
  );
}
