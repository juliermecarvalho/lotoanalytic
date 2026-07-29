---
name: LotoAnalytics
description: Landing web para analise estatistica, filtros, geracao e conferencia de jogos de loteria sem promessa de premio.
colors:
  analysis-bg: "#f3f6f7"
  surface: "#ffffff"
  ink: "#101b1d"
  muted-ink: "#526366"
  line: "#d5dee1"
  technical-teal: "#0e7c7b"
  technical-teal-dark: "#075f61"
  statistical-yellow: "#c99700"
  alert-red: "#b3262e"
  notice-ink: "#40585c"
  error-dark: "#8d1f27"
  analysis-wash: "#e7f1f1"
  pricing-wash: "#edf3f4"
  field-bg: "#f8fbfb"
  code-bg: "#0b2327"
  code-ink: "#e6f6f4"
  instrument-bg: "#0b2327"
  instrument-ink: "#e6f6f4"
  instrument-muted: "#93d8d2"
  instrument-soft: "#c7e7e4"
  proof-wash: "#e7f1f1"
typography:
  display:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "clamp(42px, 6vw, 76px)"
    fontWeight: 800
    lineHeight: 0.98
    letterSpacing: "0"
  headline:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "clamp(30px, 4vw, 48px)"
    fontWeight: 800
    lineHeight: 1.05
    letterSpacing: "0"
  title:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "20px"
    fontWeight: 800
    lineHeight: 1.2
  body:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "13px"
    fontWeight: 900
    lineHeight: 1.2
    letterSpacing: "0"
  small:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.45
  proof:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "18px"
    fontWeight: 800
    lineHeight: 1.2
  routine:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "19px"
    fontWeight: 800
    lineHeight: 1.2
  price:
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "34px"
    fontWeight: 900
    lineHeight: 1.1
rounded:
  md: "8px"
spacing:
  xs: "8px"
  sm: "12px"
  md: "16px"
  lg: "24px"
  xl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.criterion-teal}"
    textColor: "{colors.surface}"
    rounded: "{rounded.md}"
    padding: "12px 18px"
    height: "46px"
  button-primary-hover:
    backgroundColor: "{colors.criterion-teal-dark}"
    textColor: "{colors.surface}"
    rounded: "{rounded.md}"
    padding: "12px 18px"
    height: "46px"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "12px 18px"
    height: "46px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "22px"
  input:
    backgroundColor: "{colors.field-bg}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "8px 10px"
    height: "42px"
---

# Design System: LotoAnalytics

## 1. Overview

**Creative North Star: "Sala de análise responsável"**

The LotoAnalytics system should feel like a calm analysis panel for a risky domain: clear, criterioso, and commercially contained. It sells a product, but the visual tone must never borrow the pressure, color noise, or false certainty of gambling funnels.

The current system uses a cold neutral surface, teal as the primary analytical signal, compact 8px geometry, and a dark analytical instrument panel for criteria. The direction is practical and readable first. The design should make filters, statistics, checking, and pricing feel inspectable, not mysterious.

It explicitly rejects the anti-references in PRODUCT.md: guaranteed-prize language, "numeros certeiros", "metodo infalivel", aggressive betting-house aesthetics, fake urgency, and generic SaaS landing-page patterns disconnected from lottery analysis.

**Key Characteristics:**
- Cold light page background with white analytical surfaces and one dark criteria instrument panel.
- Teal primary actions reserved for progress and access.
- Dense but readable sections with practical component shapes.
- Almost-flat elevation, using shadow only to separate important surfaces.
- Responsible commercial tone with no visual manipulation.

## 2. Colors

The palette is a restrained analytical teal system on a cold neutral background, with blue and gold used only as small informational signals.

### Primary
- **Technical Teal** (`criterion-teal`): the main action and brand signal. Use for primary CTAs, the LA mark, and selected/actionable moments.
- **Deep Technical Teal** (`criterion-teal-dark`): the hover and emphasis state for primary teal actions. Use sparingly so the base teal remains legible.

### Secondary
- **Statistical Yellow** (`signal-gold`): a numeric or sequence accent. It may mark ordered routines, but it must not imply prize certainty.
- **Alert Red** (`warning-red`): reserved for future warnings or validation states. Do not use it as a sales urgency color.
- **Notice Ink** (`notice-brown`): responsible-lottery notice text near commercial claims.
- **Error Dark** (`error-dark`): generator validation feedback when the user enters an invalid range.

### Neutral
- **Analysis Background** (`analysis-bg`): the cold page canvas.
- **Surface White** (`surface`): cards, panels, and metric cells.
- **Ink** (`ink`): primary text and dark header CTA.
- **Muted Ink** (`muted-ink`): supporting copy. Keep contrast strong enough for WCAG AA.
- **Line** (`line`): borders, dividers, and quiet structure.
- **VIP Wash** (`vip-wash`): low-intensity teal section background.
- **Pricing Wash** (`pricing-wash`): pale pricing section background.
- **Field Background** (`field-bg`): form and select interiors.
- **Code Panel** (`code-bg` / `code-ink`): generated-game output.
- **Instrument Panel** (`instrument-bg` / `instrument-ink` / `instrument-muted` / `instrument-soft`): hero criteria panel and high-trust analytical moments.
- **Proof Wash** (`proof-wash`): generator proof section background.

### Named Rules

**The No-Hype Color Rule.** Teal means criteria, access, and workflow. It never means certainty, jackpot, or "winning numbers".

**The Accent Rarity Rule.** Yellow and red are rare signals. If they compete with teal, the page starts to feel like generic gambling promotion.

## 3. Typography

**Display Font:** Inter/system UI stack  
**Body Font:** Inter/system UI stack  
**Label/Mono Font:** none

**Character:** The type is direct and operational. It should read like an analytical product brochure, not a casino ad, financial promise, or decorative editorial page.

### Hierarchy
- **Display** (800, `clamp(42px, 6vw, 76px)`, `0.98`): hero headlines only. Keep letter spacing at `0`; do not tighten below `-0.04em`.
- **Headline** (800, `clamp(30px, 4vw, 48px)`, `1.05`): section headlines.
- **Title** (800, `20px`, `1.2`): card and panel titles.
- **Body** (400, `16px`, `1.5`): explanations, FAQ copy, and product details. Keep long prose around 65-75ch.
- **Label** (900, `13px`, uppercase): current section kickers. Use only when the label materially helps scanning.
- **Small** (400/800, `14px`): navigation links, header CTA, notices, method labels, and generator status.
- **Proof** (800, `18px`): proof-strip statements and compact emphasis.
- **Routine** (800, `19px`): VIP timeline step titles.
- **Price** (900, `34px`): pricing amounts only.

### Named Rules

**The Plain Claim Rule.** Type should make the claim easier to inspect. Do not use dramatic display treatments to compensate for weak proof.

**The Kicker Restraint Rule.** Repeated uppercase eyebrows are allowed only while they improve navigation. Future redesigns should reduce or vary them if they start reading as landing-page scaffolding.

## 4. Elevation

This system is almost flat. Depth is conveyed through background shifts, borders, and occasional low shadows for panels that need to separate from the page. Shadows are not decorative atmosphere; they are structural emphasis.

### Shadow Vocabulary
- **Panel Low** (`0 10px 24px rgba(23, 34, 31, 0.06)`): default feature cards, FAQ items, and generator panels.
- **Featured Lift** (`0 18px 45px rgba(23, 34, 31, 0.12)`): the featured price card only.
- **Hero Preview Drop** (`drop-shadow(0 24px 40px rgba(23, 34, 31, 0.2))`): screenshot/mockup preview treatment.

### Named Rules

**The Almost-Flat Rule.** Surfaces are flat by default. Use shadow only when a panel must stand forward from the page or represent a featured commercial choice.

**The No Ghost-Card Rule.** Avoid pairing a 1px border with a large soft shadow on ordinary cards. If the blur exceeds 16px, the component must earn that emphasis.

## 5. Components

### Buttons
- **Shape:** compact rounded rectangles (`8px`).
- **Primary:** technical teal background with white text, minimum height `46px`, padding `12px 18px`, heavy label weight.
- **Hover / Focus:** hover darkens to deep technical teal. Focus states should be added with a visible outline or ring before production.
- **Secondary:** white background, line border, ink text. It should feel precise and discrete, not visually equal to the primary CTA.

### Chips
- **Style:** no reusable chip system exists yet.
- **State:** when introduced for filters, chips should use line borders and muted fills first; use teal only for selected criteria.

### Cards / Containers
- **Corner Style:** compact (`8px`).
- **Background:** white surfaces on the cold analysis background.
- **Shadow Strategy:** panel low by default; featured lift only for the highlighted pricing plan.
- **Border:** quiet line border. Do not use thick side-stripe borders.
- **Internal Padding:** `22px` for feature cards, `24px` for panels and price cards, `18px 20px` for FAQ details.

### Inputs / Fields
- **Style:** line border, `8px` radius, `#fbfcfa` field background, ink text.
- **Focus:** a visible focus treatment is required before shipping beyond local validation.
- **Error / Disabled:** not implemented yet. Future errors should use warning red as text/border state without alarmist copy.

### Navigation
- **Style:** sticky top header with cold translucent background, 1px line divider, and compact brand mark.
- **Typography:** nav links use muted ink at `14px`; hover moves to ink.
- **Mobile:** nav hides under `920px`; header CTA hides under `760px`. A future mobile menu is needed if more sections or actions are added.

### Generator Panel

The generator is the signature proof component. It should stay more prominent than supporting content: clear labels, a criteria summary, predictable controls, reset/copy actions, and a dark output panel that makes generated games easy to copy and scan. It must never output a game that failed the selected criteria without telling the user.

## 6. Do's and Don'ts

### Do:
- **Do** use Technical Teal for primary action, access, and workflow progress.
- **Do** keep responsible warning copy visible near the hero and commercial offer.
- **Do** make filters, generated games, pricing, and FAQ items inspectable with simple spacing and clear text.
- **Do** treat the generator as the primary proof moment, not as a secondary demo panel.
- **Do** preserve WCAG AA contrast, keyboard navigation, semantic structure, and reduced-motion alternatives.
- **Do** leave room for future proof: real spreadsheet screenshots, testimonials, and usage outcomes.

### Don't:
- **Don't** promise guaranteed prize, guaranteed result, or increased chance of winning.
- **Don't** use "numeros certeiros", "metodo infalivel", "jogos vencedores", or similar language.
- **Don't** use aggressive betting-house aesthetics, fake urgency, countdown pressure, or alarmist red as a sales device.
- **Don't** let the page become a generic SaaS landing page with disconnected metrics, repeated card grids, and repeated uppercase eyebrows.
- **Don't** use thick colored `border-left` or `border-right` stripes on cards, list items, callouts, or timeline items.
- **Don't** pair large soft shadows with borders on ordinary cards; reserve lift for commercial emphasis or functional separation.







